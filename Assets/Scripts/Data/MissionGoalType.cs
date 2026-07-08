namespace SplitRun.Data
{
    public enum MissionGoalType
    {
        // Highest distance reached within a single run (best-of, not accumulated).
        DistanceSingleRun,

        // Values accumulated across every run in the daily window.
        CoinsTotal,
        JumpsTotal,
        SlidesTotal,
        LaneChangesTotal,
    }
}
