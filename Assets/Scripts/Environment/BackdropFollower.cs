using UnityEngine;

using SplitRun.Character;

namespace SplitRun.Environment
{
    public class BackdropFollower : MonoBehaviour
    {
        [Tooltip("Forward gap from the character to the silhouette group. Far beyond the ground " +
                 "fill distance so it reads as background, not approaching geometry.")]
        [SerializeField] private float _forwardOffsetZ = 125f;

        [Tooltip("Runtime vertical position of the silhouette group. Drag this against the live " +
                 "game view until the silhouette sits on the horizon — the Scene gizmo slides the " +
                 "camera frame as you change it.")]
        [SerializeField] private float _baselineY = -33f;

        private Transform _target;

        public float ForwardOffsetZ => _forwardOffsetZ;
        public float BaselineY      => _baselineY;

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

            transform.position = new Vector3(0f, _baselineY, _target.position.z + _forwardOffsetZ);
        }

        private void OnCharacterSpawned(ICharacter character) => _target = character.CharacterTransform;

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_target == character.CharacterTransform) _target = null;
        }
    }
}
