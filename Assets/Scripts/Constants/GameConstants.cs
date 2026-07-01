namespace SplitRun.Constants
{
    public static class GameConstants
    {
        public const int   k_LaneCount          = 3;
        public const int   k_LaneLeft           = 0;
        public const int   k_LaneCenter         = 1;
        public const int   k_LaneRight          = 2;

        public const float k_LaneXLeft          = -2f;
        public const float k_LaneXCenter        =  0f;
        public const float k_LaneXRight         =  2f;

        public const float k_LaneMoveDuration   = 0.15f;

        public const float k_JumpDuration       = 0.6f;
        public const float k_SlideDuration      = 0.5f;
        public const float k_JumpHeight         = 2f;

        // The landing-recovery clip always takes this long regardless of skin.
        public const float k_JumpLandRecoveryDuration = 0.15f;

        public const int   k_MaxHp              = 3;

        // Discards duplicate collision reports arriving within this window of the first.
        public const float k_CollisionDebounceDuration = 0.3f;

        // Speed is held at 0 for the obstacle impact animation, then eased back to the
        // pre-hit speed over this duration.
        public const float k_HitStunDuration    = 0.7f;

        public const float k_BaseRunSpeed       = 8f;

        // Speed increases by this amount per second during a run — reaches k_MaxRunSpeed in ~30s.
        public const float k_SpeedAcceleration  = 0.4f;

        // Hard cap so the run never becomes unplayable at extreme distances.
        public const float k_MaxRunSpeed        = 20f;

        public const float k_SwipeMinDistancePx = 50f;

        // Two taps within this window count as a double tap (skill activation).
        public const float k_DoubleTapWindow    = 0.3f;

        // Physics layer obstacles live on. Character HitBox × this must be enabled in the
        // Layer Collision Matrix for OnTriggerEnter to fire (Project Settings → Physics).
        public const string k_ObstacleLayerName = "Obstacle";

        // --- Obstacle spawn system ---
        // Fixed Z gap between consecutive obstacles. Constant by design — difficulty rises
        // because speed increases per zone, so a fixed gap takes less time to cross.
        public const float k_ObstacleSpacing                = 20f;
        public const float k_ObstacleSpawnLookAheadDistance = 60f;
        public const float k_ObstacleDespawnBehindDistance  = 20f;
        public const int   k_ObstaclePoolSizePerPrefab      = 4;

        // Slide-bar base height — the head-height gap a sliding character passes through.
        // Above k_SlideColliderHeight (0.65) so a standing character is still blocked.
        public const float k_SlideClearanceHeight           = 0.75f;

        // Shared by CharacterMovementDriver (character lane tween) and ObstacleSpawner
        // (obstacle lane placement) so both read the same lane-to-X mapping.
        public static float GetLaneX(int laneIndex) => laneIndex switch
        {
            k_LaneLeft  => k_LaneXLeft,
            k_LaneRight => k_LaneXRight,
            _           => k_LaneXCenter,
        };
    }
}
