using System;

using UnityEngine;

using SplitRun.Obstacle;
using SplitRun.Utility;

namespace SplitRun.LevelDesign
{
    public enum CoopPatternType
    {
        CoopJump  = 0,
        CoopSlide = 1,
    }

    [Serializable]
    public class ObstacleBand
    {
        [SerializeField] private float _startDistance;

        [SerializeField] private EnumKeyedArray<ObstacleType, float> _singleWeights =
            new EnumKeyedArray<ObstacleType, float>();

        [SerializeField] private EnumKeyedArray<CoopPatternType, float> _coopWeights =
            new EnumKeyedArray<CoopPatternType, float>();

        public float StartDistance => _startDistance;

        public EnumKeyedArray<ObstacleType, float>    SingleWeights => _singleWeights;
        public EnumKeyedArray<CoopPatternType, float> CoopWeights   => _coopWeights;
    }
}
