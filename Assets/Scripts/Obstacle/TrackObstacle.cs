using UnityEngine;

using DG.Tweening;

using SplitRun.Constants;

namespace SplitRun.Obstacle
{
    // Single key for an obstacle's X placement (lane vs full-width) and its stamped BoxCollider.
    // All obstacles are floor-based; slide variants raise their collider center to head height.
    public enum ObstacleFootprint
    {
        Vertical,
        LaneJump,
        LaneSlide,
        WideJump,
        WideSlide,
    }

    [RequireComponent(typeof(BoxCollider))]
    public class TrackObstacle : MonoBehaviour
    {
        [SerializeField] private ObstacleFootprint _footprint;

        private BoxCollider _collider;
        private Vector3     _initialScale;
        private Quaternion  _initialRotation;
        private Tween       _impactTween;

        public ObstacleFootprint Footprint => _footprint;

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
        // The footprint stamps the hitbox; the layer is enforced so a hand-edited layer cannot
        // silently break trigger collisions. Gizmos and auto-floor live in TrackObstacleEditor.
        private void Reset()   => EnforceObstacleLayer();
        private void OnValidate()
        {
            EnforceObstacleLayer();

            if (!TryGetComponent(out BoxCollider box)) return;

            (Vector3 size, float centerY) = ObstacleConstants.GetFootprintBox(_footprint);

            box.size      = size;
            box.center    = new Vector3(0f, centerY, 0f);
            box.isTrigger = true;
        }

        private void EnforceObstacleLayer()
        {
            int layer = LayerMask.NameToLayer(ObstacleConstants.k_LayerName);
            if (layer < 0)
            {
                Debug.LogWarning(
                    $"[TrackObstacle] Layer '{ObstacleConstants.k_LayerName}' does not exist. " +
                    "Add it in Project Settings -> Tags and Layers, then enable Character x " +
                    $"{ObstacleConstants.k_LayerName} in the Physics collision matrix.", this);
                return;
            }

            if (gameObject.layer != layer)
                gameObject.layer = layer;
        }
#endif
    }
}
