namespace SplitRun.Constants
{
    // Parameter names for AC_Character. See 05_design_principles.md,
    // "Animator Controller Architecture" — all five triggers below are
    // one-shot events already mirrored by a NetworkVariable change.
    public static class AnimatorConstants
    {
        public const string k_TriggerJump  = "Jump";
        public const string k_TriggerLand  = "Land";
        public const string k_TriggerSlide = "Slide";
        public const string k_TriggerHit   = "Hit";
        public const string k_TriggerLose  = "Lose";
        public const string k_ParamSpeed = "Speed";
        public const string k_ClipNameRoll    = "Roll";
        public const string k_ClipNameJumpOut = "Jump_Out";
    }
}
