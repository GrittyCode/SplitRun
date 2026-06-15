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
        /// </summary>
        public async UniTask StartAsync(CancellationToken ct)
        {
            _playerDataService.Load();

            // AdMob init is fire-and-forget — does not block scene transition
            _adService.Initialize();

            Debug.Log("[BootLoader] Boot init complete — loading Game scene");

            // TODO(boot): load Title scene instead of Game scene
            await SceneManager.LoadSceneAsync("Game").ToUniTask(cancellationToken: ct);
        }
    }
}
