using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Match3RPG.Battle;
using Match3RPG.Core;
using Match3RPG.Persistence;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class SaveLayerRegressionTests
    {
        [MenuItem("Tools/Match3RPG/Save/Run Save Layer Tests")]
        public static void RunAll()
        {
            string testDirectory = Path.Combine(
                Path.GetTempPath(), "Match3RPG_SaveLayerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            try
            {
                TestNewSave(testDirectory);
                TestSaveAndReload(testDirectory);
                TestEmptyCollections(testDirectory);
                TestCorruptMainRecovery(testDirectory);
                TestStarChartMatchRewards(testDirectory);
                TestStarChartPurchases(testDirectory);
                TestStarChartBreakthroughs(testDirectory);
                Debug.Log("Save-layer regression tests passed: new save, save/reload, "
                    + "empty collections, corrupt-main recovery, match rewards, star purchases, "
                    + "and breakthroughs.");
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                    Directory.Delete(testDirectory, true);
            }
        }

        static void TestNewSave(string root)
        {
            SaveManager manager = new SaveManager(Path.Combine(root, "new-save"));
            GameSaveData save = manager.CreateNewSave();
            Require(save.saveVersion == SaveMigration.CurrentSaveVersion, "New save version was incorrect.");
            Require(save.selectedHeroId == SaveMigration.StartingHeroId, "New save did not select Hero2.");
            Require(save.heroes.Count == 1
                && save.heroes[0].heroId == SaveMigration.StartingHeroId
                && save.heroes[0].level == 1, "New save did not initialize Hero2 at Level 1.");
            Require(File.Exists(manager.MainFilePath), "New save main file was not created.");
        }

        static void TestSaveAndReload(string root)
        {
            string directory = Path.Combine(root, "save-reload");
            SaveManager writer = new SaveManager(directory);
            GameSaveData save = SaveMigration.CreateNewGame();
            save.heroes[0].experience = 275;
            save.campaign.lastSelectedRegionId = "region_03";
            writer.Save(save);

            SaveManager reader = new SaveManager(directory);
            GameSaveData reloaded = reader.Reload();
            Require(reloaded.heroes[0].experience == 275, "Hero experience did not survive reload.");
            Require(reloaded.campaign.lastSelectedRegionId == "region_03",
                "Campaign region did not survive reload.");
        }

        static void TestEmptyCollections(string root)
        {
            SaveManager manager = new SaveManager(Path.Combine(root, "empty-collections"));
            GameSaveData save = new GameSaveData
            {
                saveVersion = SaveMigration.CurrentSaveVersion,
                heroes = { new HeroProgressSaveData { heroId = SaveMigration.StartingHeroId, level = 1 } },
                pets = null,
                companions = null,
                skills = null,
                inventory = null
            };
            manager.Save(save);
            GameSaveData reloaded = manager.Reload();
            Require(reloaded.pets.Count == 0, "Empty Pet collection did not remain valid.");
            Require(reloaded.companions.Count == 0, "Empty Companion collection did not remain valid.");
            Require(reloaded.skills.Count == 0, "Empty Skill collection did not remain valid.");
            Require(reloaded.inventory.stacks.Count == 0, "Empty inventory stacks did not remain valid.");
            Require(reloaded.inventory.itemInstances.Count == 0,
                "Empty inventory instances did not remain valid.");
        }

        static void TestCorruptMainRecovery(string root)
        {
            string directory = Path.Combine(root, "backup-recovery");
            SaveManager manager = new SaveManager(directory);
            GameSaveData firstSave = SaveMigration.CreateNewGame();
            firstSave.campaign.lastSelectedRegionId = "region_03";
            firstSave.heroes[0].experience = 425;
            manager.Save(firstSave);

            GameSaveData secondSave = SaveMigration.CreateNewGame();
            secondSave.campaign.lastSelectedRegionId = "region_02";
            manager.Save(secondSave);
            File.WriteAllText(manager.MainFilePath, "{ definitely not valid json");

            SaveManager recoveryManager = new SaveManager(directory);
            GameSaveData recovered = recoveryManager.Reload();
            Require(recovered.campaign.lastSelectedRegionId == "region_03"
                && recovered.heroes[0].experience == 425,
                "Corrupt main save did not recover the previous save from backup.");
            Require(File.Exists(recoveryManager.MainFilePath),
                "Recovered backup was not restored to the main save path.");
        }

        static void TestStarChartMatchRewards(string root)
        {
            StarChartMatchRewards match3 = StarChartMatchRewardCalculator.Calculate(
                CreateLineMatch(GemType.Attack, 3));
            Require(match3.StandardDiamonds == 3 && match3.SpecialDiamonds == 0,
                "A match-3 did not award exactly 3 standard diamonds.");

            StarChartMatchRewards match4 = StarChartMatchRewardCalculator.Calculate(
                CreateLineMatch(GemType.Mana, 4));
            Require(match4.StandardDiamonds == 0 && match4.SpecialDiamonds == 1,
                "A match-4 did not award exactly 1 special diamond.");

            StarChartMatchRewards match5 = StarChartMatchRewardCalculator.Calculate(
                CreateLineMatch(GemType.ExpDrain, 5));
            StarChartMatchRewards match6 = StarChartMatchRewardCalculator.Calculate(
                CreateLineMatch(GemType.Rage, 6));
            Require(match5.StandardDiamonds == 0 && match5.SpecialDiamonds == 2
                && match6.StandardDiamonds == 0 && match6.SpecialDiamonds == 2,
                "A match-5 and match-6+ must each award exactly 2 special diamonds.");

            StarChartMatchRewards lShape = StarChartMatchRewardCalculator.Calculate(
                CreateIntersectingMatch(GemType.Health, false));
            StarChartMatchRewards tShape = StarChartMatchRewardCalculator.Calculate(
                CreateIntersectingMatch(GemType.Shield, true));
            Require(lShape.StandardDiamonds == 0 && lShape.SpecialDiamonds == 2
                && lShape.BaseGemCounts[GemType.Health] == 5,
                "An L match did not award 2 special diamonds and count its 5 unique gems.");
            Require(tShape.StandardDiamonds == 0 && tShape.SpecialDiamonds == 2
                && tShape.BaseGemCounts[GemType.Shield] == 5,
                "A T match did not award 2 special diamonds and count its 5 unique gems.");

            MoveResult cascade = new MoveResult(new BoardPos(0, 0), new BoardPos(0, 1));
            AddResolvedMatch(cascade, CreateLineMatch(GemType.Attack, 3));
            AddResolvedMatch(cascade, CreateLineMatch(GemType.Mana, 5));
            Require(cascade.StandardDiamondReward == 3
                && cascade.SpecialDiamondReward == 2
                && cascade.BaseGemMatchCounts[GemType.Attack] == 3
                && cascade.BaseGemMatchCounts[GemType.Mana] == 5,
                "Cascade match rewards or per-base gem counts were not accumulated.");

            string directory = Path.Combine(root, "star-chart-gems");
            SaveManager manager = new SaveManager(directory);
            manager.CreateNewSave();
            StarChartGemInventoryService inventory =
                new StarChartGemInventoryService(manager);
            inventory.CollectMatch(cascade);

            SaveManager reader = new SaveManager(directory);
            reader.Reload();
            StarChartGemInventoryService savedInventory =
                new StarChartGemInventoryService(reader);
            Require(savedInventory.StandardDiamonds == 3
                && savedInventory.SpecialDiamonds == 2
                && savedInventory.GetBaseGemCount(GemType.Attack) == 3
                && savedInventory.GetBaseGemCount(GemType.Mana) == 5,
                "Match rewards and base gem inventory did not persist together.");
        }

        static void TestStarChartPurchases(string root)
        {
            string directory = Path.Combine(root, "star-chart-purchases");
            SaveManager manager = new SaveManager(directory);
            manager.CreateNewSave();
            manager.CurrentSave.starChartGemInventory.baseGemCounts[0] = 179;
            manager.CurrentSave.starChartGemInventory.baseGemCounts[1] = 2;
            manager.SaveNow();
            StarChartGemInventoryService inventory =
                new StarChartGemInventoryService(manager);
            GameSaveData previousVersionSave = SaveMigration.CreateNewGame();
            previousVersionSave.saveVersion = 3;
            previousVersionSave.starChartGemInventory.baseGemCounts[2] = 17;
            GameSaveData migratedSave = SaveMigration.Migrate(previousVersionSave);
            Require(migratedSave.saveVersion == SaveMigration.CurrentSaveVersion
                && migratedSave.starChartGemInventory.baseGemCounts[2] == 17
                && migratedSave.starChartGemInventory.realmProgress.Count == 0,
                "Save version 3 migration did not preserve the existing gem inventory.");

            int[] firstRealmCosts = { 1, 2, 3, 5, 8, 12, 18, 27, 41, 62 };
            for (int starIndex = 0; starIndex < firstRealmCosts.Length; starIndex++)
                Require(inventory.GetStarCost(0, starIndex) == firstRealmCosts[starIndex],
                    $"Luyện Khí star {starIndex + 1} cost was incorrect.");
            Require(inventory.GetStarCost(1, 0) == 2
                && inventory.GetStarCost(1, 9) == 93,
                "Trúc Cơ star costs did not start at 2 and end at 93.");

            for (int starIndex = 0; starIndex < firstRealmCosts.Length; starIndex++)
            {
                bool purchased = inventory.TryLightNextStar(0, GemType.Attack,
                    out int cost, out int missing);
                Require(purchased && cost == firstRealmCosts[starIndex] && missing == 0,
                    $"Luyện Khí star {starIndex + 1} could not be purchased at its cost.");
            }
            Require(inventory.GetLitStarCount(0, GemType.Attack) == 10
                && inventory.GetBaseGemCount(GemType.Attack) == 0,
                "Lighting ten stars did not spend the exact 179 Attack gems.");
            Require(!inventory.TryLightNextStar(0, GemType.Attack,
                    out int completedCost, out int completedMissing)
                && completedCost == 0 && completedMissing == 0,
                "A completed branch allowed an eleventh star.");

            bool foundationRealmPurchased = inventory.TryLightNextStar(1, GemType.Attack,
                out int foundationRealmCost, out int foundationRealmMissing);
            Require(foundationRealmPurchased && foundationRealmCost == 2
                && foundationRealmMissing == 0,
                "The first Trúc Cơ Attack star did not cost 2 gems.");
            Require(inventory.GetTotalStatBonuses().AttackStars == 11,
                "Attack star bonuses did not aggregate across realm tabs.");

            bool insufficientPurchase = inventory.TryLightNextStar(2, GemType.Mana,
                out int insufficientCost, out int missingGems);
            Require(!insufficientPurchase && insufficientCost == 3 && missingGems == 1
                && inventory.GetBaseGemCount(GemType.Mana) == 2
                && inventory.GetLitStarCount(2, GemType.Mana) == 0,
                "An insufficient purchase spent gems or lit a star.");

            SaveManager reader = new SaveManager(directory);
            reader.Reload();
            StarChartGemInventoryService reloadedInventory =
                new StarChartGemInventoryService(reader);
            Require(reloadedInventory.GetLitStarCount(0, GemType.Attack) == 10
                && reloadedInventory.GetLitStarCount(1, GemType.Attack) == 1
                && reloadedInventory.GetBaseGemCount(GemType.Attack) == 0,
                "Star progress and spent gem balances did not persist.");
        }

        static void TestStarChartBreakthroughs(string root)
        {
            string directory = Path.Combine(root, "star-chart-breakthroughs");
            SaveManager manager = new SaveManager(directory);
            GameSaveData save = manager.CreateNewSave();
            HeroProgressSaveData progress = save.heroes[0];
            StarChartGemInventoryService inventory =
                new StarChartGemInventoryService(manager);
            int[] standardCosts =
            {
                500, 2000, 8000, 25000, 70000, 180000, 400000,
                900000, 2000000, 5000000, 12000000, 30000000, 80000000
            };
            int[] specialCosts =
            {
                10, 25, 45, 70, 100, 140, 190, 250, 320, 400, 500, 620, 800
            };
            for (int realm = 0; realm < standardCosts.Length; realm++)
            {
                StarChartBreakthroughRequirement requirement =
                    StarChartBreakthroughRequirement.ForRealm(realm);
                Require(requirement.StandardDiamonds == standardCosts[realm]
                    && requirement.SpecialDiamonds == specialCosts[realm]
                    && requirement.TargetLevel == (realm + 1) * 10
                    && requirement.RequiredLevel == (realm == 0 ? 9 : realm * 10 + 9),
                    $"Breakthrough requirement for realm index {realm} was incorrect.");
            }

            progress.level = 8;
            FillRealmStars(save, 0);
            save.starChartGemInventory.standardDiamonds = standardCosts[0];
            save.starChartGemInventory.specialDiamonds = specialCosts[0];
            Require(!inventory.TryBreakthrough(0, progress, out StarChartBreakthroughStatus levelShortage)
                && !levelShortage.LevelReady && levelShortage.StarsReady
                && levelShortage.MaterialsReady && progress.level == 8,
                "Breakthrough succeeded without the required maximum level.");

            progress.level = 9;
            save.starChartGemInventory.realmProgress.Clear();
            Require(!inventory.TryBreakthrough(0, progress, out StarChartBreakthroughStatus starShortage)
                && starShortage.LevelReady && !starShortage.StarsReady
                && progress.level == 9,
                "Breakthrough succeeded without all 70 current-realm stars.");

            FillRealmStars(save, 0);
            save.starChartGemInventory.standardDiamonds = standardCosts[0] - 1;
            Require(!inventory.TryBreakthrough(0, progress, out StarChartBreakthroughStatus gemShortage)
                && gemShortage.MissingStandardDiamonds == 1
                && gemShortage.MissingSpecialDiamonds == 0
                && progress.level == 9,
                "A missing standard diamond did not block breakthrough.");

            save.starChartGemInventory.standardDiamonds = standardCosts[0];
            save.starChartGemInventory.specialDiamonds = specialCosts[0] - 1;
            Require(!inventory.TryBreakthrough(0, progress, out StarChartBreakthroughStatus specialShortage)
                && specialShortage.MissingStandardDiamonds == 0
                && specialShortage.MissingSpecialDiamonds == 1
                && progress.level == 9,
                "A missing special diamond did not block breakthrough.");

            save.starChartGemInventory.standardDiamonds = standardCosts[0];
            save.starChartGemInventory.specialDiamonds = specialCosts[0];
            Require(inventory.TryBreakthrough(0, progress, out StarChartBreakthroughStatus first)
                && progress.level == 10
                && save.starChartGemInventory.standardDiamonds == 0
                && save.starChartGemInventory.specialDiamonds == 0
                && first.Requirement.NextRealmName == "Trúc Cơ",
                "The first breakthrough did not move Lv9 to Lv10 or deduct exact costs.");
            Require(inventory.GetLitStarCount(0, GemType.Attack) == 10
                && inventory.GetLitStarCount(1, GemType.Attack) == 0,
                "Realm-A progress was not retained separately from the new realm tab.");

            progress.level = 19;
            FillRealmStars(save, 1);
            save.starChartGemInventory.standardDiamonds = standardCosts[1];
            save.starChartGemInventory.specialDiamonds = specialCosts[1];
            Require(inventory.TryBreakthrough(1, progress, out StarChartBreakthroughStatus second)
                && progress.level == 20
                && save.starChartGemInventory.standardDiamonds == 0
                && save.starChartGemInventory.specialDiamonds == 0
                && second.Requirement.NextRealmName == "Kết Đan",
                "The second breakthrough did not move Lv19 to Lv20 or deduct exact costs.");
            Require(inventory.GetLitStarCount(0, GemType.Attack) == 10
                && inventory.GetLitStarCount(1, GemType.Attack) == 10
                && inventory.GetTotalStatBonuses().AttackStars == 20,
                "Star bonuses did not sum across the separately persisted realm tabs.");

            HeroProgressSaveData cappedProgress = new HeroProgressSaveData
            {
                heroId = "cap-test",
                level = 9
            };
            HeroLevelProgression.AddExperience(cappedProgress, 100000);
            Require(cappedProgress.level == 9 && cappedProgress.experience == 100000,
                "EXP bypassed the required breakthrough at the realm level cap.");
        }

        static void FillRealmStars(GameSaveData save, int realmIndex)
        {
            StarChartRealmProgressSaveData realm = new StarChartRealmProgressSaveData
            {
                realmIndex = realmIndex,
                litStarsPerBranch = new int[7]
            };
            for (int branch = 0; branch < realm.litStarsPerBranch.Length; branch++)
                realm.litStarsPerBranch[branch] = 10;
            save.starChartGemInventory.realmProgress.Add(realm);
        }

        static MatchResult CreateLineMatch(GemType type, int length)
        {
            List<BoardPos> positions = new List<BoardPos>();
            for (int column = 0; column < length; column++)
                positions.Add(new BoardPos(0, column));
            MatchResult match = new MatchResult();
            match.Segments.Add(new MatchSegment(type, true, positions));
            return match;
        }

        static MatchResult CreateIntersectingMatch(GemType type, bool horizontalIsT)
        {
            int centerRow = 2;
            int centerColumn = 2;
            List<BoardPos> horizontal = new List<BoardPos>();
            List<BoardPos> vertical = new List<BoardPos>();
            for (int offset = -1; offset <= 1; offset++)
            {
                horizontal.Add(new BoardPos(centerRow, centerColumn + offset));
                vertical.Add(new BoardPos(centerRow + offset, centerColumn));
            }

            if (horizontalIsT)
            {
                vertical.Clear();
                vertical.Add(new BoardPos(centerRow - 1, centerColumn));
                vertical.Add(new BoardPos(centerRow, centerColumn));
                vertical.Add(new BoardPos(centerRow + 1, centerColumn));
            }
            else
            {
                horizontal.Clear();
                horizontal.Add(new BoardPos(centerRow, centerColumn));
                horizontal.Add(new BoardPos(centerRow, centerColumn + 1));
                horizontal.Add(new BoardPos(centerRow, centerColumn + 2));
            }

            MatchResult match = new MatchResult();
            match.Segments.Add(new MatchSegment(type, true, horizontal));
            match.Segments.Add(new MatchSegment(type, false, vertical));
            return match;
        }

        static void AddResolvedMatch(MoveResult move, MatchResult match)
        {
            MethodInfo addMatch = typeof(MoveResult).GetMethod("AddMatch",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (addMatch == null)
                throw new InvalidOperationException("MoveResult.AddMatch was not found.");
            addMatch.Invoke(move, new object[] { match });
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
