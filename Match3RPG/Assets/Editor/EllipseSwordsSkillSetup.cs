using System;
using System.IO;
using Match3RPG.Configuration;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class EllipseSwordsSkillSetup
    {
        const string SkillAssetPath = "Assets/Resources/Configuration/Skills/ellipse_swords.asset";
        const string SkillDatabasePath = "Assets/Resources/Configuration/SkillDatabase.asset";
        const int ExpectedWidth = 256;
        const int ExpectedHeight = 148;

        [MenuItem("Tools/Match3RPG/Install Ellipse Swords Skill")]
        public static void Install()
        {
            AssetDatabase.ImportAsset(SkillConfig.EllipseSwordsAssetPath, ImportAssetOptions.ForceUpdate);
            Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkillConfig.EllipseSwordsAssetPath);
            if (portalSprite == null)
                throw new InvalidOperationException(
                    $"Failed to load EllipsePortal Sprite at '{SkillConfig.EllipseSwordsAssetPath}'.");

            SkillConfig skill = AssetDatabase.LoadAssetAtPath<SkillConfig>(SkillAssetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillConfig>();
                AssetDatabase.CreateAsset(skill, SkillAssetPath);
            }

            SerializedObject serializedSkill = new SerializedObject(skill);
            serializedSkill.FindProperty("stableId").stringValue = SkillConfig.EllipseSwordsSkillId;
            serializedSkill.FindProperty("displayName").stringValue = SkillConfig.EllipseSwordsDisplayName;
            serializedSkill.FindProperty("icon").objectReferenceValue = portalSprite;
            serializedSkill.FindProperty("description").stringValue = SkillConfig.EllipseSwordsDescription;
            serializedSkill.FindProperty("skillType").enumValueIndex = (int)SkillType.Active;
            serializedSkill.FindProperty("targetType").enumValueIndex = (int)SkillTargetType.SingleEnemy;
            serializedSkill.FindProperty("triggerType").enumValueIndex = (int)SkillTriggerType.Manual;
            serializedSkill.FindProperty("manaCost").intValue = SkillConfig.EllipseSwordsDefaultManaCost;
            serializedSkill.FindProperty("rageCost").intValue = SkillConfig.EllipseSwordsDefaultRageCost;
            serializedSkill.FindProperty("cooldownTurns").intValue = SkillConfig.EllipseSwordsDefaultCooldownTurns;
            serializedSkill.FindProperty("level").intValue = SkillConfig.DefaultLevel;
            serializedSkill.FindProperty("levelTable").arraySize = 0;
            SerializedProperty effectFramesProp = serializedSkill.FindProperty("effectFrames");
            if (effectFramesProp != null)
                effectFramesProp.arraySize = 0;
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
            Debug.Log("[EllipseSwordsSkillSetup] Installed ellipse_swords.asset, registered in SkillDatabase, and passed DefinitionDatabaseValidator.");
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.EllipseSwordsAssetPath) as TextureImporter;
            Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkillConfig.EllipseSwordsAssetPath);
            if (importer == null || portalSprite == null)
                throw new InvalidOperationException("ellipse_portal.png importer or sprite missing.");

            if (importer.spriteImportMode != SpriteImportMode.Single)
                throw new InvalidOperationException(
                    $"Expected SpriteImportMode.Single, got {importer.spriteImportMode}.");

            if (Mathf.Abs(portalSprite.rect.width - ExpectedWidth) > 0.5f
                || Mathf.Abs(portalSprite.rect.height - ExpectedHeight) > 0.5f)
                throw new InvalidOperationException(
                    $"Expected {ExpectedWidth}x{ExpectedHeight} sprite rect, got {portalSprite.rect}.");

            float normX = portalSprite.pivot.x / portalSprite.rect.width;
            float normY = portalSprite.pivot.y / portalSprite.rect.height;
            if (Mathf.Abs(normX - 0.5f) > 0.01f || Mathf.Abs(normY - 0.5f) > 0.01f)
                throw new InvalidOperationException(
                    $"Expected Center pivot (0.5, 0.5), got ({normX:F3}, {normY:F3}).");

            string metaDiskPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                SkillConfig.EllipseSwordsAssetPath.Replace('/', Path.DirectorySeparatorChar) + ".meta");
            string metaText = File.ReadAllText(metaDiskPath);
            if (!metaText.Contains("spriteMode: 1") || !metaText.Contains("spritePivot: {x: 0.5, y: 0.5}"))
                throw new InvalidOperationException(
                    "ellipse_portal.png.meta must contain spriteMode: 1 and spritePivot: {x: 0.5, y: 0.5}.");

            Debug.Log($"[Prompt167Verify] EllipsePortal Sprite: mode={importer.spriteImportMode}, rect={portalSprite.rect}, pivot=({normX:F2}, {normY:F2})");

            GameConfigService configService = Resources.Load<GameConfigService>("Configuration/GameConfigService");
            SkillConfig loaded = configService != null ? configService.GetSkill(SkillConfig.EllipseSwordsSkillId) : null;
            if (loaded == null)
                throw new InvalidOperationException("GameConfigService.GetSkill(\"ellipse_swords\") returned null.");
            if (loaded.Icon == null || loaded.ManaCost != 3 || loaded.RageCost != 0
                || loaded.CooldownTurns != 3 || loaded.Level != 1)
                throw new InvalidOperationException("EllipseSwords SkillConfig fields do not match spec.");

            DefinitionDatabaseValidator.ValidateAll();
            HeroBossRuntimeIntegrationRegressionTests.RunAll();
            PlayerLoadoutRegressionTests.RunAll();
            Debug.Log("[Prompt167Verify] All Prompt 167 checks and existing regressions passed.");
        }
    }
}
