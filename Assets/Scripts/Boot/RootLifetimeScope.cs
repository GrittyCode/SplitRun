using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Data;
using SplitRun.Environment;
using SplitRun.Network;

namespace SplitRun.Boot
{
    public class RootLifetimeScope : LifetimeScope
    {
        [Header("Data Assets")]
        [SerializeField] private MissionCatalog _missionCatalog;

        [Header("Theme Assets")]
        [SerializeField] private WorldThemeProfile _worldTheme;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_missionCatalog);

            // Registered at Root so the boot preload and the Game scene share one theme instance.
            builder.RegisterInstance(_worldTheme);

            builder.Register<PlayerDataService>(Lifetime.Singleton);
            builder.Register<MissionService>(Lifetime.Singleton);
            builder.Register<AssetPreloadService>(Lifetime.Singleton);
            builder.Register<AdService>(Lifetime.Singleton);

            // Root-scoped so the Relay session created in Lobby survives the load into Game.
            builder.Register<NetworkService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<BootLoader>();
        }
    }
}
