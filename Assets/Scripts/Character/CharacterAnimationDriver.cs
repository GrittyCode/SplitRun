using System.Collections.Generic;

using UnityEngine;

using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    // Drives the Animator only — trigger dispatch and per-skin clip-speed compensation.
    // Owns no Transform, physics, or tween state. See CharacterMovementDriver for those.
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

            _rollClip    = ResolveOverrideClip(AnimatorConstants.k_ClipNameRoll);
            _jumpOutClip = ResolveOverrideClip(AnimatorConstants.k_ClipNameJumpOut);

            SubscribeToStateChanges();
        }

        private void SubscribeToStateChanges()
        {
            _character.VerticalStateReactive
                .Skip(1)
                .Subscribe(state => OnVerticalStateChanged(state))
                .AddTo(this);

            // ReactiveProperty skips re-emission when the value is unchanged, so HP
            // staying at 0 across multiple writes never re-triggers Lose on its own.
            _character.HpReactive
                .Skip(1)
                .Subscribe(hp => OnHpChanged(hp))
                .AddTo(this);

            // TODO(skill): subscribe to SkillStateReactive for skill idle VFX
        }

        private void OnVerticalStateChanged(VerticalState state)
        {
            switch (state)
            {
                case VerticalState.Jumping:
                    _animator.SetTrigger(AnimatorConstants.k_TriggerJump);
                    break;
                case VerticalState.Sliding:
                    ApplySpeedCompensation(_rollClip, GameConstants.k_SlideDuration);
                    _animator.SetTrigger(AnimatorConstants.k_TriggerSlide);
                    break;
                case VerticalState.Ground:
                    // Compensated so Jump_Out always finishes in k_JumpLandRecoveryDuration
                    // regardless of skin, matching Jump_Out → Run Has Exit Time = 1 in the Editor.
                    ApplySpeedCompensation(_jumpOutClip, GameConstants.k_JumpLandRecoveryDuration);
                    _animator.SetTrigger(AnimatorConstants.k_TriggerLand);
                    break;
            }
        }

        private void OnHpChanged(int hp)
        {
            _animator.SetTrigger(hp <= 0 ? AnimatorConstants.k_TriggerLose : AnimatorConstants.k_TriggerHit);
        }

        // Looks up the actual per-skin clip from the AnimatorOverrideController instead of
        // holding a direct reference — AOC_Goblin (or a future AOC_<AssetName>) stays the
        // single source of truth for which clip plays, per 06_folder_structure.md, "Animators/".
        private AnimationClip ResolveOverrideClip(string clipName)
        {
            if (_animator.runtimeAnimatorController is not AnimatorOverrideController overrideController)
            {
                Debug.LogWarning($"[CharacterAnimationDriver] Animator is not using an AnimatorOverrideController — '{clipName}' speed compensation disabled.");
                return null;
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);

            foreach (var pair in overrides)
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

            // Speed = clip length / desired duration — the clip finishes exactly when desiredDuration expires.
            float speed = clip.length / desiredDuration;
            _animator.SetFloat(AnimatorConstants.k_ParamSpeed, speed);
        }
    }
}
