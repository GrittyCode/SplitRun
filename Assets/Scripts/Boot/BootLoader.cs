using System.Threading;

using UnityEngine;

using Cysharp.Threading.Tasks;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Data;

namespace SplitRun.Boot
{
    public class BootLoader : IAsyncStartable
    {
        private readonly PlayerDataService _playerDataService;
        private readonly AdService         _adService;

        public BootLoader(PlayerDataService playerDataService, AdService adService)
        {
            _playerDataService = playerDataService;
            _adService         = adService;
        }

        /// <summary>
        /// Runs once after all VContainer injections complete.
        /// Initializes services in dependency order, then hands off to Title scene.
        /// </summary>
        public UniTask StartAsync(CancellationToken ct)
        {
            _playerDataService.Load();

            // AdMob init is fire-and-forget — does not block scene transition
            _adService.Initialize();

            // Uncomment once Title.unity is created and registered in Build Settings at index 1
            Debug.Log("[BootLoader] Boot init complete — Title scene pending");

            return UniTask.CompletedTask;
        }
    }
}