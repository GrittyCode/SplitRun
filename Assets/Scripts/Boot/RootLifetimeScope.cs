using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Data;
using SplitRun.Network;

namespace SplitRun.Boot
{
    public class RootLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PlayerDataService>(Lifetime.Singleton);
            builder.Register<AdService>(Lifetime.Singleton);

            // Root-scoped so the Relay session created in Lobby survives the load into Game.
            builder.Register<NetworkService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<BootLoader>();
        }
    }
}
