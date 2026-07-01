using System;

using UnityEngine;

namespace SplitRun.Item
{
    // Serialized on GameLifetimeScope and registered as an instance — the single point that
    // hands pickup prefabs into the DI graph, so no scene object holds them directly.
    [Serializable]
    public sealed class ItemCatalog
    {
        [SerializeField] private ItemPickup _coinPrefab;
        [SerializeField] private ItemPickup _magnetPrefab;

        public ItemPickup CoinPrefab   => _coinPrefab;
        public ItemPickup MagnetPrefab => _magnetPrefab;
    }
}
