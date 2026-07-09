namespace SplitRun.Constants
{
    public static class CharacterConstants
    {
        public const string k_HatSocketName = "HatSocket";

        // --- Collider ---

        public const float k_ReferenceHeight = 1.2f;
        public const float k_ColliderRadius  = 0.35f;
        public const float k_ColliderHeight  = k_ReferenceHeight;
        public const float k_ColliderCenterY = k_ReferenceHeight / 2f;

        public const float k_SlideColliderRadius  = 0.3f;
        public const float k_SlideColliderHeight  = 0.65f;
        public const float k_SlideColliderCenterY = k_SlideColliderHeight / 2f;

        // --- Locomotion ---

        public const float k_BaseRunSpeed = 8f;

        // Speed increases by this amount per second during a run — reaches k_MaxRunSpeed in ~30s.
        public const float k_SpeedAcceleration = 0.4f;

        // Hard cap so the run never becomes unplayable at extreme distances.
        public const float k_MaxRunSpeed = 20f;

        public const float k_LaneMoveDuration = 0.15f;
        public const float k_JumpDuration     = 0.6f;
        public const float k_SlideDuration    = 0.5f;
        public const float k_JumpHeight       = 2f;

        // The landing-recovery clip always takes this long regardless of skin.
        public const float k_JumpLandRecoveryDuration = 0.15f;

        // Speed is held at 0 for the obstacle impact animation, then eased back to the pre-hit speed.
        public const float k_HitStunDuration = 0.7f;

        // Discards duplicate collision reports arriving within this window of the first.
        public const float k_CollisionDebounceDuration = 0.3f;

        // Client-side distance smoothing between NetworkVariable ticks: chase the synced value
        // slightly faster than the run speed, but never overshoot it.
        public const float k_DistanceCatchUpMultiplier = 1.2f;

        // A gap beyond this is a teleport (scene start, correction burst) — snap instead of chasing.
        public const float k_DistanceSnapThreshold = 3f;

        // --- Animator ---

        public const string k_TriggerJump  = "Jump";
        public const string k_TriggerLand  = "Land";
        public const string k_TriggerSlide = "Slide";
        public const string k_TriggerHit   = "Hit";
        public const string k_TriggerLose  = "Lose";
        public const string k_TriggerRoar  = "Roar";

        public const string k_ParamSpeed   = "Speed";
        public const string k_ParamRunning = "Running";

        public const string k_ClipNameRoll    = "Roll";
        public const string k_ClipNameJumpOut = "Jump_Out";

        // --- Skills ---

        public const float k_ShieldCooldownDuration = 20f;

        public const float k_DashCooldownDuration = 25f;
        public const float k_DashDuration         = 1.5f;
        public const float k_DashSpeedMultiplier  = 2f;
    }
}
