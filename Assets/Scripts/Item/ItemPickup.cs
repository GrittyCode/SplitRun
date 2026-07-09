using System;

using UnityEngine;

using SplitRun.Constants;

namespace SplitRun.Item
{
    public enum ItemType
    {
        Coin,
        Magnet,
    }

    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemType _type;

        private Quaternion _initialRotation;

        public ItemType Type    => _type;
        public int      SpawnId { get; private set; }

        // Pooled instances live outside the DI graph, so the character's trigger reaches ItemService here.
        public static event Action<ItemPickup> OnCollected;

        private void Awake() => _initialRotation = transform.localRotation;

        private void Update()
        {
            transform.Rotate(0f, ItemConstants.k_SpinSpeed * Time.deltaTime, 0f, Space.World);
        }

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
        private void Reset() => EnforceItemLayer();

        private void OnValidate() => EnforceItemLayer();

        // Enforced at author time so a hand-edited layer cannot silently break trigger collisions.
        private void EnforceItemLayer()
        {
            int layer = LayerMask.NameToLayer(ItemConstants.k_ItemLayerName);
            if (layer < 0)
            {
                Debug.LogWarning(
                    $"[ItemPickup] Layer '{ItemConstants.k_ItemLayerName}' does not exist. " +
                    "Add it in Project Settings -> Tags and Layers, then enable Character x " +
                    $"{ItemConstants.k_ItemLayerName} in the Physics collision matrix.", this);
                return;
            }

            if (gameObject.layer != layer)
                gameObject.layer = layer;
        }
#endif
    }
}
