using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace SplitRun.EditorTools
{
    // Modeless window listing obstacle prefabs with a non-identity root Transform, each with a
    // Ping/Select control. Shown when Play or a build is blocked. Modeless (Show, not ShowModal)
    // so the author can fix prefabs while it stays open, then Re-validate to clear them.
    public class ObstacleValidationWindow : EditorWindow
    {
        private List<GameObject> _invalid = new List<GameObject>();
        private Vector2 _scroll;

        public static void ShowFor(List<GameObject> invalid)
        {
            var window = GetWindow<ObstacleValidationWindow>(utility: false, title: "Obstacle Validation");
            window._invalid = invalid;
            window.minSize = new Vector2(440f, 260f);
            window.Show();
            window.Focus();
        }

        private void OnGUI()
        {
            PruneDestroyed();

            EditorGUILayout.Space();

            if (GUILayout.Button("Re-validate"))
                _invalid = ObstaclePrefabValidator.CollectInvalidObstaclePrefabs();

            EditorGUILayout.Space();

            if (_invalid.Count == 0)
            {
                EditorGUILayout.HelpBox("All obstacle prefabs are valid.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "These obstacle prefabs have a non-identity root Transform, which multiplies into the " +
                "stamped collider. Reset each root to Scale (1,1,1) / Rotation (0,0,0) and move all " +
                "visual scale/rotation to the child Model.",
                MessageType.Error);

            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (GameObject prefab in _invalid)
                DrawRow(prefab);
            EditorGUILayout.EndScrollView();
        }

        private void PruneDestroyed()
        {
            for (int i = _invalid.Count - 1; i >= 0; i--)
            {
                if (!_invalid[i]) _invalid.RemoveAt(i);
            }
        }

        private void DrawRow(GameObject prefab)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(prefab));

                if (GUILayout.Button("Ping", GUILayout.Width(60f)))
                    EditorGUIUtility.PingObject(prefab);

                if (GUILayout.Button("Select", GUILayout.Width(60f)))
                    Selection.activeObject = prefab;
            }
        }
    }
}
