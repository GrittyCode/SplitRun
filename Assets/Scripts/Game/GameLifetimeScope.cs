using VContainer;
using VContainer.Unity;

using SplitRun.Utility;

namespace SplitRun.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // AsSelf() makes GameService resolvable by concrete type for injection into GameInput.
            builder.RegisterEntryPoint<GameService>().AsSelf();

            builder.RegisterEntryPoint<SwipeDetector>().AsSelf();
            builder.RegisterEntryPoint<GameInput>();

            // TODO(netcode): builder.Register<NetworkService>(Lifetime.Singleton)
            // TODO(chunk): register ChunkSpawner via RegisterComponent after adding [SerializeField] field

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
