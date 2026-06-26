using UnityEngine;

using SplitRun.Constants;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SplitRun.Environment
{
    // The Floor child is the tiling unit TrackScroller measures, so decoration may overhang it freely.
    [ExecuteAlways]
    public class TrackSegment : MonoBehaviour
    {
        [SerializeField] private Transform _floor;

        public Transform Floor => _floor;

        // Floor-only so overhanging decoration never lengthens the recycle step and opens seams.
        public bool TryGetFloorMetrics(out float lengthZ, out float minZ)
        {
            lengthZ = 0f;
            minZ    = 0f;

            if (!TryGetFloorWorldBounds(out Bounds bounds)) return false;

            lengthZ = bounds.size.z;
            minZ    = bounds.min.z;
            return lengthZ > 0f;
        }

        // A non-identity root offsets every tiled copy, since TrackScroller positions the root directly.
        public bool IsRootTransformValid()
        {
            const float k_Tolerance = 0.0001f;

            bool isPosIdentity   = transform.localPosition.sqrMagnitude < k_Tolerance;
            bool isScaleIdentity = (transform.localScale - Vector3.one).sqrMagnitude < k_Tolerance;
            bool isRotIdentity   = Quaternion.Angle(transform.localRotation, Quaternion.identity) < k_Tolerance;

            return isPosIdentity && isScaleIdentity && isRotIdentity;
        }

        private bool TryGetFloorWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (!_floor) return false;

            Renderer[] renderers = _floor.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

#if UNITY_EDITOR
        private const float k_CorridorGuideHeight = 3f;
        private const float k_OverhangTolerance   = 0.001f;

        // Advisory only: overhang no longer opens seams but overlaps the neighbouring tile's decoration.
        public bool HasDecorationOverhang()
        {
            if (!TryGetFloorWorldBounds(out Bounds floor)) return false;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer.transform.IsChildOf(_floor)) continue;

                Bounds b = renderer.bounds;
                if (b.min.z < floor.min.z - k_OverhangTolerance || b.max.z > floor.max.z + k_OverhangTolerance)
                    return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (!IsThisHierarchySelected()) return;
            if (!TryGetFloorWorldBounds(out Bounds floor)) return;

            DrawFloorExtentGuide(floor);
            DrawPlayCorridorGuide(floor);
        }

        private bool IsThisHierarchySelected()
        {
            GameObject active = Selection.activeGameObject;
            if (!active) return false;

            return active == gameObject || active.transform.IsChildOf(transform);
        }

        private void DrawFloorExtentGuide(Bounds floor)
        {
            Vector3 center = new Vector3(transform.position.x, floor.center.y, floor.center.z);
            Vector3 size   = new Vector3(ObstacleConstants.k_WideWidth, 0.05f, floor.size.z);

            Gizmos.color  = Color.cyan;
            Gizmos.DrawWireCube(center, size);
            Handles.color = Color.cyan;
            Handles.Label(center, "  Floor tile (Z extent)");
        }

        private void DrawPlayCorridorGuide(Bounds floor)
        {
            float centerY  = floor.min.y + k_CorridorGuideHeight * 0.5f;
            Vector3 center = new Vector3(transform.position.x, centerY, floor.center.z);
            Vector3 size   = new Vector3(ObstacleConstants.k_WideWidth, k_CorridorGuideHeight, floor.size.z);

            Gizmos.color  = Color.yellow;
            Gizmos.DrawWireCube(center, size);
            Handles.color = Color.yellow;
            Handles.Label(center + Vector3.up * (k_CorridorGuideHeight * 0.5f), "  Play corridor — keep walls/props outside");
        }
#endif
    }
}
