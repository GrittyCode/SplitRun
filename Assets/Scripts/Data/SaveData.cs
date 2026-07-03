using System;

using SplitRun.Character;

namespace SplitRun.Data
{
    [Serializable]
    public class SaveData
    {
        public int           Coins;
        public int           BestDistance;
        public CharacterType SelectedCharacter = CharacterType.Default;
        public int[]         UnlockedCharacters = { 0 };
        public int[]         UnlockedColors     = { 0 };
        public int[]         UnlockedTrails     = { 0 };
    }
}
