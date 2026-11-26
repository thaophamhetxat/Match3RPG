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
    public static class RockSpikeSkillSetup
    {
        const string SkillAssetPath = "Assets/Resources/Configuration/Skills/rock_spike.asset";
        const string SkillDatabasePath = "Assets/Resources/Configuration/SkillDatabase.asset";
        const int ExpectedCellWidth = 634;
        const int ExpectedCellHeight = 392;

        [MenuItem("Tools/Match3RPG/Install Rock Spike Skill")]
        public static void Install()
        {
            ImportRockSpikeSheet();
            Sprite[] sortedFrames = LoadSortedRockSpikeFrames();
            if (sortedFrames.Length != SkillConfig.RockSpikeFrameCount)
            {
                throw new InvalidOperationException(
                    $"Expected {SkillConfig.RockSpikeFrameCount} RockSpike frames, got {sortedFrames.Length} at '{SkillConfig.RockSpikeAssetPath}'.");
            }

            Sprite peakIcon = sortedFrames[SkillConfig.RockSpikePeakFrameIndex];

            SkillConfig skill = AssetDatabase.LoadAssetAtPath<SkillConfig>(SkillAssetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillConfig>();
                AssetDatabase.CreateAsset(skill, SkillAssetPath);
            }

            SerializedObject serializedSkill = new SerializedObject(skill);
            serializedSkill.FindProperty("stableId").stringValue = SkillConfig.RockSpikeSkillId;
            serializedSkill.FindProperty("displayName").stringValue = SkillConfig.RockSpikeDisplayName;
            serializedSkill.FindProperty("icon").objectReferenceValue = peakIcon;
            serializedSkill.FindProperty("description").stringValue = SkillConfig.RockSpikeDescription;
            serializedSkill.FindProperty("skillType").enumValueIndex = (int)SkillType.Active;
            serializedSkill.FindProperty("targetType").enumValueIndex = (int)SkillTargetType.SingleEnemy;
            serializedSkill.FindProperty("triggerType").enumValueIndex = (int)SkillTriggerType.Manual;
            serializedSkill.FindProperty("manaCost").intValue = SkillConfig.RockSpikeDefaultManaCost;
            serializedSkill.FindProperty("rageCost").intValue = SkillConfig.RockSpikeDefaultRageCost;
            serializedSkill.FindProperty("cooldownTurns").intValue = SkillConfig.RockSpikeDefaultCooldownTurns;
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
            Debug.Log("[RockSpikeSkillSetup] Imported sheet (7x6, 42 frames, pivot 0.5/0.36), installed rock_spike.asset, registered in SkillDatabase, and passed DefinitionDatabaseValidator.");
        }

        static void ImportRockSpikeSheet()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.RockSpikeAssetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(SkillConfig.RockSpikeAssetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(SkillConfig.RockSpikeAssetPath) as TextureImporter;
            }
            if (importer == null)
                throw new InvalidOperationException(
                    $"Missing TextureImporter for '{SkillConfig.RockSpikeAssetPath}'.");

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
            int columns = SkillConfig.RockSpikeGridColumns;
            int rows = SkillConfig.RockSpikeGridRows;
            if (width % columns != 0 || height % rows != 0)
                throw new InvalidOperationException(
                    $"RockSpike sheet dimensions {width}x{height} do not divide evenly into {columns}x{rows}.");

            int cellWidth = width / columns;
            int cellHeight = height / rows;
            if (cellWidth != ExpectedCellWidth || cellHeight != ExpectedCellHeight)
                throw new InvalidOperationException(
                    $"RockSpike cell size {cellWidth}x{cellHeight} differs from expected {ExpectedCellWidth}x{ExpectedCellHeight}.");

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] existingRects = provider.GetSpriteRects() ?? Array.Empty<SpriteRect>();
            Dictionary<string, SpriteRect> existingByName = new Dictionary<string, SpriteRect>(StringComparer.Ordinal);
            for (int i = 0; i < existingRects.Length; i++)
                existingByName[existingRects[i].name] = existingRects[i];

            string baseName = Path.GetFileNameWithoutExtension(SkillConfig.RockSpikeAssetPath);
            Vector2 customPivot = new Vector2(0.5f, SkillConfig.RockSpikePivotY);
            SpriteRect[] rects = new SpriteRect[SkillConfig.RockSpikeFrameCount];

            // Cắt lưới 7x6 (không Trim), đánh tên theo thứ tự từng ô, pivot (0.5, 0.36).
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

        static Sprite[] LoadSortedRockSpikeFrames()
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SkillConfig.RockSpikeAssetPath)
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
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.RockSpikeAssetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("RockSpike sheet TextureImporter missing.");
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
                throw new InvalidOperationException(
                    $"Expected SpriteImportMode.Multiple, got {importer.spriteImportMode}.");

            Sprite[] frames = LoadSortedRockSpikeFrames();
            if (frames.Length != SkillConfig.RockSpikeFrameCount)
                throw new InvalidOperationException(
                    $"Expected {SkillConfig.RockSpikeFrameCount} sub-sprites, got {frames.Length}.");

            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = frames[i];
                float normX = frame.pivot.x / frame.rect.width;
                float normY = frame.pivot.y / frame.rect.height;
                if (Mathf.Abs(frame.rect.width - ExpectedCellWidth) > 0.5f
                    || Mathf.Abs(frame.rect.height - ExpectedCellHeight) > 0.5f)
                    throw new InvalidOperationException(
                        $"Frame {i} ({frame.name}) has unexpected rect {frame.rect}.");
                if (Mathf.Abs(normX - 0.5f) > 0.01f || Mathf.Abs(normY - SkillConfig.RockSpikePivotY) > 0.01f)
                    throw new InvalidOperationException(
                        $"Frame {i} ({frame.name}) has unexpected normalized pivot ({normX:F3}, {normY:F3}).");
            }

            string metaDiskPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                SkillConfig.RockSpikeAssetPath.Replace('/', Path.DirectorySeparatorChar) + ".meta");
            string metaText = File.ReadAllText(metaDiskPath);
            if (!metaText.Contains("spriteMode: 2"))
                throw new InvalidOperationException("RockSpike .meta does not contain spriteMode: 2.");

            Debug.Log($"[Prompt166Verify] RockSpike sheet: mode={importer.spriteImportMode}, frames={frames.Length}, "
                + $"first={frames[0].name} rect={frames[0].rect}, peak13={frames[SkillConfig.RockSpikePeakFrameIndex].name} "
                + $"pivot=({frames[SkillConfig.RockSpikePeakFrameIndex].pivot.x / frames[SkillConfig.RockSpikePeakFrameIndex].rect.width:F2}, "
                + $"{frames[SkillConfig.RockSpikePeakFrameIndex].pivot.y / frames[SkillConfig.RockSpikePeakFrameIndex].rect.height:F2})");

            GameConfigService configService = Resources.Load<GameConfigService>("Configuration/GameConfigService");
            SkillConfig loaded = configService != null ? configService.GetSkill(SkillConfig.RockSpikeSkillId) : null;
            if (loaded == null)
                throw new InvalidOperationException("GameConfigService.GetSkill(\"rock_spike\") returned null.");
            if (loaded.Icon == null || loaded.ManaCost != 3 || loaded.RageCost != 0
                || loaded.CooldownTurns != 3 || loaded.Level != 1
                || loaded.EffectFrames == null || loaded.EffectFrames.Length != SkillConfig.RockSpikeFrameCount)
                throw new InvalidOperationException("RockSpike SkillConfig fields do not match spec.");

            DefinitionDatabaseValidator.ValidateAll();
            HeroBossRuntimeIntegrationRegressionTests.RunAll();
            PlayerLoadoutRegressionTests.RunAll();
            Debug.Log("[Prompt166Verify] All Prompt 166 checks and existing regressions passed.");
        }
    }
}
