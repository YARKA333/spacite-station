using Content.Shared.Preferences;
using Content.Shared.Humanoid;

namespace Content.Shared.YARtech.HumanoidAppearanceExtension
{
    public record struct HumanoidAppearanceLoadedEvent
    {
        public HumanoidCharacterProfile Profile { get; set; }

        public Entity<HumanoidAppearanceComponent> Entity { get; set; }

        public HumanoidAppearanceLoadedEvent(Entity<HumanoidAppearanceComponent> entity, HumanoidCharacterProfile profile)
        {
            Entity = entity;
            Profile = profile;
        }
    }
}
