using UnityEngine;

namespace SplitRun.Data
{
    // Runtime progress for one active daily mission; the definition supplies its fixed data.
    public sealed class MissionState
    {
        public MissionState(MissionDefinition definition, int progress, bool claimed)
        {
            Definition = definition;
            Progress   = progress;
            Claimed    = claimed;
        }

        public MissionDefinition Definition { get; }
        public int               Progress   { get; set; }
        public bool              Claimed    { get; set; }

        public bool IsComplete  => Progress >= Definition.Target;
        public bool IsClaimable => IsComplete && !Claimed;

        public float NormalizedProgress =>
            Definition.Target <= 0 ? 1f : Mathf.Clamp01((float)Progress / Definition.Target);
    }
}
