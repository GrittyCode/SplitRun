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
                .Subscribe(lane => AnimateLaneChange(lane))
                .AddTo(this);

            _character.VerticalStateReactive
                .Skip(1)
                .Subscribe(state => OnVerticalStateChanged(state))
                .AddTo(this);

            _character.DistanceReactive
                .Subscribe(distance => SetForwardPosition(distance))
                .AddTo(this);
        }

        private void AnimateLaneChange(int lane)
        {
            _laneTween?.Kill();
            _laneTween = transform
                .DOLocalMoveX(GameConstants.GetLaneX(lane), GameConstants.k_LaneMoveDuration)
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
            float halfDuration = GameConstants.k_JumpDuration * 0.5f;
            _verticalTween = DOTween.Sequence()
                .Append(transform.DOLocalMoveY(GameConstants.k_JumpHeight, halfDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMoveY(0f, halfDuration).SetEase(Ease.InQuad));
        }

        private void SnapToGround()
        {
            Vector3 pos = transform.localPosition;
            pos.y = 0f;
            transform.localPosition = pos;
        }

        private void ShrinkHitboxForSlide()
        {
            if (_hitboxCollider == null) return;

            _hitboxCollider.radius = CharacterConstants.k_SlideColliderRadius;
            _hitboxCollider.height = CharacterConstants.k_SlideColliderHeight;

            Vector3 center = _hitboxCollider.center;
            center.y = CharacterConstants.k_SlideColliderCenterY;
            _hitboxCollider.center = center;
        }

        private void RestoreHitboxToStanding()
        {
            if (_hitboxCollider == null) return;

            _hitboxCollider.radius = CharacterConstants.k_ColliderRadius;
            _hitboxCollider.height = CharacterConstants.k_ColliderHeight;

            Vector3 center = _hitboxCollider.center;
            center.y = CharacterConstants.k_ColliderCenterY;
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
