namespace SplitRun.Constants
{
    public static class CharacterConstants
    {
        public const string k_HatSocketName = "HatSocket";

        public const float k_ReferenceHeight = 1.2f;
        public const float k_ColliderRadius  = 0.35f;
        public const float k_ColliderHeight  = k_ReferenceHeight;
        public const float k_ColliderCenterY = k_ReferenceHeight / 2f;
        public const float k_SlideColliderRadius  = 0.3f;
        public const float k_SlideColliderHeight  = 0.65f;
        public const float k_SlideColliderCenterY = k_SlideColliderHeight / 2f;

        // Client-side distance smoothing between NetworkVariable ticks: chase the synced value
        // slightly faster than the run speed, but never overshoot it.
        public const float k_DistanceCatchUpMultiplier = 1.2f;

        // A gap beyond this is a teleport (scene start, correction burst) — snap instead of chasing.
        public const float k_DistanceSnapThreshold     = 3f;
    }
}
