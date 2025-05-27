using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.YARtech.SelfHeal;

[RegisterComponent]
public sealed partial class SelfHealComponent : Component
{
    [DataField]
    public float Delay = 3f;

    [DataField]
    public SoundSpecifier? HealingSound;

    [DataField(required: true)]
    public DamageSpecifier Damage;

    [DataField]
    public List<string>? DamageContainers;

    [DataField]
    public List<string>? DisallowedClothingUser;

    [DataField]
    public List<string>? DisallowedClothingTarget;

    [DataField]
    public EntProtoId Action = "SelfHealAction";

    [DataField]
    public EntityUid? ActionEntity;

}

