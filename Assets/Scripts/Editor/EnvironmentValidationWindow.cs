using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SplitRun.Environment;

namespace SplitRun.EditorTools
{
    // Lists every TrackSegment prefab with its measured Floor Z length so the author can confirm all
    // variants share one length (the seamless-tiling invariant). Modeless with per-row Ping/Select,
    // mirroring ObstacleValidationWindow. Lengths differing from the first measured length are red.
    public class EnvironmentValidationWindow : EditorWindow
    {
        private const float k_LengthTolerance = 0.01f;

        private List<TrackSegment> _segments = new List<TrackSegment>();
        private Vector2 _scroll;

        public static void ShowFor(List<TrackSegment> segments)
        {
            var window = GetWindow<EnvironmentValidationWindow>(utility: false, title: "Environment Validation");
            window._segments = segments;
            window.minSize = new Vector2(520f, 280f);
            window.Show();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Every track segment prefab should share one Floor Z length so TrackScroller can tile " +
                "any variant seamlessly. A length shown in red differs from the first measured length.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Re-validate"))
                _segments = EnvironmentPrefabValidator.CollectSegments();

            EditorGUILayout.Space();

            if (_segments.Count == 0)
            {
                EditorGUILayout.HelpBox("No TrackSegment prefabs found.", MessageType.Info);
                return;
            }

            DrawSegmentRows();
        }

        private void DrawSegmentRows()
        {
            float reference   = 0f;
            bool hasReference = false;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                TrackSegment segment = _segments[i];
                if (!segment) { _segments.RemoveAt(i); continue; }

                DrawRow(segment, ref reference, ref hasReference);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(TrackSegment segment, ref float reference, ref bool hasReference)
        {
            bool measured = EnvironmentPrefabValidator.TryMeasureFloorLength(segment, out float lengthZ);

            if (measured && !hasReference)
            {
                reference    = lengthZ;
                hasReference = true;
            }

            bool isMismatched = measured && hasReference && Mathf.Abs(lengthZ - reference) > k_LengthTolerance;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(segment.gameObject));
                DrawLengthLabel(measured ? $"{lengthZ:F2}m" : "no floor", isMismatched);

                if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                    EditorGUIUtility.PingObject(segment.gameObject);

                if (GUILayout.Button("Select", GUILayout.Width(60f)))
                    Selection.activeObject = segment.gameObject;
            }
        }

        private void DrawLengthLabel(string text, bool isMismatched)
        {
            Color previous = GUI.contentColor;
            if (isMismatched) GUI.contentColor = Color.red;

            EditorGUILayout.LabelField(text, GUILayout.Width(80f));

            GUI.contentColor = previous;
        }
    }
}
