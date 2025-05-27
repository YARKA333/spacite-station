using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Server.GameObjects;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.YARtech;
using Content.Server.Popups;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.Damage.Components;
using Content.Server.Physics;

namespace Content.Server.YARtech;

public sealed partial class PepelSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PhysicsSystem _phys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PepelComponent, StartCollideEvent>(OnCollideEvent);
        SubscribeLocalEvent<PepelComponent, InteractHandEvent>(OnHandInteract);
    }

    private void OnCollideEvent(EntityUid uid, PepelComponent pep, ref StartCollideEvent args)
    {

        var target = args.OtherEntity;
        if (HasComp<GodmodeComponent>(target)
         || HasComp<PepelImmuneComponent>(target)
         || _container.IsEntityInContainer(uid)
         || EntityManager.IsQueuedForDeletion(target))
            return;

        //args.OtherBody.BodyType == BodyType.Static

        if (!HasComp<ProjectileComponent>(target))
        {
            var pepel = EntityManager.SpawnEntity(pep.CollisionResultPrototype, Transform(target).Coordinates);

            _phys.SetLinearVelocity(pepel, _phys.GetMapLinearVelocity(target));
            _phys.SetAngularVelocity(pepel, _phys.GetMapAngularVelocity(target));

            _audio.PlayPvs(pep.DustSound, uid);
        }
        var baseXform = Transform(target);
        _popup.PopupCoordinates(Loc.GetString("admin-smite-pepel-others", ("name", target)), baseXform.Coordinates,
            Filter.PvsExcept(target), true, PopupType.MediumCaution);

        EntityManager.QueueDeleteEntity(target);
    }

    private void OnHandInteract(EntityUid uid, PepelComponent pep, ref InteractHandEvent args)
    {

        var target = args.User;

        if (HasComp<GodmodeComponent>(target)
         || HasComp<PepelImmuneComponent>(target))
            return;


        EntityManager.SpawnEntity(pep.CollisionResultPrototype, Transform(target).Coordinates);
        _audio.PlayPvs(pep.DustSound, uid);
        var baseXform = Transform(args.Target);
        _popup.PopupCoordinates(Loc.GetString("admin-smite-pepel-others", ("name", args.Target)), baseXform.Coordinates,
        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
        EntityManager.QueueDeleteEntity(target);
    }

}
