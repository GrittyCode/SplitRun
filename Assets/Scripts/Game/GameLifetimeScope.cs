using UnityEngine;

using VContainer;
using VContainer.Unity;

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
        [SerializeField] private TrackSpawner   _trackSpawner;
        [SerializeField] private TrackScroller  _trackScroller;
        [SerializeField] private GameHUDView    _hudView;
        [SerializeField] private ItemBuffView   _itemBuffView;
        [SerializeField] private SkillGaugeView _skillGaugeView;

        [SerializeField] private LevelDesignProfile _levelProfile;
        [SerializeField] private WorldThemeProfile  _worldTheme;
        [SerializeField] private ItemCatalog        _itemCatalog;
        [SerializeField] private HudIconLibrary     _hudIconLibrary;

        protected override void Configure(IContainerBuilder builder)
        {
            // AsSelf() makes GameService resolvable by concrete type for GameInput injection.
            builder.RegisterEntryPoint<GameService>().AsSelf();

            builder.RegisterEntryPoint<SwipeDetector>().AsSelf();
            builder.RegisterEntryPoint<GameInput>();

            // AsSelf() so TrackSpawner and the HUD views resolve ItemService by concrete type.
            builder.RegisterInstance(_itemCatalog);
            builder.RegisterEntryPoint<ItemService>().AsSelf();

            if (_hudIconLibrary)
                builder.RegisterInstance(_hudIconLibrary);

            // Single assignment point for level + theme data: every consumer injects these, so no
            // scene object holds prefab or profile references directly.
            if (_levelProfile)
                builder.RegisterInstance(_levelProfile);

            if (_worldTheme)
                builder.RegisterInstance(_worldTheme);

            // MonoBehaviours placed in the scene — registered so dependencies are injected into
            // them. Null-guarded so the scene still runs in isolated tests with any absent.
            if (_trackSpawner)
                builder.RegisterComponent(_trackSpawner);

            if (_trackScroller)
                builder.RegisterComponent(_trackScroller);

            if (_hudView)
                builder.RegisterComponent(_hudView);

            if (_itemBuffView)
                builder.RegisterComponent(_itemBuffView);

            if (_skillGaugeView)
                builder.RegisterComponent(_skillGaugeView);

            builder.RegisterEntryPoint<WorldBuilder>();

            // TODO(netcode): builder.Register<NetworkService>(Lifetime.Singleton)

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
