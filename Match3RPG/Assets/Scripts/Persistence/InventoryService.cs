using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Persistence
{
    public sealed class InventoryService
    {
        readonly SaveManager saveManager;
        readonly Func<string, ItemConfig> getItem;

        public InventoryService(SaveManager saveManager, GameConfigService configService)
            : this(saveManager, configService != null
                ? new Func<string, ItemConfig>(configService.GetItem)
                : throw new ArgumentNullException(nameof(configService)))
        {
        }

        public InventoryService(SaveManager saveManager, Func<string, ItemConfig> itemResolver)
        {
            this.saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            getItem = itemResolver ?? throw new ArgumentNullException(nameof(itemResolver));
            EnsureSaveLoaded();
        }

        public bool AddStackItem(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                return false;

            ItemConfig item = getItem(itemId);
            if (item == null || item.Type == ItemType.Equipment)
                return false;

            InventoryStackSaveData stack = FindStack(itemId);
            if (stack == null)
            {
                stack = new InventoryStackSaveData { itemId = itemId };
                saveManager.CurrentSave.inventory.stacks.Add(stack);
            }

            try
            {
                stack.quantity = checked(stack.quantity + quantity);
            }
            catch (OverflowException)
            {
                return false;
            }

            SaveChanges();
            return true;
        }

        public bool RemoveStackItem(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                return false;

            InventoryStackSaveData stack = FindStack(itemId);
            if (stack == null || stack.quantity < quantity)
                return false;

            stack.quantity -= quantity;
            if (stack.quantity == 0)
                saveManager.CurrentSave.inventory.stacks.Remove(stack);
            SaveChanges();
            return true;
        }

        public int GetQuantity(string itemId)
        {
            InventoryStackSaveData stack = FindStack(itemId);
            return stack != null ? stack.quantity : 0;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            return quantity > 0 && GetQuantity(itemId) >= quantity;
        }

        public ItemInstanceSaveData AddEquipmentInstance(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Item ID must be a stable, non-empty ID.", nameof(itemId));

            ItemConfig item = getItem(itemId);
            if (!IsEquippable(item))
                throw new InvalidOperationException(
                    $"Item '{itemId}' is missing, is not equipment, or has no equipment slot.");

            string instanceId;
            do
            {
                instanceId = Guid.NewGuid().ToString("N");
            }
            while (GetEquipmentInstance(instanceId) != null);

            ItemInstanceSaveData instance = new ItemInstanceSaveData
            {
                instanceId = instanceId,
                itemId = itemId,
                level = 1,
                upgradeLevel = 0,
                locked = false
            };
            saveManager.CurrentSave.inventory.itemInstances.Add(instance);
            SaveChanges();
            return instance;
        }

        public ItemInstanceSaveData GetEquipmentInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return null;

            foreach (ItemInstanceSaveData instance in saveManager.CurrentSave.inventory.itemInstances)
                if (instance != null && string.Equals(instance.instanceId, instanceId, StringComparison.Ordinal))
                    return instance;
            return null;
        }

        public bool LockEquipmentInstance(string instanceId, bool locked = true)
        {
            ItemInstanceSaveData instance = GetEquipmentInstance(instanceId);
            if (instance == null || instance.locked == locked)
                return false;

            instance.locked = locked;
            SaveChanges();
            return true;
        }

        internal static bool IsEquippable(ItemConfig item)
        {
            return item != null && item.Type == ItemType.Equipment
                && item.EquipmentSlot != EquipmentSlot.None
                && Enum.IsDefined(typeof(EquipmentSlot), item.EquipmentSlot);
        }

        InventoryStackSaveData FindStack(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            foreach (InventoryStackSaveData stack in saveManager.CurrentSave.inventory.stacks)
                if (stack != null && string.Equals(stack.itemId, itemId, StringComparison.Ordinal))
                    return stack;
            return null;
        }

        void EnsureSaveLoaded()
        {
            if (saveManager.CurrentSave == null)
                saveManager.LoadOrCreate();
            saveManager.CurrentSave.inventory = saveManager.CurrentSave.inventory ?? new InventorySaveData();
            saveManager.CurrentSave.inventory.stacks =
                saveManager.CurrentSave.inventory.stacks ?? new List<InventoryStackSaveData>();
            saveManager.CurrentSave.inventory.itemInstances =
                saveManager.CurrentSave.inventory.itemInstances ?? new List<ItemInstanceSaveData>();
        }

        void SaveChanges()
        {
            saveManager.SaveNow();
        }
    }

    public sealed class StarChartGemInventoryService
    {
        const int BaseGemTypeCount = 7;
        const int RealmCount = 14;
        const int StarsPerBranch = 10;

        readonly SaveManager saveManager;

        public StarChartGemInventoryService(SaveManager saveManager)
        {
            this.saveManager = saveManager
                ?? throw new ArgumentNullException(nameof(saveManager));
            if (this.saveManager.CurrentSave == null)
                this.saveManager.LoadOrCreate(
                    Resources.Load<CampaignCatalog>("Campaign/CampaignCatalog"));
        }

        public int StandardDiamonds => GetInventory().standardDiamonds;
        public int SpecialDiamonds => GetInventory().specialDiamonds;

        public int GetBaseGemCount(GemType type)
        {
            int index = GetBaseGemIndex(type);
            return GetInventory().baseGemCounts[index];
        }

        public int GetLitStarCount(int realmIndex, GemType type)
        {
            ValidateRealmIndex(realmIndex);
            int branchIndex = GetBaseGemIndex(type);
            StarChartRealmProgressSaveData realm = FindRealmProgress(realmIndex);
            return realm == null ? 0 : realm.litStarsPerBranch[branchIndex];
        }

        public int[] GetRealmStarCounts(int realmIndex)
        {
            ValidateRealmIndex(realmIndex);
            StarChartRealmProgressSaveData realm = FindRealmProgress(realmIndex);
            return realm == null
                ? new int[BaseGemTypeCount]
                : (int[])realm.litStarsPerBranch.Clone();
        }

        public StarChartBreakthroughStatus GetBreakthroughStatus(
            int realmIndex, HeroProgressSaveData heroProgress)
        {
            StarChartBreakthroughRequirement requirement =
                StarChartBreakthroughRequirement.ForRealm(realmIndex);
            int level = heroProgress != null ? Math.Max(1, heroProgress.level) : 0;
            int litStars = 0;
            foreach (int branchStars in GetRealmStarCounts(realmIndex))
                litStars += branchStars;

            StarChartGemInventorySaveData inventory = GetInventory();
            return new StarChartBreakthroughStatus(requirement,
                level == requirement.RequiredLevel,
                litStars == 70,
                Math.Max(0, requirement.StandardDiamonds - inventory.standardDiamonds),
                Math.Max(0, requirement.SpecialDiamonds - inventory.specialDiamonds),
                litStars);
        }

        public bool TryBreakthrough(int realmIndex, HeroProgressSaveData heroProgress,
            out StarChartBreakthroughStatus status)
        {
            status = GetBreakthroughStatus(realmIndex, heroProgress);
            if (!status.CanBreakthrough || heroProgress == null
                || !GetSaveContainsHeroProgress(heroProgress))
                return false;

            StarChartGemInventorySaveData inventory = GetInventory();
            inventory.standardDiamonds -= status.Requirement.StandardDiamonds;
            inventory.specialDiamonds -= status.Requirement.SpecialDiamonds;
            heroProgress.level = status.Requirement.TargetLevel;
            saveManager.SaveNow();
            return true;
        }

        public int GetStarCost(int realmIndex, int starIndex)
        {
            ValidateRealmIndex(realmIndex);
            if (starIndex < 0 || starIndex >= StarsPerBranch)
                throw new ArgumentOutOfRangeException(nameof(starIndex));

            long cost = realmIndex + 1L;
            for (int index = 0; index < starIndex; index++)
                cost = Math.Min(int.MaxValue, (cost * 3L + 1L) / 2L);
            return (int)cost;
        }

        public bool TryLightNextStar(int realmIndex, GemType type,
            out int cost, out int missingGems)
        {
            ValidateRealmIndex(realmIndex);
            int branchIndex = GetBaseGemIndex(type);
            StarChartRealmProgressSaveData realm = FindRealmProgress(realmIndex);
            int starCount = realm == null ? 0 : realm.litStarsPerBranch[branchIndex];
            cost = starCount < StarsPerBranch
                ? GetStarCost(realmIndex, starCount)
                : 0;
            int gemCount = GetInventory().baseGemCounts[branchIndex];
            missingGems = Math.Max(0, cost - gemCount);
            if (starCount >= StarsPerBranch || missingGems > 0)
                return false;

            if (realm == null)
            {
                realm = new StarChartRealmProgressSaveData
                {
                    realmIndex = realmIndex
                };
                GetInventory().realmProgress.Add(realm);
            }

            GetInventory().baseGemCounts[branchIndex] -= cost;
            realm.litStarsPerBranch[branchIndex]++;
            saveManager.SaveNow();
            return true;
        }

        public StarChartStatBonuses GetTotalStatBonuses()
        {
            int[] totals = new int[BaseGemTypeCount];
            foreach (StarChartRealmProgressSaveData realm in GetInventory().realmProgress)
                if (realm != null && realm.litStarsPerBranch != null)
                    for (int index = 0; index < totals.Length
                        && index < realm.litStarsPerBranch.Length; index++)
                        totals[index] = checked(totals[index]
                            + Math.Max(0, realm.litStarsPerBranch[index]));
            return new StarChartStatBonuses(totals[0], totals[1], totals[2],
                totals[3], totals[4], totals[5], totals[6]);
        }

        public void CollectMatch(MoveResult move)
        {
            if (move == null)
                throw new ArgumentNullException(nameof(move));

            StarChartGemInventorySaveData inventory = GetInventory();
            int[] updatedBaseGemCounts = (int[])inventory.baseGemCounts.Clone();
            bool hasRewards = move.StandardDiamondReward > 0
                || move.SpecialDiamondReward > 0;
            foreach (KeyValuePair<GemType, int> pair in move.BaseGemMatchCounts)
            {
                if (pair.Value <= 0)
                    continue;
                int index = GetBaseGemIndex(pair.Key);
                updatedBaseGemCounts[index] = checked(updatedBaseGemCounts[index] + pair.Value);
                hasRewards = true;
            }

            if (!hasRewards)
                return;

            int updatedStandardDiamonds = checked(inventory.standardDiamonds
                + move.StandardDiamondReward);
            int updatedSpecialDiamonds = checked(inventory.specialDiamonds
                + move.SpecialDiamondReward);
            inventory.baseGemCounts = updatedBaseGemCounts;
            inventory.standardDiamonds = updatedStandardDiamonds;
            inventory.specialDiamonds = updatedSpecialDiamonds;
            saveManager.SaveNow();
        }

        StarChartGemInventorySaveData GetInventory()
        {
            return saveManager.CurrentSave.starChartGemInventory;
        }

        StarChartRealmProgressSaveData FindRealmProgress(int realmIndex)
        {
            foreach (StarChartRealmProgressSaveData realm in GetInventory().realmProgress)
                if (realm != null && realm.realmIndex == realmIndex)
                    return realm;
            return null;
        }

        bool GetSaveContainsHeroProgress(HeroProgressSaveData heroProgress)
        {
            if (heroProgress == null || saveManager.CurrentSave.heroes == null)
                return false;
            foreach (HeroProgressSaveData savedProgress in saveManager.CurrentSave.heroes)
                if (ReferenceEquals(savedProgress, heroProgress))
                    return true;
            return false;
        }

        static void ValidateRealmIndex(int realmIndex)
        {
            if (realmIndex < 0 || realmIndex >= RealmCount)
                throw new ArgumentOutOfRangeException(nameof(realmIndex));
        }

        static int GetBaseGemIndex(GemType type)
        {
            int index = (int)type;
            if (index < 0 || index >= BaseGemTypeCount)
                throw new ArgumentOutOfRangeException(nameof(type),
                    $"{type} is not one of the seven base gem types.");
            return index;
        }
    }

    public readonly struct StarChartStatBonuses
    {
        public int AttackStars { get; }
        public int ManaStars { get; }
        public int ExperienceStars { get; }
        public int RageStars { get; }
        public int HealthStars { get; }
        public int ShieldStars { get; }
        public int SoulStars { get; }

        public StarChartStatBonuses(int attackStars, int manaStars,
            int experienceStars, int rageStars, int healthStars,
            int shieldStars, int soulStars)
        {
            AttackStars = attackStars;
            ManaStars = manaStars;
            ExperienceStars = experienceStars;
            RageStars = rageStars;
            HealthStars = healthStars;
            ShieldStars = shieldStars;
            SoulStars = soulStars;
        }
    }

    public readonly struct StarChartBreakthroughRequirement
    {
        static readonly int[] StandardCosts =
        {
            500, 2000, 8000, 25000, 70000, 180000, 400000,
            900000, 2000000, 5000000, 12000000, 30000000, 80000000
        };

        static readonly int[] SpecialCosts =
        {
            10, 25, 45, 70, 100, 140, 190, 250, 320, 400, 500, 620, 800
        };

        public int RealmIndex { get; }
        public int RequiredLevel { get; }
        public int TargetLevel { get; }
        public int StandardDiamonds { get; }
        public int SpecialDiamonds { get; }
        public string CurrentRealmName { get; }
        public string NextRealmName { get; }

        StarChartBreakthroughRequirement(int realmIndex, int requiredLevel,
            int targetLevel, int standardDiamonds, int specialDiamonds,
            string currentRealmName, string nextRealmName)
        {
            RealmIndex = realmIndex;
            RequiredLevel = requiredLevel;
            TargetLevel = targetLevel;
            StandardDiamonds = standardDiamonds;
            SpecialDiamonds = specialDiamonds;
            CurrentRealmName = currentRealmName;
            NextRealmName = nextRealmName;
        }

        public static StarChartBreakthroughRequirement ForRealm(int realmIndex)
        {
            string[] realmNames =
            {
                "Luyện Khí", "Trúc Cơ", "Kết Đan", "Nguyên Anh",
                "Hóa Thần", "Luyện Hư", "Hợp Thể", "Đại Thừa",
                "Độ Kiếp", "Chân Tiên", "Kim Tiên", "Thái Ất", "Đại La",
                "Đạo Tổ"
            };
            if (realmIndex < 0 || realmIndex >= StandardCosts.Length)
                throw new ArgumentOutOfRangeException(nameof(realmIndex),
                    "Đạo Tổ is the final realm and has no breakthrough.");

            return new StarChartBreakthroughRequirement(realmIndex,
                realmIndex == 0 ? 9 : (realmIndex + 1) * 10 - 1,
                (realmIndex + 1) * 10,
                StandardCosts[realmIndex], SpecialCosts[realmIndex],
                realmNames[realmIndex], realmNames[realmIndex + 1]);
        }
    }

    public readonly struct StarChartBreakthroughStatus
    {
        public StarChartBreakthroughRequirement Requirement { get; }
        public bool LevelReady { get; }
        public bool StarsReady { get; }
        public int MissingStandardDiamonds { get; }
        public int MissingSpecialDiamonds { get; }
        public int LitStarCount { get; }
        public bool MaterialsReady => MissingStandardDiamonds == 0
            && MissingSpecialDiamonds == 0;
        public bool CanBreakthrough => LevelReady && StarsReady && MaterialsReady;

        public StarChartBreakthroughStatus(
            StarChartBreakthroughRequirement requirement, bool levelReady,
            bool starsReady, int missingStandardDiamonds,
            int missingSpecialDiamonds, int litStarCount)
        {
            Requirement = requirement;
            LevelReady = levelReady;
            StarsReady = starsReady;
            MissingStandardDiamonds = missingStandardDiamonds;
            MissingSpecialDiamonds = missingSpecialDiamonds;
            LitStarCount = litStarCount;
        }
    }
}
