namespace SplitRun.Obstacle
{
    // How ObstacleSpawner positions a rented prefab on the X axis.
    public enum ObstaclePlacement
    {
        // Single-lane obstacle — spawner picks a random valid lane (-2 / 0 / 2).
        RandomLane,

        // Full-width wall or composite coop prefab — root sits at center and its own
        FixedCenter,
    }
}
