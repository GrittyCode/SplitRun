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
    public readonly struct ThemeObstacleTypeMismatch
    {
        public WorldThemeProfile Profile     { get; }
        public ObstacleType      Slot        { get; }
        public int               PrefabIndex { get; }
        public TrackObstacle     Prefab      { get; }

        public ThemeObstacleTypeMismatch(
            WorldThemeProfile profile, ObstacleType slot, int prefabIndex, TrackObstacle prefab)
        {
            Profile     = profile;
            Slot        = slot;
            PrefabIndex = prefabIndex;
            Prefab      = prefab;
        }
    }

    // A prefab in the wrong slot would spawn under another obstacle type's selection weight.
    [InitializeOnLoad]
    public class WorldThemeProfileValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static WorldThemeProfileValidator()
        {
            PlayModeBuildGuard.Register<ThemeObstacleTypeMismatch>(
                CollectMismatches, WorldThemeValidationWindow.ShowFor, blocksPlay: true);
        }

        [MenuItem("SplitRun/Validate World Theme Profiles", priority = 22)]
        private static void Validate() => WorldThemeValidationWindow.ShowFor(CollectMismatches());

        public void OnPreprocessBuild(BuildReport report)
        {
            List<ThemeObstacleTypeMismatch> mismatches = CollectMismatches();
            if (mismatches.Count == 0) return;

            WorldThemeValidationWindow.ShowFor(mismatches);
            PlayModeBuildGuard.FailBuild(mismatches.Count,
                "World Theme Profile slot(s) hold a prefab with a mismatched obstacle type",
                "World Theme Validation window");
        }

        public static List<ThemeObstacleTypeMismatch> CollectMismatches()
        {
            var mismatches = new List<ThemeObstacleTypeMismatch>();

            foreach (string guid in AssetDatabase.FindAssets("t:WorldThemeProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<WorldThemeProfile>(path);
                if (!profile) continue;

                CollectProfileMismatches(profile, mismatches);
            }

            return mismatches;
        }

        private static void CollectProfileMismatches(WorldThemeProfile profile, List<ThemeObstacleTypeMismatch> into)
        {
            foreach ((ObstacleType slot, ObstacleVariants variants) in profile.ObstaclePrefabs)
            {
                if (variants?.Prefabs == null) continue;

                for (int prefabIndex = 0; prefabIndex < variants.Prefabs.Count; prefabIndex++)
                {
                    AssetReferenceGameObject reference = variants.Prefabs[prefabIndex];
                    if (reference == null) continue;

                    GameObject asset = reference.editorAsset;
                    if (!asset || !asset.TryGetComponent(out TrackObstacle prefab)) continue;
                    if (prefab.Type == slot) continue;

                    into.Add(new ThemeObstacleTypeMismatch(profile, slot, prefabIndex, prefab));
                }
            }
        }
    }

    public class WorldThemeValidationWindow : ValidationWindow<WorldThemeValidationWindow, ThemeObstacleTypeMismatch>
    {
        private WorldThemeProfile _currentGroup;

        protected override string  WindowTitle   => "World Theme Validation";
        protected override Vector2 WindowMinSize => new Vector2(620f, 280f);
        protected override string  EmptyMessage  => "All World Theme Profile slots match their prefab obstacle types.";

        protected override string ProblemMessage =>
            "These elements hold a prefab whose obstacle type differs from its slot, so it would spawn " +
            "under the wrong selection weight. Ping the SO, then move each listed element to the matching slot.";

        public static void ShowFor(List<ThemeObstacleTypeMismatch> mismatches) => ShowWith(mismatches);

        protected override List<ThemeObstacleTypeMismatch> Collect() => WorldThemeProfileValidator.CollectMismatches();

        protected override bool IsAlive(ThemeObstacleTypeMismatch item)
        {
            if (!item.Profile) return false;
            return item.Prefab;
        }

        protected override void OnBeforeRows() => _currentGroup = null;

        // A serialized array element cannot be pinged, so the profile header carries the Ping button.
        protected override void DrawRow(ThemeObstacleTypeMismatch mismatch)
        {
            if (!ReferenceEquals(mismatch.Profile, _currentGroup))
            {
                _currentGroup = mismatch.Profile;
                DrawProfileHeader(_currentGroup);
            }

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField(
                $"{mismatch.Slot}  ->  Prefabs[{mismatch.PrefabIndex}] = " +
                $"{mismatch.Prefab.name} ({mismatch.Prefab.Type})");
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
