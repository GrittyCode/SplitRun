using System;

using UnityEngine;

using R3;

using SplitRun.Character;
using SplitRun.Constants;

namespace SplitRun.Game
{
    // Attached directly to the scene's Main Camera. Not VContainer-registered — subscribes to
    // CharacterEvents directly since the target doesn't exist at scene load time.
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        private float       _trackedDistance;
        private IDisposable _distanceSubscription;

        private void Awake()
        {
            GetComponent<Camera>().fieldOfView = CameraConstants.k_CameraFov;
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

            _distanceSubscription?.Dispose();
            _distanceSubscription = null;
        }

        private void LateUpdate()
        {
            // X and Y fixed — lane changes and jumps never sway the camera.
            transform.position = new Vector3(
                0f,
                CameraConstants.k_CameraOffsetY,
                _trackedDistance + CameraConstants.k_CameraOffsetZ
            );

            // Direct pitch instead of LookAt — tuning offsets never changes the viewing angle.
            transform.rotation = Quaternion.Euler(CameraConstants.k_CameraPitchAngle, 0f, 0f);
        }

        // Disposed per despawn — AddTo(this) would stack one live subscription per respawn.
        private void OnCharacterSpawned(ICharacter character)
        {
            _distanceSubscription?.Dispose();

            // Track authoritative distance instead of Transform.position.z so visual-only
            // knockback offset on the character never pulls the camera backward.
            _distanceSubscription = character.DistanceReactive
                .Subscribe(distance => _trackedDistance = distance);
        }

        private void OnCharacterDespawned(ICharacter character)
        {
            _distanceSubscription?.Dispose();
            _distanceSubscription = null;
            _trackedDistance      = 0f;
        }
    }
}
