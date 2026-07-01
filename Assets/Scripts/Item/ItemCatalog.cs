using UnityEngine;

namespace SplitRun.Item
{
    // Single point that hands pickup prefabs into the DI graph, so no scene object holds them.
    [CreateAssetMenu(fileName = "ITEM_Catalog", menuName = "SplitRun/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemPickup _coinPrefab;
        [SerializeField] private ItemPickup _magnetPrefab;

        public ItemPickup CoinPrefab   => _coinPrefab;
        public ItemPickup MagnetPrefab => _magnetPrefab;
    }
}
