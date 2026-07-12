using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;

using SplitRun.Obstacle;
using SplitRun.Utility;

namespace SplitRun.Environment
{
    [CreateAssetMenu(fileName = "WTP_New", menuName = "SplitRun/World Theme Profile")]
    public class WorldThemeProfile : ScriptableObject
    {
        [SerializeField] private string           _themeName;
        [SerializeField] private Transform        _segmentPrefab;
        [SerializeField] private BackdropFollower _backdropPrefab;

        [SerializeField] private EnumKeyedArray<ObstacleType, ObstacleVariants> _obstaclePrefabs =
            new EnumKeyedArray<ObstacleType, ObstacleVariants>();

        public string                                          ThemeName       => _themeName;
        public Transform                                       SegmentPrefab   => _segmentPrefab;
        public BackdropFollower                                BackdropPrefab  => _backdropPrefab;
        public EnumKeyedArray<ObstacleType, ObstacleVariants> ObstaclePrefabs => _obstaclePrefabs;
    }

    // Unity cannot serialize a jagged array, so the variant list needs one wrapper level.
    [Serializable]
    public sealed class ObstacleVariants
    {
        [SerializeField] private AssetReferenceGameObject[] _prefabs = Array.Empty<AssetReferenceGameObject>();

        public IReadOnlyList<AssetReferenceGameObject> Prefabs => _prefabs;
    }
}
