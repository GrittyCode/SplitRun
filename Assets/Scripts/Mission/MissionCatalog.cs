using System;
using System.Collections.Generic;

using UnityEngine;

namespace SplitRun.Mission
{
    public enum MissionGoalType
    {
        // Best single run, not accumulated.
        DistanceSingleRun = 0,

        CoinsTotal       = 1,
        JumpsTotal       = 2,
        SlidesTotal      = 3,
        LaneChangesTotal = 4,
    }

    // Adding a mission of an existing goal type is one entry; a new goal type is one enum value
    // plus one report call.
    [CreateAssetMenu(fileName = "MissionCatalog", menuName = "SplitRun/Mission Catalog")]
    public sealed class MissionCatalog : ScriptableObject
    {
        [SerializeField] private MissionDefinition[] _pool;
        [SerializeField] private int                 _dailyCount = 3;

        public IReadOnlyList<MissionDefinition> Pool       => _pool;
        public int                              DailyCount => _dailyCount;

        public MissionDefinition Find(string id)
        {
            foreach (MissionDefinition definition in _pool)
            {
                if (definition.Id == id)
                    return definition;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class MissionDefinition
    {
        // Save key — keep unique and unchanged once shipped.
        [SerializeField] private string          _id;
        [SerializeField] private string          _displayName;
        [SerializeField] private MissionGoalType _goalType;
        [SerializeField] private int             _target;
        [SerializeField] private int             _rewardCoins;

        public string          Id          => _id;
        public string          DisplayName => _displayName;
        public MissionGoalType GoalType    => _goalType;
        public int             Target      => _target;
        public int             RewardCoins => _rewardCoins;
    }

    public sealed class MissionState
    {
        public MissionState(MissionDefinition definition, int progress, bool claimed)
        {
            Definition = definition;
            Progress   = progress;
            Claimed    = claimed;
        }

        public MissionDefinition Definition { get; }
        public int               Progress   { get; set; }
        public bool              Claimed    { get; set; }

        public bool IsComplete  => Progress >= Definition.Target;
        public bool IsClaimable => IsComplete && !Claimed;

        public float NormalizedProgress =>
            Definition.Target <= 0 ? 1f : Mathf.Clamp01((float)Progress / Definition.Target);
    }
}
