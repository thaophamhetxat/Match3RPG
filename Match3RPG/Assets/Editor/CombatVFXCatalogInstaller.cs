using System;
using Match3RPG.Battle;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class CombatVFXCatalogInstaller
    {
        const string VfxRoot = "Assets/Art/VFX/Campaign/";
        const string ReferenceRoot = VfxRoot + "Reference/";
        const string ResourcesRoot = "Assets/Resources";
        const string CatalogFolder = ResourcesRoot + "/CombatVFX";
        const string CatalogPath = CatalogFolder + "/CombatVFXCatalog.asset";

        static readonly CombatVFXType[] VfxTypes =
        {
            CombatVFXType.Slash, CombatVFXType.Fireball, CombatVFXType.AirSlash,
            CombatVFXType.Dash, CombatVFXType.Heal, CombatVFXType.Shield,
            CombatVFXType.ShieldHit, CombatVFXType.HitImpact, CombatVFXType.HitPhysical,
            CombatVFXType.CriticalHit, CombatVFXType.Ultimate, CombatVFXType.Stun,
            CombatVFXType.Burn, CombatVFXType.Freeze, CombatVFXType.Poison,
            CombatVFXType.RageReady
        };

        static readonly string[] SpritePaths =
        {
            VfxRoot + "Attack/VFX_Slash.png",
            VfxRoot + "Attack/VFX_Fireball.png",
            VfxRoot + "Attack/VFX_AirSlash.png",
            VfxRoot + "Attack/VFX_Dash.png",
            VfxRoot + "Heal/VFX_Heal.png",
            VfxRoot + "Shield/shield_ring.png",
            VfxRoot + "Defense/VFX_ShieldHit.png",
            VfxRoot + "Impact/VFX_HitImpact.png",
            VfxRoot + "Impact/VFX_HitPhysical.png",
            VfxRoot + "Impact/VFX_CriticalHit.png",
            VfxRoot + "Ultimate/VFX_Ultimate.png",
            VfxRoot + "Status/VFX_StunStars.png",
            VfxRoot + "Status/VFX_BurnAura.png",
            VfxRoot + "Status/VFX_FreezeBurst.png",
            VfxRoot + "Status/VFX_PoisonMist.png",
            VfxRoot + "Status/VFX_RageReady.png"
        };

        [MenuItem("Tools/Match3RPG/Install Campaign Combat VFX")]
        public static void Install()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder(ResourcesRoot, "CombatVFX");
            EnsureVfxImportSettings();

            Sprite[] sprites = new Sprite[SpritePaths.Length];
            for (int index = 0; index < SpritePaths.Length; index++)
            {
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePaths[index]);
                if (sprites[index] == null)
                    Debug.LogError($"Combat VFX asset did not import as a Sprite: {SpritePaths[index]}");
            }

            CombatVFXCatalog catalog = AssetDatabase.LoadAssetAtPath<CombatVFXCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CombatVFXCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.Configure(VfxTypes, sprites);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Combat VFX catalog installed with {Array.FindAll(sprites, sprite => sprite != null).Length} sprites.");
        }

        static void EnsureVfxImportSettings()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D",
                new[] { VfxRoot.TrimEnd('/') });
            int spriteCount = 0;
            foreach (string guid in textureGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || assetPath.StartsWith(ReferenceRoot, StringComparison.Ordinal))
                    continue;

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"Combat VFX texture has no TextureImporter: {assetPath}");
                    continue;
                }

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

                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || !importer.alphaIsTransparency
                    || importer.filterMode != FilterMode.Bilinear
                    || importer.wrapMode != TextureWrapMode.Clamp
                    || importer.mipmapEnabled
                    || AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) == null)
                {
                    Debug.LogError($"Combat VFX import validation failed: {assetPath}");
                    continue;
                }
                spriteCount++;
            }

            Debug.Log($"Validated {spriteCount} individual Combat VFX sprites (Reference excluded).");
        }

        static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
