using System;
using Match3RPG.Battle;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using Match3RPG.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3RPG.Editor
{
    public static class HeroBossRuntimeIntegrationRegressionTests
    {
        const string GameScenePath = "Assets/Scenes/Game.unity";
        const string DevelopmentBossPath = "Assets/Resources/Campaign/Bosses/DevelopmentBoss.asset";

        [MenuItem("Tools/Match3RPG/Run Hero Boss Runtime Integration Tests")]
        public static void RunAll()
        {
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            Require(config != null, "GameConfigService is missing.");
            Require(config.Heroes != null, "HeroDatabase is not wired to GameConfigService.");
            Require(config.Bosses != null && config.Stages != null,
                "BossDatabase or StageDatabase is not wired to GameConfigService.");

            GameObject existingProgression = GameObject.Find(nameof(PlayerProgressionService));
            GameObject existingCurrency = GameObject.Find(nameof(CurrencyService));
            GameObject battleObject = new GameObject("HeroBossRuntimeRegressionBattle");
            BattleController controller = battleObject.AddComponent<BattleController>();
            try
            {
                TestHeroResolution(config, controller);
                TestStageBossResolution(config, controller);
                TestDevelopmentBossRuntimeUpdate(config, controller);
                TestDirectGameScene();
                Debug.Log("Hero/Boss runtime integration regressions passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
                if (existingProgression == null)
                    DestroyIfCreated(nameof(PlayerProgressionService));
                if (existingCurrency == null)
                    DestroyIfCreated(nameof(CurrencyService));
            }
        }

        static void TestHeroResolution(GameConfigService config, BattleController controller)
        {
            GameSaveData save = SaveMigration.CreateNewGame();
            save.loadout.SelectedHeroId = "hero_02";
            HeroData hero = PlayerHeroSelectionService.GetSelectedHeroData(config, save);
            HeroProgressSaveData progress = PlayerHeroSelectionService.GetHeroProgress(save, hero.StableId);
            RuntimeHeroStats stats = new RuntimeHeroStats(hero, progress);

            Require(hero.StableId == "hero_02", "Hero2 did not resolve by stable ID.");
            Require(stats.Level == 1 && stats.Experience == 0,
                "New Hero2 progress was not applied.");
            Require(stats.MaxHP == 500 && stats.MaxSoulHP == 400
                && stats.Attack == 50 && stats.SoulAttack == 50
                && stats.MaxMana == 100 && stats.MaxRage == 100,
                "Level-1 Hero stats do not match the defined base values.");
            Require(stats.CriticalChancePercent == 10f
                && stats.CriticalDamageMultiplier == 2f
                && stats.DodgePercent == 0f && stats.AccuracyPercent == 100f
                && stats.Speed == 10,
                "Level-1 secondary combat stats are incorrect.");
            RuntimeHeroStats levelTwo = new RuntimeHeroStats(hero,
                new HeroProgressSaveData { heroId = hero.StableId, level = 2 });
            Require(levelTwo.MaxHP == 600 && levelTwo.MaxSoulHP == 520
                && levelTwo.Attack == 55 && levelTwo.SoulAttack == 55
                && levelTwo.CriticalChancePercent == 10.5f
                && levelTwo.DodgePercent == .5f
                && levelTwo.AccuracyPercent == 100.5f
                && levelTwo.Speed == 11,
                "Level-2 stat gains do not match the progression rules.");
            VerifyLevelExperienceCurve();
            RuntimeHeroStats withStars = new RuntimeHeroStats(hero, progress,
                starBonuses: new StarChartStatBonuses(13, 3, 2, 1, 1, 2, 3));
            Require(withStars.Attack == 63 && withStars.SoulAttack == 53
                && withStars.MaxHP == 550 && withStars.CriticalDamageMultiplier == 2.01f
                && withStars.DodgePercent == 3f && withStars.AccuracyPercent == 101.5f
                && withStars.Speed == 13,
                "Star bonuses did not aggregate on top of level-1 hero stats.");
            controller.Configure(null, null, null, runtimeHeroStats: stats);
            Require(controller.HeroMaxHealth == 500 && controller.HeroAttackDamagePerGem == 60
                && controller.HeroSoulAttackDamagePerGem == 60
                && controller.MaxRage == 100,
                "Hero2 runtime values were not applied to BattleController.");
            controller.Configure(null, null, null, runtimeHeroStats: withStars);
            Require(controller.HeroMaxHealth == 550
                && controller.HeroAttackDamagePerGem == 73
                && controller.HeroSoulAttackDamagePerGem == 63
                && controller.HeroExperiencePerGem == 3
                && controller.HeroShieldPerGem == 70,
                "BattleController did not apply all star-derived stat bonuses.");
            Require(TurnManager.GetFasterActor(11, 10) == BattleActor.Player
                && TurnManager.GetFasterActor(10, 11) == BattleActor.Boss
                && TurnManager.GetFasterActor(10, 10) == null,
                "Speed comparison did not identify faster actors or preserve an undecided tie.");
        }

        static void VerifyLevelExperienceCurve()
        {
            int[] thresholds = { 200, 250, 313, 392, 490, 613, 767, 959, 1199 };
            for (int level = 1; level <= thresholds.Length; level++)
                Require(HeroLevelProgression.GetExperienceToNextLevel(level)
                    == thresholds[level - 1], $"EXP threshold at Lv{level} was incorrect.");

            HeroProgressSaveData progress = new HeroProgressSaveData
            {
                heroId = "progression-test",
                level = 1
            };
            HeroLevelProgression.AddExperience(progress, 450);
            Require(progress.level == 3 && progress.experience == 0,
                "Accumulated EXP did not advance through Lv1 and Lv2 thresholds.");
            HeroLevelProgression.AddExperience(progress, 313);
            Require(progress.level == 4 && progress.experience == 0,
                "Lv3 to Lv4 did not require 313 EXP.");
        }

        static void TestStageBossResolution(GameConfigService config, BattleController controller)
        {
            StageData stage = config.GetStage("region_01_stage_01");
            Require(stage != null, "The first Region 01 stage did not resolve.");
            Require(config.Stages.TryGetBoss(stage.StableId, config.Bosses,
                out StageData resolvedStage, out BossData resolvedBoss),
                "StageDatabase could not resolve the Stage BossId.");
            Require(resolvedStage == stage && resolvedBoss == stage.BossData,
                "The database Boss differs from the existing Stage popup Boss reference.");

            RuntimeBossStats stats = new RuntimeBossStats(resolvedBoss);
            Require(stats.BossId == resolvedBoss.StableId
                && stats.MaxHP == resolvedBoss.MaxHP
                && stats.Attack == resolvedBoss.Attack,
                "Runtime Boss stats differ from BossConfig.");
            controller.Configure(null, null, null, bossData: resolvedBoss,
                runtimeBossStats: stats);
            Require(controller.BossMaxHealth == resolvedBoss.MaxHP
                && controller.BossAttackDamagePerGem == resolvedBoss.Attack,
                "Stage Boss runtime values were not applied to BattleController.");
        }

        static void TestDevelopmentBossRuntimeUpdate(GameConfigService config,
            BattleController controller)
        {
            BossData developmentBoss = config.GetBoss("development_boss");
            Require(developmentBoss != null, "Development Boss did not resolve by stable ID.");

            SerializedObject serializedBoss = new SerializedObject(developmentBoss);
            SerializedProperty maxHp = serializedBoss.FindProperty("maxHP");
            Require(maxHp != null, "Development Boss MaxHP field was not found.");
            int originalMaxHp = maxHp.intValue;
            try
            {
                maxHp.intValue = originalMaxHp + 17;
                serializedBoss.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(developmentBoss);
                AssetDatabase.SaveAssets();

                controller.Configure(null, null, null, bossData: developmentBoss,
                    runtimeBossStats: new RuntimeBossStats(developmentBoss));
                Require(controller.BossMaxHealth == originalMaxHp + 17,
                    "A changed Development Boss HP did not reach the next configured Battle.");
            }
            finally
            {
                maxHp.intValue = originalMaxHp;
                serializedBoss.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(developmentBoss);
                AssetDatabase.SaveAssets();
            }
        }

        static void DestroyIfCreated(string objectName)
        {
            GameObject createdObject = GameObject.Find(objectName);
            if (createdObject != null)
                UnityEngine.Object.DestroyImmediate(createdObject);
        }

        static void TestDirectGameScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
            try
            {
                bool hasBootstrap = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren<GameBootstrap>(true) != null)
                    {
                        hasBootstrap = true;
                        break;
                    }
                Require(hasBootstrap, "The direct Game scene has no GameBootstrap component.");
                Require(Resources.Load<BossData>("Campaign/Bosses/DevelopmentBoss") != null,
                    "Direct Game fallback Development Boss is unavailable.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
