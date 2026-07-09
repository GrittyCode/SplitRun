using UnityEditor;
using UnityEngine;

using SplitRun.Constants;
using SplitRun.Environment;
using SplitRun.Utility;

namespace SplitRun.EditorTools
{
    [CustomEditor(typeof(TrackSegment))]
    public class TrackSegmentEditor : Editor
    {
        private const float k_CorridorGuideHeight = 3f;
        private const float k_OverhangTolerance   = 0.001f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var segment = (TrackSegment)target;

            if (!segment.Floor)
            {
                Show("Assign the Floor child — it defines the tile length.", MessageType.Error);
                return;
            }

            if (!segment.TryGetFloorMetrics(out _, out _))
                Show("Floor has no mesh yet.", MessageType.Info);

            if (!GeometryUtils.IsIdentity(segment.transform))
                Show("Root must be identity (Pos 0 / Rot 0 / Scale 1). Move offsets to children.", MessageType.Error);

            if (HasDecorationOverhang(segment))
                Show("Decoration overhangs the floor tile (overlaps the next tile).", MessageType.Warning);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        private static void DrawGuides(TrackSegment segment, GizmoType gizmoType)
        {
            if (!GeometryUtils.TryGetHierarchyBounds(segment.Floor, out Bounds floor)) return;

            DrawFloorExtentGuide(segment, floor);
            DrawPlayCorridorGuide(segment, floor);
        }

        // Advisory only: overhang no longer opens seams but overlaps the neighbouring tile's decoration.
        private static bool HasDecorationOverhang(TrackSegment segment)
        {
            if (!GeometryUtils.TryGetHierarchyBounds(segment.Floor, out Bounds floor)) return false;

            foreach (Renderer renderer in segment.GetComponentsInChildren<Renderer>())
            {
                if (renderer.transform.IsChildOf(segment.Floor)) continue;

                Bounds b = renderer.bounds;
                if (b.min.z < floor.min.z - k_OverhangTolerance || b.max.z > floor.max.z + k_OverhangTolerance)
                    return true;
            }

            return false;
        }

        private static void DrawFloorExtentGuide(TrackSegment segment, Bounds floor)
        {
            Vector3 center = new Vector3(segment.transform.position.x, floor.center.y, floor.center.z);
            Vector3 size   = new Vector3(ObstacleConstants.k_WideWidth, 0.05f, floor.size.z);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, size);
            Handles.color = Color.cyan;
            Handles.Label(center, "  Floor tile (Z extent)");
        }

        private static void DrawPlayCorridorGuide(TrackSegment segment, Bounds floor)
        {
            float centerY  = floor.min.y + k_CorridorGuideHeight * 0.5f;
            Vector3 center = new Vector3(segment.transform.position.x, centerY, floor.center.z);
            Vector3 size   = new Vector3(ObstacleConstants.k_WideWidth, k_CorridorGuideHeight, floor.size.z);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
            Handles.color = Color.yellow;
            Handles.Label(center + Vector3.up * (k_CorridorGuideHeight * 0.5f), "  Play corridor — keep walls/props outside");
        }

        private static void Show(string message, MessageType type)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(message, type);
        }
    }
}
