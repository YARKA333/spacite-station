using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Content.Shared.YARtech.HumanoidAppearanceExtension;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Shared.YARtech.CustomHeight
{
    public abstract class SharedCustomHeightSystem : EntitySystem
    {
        [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CustomHeightComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CustomHeightComponent, HumanoidAppearanceLoadedEvent>(OnLoaded);
            SubscribeLocalEvent<CustomHeightComponent, HumanoidAppearanceClonedEvent>(OnCloned);
        }

        private void OnCloned(EntityUid uid, CustomHeightComponent component, HumanoidAppearanceClonedEvent args)
        {
            if (AppearanceSystem.TryGetData(uid, HeightVisuals.State, out float height, null))
            {
                SetHeight(args.Target.Owner, height);
            }
        }

        private void OnLoaded(EntityUid uid, CustomHeightComponent component, HumanoidAppearanceLoadedEvent args)
        {
            SetHeight(uid, GetHeightFromByte(uid, args.Profile.Appearance.Height));
        }

        private void OnInit(EntityUid uid, CustomHeightComponent component, ComponentInit args)
        {
            if (!HasComp<HumanoidAppearanceComponent>(uid))
            {
                if (component.Random)
                {
                    component.Starting = _robustRandom.NextFloat(component.Min, component.Max);
                }
                if (component.Starting != 1f)
                {
                    SetHeight(uid, component.Starting);
                }
            }
        }

        public void SetHeight(Entity<CustomHeightComponent?> entity, float height)
        {
            if (!Resolve(entity, ref entity.Comp))
            {
                entity.Comp = EnsureComp<CustomHeightComponent>(entity);
            }
            height = Math.Clamp(height, entity.Comp.Min, entity.Comp.Max);
            AppearanceSystem.SetData(entity, HeightVisuals.State, height);
        }

        public float GetHeightFromByte(Entity<CustomHeightComponent?> entity, byte per)
        {
            if (!Resolve(entity, ref entity.Comp))
            {
                entity.Comp = EnsureComp<CustomHeightComponent>(entity);
            }
            var percent = per / 255f;
            var min = entity.Comp.Min;
            var max = entity.Comp.Max;
            return min + (max - min) * percent;
        }

        public byte GetByteFromHeight(Entity<CustomHeightComponent?> entity, float? varheight = null)
        {
            if (!Resolve(entity, ref entity.Comp))
            {
                entity.Comp = EnsureComp<CustomHeightComponent>(entity);
            }
            var min = entity.Comp.Min;
            var max = entity.Comp.Max;
            var valueOrDefault = varheight.GetValueOrDefault();
            if (!varheight.HasValue)
            {
                valueOrDefault = AppearanceSystem.TryGetData(entity, HeightVisuals.State, out float height, null) ? height : min;
                varheight = valueOrDefault;
            }
            return (byte)((varheight.Value - min) / (max - min) * 255f);
        }
    }
}

