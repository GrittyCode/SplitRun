using UnityEngine;

using DG.Tweening;
using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    // Resolves ICharacter via GetComponent — works for both ServerCharacter and LocalCharacter.
    // Owns no game logic and makes no network calls.
    public class CharacterVisuals : MonoBehaviour
    {
        private ICharacter _character;
        private Tween      _laneTween;
        private Tween      _verticalTween;

        private void Start()
        {
            _character = GetComponent<ICharacter>();

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

            // TODO(hp): subscribe to HpReactive for damage flash
            // TODO(skill): subscribe to SkillStateReactive for skill idle VFX
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
            float halfDuration = GameConstants.k_JumpDuration * 0.5f;
            _verticalTween = DOTween.Sequence()
                .Append(transform.DOLocalMoveY(GameConstants.k_JumpHeight, halfDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMoveY(0f, halfDuration).SetEase(Ease.InQuad));
        }

        private void AnimateSlide()
        {
            float inOut    = GameConstants.k_SlideDuration * 0.2f;
            float holdTime = GameConstants.k_SlideDuration * 0.6f;
            _verticalTween = DOTween.Sequence()
                .Append(transform.DOLocalMoveY(GameConstants.k_SlideYOffset, inOut).SetEase(Ease.OutQuad))
                .AppendInterval(holdTime)
                .Append(transform.DOLocalMoveY(0f, inOut).SetEase(Ease.InQuad));
        }

        private void SnapToGround()
        {
            // Safety fallback — snaps Y to 0 if server resets state before the animation completes.
            Vector3 pos = transform.localPosition;
            pos.y = 0f;
            transform.localPosition = pos;
        }

        private static float GetLaneX(int laneIndex) => laneIndex switch
        {
            GameConstants.k_LaneLeft  => GameConstants.k_LaneXLeft,
            GameConstants.k_LaneRight => GameConstants.k_LaneXRight,
            _                         => GameConstants.k_LaneXCenter,
        };
    }
}
