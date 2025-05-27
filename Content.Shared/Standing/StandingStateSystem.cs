using Content.Shared.Hands.Components;
using Content.Shared.Physics;
using Content.Shared.Rotation;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Input.Binding;
using Content.Shared.Input;
using Content.Shared.Movement;
using Robust.Shared.Serialization;
using Robust.Shared.Player;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Slippery;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;
using Content.Shared.Stunnable;

namespace Content.Shared.Standing
{
    public sealed class StandingStateSystem : EntitySystem
    {

        public enum DropHeldItemsBehavior : byte
        {
            NoDrop,
            DropIfStanding,
            AlwaysDrop
        }

        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
        [Dependency] private readonly SharedBuckleSystem _buckle = default!;
        [Dependency] private readonly SharedRotationVisualsSystem _rotation = default!;

        // If StandingCollisionLayer value is ever changed to more than one layer, the logic needs to be edited.
        private const int StandingCollisionLayer = (int) CollisionGroup.MidImpassable;

        public override void Initialize()
        {
            base.Initialize();
            CommandBinds.Builder.Bind(ContentKeyFunctions.LieDown, InputCmdHandler.FromDelegate(ChangeLyingState)).Register<StandingStateSystem>();
            SubscribeNetworkEvent<ChangeStandingStateEvent>(OnChangeState);
            SubscribeLocalEvent<StandingStateComponent, StandingUpDoAfterEvent>(OnStandingUpDoAfter);
            SubscribeLocalEvent<StandingStateComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
            SubscribeLocalEvent<StandingStateComponent, SlipAttemptEvent>(OnSlipAttempt);
        }

        private void OnChangeState(ChangeStandingStateEvent ev, EntitySessionEventArgs args)
        {
            if (!args.SenderSession.AttachedEntity.HasValue)
            {
                return;
            }
            var uid = args.SenderSession.AttachedEntity.Value;
            if (!TryComp(uid, out StandingStateComponent? standing))
            {
                return;
            }

            RaiseNetworkEvent(new CheckAutoGetUpEvent());
            if (!HasComp<KnockedDownComponent>(uid) && _mobState.IsAlive(uid))
            {
                if (IsDown(uid, standing))
                {
                    TryStandUp(uid, standing);
                }
                else
                {
                    TryLieDown(uid, standing);
                }
            }
        }

        private void OnStandingUpDoAfter(EntityUid uid, StandingStateComponent component, StandingUpDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || HasComp<KnockedDownComponent>(uid) || _mobState.IsIncapacitated(uid) || !Stand(uid))
            {
                component.CurrentState = StandingState.Lying;
            }
        }

        private void OnRefreshMovementSpeed(EntityUid uid, StandingStateComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (IsDown(uid))
            {
                args.ModifySpeed(0.4f, 0.4f);
            }
            else
            {
                args.ModifySpeed(1f, 1f);
            }
        }

        private void OnSlipAttempt(EntityUid uid, StandingStateComponent component, SlipAttemptEvent args)
        {
            if (IsDown(uid))
            {
                args.NoSlip = true;
            }
        }

        private void ChangeLyingState(ICommonSession? session)
        {
            if (session != null && session.AttachedEntity.HasValue && TryComp(session.AttachedEntity, out StandingStateComponent? standing) && standing.CanLieDown)
            {
                RaiseNetworkEvent(new ChangeStandingStateEvent());
            }
        }

        public bool TryStandUp(EntityUid uid, StandingStateComponent? standingState)
        {
            if (!Resolve(uid, ref standingState, false))
            {
                return false;
            }
            if (standingState.CurrentState != StandingState.Lying)
            {
                return false;
            }
            var doargs = new DoAfterArgs(EntityManager, uid, standingState.StandingUpTime, new StandingUpDoAfterEvent(), uid)
            {
                BreakOnMove = false,
                BreakOnDamage = false,
                BreakOnHandChange = false,
                RequireCanInteract = false
            };
            if (!_doAfter.TryStartDoAfter(doargs))
            {
                return false;
            }
            standingState.CurrentState = StandingState.GettingUp;
            return true;
        }

        public bool TryLieDown(EntityUid uid, StandingStateComponent? standingState, DropHeldItemsBehavior behavior = DropHeldItemsBehavior.NoDrop)
        {
            if (!Resolve(uid, ref standingState, logMissing: false) || !standingState.CanLieDown || standingState.CurrentState != StandingState.Standing)
            {
                if (behavior == DropHeldItemsBehavior.AlwaysDrop)
                {
                    RaiseLocalEvent(uid, new DropHandItemsEvent());
                }
                return false;
            }
            Down(uid, playSound: true, behavior != DropHeldItemsBehavior.NoDrop, standingState);
            return true;
        }

        public bool IsDown(EntityUid uid, StandingStateComponent? standingState = null)
        {
            if (!Resolve(uid, ref standingState, false))
                return false;

            return standingState.CurrentState < StandingState.Standing;
        }

        public bool Down(EntityUid uid,
            bool playSound = true,
            bool dropHeldItems = true,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null,
            HandsComponent? hands = null)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, ref hands, false);

            if (TryComp(uid, out BuckleComponent? buckle) && buckle.Buckled && !_buckle.TryUnbuckle(uid, uid, buckle))
            {
                return false;
            }

            // This is just to avoid most callers doing this manually saving boilerplate
            // 99% of the time you'll want to drop items but in some scenarios (e.g. buckling) you don't want to.
            // We do this BEFORE downing because something like buckle may be blocking downing but we want to drop hand items anyway
            // and ultimately this is just to avoid boilerplate in Down callers + keep their behavior consistent.
            if (dropHeldItems && hands != null)
            {
                RaiseLocalEvent(uid, new DropHandItemsEvent(), false);
            }

            if (standingState.CurrentState < StandingState.Standing)
            {
                return true;
            }

            var msg = new DownAttemptEvent();
            RaiseLocalEvent(uid, msg, false);
            if (msg.Cancelled)
                return false;

            standingState.CurrentState = StandingState.Lying;
            Dirty(uid, standingState);

            if (TryComp(uid, out TransformComponent? transform))
            {
                var rotation = transform.LocalRotation;
                _appearance.TryGetData(uid, (Enum)BuckleVisuals.Buckled, out bool buckled, appearance);
                if (!buckled && (!_appearance.TryGetData(uid, (Enum)MobStateVisuals.State, out MobState state, appearance) || state == MobState.Alive))
                {
                    var dir = rotation.GetDir();
                    if ((uint)(dir - 1) <= 3u)
                    {
                        _rotation.SetHorizontalAngle(uid, Angle.FromDegrees(270.0));
                    }
                    else
                    {
                        _rotation.ResetHorizontalAngle(uid);
                    }
                }
            }

            RaiseLocalEvent(uid, new DownedEvent(), false);

            // Seemed like the best place to put it
            _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal, appearance);

            // Change collision masks to allow going under certain entities like flaps and tables
            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var (key, fixture) in fixtureComponent.Fixtures)
                {
                    if ((fixture.CollisionMask & StandingCollisionLayer) == 0)
                        continue;

                    standingState.ChangedFixtures.Add(key);
                    _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask & ~StandingCollisionLayer, manager: fixtureComponent);
                }
            }

            // check if component was just added or streamed to client
            // if true, no need to play sound - mob was down before player could seen that
            if (standingState.LifeStage <= ComponentLifeStage.Starting)
                return true;

            if (playSound)
            {
                _audio.PlayPredicted(standingState.DownSound, uid, uid);
            }
            _movement.RefreshMovementSpeedModifiers(uid);
            return true;
        }

        public bool Stand(EntityUid uid,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null,
            bool force = false,
            bool unbuckle = true)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, false);

            if (unbuckle && TryComp(uid, out BuckleComponent? buckle) && buckle.Buckled && !_buckle.TryUnbuckle(uid, uid, buckle))
            {
                return false;
            }

            if (standingState.CurrentState == StandingState.Standing)
            {
                return true;
            }

            if (!force)
            {
                var msg = new StandAttemptEvent();
                RaiseLocalEvent(uid, msg, false);

                if (msg.Cancelled)
                    return false;
            }

            standingState.CurrentState = StandingState.Standing;
            Dirty(uid, standingState);
            RaiseLocalEvent(uid, new StoodEvent(), false);

            _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical, appearance);

            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var key in standingState.ChangedFixtures)
                {
                    if (fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                        _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask | StandingCollisionLayer, fixtureComponent);
                }
            }
            standingState.ChangedFixtures.Clear();
            _movement.RefreshMovementSpeedModifiers(uid);
            return true;
        }
    }

    [Serializable]
    [NetSerializable]
    public sealed class CheckAutoGetUpEvent : CancellableEntityEventArgs
    {
    }

    [Serializable]
    [NetSerializable]
    public sealed partial class StandingUpDoAfterEvent : SimpleDoAfterEvent
    {
    }

    public enum StandingState
    {
        Lying,
        GettingUp,
        Standing
    }

    [Serializable]
    [NetSerializable]
    public sealed class ChangeStandingStateEvent : CancellableEntityEventArgs
    {
    }


    public sealed class DropHandItemsEvent : EventArgs
    {
    }

    /// <summary>
    /// Subscribe if you can potentially block a down attempt.
    /// </summary>
    public sealed class DownAttemptEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Subscribe if you can potentially block a stand attempt.
    /// </summary>
    public sealed class StandAttemptEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity becomes standing
    /// </summary>
    public sealed class StoodEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity is not standing
    /// </summary>
    public sealed class DownedEvent : EntityEventArgs
    {
    }
}
