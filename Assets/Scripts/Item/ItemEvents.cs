using System;

namespace SplitRun.Item
{
    // Bridges the runtime-spawned character's trigger (outside the DI graph) to ItemService.
    public static class ItemEvents
    {
        public static event Action<ItemPickup> OnCollected;
        public static event Action<int>        OnCollectionConfirmed;

        public static void NotifyCollected(ItemPickup item) => OnCollected?.Invoke(item);

        /// <summary>Raised on every client when the server confirms a pickup collection.</summary>
        public static void NotifyCollectionConfirmed(int spawnId) => OnCollectionConfirmed?.Invoke(spawnId);
    }
}
