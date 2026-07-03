using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Character;
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
        [SerializeField] private GameSession      _gameSession;
        [SerializeField] private TrackSpawner     _trackSpawner;
        [SerializeField] private TrackScroller    _trackScroller;
        [SerializeField] private GameHUDView      _hudView;
        [SerializeField] private ItemBuffView     _itemBuffView;
        [SerializeField] private SkillGaugeView   _skillGaugeView;
        [SerializeField] private PauseOverlayView _pauseOverlayView;

        [Header("Scriptable Objects")]
        [SerializeField] private LevelDesignProfile _levelProfile;
        [SerializeField] private WorldThemeProfile  _worldTheme;
        [SerializeField] private ItemCatalog        _itemCatalog;
        [SerializeField] private HudIconLibrary     _hudIconLibrary;
        [SerializeField] private CharacterCatalog   _characterCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            // AsSelf() so consumers resolve these by concrete type.
            builder.RegisterEntryPoint<GameService>().AsSelf();
            builder.RegisterEntryPoint<SwipeDetector>().AsSelf();
            builder.RegisterEntryPoint<GameInput>();
            builder.RegisterEntryPoint<ItemService>().AsSelf();

            // Single assignment point for asset references — every consumer injects these.
            builder.RegisterInstance(_levelProfile);
            builder.RegisterInstance(_worldTheme);
            builder.RegisterInstance(_itemCatalog);
            builder.RegisterInstance(_hudIconLibrary);
            builder.RegisterInstance(_characterCatalog);

            builder.RegisterComponent(_gameSession);
            builder.RegisterComponent(_trackSpawner);
            builder.RegisterComponent(_trackScroller);
            builder.RegisterComponent(_hudView);
            builder.RegisterComponent(_itemBuffView);
            builder.RegisterComponent(_skillGaugeView);
            builder.RegisterComponent(_pauseOverlayView);

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
