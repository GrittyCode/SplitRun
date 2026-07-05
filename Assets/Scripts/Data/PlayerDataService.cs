using System;

using UnityEngine;

using R3;

using SplitRun.Character;
using SplitRun.Utility;

namespace SplitRun.Data
{
    public class PlayerDataService : IDisposable
    {
        private const string k_SaveFile = "player_data.json";

        private readonly ReactiveProperty<int>           _coins             = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int>           _bestDistance      = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<CharacterType> _selectedCharacter = new ReactiveProperty<CharacterType>(CharacterType.Default);

        // Persisted round-trip only — no consumer exists yet.
        // TODO(shop): expose these reactively once the shop/customization views consume them
        private int[] _unlockedCharacters = { 0 };
        private int[] _unlockedColors     = { 0 };
        private int[] _unlockedTrails     = { 0 };

        public ReadOnlyReactiveProperty<int>           Coins             => _coins;
        public ReadOnlyReactiveProperty<int>           BestDistance      => _bestDistance;
        public ReadOnlyReactiveProperty<CharacterType> SelectedCharacter => _selectedCharacter;

        /// <summary>Loads persisted player data from local JSON into reactive state.</summary>
        public void Load()
        {
            SaveData data = LocalJsonStorage.Load<SaveData>(k_SaveFile);

            _coins.Value             = data.Coins;
            _bestDistance.Value      = data.BestDistance;
            _selectedCharacter.Value = data.SelectedCharacter;
            _unlockedCharacters      = data.UnlockedCharacters;
            _unlockedColors          = data.UnlockedColors;
            _unlockedTrails          = data.UnlockedTrails;

            Debug.Log($"[PlayerDataService] Loaded — coins: {data.Coins}, best: {data.BestDistance}m");
        }

        /// <summary>Writes current reactive state to local JSON.</summary>
        public void Save()
        {
            var data = new SaveData
            {
                Coins              = _coins.Value,
                BestDistance       = _bestDistance.Value,
                SelectedCharacter  = _selectedCharacter.Value,
                UnlockedCharacters = _unlockedCharacters,
                UnlockedColors     = _unlockedColors,
                UnlockedTrails     = _unlockedTrails,
            };

            LocalJsonStorage.Save(k_SaveFile, data);
            Debug.Log($"[PlayerDataService] Saved — coins: {data.Coins}, best: {data.BestDistance}m");
        }

        /// <summary>Adds earned coins to the persistent total. Called once per run at run end.</summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            _coins.Value += amount;

            // Mutations persist immediately — a mobile OS kill must not lose earned progress.
            Save();
        }

        /// <summary>Updates the best distance record if the new value exceeds the current one.</summary>
        public void UpdateBestDistance(int distance)
        {
            if (distance <= _bestDistance.Value)
                return;

            _bestDistance.Value = distance;
            Save();
        }

        /// <summary>Sets the character spawned for this player's runs. Written by the Storage view.</summary>
        public void SelectCharacter(CharacterType type) => _selectedCharacter.Value = type;

        // TODO(shop): add SpendCoins/Unlock when the shop view consumes them

        public void Dispose()
        {
            // Safety save so no progress is lost when the root scope tears down
            Save();

            _coins.Dispose();
            _bestDistance.Dispose();
            _selectedCharacter.Dispose();
        }
    }
}
