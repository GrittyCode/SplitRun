using UnityEngine;

using SplitRun.Constants;

namespace SplitRun.Item
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemType _type;

        private Quaternion _initialRotation;

        public ItemType Type => _type;

        private void Awake() => _initialRotation = transform.localRotation;

        private void Update()
        {
            transform.Rotate(0f, ItemConstants.k_SpinSpeed * Time.deltaTime, 0f, Space.World);
        }

        public void ResetState()
        {
            transform.localRotation = _initialRotation;
            gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        private void Reset() => EnforceItemLayer();

        private void OnValidate() => EnforceItemLayer();

        // Sets the item layer at author time so a pickup can't silently miss trigger collisions
        // through a hand-edited layer — mirrors TrackObstacle's obstacle-layer enforcement.
        private void EnforceItemLayer()
        {
            int layer = LayerMask.NameToLayer(ItemConstants.k_ItemLayerName);
            if (layer < 0)
            {
                Debug.LogWarning(
                    $"[ItemPickup] Layer '{ItemConstants.k_ItemLayerName}' does not exist. " +
                    "Add it in Project Settings → Tags and Layers, then enable Character × " +
                    $"{ItemConstants.k_ItemLayerName} in the Physics collision matrix.", this);
                return;
            }

            if (gameObject.layer != layer)
                gameObject.layer = layer;
        }
#endif
    }
}
