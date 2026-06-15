using UnityEngine;

using GoogleMobileAds.Api;

namespace SplitRun.Ad
{
    public class AdService
    {
        /// <summary>Triggers AdMob SDK initialization. Runs asynchronously in the background.</summary>
        public void Initialize()
        {
            MobileAds.Initialize(OnInitialized);
        }

        private void OnInitialized(InitializationStatus status)
        {
            Debug.Log("[AdService] AdMob initialized");
        }

        // TODO(ad): implement ShowRewardedAdAsync(CancellationToken) via UniTaskCompletionSource
    }
}
