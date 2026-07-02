using System;

using UnityEngine;

using SplitRun.Obstacle;

namespace SplitRun.LevelDesign
{
    // Singles and coop patterns share one weighted roll, so coop frequency is tuned like any obstacle.
    [Serializable]
    public class ObstacleBand
    {
        [SerializeField] private float                     _startDistance;
        [SerializeField] private ObstacleFootprintWeight[] _singleWeights = Array.Empty<ObstacleFootprintWeight>();
        [SerializeField] private CoopPatternWeight[]       _coopWeights   = Array.Empty<CoopPatternWeight>();

        public float StartDistance => _startDistance;

        // Arrays so the spawner's per-slot selection iterates without allocating an enumerator.
        public ObstacleFootprintWeight[] SingleWeights => _singleWeights;
        public CoopPatternWeight[]       CoopWeights   => _coopWeights;
    }

    // Named pair so inspector authoring never depends on the enum's declaration order.
    [Serializable]
    public struct ObstacleFootprintWeight
    {
        [SerializeField] private ObstacleFootprint _footprint;
        [SerializeField] private float             _weight;

        public ObstacleFootprint Footprint => _footprint;
        public float             Weight    => _weight;
    }

    [Serializable]
    public struct CoopPatternWeight
    {
        [SerializeField] private CoopPatternType _pattern;
        [SerializeField] private float           _weight;

        public CoopPatternType Pattern => _pattern;
        public float           Weight  => _weight;
    }
}
