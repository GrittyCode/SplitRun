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

        /// <summary>
        /// Runs once after all VContainer injections in the Game scene complete.
        /// Responsible for orchestrating initialization order across Game scene services.
        /// </summary>
        public void Start()
        {
            _gameService.Initialize();

            // TODO(netcode): subscribe to NetworkService.State and call GameService.StartRun()
            // once both players are connected — implement in Phase 4
        }
    }
}
