using VContainer;
using VContainer.Unity;

namespace SplitRun.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GameService>(Lifetime.Singleton);

            // TODO(netcode): builder.Register<NetworkService>(Lifetime.Singleton)
            // TODO(chunk): add [SerializeField] private ChunkSpawner _chunkSpawner

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
