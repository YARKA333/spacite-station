using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Standing
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(StandingStateSystem))]
    public sealed partial class StandingStateComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public SoundSpecifier? DownSound { get; private set; } = new SoundCollectionSpecifier("BodyFall");

        //[DataField, AutoNetworkedField]
        //public bool Standing { get; set; } = true;

        /// <summary>
        ///     List of fixtures that had their collision mask changed when the entity was downed.
        ///     Required for re-adding the collision mask.
        /// </summary>
        [DataField, AutoNetworkedField]
        public List<string> ChangedFixtures = new();

        [DataField, AutoNetworkedField]
        public bool CanLieDown;

        [DataField, AutoNetworkedField]
        public bool AutoGetUp = true;

        [DataField, AutoNetworkedField]
        public StandingState CurrentState { get; set; } = StandingState.Standing;

        [DataField, AutoNetworkedField]
        public TimeSpan StandingUpTime { get; set; } = TimeSpan.FromSeconds(1.0);
    }
}
