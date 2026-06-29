using System;
using System.Collections.Generic;

using UnityEngine;

using SplitRun.Obstacle;

namespace SplitRun.Environment
{
    // Maps abstract gameplay data to concrete art for one theme.
    [CreateAssetMenu(fileName = "WTP_New", menuName = "SplitRun/World Theme Profile")]
    public class WorldThemeProfile : ScriptableObject
    {
        [SerializeField] private string             _themeName;
        [SerializeField] private FootprintPrefabs[] _obstaclePrefabs = Array.Empty<FootprintPrefabs>();
        [SerializeField] private Transform          _segmentPrefab;
        [SerializeField] private BackdropFollower   _backdropPrefab;

        public string                           ThemeName       => _themeName;
        public IReadOnlyList<FootprintPrefabs>  ObstaclePrefabs => _obstaclePrefabs;
        public Transform                        SegmentPrefab   => _segmentPrefab;
        public BackdropFollower                 BackdropPrefab  => _backdropPrefab;

        // A prefab's own stamped footprint is the source of truth, so a prefab dropped into the
        // wrong slot would spawn under the wrong selection weight — caught here at author time.
        private void OnValidate()
        {
            if (_obstaclePrefabs == null) return;

            foreach (FootprintPrefabs set in _obstaclePrefabs)
            {
                if (set.Prefabs == null) continue;

                foreach (TrackObstacle prefab in set.Prefabs)
                {
                    if (!prefab || prefab.Footprint == set.Footprint) continue;

                    Debug.LogWarning(
                        $"[WorldThemeProfile] '{name}': '{prefab.name}' has footprint {prefab.Footprint} " +
                        $"but is assigned to the {set.Footprint} slot.",
                        this);
                }
            }
        }
    }

    // One or more interchangeable prefab variants for a footprint
    [Serializable]
    public struct FootprintPrefabs
    {
        [SerializeField] private ObstacleFootprint _footprint;
        [SerializeField] private TrackObstacle[]   _prefabs;

        public ObstacleFootprint            Footprint => _footprint;
        public IReadOnlyList<TrackObstacle> Prefabs   => _prefabs;
    }
}
