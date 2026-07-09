using System;

using UnityEngine;
using UnityEngine.SceneManagement;

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
        private readonly ShopCatalog       _shopCatalog;
        private readonly PlayerDataService _playerDataService;
        private readonly NetworkService    _networkService;

        private DisposableBag _disposables;

        public GameEntryPoint(GameService gameService, WorldThemeProfile theme,
            ShopCatalog shopCatalog, PlayerDataService playerDataService,
            NetworkService networkService)
        {
            _gameService       = gameService;
            _theme             = theme;
            _shopCatalog       = shopCatalog;
            _playerDataService = playerDataService;
            _networkService    = networkService;
        }

        public void Start()
        {
            // Subscribed before the server spawn so the host's own character is dressed too;
            // on clients the CharacterEvents replay covers a spawn that beat this entry point.
            CharacterEvents.OnSpawned += OnCharacterSpawned;

            SpawnBackdrop();
            StartSession();
            WatchSessionLoss();
            WatchGameOver();

            // The run is not started here — GameSession gates it behind both players readying up
            // and a short control-guide intro, then GameService flips to Running on the Live signal.
        }

        public void Dispose()
        {
            CharacterEvents.OnSpawned -= OnCharacterSpawned;

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

        // The result overlay's quit button drives teardown — no auto-return timer.
        private void WatchGameOver()
        {
            _gameService.EndSessionRequested
                .Subscribe(_ => EndSession())
                .AddTo(ref _disposables);
        }

        // The player dismissed the result screen — shut the session down and head back to the Lobby.
        private void EndSession()
        {
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
            ShopCharacterEntry entry = _shopCatalog.FindCharacter(_playerDataService.SelectedCharacter.CurrentValue);
            if (entry == null || !entry.GamePrefab)
            {
                Debug.LogError("[GameEntryPoint] ShopCatalog has no game prefab for the selected type.");
                return;
            }

            ServerCharacter character = UnityEngine.Object.Instantiate(entry.GamePrefab);
            character.SetHat(_playerDataService.SelectedHat.CurrentValue);

            // The character's lifetime is one run — a later NGO scene change must take it down.
            character.NetworkObject.Spawn(destroyWithScene: true);
        }

        // Dresses the NGO-spawned character (outside the DI graph) with the hat carried in its spawn payload.
        private void OnCharacterSpawned(ICharacter character)
        {
            if (character.Hat == HatType.None)
                return;

            ShopHatEntry entry = _shopCatalog.FindHat(character.Hat);
            if (entry == null || !entry.HatPrefab)
            {
                Debug.LogWarning($"[GameEntryPoint] ShopCatalog has no prefab for {character.Hat} — hat skipped.");
                return;
            }

            character.AttachHat(entry.HatPrefab);
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
