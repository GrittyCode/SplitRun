namespace SplitRun.Constants
{
    public static class GameConstants
    {
        public const int   k_LaneCount          = 3;
        public const int   k_LaneLeft           = 0;
        public const int   k_LaneCenter         = 1;
        public const int   k_LaneRight          = 2;

        public const int   k_MaxHp              = 3;

        // HP value at which the danger BGM kicks in
        public const int   k_DangerHpThreshold  = 1;

        public const float k_BaseRunSpeed       = 8f;

        // Minimum interval between accepted swipe inputs (milliseconds)
        public const float k_SwipeThrottleMs    = 150f;

        // Minimum pixel distance a touch must travel to register as a swipe
        public const float k_SwipeMinDistancePx = 50f;
    }
}
