using UnityEngine;

using SplitRun.Utility;

namespace SplitRun.Environment
{
    public class TrackSegment : MonoBehaviour
    {
        [SerializeField] private Transform _floor;

        public Transform Floor => _floor;

        // Floor-only so overhanging decoration never lengthens the recycle step and opens seams.
        public bool TryGetFloorMetrics(out float lengthZ, out float minZ)
        {
            lengthZ = 0f;
            minZ    = 0f;

            if (!GeometryUtils.TryGetHierarchyBounds(_floor, out Bounds bounds)) return false;

            lengthZ = bounds.size.z;
            minZ    = bounds.min.z;
            return lengthZ > 0f;
        }
    }
}
