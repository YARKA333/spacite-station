using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.YARtech;

[RegisterComponent]
public sealed partial class ElprimoComponent : Component
{
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/YARtech/elprimo.ogg");

    [DataField]
    public float SoundDelay = 0f;

    [DataField]
    public float AnimLength = 2f;

    [DataField]
    public float JumpLength = 20f;

    [DataField]
    public float JumpHeight = 4f;

    [DataField]
    public TimeSpan SpawnTime;

    [DataField]
    public bool Played = false;

    [DataField]
    public bool Exploded = false;

    [DataField]
    public EntityUid Target;
}
