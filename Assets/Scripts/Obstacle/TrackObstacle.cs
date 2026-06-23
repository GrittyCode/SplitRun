using UnityEngine;

using DG.Tweening;

using SplitRun.Constants;

namespace SplitRun.Obstacle
{
    public class TrackObstacle : MonoBehaviour
    {
        [SerializeField] private ObstaclePlacement _placement;
        [SerializeField] private ObstacleAnchor    _anchor;

        private Collider[] _obstacleColliders;
        private Vector3    _initialScale;
        private Tween      _impactTween;

        public ObstaclePlacement Placement    => _placement;
        public ObstacleAnchor    Anchor       => _anchor;

        private void Awake()
        {
            // GetComponentsInChildren: the composite coop prefab keeps its BoxColliders on
            // child cubes (one MeshRenderer per child), all driven by this single root
            // TrackObstacle. Single obstacles keep their collider on the root, which this
            // also returns.
            _obstacleColliders = GetComponentsInChildren<Collider>(true);
            _initialScale      = transform.localScale;
        }

        private void OnDestroy() => _impactTween?.Kill();

        // Colliders disabled before the tween starts so an overlapping trigger in the
        // same physics step can never fire Impacted() a second time.
        // TODO(obstacle): per-collider Impacted() if coop obstacles should shatter one wall at a time
        public void Impacted()
        {
            _impactTween?.Kill();

            SetCollidersEnabled(false);

            _impactTween = transform
                .DOScale(Vector3.zero, ObstacleConstants.k_ImpactDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }

        // Called by ObstaclePool.Rent() on pool reuse — restores an obstacle that was
        // blown away on a previous use of this pooled instance.
        public void ResetState()
        {
            _impactTween?.Kill();

            transform.localScale = _initialScale;

            SetCollidersEnabled(true);

            gameObject.SetActive(true);
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            foreach (Collider obstacleCollider in _obstacleColliders)
                obstacleCollider.enabled = isEnabled;
        }
    }
}
