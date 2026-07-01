using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using SplitRun.Environment;

namespace SplitRun.EditorTools
{
    // Groups footprint mismatches by their World Theme Profile: one header per profile carries the
    // Ping SO button (a serialized array element cannot be pinged directly), and the offending
    // elements are listed beneath it by their array index path so the author can reach each one.
    public class WorldThemeValidationWindow : EditorWindow
    {
        private List<ThemeFootprintMismatch> _mismatches = new List<ThemeFootprintMismatch>();
        private Vector2 _scroll;

        public static void ShowFor(List<ThemeFootprintMismatch> mismatches)
        {
            var window = GetWindow<WorldThemeValidationWindow>(utility: false, title: "World Theme Validation");
            window._mismatches = mismatches;
            window.minSize = new Vector2(620f, 280f);
            window.Show();
            window.Focus();
        }

        private void OnGUI()
        {
            PruneDestroyed();

            EditorGUILayout.Space();

            if (GUILayout.Button("Re-validate"))
                _mismatches = WorldThemeProfileValidator.CollectMismatches();

            EditorGUILayout.Space();

            if (_mismatches.Count == 0)
            {
                EditorGUILayout.HelpBox("All World Theme Profile slots match their prefab footprints.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "These elements hold a prefab whose footprint differs from its slot, so it would spawn " +
                "under the wrong selection weight. Ping the SO, then move each listed element to the matching slot.",
                MessageType.Error);

            EditorGUILayout.Space();

            DrawGroupedMismatches();
        }

        private void DrawGroupedMismatches()
        {
            WorldThemeProfile current = null;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (ThemeFootprintMismatch mismatch in _mismatches)
            {
                if (!ReferenceEquals(mismatch.Profile, current))
                {
                    current = mismatch.Profile;
                    DrawProfileHeader(current);
                }

                DrawElementRow(mismatch);
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndScrollView();
        }

        private void DrawProfileHeader(WorldThemeProfile profile)
        {
            EditorGUI.indentLevel = 0;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(profile.name, EditorStyles.boldLabel);

                if (GUILayout.Button("Ping SO", GUILayout.Width(80f)))
                {
                    EditorGUIUtility.PingObject(profile);
                    Selection.activeObject = profile;
                }
            }
        }

        private void DrawElementRow(ThemeFootprintMismatch mismatch)
        {
            EditorGUI.indentLevel = 1;

            EditorGUILayout.LabelField(
                $"Obstacle Prefabs[{mismatch.SetIndex}] ({mismatch.Slot})  →  " +
                $"Prefabs[{mismatch.PrefabIndex}] = {mismatch.Prefab.name} ({mismatch.Prefab.Footprint})");
        }

        private void PruneDestroyed()
        {
            for (int i = _mismatches.Count - 1; i >= 0; i--)
            {
                if (!_mismatches[i].Profile || !_mismatches[i].Prefab) _mismatches.RemoveAt(i);
            }
        }
    }
}
