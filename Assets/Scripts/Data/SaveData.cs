using System;

namespace SplitRun.Data
{
    [Serializable]
    public class SaveData
    {
        public int   Coins;
        public int   BestDistance;
        public int[] UnlockedCharacters = { 0 };
        public int[] UnlockedColors     = { 0 };
        public int[] UnlockedTrails     = { 0 };
    }
}