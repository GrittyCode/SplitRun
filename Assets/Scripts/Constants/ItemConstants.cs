namespace SplitRun.Constants
{
    public static class ItemConstants
    {
        public const int k_CoinValue = 1;

        // Coin line geometry — coins fill a free lane across the gap to the next slot.
        public const float k_CoinSpacing     = 4f;
        public const float k_CoinLineMargin  = 2f;
        public const float k_ItemHoverHeight = 1f;

        // Per-slot roll: chance a free lane gets a line, then chance that line is a magnet.
        public const float k_CoinLineChance = 0.6f;
        public const float k_MagnetChance   = 0.08f;

        public const float k_SpinSpeed = 180f;

        public const float k_MagnetDuration = 5f;
        public const float k_MagnetRadius   = 20f;

        // Above k_MaxRunSpeed so a pulled coin always catches the character.
        public const float k_MagnetPullSpeed = 24f;

        public const float k_ItemDespawnBehindDistance = 20f;
    }
}
