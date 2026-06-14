using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Data;

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

            // BootLoader runs StartAsync once after all singletons are resolved
            builder.RegisterEntryPoint<BootLoader>();
        }
    }
}