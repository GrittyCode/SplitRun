using System;

using UnityEngine;

using SplitRun.Utility;

namespace SplitRun.Item
{
    public enum ItemType
    {
        Coin   = 0,
        Magnet = 1,
    }

    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour
    {
        // Physics layer pickups live on; the character HitBox layer x this must be enabled.
        private const string k_LayerName = "Item";

        [SerializeField] private ItemType _type;

        private Quaternion _initialRotation;

        public ItemType Type    => _type;
        public int      SpawnId { get; private set; }

        // Pooled instances live outside the DI graph, so the character's trigger reaches ItemService here.
        public static event Action<ItemPickup> OnCollected;

        private void Awake() => _initialRotation = transform.localRotation;

        /// <summary>Assigns the deterministic per-run id shared by every client's copy of this pickup.</summary>
        public void Initialize(int spawnId) => SpawnId = spawnId;

        /// <summary>Raised by the character's trigger. Only the server's report is authoritative.</summary>
        public void NotifyCollected() => OnCollected?.Invoke(this);

        public void ResetState()
        {
            transform.localRotation = _initialRotation;
            gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        private void Reset() => LayerGuard.Enforce(gameObject, k_LayerName, nameof(ItemPickup));

        private void OnValidate() => LayerGuard.Enforce(gameObject, k_LayerName, nameof(ItemPickup));
#endif
    }
}
