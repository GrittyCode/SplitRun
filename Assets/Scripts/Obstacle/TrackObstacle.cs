using UnityEngine;
using UnityEngine.Serialization;

using DG.Tweening;

using SplitRun.Constants;
using SplitRun.Utility;

namespace SplitRun.Obstacle
{
    public enum ObstacleType
    {
        Vertical  = 0,
        LaneJump  = 1,
        LaneSlide = 2,
        WideJump  = 3,
        WideSlide = 4,
    }

    public static class ObstacleTypeExtensions
    {
        /// <summary>Returns the stamped BoxCollider size and center Y. Every obstacle type is floor-based.</summary>
        public static (Vector3 size, float centerY) ToColliderBox(this ObstacleType type) => type switch
        {
            ObstacleType.Vertical => (
                new Vector3(ObstacleConstants.k_LaneWidth, ObstacleConstants.k_VerticalHeight, ObstacleConstants.k_Depth),
                ObstacleConstants.k_VerticalHeight * 0.5f),

            ObstacleType.LaneJump => (
                new Vector3(ObstacleConstants.k_LaneWidth, ObstacleConstants.k_JumpBarHeight, ObstacleConstants.k_Depth),
                ObstacleConstants.k_JumpBarHeight * 0.5f),

            ObstacleType.LaneSlide => (
                new Vector3(ObstacleConstants.k_LaneWidth, ObstacleConstants.k_SlideBarHeight, ObstacleConstants.k_Depth),
                ObstacleConstants.k_SlideClearanceHeight + ObstacleConstants.k_SlideBarHeight * 0.5f),

            ObstacleType.WideJump => (
                new Vector3(ObstacleConstants.k_WideWidth, ObstacleConstants.k_JumpBarHeight, ObstacleConstants.k_Depth),
                ObstacleConstants.k_JumpBarHeight * 0.5f),

            ObstacleType.WideSlide => (
                new Vector3(ObstacleConstants.k_WideWidth, ObstacleConstants.k_SlideBarHeight, ObstacleConstants.k_Depth),
                ObstacleConstants.k_SlideClearanceHeight + ObstacleConstants.k_SlideBarHeight * 0.5f),

            _ => (Vector3.one, 0.5f),
        };

        public static bool IsFullWidth(this ObstacleType type) =>
            type == ObstacleType.WideJump || type == ObstacleType.WideSlide;
    }

    [RequireComponent(typeof(BoxCollider))]
    public class TrackObstacle : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_footprint")] private ObstacleType _type;

        private BoxCollider _collider;
        private Vector3     _initialScale;
        private Quaternion  _initialRotation;
        private Tween       _impactTween;

        public ObstacleType Type => _type;

        private void Awake()
        {
            _collider        = GetComponent<BoxCollider>();
            _initialScale    = transform.localScale;
            _initialRotation = transform.localRotation;
        }

        private void OnDestroy() => _impactTween?.Kill();

        // Collider disabled first so an overlapping trigger in the same physics step can't re-fire Impacted().
        public void Impacted()
        {
            _impactTween?.Kill();

            _collider.enabled = false;

            Vector3 flyOffset = new Vector3(0f, ObstacleConstants.k_ImpactFlyUp, ObstacleConstants.k_ImpactFlyForward);
            Vector3 spin      = new Vector3(ObstacleConstants.k_ImpactSpinDegrees, ObstacleConstants.k_ImpactSpinDegrees * 0.5f, 0f);

            _impactTween = DOTween.Sequence()
                .Join(transform.DOBlendableMoveBy(flyOffset, ObstacleConstants.k_ImpactFlyDuration).SetEase(Ease.OutQuad))
                .Join(transform.DOLocalRotate(spin, ObstacleConstants.k_ImpactFlyDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
                .Join(transform.DOScale(Vector3.zero, ObstacleConstants.k_ImpactFlyDuration).SetEase(Ease.InQuad))
                .OnComplete(() => gameObject.SetActive(false));
        }

        // Restores rotation blown away on a previous use — the spawner resets position only.
        public void ResetState()
        {
            _impactTween?.Kill();

            transform.localScale    = _initialScale;
            transform.localRotation = _initialRotation;
            _collider.enabled       = true;

            gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        private void Reset() => LayerGuard.Enforce(gameObject, ObstacleConstants.k_LayerName, nameof(TrackObstacle));

        private void OnValidate()
        {
            LayerGuard.Enforce(gameObject, ObstacleConstants.k_LayerName, nameof(TrackObstacle));

            if (!TryGetComponent(out BoxCollider box)) return;

            (Vector3 size, float centerY) = _type.ToColliderBox();

            box.size      = size;
            box.center    = new Vector3(0f, centerY, 0f);
            box.isTrigger = true;
        }
#endif
    }
}
