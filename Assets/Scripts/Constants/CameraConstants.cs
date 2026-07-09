namespace SplitRun.Constants
{
    public static class CameraConstants
    {
        public const float k_CameraOffsetY = 6.0f;
        public const float k_CameraOffsetZ = -3.5f;

        // Lower = more horizontal = stronger perspective convergence. Higher = flatter view.
        public const float k_CameraPitchAngle = 30f;

        // Narrower FOV keeps ceiling (slide) and floor (jump) bars vertically separated.
        public const float k_CameraFov = 80f;
    }
}
