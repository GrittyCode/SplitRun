using UnityEngine;

using VContainer.Unity;

namespace SplitRun.Environment
{
    // Instantiates the active theme's single runtime world object, the backdrop. The track and
    // obstacles are pooled by their own systems; the backdrop is one follow object, so it is
    // spawned once here straight from the theme rather than living in the scene.
    public class WorldBuilder : IStartable
    {
        private readonly WorldThemeProfile _theme;

        public WorldBuilder(WorldThemeProfile theme)
        {
            _theme = theme;
        }

        public void Start()
        {
            if (!_theme || !_theme.BackdropPrefab)
            {
                Debug.LogWarning("[WorldBuilder] No theme backdrop prefab — backdrop skipped.");
                return;
            }

            Object.Instantiate(_theme.BackdropPrefab);
        }
    }
}
