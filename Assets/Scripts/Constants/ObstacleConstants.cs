namespace SplitRun.Constants
{
    public static class ObstacleConstants
    {
        // Obstacle blow-away tween duration on collision.
        public const float k_ImpactDuration = 0.3f;

        // --- Collider footprint dimensions ---

        // One-lane occupancy, kept under the 2-unit lane spacing so it never clips a neighbour.
        public const float k_LaneWidth      = 1.8f;

        // Full three-lane span.
        public const float k_WideWidth      = 6f;

        // Running-direction thickness — obstacles are walls, not volumes.
        public const float k_Depth          = 1.2f;

        // Above the jump apex (head ~3.2) so a Vertical wall cannot be jumped over.
        public const float k_VerticalHeight = 3.5f;

        // Above the slide head (0.65) but below the jump feet (2) — forces a jump.
        public const float k_JumpBarHeight  = 0.8f;

        // Slide bar height. Its base sits at GameConstants.k_SlideClearanceHeight
        public const float k_SlideBarHeight = 1.5f;
    }
}
