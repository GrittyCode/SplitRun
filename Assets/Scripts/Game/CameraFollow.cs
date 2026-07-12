using System;

using UnityEngine;

using R3;

using SplitRun.Character;

namespace SplitRun.Game
{
    // Not VContainer-registered: the target does not exist at scene load time.
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public const float k_OffsetY = 6.0f;
        public const float k_OffsetZ = -3.5f;

        // Lower = more horizontal = stronger perspective convergence. Higher = flatter view.
        public const float k_PitchAngle = 30f;

        // Narrower FOV keeps ceiling (slide) and floor (jump) bars vertically separated.
        public const float k_Fov = 80f;

        private float       _trackedDistance;
        private IDisposable _distanceSubscription;

        private void Awake()
        {
            GetComponent<Camera>().fieldOfView = k_Fov;

            // Direct pitch instead of LookAt — tuning offsets never changes the viewing angle.
            transform.rotation = Quaternion.Euler(k_PitchAngle, 0f, 0f);
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
            transform.position = new Vector3(0f, k_OffsetY, _trackedDistance + k_OffsetZ);
        }

        // Disposed per despawn — AddTo(this) would stack one live subscription per respawn.
        private void OnCharacterSpawned(ICharacter character)
        {
            _distanceSubscription?.Dispose();

            // Authoritative distance, not Transform.position.z — knockback must not pull the camera back.
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
