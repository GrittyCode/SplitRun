using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplitRun.Environment;

namespace SplitRun.EditorTools
{
    // Cross-checks that every TrackSegment prefab shares one Floor Z length: a mismatched length
    // makes TrackScroller's single recycle step open or overlap seams once variants are mixed.
    [InitializeOnLoad]
    public static class EnvironmentPrefabValidator
    {
        private const float k_LengthTolerance = 0.01f;

        static EnvironmentPrefabValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("SplitRun/Validate Environment Segments", priority = 21)]
        private static void Validate() => EnvironmentValidationWindow.ShowFor(CollectSegments());

        // Surfaces a warning on Play entry but never cancels the transition — the seam is cosmetic.
        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            if (!HasInconsistentFloorLength()) return;

            Debug.LogWarning(
                "[EnvironmentPrefabValidator] Track segment prefabs do not share one Floor Z length — " +
                "mixing them in TrackScroller may open seams. See SplitRun → Validate Environment Segments.");

            EnvironmentValidationWindow.ShowFor(CollectSegments());
        }

        public static List<TrackSegment> CollectSegments()
        {
            var segments = new List<TrackSegment>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!root) continue;

                if (root.TryGetComponent(out TrackSegment segment))
                    segments.Add(segment);
            }

            return segments;
        }

        // Measures by instantiating the prefab, exactly as TrackScroller does at runtime — a prefab
        // asset's Renderer.bounds is unreliable until the object exists in a scene.
        public static bool TryMeasureFloorLength(TrackSegment prefab, out float lengthZ)
        {
            lengthZ = 0f;

            GameObject instance = Object.Instantiate(prefab.gameObject);
            instance.hideFlags  = HideFlags.HideAndDontSave;
            try
            {
                if (!instance.TryGetComponent(out TrackSegment segment)) return false;
                return segment.TryGetFloorMetrics(out lengthZ, out _);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static bool HasInconsistentFloorLength()
        {
            float reference   = 0f;
            bool hasReference = false;

            foreach (TrackSegment segment in CollectSegments())
            {
                if (!TryMeasureFloorLength(segment, out float lengthZ)) continue;

                if (!hasReference)
                {
                    reference    = lengthZ;
                    hasReference = true;
                    continue;
                }

                if (Mathf.Abs(lengthZ - reference) > k_LengthTolerance)
                    return true;
            }

            return false;
        }
    }
}
