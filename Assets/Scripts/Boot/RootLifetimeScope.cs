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
        [Header("Data Assets")]
        [SerializeField] private MissionCatalog _missionCatalog;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_missionCatalog);

            builder.Register<PlayerDataService>(Lifetime.Singleton);
            builder.Register<MissionService>(Lifetime.Singleton);
            builder.Register<AdService>(Lifetime.Singleton);

            // Root-scoped so the Relay session created in Lobby survives the load into Game.
            builder.Register<NetworkService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<BootLoader>();
        }
    }
}
