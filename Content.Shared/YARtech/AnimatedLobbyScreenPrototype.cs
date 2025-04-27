using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.YARtech
{
    [Prototype(null, 1)]
    public sealed class AnimatedLobbyScreenPrototype : IPrototype
    {
        [DataField(null, false, 1, true, false, null)]
        public string Path = default!;

        [IdDataField(1, null)]
        public string ID { get; } = default!;
    }

}
