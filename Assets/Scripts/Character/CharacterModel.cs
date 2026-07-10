using UnityEngine;

using SplitRun.Constants;

namespace SplitRun.Character
{
    public class CharacterModel : MonoBehaviour
    {
        private Transform  _hatSocket;
        private GameObject _hatInstance;

        private void Awake()
        {
            _hatSocket = FindHatSocket();

            if (!_hatSocket)
                Debug.LogWarning($"[CharacterModel] '{name}' has no child named '{CharacterConstants.k_HatSocketName}' — hats disabled.");
        }

        /// <summary>Replaces the worn hat with the given prefab. Pass null to remove the hat.</summary>
        public void AttachHat(GameObject hatPrefab)
        {
            if (_hatInstance)
                Destroy(_hatInstance);

            _hatInstance = hatPrefab && _hatSocket ? Instantiate(hatPrefab, _hatSocket) : null;
        }

        // The socket sits under the head bone, so the lookup must walk the whole rig by name.
        private Transform FindHatSocket()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == CharacterConstants.k_HatSocketName)
                    return child;
            }

            return null;
        }
    }
}
