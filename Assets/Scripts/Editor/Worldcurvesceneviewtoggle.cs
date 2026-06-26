using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SplitRun.EditorTools
{
    // camera is about to render. Requires the graph to multiply its Y bend by _WorldCurveEnabled
    // (default 1); builds have no Scene view and keep the default, so the curve stays on.
    [InitializeOnLoad]
    public static class WorldCurveSceneViewToggle
    {
        private static readonly int s_kCurveEnabledId = Shader.PropertyToID("_WorldCurveEnabled");

        static WorldCurveSceneViewToggle()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            float isEnabled = camera.cameraType == CameraType.SceneView ? 0f : 1f;
            Shader.SetGlobalFloat(s_kCurveEnabledId, isEnabled);
        }
    }
}
