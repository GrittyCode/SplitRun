using UnityEngine;

using SplitRun.Utility;

namespace SplitRun.Item
{
    [CreateAssetMenu(fileName = "ITEM_Catalog", menuName = "SplitRun/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private EnumKeyedArray<ItemType, ItemPickup> _prefabs =
            new EnumKeyedArray<ItemType, ItemPickup>();

        [SerializeField] private EnumKeyedArray<ItemType, int> _poolSizes =
            new EnumKeyedArray<ItemType, int>();

        public EnumKeyedArray<ItemType, ItemPickup> Prefabs => _prefabs;

        public int PoolSizeFor(ItemType type) => _poolSizes[type];
    }
}
