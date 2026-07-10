using System;
using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

using Cysharp.Threading.Tasks;
using R3;
using VContainer.Unity;

using SplitRun.Audio;
using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Mission;
using SplitRun.Network;

namespace SplitRun.Boot
{
    public class BootLoader : IAsyncStartable, IDisposable
    {
        private const float k_MinLoadingSeconds   = 3f;
        private const float k_LoadingHoldFraction = 0.9f;

        private const string k_StatusLoading = "Loading...";
        private const string k_StatusReady   = "Ready!";

        private readonly PlayerDataService   _playerDataService;
        private readonly MissionService      _missionService;
        private readonly AssetPreloadService _assetPreloadService;
        private readonly NetworkService      _networkService;

        private readonly ReactiveProperty<float>  _progress = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<string> _status   = new ReactiveProperty<string>(k_StatusLoading);

        public BootLoader(PlayerDataService playerDataService, MissionService missionService,
            AssetPreloadService assetPreloadService, NetworkService networkService)
        {
            _playerDataService   = playerDataService;
            _missionService      = missionService;
            _assetPreloadService = assetPreloadService;
            _networkService      = networkService;
        }

        public ReadOnlyReactiveProperty<float>  Progress => _progress;
        public ReadOnlyReactiveProperty<string> Status   => _status;

        public async UniTask StartAsync(CancellationToken ct)
        {
            _playerDataService.Load();
            _missionService.Load();

            // Sign-in races the loading screen — failure only disables multiplayer.
            _networkService.InitializeAsync(ct).Forget();

            await RunLoadingScreenAsync(ct);

            _progress.Value = 1f;
            _status.Value   = k_StatusReady;

            AudioEvents.RequestBgm(BgmType.Lobby);

            await SceneManager.LoadSceneAsync(GameConstants.k_LobbySceneName).ToUniTask(cancellationToken: ct);
        }

        public void Dispose()
        {
            _progress.Dispose();
            _status.Dispose();
        }

        // Holding below full until the preload lands means a full bar always reads as ready.
        private async UniTask RunLoadingScreenAsync(CancellationToken ct)
        {
            bool isPreloadDone = false;
            LoadAssetsAsync(() => isPreloadDone = true, ct).Forget();

            float elapsed = 0f;
            while (elapsed < k_MinLoadingSeconds || !isPreloadDone)
            {
                elapsed += Time.deltaTime;

                float dwell = Mathf.Clamp01(elapsed / k_MinLoadingSeconds);
                _progress.Value = isPreloadDone ? dwell : Mathf.Min(dwell, k_LoadingHoldFraction);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

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
                // A failed preload only disables obstacle spawning; the player must still reach the Lobby.
                Debug.LogError($"[BootLoader] Asset preload failed: {e.Message}");
            }

            onComplete();
        }
    }
}
