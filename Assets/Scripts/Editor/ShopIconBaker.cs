using System.IO;

using UnityEditor;
using UnityEngine;

using SplitRun.Data;

namespace SplitRun.EditorTools
{
    // Renders each catalog prefab front-on into a Sprite asset and writes it back onto the
    // matching ShopCatalog entry, so shop/storage cards get icons without hand-authored art.
    // Driven by ShopIconBakerWindow, which supplies the catalog and the output folder.
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

            baked += BakeArray(serialized, outputFolder, "_characters", "_modelPrefab", "CHR");
            baked += BakeArray(serialized, outputFolder, "_hats", "_hatPrefab", "HAT");

            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            return baked;
        }

        // Renders each entry's source prefab and wires the resulting sprite onto its _icon slot.
        private static int BakeArray(SerializedObject serialized, string outputFolder,
            string arrayField, string sourceField, string prefix)
        {
            SerializedProperty array = serialized.FindProperty(arrayField);
            int baked = 0;

            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                SerializedProperty source  = element.FindPropertyRelative(sourceField);
                SerializedProperty target  = element.FindPropertyRelative("_icon");

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

        // The source may be a GameObject (hat prefab) or a Component reference (CharacterModel) — both resolve to a prefab.
        private static GameObject ResolvePrefab(Object reference) => reference switch
        {
            GameObject gameObject => gameObject,
            Component component   => component.gameObject,
            _                     => null,
        };

        // Spins up a throwaway scene camera, frames the prefab from the front, and captures one RGBA frame.
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

            Light light   = host.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = k_LightIntensity;

            return light;
        }

        // Centers the orthographic camera on the subject's render bounds and sizes it to fit with padding.
        private static void FrameSubject(Camera camera, GameObject subject)
        {
            if (!TryGetBounds(subject, out Bounds bounds))
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

        private static bool TryGetBounds(GameObject subject, out Bounds bounds)
        {
            Renderer[] renderers = subject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
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

            // ImportAsset registers the file; SaveAndReimport then applies the Sprite type synchronously,
            // so the sprite sub-asset exists by the time LoadSprite runs on the next line.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureAsSprite(path);

            return LoadSprite(path);
        }

        private static void ConfigureAsSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            importer.textureType         = TextureImporterType.Sprite;
            // Without an explicit Single mode the sprite sub-asset is never generated, so the load returns null.
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
        }

        // LoadAssetAtPath returns the main asset (the Texture2D) — the Sprite is a sub-representation,
        // so it must be pulled from LoadAllAssetRepresentationsAtPath instead.
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
}
