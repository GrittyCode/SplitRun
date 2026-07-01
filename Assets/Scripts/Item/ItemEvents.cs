using System;

namespace SplitRun.Item
{
    // Bridges the runtime-spawned character's trigger (outside the DI graph) to ItemService.
    public static class ItemEvents
    {
        public static event Action<ItemPickup> OnCollected;

        public static void NotifyCollected(ItemPickup item) => OnCollected?.Invoke(item);
    }
}
