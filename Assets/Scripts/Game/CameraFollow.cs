using UnityEngine;

using SplitRun.Character;
using SplitRun.Constants;

namespace SplitRun.Game
{
    // Attached directly to the scene's Main Camera. Not VContainer-registered — subscribes to
    // CharacterEvents directly, the same static hub GameService uses, since the target (a
    // dynamically Netcode-spawned or locally-spawned ICharacter) doesn't exist at scene load
    // time for a [SerializeField] reference.
    public class CameraFollow : MonoBehaviour
    {
        private Transform _target;

        private void OnEnable()
        {
            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;
        }

        private void OnDisable()
        {
            CharacterEvents.OnSpawned   -= OnCharacterSpawned;
            CharacterEvents.OnDespawned -= OnCharacterDespawned;
        }

        private void LateUpdate()
        {
            if (!_target) return;

            // X and Y are intentionally fixed — only Z (forward progress) is tracked, so lane
            // changes and jumps never sway or bob the camera. A stable frame is what lets an
            // obstacle's shape read as an instruction in its first visible frame — see
            // 05_design_principles.md, "Obstacle Readability".
            float targetZ = _target.position.z;

            Vector3 desiredPosition = new Vector3(
                CameraConstants.k_CameraOffsetX,
                CameraConstants.k_CameraOffsetY,
                targetZ + CameraConstants.k_CameraOffsetZ
            );

            Vector3 lookTarget = new Vector3(
                CameraConstants.k_CameraOffsetX,
                CameraConstants.k_CameraLookHeight,
                targetZ + CameraConstants.k_CameraLookAheadDistance
            );

            transform.position = desiredPosition;
            transform.rotation = Quaternion.LookRotation(lookTarget - desiredPosition);
        }

        private void OnCharacterSpawned(ICharacter character) => _target = character.CharacterTransform;

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_target == character.CharacterTransform) _target = null;
        }
    }
}
