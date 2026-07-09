using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Data;
using SplitRun.Environment;
using SplitRun.Item;
using SplitRun.LevelDesign;
using SplitRun.Obstacle;
using SplitRun.UI.Game;
using SplitRun.Utility;

namespace SplitRun.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private GameSession   _gameSession;
        [SerializeField] private TrackSpawner  _trackSpawner;
        [SerializeField] private TrackScroller _trackScroller;
        [SerializeField] private GameHUDView   _hudView;

        [Header("Scriptable Objects")]
        [SerializeField] private LevelDesignProfile _levelProfile;
        [SerializeField] private ItemCatalog        _itemCatalog;
        [SerializeField] private HudIconLibrary     _hudIconLibrary;
        [SerializeField] private ShopCatalog        _shopCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            // AsSelf() so consumers resolve these by concrete type.
            builder.RegisterEntryPoint<GameService>().AsSelf();
            builder.RegisterEntryPoint<SwipeDetector>().AsSelf();
            builder.RegisterEntryPoint<GameInput>();
            builder.RegisterEntryPoint<ItemService>().AsSelf();

            // Single assignment point for asset references — every consumer injects these.
            // WorldThemeProfile is registered at Root so boot preload and the run share one instance.
            builder.RegisterInstance(_levelProfile);
            builder.RegisterInstance(_itemCatalog);
            builder.RegisterInstance(_hudIconLibrary);
            builder.RegisterInstance(_shopCatalog);

            builder.RegisterComponent(_gameSession);
            builder.RegisterComponent(_trackSpawner);
            builder.RegisterComponent(_trackScroller);
            builder.RegisterComponent(_hudView);

            builder.RegisterEntryPoint<GameAudioBinder>();
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
