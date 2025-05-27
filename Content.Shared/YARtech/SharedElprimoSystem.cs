using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Timing;

namespace Content.Shared.YARtech
{
    public abstract class SharedElprimoSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ElprimoComponent, ComponentInit>(OnComponentInit);
        }

        protected virtual void OnComponentInit(Entity<ElprimoComponent> ent, ref ComponentInit args)
        {
            ent.Comp.SpawnTime = _timing.CurTime;
        }


    }
}
