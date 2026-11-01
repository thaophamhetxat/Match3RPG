#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public sealed class GemSpriteImporter : AssetPostprocessor
    {
        const string GemFolder = "Assets/Art/Gems";
        static readonly string[] NormalGemNames =
        {
            "Gem_Red", "Gem_Blue", "Gem_Green", "Gem_Yellow", "Gem_Purple", "Gem_Orange"
        };
        static bool repairingGemAssets;

        void OnPreprocessTexture()
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            if (!IsLegacyGemPngPath(normalizedPath))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        [InitializeOnLoadMethod]
        static void ScheduleGemSpriteRepair()
        {
            EditorApplication.delayCall += EnsureLegacyGemSpritesImported;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += EnsureLegacyGemSpritesImported;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (repairingGemAssets || !importedAssets.Any(IsLegacyGemPngPath)) return;
            EditorApplication.delayCall += EnsureLegacyGemSpritesImported;
        }

        static bool IsLegacyGemPngPath(string path)
        {
            string normalizedPath = path.Replace('\\', '/');
            return normalizedPath.StartsWith(GemFolder + "/", StringComparison.OrdinalIgnoreCase)
                && !normalizedPath.Substring(GemFolder.Length + 1).Contains("/")
                && normalizedPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        [MenuItem("Tools/Match3RPG/Reimport Legacy Gem Sprites")]
        public static void ReimportLegacyGemSprites()
        {
            EnsureLegacyGemSpritesImported();
        }

        static void EnsureLegacyGemSpritesImported()
        {
            if (repairingGemAssets || EditorApplication.isPlayingOrWillChangePlaymode) return;
            repairingGemAssets = true;
            try
            {
                ImportPngsWithRequiredSettings();
            }
            finally
            {
                repairingGemAssets = false;
            }
        }

        static bool ImportPngsWithRequiredSettings()
        {
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { GemFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsLegacyGemPngPath)
                .ToArray();

            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || HasRequiredSettings(importer)) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            bool allSpritesLoaded = true;
            for (int index = 0; index < NormalGemNames.Length; index++)
            {
                string spritePath = $"{GemFolder}/{NormalGemNames[index]}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogError($"Gem sprite asset is missing or did not import as a Sprite: {spritePath}");
                    allSpritesLoaded = false;
                }
            }
            string rainbowPath = $"{GemFolder}/Gem_Rainbow.png";
            Sprite rainbow = AssetDatabase.LoadAssetAtPath<Sprite>(rainbowPath);
            if (rainbow == null)
            {
                Debug.LogError($"Rainbow gem sprite asset is missing or did not import as a Sprite: {rainbowPath}");
                allSpritesLoaded = false;
            }
            return allSpritesLoaded;
        }

        static bool HasRequiredSettings(TextureImporter importer)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && importer.alphaIsTransparency
                && importer.filterMode == FilterMode.Bilinear
                && importer.wrapMode == TextureWrapMode.Clamp
                && !importer.mipmapEnabled
                && settings.spriteMeshType == SpriteMeshType.FullRect;
        }

    }
}
#endif
