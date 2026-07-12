using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Environment;
using SplitRun.Item;
using SplitRun.LevelDesign;
using SplitRun.Obstacle;
using SplitRun.UI.Game;

namespace SplitRun.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private GameSession     _gameSession;
        [SerializeField] private ObstacleSpawner _obstacleSpawner;
        [SerializeField] private TrackScroller   _trackScroller;
        [SerializeField] private GameHudView     _hudView;

        [Header("Scriptable Objects")]
        [SerializeField] private LevelDesignProfile _levelProfile;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameService>().AsSelf();
            builder.RegisterEntryPoint<ItemService>().AsSelf();
            builder.RegisterEntryPoint<GameInput>();

            // The difficulty axis is a per-run swap target; every other catalog lives at Root.
            builder.RegisterInstance(_levelProfile);

            builder.RegisterComponent(_gameSession);
            builder.RegisterComponent(_obstacleSpawner);
            builder.RegisterComponent(_trackScroller);
            builder.RegisterComponent(_hudView);

            builder.RegisterEntryPoint<GameAudioBinder>();
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
