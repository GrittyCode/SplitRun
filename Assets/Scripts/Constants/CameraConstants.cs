namespace SplitRun.Constants
{
    public static class CameraConstants
    {
        public const float k_CameraOffsetY    =  6.0f;
        public const float k_CameraOffsetZ    = -3.5f;

        // Pitch angle in degrees. Lower = more horizontal = stronger perspective
        // convergence toward the vanishing point. Higher = more top-down = flatter view.
        public const float k_CameraPitchAngle = 30;

        // Narrower FOV reduces perspective distortion so ceiling (slide) and floor (jump)
        // obstacles stay vertically separated instead of collapsing at the vanishing point.
        public const float k_CameraFov        = 80;
    }
}
