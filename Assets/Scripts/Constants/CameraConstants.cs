namespace SplitRun.Constants
{
    public static class CameraConstants
    {
        // X stays centered on the track regardless of the character's current lane —
        // keeps all three lanes symmetric on screen instead of swaying with lane changes.
        public const float k_CameraOffsetX = 0f;
        public const float k_CameraOffsetY = 5f;
        public const float k_CameraOffsetZ = -5f;

        // The camera looks at a point ahead of the character rather than at the character
        // itself, biasing the frame down the track so upcoming obstacles read early.
        public const float k_CameraLookAheadDistance = 6f;
        public const float k_CameraLookHeight         = 1.0f;
    }
}
