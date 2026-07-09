namespace SplitRun.Constants
{
    public static class BootConstants
    {
        // The loading screen dwells at least this long even if assets resolve instantly,
        // so the title and progress bar read as a deliberate screen rather than a flash.
        public const float k_MinLoadingSeconds = 3f;

        // The bar holds here until the real preload completes, so a full bar always means "ready".
        public const float k_LoadingHoldFraction = 0.9f;

        public const string k_StatusLoading = "Loading...";
        public const string k_StatusReady   = "Ready!";
    }
}
