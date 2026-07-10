#if UNITY_EDITOR
using UnityEngine;

namespace SplitRun.Utility
{
    public static class LayerGuard
    {
        /// <summary>Author-time layer stamp. A hand-edited layer would silently break trigger collisions.</summary>
        public static void Enforce(GameObject target, string layerName, string logPrefix)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning(
                    $"[{logPrefix}] Layer '{layerName}' does not exist. Add it in Project Settings -> " +
                    $"Tags and Layers, then enable Character x {layerName} in the Physics collision matrix.",
                    target);
                return;
            }

            if (target.layer != layer)
                target.layer = layer;
        }
    }
}
#endif
