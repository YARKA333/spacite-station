using Content.Shared.Humanoid;

namespace Content.Shared.YARtech.HumanoidAppearanceExtension
{
    public record struct HumanoidAppearanceClonedEvent
    {
        public Entity<HumanoidAppearanceComponent> Source { get; set; }

        public Entity<HumanoidAppearanceComponent> Target { get; set; }

        public HumanoidAppearanceClonedEvent(Entity<HumanoidAppearanceComponent> source, Entity<HumanoidAppearanceComponent> target)
        {
            Source = source;
            Target = target;
        }
    }
}
