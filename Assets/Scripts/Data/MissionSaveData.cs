using System;

namespace SplitRun.Data
{
    [Serializable]
    public sealed class MissionSaveData
    {
        // Local calendar date (yyyy-MM-dd) the current set was generated on; drives the daily reset.
        public string         GeneratedDate = string.Empty;
        public MissionEntry[] Missions      = { };
    }

    [Serializable]
    public sealed class MissionEntry
    {
        public string Id;
        public int    Progress;
        public bool   Claimed;
    }
}
