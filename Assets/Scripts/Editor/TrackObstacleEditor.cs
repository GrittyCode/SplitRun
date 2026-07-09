using UnityEditor;
using UnityEngine;

using SplitRun.Constants;
using SplitRun.Obstacle;
using SplitRun.Utility;

namespace SplitRun.EditorTools
{
    [CustomEditor(typeof(TrackObstacle))]
    public class TrackObstacleEditor : Editor
    {
        private const float k_AlignTolerance = 0.001f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var obstacle = (TrackObstacle)target;

            AutoFloorModel(obstacle);

            if (GeometryUtils.IsIdentity(obstacle.transform)) return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Root Transform is not identity. Move all visual scale/rotation to the child " +
                "Model. The root must stay Scale (1,1,1) / Rotation (0,0,0) so the stamped " +
                "collider holds its gameplay-authoritative size in world space.",
                MessageType.Error);
        }

        // Runs while the obstacle is selected, so dragging the child Model re-floors it live.
        private void OnSceneGUI() => AutoFloorModel((TrackObstacle)target);

        // Guides stay visible while the author resizes the child Model:
        //   green  — floor plane (Y=0)
        //   cyan   — stamped collider, labelled with the footprint
        //   yellow — current model bounds
        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        private static void DrawGuides(TrackObstacle obstacle, GizmoType gizmoType)
        {
            DrawFloorGuide(obstacle);
            DrawColliderGuide(obstacle);
            DrawModelGuide(obstacle);
        }

        // A prefab has exactly one model child; if more exist, the first is the alignment reference.
        private static void AutoFloorModel(TrackObstacle obstacle)
        {
            if (Application.isPlaying) return;

            Transform model = obstacle.transform.childCount > 0 ? obstacle.transform.GetChild(0) : null;
            if (!GeometryUtils.TryGetHierarchyBounds(model, out Bounds bounds)) return;

            float deltaY = -bounds.min.y;
            if (Mathf.Abs(deltaY) < k_AlignTolerance) return;

            model.position += new Vector3(0f, deltaY, 0f);
            EditorUtility.SetDirty(model);
        }

        private static void DrawFloorGuide(TrackObstacle obstacle)
        {
            float halfWidth = ObstacleConstants.k_WideWidth * 0.5f;
            Vector3 left  = obstacle.transform.position + Vector3.left  * halfWidth;
            Vector3 right = obstacle.transform.position + Vector3.right * halfWidth;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(left, right);
            Handles.color = Color.green;
            Handles.Label(right, "  Floor (Y=0)");
        }

        private static void DrawColliderGuide(TrackObstacle obstacle)
        {
            if (!obstacle.TryGetComponent(out BoxCollider box)) return;

            Vector3 center = obstacle.transform.position + box.center;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, box.size);
            Handles.color = Color.cyan;
            Handles.Label(center + Vector3.up * (box.size.y * 0.5f), $"  Hitbox: {obstacle.Footprint}");
        }

        private static void DrawModelGuide(TrackObstacle obstacle)
        {
            Transform model = obstacle.transform.childCount > 0 ? obstacle.transform.GetChild(0) : null;
            if (!GeometryUtils.TryGetHierarchyBounds(model, out Bounds bounds)) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Handles.color = Color.yellow;
            Handles.Label(bounds.center + Vector3.up * (bounds.extents.y + 0.1f), "  Model (auto-floored)");
        }
    }
}
