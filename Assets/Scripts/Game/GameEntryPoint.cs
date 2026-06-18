using Unity.Netcode;
using VContainer.Unity;

namespace SplitRun.Game
{
    public class GameEntryPoint : IStartable
    {
        private readonly GameService _gameService;

        public GameEntryPoint(GameService gameService)
        {
            _gameService = gameService;
        }

        public void Start()
        {
            // Guard allows LocalCharacter to be used without NetworkManager in the scene.
            // TODO(netcode): use NetworkService.CreateRoomAsync() instead of StartHost
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.StartHost();

            // TODO(lobby): call this from a ready-up flow once Phase 4's Lobby scene exists —
            // starting the run immediately on scene load is a temporary testing shortcut so
            // GamePhase.Running is reachable before the Lobby ready-up flow is built.
            _gameService.StartRun();
        }
    }
}
