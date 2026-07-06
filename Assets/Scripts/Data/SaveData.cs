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
        public HatType       SelectedHat       = HatType.None;
        public int[]         UnlockedCharacters = { 0 };
        public int[]         UnlockedHats       = { };
    }
}
