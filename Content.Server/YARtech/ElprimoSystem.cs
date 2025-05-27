using CancellationToken = System.Threading.CancellationToken;
using Robust.Shared.Timing;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Content.Shared.Body.Components;
using Content.Server.Destructible;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;

namespace Content.Shared.YARtech
{
    internal sealed class ElprimoSystem : SharedElprimoSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly EntityManager _entMan = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly DestructibleSystem _destruct = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly TransformSystem _xformSys = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ElprimoComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                Update(uid, comp);
            }
        }

        public void Update(EntityUid uid, ElprimoComponent comp)
        {
            if (comp.Target == default) { return; }
            var target = comp.Target;
            if (!Deleted(target))
            {
                _xformSys.SetCoordinates(uid, Transform(target).Coordinates);
            }
               


            var span = (float)(_timing.CurTime - comp.SpawnTime).TotalSeconds;
            if ((span > comp.SoundDelay) & !comp.Played)
            {
                comp.Played = true;
                _audio.PlayPvs(comp.Sound, uid);
            }
            if (span > comp.AnimLength)
            {
                if (!comp.Exploded)
                {
                    comp.Exploded = true;

                    var coords = _xformSys.GetMapCoordinates(target);
                    _xformSys.SetCoordinates(uid, Transform(target).Coordinates);
                    if (HasComp<BodyComponent>(target))
                    {
                        _bodySystem.GibBody(target, true, splatModifier: 10);
                        return;
                    }
                    else
                    {
                        _destruct.DestroyEntity(target);
                    }
                    Timer.Spawn(_timing.TickPeriod,
                        () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                            4, 1, 2, target, maxTileBreak: 0), CancellationToken.None);
                }
                else if (!_audio.IsPlaying(uid))
                {
                    _entMan.QueueDeleteEntity(uid);
                }
            }
        }
    }
}
