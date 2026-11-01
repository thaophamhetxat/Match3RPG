using System;
using System.Collections.Generic;
using Match3RPG.Battle;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Persistence
{
    public sealed class PlayerLoadoutService
    {
        const int DefaultMaxPetSlots = 1;
        const int DefaultMaxCompanionSlots = 1;
        const int DefaultMaxSkillSlots = 4;

        readonly SaveManager saveManager;
        readonly LoadoutConfig loadoutConfig;
        readonly Func<string, HeroData> getHero;
        readonly Func<string, PetConfig> getPet;
        readonly Func<string, CompanionConfig> getCompanion;
        readonly Func<string, SkillConfig> getSkill;

        public PlayerLoadoutService(SaveManager saveManager, GameConfigService configService,
            LoadoutConfig loadoutConfig = null)
        {
            this.saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            this.loadoutConfig = loadoutConfig != null
                ? loadoutConfig
                : configService != null ? configService.Loadout : null;
            getHero = configService != null && configService.Heroes != null
                ? new Func<string, HeroData>(configService.GetHero)
                : null;
            getPet = configService != null && configService.Pets != null
                ? new Func<string, PetConfig>(configService.GetPet)
                : null;
            getCompanion = configService != null && configService.Companions != null
                ? new Func<string, CompanionConfig>(configService.GetCompanion)
                : null;
            getSkill = configService != null && configService.Skills != null
                ? new Func<string, SkillConfig>(configService.GetSkill)
                : null;
            EnsureSaveLoaded();
        }

        public PlayerLoadoutService(SaveManager saveManager,
            Func<string, HeroData> heroResolver,
            Func<string, PetConfig> petResolver = null,
            Func<string, CompanionConfig> companionResolver = null,
            Func<string, SkillConfig> skillResolver = null,
            LoadoutConfig loadoutConfig = null)
        {
            this.saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            this.loadoutConfig = loadoutConfig;
            getHero = heroResolver;
            getPet = petResolver;
            getCompanion = companionResolver;
            getSkill = skillResolver;
            EnsureSaveLoaded();
        }

        public bool EquipPet(string petId)
        {
            PlayerLoadoutSaveData loadout = CurrentLoadout;
            if (!TryValidateOptionalId(petId, getPet, "Pet"))
                return false;

            List<string> petIds = loadout.petIds;
            if (petIds.Contains(petId))
                return false;
            if (petIds.Count >= MaxPetSlots)
            {
                Debug.LogWarning($"Cannot equip Pet '{petId}': maximum Pet slots ({MaxPetSlots}) reached.");
                return false;
            }

            petIds.Add(petId);
            SaveLoadout();
            return true;
        }

        public bool UnequipPet()
        {
            List<string> petIds = CurrentLoadout.petIds;
            if (petIds.Count == 0)
                return false;
            petIds.Clear();
            SaveLoadout();
            return true;
        }

        public bool UnequipPet(string petId)
        {
            if (string.IsNullOrEmpty(petId))
                return false;

            bool removed = CurrentLoadout.petIds.Remove(petId);
            if (removed)
                SaveLoadout();
            return removed;
        }

        public bool EquipCompanion(string companionId)
        {
            if (MaxCompanionSlots == 0)
            {
                Debug.LogWarning("Cannot equip a Companion: LoadoutConfig allows no Companion slots.");
                return false;
            }
            if (!TryValidateOptionalId(companionId, getCompanion, "Companion"))
                return false;

            CurrentLoadout.EquippedCompanionId = companionId;
            SaveLoadout();
            return true;
        }

        public bool UnequipCompanion()
        {
            PlayerLoadoutSaveData loadout = CurrentLoadout;
            if (string.IsNullOrEmpty(loadout.EquippedCompanionId))
                return false;
            loadout.EquippedCompanionId = string.Empty;
            SaveLoadout();
            return true;
        }

        public bool EquipSkill(string skillId)
        {
            if (!TryValidateOptionalId(skillId, getSkill, "Skill"))
                return false;

            List<string> skillIds = CurrentLoadout.EquippedSkillIds;
            if (skillIds.Contains(skillId))
            {
                Debug.LogWarning($"Cannot equip Skill '{skillId}': it is already equipped.");
                return false;
            }
            if (skillIds.Count >= MaxSkillSlots)
            {
                Debug.LogWarning($"Cannot equip Skill '{skillId}': maximum Skill slots ({MaxSkillSlots}) reached.");
                return false;
            }

            skillIds.Add(skillId);
            SaveLoadout();
            return true;
        }

        public bool UnequipSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return false;

            bool removed = CurrentLoadout.EquippedSkillIds.Remove(skillId);
            if (removed)
                SaveLoadout();
            return removed;
        }

        public bool ValidateLoadout(out IReadOnlyList<string> errors)
        {
            List<string> validationErrors = new List<string>();
            PlayerLoadoutSaveData loadout = CurrentLoadout;
            ResolveSelectedHero(loadout, true);

            if (loadout.petIds.Count > MaxPetSlots)
                validationErrors.Add(
                    $"Equipped Pet count {loadout.petIds.Count} exceeds the limit of {MaxPetSlots}.");
            if (!string.IsNullOrEmpty(loadout.EquippedCompanionId) && MaxCompanionSlots == 0)
                validationErrors.Add("A Companion is equipped but LoadoutConfig allows no Companion slots.");
            if (loadout.EquippedSkillIds.Count > MaxSkillSlots)
                validationErrors.Add(
                    $"Equipped Skill count {loadout.EquippedSkillIds.Count} exceeds the limit of {MaxSkillSlots}.");

            HashSet<string> uniqueSkillIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string skillId in loadout.EquippedSkillIds)
                if (!uniqueSkillIds.Add(skillId))
                    validationErrors.Add($"Skill '{skillId}' is equipped more than once.");

            ValidateOptionalIds(loadout.petIds, getPet, "Pet");
            ValidateOptionalId(loadout.EquippedCompanionId, getCompanion, "Companion");
            ValidateOptionalIds(loadout.EquippedSkillIds, getSkill, "Skill");

            errors = validationErrors.AsReadOnly();
            return validationErrors.Count == 0;
        }

        public BattleLoadout ResolveBattleLoadout()
        {
            PlayerLoadoutSaveData loadout = CurrentLoadout;
            HeroData selectedHero = ResolveSelectedHero(loadout, true);
            List<string> petIds = ResolveValidOptionalIds(
                loadout.petIds, getPet, "Pet", MaxPetSlots);
            string companionId = string.Empty;
            if (MaxCompanionSlots > 0)
                companionId = ResolveValidOptionalId(
                    loadout.EquippedCompanionId, getCompanion, "Companion");
            else if (!string.IsNullOrEmpty(loadout.EquippedCompanionId))
                Debug.LogWarning($"Skipping Companion '{loadout.EquippedCompanionId}': "
                    + "LoadoutConfig allows no Companion slots.");
            List<string> skillIds = ResolveValidOptionalIds(
                loadout.EquippedSkillIds, getSkill, "Skill", MaxSkillSlots);

            return new BattleLoadout(selectedHero, petIds, companionId, skillIds);
        }

        public void SaveLoadout()
        {
            EnsureSaveLoaded();
            saveManager.SaveNow();
        }

        int MaxPetSlots => loadoutConfig != null
            ? loadoutConfig.MaxPetSlots
            : DefaultMaxPetSlots;
        int MaxCompanionSlots => loadoutConfig != null
            ? loadoutConfig.MaxCompanionSlots
            : DefaultMaxCompanionSlots;
        int MaxSkillSlots => loadoutConfig != null
            ? loadoutConfig.MaxSkillSlots
            : DefaultMaxSkillSlots;

        PlayerLoadoutSaveData CurrentLoadout
        {
            get
            {
                EnsureSaveLoaded();
                return saveManager.CurrentSave.loadout;
            }
        }

        HeroData ResolveSelectedHero(PlayerLoadoutSaveData loadout, bool saveCorrection)
        {
            string selectedHeroId = loadout.SelectedHeroId;
            if (string.IsNullOrEmpty(selectedHeroId))
                selectedHeroId = saveManager.CurrentSave.selectedHeroId;
            HeroData hero = getHero != null ? getHero(selectedHeroId) : null;
            if (hero == null)
            {
                hero = getHero != null ? getHero(SaveMigration.StartingHeroId) : null;
                if (hero == null)
                    hero = PlayerHeroSelectionService.LoadHeroData();
            }

            if (hero != null)
            {
                bool corrected = selectedHeroId != hero.StableId
                    || loadout.SelectedHeroId != hero.StableId
                    || saveManager.CurrentSave.selectedHeroId != hero.StableId;
                if (selectedHeroId != hero.StableId)
                    Debug.LogWarning($"Selected HeroId '{selectedHeroId}' is invalid; "
                        + $"using Hero2 '{hero.StableId}'.");
                loadout.SelectedHeroId = hero.StableId;
                saveManager.CurrentSave.selectedHeroId = hero.StableId;
                if (corrected && saveCorrection)
                    saveManager.SaveNow();
            }

            if (hero == null)
                Debug.LogError($"Selected HeroId '{selectedHeroId}' and Hero2 fallback could not be resolved.");
            return hero;
        }

        bool TryValidateOptionalId<T>(string stableId, Func<string, T> resolver, string typeName)
            where T : UnityEngine.Object
        {
            if (!StableIdRules.IsValid(stableId))
            {
                Debug.LogWarning($"Cannot equip {typeName}: '{stableId}' is not a valid stable ID.");
                return false;
            }
            if (resolver != null && resolver(stableId) == null)
            {
                Debug.LogWarning($"Cannot equip {typeName} '{stableId}': no matching definition exists.");
                return false;
            }
            return true;
        }

        static void ValidateOptionalIds<T>(IReadOnlyList<string> ids,
            Func<string, T> resolver, string typeName) where T : UnityEngine.Object
        {
            foreach (string id in ids)
                ValidateOptionalId(id, resolver, typeName);
        }

        static void ValidateOptionalId<T>(string id, Func<string, T> resolver, string typeName)
            where T : UnityEngine.Object
        {
            if (!string.IsNullOrEmpty(id) && !StableIdRules.IsValid(id))
                Debug.LogWarning($"Invalid {typeName} ID '{id}' in PlayerLoadout; it will be skipped.");
            else if (!string.IsNullOrEmpty(id) && resolver == null)
                Debug.LogWarning($"No {typeName} database is configured; ID '{id}' will be skipped.");
            else if (!string.IsNullOrEmpty(id) && resolver != null && resolver(id) == null)
                Debug.LogWarning($"Unknown {typeName} ID '{id}' in PlayerLoadout; it will be skipped.");
        }

        static List<string> ResolveValidOptionalIds<T>(IReadOnlyList<string> ids,
            Func<string, T> resolver, string typeName, int maximumCount)
            where T : UnityEngine.Object
        {
            List<string> validIds = new List<string>(Math.Min(ids.Count, maximumCount));
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                if (validIds.Count >= maximumCount)
                {
                    Debug.LogWarning($"Skipping {typeName} '{id}': the configured slot limit is "
                        + $"{maximumCount}.");
                    continue;
                }
                if (!StableIdRules.IsValid(id) || !seen.Add(id))
                {
                    Debug.LogWarning($"Skipping invalid or duplicate {typeName} ID '{id}'.");
                    continue;
                }
                if (resolver == null)
                {
                    Debug.LogWarning($"Skipping {typeName} '{id}': no {typeName} database is configured.");
                    continue;
                }
                if (resolver(id) == null)
                {
                    Debug.LogWarning($"Skipping unknown {typeName} ID '{id}'.");
                    continue;
                }
                validIds.Add(id);
            }
            return validIds;
        }

        string ResolveValidOptionalId<T>(string id, Func<string, T> resolver, string typeName)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;
            if (!StableIdRules.IsValid(id))
            {
                Debug.LogWarning($"Skipping invalid {typeName} ID '{id}'.");
                return string.Empty;
            }
            if (resolver == null)
            {
                Debug.LogWarning($"Skipping {typeName} '{id}': no {typeName} database is configured.");
                return string.Empty;
            }
            if (resolver(id) == null)
            {
                Debug.LogWarning($"Skipping unknown {typeName} ID '{id}'.");
                return string.Empty;
            }
            return id;
        }

        void EnsureSaveLoaded()
        {
            if (saveManager.CurrentSave == null)
                saveManager.LoadOrCreate();

            GameSaveData save = saveManager.CurrentSave;
            save.loadout = save.loadout ?? new PlayerLoadoutSaveData();
            save.loadout.petIds = save.loadout.petIds ?? new List<string>();
            save.loadout.EquippedSkillIds = save.loadout.EquippedSkillIds ?? new List<string>();
        }
    }
}
