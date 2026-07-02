using UnityEngine;

using DG.Tweening;

using SplitRun.Constants;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SplitRun.Obstacle
{
    // ExecuteAlways only for the editor-time auto-floor; all of it is guarded out of play mode.
    [ExecuteAlways]
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

        // Warn-only: auto-resetting would clobber the author's in-progress edit.
        public bool IsRootTransformValid()
        {
            const float k_Tolerance = 0.0001f;

            bool isPosIdentity   = transform.localPosition.sqrMagnitude < k_Tolerance;
            bool isScaleIdentity = (transform.localScale - Vector3.one).sqrMagnitude < k_Tolerance;
            bool isRotIdentity   = Quaternion.Angle(transform.localRotation, Quaternion.identity) < k_Tolerance;

            return isPosIdentity && isScaleIdentity && isRotIdentity;
        }

#if UNITY_EDITOR
        private const float k_AlignTolerance = 0.001f;

        // Re-floors the model every editor tick so resizing the child Model snaps it back to the
        // ground automatically — no manual button. Play mode and runtime pooled instances bail
        // immediately so this is purely an authoring aid.
        private void Update()
        {
            if (Application.isPlaying) return;

            AutoFloorModel();
        }

        // Sets the obstacle layer once when the component is first added, so authoring a new
        // prefab doesn't require remembering the layer by hand.
        private void Reset()
        {
            EnforceObstacleLayer();
        }

        // The footprint is the single source of truth for the hitbox: it stamps the root
        // BoxCollider's size and center, the obstacle layer is enforced so a hand-edited layer
        // can't silently break trigger collisions, and a non-identity root is warned about.
        private void OnValidate()
        {
            EnforceObstacleLayer();
            WarnIfRootNotIdentity();

            if (!TryGetComponent(out BoxCollider box)) return;

            (Vector3 size, float centerY) = GetFootprintBox(_footprint);

            box.size      = size;
            box.center    = new Vector3(0f, centerY, 0f);
            box.isTrigger = true;
        }

        private void WarnIfRootNotIdentity()
        {
            if (IsRootTransformValid()) return;

            Debug.LogWarning(
                $"[TrackObstacle] '{name}' root Transform is not identity " +
                $"(Position {transform.localPosition}, Rotation {transform.localEulerAngles}, " +
                $"Scale {transform.localScale}). Keep the root at Position (0,0,0) / Rotation " +
                "(0,0,0) / Scale (1,1,1) and move all visual offset/scale/rotation to the child " +
                "Model.", this);
        }

        private void EnforceObstacleLayer()
        {
            int layer = LayerMask.NameToLayer(GameConstants.k_ObstacleLayerName);
            if (layer < 0)
            {
                Debug.LogWarning(
                    $"[TrackObstacle] Layer '{GameConstants.k_ObstacleLayerName}' does not exist. " +
                    "Add it in Project Settings → Tags and Layers, then enable Character × " +
                    $"{GameConstants.k_ObstacleLayerName} in the Physics collision matrix.", this);
                return;
            }

            if (gameObject.layer != layer)
                gameObject.layer = layer;
        }

        // Drops the first child (the visual Model) so its base rests on Y=0. A prefab has exactly
        // one model child; if more exist, the first is the alignment reference by design.
        private void AutoFloorModel()
        {
            if (!TryGetModelBounds(out Bounds bounds, out Transform model)) return;

            float deltaY = 0f - bounds.min.y;
            if (Mathf.Abs(deltaY) < k_AlignTolerance) return;

            model.position += new Vector3(0f, deltaY, 0f);
            EditorUtility.SetDirty(model);
        }

        private bool TryGetModelBounds(out Bounds bounds, out Transform model)
        {
            bounds = default;
            model  = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (!model) return false;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

        // Scene guide, drawn whenever this obstacle or any of its children is selected — so it
        // stays visible while the author resizes the child Model. Labels make each box readable:
        //   green  — floor plane (Y=0)
        //   cyan   — stamped collider (the hitbox), labelled with the footprint
        //   yellow — current model bounds (kept floored automatically)
        private void OnDrawGizmos()
        {
            if (!IsThisHierarchySelected()) return;

            DrawFloorGuide();
            DrawColliderGuide();
            DrawModelGuide();
        }

        private bool IsThisHierarchySelected()
        {
            GameObject active = Selection.activeGameObject;
            if (active == null) return false;

            return active == gameObject || active.transform.IsChildOf(transform);
        }

        private void DrawFloorGuide()
        {
            float halfWidth = ObstacleConstants.k_WideWidth * 0.5f;
            Vector3 left  = transform.position + Vector3.left  * halfWidth;
            Vector3 right = transform.position + Vector3.right * halfWidth;

            Gizmos.color  = Color.green;
            Gizmos.DrawLine(left, right);
            Handles.color = Color.green;
            Handles.Label(right, "  Floor (Y=0)");
        }

        private void DrawColliderGuide()
        {
            if (!TryGetComponent(out BoxCollider box)) return;

            Vector3 center = transform.position + box.center;

            Gizmos.color  = Color.cyan;
            Gizmos.DrawWireCube(center, box.size);
            Handles.color = Color.cyan;
            Handles.Label(center + Vector3.up * (box.size.y * 0.5f), $"  Hitbox: {_footprint}");
        }

        private void DrawModelGuide()
        {
            if (!TryGetModelBounds(out Bounds bounds, out _)) return;

            Gizmos.color  = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Handles.color = Color.yellow;
            Handles.Label(bounds.center + Vector3.up * (bounds.extents.y + 0.1f), "  Model (auto-floored)");
        }

        // All footprints are floor-based. The slide bars use a positive center offset (head height)
        private static (Vector3 size, float centerY) GetFootprintBox(ObstacleFootprint footprint)
        {
            float slideBase = GameConstants.k_SlideClearanceHeight;

            return footprint switch
            {
                ObstacleFootprint.Vertical => (
                    new Vector3(ObstacleConstants.k_LaneWidth, ObstacleConstants.k_VerticalHeight, ObstacleConstants.k_Depth),
                    ObstacleConstants.k_VerticalHeight * 0.5f),

                ObstacleFootprint.LaneJump => (
                    new Vector3(ObstacleConstants.k_LaneWidth, ObstacleConstants.k_JumpBarHeight, ObstacleConstants.k_Depth),
                    ObstacleConstants.k_JumpBarHeight * 0.5f),

                ObstacleFootprint.LaneSlide => (
                    new Vector3(ObstacleConstants.k_LaneWidth, ObstacleConstants.k_SlideBarHeight, ObstacleConstants.k_Depth),
                    slideBase + ObstacleConstants.k_SlideBarHeight * 0.5f),

                ObstacleFootprint.WideJump => (
                    new Vector3(ObstacleConstants.k_WideWidth, ObstacleConstants.k_JumpBarHeight, ObstacleConstants.k_Depth),
                    ObstacleConstants.k_JumpBarHeight * 0.5f),

                ObstacleFootprint.WideSlide => (
                    new Vector3(ObstacleConstants.k_WideWidth, ObstacleConstants.k_SlideBarHeight, ObstacleConstants.k_Depth),
                    slideBase + ObstacleConstants.k_SlideBarHeight * 0.5f),

                _ => (Vector3.one, 0.5f),
            };
        }
#endif
    }
}
