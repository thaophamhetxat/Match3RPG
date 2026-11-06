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
        const string HealingAuraPath = CampaignVfxPath + "Other/healing_aura.png";
        const string LifeStealPath = CampaignVfxPath + "Other/life_steal.png";
        const string RageAuraPath = CampaignVfxPath + "Other/rage_aura_blue.png";

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
            importer.spriteImportMode = assetPath == HealingAuraPath || assetPath == LifeStealPath
                || assetPath == RageAuraPath
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
        }
    }
}
