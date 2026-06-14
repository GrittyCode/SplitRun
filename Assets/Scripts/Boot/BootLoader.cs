using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// Initializes services in dependency order, then loads the Game scene for testing.
        /// TODO(boot): replace with Title scene transition once Title/Lobby flow is implemented in Phase 4
        /// </summary>
        public async UniTask StartAsync(CancellationToken ct)
        {
            _playerDataService.Load();

            // AdMob init is fire-and-forget — does not block scene transition
            _adService.Initialize();

            Debug.Log("[BootLoader] Boot init complete — loading Game scene");

            await SceneManager.LoadSceneAsync("Game").ToUniTask(cancellationToken: ct);
        }
    }
}
