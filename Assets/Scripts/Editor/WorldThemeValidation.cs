using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;

using SplitRun.Environment;
using SplitRun.Obstacle;

namespace SplitRun.EditorTools
{
    public readonly struct ThemeFootprintMismatch
    {
        public WorldThemeProfile Profile     { get; }
        public ObstacleFootprint Slot        { get; }
        public int               PrefabIndex { get; }
        public TrackObstacle     Prefab      { get; }

        public ThemeFootprintMismatch(
            WorldThemeProfile profile, ObstacleFootprint slot, int prefabIndex, TrackObstacle prefab)
        {
            Profile     = profile;
            Slot        = slot;
            PrefabIndex = prefabIndex;
            Prefab      = prefab;
        }
    }

    // A prefab in the wrong slot would spawn under another footprint's selection weight.
    [InitializeOnLoad]
    public class WorldThemeProfileValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static WorldThemeProfileValidator()
        {
            PlayModeBuildGuard.Register<ThemeFootprintMismatch>(
                CollectMismatches, WorldThemeValidationWindow.ShowFor, blocksPlay: true);
        }

        [MenuItem("SplitRun/Validate World Theme Profiles", priority = 22)]
        private static void Validate() => WorldThemeValidationWindow.ShowFor(CollectMismatches());

        public void OnPreprocessBuild(BuildReport report)
        {
            List<ThemeFootprintMismatch> mismatches = CollectMismatches();
            if (mismatches.Count == 0) return;

            WorldThemeValidationWindow.ShowFor(mismatches);
            PlayModeBuildGuard.FailBuild(mismatches.Count,
                "World Theme Profile slot(s) hold a prefab with a mismatched footprint",
                "World Theme Validation window");
        }

        public static List<ThemeFootprintMismatch> CollectMismatches()
        {
            var mismatches = new List<ThemeFootprintMismatch>();

            foreach (string guid in AssetDatabase.FindAssets("t:WorldThemeProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<WorldThemeProfile>(path);
                if (!profile) continue;

                CollectProfileMismatches(profile, mismatches);
            }

            return mismatches;
        }

        private static void CollectProfileMismatches(WorldThemeProfile profile, List<ThemeFootprintMismatch> into)
        {
            foreach ((ObstacleFootprint slot, ObstacleVariants variants) in profile.ObstaclePrefabs)
            {
                if (variants?.Prefabs == null) continue;

                for (int prefabIndex = 0; prefabIndex < variants.Prefabs.Count; prefabIndex++)
                {
                    AssetReferenceGameObject reference = variants.Prefabs[prefabIndex];
                    if (reference == null) continue;

                    GameObject asset = reference.editorAsset;
                    if (!asset || !asset.TryGetComponent(out TrackObstacle prefab)) continue;
                    if (prefab.Footprint == slot) continue;

                    into.Add(new ThemeFootprintMismatch(profile, slot, prefabIndex, prefab));
                }
            }
        }
    }

    public class WorldThemeValidationWindow : ValidationWindow<WorldThemeValidationWindow, ThemeFootprintMismatch>
    {
        private WorldThemeProfile _currentGroup;

        protected override string  WindowTitle   => "World Theme Validation";
        protected override Vector2 WindowMinSize => new Vector2(620f, 280f);
        protected override string  EmptyMessage  => "All World Theme Profile slots match their prefab footprints.";

        protected override string ProblemMessage =>
            "These elements hold a prefab whose footprint differs from its slot, so it would spawn " +
            "under the wrong selection weight. Ping the SO, then move each listed element to the matching slot.";

        public static void ShowFor(List<ThemeFootprintMismatch> mismatches) => ShowWith(mismatches);

        protected override List<ThemeFootprintMismatch> Collect() => WorldThemeProfileValidator.CollectMismatches();

        protected override bool IsAlive(ThemeFootprintMismatch item)
        {
            if (!item.Profile) return false;
            return item.Prefab;
        }

        protected override void OnBeforeRows() => _currentGroup = null;

        // A serialized array element cannot be pinged, so the profile header carries the Ping button.
        protected override void DrawRow(ThemeFootprintMismatch mismatch)
        {
            if (!ReferenceEquals(mismatch.Profile, _currentGroup))
            {
                _currentGroup = mismatch.Profile;
                DrawProfileHeader(_currentGroup);
            }

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField(
                $"{mismatch.Slot}  ->  Prefabs[{mismatch.PrefabIndex}] = " +
                $"{mismatch.Prefab.name} ({mismatch.Prefab.Footprint})");
            EditorGUI.indentLevel = 0;
        }

        private static void DrawProfileHeader(WorldThemeProfile profile)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(profile.name, EditorStyles.boldLabel);
                DrawPingSelect(profile);
            }
        }
    }
}
