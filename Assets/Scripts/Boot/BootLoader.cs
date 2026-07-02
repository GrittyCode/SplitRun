using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

using Cysharp.Threading.Tasks;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Network;

namespace SplitRun.Boot
{
    public class BootLoader : IAsyncStartable
    {
        private readonly PlayerDataService _playerDataService;
        private readonly AdService         _adService;
        private readonly NetworkService    _networkService;

        public BootLoader(PlayerDataService playerDataService, AdService adService, NetworkService networkService)
        {
            _playerDataService = playerDataService;
            _adService         = adService;
            _networkService    = networkService;
        }

        /// <summary>
        /// Runs once after all VContainer injections complete.
        /// Initializes services in dependency order, then loads the Lobby scene.
        /// </summary>
        public async UniTask StartAsync(CancellationToken ct)
        {
            _playerDataService.Load();

            // AdMob init is fire-and-forget — does not block scene transition
            _adService.Initialize();

            // Sign-in races the scene load — failure only disables multiplayer, so boot never blocks on it.
            _networkService.InitializeAsync(ct).Forget();

            Debug.Log("[BootLoader] Boot init complete — loading Lobby scene");

            // TODO(ui): route through a Title/loading screen before Lobby
            await SceneManager.LoadSceneAsync(SceneConstants.k_LobbySceneName).ToUniTask(cancellationToken: ct);
        }
    }
}
