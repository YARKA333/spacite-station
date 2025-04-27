using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Server.GameObjects;
//using Content.Shared.Atmos;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.Mobs.Components;
using Content.Shared.YARtech;
//using Content.Server.Atmos.EntitySystems;
//using Content.Server.Chat.Systems;
//using Content.Server.Explosion.EntitySystems;
//using Content.Server.Lightning;
//using Content.Server.AlertLevel;
//using Content.Server.Station.Systems;
//using Content.Server.Kitchen.Components;
//using Content.Shared.DoAfter;
//using Content.Shared.Examine;
//using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Popups;
//using Content.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Damage.Components;

namespace Content.Server.YARtech;

public sealed partial class PepelSystem : EntitySystem
{
    //[Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    //[Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    //[Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    //[Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    //[Dependency] private readonly LightningSystem _lightning = default!;
    //[Dependency] private readonly AlertLevelSystem _alert = default!;
    //[Dependency] private readonly StationSystem _station = default!;
    //[Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    //[Dependency] private readonly IConfigurationManager _config = default!;

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
            EntityManager.SpawnEntity(pep.CollisionResultPrototype, Transform(target).Coordinates);
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
