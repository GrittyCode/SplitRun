using System;

using UnityEngine;

using R3;

using SplitRun.Utility;

namespace SplitRun.Data
{
    public class PlayerDataService : IDisposable
    {
        private const string SAVE_FILE = "player_data.json";

        private readonly ReactiveProperty<int>   _coins              = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int>   _bestDistance       = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int[]> _unlockedCharacters = new ReactiveProperty<int[]>(new[] { 0 });
        private readonly ReactiveProperty<int[]> _unlockedColors     = new ReactiveProperty<int[]>(new[] { 0 });
        private readonly ReactiveProperty<int[]> _unlockedTrails     = new ReactiveProperty<int[]>(new[] { 0 });

        public ReadOnlyReactiveProperty<int>   Coins              => _coins;
        public ReadOnlyReactiveProperty<int>   BestDistance       => _bestDistance;
        public ReadOnlyReactiveProperty<int[]> UnlockedCharacters => _unlockedCharacters;
        public ReadOnlyReactiveProperty<int[]> UnlockedColors     => _unlockedColors;
        public ReadOnlyReactiveProperty<int[]> UnlockedTrails     => _unlockedTrails;

        /// <summary>Loads persisted player data from local JSON into reactive state.</summary>
        public void Load()
        {
            SaveData data = LocalJsonStorage.Load<SaveData>(SAVE_FILE);

            _coins.Value              = data.Coins;
            _bestDistance.Value       = data.BestDistance;
            _unlockedCharacters.Value = data.UnlockedCharacters;
            _unlockedColors.Value     = data.UnlockedColors;
            _unlockedTrails.Value     = data.UnlockedTrails;

            Debug.Log($"[PlayerDataService] Loaded — coins: {data.Coins}, best: {data.BestDistance}m");
        }

        /// <summary>Writes current reactive state to local JSON.</summary>
        public void Save()
        {
            var data = new SaveData
            {
                Coins              = _coins.Value,
                BestDistance       = _bestDistance.Value,
                UnlockedCharacters = _unlockedCharacters.Value,
                UnlockedColors     = _unlockedColors.Value,
                UnlockedTrails     = _unlockedTrails.Value,
            };

            LocalJsonStorage.Save(SAVE_FILE, data);
            Debug.Log($"[PlayerDataService] Saved — coins: {data.Coins}, best: {data.BestDistance}m");
        }

        /// <summary>Updates the best distance record if the new value exceeds the current one.</summary>
        public void UpdateBestDistance(int distance)
        {
            if (distance <= _bestDistance.Value)
                return;

            _bestDistance.Value = distance;
        }

        // TODO(shop): AddCoins(int amount), SpendCoins(int amount), Unlock(int unlockId)
        // Implement when ShopService is added in Phase 6

        public void Dispose()
        {
            // Safety save so no progress is lost when the root scope tears down
            Save();
        }
    }
}