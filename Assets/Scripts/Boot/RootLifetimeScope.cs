using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Audio;
using SplitRun.Data;
using SplitRun.Environment;
using SplitRun.Mission;
using SplitRun.Network;
using SplitRun.UI.Boot;

namespace SplitRun.Boot
{
    public class RootLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private BootView _bootView;

        [Header("Data Assets")]
        [SerializeField] private MissionCatalog _missionCatalog;
        [SerializeField] private AudioLibrary   _audioLibrary;

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
            builder.RegisterInstance(_audioLibrary);

            // Registered at Root so the boot preload and the Game scene share one theme instance.
            builder.RegisterInstance(_worldTheme);

            builder.Register<PlayerDataService>(Lifetime.Singleton);
            builder.Register<MissionService>(Lifetime.Singleton);
            builder.Register<AssetPreloadService>(Lifetime.Singleton);
            builder.Register<AdService>(Lifetime.Singleton);

            // Root-scoped so the Relay session created in Lobby survives the load into Game.
            builder.Register<NetworkService>(Lifetime.Singleton);

            builder.RegisterComponent(_bootView);

            // One audio host across every scene.
            builder.RegisterEntryPoint<AudioService>();

            // AsSelf() so BootView can read the loader's progress/status reactives.
            builder.RegisterEntryPoint<BootLoader>().AsSelf();
        }
    }
}
