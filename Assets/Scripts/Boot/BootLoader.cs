using System;
using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

using Cysharp.Threading.Tasks;
using R3;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Environment;
using SplitRun.Network;

namespace SplitRun.Boot
{
    public class BootLoader : IAsyncStartable, IDisposable
    {
        private readonly PlayerDataService   _playerDataService;
        private readonly MissionService      _missionService;
        private readonly AssetPreloadService _assetPreloadService;
        private readonly AdService           _adService;
        private readonly NetworkService      _networkService;

        private readonly ReactiveProperty<float>  _progress = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<string> _status   = new ReactiveProperty<string>(BootConstants.k_StatusLoading);

        public BootLoader(PlayerDataService playerDataService, MissionService missionService,
            AssetPreloadService assetPreloadService, AdService adService, NetworkService networkService)
        {
            _playerDataService   = playerDataService;
            _missionService      = missionService;
            _assetPreloadService = assetPreloadService;
            _adService           = adService;
            _networkService      = networkService;
        }

        public ReadOnlyReactiveProperty<float>  Progress => _progress;
        public ReadOnlyReactiveProperty<string> Status   => _status;

        /// <summary>
        /// Runs once after all VContainer injections complete. Initializes services, drives the
        /// loading screen for a minimum dwell while theme assets preload, then loads the Lobby scene.
        /// </summary>
        public async UniTask StartAsync(CancellationToken ct)
        {
            _playerDataService.Load();
            _missionService.Load();

            // AdMob init is fire-and-forget — does not block the loading screen.
            _adService.Initialize();

            // Sign-in races the loading screen — failure only disables multiplayer, so boot never blocks on it.
            _networkService.InitializeAsync(ct).Forget();

            await RunLoadingScreenAsync(ct);

            _progress.Value = 1f;
            _status.Value   = BootConstants.k_StatusReady;

            await SceneManager.LoadSceneAsync(SceneConstants.k_LobbySceneName).ToUniTask(cancellationToken: ct);
        }

        public void Dispose()
        {
            _progress.Dispose();
            _status.Dispose();
        }

        // Fills the bar over a minimum dwell while the theme assets preload in parallel. The bar
        // never reads full before the assets are resident, and an early finish never cuts the dwell short.
        private async UniTask RunLoadingScreenAsync(CancellationToken ct)
        {
            bool isPreloadDone = false;
            LoadAssetsAsync(() => isPreloadDone = true, ct).Forget();

            float elapsed = 0f;
            while (elapsed < BootConstants.k_MinLoadingSeconds || !isPreloadDone)
            {
                elapsed += Time.deltaTime;

                float dwell = Mathf.Clamp01(elapsed / BootConstants.k_MinLoadingSeconds);
                _progress.Value = isPreloadDone ? dwell : Mathf.Min(dwell, BootConstants.k_LoadingHoldFraction);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        // Runs the real preload alongside the dwell; the flag flips on completion or on a handled
        // failure so the bar can finish and the boot never hangs on a bad asset.
        private async UniTaskVoid LoadAssetsAsync(Action onComplete, CancellationToken ct)
        {
            try
            {
                await _assetPreloadService.LoadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                // A failed preload only disables obstacle spawning (AssetPreloadService handles the
                // empty case); the player must still reach the Lobby.
                Debug.LogError($"[BootLoader] Asset preload failed: {e.Message}");
            }

            onComplete();
        }
    }
}
