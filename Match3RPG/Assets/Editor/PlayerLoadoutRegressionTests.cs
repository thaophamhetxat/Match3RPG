using System;
using System.IO;
using Match3RPG.Battle;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using Match3RPG.Persistence;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class PlayerLoadoutRegressionTests
    {
        [MenuItem("Tools/Match3RPG/Run Player Loadout Tests")]
        public static void RunAll()
        {
            GameConfigService gameConfig = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            HeroData hero = gameConfig != null
                ? gameConfig.GetHero(SaveMigration.StartingHeroId)
                : PlayerHeroSelectionService.LoadHeroData();
            Require(hero != null, "Hero2 is required for loadout tests.");

            GameObject existingProgression = GameObject.Find(nameof(PlayerProgressionService));
            GameObject existingCurrency = GameObject.Find(nameof(CurrencyService));
            PetConfig pet = ScriptableObject.CreateInstance<PetConfig>();
            CompanionConfig companion = ScriptableObject.CreateInstance<CompanionConfig>();
            SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();
            LoadoutConfig limits = CreateLoadoutConfig(1, 0, 2);
            string testDirectory = Path.Combine(Path.GetTempPath(),
                "Match3RPG_PlayerLoadoutTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            try
            {
                TestEmptyOptionalLoadout(testDirectory, gameConfig);
                TestInvalidOptionalIds(testDirectory, hero, pet, companion, skill);
                TestDuplicateSkillAndLimits(testDirectory, hero, pet, companion, skill, limits);
                TestSaveReload(testDirectory, hero);
                TestHeroFallback(testDirectory, hero);
                TestBattleWithoutOptionalDefinitions(testDirectory, gameConfig, hero);
                Debug.Log("Player loadout regressions passed: empty and invalid optional IDs, "
                    + "duplicate/slot limits, save/reload, Hero2 fallback, and Battle without "
                    + "Pet/Companion/Skill definitions.");
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                    Directory.Delete(testDirectory, true);
                UnityEngine.Object.DestroyImmediate(pet);
                UnityEngine.Object.DestroyImmediate(companion);
                UnityEngine.Object.DestroyImmediate(skill);
                UnityEngine.Object.DestroyImmediate(limits);
                if (existingProgression == null)
                    DestroyIfCreated(nameof(PlayerProgressionService));
                if (existingCurrency == null)
                    DestroyIfCreated(nameof(CurrencyService));
            }
        }

        static void TestEmptyOptionalLoadout(string root, GameConfigService config)
        {
            SaveManager manager = NewManager(root, "empty");
            PlayerLoadoutService service = new PlayerLoadoutService(manager, config);
            BattleLoadout loadout = service.ResolveBattleLoadout();
            Require(loadout.SelectedHeroId == SaveMigration.StartingHeroId,
                "Empty loadout did not resolve the default Hero.");
            Require(loadout.EquippedPetId == string.Empty
                && loadout.EquippedCompanionId == string.Empty
                && loadout.EquippedSkillIds.Count == 0,
                "Empty optional loadout did not remain empty.");
        }

        static void TestInvalidOptionalIds(string root, HeroData hero,
            PetConfig pet, CompanionConfig companion, SkillConfig skill)
        {
            SaveManager manager = NewManager(root, "invalid-optional");
            PlayerLoadoutSaveData saveLoadout = manager.CurrentSave.loadout;
            saveLoadout.petIds.Add("missing_pet");
            saveLoadout.EquippedCompanionId = "missing_companion";
            saveLoadout.EquippedSkillIds.Add("missing_skill");
            manager.SaveNow();

            PlayerLoadoutService service = CreateService(manager, hero, pet, companion, skill);
            BattleLoadout loadout = service.ResolveBattleLoadout();
            Require(loadout.PetIds.Count == 0 && loadout.CompanionId == string.Empty
                && loadout.SkillIds.Count == 0,
                "Invalid optional IDs were not skipped during BattleLoadout resolution.");
        }

        static void TestDuplicateSkillAndLimits(string root, HeroData hero,
            PetConfig pet, CompanionConfig companion, SkillConfig skill, LoadoutConfig limits)
        {
            SaveManager manager = NewManager(root, "limits");
            PlayerLoadoutService service = CreateService(manager, hero, pet, companion, skill, limits);

            Require(service.EquipPet("pet_first"), "First Pet did not equip.");
            Require(!service.EquipPet("pet_second"), "Pet slot maximum was not enforced.");
            Require(!service.EquipCompanion("companion_first"),
                "Zero Companion slots were not enforced.");
            Require(service.EquipSkill("skill_first"), "First Skill did not equip.");
            Require(!service.EquipSkill("skill_first"), "Duplicate Skill was accepted.");
            Require(service.EquipSkill("skill_second"), "Second Skill did not equip.");
            Require(!service.EquipSkill("skill_third"), "Skill slot maximum was not enforced.");
            Require(service.ValidateLoadout(out _), "A valid within-limit loadout failed validation.");
            manager.CurrentSave.loadout.EquippedCompanionId = "companion_first";
            manager.SaveNow();
            Require(service.ResolveBattleLoadout().EquippedCompanionId == string.Empty,
                "A stale Companion was not omitted when its configured slot maximum was zero.");
        }

        static void TestSaveReload(string root, HeroData hero)
        {
            SaveManager writer = NewManager(root, "save-reload");
            PlayerLoadoutService service = new PlayerLoadoutService(writer,
                id => id == hero.StableId ? hero : null);
            Require(service.EquipPet("pet_persisted"), "Pet could not be equipped.");
            Require(service.EquipCompanion("companion_persisted"),
                "Companion could not be equipped.");
            Require(service.EquipSkill("skill_persisted"), "Skill could not be equipped.");
            service.SaveLoadout();

            SaveManager reader = new SaveManager(Path.GetDirectoryName(writer.MainFilePath));
            GameSaveData reloaded = reader.Reload();
            Require(reloaded.loadout.EquippedPetId == "pet_persisted"
                && reloaded.loadout.EquippedCompanionId == "companion_persisted"
                && reloaded.loadout.EquippedSkillIds.Count == 1
                && reloaded.loadout.EquippedSkillIds[0] == "skill_persisted",
                "Pet, Companion, or Skill loadout did not survive save/reload.");
        }

        static void TestHeroFallback(string root, HeroData hero)
        {
            SaveManager manager = NewManager(root, "hero-fallback");
            manager.CurrentSave.loadout.SelectedHeroId = "invalid_hero";
            manager.CurrentSave.selectedHeroId = "invalid_hero";
            manager.SaveNow();

            PlayerLoadoutService service = new PlayerLoadoutService(manager,
                id => id == hero.StableId ? hero : null);
            BattleLoadout loadout = service.ResolveBattleLoadout();
            Require(loadout.SelectedHeroId == SaveMigration.StartingHeroId
                && manager.CurrentSave.loadout.SelectedHeroId == SaveMigration.StartingHeroId
                && manager.CurrentSave.selectedHeroId == SaveMigration.StartingHeroId,
                "Invalid selected Hero did not fall back to and persist Hero2.");
        }

        static void TestBattleWithoutOptionalDefinitions(string root,
            GameConfigService config, HeroData hero)
        {
            SaveManager manager = NewManager(root, "battle-empty-optional");
            PlayerLoadoutService service = new PlayerLoadoutService(manager, config);
            BattleLoadout loadout = service.ResolveBattleLoadout();
            BossData boss = config != null
                ? config.GetBoss("development_boss")
                : Resources.Load<BossData>("Campaign/Bosses/DevelopmentBoss");
            Require(boss != null, "Development Boss is required for the Battle regression.");

            GameObject battleObject = new GameObject("PlayerLoadoutRegressionBattle");
            try
            {
                BattleController controller = battleObject.AddComponent<BattleController>();
                controller.Configure(null, null, null, bossData: boss,
                    heroData: loadout.Hero,
                    runtimeHeroStats: new RuntimeHeroStats(loadout.Hero, null),
                    runtimeBossStats: new RuntimeBossStats(boss));
                Require(!controller.IsGameOver
                    && controller.HeroMaxHealth == hero.BaseMaxHP
                    && controller.BossMaxHealth == boss.MaxHP,
                    "Battle did not initialize with optional loadout definitions absent.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
            }
        }

        static PlayerLoadoutService CreateService(SaveManager manager, HeroData hero,
            PetConfig pet, CompanionConfig companion, SkillConfig skill,
            LoadoutConfig limits = null)
        {
            return new PlayerLoadoutService(manager,
                id => id == hero.StableId ? hero : null,
                id => id.StartsWith("pet_", StringComparison.Ordinal) ? pet : null,
                id => id.StartsWith("companion_", StringComparison.Ordinal) ? companion : null,
                id => id.StartsWith("skill_", StringComparison.Ordinal) ? skill : null,
                limits);
        }

        static LoadoutConfig CreateLoadoutConfig(int petSlots, int companionSlots, int skillSlots)
        {
            LoadoutConfig config = ScriptableObject.CreateInstance<LoadoutConfig>();
            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("maxPetSlots").intValue = petSlots;
            serialized.FindProperty("maxCompanionSlots").intValue = companionSlots;
            serialized.FindProperty("maxSkillSlots").intValue = skillSlots;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        static SaveManager NewManager(string root, string name)
        {
            SaveManager manager = new SaveManager(Path.Combine(root, name));
            manager.LoadOrCreate();
            return manager;
        }

        static void DestroyIfCreated(string objectName)
        {
            GameObject created = GameObject.Find(objectName);
            if (created != null)
                UnityEngine.Object.DestroyImmediate(created);
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
