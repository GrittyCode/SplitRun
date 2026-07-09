using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

using SplitRun.Obstacle;
using SplitRun.Utility;

namespace SplitRun.EditorTools
{
    // A non-identity root multiplies its Scale/Rotation into the stamped collider, so both
    // Play entry and builds are blocked while any such prefab exists.
    [InitializeOnLoad]
    public class ObstaclePrefabValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static ObstaclePrefabValidator()
        {
            PlayModeBuildGuard.RegisterPlayModeCheck(
                () => CollectInvalid().Count > 0,
                ReportAndShow,
                blocksPlay: true);
        }

        [MenuItem("SplitRun/Validate Obstacle Prefabs", priority = 20)]
        private static void Validate() => ObstacleValidationWindow.ShowFor(CollectInvalid());

        public void OnPreprocessBuild(BuildReport report)
        {
            List<GameObject> invalid = CollectInvalid();
            if (invalid.Count == 0) return;

            ReportAndShow();
            PlayModeBuildGuard.FailBuild(invalid.Count, "obstacle prefab(s) have a non-identity root Transform",
                "Obstacle Validation window");
        }

        public static List<GameObject> CollectInvalid()
        {
            var invalid = new List<GameObject>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!root) continue;

                if (!root.TryGetComponent(out TrackObstacle _)) continue;
                if (GeometryUtils.IsIdentity(root.transform)) continue;

                invalid.Add(root);
            }

            return invalid;
        }

        // The prefab is the console context object, so clicking the line pings it in the Project window.
        private static void ReportAndShow()
        {
            List<GameObject> invalid = CollectInvalid();

            foreach (GameObject prefab in invalid)
                Debug.LogError(
                    "[ObstaclePrefabValidator] Non-identity root Transform: " +
                    $"{AssetDatabase.GetAssetPath(prefab)} — reset root to Scale (1,1,1) / " +
                    "Rotation (0,0,0), move visual scale/rotation to the child Model.",
                    prefab);

            ObstacleValidationWindow.ShowFor(invalid);
        }
    }

    public class ObstacleValidationWindow : ValidationWindow<ObstacleValidationWindow, GameObject>
    {
        protected override string  WindowTitle   => "Obstacle Validation";
        protected override Vector2 WindowMinSize => new Vector2(440f, 260f);
        protected override string  EmptyMessage  => "All obstacle prefabs are valid.";

        protected override string ProblemMessage =>
            "These obstacle prefabs have a non-identity root Transform, which multiplies into the " +
            "stamped collider. Reset each root to Scale (1,1,1) / Rotation (0,0,0) and move all " +
            "visual scale/rotation to the child Model.";

        public static void ShowFor(List<GameObject> invalid) => ShowWith(invalid);

        protected override List<GameObject> Collect() => ObstaclePrefabValidator.CollectInvalid();

        protected override bool IsAlive(GameObject item) => item;

        protected override void DrawRow(GameObject prefab)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(prefab));
                DrawPingSelect(prefab);
            }
        }
    }
}
