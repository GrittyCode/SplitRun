using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;

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

        public string                          ThemeName       => _themeName;
        public IReadOnlyList<FootprintPrefabs> ObstaclePrefabs => _obstaclePrefabs;
        public Transform                       SegmentPrefab   => _segmentPrefab;
        public BackdropFollower                BackdropPrefab  => _backdropPrefab;
    }

    // One or more interchangeable prefab variants for a footprint, loaded through Addressables.
    [Serializable]
    public struct FootprintPrefabs
    {
        [SerializeField] private ObstacleFootprint          _footprint;
        [SerializeField] private AssetReferenceGameObject[] _prefabs;

        public ObstacleFootprint                      Footprint => _footprint;
        public IReadOnlyList<AssetReferenceGameObject> Prefabs   => _prefabs;
    }
}
