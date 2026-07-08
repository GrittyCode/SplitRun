using System;
using System.Collections.Generic;

using UnityEngine;

namespace SplitRun.Data
{
    // The full mission pool the daily set is drawn from. Adding a mission of an existing goal type
    // is one entry, no code — a new goal type is one enum value plus one report call.
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
        // Stable identity used as the save key — keep unique and unchanged once shipped.
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
}
