using UnityEngine;

using DG.Tweening;
using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    public class CharacterMovementDriver : MonoBehaviour
    {
        private ICharacter      _character;
        private CapsuleCollider _hitboxCollider;
        private Tween           _laneTween;
        private Tween           _verticalTween;

        private void Start()
        {
            _character      = GetComponent<ICharacter>();
            _hitboxCollider = GetComponentInChildren<CapsuleCollider>();

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
            pos.x = GameConstants.GetLaneX(_character.LaneReactive.CurrentValue);
            pos.z = _character.DistanceReactive.CurrentValue;
            transform.localPosition = pos;
        }

        private void SubscribeToStateChanges()
        {
            _character.LaneReactive
                .Skip(1)
                .Subscribe(AnimateLaneChange)
                .AddTo(this);

            _character.VerticalStateReactive
                .Skip(1)
                .Subscribe(OnVerticalStateChanged)
                .AddTo(this);

            _character.DistanceReactive
                .Subscribe(SetForwardPosition)
                .AddTo(this);
        }

        private void AnimateLaneChange(int lane)
        {
            _laneTween?.Kill();
            _laneTween = transform
                .DOLocalMoveX(GameConstants.GetLaneX(lane), CharacterConstants.k_LaneMoveDuration)
                .SetEase(Ease.OutQuad);
        }

        private void OnVerticalStateChanged(VerticalState state)
        {
            _verticalTween?.Kill();

            switch (state)
            {
                case VerticalState.Jumping:
                    AnimateJumpArc();
                    break;
                case VerticalState.Sliding:
                    ShrinkHitboxForSlide();
                    break;
                case VerticalState.Ground:
                    RestoreHitboxToStanding();
                    SnapToGround();
                    break;
            }
        }

        private void AnimateJumpArc()
        {
            float halfDuration = CharacterConstants.k_JumpDuration * 0.5f;
            _verticalTween = DOTween.Sequence()
                .Append(transform.DOLocalMoveY(CharacterConstants.k_JumpHeight, halfDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMoveY(0f, halfDuration).SetEase(Ease.InQuad));
        }

        private void SnapToGround()
        {
            Vector3 pos = transform.localPosition;
            pos.y = 0f;
            transform.localPosition = pos;
        }

        private void ShrinkHitboxForSlide() => ApplyHitbox(
            CharacterConstants.k_SlideColliderRadius,
            CharacterConstants.k_SlideColliderHeight,
            CharacterConstants.k_SlideColliderCenterY);

        private void RestoreHitboxToStanding() => ApplyHitbox(
            CharacterConstants.k_ColliderRadius,
            CharacterConstants.k_ColliderHeight,
            CharacterConstants.k_ColliderCenterY);

        private void ApplyHitbox(float radius, float height, float centerY)
        {
            if (!_hitboxCollider) return;

            _hitboxCollider.radius = radius;
            _hitboxCollider.height = height;

            Vector3 center = _hitboxCollider.center;
            center.y = centerY;
            _hitboxCollider.center = center;
        }

        private void SetForwardPosition(float distance)
        {
            Vector3 pos = transform.localPosition;
            pos.z = distance;
            transform.localPosition = pos;
        }
    }
}
