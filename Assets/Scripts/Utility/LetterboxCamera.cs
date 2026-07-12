using UnityEngine;

namespace SplitRun.Utility
{
    [RequireComponent(typeof(Camera))]
    public sealed class LetterboxCamera : MonoBehaviour
    {
        private const float k_DesignAspect = 1080f / 1920f;

        private Camera     _camera;
        private Vector2Int _appliedScreenSize;

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable() => Apply();

        // Screen size changes on resume, split screen and foldable unfold, and Unity raises no callback for any of them.
        private void Update()
        {
            if (Screen.width == _appliedScreenSize.x && Screen.height == _appliedScreenSize.y)
                return;

            Apply();
        }

        private void Apply()
        {
            int width  = Screen.width;
            int height = Screen.height;

            if (width == 0 || height == 0)
                return;

            _appliedScreenSize = new Vector2Int(width, height);

            float scale = width / (float)height / k_DesignAspect;

            _camera.rect = scale < 1f
                ? new Rect(0f, (1f - scale) * 0.5f, 1f, scale)
                : new Rect((1f - 1f / scale) * 0.5f, 0f, 1f / scale, 1f);
        }
    }
}
