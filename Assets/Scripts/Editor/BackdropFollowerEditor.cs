using UnityEditor;
using UnityEngine;

using SplitRun.Environment;
using SplitRun.Game;
using SplitRun.Utility;

namespace SplitRun.EditorTools
{
    [CustomEditor(typeof(BackdropFollower))]
    public class BackdropFollowerEditor : Editor
    {
        private const string k_CurveShaderNameFragment = "WorldCurve";

        // The build's target aspect, used only to size the framing gizmo's width.
        private const float k_GizmoAspect = 9f / 16f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var backdrop = (BackdropFollower)target;

            if (!backdrop.GetComponentInChildren<Renderer>())
                Show("Add the distant silhouette meshes as children — the framing gizmo appears " +
                     "once a Renderer exists.", MessageType.Info);

            if (UsesCurveShader(backdrop))
                Show("A child uses the world-curve shader. The backdrop must use a plain non-curve " +
                     "material — at this Z the curve sinks it far below the floor.", MessageType.Error);

            if (!GeometryUtils.IsRotationIdentity(backdrop.transform))
                Show("Root rotation is not identity. BackdropFollower drives Z only and assumes an " +
                     "upright silhouette — reset rotation to (0,0,0).", MessageType.Error);
        }

        // Reconstructs the run-start camera (characterZ = 0) and projects its view onto the
        // silhouette plane, so Baseline Y slides the frame exactly as it will in-game.
        //   cyan   — run-start camera frame (fill the silhouette across this width)
        //   green  — Baseline Y plane (align with the live game horizon)
        //   yellow — the silhouette meshes currently authored
        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        private static void DrawGuides(BackdropFollower backdrop, GizmoType gizmoType)
        {
            if (!TryComputeFrame(backdrop, out Vector3 camLocal, out Vector3[] corners)) return;

            Gizmos.matrix  = backdrop.transform.localToWorldMatrix;
            Handles.matrix = backdrop.transform.localToWorldMatrix;

            DrawCameraFrame(camLocal, corners);
            DrawBaselinePlane(corners);
            DrawCurrentSilhouette(backdrop);
        }

        // _WorldCurveEnabled is a global shader property, so it never shows in material.HasProperty.
        private static bool UsesCurveShader(BackdropFollower backdrop)
        {
            foreach (Renderer renderer in backdrop.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material && material.shader &&
                        material.shader.name.Contains(k_CurveShaderNameFragment))
                        return true;
                }
            }

            return false;
        }

        // Intersects the four camera corner rays with the silhouette plane (local Z = 0), which sits
        // at the root origin — so corners come back in the same local space the author edits in.
        private static bool TryComputeFrame(BackdropFollower backdrop, out Vector3 camLocal, out Vector3[] corners)
        {
            camLocal = new Vector3(
                0f,
                CameraFollow.k_OffsetY - backdrop.BaselineY,
                CameraFollow.k_OffsetZ - backdrop.ForwardOffsetZ);
            corners = new Vector3[4];

            Quaternion camRot = Quaternion.Euler(CameraFollow.k_PitchAngle, 0f, 0f);
            float tanV = Mathf.Tan(CameraFollow.k_Fov * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * k_GizmoAspect;

            Vector3[] viewDirs =
            {
                new Vector3(-tanH,  tanV, 1f),
                new Vector3( tanH,  tanV, 1f),
                new Vector3( tanH, -tanV, 1f),
                new Vector3(-tanH, -tanV, 1f),
            };

            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = camRot * viewDirs[i];
                if (dir.z <= 0.0001f) return false;

                corners[i] = camLocal + dir * (-camLocal.z / dir.z);
            }

            return true;
        }

        private static void DrawCameraFrame(Vector3 camLocal, Vector3[] corners)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireSphere(camLocal, 1f);

            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(camLocal, corners[i]);
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }

            Handles.color = new Color(0f, 1f, 1f, 0.7f);
            Handles.Label(Vector3.Lerp(corners[0], corners[1], 0.5f), "  Camera view — fill silhouette across this width");
        }

        // The root origin equals the runtime Baseline Y height, so the plane is local Y = 0.
        private static void DrawBaselinePlane(Vector3[] corners)
        {
            if (!TryEdgeXAtHeight(corners[3], corners[0], 0f, out float leftX))  return;
            if (!TryEdgeXAtHeight(corners[2], corners[1], 0f, out float rightX)) return;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(leftX, 0f, 0f), new Vector3(rightX, 0f, 0f));

            Handles.color = Color.green;
            Handles.Label(new Vector3(leftX, 0f, 0f), "  Baseline Y — align with live game horizon");
        }

        private static void DrawCurrentSilhouette(BackdropFollower backdrop)
        {
            if (!GeometryUtils.TryGetHierarchyBounds(backdrop.transform, out Bounds world)) return;

            Vector3 center = backdrop.transform.InverseTransformPoint(world.center);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), new Vector3(world.size.x, world.size.y, 0f));

            Handles.color = Color.yellow;
            Handles.Label(new Vector3(center.x, center.y + world.extents.y, 0f), "  Current silhouette");
        }

        private static bool TryEdgeXAtHeight(Vector3 a, Vector3 b, float y, out float x)
        {
            x = 0f;

            float dy = b.y - a.y;
            if (Mathf.Abs(dy) < 0.0001f) return false;

            float t = (y - a.y) / dy;
            if (t < 0f || t > 1f) return false;

            x = Mathf.Lerp(a.x, b.x, t);
            return true;
        }

        private static void Show(string message, MessageType type)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(message, type);
        }
    }
}
