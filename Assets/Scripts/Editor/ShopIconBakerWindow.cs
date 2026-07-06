using UnityEditor;
using UnityEngine;

using SplitRun.Data;

namespace SplitRun.EditorTools
{
    // Places a ShopCatalog, picks an output folder, and bakes card icons for both — so authoring
    // never depends on which asset happens to be selected in the Project window.
    public class ShopIconBakerWindow : EditorWindow
    {
        private ShopCatalog _catalog;
        private string      _outputFolder = ShopIconBaker.k_DefaultIconFolder;

        [MenuItem("SplitRun/Shop Icon Baker", priority = 40)]
        public static void Open()
        {
            var window = GetWindow<ShopIconBakerWindow>(utility: false, title: "Shop Icon Baker");
            window.minSize = new Vector2(460f, 200f);
            window.Show();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Renders each character and hat prefab front-on into a Sprite and wires it onto the " +
                "catalog entry. Character icons come from the model prefab, hat icons from the hat prefab.",
                MessageType.Info);

            EditorGUILayout.Space();

            _catalog = (ShopCatalog)EditorGUILayout.ObjectField(
                "Shop Catalog", _catalog, typeof(ShopCatalog), allowSceneObjects: false);

            DrawOutputFolderField();

            EditorGUILayout.Space();

            DrawBakeButton();
        }

        private void DrawOutputFolderField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                    BrowseForFolder();
            }
        }

        private void DrawBakeButton()
        {
            using (new EditorGUI.DisabledScope(!CanBake()))
            {
                if (GUILayout.Button("Bake Icons", GUILayout.Height(32f)))
                    Bake();
            }

            if (!_catalog)
                EditorGUILayout.HelpBox("Assign a Shop Catalog to bake.", MessageType.Warning);
            else if (!IsFolderInsideProject(_outputFolder))
                EditorGUILayout.HelpBox("Output folder must be inside the project's Assets folder.", MessageType.Warning);
        }

        private void BrowseForFolder()
        {
            string absolute = EditorUtility.OpenFolderPanel("Icon Output Folder", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(absolute))
                return;

            string relative = ToProjectRelativePath(absolute);
            if (string.IsNullOrEmpty(relative))
            {
                EditorUtility.DisplayDialog("Shop Icon Baker", "Pick a folder inside the project's Assets folder.", "OK");
                return;
            }

            _outputFolder = relative;
        }

        private void Bake()
        {
            int baked = ShopIconBaker.Bake(_catalog, _outputFolder);

            Debug.Log($"[ShopIconBakerWindow] Baked {baked} icon(s) into '{_catalog.name}'.");
            EditorUtility.DisplayDialog("Shop Icon Baker", $"Baked {baked} icon(s) into '{_catalog.name}'.", "OK");
        }

        private bool CanBake() => _catalog && IsFolderInsideProject(_outputFolder);

        private static bool IsFolderInsideProject(string folder)
            => !string.IsNullOrEmpty(folder) && folder.Replace('\\', '/').StartsWith("Assets/");

        // EditorUtility.OpenFolderPanel returns an absolute path; asset APIs need one relative to the project root.
        private static string ToProjectRelativePath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string dataPath    = Application.dataPath.Replace('\\', '/');

            if (!normalized.StartsWith(dataPath))
                return null;

            return "Assets" + normalized.Substring(dataPath.Length);
        }
    }
}
