using System;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;

using SplitRun.Utility;

namespace SplitRun.Data
{
    // Owns the daily mission set: generation, progress reporting, claiming, and persistence.
    // Root-scoped so run progress reported from the Game scene survives the scene transition.
    public sealed class MissionService : IDisposable
    {
        private const string k_SaveFile = "mission_data.json";

        private readonly MissionCatalog    _catalog;
        private readonly PlayerDataService _playerDataService;

        private readonly List<MissionState> _missions = new List<MissionState>();

        private string _generatedDate = string.Empty;

        public MissionService(MissionCatalog catalog, PlayerDataService playerDataService)
        {
            _catalog           = catalog;
            _playerDataService = playerDataService;
        }

        public IReadOnlyList<MissionState> Missions => _missions;

        /// <summary>Loads the persisted daily set, regenerating it when the local date has rolled over.</summary>
        public void Load()
        {
            MissionSaveData data = LocalJsonStorage.Load<MissionSaveData>(k_SaveFile);
            _generatedDate = data.GeneratedDate;

            if (_generatedDate == Today())
                Restore(data);
            else
                GenerateDailySet();

            Debug.Log($"[MissionService] Loaded — {_missions.Count} missions for {_generatedDate}");
        }

        /// <summary>Regenerates the set when the local day has changed since the last generation.</summary>
        public void RefreshIfNewDay()
        {
            if (_generatedDate == Today())
                return;

            GenerateDailySet();
        }

        /// <summary>Writes the current daily set and its progress to local JSON.</summary>
        public void Save()
        {
            var data = new MissionSaveData
            {
                GeneratedDate = _generatedDate,
                Missions      = ToEntries(),
            };

            LocalJsonStorage.Save(k_SaveFile, data);
        }

        /// <summary>Reports one run's action totals. Called once at run end from the Game scene.</summary>
        public void ReportRun(int distance, int jumps, int slides, int laneChanges)
        {
            bool changed = false;
            changed |= Apply(MissionGoalType.DistanceSingleRun, distance);
            changed |= Apply(MissionGoalType.JumpsTotal,        jumps);
            changed |= Apply(MissionGoalType.SlidesTotal,       slides);
            changed |= Apply(MissionGoalType.LaneChangesTotal,  laneChanges);

            if (changed)
                Save();
        }

        /// <summary>Reports the coins collected in one run. Called once at run end from the item merge.</summary>
        public void ReportRunCoins(int coins)
        {
            if (Apply(MissionGoalType.CoinsTotal, coins))
                Save();
        }

        /// <summary>Grants the reward and marks the mission claimed. Returns false when not claimable.</summary>
        public bool TryClaim(string missionId)
        {
            MissionState mission = Find(missionId);
            if (mission == null || !mission.IsClaimable)
                return false;

            mission.Claimed = true;
            _playerDataService.AddCoins(mission.Definition.RewardCoins);
            Save();
            return true;
        }

        public void Dispose()
        {
            // Safety save so no daily progress is lost when the root scope tears down.
            Save();
        }

        // DistanceSingleRun keeps the best single run; every other goal accumulates. Claimed missions
        // stop advancing. Returns whether any mission's progress moved.
        private bool Apply(MissionGoalType type, int value)
        {
            if (value <= 0)
                return false;

            bool changed = false;
            foreach (MissionState mission in _missions)
            {
                if (mission.Definition.GoalType != type || mission.Claimed)
                    continue;

                int raised = IsSingleRunBest(type) ? Mathf.Max(mission.Progress, value) : mission.Progress + value;
                int next   = Mathf.Min(mission.Definition.Target, raised);

                if (next == mission.Progress)
                    continue;

                mission.Progress = next;
                changed = true;
            }

            return changed;
        }

        private void GenerateDailySet()
        {
            _generatedDate = Today();
            _missions.Clear();

            foreach (MissionDefinition definition in PickDaily())
                _missions.Add(new MissionState(definition, 0, false));

            Save();
        }

        // Restores saved progress, dropping entries whose definition no longer exists in the catalog.
        private void Restore(MissionSaveData data)
        {
            _missions.Clear();
            foreach (MissionEntry entry in data.Missions)
            {
                MissionDefinition definition = _catalog.Find(entry.Id);
                if (definition == null)
                    continue;

                int progress = Mathf.Clamp(entry.Progress, 0, definition.Target);
                _missions.Add(new MissionState(definition, progress, entry.Claimed));
            }
        }

        // Fisher–Yates shuffle over a copy of the pool, then take the daily count.
        private List<MissionDefinition> PickDaily()
        {
            var pool = new List<MissionDefinition>(_catalog.Pool);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                MissionDefinition swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }

            int count = Mathf.Min(_catalog.DailyCount, pool.Count);
            return pool.GetRange(0, count);
        }

        private MissionEntry[] ToEntries()
        {
            var entries = new MissionEntry[_missions.Count];
            for (int i = 0; i < _missions.Count; i++)
            {
                MissionState mission = _missions[i];
                entries[i] = new MissionEntry
                {
                    Id       = mission.Definition.Id,
                    Progress = mission.Progress,
                    Claimed  = mission.Claimed,
                };
            }

            return entries;
        }

        private MissionState Find(string missionId)
        {
            foreach (MissionState mission in _missions)
            {
                if (mission.Definition.Id == missionId)
                    return mission;
            }

            return null;
        }

        private static bool IsSingleRunBest(MissionGoalType type) => type == MissionGoalType.DistanceSingleRun;

        private static string Today() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
