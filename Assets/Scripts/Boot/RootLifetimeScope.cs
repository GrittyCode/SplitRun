using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Ad;
using SplitRun.Audio;
using SplitRun.Data;
using SplitRun.Environment;
using SplitRun.Item;
using SplitRun.Mission;
using SplitRun.Network;
using SplitRun.UI.Boot;
using SplitRun.UI.Game;

namespace SplitRun.Boot
{
    public class RootLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private BootView _bootView;

        [Header("Catalog Assets")]
        [SerializeField] private MissionCatalog    _missionCatalog;
        [SerializeField] private AudioLibrary      _audioLibrary;
        [SerializeField] private ShopCatalog       _shopCatalog;
        [SerializeField] private ItemCatalog       _itemCatalog;
        [SerializeField] private HudIconLibrary    _hudIconLibrary;
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
            builder.RegisterInstance(_shopCatalog);
            builder.RegisterInstance(_itemCatalog);
            builder.RegisterInstance(_hudIconLibrary);
            builder.RegisterInstance(_worldTheme);

            builder.Register<PlayerDataService>(Lifetime.Singleton);
            builder.Register<MissionService>(Lifetime.Singleton);
            builder.Register<AssetPreloadService>(Lifetime.Singleton);
            builder.Register<AdService>(Lifetime.Singleton);

            // Root-scoped so the Relay session created in Lobby survives the load into Game.
            builder.Register<NetworkService>(Lifetime.Singleton);

            builder.RegisterComponent(_bootView);

            builder.RegisterEntryPoint<AudioService>();

            // AsSelf() so BootView can read the loader's progress/status reactives.
            builder.RegisterEntryPoint<BootLoader>().AsSelf();
        }
    }
}
