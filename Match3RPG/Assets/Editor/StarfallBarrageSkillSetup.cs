using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Match3RPG.Configuration;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class StarfallBarrageSkillSetup
    {
        const string SkillAssetPath = "Assets/Resources/Configuration/Skills/starfall_barrage.asset";
        const string SkillDatabasePath = "Assets/Resources/Configuration/SkillDatabase.asset";
        const int ExpectedCellWidth = 768;
        const int ExpectedCellHeight = 640;

        [MenuItem("Tools/Match3RPG/Install Starfall Barrage Skill")]
        public static void Install()
        {
            ImportStarfallBarrageSheet();
            Sprite[] sortedFrames = LoadSortedStarfallBarrageFrames();
            if (sortedFrames.Length != SkillConfig.StarfallBarrageFrameCount)
            {
                throw new InvalidOperationException(
                    $"Expected {SkillConfig.StarfallBarrageFrameCount} StarfallBarrage frames, got {sortedFrames.Length} at '{SkillConfig.StarfallBarrageAssetPath}'.");
            }

            Sprite peakIcon = sortedFrames[SkillConfig.StarfallBarragePeakFrameIndex];

            SkillConfig skill = AssetDatabase.LoadAssetAtPath<SkillConfig>(SkillAssetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillConfig>();
                AssetDatabase.CreateAsset(skill, SkillAssetPath);
            }

            SerializedObject serializedSkill = new SerializedObject(skill);
            serializedSkill.FindProperty("stableId").stringValue = SkillConfig.StarfallBarrageSkillId;
            serializedSkill.FindProperty("displayName").stringValue = SkillConfig.StarfallBarrageDisplayName;
            serializedSkill.FindProperty("icon").objectReferenceValue = peakIcon;
            serializedSkill.FindProperty("description").stringValue = SkillConfig.StarfallBarrageDescription;
            serializedSkill.FindProperty("skillType").enumValueIndex = (int)SkillType.Active;
            serializedSkill.FindProperty("targetType").enumValueIndex = (int)SkillTargetType.SingleEnemy;
            serializedSkill.FindProperty("triggerType").enumValueIndex = (int)SkillTriggerType.Manual;
            serializedSkill.FindProperty("manaCost").intValue = SkillConfig.StarfallBarrageDefaultManaCost;
            serializedSkill.FindProperty("rageCost").intValue = SkillConfig.StarfallBarrageDefaultRageCost;
            serializedSkill.FindProperty("cooldownTurns").intValue = SkillConfig.StarfallBarrageDefaultCooldownTurns;
            serializedSkill.FindProperty("level").intValue = SkillConfig.DefaultLevel;
            serializedSkill.FindProperty("levelTable").arraySize = 0;

            SerializedProperty framesProp = serializedSkill.FindProperty("effectFrames");
            framesProp.arraySize = sortedFrames.Length;
            for (int i = 0; i < sortedFrames.Length; i++)
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = sortedFrames[i];

            serializedSkill.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);

            SkillDatabase database = AssetDatabase.LoadAssetAtPath<SkillDatabase>(SkillDatabasePath);
            if (database == null)
                throw new InvalidOperationException(
                    $"Missing SkillDatabase at '{SkillDatabasePath}'.");

            SerializedObject serializedDb = new SerializedObject(database);
            SerializedProperty skillsProp = serializedDb.FindProperty("skills");
            bool alreadyPresent = false;
            for (int i = 0; i < skillsProp.arraySize; i++)
            {
                if (skillsProp.GetArrayElementAtIndex(i).objectReferenceValue == skill)
                {
                    alreadyPresent = true;
                    break;
                }
            }
            if (!alreadyPresent)
            {
                int index = skillsProp.arraySize;
                skillsProp.InsertArrayElementAtIndex(index);
                skillsProp.GetArrayElementAtIndex(index).objectReferenceValue = skill;
                serializedDb.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DefinitionDatabaseValidator.ValidateAll();
            Debug.Log("[StarfallBarrageSkillSetup] Imported sheet (10x9, 90 frames, pivot 0.5/0.30), installed starfall_barrage.asset, registered in SkillDatabase, and passed DefinitionDatabaseValidator.");
        }

        static void ImportStarfallBarrageSheet()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.StarfallBarrageAssetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(SkillConfig.StarfallBarrageAssetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(SkillConfig.StarfallBarrageAssetPath) as TextureImporter;
            }
            if (importer == null)
                throw new InvalidOperationException(
                    $"Missing TextureImporter for '{SkillConfig.StarfallBarrageAssetPath}'.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;
            importer.SaveAndReimport();

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int columns = SkillConfig.StarfallBarrageGridColumns;
            int rows = SkillConfig.StarfallBarrageGridRows;
            if (width % columns != 0 || height % rows != 0)
                throw new InvalidOperationException(
                    $"StarfallBarrage sheet dimensions {width}x{height} do not divide evenly into {columns}x{rows}.");

            int cellWidth = width / columns;
            int cellHeight = height / rows;
            if (cellWidth != ExpectedCellWidth || cellHeight != ExpectedCellHeight)
                throw new InvalidOperationException(
                    $"StarfallBarrage cell size {cellWidth}x{cellHeight} differs from expected {ExpectedCellWidth}x{ExpectedCellHeight}.");

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] existingRects = provider.GetSpriteRects() ?? Array.Empty<SpriteRect>();
            Dictionary<string, SpriteRect> existingByName = new Dictionary<string, SpriteRect>(StringComparer.Ordinal);
            for (int i = 0; i < existingRects.Length; i++)
                existingByName[existingRects[i].name] = existingRects[i];

            string baseName = Path.GetFileNameWithoutExtension(SkillConfig.StarfallBarrageAssetPath);
            Vector2 customPivot = new Vector2(0.5f, SkillConfig.StarfallBarragePivotY);
            SpriteRect[] rects = new SpriteRect[SkillConfig.StarfallBarrageFrameCount];

            // Cắt lưới 10x9 (không Trim), đánh tên theo thứ tự từng ô, pivot (0.5, 0.30).
            int index = 0;
            for (int rowFromBottom = 0; rowFromBottom < rows; rowFromBottom++)
            {
                for (int col = 0; col < columns; col++)
                {
                    string spriteName = $"{baseName}_{index}";
                    rects[index] = new SpriteRect
                    {
                        name = spriteName,
                        rect = new Rect(col * cellWidth, rowFromBottom * cellHeight, cellWidth, cellHeight),
                        alignment = SpriteAlignment.Custom,
                        pivot = customPivot,
                        spriteID = existingByName.TryGetValue(spriteName, out SpriteRect old)
                            ? old.spriteID
                            : GUID.Generate()
                    };
                    index++;
                }
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        static Sprite[] LoadSortedStarfallBarrageFrames()
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SkillConfig.StarfallBarrageAssetPath)
                .OfType<Sprite>()
                .ToArray();
            Array.Sort(sprites, (a, b) =>
            {
                if (Mathf.Abs(b.rect.y - a.rect.y) > 1f)
                    return b.rect.y.CompareTo(a.rect.y);
                return a.rect.x.CompareTo(b.rect.x);
            });
            return sprites;
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.StarfallBarrageAssetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("StarfallBarrage sheet TextureImporter missing.");
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
                throw new InvalidOperationException(
                    $"Expected SpriteImportMode.Multiple, got {importer.spriteImportMode}.");

            Sprite[] frames = LoadSortedStarfallBarrageFrames();
            if (frames.Length != SkillConfig.StarfallBarrageFrameCount)
                throw new InvalidOperationException(
                    $"Expected {SkillConfig.StarfallBarrageFrameCount} sub-sprites, got {frames.Length}.");

            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = frames[i];
                float normX = frame.pivot.x / frame.rect.width;
                float normY = frame.pivot.y / frame.rect.height;
                if (Mathf.Abs(frame.rect.width - ExpectedCellWidth) > 0.5f
                    || Mathf.Abs(frame.rect.height - ExpectedCellHeight) > 0.5f)
                    throw new InvalidOperationException(
                        $"Frame {i} ({frame.name}) has unexpected rect {frame.rect}.");
                if (Mathf.Abs(normX - 0.5f) > 0.01f || Mathf.Abs(normY - SkillConfig.StarfallBarragePivotY) > 0.01f)
                    throw new InvalidOperationException(
                        $"Frame {i} ({frame.name}) has unexpected normalized pivot ({normX:F3}, {normY:F3}).");
            }

            string metaDiskPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                SkillConfig.StarfallBarrageAssetPath.Replace('/', Path.DirectorySeparatorChar) + ".meta");
            string metaText = File.ReadAllText(metaDiskPath);
            if (!metaText.Contains("spriteMode: 2"))
                throw new InvalidOperationException("StarfallBarrage .meta does not contain spriteMode: 2.");

            Debug.Log($"[Prompt168Verify] StarfallBarrage sheet: mode={importer.spriteImportMode}, frames={frames.Length}, "
                + $"first={frames[0].name} rect={frames[0].rect}, peak52={frames[SkillConfig.StarfallBarragePeakFrameIndex].name} "
                + $"pivot=({frames[SkillConfig.StarfallBarragePeakFrameIndex].pivot.x / frames[SkillConfig.StarfallBarragePeakFrameIndex].rect.width:F2}, "
                + $"{frames[SkillConfig.StarfallBarragePeakFrameIndex].pivot.y / frames[SkillConfig.StarfallBarragePeakFrameIndex].rect.height:F2})");

            GameConfigService configService = Resources.Load<GameConfigService>("Configuration/GameConfigService");
            SkillConfig loaded = configService != null ? configService.GetSkill(SkillConfig.StarfallBarrageSkillId) : null;
            if (loaded == null)
                throw new InvalidOperationException("GameConfigService.GetSkill(\"starfall_barrage\") returned null.");
            if (loaded.Icon == null || loaded.ManaCost != 3 || loaded.RageCost != 0
                || loaded.CooldownTurns != 3 || loaded.Level != 1
                || loaded.EffectFrames == null || loaded.EffectFrames.Length != SkillConfig.StarfallBarrageFrameCount)
                throw new InvalidOperationException("StarfallBarrage SkillConfig fields do not match spec.");

            DefinitionDatabaseValidator.ValidateAll();
            HeroBossRuntimeIntegrationRegressionTests.RunAll();
            PlayerLoadoutRegressionTests.RunAll();
            Debug.Log("[Prompt168Verify] All Prompt 168 checks and existing regressions passed.");
        }
    }
}
