using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SplitRun.EditorTools
{
    // Disables the curve in the Scene view only; the graph multiplies its Y bend by this global.
    [InitializeOnLoad]
    public static class WorldCurveSceneViewToggle
    {
        private static readonly int s_curveEnabledId = Shader.PropertyToID("_WorldCurveEnabled");

        static WorldCurveSceneViewToggle()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            Shader.SetGlobalFloat(s_curveEnabledId, camera.cameraType == CameraType.SceneView ? 0f : 1f);
        }
    }
}
