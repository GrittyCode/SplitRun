using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using R3;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using VContainer.Unity;

using SplitRun.Audio;
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
            // Before the server spawn so the host's own character is dressed; the CharacterEvents replay covers clients.
            CharacterEvents.OnSpawned += OnCharacterSpawned;

            SpawnBackdrop();
            StartSession();
            WatchSessionLoss();
            WatchGameOver();

            // The run is not started here — GameSession gates it behind the ready handshake and intro.
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

        private void WatchGameOver()
        {
            _gameService.EndSessionRequested
                .Subscribe(_ => EndSession())
                .AddTo(ref _disposables);
        }

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

            AudioEvents.RequestBgm(BgmType.Lobby);

            // NGO already shut down in NetworkService.ResetSession, so a plain load is correct here.
            SceneManager.LoadScene(GameConstants.k_LobbySceneName);
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

        // The NGO-spawned character is outside the DI graph, so the hat is attached from its spawn payload.
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

        // The backdrop follows the character, so it is spawned from the theme rather than placed in the scene.
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
