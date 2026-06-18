using System.Collections.Generic;

using UnityEngine;

using DG.Tweening;
using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    // Resolves ICharacter via GetComponent — works for both ServerCharacter and LocalCharacter.
    // Owns no game logic and makes no network calls. Drives the Animator (trigger dispatch +
    // per-skin clip-speed compensation) and the DOTween position tweens in response to
    // ICharacter's reactive state — the two concerns that previously lived in CharacterVisuals.
    public class CharacterAnimationDriver : MonoBehaviour
    {
        private ICharacter    _character;
        private Animator      _animator;
        private AnimationClip _rollClip;
        private AnimationClip _jumpOutClip;
        private Tween         _laneTween;
        private Tween         _verticalTween;

        private void Start()
        {
            _character   = GetComponent<ICharacter>();
            _animator    = GetComponent<Animator>();
            _rollClip    = ResolveOverrideClip(AnimatorConstants.k_ClipNameRoll);
            _jumpOutClip = ResolveOverrideClip(AnimatorConstants.k_ClipNameJumpOut);

            SetInitialPosition();
            SubscribeToStateChanges();
        }

        private void OnDestroy()
        {
            _laneTween?.Kill();
            _verticalTween?.Kill();
        }

        private void SetInitialPosition()
        {
            Vector3 pos = transform.localPosition;
            pos.x = GetLaneX(_character.LaneReactive.CurrentValue);
            transform.localPosition = pos;
        }

        private void SubscribeToStateChanges()
        {
            // Skip(1) avoids animating the value emitted on subscription.
            // SetInitialPosition() already places the character correctly at spawn.
            _character.LaneReactive
                .Skip(1)
                .Subscribe(lane => AnimateLaneChange(lane))
                .AddTo(this);

            _character.VerticalStateReactive
                .Skip(1)
                .Subscribe(state => AnimateVerticalState(state))
                .AddTo(this);

            // ReactiveProperty skips re-emission when the value is unchanged, so HP
            // staying at 0 across multiple writes never re-triggers Lose on its own.
            _character.HpReactive
                .Skip(1)
                .Subscribe(hp => OnHpChanged(hp))
                .AddTo(this);

            // TODO(skill): subscribe to SkillStateReactive for skill idle VFX
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
                if (pair.Key != null && pair.Key.name == clipName)
                    return pair.Value;
            }

            Debug.LogWarning($"[CharacterAnimationDriver] No override found for clip '{clipName}' — speed compensation disabled.");
            return null;
        }

        private void AnimateLaneChange(int lane)
        {
            _laneTween?.Kill();
            _laneTween = transform
                .DOLocalMoveX(GetLaneX(lane), GameConstants.k_LaneMoveDuration)
                .SetEase(Ease.OutQuad);
        }

        private void AnimateVerticalState(VerticalState state)
        {
            _verticalTween?.Kill();

            switch (state)
            {
                case VerticalState.Jumping:
                    AnimateJump();
                    break;
                case VerticalState.Sliding:
                    AnimateSlide();
                    break;
                case VerticalState.Ground:
                    SnapToGround();
                    break;
            }
        }

        private void AnimateJump()
        {
            _animator.SetTrigger(AnimatorConstants.k_TriggerJump);

            float halfDuration = GameConstants.k_JumpDuration * 0.5f;
            _verticalTween = DOTween.Sequence()
                .Append(transform.DOLocalMoveY(GameConstants.k_JumpHeight, halfDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMoveY(0f, halfDuration).SetEase(Ease.InQuad));
        }

        private void AnimateSlide()
        {
            ApplySpeedCompensation(_rollClip, GameConstants.k_SlideDuration);
            _animator.SetTrigger(AnimatorConstants.k_TriggerSlide);
        }

        private void SnapToGround()
        {
            // Compensated so Jump_Out always finishes in k_JumpLandRecoveryDuration regardless
            // of skin, matching the Jump_Out → Run transition's Has Exit Time = 1 in the Editor.
            ApplySpeedCompensation(_jumpOutClip, GameConstants.k_JumpLandRecoveryDuration);
            _animator.SetTrigger(AnimatorConstants.k_TriggerLand);

            // Safety fallback — snaps Y to 0 if server resets state before the animation completes.
            Vector3 pos = transform.localPosition;
            pos.y = 0f;
            transform.localPosition = pos;
        }

        private void OnHpChanged(int hp)
        {
            _animator.SetTrigger(hp <= 0 ? AnimatorConstants.k_TriggerLose : AnimatorConstants.k_TriggerHit);
        }

        private void ApplySpeedCompensation(AnimationClip clip, float desiredDuration)
        {
            if (clip == null) return;

            // Speed = clip length / desired duration — the clip finishes exactly when desiredDuration expires.
            float speed = clip.length / desiredDuration;
            _animator.SetFloat(AnimatorConstants.k_ParamSpeed, speed);
        }

        private static float GetLaneX(int laneIndex) => laneIndex switch
        {
            GameConstants.k_LaneLeft  => GameConstants.k_LaneXLeft,
            GameConstants.k_LaneRight => GameConstants.k_LaneXRight,
            _                         => GameConstants.k_LaneXCenter,
        };
    }
}
