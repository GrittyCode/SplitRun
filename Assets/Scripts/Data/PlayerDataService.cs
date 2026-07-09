using System;
using System.Collections.Generic;

using UnityEngine;

using R3;

using SplitRun.Character;
using SplitRun.Utility;

namespace SplitRun.Data
{
    [Serializable]
    public class SaveData
    {
        public int           Coins;
        public int           BestDistance;
        public CharacterType SelectedCharacter  = CharacterType.Default;
        public HatType       SelectedHat        = HatType.None;
        public int[]         UnlockedCharacters = { 0 };
        public int[]         UnlockedHats       = { };
    }

    public class PlayerDataService : IDisposable
    {
        private const string k_SaveFile = "player_data.json";

        private readonly ReactiveProperty<int>           _coins             = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int>           _bestDistance      = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<CharacterType> _selectedCharacter = new ReactiveProperty<CharacterType>(CharacterType.Default);
        private readonly ReactiveProperty<HatType>       _selectedHat       = new ReactiveProperty<HatType>(HatType.None);

        private readonly HashSet<CharacterType> _unlockedCharacters = new HashSet<CharacterType> { CharacterType.Default };
        private readonly HashSet<HatType>       _unlockedHats       = new HashSet<HatType>();

        public ReadOnlyReactiveProperty<int>           Coins             => _coins;
        public ReadOnlyReactiveProperty<int>           BestDistance      => _bestDistance;
        public ReadOnlyReactiveProperty<CharacterType> SelectedCharacter => _selectedCharacter;
        public ReadOnlyReactiveProperty<HatType>       SelectedHat       => _selectedHat;

        public void Load()
        {
            SaveData data = LocalJsonStorage.Load<SaveData>(k_SaveFile);

            _coins.Value        = data.Coins;
            _bestDistance.Value = data.BestDistance;

            _unlockedCharacters.Clear();
            _unlockedCharacters.Add(CharacterType.Default);
            foreach (int id in data.UnlockedCharacters)
                _unlockedCharacters.Add((CharacterType)id);

            _unlockedHats.Clear();
            foreach (int id in data.UnlockedHats)
                _unlockedHats.Add((HatType)id);

            // A selection pointing at locked content (edited or stale save) falls back to defaults.
            _selectedCharacter.Value = IsCharacterUnlocked(data.SelectedCharacter) ? data.SelectedCharacter : CharacterType.Default;
            _selectedHat.Value       = IsHatUnlocked(data.SelectedHat) ? data.SelectedHat : HatType.None;

            Debug.Log($"[PlayerDataService] Loaded — coins: {data.Coins}, best: {data.BestDistance}m");
        }

        public void Save()
        {
            var data = new SaveData
            {
                Coins              = _coins.Value,
                BestDistance       = _bestDistance.Value,
                SelectedCharacter  = _selectedCharacter.Value,
                SelectedHat        = _selectedHat.Value,
                UnlockedCharacters = ToIntArray(_unlockedCharacters),
                UnlockedHats       = ToIntArray(_unlockedHats),
            };

            LocalJsonStorage.Save(k_SaveFile, data);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            _coins.Value += amount;

            // Mutations persist immediately — a mobile OS kill must not lose earned progress.
            Save();
        }

        public void UpdateBestDistance(int distance)
        {
            if (distance <= _bestDistance.Value)
                return;

            _bestDistance.Value = distance;
            Save();
        }

        public bool IsCharacterUnlocked(CharacterType type) => _unlockedCharacters.Contains(type);

        public bool IsHatUnlocked(HatType type) => type == HatType.None || _unlockedHats.Contains(type);

        /// <summary>Spends coins to unlock the character and equips it. False when already owned or unaffordable.</summary>
        public bool TryPurchaseCharacter(CharacterType type, int price)
        {
            if (IsCharacterUnlocked(type) || _coins.Value < price)
                return false;

            _coins.Value -= price;
            _unlockedCharacters.Add(type);
            _selectedCharacter.Value = type;
            Save();
            return true;
        }

        /// <summary>Spends coins to unlock the hat and equips it. False when already owned or unaffordable.</summary>
        public bool TryPurchaseHat(HatType type, int price)
        {
            if (IsHatUnlocked(type) || _coins.Value < price)
                return false;

            _coins.Value -= price;
            _unlockedHats.Add(type);
            _selectedHat.Value = type;
            Save();
            return true;
        }

        public void SelectCharacter(CharacterType type)
        {
            if (!IsCharacterUnlocked(type) || _selectedCharacter.Value == type)
                return;

            _selectedCharacter.Value = type;
            Save();
        }

        public void SelectHat(HatType type)
        {
            if (!IsHatUnlocked(type) || _selectedHat.Value == type)
                return;

            _selectedHat.Value = type;
            Save();
        }

        public void Dispose()
        {
            Save();

            _coins.Dispose();
            _bestDistance.Dispose();
            _selectedCharacter.Dispose();
            _selectedHat.Dispose();
        }

        private static int[] ToIntArray<T>(HashSet<T> set) where T : struct, Enum
        {
            var result = new int[set.Count];
            int index = 0;
            foreach (T value in set)
                result[index++] = Convert.ToInt32(value);

            return result;
        }
    }
}
