using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

using SplitRun.Environment;

namespace SplitRun.EditorTools
{
    // TrackScroller uses one recycle step, so mismatched Floor Z lengths open or overlap seams.
    // The seam is cosmetic, so this never blocks Play or fails a build — measuring instantiates
    // every segment prefab, a cost that does not belong on every Play entry.
    public class EnvironmentPrefabValidator : IPreprocessBuildWithReport
    {
        public const float k_LengthTolerance = 0.01f;

        public int callbackOrder => 0;

        [MenuItem("SplitRun/Validate Environment Segments", priority = 21)]
        private static void Validate() => EnvironmentValidationWindow.ShowFor(CollectSegments());

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!HasInconsistentFloorLength()) return;

            Debug.LogWarning(
                "[EnvironmentPrefabValidator] Track segment prefabs do not share one Floor Z length — " +
                "mixing them in TrackScroller may open seams. See SplitRun -> Validate Environment Segments.");
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

        // Measures by instantiating, exactly as TrackScroller does — a prefab asset's
        // Renderer.bounds is unreliable until the object exists in a scene.
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

    public class EnvironmentValidationWindow : ValidationWindow<EnvironmentValidationWindow, TrackSegment>
    {
        private float _reference;
        private bool  _hasReference;

        protected override string  WindowTitle   => "Environment Validation";
        protected override Vector2 WindowMinSize => new Vector2(520f, 280f);
        protected override string  EmptyMessage  => "No TrackSegment prefabs found.";

        protected override MessageType ProblemSeverity => MessageType.Info;

        protected override string ProblemMessage =>
            "Every track segment prefab should share one Floor Z length so TrackScroller can tile " +
            "any variant seamlessly. A length shown in red differs from the first measured length.";

        public static void ShowFor(List<TrackSegment> segments) => ShowWith(segments);

        protected override List<TrackSegment> Collect() => EnvironmentPrefabValidator.CollectSegments();

        protected override bool IsAlive(TrackSegment item) => item;

        // The first measured length is the reference every later row is compared against.
        protected override void OnBeforeRows()
        {
            _reference    = 0f;
            _hasReference = false;
        }

        protected override void DrawRow(TrackSegment segment)
        {
            bool measured = EnvironmentPrefabValidator.TryMeasureFloorLength(segment, out float lengthZ);

            if (measured && !_hasReference)
            {
                _reference    = lengthZ;
                _hasReference = true;
            }

            bool isMismatched = measured && _hasReference &&
                                Mathf.Abs(lengthZ - _reference) > EnvironmentPrefabValidator.k_LengthTolerance;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(segment.gameObject));
                DrawLengthLabel(measured ? $"{lengthZ:F2}m" : "no floor", isMismatched);
                DrawPing(segment.gameObject);
            }
        }

        private static void DrawLengthLabel(string text, bool isMismatched)
        {
            Color previous = GUI.contentColor;
            if (isMismatched) GUI.contentColor = Color.red;

            EditorGUILayout.LabelField(text, GUILayout.Width(80f));

            GUI.contentColor = previous;
        }
    }
}
