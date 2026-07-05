using System;
using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using VContainer.Unity;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Environment;
using SplitRun.Network;

namespace SplitRun.Game
{
    public class GameEntryPoint : IStartable, IDisposable
    {
        private readonly GameService       _gameService;
        private readonly WorldThemeProfile _theme;
        private readonly CharacterCatalog  _characterCatalog;
        private readonly PlayerDataService _playerDataService;
        private readonly NetworkService    _networkService;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private DisposableBag _disposables;

        public GameEntryPoint(GameService gameService, WorldThemeProfile theme,
            CharacterCatalog characterCatalog, PlayerDataService playerDataService,
            NetworkService networkService)
        {
            _gameService       = gameService;
            _theme             = theme;
            _characterCatalog  = characterCatalog;
            _playerDataService = playerDataService;
            _networkService    = networkService;
        }

        public void Start()
        {
            SpawnBackdrop();
            StartSession();
            WatchSessionLoss();
            WatchGameOver();

            // Reaching this scene already implies intent: ready-up completed (multiplayer) or Solo pressed.
            // A late-arriving client character picks up the Running phase in GameService.OnCharacterSpawned.
            _gameService.StartRun();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }

        private void StartSession()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (!networkManager)
            {
                Debug.LogError("[GameEntryPoint] No NetworkManager — the run cannot start.");
                return;
            }

            // Already listening means the Relay session from the Lobby carried over — never restart it.
            if (!networkManager.IsListening)
                StartSoloHost(networkManager);

            if (networkManager.IsServer)
                SpawnCharacter();
        }

        // Either peer leaving destroys the room — the survivor returns to the Lobby to make a new one.
        private void WatchSessionLoss()
        {
            _networkService.ConnectionState
                .Pairwise()
                .Where(pair => IsSessionLost(pair.Previous, pair.Current))
                .Subscribe(_ => ReturnToLobby())
                .AddTo(ref _disposables);
        }

        // TODO(ui): replace the fixed delay with a GameOverView button when it lands
        private void WatchGameOver()
        {
            _gameService.Phase
                .Where(phase => phase == GamePhase.GameOver)
                .Subscribe(_ => EndSessionAfterDelayAsync(_cts.Token).Forget())
                .AddTo(ref _disposables);
        }

        private async UniTaskVoid EndSessionAfterDelayAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(GameConstants.k_GameOverReturnDelaySeconds), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Solo runs never enter Hosting/Joined, so the session-loss watcher cannot fire for them.
            bool isMultiplayer = _networkService.ConnectionState.CurrentValue
                is NetworkConnectionState.Hosting or NetworkConnectionState.Joined;

            // Shuts NGO down in both modes; a multiplayer teardown also trips the peer's watcher symmetrically.
            _networkService.Disconnect();

            if (!isMultiplayer)
                ReturnToLobby();
        }

        private static bool IsSessionLost(NetworkConnectionState previous, NetworkConnectionState current)
            => (previous is NetworkConnectionState.Hosting or NetworkConnectionState.Joined)
            && (current is NetworkConnectionState.Offline or NetworkConnectionState.Failed);

        private static void ReturnToLobby()
        {
            Debug.Log("[GameEntryPoint] Returning to Lobby.");

            // NGO already shut down in NetworkService.ResetSession, so a plain load is correct here.
            SceneManager.LoadScene(SceneConstants.k_LobbySceneName);
        }

        private static void StartSoloHost(NetworkManager networkManager)
        {
            // An abandoned Relay session leaves its server data on the transport — overwrite before hosting locally.
            networkManager.GetComponent<UnityTransport>()
                .SetConnectionData(NetworkConstants.k_LocalHostAddress, NetworkConstants.k_LocalHostPort);

            if (!networkManager.StartHost())
                Debug.LogError("[GameEntryPoint] Local StartHost failed — no character will spawn.");
        }

        private void SpawnCharacter()
        {
            ServerCharacter prefab = _characterCatalog.Resolve(_playerDataService.SelectedCharacter.CurrentValue);
            if (!prefab)
            {
                Debug.LogError("[GameEntryPoint] CharacterCatalog has no prefab for the selected type.");
                return;
            }

            ServerCharacter character = UnityEngine.Object.Instantiate(prefab);

            // The character's lifetime is one run — a later NGO scene change must take it down.
            character.NetworkObject.Spawn(destroyWithScene: true);
        }

        // The backdrop is one runtime follow object, spawned from the theme rather than living in the scene.
        private void SpawnBackdrop()
        {
            if (!_theme || !_theme.BackdropPrefab)
            {
                Debug.LogWarning("[GameEntryPoint] No theme backdrop prefab — backdrop skipped.");
                return;
            }

            UnityEngine.Object.Instantiate(_theme.BackdropPrefab);
        }
    }
}
