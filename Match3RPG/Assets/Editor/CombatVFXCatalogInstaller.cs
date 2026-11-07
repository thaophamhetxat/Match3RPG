using System;
using System.Collections.Generic;
using System.Linq;
using Match3RPG.Battle;
using UnityEditor;
using UnityEditor.U2D.Sprites;
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
        const string HealingAuraPath = VfxRoot + "Other/healing_aura.png";
        const string LifeStealPath = VfxRoot + "Other/life_steal.png";
        const string RageAuraPath = VfxRoot + "Other/rage_aura_blue.png";

        static readonly CombatVFXType[] VfxTypes =
        {
            CombatVFXType.Slash, CombatVFXType.Fireball, CombatVFXType.AirSlash,
            CombatVFXType.Dash, CombatVFXType.Heal, CombatVFXType.Shield,
            CombatVFXType.ShieldHit, CombatVFXType.HitImpact, CombatVFXType.HitPhysical,
            CombatVFXType.CriticalHit, CombatVFXType.Ultimate, CombatVFXType.Stun,
            CombatVFXType.Burn, CombatVFXType.Freeze, CombatVFXType.Poison,
            CombatVFXType.RageReady, CombatVFXType.LifeSteal
        };

        static readonly string[] SpritePaths =
        {
            VfxRoot + "Attack/VFX_Slash.png",
            VfxRoot + "Attack/VFX_Fireball.png",
            VfxRoot + "Attack/VFX_AirSlash.png",
            VfxRoot + "Attack/VFX_Dash.png",
            HealingAuraPath,
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
            VfxRoot + "Status/VFX_RageReady.png",
            LifeStealPath
        };

        [MenuItem("Tools/Match3RPG/Install Campaign Combat VFX")]
        public static void Install()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder(ResourcesRoot, "CombatVFX");
            EnsureVfxImportSettings();

            Sprite[] sprites = new Sprite[SpritePaths.Length];
            Sprite[][] frames = new Sprite[SpritePaths.Length][];
            for (int index = 0; index < SpritePaths.Length; index++)
            {
                if (VfxTypes[index] == CombatVFXType.Heal)
                    frames[index] = LoadFrameSprites(HealingAuraPath, "HealingAura", 72);
                else if (VfxTypes[index] == CombatVFXType.LifeSteal)
                    frames[index] = LoadFrameSprites(LifeStealPath, "LifeSteal", 60);
                else if (VfxTypes[index] == CombatVFXType.RageReady)
                    frames[index] = LoadFrameSprites(RageAuraPath, "RageAura", 30);

                sprites[index] = VfxTypes[index] != CombatVFXType.RageReady
                    && frames[index] != null && frames[index].Length > 0
                    ? frames[index][0]
                    : AssetDatabase.LoadAssetAtPath<Sprite>(SpritePaths[index]);
                if (sprites[index] == null)
                    Debug.LogError($"Combat VFX asset did not import as a Sprite: {SpritePaths[index]}");
            }

            CombatVFXCatalog catalog = AssetDatabase.LoadAssetAtPath<CombatVFXCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CombatVFXCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.Configure(VfxTypes, sprites, frames);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            int importedFrames = frames.Where(frameSet => frameSet != null).Sum(frameSet => frameSet.Length);
            Debug.Log($"Combat VFX catalog installed with {Array.FindAll(sprites, sprite => sprite != null).Length} sprites and {importedFrames} animation frames.");
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

                if (string.Equals(assetPath, HealingAuraPath, StringComparison.OrdinalIgnoreCase))
                {
                    EnsureFrameSheetImportSettings(assetPath, "HealingAura", 9, 8, 72,
                        SpriteAlignment.BottomCenter, new Vector2(.5f, 0f));
                    spriteCount += 72;
                    continue;
                }
                if (string.Equals(assetPath, LifeStealPath, StringComparison.OrdinalIgnoreCase))
                {
                    EnsureFrameSheetImportSettings(assetPath, "LifeSteal", 8, 8, 60,
                        SpriteAlignment.Center, new Vector2(.5f, .5f));
                    spriteCount += 60;
                    continue;
                }
                if (string.Equals(assetPath, RageAuraPath, StringComparison.OrdinalIgnoreCase))
                {
                    EnsureFrameSheetImportSettings(assetPath, "RageAura", 6, 5, 30,
                        SpriteAlignment.BottomCenter, new Vector2(.5f, 0f));
                    spriteCount += 30;
                    continue;
                }

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

            Debug.Log($"Validated {spriteCount} Combat VFX sprites/frames (Reference excluded).");
        }

        static void EnsureFrameSheetImportSettings(string assetPath, string namePrefix,
            int columns, int rows, int frameCount, SpriteAlignment alignment, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Texture importer is unavailable: {assetPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width % columns != 0 || height % rows != 0)
                throw new InvalidOperationException(
                    $"Sprite sheet dimensions {width}x{height} do not divide evenly into {columns}x{rows}: {assetPath}");

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, SpriteRect> existing = provider.GetSpriteRects()
                .ToDictionary(sprite => sprite.name, sprite => sprite);
            SpriteRect[] rectangles = new SpriteRect[frameCount];
            int cellWidth = width / columns;
            int cellHeight = height / rows;
            for (int index = 0; index < frameCount; index++)
            {
                int column = index % columns;
                int row = index / columns;
                string spriteName = $"{namePrefix}_{index:00}";
                rectangles[index] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(column * cellWidth, height - (row + 1) * cellHeight,
                        cellWidth, cellHeight),
                    alignment = alignment,
                    pivot = pivot,
                    spriteID = existing.TryGetValue(spriteName, out SpriteRect old)
                        ? old.spriteID
                        : GUID.Generate()
                };
            }

            provider.SetSpriteRects(rectangles);
            provider.Apply();
            importer.SaveAndReimport();
            if (LoadFrameSprites(assetPath, namePrefix, frameCount).Length != frameCount)
                throw new InvalidOperationException($"Sprite sheet frame validation failed: {assetPath}");
        }

        static Sprite[] LoadFrameSprites(string assetPath, string namePrefix, int frameCount)
        {
            Dictionary<string, Sprite> imported = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>().ToDictionary(sprite => sprite.name, sprite => sprite);
            Sprite[] frames = new Sprite[frameCount];
            for (int index = 0; index < frameCount; index++)
            {
                string frameName = $"{namePrefix}_{index:00}";
                if (!imported.TryGetValue(frameName, out frames[index]))
                    throw new InvalidOperationException($"Imported animation frame is missing: {assetPath} -> {frameName}");
            }
            return frames;
        }

        static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
