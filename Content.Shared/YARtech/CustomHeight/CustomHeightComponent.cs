using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;


namespace Content.Shared.YARtech.CustomHeight
{
    [RegisterComponent]
    public sealed partial class CustomHeightComponent : Component
    {
        [DataField(null, false, 1, false, false, null)]
        public float Min = 0.89f;

        [DataField(null, false, 1, false, false, null)]
        public float Max = 1.1f;

        [DataField(null, false, 1, false, false, null)]
        public float Starting = 1f;

        [DataField(null, false, 1, false, false, null)]
        public bool Random = true;
    }

    [Serializable]
    [NetSerializable]
    public enum HeightVisuals : byte
    {
        State
    }
}
