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
        const string SoulHarvestPath = CampaignVfxPath + "SouldH/";
        const string SkillsVfxPath = "Assets/Art/VFX/Skills/";
        const string RockSpikePath = SkillsVfxPath + "RockSpike/";
        const string StarfallBarragePath = SkillsVfxPath + "starfall_barrage/";
        const string HealingAuraPath = CampaignVfxPath + "Other/healing_aura.png";
        const string LifeStealPath = CampaignVfxPath + "Other/life_steal.png";
        const string RageAuraPath = CampaignVfxPath + "Other/rage_aura_blue.png";
        const string BloodHitPath = CampaignVfxPath + "Impact/BloodHit.png";
        const string ShieldBubblePath = CampaignVfxPath + "Shield/ShieldBubble.png";

        void OnPreprocessTexture()
        {
            bool isSkillsVfx = assetPath.StartsWith(SkillsVfxPath, System.StringComparison.Ordinal);
            if (!isSkillsVfx
                && !assetPath.StartsWith(MapsPath, System.StringComparison.Ordinal)
                && !assetPath.StartsWith(StageMarkersPath, System.StringComparison.Ordinal)
                && (!assetPath.StartsWith(CampaignUiPath, System.StringComparison.Ordinal)
                    || assetPath.StartsWith(CampaignReferencePath, System.StringComparison.Ordinal))
                && (!assetPath.StartsWith(CampaignVfxPath, System.StringComparison.Ordinal)
                    || assetPath.StartsWith(CampaignVfxReferencePath, System.StringComparison.Ordinal)))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            bool isRockSpike = assetPath.StartsWith(RockSpikePath, System.StringComparison.Ordinal);
            bool isStarfallBarrage = assetPath.StartsWith(StarfallBarragePath, System.StringComparison.Ordinal);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = isRockSpike
                || isStarfallBarrage
                || (!isSkillsVfx
                    && (assetPath == HealingAuraPath || assetPath == LifeStealPath
                        || assetPath == RageAuraPath || assetPath == BloodHitPath
                        || assetPath == ShieldBubblePath
                        || assetPath.StartsWith(SoulHarvestPath, System.StringComparison.Ordinal)))
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
            if (isSkillsVfx && !isRockSpike && !isStarfallBarrage)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(settings);
            }
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
        }
    }
}
