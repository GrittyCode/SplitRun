using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SplitRun.EditorTools
{
    // The graph bends by (1 - this global), so an unset value curves and a player build can never ship flat.
    [InitializeOnLoad]
    public static class WorldCurveSceneViewToggle
    {
        private static readonly int s_curveDisabledId = Shader.PropertyToID("_WorldCurveDisabled");

        static WorldCurveSceneViewToggle()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            Shader.SetGlobalFloat(s_curveDisabledId, camera.cameraType == CameraType.SceneView ? 1f : 0f);
        }
    }
}
