namespace SplitRun.Constants
{
    public static class ObstacleConstants
    {
        // Constant by design — difficulty rises because speed increases, so a fixed gap takes less time to cross.
        public const float k_ObstacleSpacing                = 20f;
        public const float k_ObstacleSpawnLookAheadDistance = 60f;
        public const float k_ObstacleDespawnBehindDistance  = 20f;
        public const int   k_ObstaclePoolSizePerPrefab      = 4;

        // Character HitBox x this must be enabled in the Layer Collision Matrix for triggers to fire.
        public const string k_LayerName = "Obstacle";

        public const float k_ImpactDuration    = 0.3f;
        public const float k_ImpactFlyForward  = 12f;
        public const float k_ImpactFlyUp       = 8f;
        public const float k_ImpactSpinDegrees = 540f;
        public const float k_ImpactFlyDuration = 0.5f;

        // Kept under the 2-unit lane spacing so a one-lane obstacle never clips a neighbour.
        public const float k_LaneWidth = 1.8f;

        public const float k_WideWidth = 6f;

        // Running-direction thickness — obstacles are walls, not volumes.
        public const float k_Depth = 1.2f;

        // Above the jump apex (head ~3.2) so a Vertical wall cannot be jumped over.
        public const float k_VerticalHeight = 3.5f;

        // Above the slide head (0.65) but below the jump feet (2) — forces a jump.
        public const float k_JumpBarHeight = 0.8f;

        public const float k_SlideBarHeight = 1.5f;

        // Above k_SlideColliderHeight, so a standing character is still blocked.
        public const float k_SlideClearanceHeight = 0.75f;
    }
}
