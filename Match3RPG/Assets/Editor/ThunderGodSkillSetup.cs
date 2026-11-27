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
    public static class ThunderGodSkillSetup
    {
        const string SkillAssetPath = "Assets/Resources/Configuration/Skills/thunder_god.asset";
        const string SkillDatabasePath = "Assets/Resources/Configuration/SkillDatabase.asset";
        const int ExpectedCellWidth = 640;
        const int ExpectedCellHeight = 768;

        [MenuItem("Tools/Match3RPG/Install Thunder God Skill")]
        public static void Install()
        {
            ImportThunderGodSheet();
            Sprite[] sortedFrames = LoadSortedThunderGodFrames();
            if (sortedFrames.Length != SkillConfig.ThunderGodFrameCount)
            {
                throw new InvalidOperationException(
                    $"Expected {SkillConfig.ThunderGodFrameCount} ThunderGod frames, got {sortedFrames.Length} at '{SkillConfig.ThunderGodAssetPath}'.");
            }

            Sprite peakIcon = sortedFrames[SkillConfig.ThunderGodPeakFrameIndex];

            SkillConfig skill = AssetDatabase.LoadAssetAtPath<SkillConfig>(SkillAssetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillConfig>();
                AssetDatabase.CreateAsset(skill, SkillAssetPath);
            }

            SerializedObject serializedSkill = new SerializedObject(skill);
            serializedSkill.FindProperty("stableId").stringValue = SkillConfig.ThunderGodSkillId;
            serializedSkill.FindProperty("displayName").stringValue = SkillConfig.ThunderGodDisplayName;
            serializedSkill.FindProperty("icon").objectReferenceValue = peakIcon;
            serializedSkill.FindProperty("description").stringValue = SkillConfig.ThunderGodDescription;
            serializedSkill.FindProperty("skillType").enumValueIndex = (int)SkillType.Active;
            serializedSkill.FindProperty("targetType").enumValueIndex = (int)SkillTargetType.SingleEnemy;
            serializedSkill.FindProperty("triggerType").enumValueIndex = (int)SkillTriggerType.Manual;
            serializedSkill.FindProperty("manaCost").intValue = SkillConfig.ThunderGodDefaultManaCost;
            serializedSkill.FindProperty("rageCost").intValue = SkillConfig.ThunderGodDefaultRageCost;
            serializedSkill.FindProperty("cooldownTurns").intValue = SkillConfig.ThunderGodDefaultCooldownTurns;
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
            Debug.Log("[ThunderGodSkillSetup] Imported sheet (10x9, 90 frames, pivot 0.5/0.26), installed thunder_god.asset, registered in SkillDatabase, and passed DefinitionDatabaseValidator.");
        }

        static void ImportThunderGodSheet()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.ThunderGodAssetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(SkillConfig.ThunderGodAssetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(SkillConfig.ThunderGodAssetPath) as TextureImporter;
            }
            if (importer == null)
                throw new InvalidOperationException(
                    $"Missing TextureImporter for '{SkillConfig.ThunderGodAssetPath}'.");

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
            int columns = SkillConfig.ThunderGodGridColumns;
            int rows = SkillConfig.ThunderGodGridRows;
            if (width % columns != 0 || height % rows != 0)
                throw new InvalidOperationException(
                    $"ThunderGod sheet dimensions {width}x{height} do not divide evenly into {columns}x{rows}.");

            int cellWidth = width / columns;
            int cellHeight = height / rows;
            if (cellWidth != ExpectedCellWidth || cellHeight != ExpectedCellHeight)
                throw new InvalidOperationException(
                    $"ThunderGod cell size {cellWidth}x{cellHeight} differs from expected {ExpectedCellWidth}x{ExpectedCellHeight}.");

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] existingRects = provider.GetSpriteRects() ?? Array.Empty<SpriteRect>();
            Dictionary<string, SpriteRect> existingByName = new Dictionary<string, SpriteRect>(StringComparer.Ordinal);
            for (int i = 0; i < existingRects.Length; i++)
                existingByName[existingRects[i].name] = existingRects[i];

            string baseName = Path.GetFileNameWithoutExtension(SkillConfig.ThunderGodAssetPath);
            Vector2 customPivot = new Vector2(0.5f, SkillConfig.ThunderGodPivotY);
            SpriteRect[] rects = new SpriteRect[SkillConfig.ThunderGodFrameCount];

            // Cắt lưới 10x9 (không Trim), đánh tên theo thứ tự từng ô, pivot (0.5, 0.26).
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

        static Sprite[] LoadSortedThunderGodFrames()
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SkillConfig.ThunderGodAssetPath)
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
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.ThunderGodAssetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("ThunderGod sheet TextureImporter missing.");
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
                throw new InvalidOperationException(
                    $"Expected SpriteImportMode.Multiple, got {importer.spriteImportMode}.");

            Sprite[] frames = LoadSortedThunderGodFrames();
            if (frames.Length != SkillConfig.ThunderGodFrameCount)
                throw new InvalidOperationException(
                    $"Expected {SkillConfig.ThunderGodFrameCount} sub-sprites, got {frames.Length}.");

            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = frames[i];
                float normX = frame.pivot.x / frame.rect.width;
                float normY = frame.pivot.y / frame.rect.height;
                if (Mathf.Abs(frame.rect.width - ExpectedCellWidth) > 0.5f
                    || Mathf.Abs(frame.rect.height - ExpectedCellHeight) > 0.5f)
                    throw new InvalidOperationException(
                        $"Frame {i} ({frame.name}) has unexpected rect {frame.rect}.");
                if (Mathf.Abs(normX - 0.5f) > 0.01f || Mathf.Abs(normY - SkillConfig.ThunderGodPivotY) > 0.01f)
                    throw new InvalidOperationException(
                        $"Frame {i} ({frame.name}) has unexpected normalized pivot ({normX:F3}, {normY:F3}).");
            }

            string metaDiskPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                SkillConfig.ThunderGodAssetPath.Replace('/', Path.DirectorySeparatorChar) + ".meta");
            string metaText = File.ReadAllText(metaDiskPath);
            if (!metaText.Contains("spriteMode: 2"))
                throw new InvalidOperationException("ThunderGod .meta does not contain spriteMode: 2.");
            if (!metaText.Contains("pivot: {x: 0.5, y: 0.26}"))
                throw new InvalidOperationException("ThunderGod .meta does not contain custom pivot {x: 0.5, y: 0.26}.");

            Sprite peakFrame = frames[SkillConfig.ThunderGodPeakFrameIndex];
            Debug.Log($"[Prompt169Verify] ThunderGod sheet: mode={importer.spriteImportMode}, frames={frames.Length}, "
                + $"first={frames[0].name} rect={frames[0].rect}, peak46={peakFrame.name} "
                + $"pivot=({peakFrame.pivot.x / peakFrame.rect.width:F2}, "
                + $"{peakFrame.pivot.y / peakFrame.rect.height:F2})");

            GameConfigService configService = Resources.Load<GameConfigService>("Configuration/GameConfigService");
            SkillConfig loaded = configService != null ? configService.GetSkill(SkillConfig.ThunderGodSkillId) : null;
            if (loaded == null)
                throw new InvalidOperationException("GameConfigService.GetSkill(\"thunder_god\") returned null.");
            if (loaded.Icon == null || loaded.ManaCost != 3 || loaded.RageCost != 0
                || loaded.CooldownTurns != 3 || loaded.Level != 1
                || loaded.EffectFrames == null || loaded.EffectFrames.Length != SkillConfig.ThunderGodFrameCount)
                throw new InvalidOperationException("ThunderGod SkillConfig fields do not match spec.");

            DefinitionDatabaseValidator.ValidateAll();
            HeroBossRuntimeIntegrationRegressionTests.RunAll();
            PlayerLoadoutRegressionTests.RunAll();
            Debug.Log("[Prompt169Verify] All Prompt 169 checks and existing regressions passed.");
        }
    }
}
