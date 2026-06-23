namespace SplitRun.Constants
{
    public static class CameraConstants
    {
        public const float k_CameraOffsetY    = 4f;
        public const float k_CameraOffsetZ    = -4f;

        // Pitch angle in degrees. Lower = more horizontal = stronger perspective
        // convergence toward the vanishing point (reference game feel).
        // Higher = more top-down = flatter view. Tune in Play Mode.
        public const float k_CameraPitchAngle = 20f;
    }
}
