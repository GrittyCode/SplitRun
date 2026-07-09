using System.IO;

using UnityEditor;
using UnityEngine;

using SplitRun.Constants;
using SplitRun.Environment;
using SplitRun.Obstacle;

namespace SplitRun.EditorTools
{
    // Scaffolds a new obstacle, environment segment, or backdrop directly as a named prefab asset,
    // so authoring always starts from the correct skeleton (identity root, required components,
    // expected children).
    public static class SplitRunCreateMenu
    {
        private const string k_ObstacleFolder      = "Assets/Prefabs/Obstacles";
        private const string k_SegmentFolder       = "Assets/Prefabs/Environment";
        private const string k_ObstacleDefaultName = "OBS_New";
        private const string k_SegmentDefaultName  = "ENV_TrackSegment";
        private const string k_BackdropDefaultName = "ENV_Backdrop";
        private const string k_ModelChildName      = "Model";
        private const string k_FloorChildName      = "Floor";
        private const string k_SilhouetteChildName = "Silhouette";
        private const string k_FloorPlaneName      = "FloorPlane";
        private const string k_FloorFieldName      = "_floor";

        private static readonly string[] k_SegmentGroups = { "Left", "Center", "Right", "Decoration" };

        [MenuItem("SplitRun/Create Obstacle Prefab", priority = 0)]
        private static void CreateObstacle()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Obstacle Prefab", k_ObstacleDefaultName, "prefab", "Name the obstacle prefab.", k_ObstacleFolder);
            if (string.IsNullOrEmpty(path)) return;

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
            try
            {
                root.AddComponent<TrackObstacle>();
                root.layer = ResolveObstacleLayer();
                CreateChild(k_ModelChildName, root.transform);
                SaveAsPrefab(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [MenuItem("SplitRun/Create Environment Segment", priority = 1)]
        private static void CreateEnvironmentSegment()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Environment Segment", k_SegmentDefaultName, "prefab", "Name the segment prefab.", k_SegmentFolder);
            if (string.IsNullOrEmpty(path)) return;

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
            try
            {
                TrackSegment segment = root.AddComponent<TrackSegment>();
                Transform floor = CreateChild(k_FloorChildName, root.transform).transform;
                AddFloorPlane(floor);
                AssignFloor(segment, floor);

                foreach (string groupName in k_SegmentGroups)
                    CreateChild(groupName, root.transform);

                SaveAsPrefab(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [MenuItem("SplitRun/Create Backdrop", priority = 2)]
        private static void CreateBackdrop()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Backdrop", k_BackdropDefaultName, "prefab", "Name the backdrop prefab.", k_SegmentFolder);
            if (string.IsNullOrEmpty(path)) return;

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
            try
            {
                root.AddComponent<BackdropFollower>();
                CreateChild(k_SilhouetteChildName, root.transform);
                SaveAsPrefab(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateChild(string childName, Transform parent)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, worldPositionStays: false);
            return child;
        }

        // Cosmetic floor placeholder — its MeshCollider is removed since collisions are obstacle triggers only.
        private static void AddFloorPlane(Transform floor)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = k_FloorPlaneName;
            plane.transform.SetParent(floor, worldPositionStays: false);

            if (plane.TryGetComponent(out MeshCollider collider))
                Object.DestroyImmediate(collider);
        }

        private static void AssignFloor(TrackSegment segment, Transform floor)
        {
            var serialized = new SerializedObject(segment);
            serialized.FindProperty(k_FloorFieldName).objectReferenceValue = floor;
            serialized.ApplyModifiedProperties();
        }

        private static int ResolveObstacleLayer()
        {
            int layer = LayerMask.NameToLayer(ObstacleConstants.k_LayerName);
            return layer < 0 ? 0 : layer;
        }

        private static void SaveAsPrefab(GameObject root, string path)
        {
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success)
            {
                Debug.LogError($"[SplitRunCreateMenu] Failed to save prefab at {path}.");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
