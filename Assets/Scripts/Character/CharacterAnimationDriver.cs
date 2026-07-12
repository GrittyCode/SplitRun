using System.Collections.Generic;

using UnityEngine;

using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    public class CharacterAnimationDriver : MonoBehaviour
    {
        private ICharacter    _character;
        private Animator      _animator;
        private AnimationClip _rollClip;
        private AnimationClip _jumpOutClip;

        private void Start()
        {
            _character = GetComponent<ICharacter>();

            // The Animator lives on the nested CharacterModel child, not the network root.
            _animator = GetComponentInChildren<Animator>();

            _rollClip    = ResolveOverrideClip(CharacterConstants.k_ClipNameRoll);
            _jumpOutClip = ResolveOverrideClip(CharacterConstants.k_ClipNameJumpOut);

            SubscribeToStateChanges();
        }

        private void SubscribeToStateChanges()
        {
            _character.RunningReactive
                .Subscribe(isRunning => _animator.SetBool(CharacterConstants.k_ParamRunning, isRunning))
                .AddTo(this);

            _character.VerticalStateReactive
                .Skip(1)
                .Subscribe(OnVerticalStateChanged)
                .AddTo(this);

            // ReactiveProperty skips re-emission of an unchanged value, so HP held at 0 never re-triggers Lose.
            _character.HpReactive
                .Skip(1)
                .Subscribe(OnHpChanged)
                .AddTo(this);
        }

        private void OnVerticalStateChanged(VerticalState state)
        {
            switch (state)
            {
                case VerticalState.Jumping:
                    _animator.SetTrigger(CharacterConstants.k_TriggerJump);
                    break;
                case VerticalState.Sliding:
                    ApplySpeedCompensation(_rollClip, CharacterConstants.k_SlideDuration);
                    _animator.SetTrigger(CharacterConstants.k_TriggerSlide);
                    break;
                case VerticalState.Ground:
                    ApplySpeedCompensation(_jumpOutClip, CharacterConstants.k_JumpLandRecoveryDuration);
                    _animator.SetTrigger(CharacterConstants.k_TriggerLand);
                    break;
            }
        }

        private void OnHpChanged(int hp) =>
            _animator.SetTrigger(hp <= 0 ? CharacterConstants.k_TriggerLose : CharacterConstants.k_TriggerHit);

        private AnimationClip ResolveOverrideClip(string clipName)
        {
            if (_animator.runtimeAnimatorController is not AnimatorOverrideController overrideController)
            {
                Debug.LogWarning($"[CharacterAnimationDriver] Animator is not using an AnimatorOverrideController — '{clipName}' speed compensation disabled.");
                return null;
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);

            foreach (KeyValuePair<AnimationClip, AnimationClip> pair in overrides)
            {
                if (pair.Key && pair.Key.name == clipName)
                    return pair.Value;
            }

            Debug.LogWarning($"[CharacterAnimationDriver] No override found for clip '{clipName}' — speed compensation disabled.");
            return null;
        }

        private void ApplySpeedCompensation(AnimationClip clip, float desiredDuration)
        {
            if (!clip) return;

            // The clip finishes exactly when desiredDuration expires, regardless of the skin's clip length.
            _animator.SetFloat(CharacterConstants.k_ParamSpeed, clip.length / desiredDuration);
        }
    }
}
