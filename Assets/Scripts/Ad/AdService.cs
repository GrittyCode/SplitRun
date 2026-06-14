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

        // TODO(ad): UniTask<bool> ShowRewardedAdAsync(CancellationToken ct)
        // Load rewarded ad unit, present it on request, and return true if the user earns the reward.
        // Bridge AdMob's event callbacks to UniTask via UniTaskCompletionSource.
    }
}