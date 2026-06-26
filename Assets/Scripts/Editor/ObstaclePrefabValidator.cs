using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

using SplitRun.Obstacle;

namespace SplitRun.EditorTools
{
    // Guards the "footprint = fixed hitbox" invariant: a non-identity root multiplies its
    // Scale/Rotation into the stamped collider, so build and Play entry are blocked while any
    // such prefab exists.
    [InitializeOnLoad]
    public class ObstaclePrefabValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static ObstaclePrefabValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("SplitRun/Validate Obstacle Prefabs", priority = 20)]
        private static void Validate() => ObstacleValidationWindow.ShowFor(CollectInvalidObstaclePrefabs());

        public void OnPreprocessBuild(BuildReport report)
        {
            List<GameObject> invalid = CollectInvalidObstaclePrefabs();
            if (invalid.Count == 0) return;

            ReportInvalid(invalid);
            ObstacleValidationWindow.ShowFor(invalid);

            throw new BuildFailedException(
                $"{invalid.Count} obstacle prefab(s) have a non-identity root Transform. " +
                "See the Obstacle Validation window or the clickable console errors.");
        }

        // Fires just before edit mode exits (Play entry). Cancelling isPlaying here aborts the
        // transition so Play never actually starts with an invalid prefab present.
        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;

            List<GameObject> invalid = CollectInvalidObstaclePrefabs();
            if (invalid.Count == 0) return;

            EditorApplication.isPlaying = false;

            ReportInvalid(invalid);
            ObstacleValidationWindow.ShowFor(invalid);
        }

        // Each error carries its prefab as the context object, so clicking the console line
        // pings and selects that prefab in the Project window.
        private static void ReportInvalid(List<GameObject> invalid)
        {
            foreach (GameObject prefab in invalid)
                Debug.LogError(
                    $"[ObstaclePrefabValidator] Non-identity root Transform: " +
                    $"{AssetDatabase.GetAssetPath(prefab)} — reset root to Scale (1,1,1) / " +
                    "Rotation (0,0,0), move visual scale/rotation to the child Model.",
                    prefab);
        }

        public static List<GameObject> CollectInvalidObstaclePrefabs()
        {
            var invalid = new List<GameObject>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) continue;

                if (!root.TryGetComponent(out TrackObstacle obstacle)) continue;
                if (obstacle.IsRootTransformValid()) continue;

                invalid.Add(root);
            }

            return invalid;
        }
    }
}
