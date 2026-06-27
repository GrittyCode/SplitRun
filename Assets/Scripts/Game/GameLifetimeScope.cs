using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Environment;
using SplitRun.UI.Game;
using SplitRun.Utility;

namespace SplitRun.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private ObstacleSpawner _obstacleSpawner;
        [SerializeField] private TrackScroller   _trackScroller;
        [SerializeField] private GameHUDView     _hudView;

        protected override void Configure(IContainerBuilder builder)
        {
            // AsSelf() makes GameService resolvable by concrete type for GameInput injection.
            builder.RegisterEntryPoint<GameService>().AsSelf();

            builder.RegisterEntryPoint<SwipeDetector>().AsSelf();
            builder.RegisterEntryPoint<GameInput>();

            // MonoBehaviours placed in the scene — registered so GameService is injected into
            // them. Null-guarded so the scene still runs in isolated tests with any absent.
            if (_obstacleSpawner)
                builder.RegisterComponent(_obstacleSpawner);

            if (_trackScroller)
                builder.RegisterComponent(_trackScroller);

            if (_hudView)
                builder.RegisterComponent(_hudView);

            // TODO(netcode): builder.Register<NetworkService>(Lifetime.Singleton)

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
