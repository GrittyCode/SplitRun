using UnityEngine;

using Unity.Netcode;
using VContainer.Unity;

using SplitRun.Environment;

namespace SplitRun.Game
{
    public class GameEntryPoint : IStartable
    {
        private readonly GameService       _gameService;
        private readonly WorldThemeProfile _theme;

        public GameEntryPoint(GameService gameService, WorldThemeProfile theme)
        {
            _gameService = gameService;
            _theme       = theme;
        }

        public void Start()
        {
            SpawnBackdrop();

            // Guard allows LocalCharacter to be used without NetworkManager in the scene.
            // TODO(netcode): use NetworkService.CreateRoomAsync() instead of StartHost
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.StartHost();

            // TODO(lobby): call from a ready-up flow — starting on scene load is a temporary shortcut.
            _gameService.StartRun();
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
