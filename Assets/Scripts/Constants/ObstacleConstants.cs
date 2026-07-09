using UnityEngine;

using SplitRun.Obstacle;

namespace SplitRun.Constants
{
    public static class ObstacleConstants
    {
        // --- Spawn cadence ---

        // Fixed Z gap between consecutive obstacles. Constant by design — difficulty rises
        // because speed increases, so a fixed gap takes less time to cross.
        public const float k_ObstacleSpacing                = 20f;
        public const float k_ObstacleSpawnLookAheadDistance = 60f;
        public const float k_ObstacleDespawnBehindDistance  = 20f;
        public const int   k_ObstaclePoolSizePerPrefab      = 4;

        // Character HitBox x this must be enabled in the Layer Collision Matrix for triggers to fire.
        public const string k_LayerName = "Obstacle";

        // --- Impact blow-away motion ---

        public const float k_ImpactDuration    = 0.3f;
        public const float k_ImpactFlyForward  = 12f;
        public const float k_ImpactFlyUp       = 8f;
        public const float k_ImpactSpinDegrees = 540f;
        public const float k_ImpactFlyDuration = 0.5f;

        // --- Collider footprint dimensions ---

        // One-lane occupancy, kept under the 2-unit lane spacing so it never clips a neighbour.
        public const float k_LaneWidth = 1.8f;

        // Full three-lane span.
        public const float k_WideWidth = 6f;

        // Running-direction thickness — obstacles are walls, not volumes.
        public const float k_Depth = 1.2f;

        // Above the jump apex (head ~3.2) so a Vertical wall cannot be jumped over.
        public const float k_VerticalHeight = 3.5f;

        // Above the slide head (0.65) but below the jump feet (2) — forces a jump.
        public const float k_JumpBarHeight = 0.8f;

        public const float k_SlideBarHeight = 1.5f;

        // The head-height gap a sliding character passes through; above k_SlideColliderHeight
        // so a standing character is still blocked.
        public const float k_SlideClearanceHeight = 0.75f;

        /// <summary>Returns the stamped BoxCollider size and center Y for a footprint. All footprints are floor-based.</summary>
        public static (Vector3 size, float centerY) GetFootprintBox(ObstacleFootprint footprint) => footprint switch
        {
            ObstacleFootprint.Vertical => (
                new Vector3(k_LaneWidth, k_VerticalHeight, k_Depth),
                k_VerticalHeight * 0.5f),

            ObstacleFootprint.LaneJump => (
                new Vector3(k_LaneWidth, k_JumpBarHeight, k_Depth),
                k_JumpBarHeight * 0.5f),

            ObstacleFootprint.LaneSlide => (
                new Vector3(k_LaneWidth, k_SlideBarHeight, k_Depth),
                k_SlideClearanceHeight + k_SlideBarHeight * 0.5f),

            ObstacleFootprint.WideJump => (
                new Vector3(k_WideWidth, k_JumpBarHeight, k_Depth),
                k_JumpBarHeight * 0.5f),

            ObstacleFootprint.WideSlide => (
                new Vector3(k_WideWidth, k_SlideBarHeight, k_Depth),
                k_SlideClearanceHeight + k_SlideBarHeight * 0.5f),

            _ => (Vector3.one, 0.5f),
        };
    }
}
