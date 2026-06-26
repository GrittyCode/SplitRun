namespace SplitRun.Obstacle
{
    // Single key for an obstacle's X placement (lane vs full-width) and its stamped BoxCollider
    // footprint (size + center). All obstacles are floor-based; the slide variants offset their
    // collider center up to head height to force a slide — they are not ceiling-hung.
    public enum ObstacleFootprint
    {
        Vertical,   // one lane, full-height wall — P1 lane change
        LaneJump,   // one lane, low ground bar — P2 jump or P1 lane change
        LaneSlide,  // one lane, head-height bar — P2 slide or P1 lane change
        WideJump,   // full width, low ground bar — P2 jump
        WideSlide,  // full width, head-height bar — P2 slide
    }
}
