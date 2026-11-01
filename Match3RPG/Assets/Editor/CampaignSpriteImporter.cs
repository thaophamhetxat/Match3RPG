using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    sealed class CampaignSpriteImporter : AssetPostprocessor
    {
        const string MapsPath = "Assets/Art/Maps/";
        const string StageMarkersPath = "Assets/Art/UI/StageMarkers/";
        const string CampaignUiPath = "Assets/Art/UI/Campaign/";
        const string CampaignReferencePath = CampaignUiPath + "Reference/";
        const string CampaignVfxPath = "Assets/Art/VFX/Campaign/";
        const string CampaignVfxReferencePath = CampaignVfxPath + "Reference/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(MapsPath, System.StringComparison.Ordinal)
                && !assetPath.StartsWith(StageMarkersPath, System.StringComparison.Ordinal)
                && (!assetPath.StartsWith(CampaignUiPath, System.StringComparison.Ordinal)
                    || assetPath.StartsWith(CampaignReferencePath, System.StringComparison.Ordinal))
                && (!assetPath.StartsWith(CampaignVfxPath, System.StringComparison.Ordinal)
                    || assetPath.StartsWith(CampaignVfxReferencePath, System.StringComparison.Ordinal)))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
        }
    }
}
