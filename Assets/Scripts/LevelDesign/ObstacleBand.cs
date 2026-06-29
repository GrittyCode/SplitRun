using System;

using UnityEngine;

using SplitRun.Obstacle;

namespace SplitRun.LevelDesign
{
    // A distance tier within a profile, active from its StartDistance until the next band begins.
    // Single-footprint obstacles and coop patterns share one weighted roll, so coop frequency is
    // tuned per tier exactly like any single obstacle (e.g. coop weight 0 in the opening tier).
    [Serializable]
    public class ObstacleBand
    {
        [SerializeField] private float                     _startDistance;
        [SerializeField] private ObstacleFootprintWeight[] _singleWeights = Array.Empty<ObstacleFootprintWeight>();
        [SerializeField] private CoopPatternWeight[]       _coopWeights   = Array.Empty<CoopPatternWeight>();

        public float StartDistance => _startDistance;

        // Returned as arrays so the spawner's per-slot selection iterates without allocating an
        // interface enumerator.
        public ObstacleFootprintWeight[] SingleWeights => _singleWeights;
        public CoopPatternWeight[]       CoopWeights   => _coopWeights;
    }

    // Named pair instead of a positional array: the footprint is stored with its weight so
    // authoring never depends on remembering the enum's declaration order in the inspector.
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
