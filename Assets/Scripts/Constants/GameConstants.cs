namespace SplitRun.Constants
{
    public static class GameConstants
    {
        public const string k_LobbySceneName = "Lobby";
        public const string k_GameSceneName  = "Game";

        public const int k_MaxHp = 3;

        public const int k_LaneCount  = 3;
        public const int k_LaneLeft   = 0;
        public const int k_LaneCenter = 1;
        public const int k_LaneRight  = 2;

        public const float k_LaneXLeft   = -2f;
        public const float k_LaneXCenter =  0f;
        public const float k_LaneXRight  =  2f;

        public const float k_SwipeMinDistancePx = 50f;

        public const float k_DoubleTapWindow = 0.3f;

        // Both players must reach the game screen before the run starts
        public const float k_RunIntroSeconds = 5f;

        // Server delay between accepting a resume and unpausing; clients render 3-2-1 locally.
        public const float  k_ResumeCountdownSeconds     = 3f;
        public const float  k_ResumeCountdownStepSeconds = 1f;
        public const string k_PausedLabel                = "PAUSED";

        public const float k_ResultRollSeconds   = 1.3f;
        public const float k_BestBlinkOnSeconds  = 0.55f;
        public const float k_BestBlinkOffSeconds = 0.3f;

        // Shared by the character lane tween and obstacle lane placement so both read one mapping.
        public static float GetLaneX(int laneIndex) => laneIndex switch
        {
            k_LaneLeft  => k_LaneXLeft,
            k_LaneRight => k_LaneXRight,
            _           => k_LaneXCenter,
        };
    }
}
