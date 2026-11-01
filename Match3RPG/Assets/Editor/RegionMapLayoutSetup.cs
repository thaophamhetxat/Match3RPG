using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3RPG.Editor
{
    static class RegionMapLayoutSetup
    {
        const string MapPath = "Assets/Art/Maps/Regions/Region01/Region01_Map.png";
        const string HeaderPath = "Assets/Art/Maps/Regions/Region01/Region01_Header.png";
        const string FooterPath = "Assets/Art/Maps/Regions/Region01/Region01_Footer.png";
        const string RegionPath = "Assets/Resources/Campaign/Regions/region_01.asset";
        const string LayoutPath = "Assets/Resources/Configuration/RegionMapLayoutConfig.asset";
        const string ScenePath = "Assets/Scenes/RegionMap.unity";

        [MenuItem("Tools/Campaign/Assign Region Map Artwork")]
        static void AssignRegionMapArtwork()
        {
            Sprite map = LoadSprite(MapPath);
            Sprite header = LoadSprite(HeaderPath);
            Sprite footer = LoadSprite(FooterPath);
            if (map == null || header == null || footer == null)
                throw new System.InvalidOperationException(
                    "Region01 map, header, or footer did not import as a Sprite.");

            RegionData region = AssetDatabase.LoadAssetAtPath<RegionData>(RegionPath);
            if (region == null)
                throw new System.InvalidOperationException($"Region data asset missing: {RegionPath}");
            var regionObject = new SerializedObject(region);
            regionObject.FindProperty("mapSprite").objectReferenceValue = map;
            regionObject.FindProperty("headerSprite").objectReferenceValue = footer;
            regionObject.FindProperty("footerSprite").objectReferenceValue = header;
            SerializedProperty stages = regionObject.FindProperty("stages");
            Vector2[] positions =
            {
                new Vector2(.413f, .799f), new Vector2(.492f, .750f),
                new Vector2(.350f, .695f), new Vector2(.385f, .612f),
                new Vector2(.589f, .554f), new Vector2(.298f, .500f),
                new Vector2(.585f, .426f), new Vector2(.385f, .348f),
                new Vector2(.615f, .228f), new Vector2(.417f, .120f)
            };
            for (int i = 0; i < Mathf.Min(stages.arraySize, positions.Length); i++)
                stages.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("regionMapNodePosition").vector2Value = positions[i];
            regionObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(region);

            RegionMapLayoutConfig layout =
                AssetDatabase.LoadAssetAtPath<RegionMapLayoutConfig>(LayoutPath);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<RegionMapLayoutConfig>();
                AssetDatabase.CreateAsset(layout, LayoutPath);
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RegionMapController controller = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                controller = root.GetComponentInChildren<RegionMapController>(true);
                if (controller != null)
                    break;
            }
            if (controller == null)
                throw new System.InvalidOperationException(
                    "RegionMapController was not found in the RegionMap scene.");

            var controllerObject = new SerializedObject(controller);
            controllerObject.FindProperty("regionMapLayout").objectReferenceValue = layout;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static Sprite LoadSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Texture importer missing: {path}");

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.filterMode != FilterMode.Bilinear
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.mipmapEnabled;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
                if (asset is Sprite matchingSprite && matchingSprite.name == fileName + "_0")
                    return matchingSprite;
            foreach (Object asset in assets)
                if (asset is Sprite anySprite)
                    return anySprite;
            return null;
        }
    }
}
