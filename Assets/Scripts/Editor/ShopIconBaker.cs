using System.IO;

using UnityEditor;
using UnityEngine;

using SplitRun.Data;
using SplitRun.Utility;

namespace SplitRun.EditorTools
{
    // Renders each catalog prefab front-on into a Sprite and wires it onto its ShopCatalog entry.
    public static class ShopIconBaker
    {
        public const string k_DefaultIconFolder = "Assets/ScriptableObjects/Shop/Icons";

        private const int k_IconResolution = 256;

        private const float k_FramePadding      = 1.25f;
        private const float k_CameraDistanceMin = 1f;
        private const float k_LightIntensity    = 1.1f;

        private static readonly Vector3 k_ViewDirection = new Vector3(0f, 0.1f, -1f);
        private static readonly Vector3 k_LightEuler    = new Vector3(35f, 200f, 0f);
        private static readonly Color   k_Background    = new Color(0f, 0f, 0f, 0f);

        /// <summary>Bakes every character and hat icon into outputFolder and wires each onto the catalog. Returns the icon count.</summary>
        public static int Bake(ShopCatalog catalog, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            var serialized = new SerializedObject(catalog);
            int baked = 0;

            baked += BakeArray(serialized, outputFolder,
                ValuesPath(ShopCatalog.k_CharactersField),
                ShopCharacterEntry.k_ModelPrefabField, ShopCharacterEntry.k_IconField, "CHR");

            baked += BakeArray(serialized, outputFolder,
                ValuesPath(ShopCatalog.k_HatsField),
                ShopHatEntry.k_HatPrefabField, ShopHatEntry.k_IconField, "HAT");

            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            return baked;
        }

        // EnumKeyedArray nests its storage one level down, so the entry array is reached through _values.
        private static string ValuesPath(string arrayField) => $"{arrayField}.{EnumKeyedArray.k_ValuesField}";

        private static int BakeArray(SerializedObject serialized, string outputFolder,
            string arrayPath, string sourceField, string iconField, string prefix)
        {
            SerializedProperty array = serialized.FindProperty(arrayPath);
            if (array == null)
            {
                Debug.LogError($"[ShopIconBaker] '{arrayPath}' not found on the catalog — nothing baked.");
                return 0;
            }

            int baked = 0;

            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                SerializedProperty source  = element.FindPropertyRelative(sourceField);
                SerializedProperty target  = element.FindPropertyRelative(iconField);

                if (source == null || target == null)
                    continue;

                GameObject prefab = ResolvePrefab(source.objectReferenceValue);
                if (!prefab)
                    continue;

                Sprite icon = RenderToSprite(prefab, outputFolder, $"{prefix}_{prefab.name}_Icon");
                if (icon)
                {
                    target.objectReferenceValue = icon;
                    baked++;
                }
            }

            return baked;
        }

        // The source may be a GameObject (hat) or a Component (CharacterModel) — both resolve to a prefab.
        private static GameObject ResolvePrefab(Object reference) => reference switch
        {
            GameObject gameObject => gameObject,
            Component component   => component.gameObject,
            _                     => null,
        };

        private static Sprite RenderToSprite(GameObject prefab, string outputFolder, string iconName)
        {
            GameObject subject = Object.Instantiate(prefab);
            SetLayerRecursive(subject, 0);

            Camera camera = CreateCamera();
            Light  light  = CreateLight();

            var texture = new RenderTexture(k_IconResolution, k_IconResolution, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = texture;

            FrameSubject(camera, subject);
            camera.Render();

            Texture2D readback = ReadBack(texture);

            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(subject);
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(light.gameObject);

            return SaveSprite(readback, outputFolder, iconName);
        }

        private static Camera CreateCamera()
        {
            var host = new GameObject("__IconBakeCamera");
            Camera camera = host.AddComponent<Camera>();
            camera.clearFlags      = CameraClearFlags.SolidColor;
            camera.backgroundColor = k_Background;
            camera.orthographic    = true;
            camera.nearClipPlane   = 0.01f;

            return camera;
        }

        private static Light CreateLight()
        {
            var host = new GameObject("__IconBakeLight");
            host.transform.rotation = Quaternion.Euler(k_LightEuler);

            Light light     = host.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = k_LightIntensity;

            return light;
        }

        private static void FrameSubject(Camera camera, GameObject subject)
        {
            if (!GeometryUtils.TryGetHierarchyBounds(subject.transform, out Bounds bounds))
            {
                camera.transform.position = k_ViewDirection.normalized * -k_CameraDistanceMin;
                camera.transform.LookAt(Vector3.zero);
                camera.orthographicSize = k_FramePadding;
                return;
            }

            float radius   = bounds.extents.magnitude;
            float distance = Mathf.Max(radius * 2f, k_CameraDistanceMin);

            camera.transform.position = bounds.center + k_ViewDirection.normalized * -distance;
            camera.transform.LookAt(bounds.center);
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * k_FramePadding;
        }

        private static Texture2D ReadBack(RenderTexture texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;

            var readback = new Texture2D(k_IconResolution, k_IconResolution, TextureFormat.RGBA32, false);
            readback.ReadPixels(new Rect(0f, 0f, k_IconResolution, k_IconResolution), 0, 0);
            readback.Apply();

            RenderTexture.active = previous;
            return readback;
        }

        private static Sprite SaveSprite(Texture2D texture, string outputFolder, string iconName)
        {
            string path = $"{outputFolder}/{iconName}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureAsSprite(path);

            return LoadSprite(path);
        }

        private static void ConfigureAsSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            importer.textureType = TextureImporterType.Sprite;

            // Without an explicit Single mode the sprite sub-asset is never generated.
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
        }

        // LoadAssetAtPath returns the Texture2D main asset; the Sprite is a sub-representation.
        private static Sprite LoadSprite(string path)
        {
            foreach (Object representation in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (representation is Sprite sprite)
                    return sprite;
            }

            Debug.LogError($"[ShopIconBaker] Rendered '{path}' but no Sprite sub-asset was produced.");
            return null;
        }

        private static void SetLayerRecursive(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursive(child.gameObject, layer);
        }
    }

    // Takes the catalog and output folder as fields so baking never depends on the Project selection.
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

        // OpenFolderPanel returns an absolute path; asset APIs need one relative to the project root.
        private static string ToProjectRelativePath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string dataPath   = Application.dataPath.Replace('\\', '/');

            if (!normalized.StartsWith(dataPath))
                return null;

            return "Assets" + normalized.Substring(dataPath.Length);
        }
    }
}
