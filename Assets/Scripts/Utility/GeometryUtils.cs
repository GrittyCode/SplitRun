using UnityEngine;

namespace SplitRun.Utility
{
    public static class GeometryUtils
    {
        private const float k_IdentityTolerance = 0.0001f;

        /// <summary>World-space bounds enclosing every renderer under the root. False when the root has none.</summary>
        public static bool TryGetHierarchyBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (!root) return false;

            return TryGetBounds(root.GetComponentsInChildren<Renderer>(), out bounds);
        }

        /// <summary>World-space bounds enclosing every renderer in the array. False when the array is empty.</summary>
        public static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            if (renderers == null || renderers.Length == 0) return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

        /// <summary>True when local position, rotation, and scale are all identity.</summary>
        public static bool IsIdentity(Transform target)
        {
            bool isPosIdentity   = target.localPosition.sqrMagnitude < k_IdentityTolerance;
            bool isScaleIdentity = (target.localScale - Vector3.one).sqrMagnitude < k_IdentityTolerance;

            return isPosIdentity && isScaleIdentity && IsRotationIdentity(target);
        }

        public static bool IsRotationIdentity(Transform target) =>
            Quaternion.Angle(target.localRotation, Quaternion.identity) < k_IdentityTolerance;
    }
}
