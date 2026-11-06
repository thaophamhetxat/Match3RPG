using System;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    static class FixProgressMain01Importer
    {
        const string AssetPath =
            "Assets/Art/MainProgress/Progress/progress_main01.PNG";

        [MenuItem("Tools/Fix Progress Main01 Sprite")]
        static void FixImporter()
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(AssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"TextureImporter not found at {AssetPath}.");
                return;
            }

            try
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.SaveAndReimport();

                TextureImporter imported =
                    AssetImporter.GetAtPath(AssetPath) as TextureImporter;
                if (imported == null)
                {
                    Debug.LogError(
                        $"Reimport finished, but TextureImporter is unavailable at {AssetPath}.");
                    return;
                }

                int spriteCount = 0;
                foreach (UnityEngine.Object asset
                    in AssetDatabase.LoadAllAssetsAtPath(AssetPath))
                    if (asset is Sprite)
                        spriteCount++;

                bool settingsCorrect =
                    imported.textureType == TextureImporterType.Sprite
                    && imported.spriteImportMode == SpriteImportMode.Single
                    && imported.textureShape == TextureImporterShape.Texture2D;
                if (settingsCorrect && spriteCount == 1)
                {
                    Debug.Log(
                        $"Reimport succeeded for {AssetPath}: "
                        + $"textureType={imported.textureType}, "
                        + $"spriteMode={imported.spriteImportMode}, "
                        + $"textureShape={imported.textureShape}, "
                        + $"Sprite sub-assets={spriteCount}. "
                        + "Check the Console for any remaining import errors.");
                }
                else
                {
                    Debug.LogError(
                        $"Reimport did not produce the expected Sprite for {AssetPath}: "
                        + $"textureType={imported.textureType}, "
                        + $"spriteMode={imported.spriteImportMode}, "
                        + $"textureShape={imported.textureShape}, "
                        + $"Sprite sub-assets={spriteCount}.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Failed to reimport {AssetPath}: {exception}");
            }
        }
    }
}
