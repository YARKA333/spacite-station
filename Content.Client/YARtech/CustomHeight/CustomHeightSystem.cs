using System;
using System.Numerics;
using Content.Shared.YARtech.CustomHeight;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.YARtech.CustomHeight
{
    public sealed class CustomHeightSystem : SharedCustomHeightSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CustomHeightComponent, AppearanceChangeEvent>(OnHeightChange);
        }

        private void OnHeightChange(EntityUid uid, CustomHeightComponent component, AppearanceChangeEvent args)
        {
            if (args.Sprite != null && AppearanceSystem.TryGetData(uid, HeightVisuals.State, out float height, (AppearanceComponent?)null))
            {
                height = Math.Clamp(height, component.Min, component.Max);
                args.Sprite.Scale = new Vector2(height);
            }
        }
    }
}
