using UnityEditor;
using UnityEngine;

using SplitRun.Environment;

namespace SplitRun.EditorTools
{
    [CustomEditor(typeof(BackdropFollower))]
    public class BackdropFollowerEditor : Editor
    {
        private const string k_CurveShaderNameFragment = "WorldCurve";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var backdrop = (BackdropFollower)target;

            if (!HasAnyRenderer(backdrop))
                Show("Add the distant silhouette meshes as children — the framing gizmo appears " +
                     "once a Renderer exists.", MessageType.Info);

            if (UsesCurveShader(backdrop))
                Show("A child uses the world-curve shader. The backdrop must use a plain non-curve " +
                     "material — at this Z the curve sinks it far below the floor.", MessageType.Error);

            if (!backdrop.IsRootRotationValid())
                Show("Root rotation is not identity. BackdropFollower drives Z only and assumes an " +
                     "upright silhouette — reset rotation to (0,0,0).", MessageType.Error);
        }

        private static bool HasAnyRenderer(BackdropFollower backdrop) =>
            backdrop.GetComponentInChildren<Renderer>();

        // _WorldCurveEnabled is a global-scope shader property, so it never shows in
        // material.HasProperty — match the shader name instead.
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

        private static void Show(string message, MessageType type)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(message, type);
        }
    }
}
