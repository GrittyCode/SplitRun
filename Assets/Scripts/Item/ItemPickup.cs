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

        public void Collect() => gameObject.SetActive(false);

        public void ResetState()
        {
            transform.localRotation = _initialRotation;
            gameObject.SetActive(true);
        }
    }
}
