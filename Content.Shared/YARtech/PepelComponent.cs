using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Audio;


namespace Content.Shared.YARtech;

[RegisterComponent]
public sealed partial class PepelComponent : Component
{
    [DataField]
    public string CollisionResultPrototype = "Pepel";

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/Effects/Grenades/Supermatter/supermatter_start.ogg");
}
