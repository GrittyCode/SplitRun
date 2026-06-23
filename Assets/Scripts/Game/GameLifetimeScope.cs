using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Utility;

namespace SplitRun.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private ObstacleSpawner _obstacleSpawner;

        protected override void Configure(IContainerBuilder builder)
        {
            // AsSelf() makes GameService resolvable by concrete type for GameInput injection.
            builder.RegisterEntryPoint<GameService>().AsSelf();

            builder.RegisterEntryPoint<SwipeDetector>().AsSelf();
            builder.RegisterEntryPoint<GameInput>();

            // ObstacleSpawner is a MonoBehaviour placed in the scene — inject GameService into it.
            // Null-guard allows the scene to run without ObstacleSpawner during isolated tests.
            if (_obstacleSpawner != null)
                builder.RegisterComponent(_obstacleSpawner);

            // TODO(netcode): builder.Register<NetworkService>(Lifetime.Singleton)

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
