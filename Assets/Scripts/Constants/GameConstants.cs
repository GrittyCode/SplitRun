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

        // Cosmetic only — does not gate input. Jump_Out's playback speed is compensated
        // so the landing-recovery clip always takes this long regardless of skin.
        public const float k_JumpLandRecoveryDuration = 0.15f;

        // Negative — slide lowers the character below the ground-level origin
        public const float k_SlideYOffset       = -0.5f;

        public const int   k_MaxHp              = 3;

        // HP value at which the danger BGM kicks in
        public const int   k_DangerHpThreshold  = 1;

        public const float k_BaseRunSpeed       = 8f;

        public const float k_SwipeMinDistancePx = 50f;
    }
}
