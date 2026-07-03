using UnityEngine;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using VContainer.Unity;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Environment;

namespace SplitRun.Game
{
    public class GameEntryPoint : IStartable
    {
        private readonly GameService       _gameService;
        private readonly WorldThemeProfile _theme;
        private readonly CharacterCatalog  _characterCatalog;
        private readonly PlayerDataService _playerDataService;

        public GameEntryPoint(GameService gameService, WorldThemeProfile theme,
            CharacterCatalog characterCatalog, PlayerDataService playerDataService)
        {
            _gameService       = gameService;
            _theme             = theme;
            _characterCatalog  = characterCatalog;
            _playerDataService = playerDataService;
        }

        public void Start()
        {
            SpawnBackdrop();
            StartSession();

            // Reaching this scene already implies intent: ready-up completed (multiplayer) or Solo pressed.
            // A late-arriving client character picks up the Running phase in GameService.OnCharacterSpawned.
            _gameService.StartRun();
        }

        private void StartSession()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            // Guard allows LocalCharacter to be used without NetworkManager in the scene.
            if (!networkManager) return;

            // Already listening means the Relay session from the Lobby carried over — never restart it.
            if (!networkManager.IsListening)
                StartSoloHost(networkManager);

            if (networkManager.IsServer)
                SpawnCharacter();
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

            ServerCharacter character = Object.Instantiate(prefab);

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

            Object.Instantiate(_theme.BackdropPrefab);
        }
    }
}
