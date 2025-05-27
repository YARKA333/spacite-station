using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Shared.Inventory;
using Content.Server.Nutrition.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Actions;

using Content.Shared.YARtech.SelfHeal;

namespace Content.Server.YARtech.SelfHeal;

public sealed class SelfHealSystem : EntitySystem
{

    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    //anything that use IngestionBlocker and will be not blocking by default
    private readonly string[] _needBlocker = { "head", "mask" };


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfHealComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<SelfHealComponent, SelfHealEvent>(OnAction);
        SubscribeLocalEvent<DamageableComponent, SelfHealDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(EntityUid uid, SelfHealComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnAction(EntityUid uid, SelfHealComponent component, SelfHealEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        TryHeal(args.Performer, args.Target, component);

    }

    private bool TryHeal(EntityUid user, EntityUid target, SelfHealComponent component)
    {
        if (!TryComp<DamageableComponent>(target, out var targetDamage))
            return false;

        if (component.DamageContainers is not null &&
            targetDamage.DamageContainerID is not null &&
            !component.DamageContainers.Contains(targetDamage.DamageContainerID))
        {
            return false;
        }

        if (user != target && !_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return false;

        var userIdentity = Identity.Entity(user, EntityManager);
        var targetIdentity = Identity.Entity(target, EntityManager);

        if (component.DisallowedClothingUser != null &&
            CheckBlocked(user, component.DisallowedClothingUser, out var blocker1))
        {
            _popupSystem.PopupEntity(Loc.GetString(
                "self-heal-cant-use-clothing",
                ("clothing", blocker1)), user, user);
            return false;
        }

        if (component.DisallowedClothingTarget != null &&
            CheckBlocked(target, component.DisallowedClothingTarget, out var blocker2))
        {
            _popupSystem.PopupEntity(Loc.GetString(
                user != target ? "self-heal-cant-use-clothing-other" : "self-heal-cant-use-clothing",
                ("clothing", blocker2)), user, user);
            return false;
        }

        if (!HasDamage(targetDamage, component))
        {
            _popupSystem.PopupEntity(Loc.GetString("self-heal-cant-use", ("name", targetIdentity)), user, user);
            return false;
        }

        _audio.PlayPvs(component.HealingSound, user,
                AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));


        var msg = user != target ?
            Loc.GetString("self-heal-using-other", ("user", userIdentity), ("target", targetIdentity)) :
            Loc.GetString("self-heal-using-self", ("user", userIdentity));

        _popupSystem.PopupEntity(msg, user, PopupType.Medium);


        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, component.Delay, new SelfHealDoAfterEvent(), target, target: target)
            {
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    private void OnDoAfter(Entity<DamageableComponent> entity, ref SelfHealDoAfterEvent args)
    {

        if (!TryComp(args.User, out SelfHealComponent? component))
            return;

        if (args.Handled || args.Cancelled)
            return;

        if (component.DamageContainers is not null &&
            entity.Comp.DamageContainerID is not null &&
            !component.DamageContainers.Contains(entity.Comp.DamageContainerID))
        {
            return;
        }

        var healed = _damageable.TryChangeDamage(entity.Owner, component.Damage, true, origin: args.Args.User);

        if (healed == null)
            return;

        var total = healed?.GetTotal() ?? FixedPoint2.Zero;

        if (entity.Owner != args.User)
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed {EntityManager.ToPrettyString(entity.Owner):target} for {total:damage} damage");
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed themselves for {total:damage} damage");
        }

        _audio.PlayPvs(component.HealingSound, entity.Owner, AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        // Logic to determine the whether or not to repeat the healing action
        args.Repeat = HasDamage(entity.Comp, component);
        if (!args.Repeat)
            _popupSystem.PopupEntity(Loc.GetString("self-heal-finished-using", ("name", entity.Owner)), entity.Owner, args.User);
        args.Handled = true;
    }

    private bool CheckBlocked(EntityUid entity, List<string> slots, [NotNullWhen(true)] out EntityUid? blocking)
    {
        blocking = null;
        if (!HasComp<InventoryComponent>(entity))
            return false;

        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(entity, slot, out var itemUid) &&
                (TryComp<IngestionBlockerComponent>(itemUid, out var blocker) ? blocker.Enabled : !_needBlocker.Contains(slot)))
            {
                blocking = itemUid;
                return true;
            }
        }
        return false;
    }

    private bool HasDamage(DamageableComponent component, SelfHealComponent healing)
    {
        var damageableDict = component.Damage.DamageDict;
        var healingDict = healing.Damage.DamageDict;
        foreach (var type in healingDict)
        {
            if (damageableDict[type.Key].Value > 0)
            {
                return true;
            }
        }
        return false;
    }
}
