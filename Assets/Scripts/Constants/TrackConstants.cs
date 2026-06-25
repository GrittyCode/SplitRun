namespace SplitRun.Constants
{
    public static class TrackConstants
    {
        // Distance ahead of the character kept filled with ground segments.
        public const float k_TrackFillAheadDistance = 80f;

        // Distance behind the character before a passed segment is recycled to the front.
        public const float k_TrackRecycleBehindDistance = 12f;
    }
}
