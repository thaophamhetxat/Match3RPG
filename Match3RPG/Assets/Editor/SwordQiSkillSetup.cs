using System;
using Match3RPG.Configuration;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class SwordQiSkillSetup
    {
        const string SkillAssetPath = "Assets/Resources/Configuration/Skills/sword_qi.asset";
        const string SkillDatabasePath = "Assets/Resources/Configuration/SkillDatabase.asset";

        [MenuItem("Tools/Match3RPG/Install Sword Qi Skill")]
        public static void Install()
        {
            AssetDatabase.ImportAsset(SkillConfig.SwordQiAssetPath, ImportAssetOptions.ForceUpdate);
            Sprite swordQiSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkillConfig.SwordQiAssetPath);
            if (swordQiSprite == null)
                throw new InvalidOperationException(
                    $"Failed to load SwordQi Sprite at '{SkillConfig.SwordQiAssetPath}'.");

            SkillConfig skill = AssetDatabase.LoadAssetAtPath<SkillConfig>(SkillAssetPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillConfig>();
                AssetDatabase.CreateAsset(skill, SkillAssetPath);
            }

            SerializedObject serializedSkill = new SerializedObject(skill);
            serializedSkill.FindProperty("stableId").stringValue = SkillConfig.SwordQiSkillId;
            serializedSkill.FindProperty("displayName").stringValue = SkillConfig.SwordQiDisplayName;
            serializedSkill.FindProperty("icon").objectReferenceValue = swordQiSprite;
            serializedSkill.FindProperty("description").stringValue = SkillConfig.SwordQiDescription;
            serializedSkill.FindProperty("skillType").enumValueIndex = (int)SkillType.Active;
            serializedSkill.FindProperty("targetType").enumValueIndex = (int)SkillTargetType.SingleEnemy;
            serializedSkill.FindProperty("triggerType").enumValueIndex = (int)SkillTriggerType.Manual;
            serializedSkill.FindProperty("manaCost").intValue = SkillConfig.SwordQiDefaultManaCost;
            serializedSkill.FindProperty("rageCost").intValue = SkillConfig.SwordQiDefaultRageCost;
            serializedSkill.FindProperty("cooldownTurns").intValue = SkillConfig.SwordQiDefaultCooldownTurns;
            serializedSkill.FindProperty("level").intValue = SkillConfig.DefaultLevel;
            serializedSkill.FindProperty("levelTable").arraySize = 0;
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
            Debug.Log("[SwordQiSkillSetup] Installed sword_qi.asset, registered in SkillDatabase, and passed DefinitionDatabaseValidator.");
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SkillConfig.SwordQiAssetPath) as TextureImporter;
            Sprite swordQiSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkillConfig.SwordQiAssetPath);
            if (importer == null || swordQiSprite == null)
                throw new InvalidOperationException("SwordQi_C.png importer or sprite missing.");

            Debug.Log($"[Prompt144Verify] SwordQi Sprite: mode={importer.spriteImportMode}, rect={swordQiSprite.rect}, pivot={swordQiSprite.pivot}");
            if (importer.spriteImportMode != SpriteImportMode.Single)
                throw new InvalidOperationException($"Expected SpriteImportMode.Single, got {importer.spriteImportMode}");

            GameConfigService configService = Resources.Load<GameConfigService>("Configuration/GameConfigService");
            SkillConfig loaded = configService != null ? configService.GetSkill(SkillConfig.SwordQiSkillId) : null;
            if (loaded == null)
                throw new InvalidOperationException("GameConfigService.GetSkill(\"sword_qi\") returned null.");
            if (loaded.Icon == null || loaded.ManaCost != 3 || loaded.RageCost != 0 || loaded.CooldownTurns != 3 || loaded.Level != 1)
                throw new InvalidOperationException("SwordQi SkillConfig fields do not match spec.");

            DefinitionDatabaseValidator.ValidateAll();
            HeroBossRuntimeIntegrationRegressionTests.RunAll();
            PlayerLoadoutRegressionTests.RunAll();
            Debug.Log("[Prompt144Verify] All Prompt 144 checks and existing regressions passed.");
        }
    }
}
