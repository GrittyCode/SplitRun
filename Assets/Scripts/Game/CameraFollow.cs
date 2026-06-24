using UnityEngine;

using SplitRun.Character;
using SplitRun.Constants;

namespace SplitRun.Game
{
    // Attached directly to the scene's Main Camera. Not VContainer-registered — subscribes to
    // CharacterEvents directly since the target (a dynamically Netcode-spawned or locally-spawned
    // ICharacter) doesn't exist at scene load time for a [SerializeField] reference.
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        private Camera _camera;
        private Transform _target;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.fieldOfView = CameraConstants.k_CameraFov;
        }

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

            // Only Z tracks the character — X and Y are fixed so lane changes and
            // jumps never sway the camera. See 05_design_principles.md, "Camera Design".
            transform.position = new Vector3(
                0f,
                CameraConstants.k_CameraOffsetY,
                _target.position.z + CameraConstants.k_CameraOffsetZ
            );

            // Direct pitch instead of LookAt — angle is independent of camera position,
            // so tuning Y/Z offsets never accidentally changes the viewing angle.
            transform.rotation = Quaternion.Euler(CameraConstants.k_CameraPitchAngle, 0f, 0f);
        }

        private void OnCharacterSpawned(ICharacter character) => _target = character.CharacterTransform;

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_target == character.CharacterTransform) _target = null;
        }
    }
}
