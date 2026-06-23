namespace SplitRun.Obstacle
{
    // How ObstacleSpawner derives a prefab's Y position from its Scale at spawn time
    public enum ObstacleAnchor
    {
        // Sits on the floor — base at Y=0, center at ScaleY / 2.
        Grounded,

        // clearance height, center at clearance + ScaleY / 2.
        Ceiling,

        // Spawner leaves Y untouched — for composite prefabs
        Manual,
    }
}
