using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.GameObjects;
using Robust.Client.Timing;

namespace Content.Shared.YARtech
{
    internal sealed class ElprimoSystem : SharedElprimoSystem
    {
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly TransformSystem _xformSys = default!;
        [Dependency] private readonly EntityManager _entMan = default!;

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            var query = EntityQueryEnumerator<ElprimoComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                Update(uid, comp, frameTime);
            }
        }

        public void Update(EntityUid uid, ElprimoComponent comp, float delta)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
            {
                return;
            }

            if (HasComp<TransformComponent>(uid) & HasComp<TransformComponent>(comp.Target))
            {
                _xformSys.SetCoordinates(uid, Transform(comp.Target).Coordinates);
            }

            var span = (float)(_timing.CurTime - comp.SpawnTime).TotalSeconds;
            var a = Math.Clamp(span / comp.AnimLength, 0f, 1f);

            if (span < 30 * delta) { return; }

            var x = comp.JumpLength * (1 - a);
            var y = comp.JumpHeight * (1 - MathF.Pow(2 * a - 1, 2));
            sprite.Offset = new(x, y);
        }

    }
}
