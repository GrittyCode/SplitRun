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
    // Guards the "theme slot footprint = prefab footprint" invariant: a prefab in the wrong slot
    [InitializeOnLoad]
    public class WorldThemeProfileValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static WorldThemeProfileValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("SplitRun/Validate World Theme Profiles", priority = 22)]
        private static void Validate() => WorldThemeValidationWindow.ShowFor(CollectMismatches());

        public void OnPreprocessBuild(BuildReport report)
        {
            List<ThemeFootprintMismatch> mismatches = CollectMismatches();
            if (mismatches.Count == 0) return;

            WorldThemeValidationWindow.ShowFor(mismatches);

            throw new BuildFailedException(
                $"{mismatches.Count} World Theme Profile slot(s) have a prefab with a mismatched footprint. " +
                "See the World Theme Validation window.");
        }

        // Fires just before edit mode exits (Play entry). Cancelling isPlaying here aborts the
        // transition so Play never starts with a mismatched profile present.
        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;

            List<ThemeFootprintMismatch> mismatches = CollectMismatches();
            if (mismatches.Count == 0) return;

            EditorApplication.isPlaying = false;
            WorldThemeValidationWindow.ShowFor(mismatches);
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
            IReadOnlyList<FootprintPrefabs> sets = profile.ObstaclePrefabs;

            for (int setIndex = 0; setIndex < sets.Count; setIndex++)
            {
                FootprintPrefabs set = sets[setIndex];
                if (set.Prefabs == null) continue;

                for (int prefabIndex = 0; prefabIndex < set.Prefabs.Count; prefabIndex++)
                {
                    AssetReferenceGameObject reference = set.Prefabs[prefabIndex];
                    if (reference == null) continue;

                    GameObject asset = reference.editorAsset;
                    if (!asset || !asset.TryGetComponent(out TrackObstacle prefab)) continue;
                    if (prefab.Footprint == set.Footprint) continue;

                    into.Add(new ThemeFootprintMismatch(profile, set.Footprint, setIndex, prefabIndex, prefab));
                }
            }
        }
    }

    public readonly struct ThemeFootprintMismatch
    {
        public WorldThemeProfile Profile     { get; }
        public ObstacleFootprint Slot        { get; }
        public int               SetIndex    { get; }
        public int               PrefabIndex { get; }
        public TrackObstacle     Prefab      { get; }

        public ThemeFootprintMismatch(
            WorldThemeProfile profile, ObstacleFootprint slot, int setIndex, int prefabIndex, TrackObstacle prefab)
        {
            Profile     = profile;
            Slot        = slot;
            SetIndex    = setIndex;
            PrefabIndex = prefabIndex;
            Prefab      = prefab;
        }
    }
}
