using System;
using System.Collections;
using System.Collections.Generic;
using Match3RPG.Board;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using Match3RPG.Persistence;
using Match3RPG.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class BattleController : MonoBehaviour
    {
        const int BoardAttackBonusPerGem = 10;
        const int BaseHealthPerGem = 15;
        [SerializeField] CharacterView hero, boss;
        [SerializeField] BattleHUD hud;
        [SerializeField] FloatingTextManager floatingTextManager;
        [SerializeField] CombatVFXManager combatVfx;
        BoardRageFireVFX boardRageFireVfx;
        [SerializeField] int heroMaxHp, bossMaxHp;
        [SerializeField] private int invalidMovePenaltyDamage = 30;
        [SerializeField, Min(0)] private int heroAttack = 50;
        [SerializeField, Min(0)] private int heroSoulAttack = 50;
        [SerializeField, Min(0)] private int manaPerGem = 10;
        [SerializeField, Min(0)] private int ragePerGem = 5;
        [SerializeField, Min(0)] private int shieldPerGem = 50;
        [SerializeField, Min(0)] private int drainHpPerGem = 50;
        [SerializeField, Min(0)] private int drainRagePerGem = 5;
        [SerializeField, Min(0f)] private float drainManaPercentPerGem = 1f;
        [SerializeField, Min(1)] private int maxMana;
        [SerializeField, Min(0)] private int ragePerReceivedHit = 1;
        [SerializeField, Min(1)] private int maxRage;
        [SerializeField, Min(1f)] private float criticalDamageMultiplier = 2f;
        [SerializeField, Range(0f, 100f)] private float heroCriticalChancePercent = 10f;
        [SerializeField, Min(0)] private int heroDefense = 20;
        [SerializeField, Range(0f, 100f)] private float heroDodgePercent;
        [SerializeField, Min(0f)] private float heroAccuracyPercent = 100f;
        [SerializeField, Min(0)] private int heroSpeed = 10;
        [SerializeField, Min(0)] private int experienceStars;
        [SerializeField, Min(0)] private int healthStars;
        [SerializeField, Min(0)] private int shieldStars;
        [SerializeField, Min(0)] private int bossAttackDamagePerGem;
        [SerializeField, Min(0)] private int bossDefense = 20;
        [SerializeField, Range(0f, 100f)] private float bossDodgePercent;
        [SerializeField, Min(0f)] private float bossAccuracyPercent = 100f;
        [SerializeField, Min(0)] private int bossSpeed = 10;
        [SerializeField, Min(1)] private int bossMaxRage = 100;
        [SerializeField, Min(0)] private int bossStartingRage;
        [SerializeField, Min(0)] private int bossRagePerGem = 5;
        [SerializeField, Min(1)] private int bossMaxMana = 100;
        [SerializeField, Min(0)] private int bossRagePerReceivedHit = 1;
        [SerializeField] private bool bossCriticalEnabled = true;
        [SerializeField, Range(0f, 100f)] private float bossCriticalChancePercent = 10f;
        [SerializeField, Min(1f)] private float bossCriticalDamageMultiplier = 2f;
        [SerializeField] private CombatVFXType bossAttackVfx = CombatVFXType.Slash;
        [SerializeField, Min(0f)] private float multiHitInterval = .15f;
        [SerializeField] private bool showReceivedHitRageText;
        [Header("Flying Sword Skill (Kiếm Bay Quanh)")]
        [SerializeField, Range(1, SkillConfig.MaxLevel)] private int flyingSwordSkillLevel = SkillConfig.DefaultLevel;
        [Header("Temporary layout test mode")]
        [SerializeField] private bool layoutTestMode = false;

        int heroHp, bossHp, heroSoulHp, bossSoulHp, mana, ultimate, bossMana, bossUltimate;
        int heroMaxSoulHp, bossMaxSoulHp;
        int heroShield, heroShieldTurns;
        int bossShield, bossShieldTurns;
        int heroRage, bossRage;
        int playerManaGemCount, playerAttackGemCount, playerSoulGemCount, playerRageGemCount;
        int flyingSwordCooldownRemaining;
        int flyingSwordOrbitTurnsRemaining;
        int swordQiCooldownRemaining;
        int rockSpikeCooldownRemaining;
        int ellipseSwordsCooldownRemaining;
        int starfallBarrageCooldownRemaining;
        int thunderGodCooldownRemaining;
        int lastSyncedFlyingSwordLevel = SkillConfig.DefaultLevel;
        SkillConfig flyingSwordSkillConfig;
        SkillConfig swordQiSkillConfig;
        SkillConfig rockSpikeSkillConfig;
        SkillConfig ellipseSwordsSkillConfig;
        SkillConfig starfallBarrageSkillConfig;
        SkillConfig thunderGodSkillConfig;
        SkillConfig swordRainSkillConfig;
        FlyingSwordSkillController flyingSwordSkillController;
        SwordQiSkillController swordQiSkillController;
        RockSpikeSkillController rockSpikeSkillController;
        EllipseSwordsSkillController ellipseSwordsSkillController;
        StarfallBarrageSkillController starfallBarrageSkillController;
        ThunderGodSkillController thunderGodSkillController;
        int loggedHeroRage = int.MinValue;
        int loggedHeroMaxRage = int.MinValue;
        bool statsInitialized;
        bool soulEnabled;
        bool soulDamageEnabled;
        bool isSoulBurning;
        Coroutine soulBurnRoutine;
        bool isBossSoulBurning;
        Coroutine bossSoulBurnRoutine;
        int heroBattleEntryLevel = 1;
        bool heroProgressionConfigured;
        PlayerProgressionService progressionService;
        StarChartGemInventoryService starChartGemInventory;

        public int InvalidMovePenaltyCount { get; private set; }
        public int PlayerManaGemCount => playerManaGemCount;
        public int PlayerAttackGemCount => playerAttackGemCount;
        public int PlayerSoulGemCount => playerSoulGemCount;
        public int PlayerRageGemCount => playerRageGemCount;
        public int FlyingSwordCooldownRemaining => flyingSwordCooldownRemaining;
        public int FlyingSwordOrbitTurnsRemaining => flyingSwordOrbitTurnsRemaining;
        public SkillConfig FlyingSwordSkill => ResolveFlyingSwordSkill();
        public int FlyingSwordSkillLevel => ResolveActiveFlyingSwordLevel();
        public bool IsFlyingSwordSkillRunning =>
            flyingSwordSkillController != null && flyingSwordSkillController.IsRunning;
        public int SwordQiCooldownRemaining => swordQiCooldownRemaining;
        public SkillConfig SwordQiSkill => ResolveSwordQiSkill();
        public int SwordQiSkillLevel => SkillConfig.DefaultLevel; // TẠM: chỉ có Lv1
        public bool IsSwordQiSkillRunning =>
            swordQiSkillController != null && swordQiSkillController.IsRunning;
        public int RockSpikeCooldownRemaining => rockSpikeCooldownRemaining;
        public SkillConfig RockSpikeSkill => ResolveRockSpikeSkill();
        public int RockSpikeSkillLevel => SkillConfig.DefaultLevel; // TẠM: chỉ có Lv1
        public bool IsRockSpikeSkillRunning =>
            rockSpikeSkillController != null && rockSpikeSkillController.IsRunning;
        public int EllipseSwordsCooldownRemaining => ellipseSwordsCooldownRemaining;
        public SkillConfig EllipseSwordsSkill => ResolveEllipseSwordsSkill();
        public int EllipseSwordsSkillLevel => SkillConfig.DefaultLevel; // TẠM: chỉ có Lv1
        public bool IsEllipseSwordsSkillRunning =>
            ellipseSwordsSkillController != null && ellipseSwordsSkillController.IsRunning;
        public int StarfallBarrageCooldownRemaining => starfallBarrageCooldownRemaining;
        public SkillConfig StarfallBarrageSkill => ResolveStarfallBarrageSkill();
        public int StarfallBarrageSkillLevel => SkillConfig.DefaultLevel; // TẠM: chỉ có Lv1
        public bool IsStarfallBarrageSkillRunning =>
            starfallBarrageSkillController != null && starfallBarrageSkillController.IsRunning;
        public int ThunderGodCooldownRemaining => thunderGodCooldownRemaining;
        public SkillConfig ThunderGodSkill => ResolveThunderGodSkill();
        public int ThunderGodSkillLevel => SkillConfig.DefaultLevel; // TẠM: chỉ có Lv1
        public bool IsThunderGodSkillRunning =>
            thunderGodSkillController != null && thunderGodSkillController.IsRunning;
        public SkillConfig SwordRainSkill => ResolveSwordRainSkill();
        public int SwordRainSkillLevel => SkillConfig.DefaultLevel; // TẠM: chỉ có Lv1
        public bool IsGameOver => (heroHp <= 0 && (!soulEnabled || heroSoulHp <= 0))
            || (soulEnabled && heroSoulHp <= 0)
            || (bossHp <= 0 && (!soulEnabled || bossSoulHp <= 0))
            || (soulEnabled && bossSoulHp <= 0);
        public bool PlayerWon => (bossHp <= 0 && (!soulEnabled || bossSoulHp <= 0))
            || (soulEnabled && bossSoulHp <= 0);
        public bool SoulEnabled => soulEnabled;
        public bool IsSoulBurning => isSoulBurning;
        public bool IsBossSoulBurning => isBossSoulBurning;
        public int HeroBattleEntryLevel => heroBattleEntryLevel;
        public float BossHealthRatio => bossMaxHp <= 0 ? 0f : (float)bossHp / bossMaxHp;
        public float BossRageRatio => (float)bossRage / BossMaxRage;
        public int HeroHealth => heroHp;
        public int HeroSoulHealth => heroSoulHp;
        public int BossSoulHealth => bossSoulHp;
        public int HeroShield => heroShield;
        public int BossShield => bossShield;
        public int BossHealth => bossHp;
        public int HeroMaxHealth => heroMaxHp;
        public int BossMaxHealth => bossMaxHp;
        public int HeroAttackDamagePerGem => AddNonNegative(heroAttack, BoardAttackBonusPerGem);
        public int HeroSoulAttackDamagePerGem => AddNonNegative(heroSoulAttack, BoardAttackBonusPerGem);
        public int HeroExperiencePerGem => AddNonNegative(1, experienceStars);
        public int HeroShieldPerGem => AddNonNegative(shieldPerGem,
            MultiplyNonNegative(shieldStars, 10));
        public float HeroCriticalChancePercent => heroCriticalChancePercent;
        public int HeroDefense => heroDefense;
        public float HeroDodgePercent => heroDodgePercent;
        public float HeroAccuracyPercent => heroAccuracyPercent;
        public int HeroSpeed => heroSpeed;
        public int BossAttackDamagePerGem => bossAttackDamagePerGem;
        public int BossDefense => bossDefense;
        public float BossDodgePercent => bossDodgePercent;
        public float BossAccuracyPercent => bossAccuracyPercent;
        public int BossSpeed => bossSpeed;
        public float BossCriticalChancePercent => bossCriticalChancePercent;
        public int HeroMana => mana;
        public int HeroUltimate => ultimate;
        public int BossMana => bossMana;
        public int BossUltimate => bossUltimate;
        public int HeroRage => heroRage;
        public int BossRage => bossRage;
        public int MaxRage => Mathf.Max(1, maxRage);
        public int BossMaxRage => Mathf.Max(1, bossMaxRage);

        void Awake() => InitializeStats();

        void OnDisable()
        {
            StopSoulBurn();
            StopBossSoulBurn();
        }

        public void Configure(CharacterView heroView, CharacterView bossView, BattleHUD battleHud,
            FloatingTextManager textManager = null, BossData bossData = null,
            CombatVFXManager vfxManager = null, BoardRageFireVFX boardRageFire = null,
            HeroData heroData = null, RuntimeHeroStats? runtimeHeroStats = null,
            RuntimeBossStats? runtimeBossStats = null, int equipmentRagePerGemBonus = 0)
        {
            heroProgressionConfigured = false;
            heroBattleEntryLevel = 1;
            soulDamageEnabled = false;
            ragePerGem = AddNonNegative(5, equipmentRagePerGemBonus);
            if (runtimeHeroStats.HasValue)
            {
                RuntimeHeroStats stats = runtimeHeroStats.Value;
                heroBattleEntryLevel = stats.Level;
                soulDamageEnabled = heroBattleEntryLevel >= 10;
                heroMaxHp = stats.MaxHP;
                heroMaxSoulHp = stats.MaxSoulHP;
                heroAttack = stats.Attack;
                heroSoulAttack = stats.SoulAttack;
                heroCriticalChancePercent = stats.CriticalChancePercent;
                criticalDamageMultiplier = stats.CriticalDamageMultiplier;
                heroDefense = stats.Defense;
                heroDodgePercent = stats.DodgePercent;
                heroAccuracyPercent = stats.AccuracyPercent;
                heroSpeed = stats.Speed;
                experienceStars = stats.ExperienceStars;
                healthStars = stats.HealthStars;
                shieldStars = stats.ShieldStars;
                maxMana = stats.MaxMana;
                maxRage = stats.MaxRage;
                soulEnabled = true;
            }
            else if (heroData != null)
            {
                heroMaxHp = 500;
                heroMaxSoulHp = SoulHealthCalculator.CalculateMaxHealth(heroMaxHp, 1);
                heroAttack = 50;
                heroSoulAttack = 50;
                heroCriticalChancePercent = 10f;
                criticalDamageMultiplier = 2f;
                heroDefense = 20;
                heroDodgePercent = 0f;
                heroAccuracyPercent = 100f;
                heroSpeed = 10;
                experienceStars = 0;
                healthStars = 0;
                shieldStars = 0;
                heroProgressionConfigured = false;
                maxMana = Mathf.Max(1, heroData.BaseMana);
                maxRage = Mathf.Max(1, heroData.BaseRage);
                soulEnabled = true;
            }
            else
            {
                soulEnabled = false;
                experienceStars = 0;
                healthStars = 0;
                shieldStars = 0;
            }

            if (runtimeBossStats.HasValue)
            {
                RuntimeBossStats stats = runtimeBossStats.Value;
                bossMaxHp = stats.MaxHP;
                bossMaxSoulHp = stats.MaxSoulHP;
                bossAttackDamagePerGem = stats.Attack;
                bossDefense = stats.Defense;
                bossDodgePercent = stats.DodgePercent;
                bossAccuracyPercent = stats.AccuracyPercent;
                bossSpeed = stats.Speed;
                bossMaxRage = stats.MaxRage;
                bossStartingRage = stats.StartingRage;
                bossMaxMana = stats.MaxMana;
                bossRagePerReceivedHit = stats.RagePerReceivedHit;
                bossCriticalEnabled = stats.CriticalEnabled;
                bossCriticalChancePercent = stats.CriticalChancePercent;
                bossCriticalDamageMultiplier = stats.CriticalDamageMultiplier;
                bossAttackVfx = stats.AttackVfx;
            }
            else if (bossData != null)
            {
                bossMaxHp = Mathf.Max(1, bossData.MaxHP);
                bossMaxSoulHp = SoulHealthCalculator.CalculateBossMaxHealth(bossMaxHp, 1);
                bossAttackDamagePerGem = Mathf.Max(0, bossData.Attack);
                bossDefense = 20;
                bossDodgePercent = 0f;
                bossAccuracyPercent = 100f;
                bossSpeed = 10;
                bossMaxRage = Mathf.Max(1, bossData.MaxRage);
                bossStartingRage = Mathf.Clamp(bossData.StartingRage, 0, bossMaxRage);
                bossRagePerReceivedHit = Mathf.Max(0, bossData.RagePerReceivedHit);
                bossCriticalEnabled = bossData.CriticalEnabled;
                bossCriticalChancePercent = 10f;
                bossCriticalDamageMultiplier = Mathf.Max(1f, bossData.CriticalDamageMultiplier);
                bossAttackVfx = bossData.PreferredAttackVfx;
            }
            statsInitialized = false;
            InitializeStats();
            hero = heroView;
            boss = bossView;
            hud = battleHud;
            floatingTextManager = textManager;
            combatVfx = vfxManager;
            boardRageFireVfx = boardRageFire;
            progressionService = PlayerProgressionService.GetOrCreate();
            starChartGemInventory = new StarChartGemInventoryService(SaveManager.Instance);
            if (runtimeHeroStats.HasValue)
            {
                progressionService.Configure(SaveManager.Instance,
                    runtimeHeroStats.Value.HeroId);
                heroProgressionConfigured = true;
            }
            else if (heroData != null)
            {
                progressionService.Configure(SaveManager.Instance, heroData.StableId);
                heroProgressionConfigured = true;
            }
            EnsureFlyingSwordSkillSetup();
            EnsureSwordQiSkillSetup();
            EnsureRockSpikeSkillSetup();
            EnsureEllipseSwordsSkillSetup();
            EnsureStarfallBarrageSkillSetup();
            EnsureThunderGodSkillSetup();
            EnsureSwordRainSkillSetup();
            UpdateRageReadyVfx(BattleActor.Player);
            UpdateRageReadyVfx(BattleActor.Boss);
            Refresh();
        }

        void InitializeStats()
        {
            if (statsInitialized) return;
            heroHp = heroMaxHp;
            bossHp = bossMaxHp;
            heroSoulHp = heroMaxSoulHp;
            bossSoulHp = bossMaxSoulHp;
            mana = 0;
            bossMana = 0;
            heroShield = 0;
            heroShieldTurns = 0;
            bossShield = 0;
            bossShieldTurns = 0;
            heroRage = 0;
            bossRage = Mathf.Clamp(bossStartingRage, 0, BossMaxRage);
            playerManaGemCount = 0;
            playerAttackGemCount = 0;
            playerSoulGemCount = 0;
            playerRageGemCount = 0;
            flyingSwordCooldownRemaining = 0;
            flyingSwordOrbitTurnsRemaining = 0;
            swordQiCooldownRemaining = 0;
            rockSpikeCooldownRemaining = 0;
            ellipseSwordsCooldownRemaining = 0;
            starfallBarrageCooldownRemaining = 0;
            thunderGodCooldownRemaining = 0;
            StopSoulBurn();
            StopBossSoulBurn();
            flyingSwordSkillController?.StopOrbitImmediate();
            if (layoutTestMode)
                ApplyLayoutTestState();
            statsInitialized = true;
        }

        void ApplyLayoutTestState()
        {
            if (!layoutTestMode)
                return;

            heroHp = Mathf.Max(0, heroMaxHp);
            bossHp = Mathf.Max(0, bossMaxHp);
            heroSoulHp = Mathf.Max(0, heroMaxSoulHp);
            bossSoulHp = Mathf.Max(0, bossMaxSoulHp);
            heroRage = Mathf.Max(0, MaxRage);
            bossRage = Mathf.Max(0, BossMaxRage);
            mana = Mathf.Max(0, maxMana);
            ultimate = 100;
            bossMana = Mathf.Max(0, bossMaxMana);
            bossUltimate = 100;
        }

        public IEnumerator ResolveMove(BattleActor actor, MoveResult move,
            float attackDamageMultiplier = 1f, bool isSkillGemResolution = false)
        {
            if (move == null || !move.IsValid || IsGameOver) yield break;
            if (layoutTestMode)
            {
                ApplyLayoutTestState();
                Refresh();
            }

            if (!isSkillGemResolution)
                starChartGemInventory.CollectMatch(move);
            int attackGems = Count(move, GemType.Attack);
            int manaGems = Count(move, GemType.Mana);
            int experienceGems = Count(move, GemType.ExpDrain);
            int healthGems = Count(move, GemType.Health);
            int rageGems = Count(move, GemType.Rage);
            int shieldGems = Count(move, GemType.Shield);
            int soulGems = soulEnabled ? Count(move, GemType.Soul) : 0;
            float precedingVfxDuration = 0f;

            if (actor == BattleActor.Player)
            {
                if (!isSkillGemResolution)
                {
                    playerManaGemCount = AddNonNegative(playerManaGemCount, manaGems);
                    playerAttackGemCount = AddNonNegative(playerAttackGemCount, attackGems);
                    playerSoulGemCount = AddNonNegative(playerSoulGemCount, Count(move, GemType.Soul));
                    playerRageGemCount = AddNonNegative(playerRageGemCount, rageGems);
                }
                int previousHealth = heroHp;
                heroHp = Mathf.Min(heroMaxHp, heroHp
                    + healthGems * AddNonNegative(BaseHealthPerGem, healthStars));
                if (heroHp > 0 && isSoulBurning)
                {
                    StopSoulBurn();
                    Refresh();
                }
                if (heroHp > previousHealth && combatVfx != null
                    && combatVfx.PlayHeal(BattleActor.Player))
                    precedingVfxDuration = CombatVFXManager.HealingAuraDuration;
                ShowResource(hero, FloatingTextType.Heal, heroHp - previousHealth);
                int previousMana = mana;
                mana = Mathf.Min(maxMana, mana + manaGems * manaPerGem);
                ShowResource(hero, FloatingTextType.Mana, mana - previousMana);
                AddExperience(experienceGems);
                AddGemRage(BattleActor.Player, rageGems);
                AddShield(BattleActor.Player, shieldGems);
                precedingVfxDuration = Mathf.Max(precedingVfxDuration,
                    ApplyDrain(BattleActor.Player, experienceGems));

                if (attackGems > 0 || soulGems > 0)
                {
                    if (precedingVfxDuration > 0f)
                        yield return new WaitForSeconds(precedingVfxDuration);
                    yield return ResolveAttackAction(BattleActor.Player, attackGems, 0, soulGems,
                        attackDamageMultiplier, isSkillGemResolution);
                }

                if (!isSkillGemResolution && ultimate >= 100 && bossHp > 0 && !IsGameOver)
                {
                    int spentUltimate = ultimate;
                    ultimate = 0;
                    ShowResource(hero, FloatingTextType.UltimateSpent, spentUltimate);
                    if (hero != null) yield return hero.PlaySkill();
                    combatVfx?.PlayUltimate(BattleActor.Boss);
                    ApplySkillDamage(BattleActor.Boss, 80);
                    if (boss != null && bossHp > 0) yield return boss.Hit();
                }
            }
            else
            {
                int previousBossHealth = bossHp;
                bossHp = Mathf.Min(bossMaxHp, bossHp + healthGems * BaseHealthPerGem);
                if (bossHp > 0 && isBossSoulBurning)
                {
                    StopBossSoulBurn();
                    Refresh();
                }
                if (bossHp > previousBossHealth && combatVfx != null
                    && combatVfx.PlayHeal(BattleActor.Boss))
                    precedingVfxDuration = CombatVFXManager.HealingAuraDuration;
                ShowResource(boss, FloatingTextType.Heal, bossHp - previousBossHealth);
                int previousBossMana = bossMana;
                bossMana = Mathf.Min(bossMaxMana, bossMana + manaGems * manaPerGem);
                ShowResource(boss, FloatingTextType.Mana, bossMana - previousBossMana);
                AddGemRage(BattleActor.Boss, rageGems);
                AddShield(BattleActor.Boss, shieldGems);
                precedingVfxDuration = Mathf.Max(precedingVfxDuration,
                    ApplyDrain(BattleActor.Boss, experienceGems));

                if (attackGems > 0 || soulGems > 0)
                {
                    if (precedingVfxDuration > 0f)
                        yield return new WaitForSeconds(precedingVfxDuration);
                    yield return ResolveAttackAction(BattleActor.Boss, attackGems, 0, soulGems);
                }

                if (bossUltimate >= 100 && heroHp > 0 && !IsGameOver)
                {
                    int spentBossUltimate = bossUltimate;
                    bossUltimate = 0;
                    ShowResource(boss, FloatingTextType.UltimateSpent, spentBossUltimate);
                    if (boss != null) yield return boss.PlaySkill();
                    combatVfx?.PlayUltimate(BattleActor.Player);
                    ApplySkillDamage(BattleActor.Player, 80);
                    if (hero != null && heroHp > 0) yield return hero.Hit();
                }
            }

            if (IsGameOver && PlayerWon && boss != null)
                yield return boss.Death();
            if (IsGameOver && !PlayerWon && hero != null)
                yield return hero.Death();
            StopFlyingSwordOrbitIfBattleEnded();
            Refresh();
        }

        public IEnumerator ApplyInvalidMovePenalty()
        {
            if (IsGameOver) yield break;
            InvalidMovePenaltyCount++;
            if (boss != null) yield return boss.Attack();
            int shieldBefore = heroShield;
            int damage = ApplyDamage(BattleActor.Player, Mathf.Max(0, invalidMovePenaltyDamage));
            int shieldAbsorbed = shieldBefore - heroShield;
            if (damage > 0)
            {
                bool shielded = shieldAbsorbed > 0 || (combatVfx != null
                    && combatVfx.IsPersistentEffectActive(BattleActor.Player, CombatVFXType.Shield));
                combatVfx?.PlayAttack(BattleActor.Boss, false, bossAttackVfx, true, shielded);
                ShowResource(hero, FloatingTextType.Damage, damage);
                AddReceivedHitRage(BattleActor.Player);
                if (hero != null) yield return hero.Hit();
            }
            if (IsGameOver && !PlayerWon && hero != null)
                yield return hero.Death();
            StopFlyingSwordOrbitIfBattleEnded();
            Refresh();
        }

        IEnumerator ResolveAttackAction(BattleActor actor, int hitCount, int bonusDamage,
            int soulHitCount = 0, float attackDamageMultiplier = 1f, bool isSkillGemResolution = false)
        {
            int totalHitCount = hitCount + soulHitCount;
            CharacterView attacker = actor == BattleActor.Player ? hero : boss;
            BattleActor targetActor = actor == BattleActor.Player ? BattleActor.Boss : BattleActor.Player;
            CharacterView target = targetActor == BattleActor.Player ? hero : boss;
            bool rageCritical = totalHitCount > 0 && IsCriticalEnabled(actor)
                && GetRage(actor) >= GetMaxRage(actor);
            bool critical = rageCritical || (totalHitCount > 0
                && UnityEngine.Random.value * 100f < (actor == BattleActor.Player
                    ? heroCriticalChancePercent : bossCriticalChancePercent));
            CombatVFXType attackVfx = actor == BattleActor.Player
                ? CombatVFXType.Slash : bossAttackVfx;
            bool rangedAttack = attackVfx == CombatVFXType.Fireball
                || attackVfx == CombatVFXType.AirSlash;
            bool shouldTriggerSwordRain = actor == BattleActor.Player
                && !isSkillGemResolution
                && !rangedAttack
                && totalHitCount > 0
                && !IsGameOver
                && boss != null
                && IsSwordRainPassiveEnabled();
            int baseAttackDamage = actor == BattleActor.Player
                ? HeroAttackDamagePerGem : bossAttackDamagePerGem;
            int attackDamage = Mathf.Max(0,
                Mathf.RoundToInt(baseAttackDamage * Mathf.Max(0f, attackDamageMultiplier)));
            int soulAttackDamage = actor == BattleActor.Player
                ? HeroSoulAttackDamagePerGem : bossAttackDamagePerGem;
            float criticalMultiplier = actor == BattleActor.Player
                ? criticalDamageMultiplier
                : bossCriticalDamageMultiplier;
            int damagePerHit = Mathf.Max(0, critical
                ? Mathf.RoundToInt(attackDamage * criticalMultiplier)
                : attackDamage);
            int soulDamagePerHit = Mathf.Max(0, critical
                ? Mathf.RoundToInt(soulAttackDamage * criticalMultiplier)
                : soulAttackDamage);
            int rageGainedByTarget = 0;

            if (critical)
                ShowResource(attacker, FloatingTextType.CriticalReady, 1);

            bool recovered = attacker == null;
            bool shieldWasActive = false;
            bool swordRainPending = false;
            Coroutine swordRainRoutine = null;
            try
            {
                if (attacker != null && !rangedAttack)
                {
                    if (attackVfx == CombatVFXType.Dash)
                        combatVfx?.PlayDash(actor);
                    shieldWasActive = combatVfx != null
                        && combatVfx.IsPersistentEffectActive(actor, CombatVFXType.Shield);
                    if (shieldWasActive)
                        combatVfx.SetPersistentEffect(actor, CombatVFXType.Shield, false);
                    attacker.BringToFront();
                    if (shouldTriggerSwordRain)
                    {
                        // Nội tại Kiếm Vũ: bắt đầu phóng 3 kiếm ngay khi hero bắt đầu lao tới,
                        // để cả 3 kiếm rơi xong trước khi hero lùi về xong.
                        swordRainPending = true;
                        swordRainRoutine = StartCoroutine(
                            RunSwordRainPassiveRoutine(() => swordRainPending = false));
                    }
                    // Lunge forward once and start the first hit's full attack clip.
                    yield return attacker.BeginAttackSequence(1, 0f, 0f);
                }

                bool firstSuccessfulHit = true;
                for (int hit = 0; hit < totalHitCount; hit++)
                {
                    if (IsGameOver) break;
                    // Follow-up hits replay one full clip in place (already at the lunge position).
                    if (hit > 0 && attacker != null && !rangedAttack)
                        attacker.PlayAttackClip();
                    if (attacker != null && attacker.HasSpriteAttackAnimation)
                        yield return attacker.WaitForAttackImpact(hit);
                    if (IsGameOver) break;
                    // Né: mỗi hit tung riêng. Tỉ lệ = max(0, Né của người bị đánh - max(0, Chính xác của người đánh - 100)).
                    bool dodged = false;
                    float dodgeChance = targetActor == BattleActor.Player
                        ? Mathf.Max(0f, heroDodgePercent - Mathf.Max(0f, bossAccuracyPercent - 100f))
                        : Mathf.Max(0f, bossDodgePercent - Mathf.Max(0f, heroAccuracyPercent - 100f));
                    if (dodgeChance > 0f && UnityEngine.Random.value * 100f < dodgeChance)
                    {
                        dodged = true;
                        // amount = 1 chỉ để qua guard amount > 0 của ShowResource; Format(Dodge) trả "Né!" không dùng amount.
                        ShowResource(target, FloatingTextType.Dodge, 1);
                    }
                    int shieldBefore = GetShield(targetActor);
                    bool soulHit = hit >= hitCount;
                    int damage = dodged ? 0 : (soulHit
                        ? ApplySoulDamage(targetActor, soulDamagePerHit)
                        : ApplyDamage(targetActor, damagePerHit));
                    int shieldAbsorbed = shieldBefore - GetShield(targetActor);
                    if (!dodged && damage <= 0 && shieldAbsorbed <= 0) break;

                    if (damage > 0)
                    {
                        bool shielded = shieldAbsorbed > 0 || (combatVfx != null
                            && combatVfx.IsPersistentEffectActive(targetActor, CombatVFXType.Shield));
                        combatVfx?.PlayAttack(actor, critical, attackVfx, firstSuccessfulHit, shielded);
                        firstSuccessfulHit = false;
                        FloatingTextType damageText = soulHit
                            && soulDamageEnabled
                            ? (critical ? FloatingTextType.SoulCritical : FloatingTextType.SoulDamage)
                            : (critical ? FloatingTextType.Critical : FloatingTextType.Damage);
                        ShowResource(target, damageText, damage);
                        rageGainedByTarget += AddReceivedHitRage(targetActor);
                        Refresh();
                        if (target != null) yield return target.Hit();
                    }
                    if (hit + 1 < totalHitCount && !IsGameOver)
                    {
                        // Melee: finish the current full clip before the next hit's clip starts.
                        if (attacker != null && !rangedAttack)
                            yield return attacker.WaitForAttackClipComplete();
                        else if (multiHitInterval > 0f)
                            yield return new WaitForSecondsRealtime(multiHitInterval);
                    }
                }

                if (!IsGameOver && bonusDamage > 0)
                {
                    int shieldBefore = GetShield(targetActor);
                    int damage = ApplyDamage(targetActor, bonusDamage);
                    int shieldAbsorbed = shieldBefore - GetShield(targetActor);
                    if (damage > 0)
                    {
                        bool shielded = shieldAbsorbed > 0 || (combatVfx != null
                            && combatVfx.IsPersistentEffectActive(targetActor, CombatVFXType.Shield));
                        combatVfx?.PlayAttack(actor, false, attackVfx, firstSuccessfulHit, shielded);
                        ShowResource(target, FloatingTextType.Damage, damage);
                        rageGainedByTarget += AddReceivedHitRage(targetActor);
                        Refresh();
                        if (target != null) yield return target.Hit();
                    }
                }

                if (attacker != null && !rangedAttack)
                {
                    yield return attacker.EndAttackSequence();
                    recovered = true;
                }

                while (swordRainPending && !IsGameOver)
                    yield return null;
            }
            finally
            {
                if ((!recovered || IsGameOver) && swordRainRoutine != null)
                {
                    StopCoroutine(swordRainRoutine);
                    swordRainRoutine = null;
                    swordRainPending = false;
                }
                if (!recovered && attacker != null)
                    attacker.CancelAttackSequence();
                if (shieldWasActive && combatVfx != null && GetShield(actor) > 0 && !IsGameOver)
                    combatVfx.SetPersistentEffect(actor, CombatVFXType.Shield, true);
                if (rageCritical)
                    SetRage(actor, 0);
                Refresh();
            }

            if (showReceivedHitRageText && rageGainedByTarget > 0)
                ShowResource(target, FloatingTextType.Rage, rageGainedByTarget);
            Refresh();
        }

        void ApplySkillDamage(BattleActor target, int amount)
        {
            int damage = ApplyDamage(target, amount);
            if (damage <= 0) return;
            ShowResource(target == BattleActor.Player ? hero : boss, FloatingTextType.Damage, damage);
            Refresh();
        }

        int ApplyDefense(BattleActor target, int amount)
        {
            if (amount <= 0) return 0;
            int defense = target == BattleActor.Player ? heroDefense : bossDefense;
            return Mathf.Max(1, Mathf.RoundToInt(amount * 100f / (100f + defense)));
        }

        int ApplyDamage(BattleActor target, int amount, bool shieldCanAbsorb = true)
        {
            if (layoutTestMode)
                return 0;
            if (amount <= 0 || IsGameOver) return 0;
            if ((target == BattleActor.Player && isSoulBurning)
                || (target == BattleActor.Boss && isBossSoulBurning))
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 0.8f));
            amount = AbsorbShield(target, amount, shieldCanAbsorb);
            if (amount <= 0) return 0;
            amount = ApplyDefense(target, amount);
            int previous = target == BattleActor.Player ? heroHp : bossHp;
            if (target == BattleActor.Player)
            {
                heroHp = Mathf.Max(0, heroHp - amount);
                if (heroHp <= 0 && soulEnabled && heroSoulHp > 0 && !isSoulBurning)
                {
                    isSoulBurning = true;
                    ShowResource(hero, FloatingTextType.SoulBurn, 1);
                    if (soulBurnRoutine == null && isActiveAndEnabled)
                        soulBurnRoutine = StartCoroutine(SoulBurnDrainRoutine());
                    Refresh();
                }
            }
            else
            {
                bossHp = Mathf.Max(0, bossHp - amount);
                if (bossHp <= 0 && soulEnabled && bossSoulHp > 0 && !isBossSoulBurning)
                {
                    isBossSoulBurning = true;
                    ShowResource(boss, FloatingTextType.SoulBurn, 1);
                    if (bossSoulBurnRoutine == null && isActiveAndEnabled)
                        bossSoulBurnRoutine = StartCoroutine(BossSoulBurnDrainRoutine());
                    Refresh();
                }
            }
            if (IsGameOver)
            {
                StopSoulBurn();
                StopBossSoulBurn();
            }
            int current = target == BattleActor.Player ? heroHp : bossHp;
            return previous - current;
        }

        int ApplySoulDamage(BattleActor target, int amount)
        {
            if (layoutTestMode)
                return 0;
            if (!soulEnabled || amount <= 0 || IsGameOver) return 0;
            if (!soulDamageEnabled)
                return ApplyDamage(target, amount);
            if ((target == BattleActor.Player && isSoulBurning)
                || (target == BattleActor.Boss && isBossSoulBurning))
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 0.8f));
            amount = AbsorbShield(target, amount, true);
            if (amount <= 0) return 0;
            amount = ApplyDefense(target, amount);
            int previous = target == BattleActor.Player ? heroSoulHp : bossSoulHp;
            if (target == BattleActor.Player)
                heroSoulHp = Mathf.Max(0, heroSoulHp - amount);
            else
                bossSoulHp = Mathf.Max(0, bossSoulHp - amount);
            if (IsGameOver)
            {
                StopSoulBurn();
                StopBossSoulBurn();
            }
            int current = target == BattleActor.Player ? heroSoulHp : bossSoulHp;
            return previous - current;
        }

        void StopSoulBurn()
        {
            isSoulBurning = false;
            if (soulBurnRoutine != null)
            {
                StopCoroutine(soulBurnRoutine);
                soulBurnRoutine = null;
            }
        }

        IEnumerator SoulBurnDrainRoutine()
        {
            while (isSoulBurning && !IsGameOver && !layoutTestMode)
            {
                if (heroHp > 0)
                {
                    StopSoulBurn();
                    Refresh();
                    yield break;
                }

                yield return new WaitForSeconds(1.0f);

                if (!isSoulBurning || IsGameOver || layoutTestMode)
                    break;
                if (heroHp > 0)
                {
                    StopSoulBurn();
                    Refresh();
                    yield break;
                }

                int drain = Mathf.Max(1, Mathf.RoundToInt(heroMaxSoulHp * 0.02f));
                int previousSoul = heroSoulHp;
                heroSoulHp = Mathf.Max(0, heroSoulHp - drain);
                ShowResource(hero, FloatingTextType.SoulDamage, previousSoul - heroSoulHp);
                Refresh();

                if (heroHp > 0 || IsGameOver)
                    break;
            }

            isSoulBurning = false;
            soulBurnRoutine = null;
        }

        void StopBossSoulBurn()
        {
            isBossSoulBurning = false;
            if (bossSoulBurnRoutine != null)
            {
                StopCoroutine(bossSoulBurnRoutine);
                bossSoulBurnRoutine = null;
            }
        }

        IEnumerator BossSoulBurnDrainRoutine()
        {
            while (isBossSoulBurning && !IsGameOver && !layoutTestMode)
            {
                if (bossHp > 0)
                {
                    StopBossSoulBurn();
                    Refresh();
                    yield break;
                }

                yield return new WaitForSeconds(1.0f);

                if (!isBossSoulBurning || IsGameOver || layoutTestMode)
                    break;
                if (bossHp > 0)
                {
                    StopBossSoulBurn();
                    Refresh();
                    yield break;
                }

                int drain = Mathf.Max(1, Mathf.RoundToInt(bossMaxSoulHp * 0.02f));
                int previousSoul = bossSoulHp;
                bossSoulHp = Mathf.Max(0, bossSoulHp - drain);
                ShowResource(boss, FloatingTextType.SoulDamage, previousSoul - bossSoulHp);
                Refresh();

                if (bossHp > 0 || IsGameOver)
                    break;
            }

            isBossSoulBurning = false;
            bossSoulBurnRoutine = null;
        }

        int GetShield(BattleActor actor) => actor == BattleActor.Player ? heroShield : bossShield;

        int AbsorbShield(BattleActor target, int amount, bool shieldCanAbsorb)
        {
            int currentShield = GetShield(target);
            if (!shieldCanAbsorb || currentShield <= 0)
                return amount;
            int absorbed = Mathf.Min(currentShield, amount);
            int remainingShield = currentShield - absorbed;
            if (target == BattleActor.Player)
                heroShield = remainingShield;
            else
                bossShield = remainingShield;
            if (remainingShield <= 0)
            {
                if (target == BattleActor.Player)
                    heroShield = 0;
                else
                    bossShield = 0;
                combatVfx?.SetPersistentEffect(target, CombatVFXType.Shield, false);
            }
            ShowResource(target == BattleActor.Player ? hero : boss, FloatingTextType.ShieldLost, absorbed);
            return amount - absorbed;
        }

        void AddGemRage(BattleActor actor, int gemCount)
        {
            if (gemCount <= 0) return;
            int ragePerGemValue = actor == BattleActor.Player ? ragePerGem : bossRagePerGem;
            int gained = AddRage(actor, gemCount * ragePerGemValue);
            CharacterView target = actor == BattleActor.Player ? hero : boss;
            ShowResource(target, FloatingTextType.Rage, gained);
            Refresh();
        }

        void AddExperience(int gemCount)
        {
            if (gemCount <= 0 || progressionService == null
                || !heroProgressionConfigured)
                return;
            long total = (long)gemCount * HeroExperiencePerGem;
            int amount = total >= int.MaxValue ? int.MaxValue : (int)total;
            if (amount <= 0) return;
            progressionService.AddExperience(amount);
            ShowResource(hero, FloatingTextType.Experience, amount);
        }

        void AddShield(BattleActor actor, int gemCount)
        {
            if (gemCount <= 0) return;
            int effectiveShieldPerGem = HeroShieldPerGem;
            long totalShield = (long)GetShield(actor) + (long)gemCount * effectiveShieldPerGem;
            int updatedShield = totalShield >= int.MaxValue ? int.MaxValue : (int)totalShield;
            if (actor == BattleActor.Player)
            {
                heroShield = updatedShield;
                heroShieldTurns = 2;
            }
            else
            {
                bossShield = updatedShield;
                bossShieldTurns = 2;
            }
            if (updatedShield > 0)
                combatVfx?.SetPersistentEffect(actor, CombatVFXType.Shield, true);
            long gained = (long)gemCount * effectiveShieldPerGem;
            ShowResource(actor == BattleActor.Player ? hero : boss, FloatingTextType.Shield,
                gained >= int.MaxValue ? int.MaxValue : (int)gained);
        }

        public void AdvancePlayerTurn()
        {
            if (heroShieldTurns <= 0) return;
            heroShieldTurns--;
            if (heroShieldTurns == 0)
            {
                heroShield = 0;
                combatVfx?.SetPersistentEffect(BattleActor.Player, CombatVFXType.Shield, false);
                Refresh();
            }
        }

        public void AdvanceBossTurn()
        {
            if (bossShieldTurns <= 0) return;
            bossShieldTurns--;
            if (bossShieldTurns == 0)
            {
                bossShield = 0;
                combatVfx?.SetPersistentEffect(BattleActor.Boss, CombatVFXType.Shield, false);
                Refresh();
            }
        }

        public void AdvanceRoundCooldowns()
        {
            if (flyingSwordCooldownRemaining > 0)
                flyingSwordCooldownRemaining--;
            if (swordQiCooldownRemaining > 0)
                swordQiCooldownRemaining--;
            if (rockSpikeCooldownRemaining > 0)
                rockSpikeCooldownRemaining--;
            if (ellipseSwordsCooldownRemaining > 0)
                ellipseSwordsCooldownRemaining--;
            if (starfallBarrageCooldownRemaining > 0)
                starfallBarrageCooldownRemaining--;
            if (thunderGodCooldownRemaining > 0)
                thunderGodCooldownRemaining--;

            if (flyingSwordOrbitTurnsRemaining > 0)
            {
                flyingSwordOrbitTurnsRemaining--;
                if (flyingSwordOrbitTurnsRemaining == 0)
                    flyingSwordSkillController?.FadeOutAndStopOrbit();
            }
        }

        void StopFlyingSwordOrbitIfBattleEnded()
        {
            if (!IsGameOver)
                return;

            StopSoulBurn();
            StopBossSoulBurn();
            if (flyingSwordOrbitTurnsRemaining <= 0)
                return;

            flyingSwordOrbitTurnsRemaining = 0;
            bool heroDead = !PlayerWon;
            if (heroDead)
                flyingSwordSkillController?.StopOrbitImmediate();
            else
                flyingSwordSkillController?.FadeOutAndStopOrbit();
        }

        void EnsureFlyingSwordSkillSetup()
        {
            ResolveFlyingSwordSkill();
            if (flyingSwordSkillController == null)
            {
                flyingSwordSkillController = GetComponent<FlyingSwordSkillController>();
                if (flyingSwordSkillController == null)
                    flyingSwordSkillController = gameObject.AddComponent<FlyingSwordSkillController>();
            }
            if (hero != null)
                flyingSwordSkillController.Configure(hero);
            ResolveActiveFlyingSwordLevel();
        }

        SkillConfig ResolveFlyingSwordSkill()
        {
            return ResolveSkillById(
                ref flyingSwordSkillConfig,
                SkillConfig.FlyingSwordSkillId,
                SkillConfig.CreateRuntimeFlyingSwordFallback);
        }

        void EnsureSwordQiSkillSetup()
        {
            ResolveSwordQiSkill();
            if (swordQiSkillController == null)
            {
                swordQiSkillController = GetComponent<SwordQiSkillController>();
                if (swordQiSkillController == null)
                    swordQiSkillController = gameObject.AddComponent<SwordQiSkillController>();
            }
            swordQiSkillController.SkillLevel = SkillConfig.DefaultLevel;
        }

        SkillConfig ResolveSwordQiSkill()
        {
            return ResolveSkillById(
                ref swordQiSkillConfig,
                SkillConfig.SwordQiSkillId,
                SkillConfig.CreateRuntimeSwordQiFallback);
        }

        void EnsureRockSpikeSkillSetup()
        {
            ResolveRockSpikeSkill();
            if (rockSpikeSkillController == null)
            {
                rockSpikeSkillController = GetComponent<RockSpikeSkillController>();
                if (rockSpikeSkillController == null)
                    rockSpikeSkillController = gameObject.AddComponent<RockSpikeSkillController>();
            }
            rockSpikeSkillController.SkillLevel = SkillConfig.DefaultLevel;
        }

        SkillConfig ResolveRockSpikeSkill()
        {
            return ResolveSkillById(
                ref rockSpikeSkillConfig,
                SkillConfig.RockSpikeSkillId,
                SkillConfig.CreateRuntimeRockSpikeFallback);
        }

        void EnsureEllipseSwordsSkillSetup()
        {
            ResolveEllipseSwordsSkill();
            if (ellipseSwordsSkillController == null)
            {
                ellipseSwordsSkillController = GetComponent<EllipseSwordsSkillController>();
                if (ellipseSwordsSkillController == null)
                    ellipseSwordsSkillController = gameObject.AddComponent<EllipseSwordsSkillController>();
            }
            ellipseSwordsSkillController.SkillLevel = SkillConfig.DefaultLevel;
        }

        SkillConfig ResolveEllipseSwordsSkill()
        {
            return ResolveSkillById(
                ref ellipseSwordsSkillConfig,
                SkillConfig.EllipseSwordsSkillId,
                SkillConfig.CreateRuntimeEllipseSwordsFallback);
        }

        void EnsureStarfallBarrageSkillSetup()
        {
            ResolveStarfallBarrageSkill();
            if (starfallBarrageSkillController == null)
            {
                starfallBarrageSkillController = GetComponent<StarfallBarrageSkillController>();
                if (starfallBarrageSkillController == null)
                    starfallBarrageSkillController = gameObject.AddComponent<StarfallBarrageSkillController>();
            }
            starfallBarrageSkillController.SkillLevel = SkillConfig.DefaultLevel;
        }

        SkillConfig ResolveStarfallBarrageSkill()
        {
            return ResolveSkillById(
                ref starfallBarrageSkillConfig,
                SkillConfig.StarfallBarrageSkillId,
                SkillConfig.CreateRuntimeStarfallBarrageFallback);
        }

        void EnsureThunderGodSkillSetup()
        {
            ResolveThunderGodSkill();
            if (thunderGodSkillController == null)
            {
                thunderGodSkillController = GetComponent<ThunderGodSkillController>();
                if (thunderGodSkillController == null)
                    thunderGodSkillController = gameObject.AddComponent<ThunderGodSkillController>();
            }
            thunderGodSkillController.SkillLevel = SkillConfig.DefaultLevel;
        }

        SkillConfig ResolveThunderGodSkill()
        {
            return ResolveSkillById(
                ref thunderGodSkillConfig,
                SkillConfig.ThunderGodSkillId,
                SkillConfig.CreateRuntimeThunderGodFallback);
        }

        void EnsureSwordRainSkillSetup()
        {
            ResolveSwordRainSkill();
        }

        SkillConfig ResolveSwordRainSkill()
        {
            return ResolveSkillById(
                ref swordRainSkillConfig,
                SkillConfig.SwordRainSkillId,
                SkillConfig.CreateRuntimeSwordRainFallback);
        }

        bool IsSwordRainPassiveEnabled()
        {
            SkillConfig skill = ResolveSwordRainSkill();
            return skill != null
                && skill.Type == SkillType.Passive
                && skill.TriggerType == SkillTriggerType.OnNormalAttack;
        }

        IEnumerator RunSwordRainPassiveRoutine(Action onFinished)
        {
            try
            {
                if (IsGameOver || combatVfx == null || boss == null)
                    yield break;

                SkillConfig skill = ResolveSwordRainSkill();
                Sprite swordSprite = skill != null && skill.Icon != null
                    ? skill.Icon
                    : SkillConfig.ResolveSwordSprite();
                if (swordSprite == null)
                {
                    Debug.LogWarning($"[SwordRainPassive] Missing sword sprite at {SkillConfig.FlyingSwordAssetPath}.", this);
                    yield break;
                }

                // TẠM: 3 kiếm bay nối tiếp cách ~0.12s, mỗi kiếm bay ~0.40s từ góc trên-trái xuống boss,
                // kích thước bằng kiếm quanh của Skill 1. Sư Huynh chốt lại sau Play-test.
                const int swordCount = SkillConfig.SwordRainDefaultSwordCount;
                const float spawnInterval = SkillConfig.SwordRainDefaultIntervalSeconds;
                const float flightDuration = SkillConfig.SwordRainDefaultFlightSeconds;
                Vector2 swordSize = FlyingSwordSkillController.CalculateOrbitSwordSize(hero, swordSprite);

                int inFlightCount = 0;
                Func<bool> shouldCancel = () => IsGameOver || bossHp <= 0;

                for (int i = 0; i < swordCount; i++)
                {
                    if (shouldCancel())
                        break;

                    int swordIndex = i;
                    bool launched = combatVfx.PlayProjectileFromTopLeftScreen(
                        swordSprite,
                        swordSize,
                        BattleActor.Boss,
                        flightDuration,
                        shouldCancel,
                        () =>
                        {
                            inFlightCount = Mathf.Max(0, inFlightCount - 1);
                            ApplySwordRainImpact(swordIndex, swordCount);
                        },
                        () =>
                        {
                            inFlightCount = Mathf.Max(0, inFlightCount - 1);
                        });

                    if (launched)
                        inFlightCount++;

                    if (i + 1 < swordCount && spawnInterval > 0f && !shouldCancel())
                    {
                        float waitElapsed = 0f;
                        while (waitElapsed < spawnInterval && !shouldCancel())
                        {
                            float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                            waitElapsed += dt;
                            yield return null;
                        }
                    }
                }

                float maxWait = flightDuration + 0.5f;
                float waitAfterLaunch = 0f;
                while (inFlightCount > 0 && !shouldCancel() && waitAfterLaunch < maxWait)
                {
                    float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                    waitAfterLaunch += dt;
                    yield return null;
                }
            }
            finally
            {
                onFinished?.Invoke();
            }
        }

        void ApplySwordRainImpact(int swordIndex, int totalSwords)
        {
            if (IsGameOver || bossHp <= 0)
                return;

            // TẠM: tổng sát thương 3 kiếm = 1 đòn đánh thường của hero (HeroAttackDamagePerGem),
            // tính tại thời điểm chính kiếm này chạm boss; chia đều số nguyên, kiếm đầu nhận phần dư.
            // Sư Huynh chốt lại sau Play-test.
            int totalDamage = HeroAttackDamagePerGem;
            int safeCount = Mathf.Max(1, totalSwords);
            int baseDamage = totalDamage / safeCount;
            int remainder = totalDamage % safeCount;
            int rawDamage = baseDamage + (swordIndex == 0 ? remainder : 0);

            int shieldBefore = GetShield(BattleActor.Boss);
            int damage = ApplyDamage(BattleActor.Boss, rawDamage);
            int shieldAbsorbed = shieldBefore - GetShield(BattleActor.Boss);
            if (damage > 0)
            {
                bool shielded = shieldAbsorbed > 0 || (combatVfx != null
                    && combatVfx.IsPersistentEffectActive(BattleActor.Boss, CombatVFXType.Shield));
                combatVfx?.PlayAttack(BattleActor.Player, false, CombatVFXType.Slash, false, shielded);
                ShowResource(boss, FloatingTextType.Damage, damage);
                AddReceivedHitRage(BattleActor.Boss);
                Refresh();
            }
            else if (shieldAbsorbed > 0)
            {
                Refresh();
            }
        }

        static SkillConfig ResolveSkillById(
            ref SkillConfig cached,
            string skillId,
            Func<SkillConfig> fallbackFactory)
        {
            if (cached != null)
                return cached;

            GameConfigService configService =
                Resources.Load<GameConfigService>("Configuration/GameConfigService");
            if (configService != null)
                cached = configService.GetSkill(skillId);
            if (cached == null)
                cached = Resources.Load<SkillConfig>("Configuration/Skills/" + skillId);
            if (cached == null && fallbackFactory != null)
                cached = fallbackFactory();
            return cached;
        }

        int ResolveActiveFlyingSwordLevel()
        {
            SkillConfig skill = ResolveFlyingSwordSkill();
            int controllerLevel = flyingSwordSkillController != null
                ? flyingSwordSkillController.SkillLevel
                : lastSyncedFlyingSwordLevel;
            int configLevel = skill != null ? skill.Level : lastSyncedFlyingSwordLevel;
            int inspectorLevel = Mathf.Clamp(flyingSwordSkillLevel, 1, SkillConfig.MaxLevel);

            int resolved = lastSyncedFlyingSwordLevel;
            if (inspectorLevel != lastSyncedFlyingSwordLevel)
                resolved = inspectorLevel;
            else if (controllerLevel != lastSyncedFlyingSwordLevel)
                resolved = controllerLevel;
            else if (configLevel != lastSyncedFlyingSwordLevel)
                resolved = configLevel;

            resolved = Mathf.Clamp(resolved, 1, SkillConfig.MaxLevel);
            lastSyncedFlyingSwordLevel = resolved;
            flyingSwordSkillLevel = resolved;
            if (flyingSwordSkillController != null)
                flyingSwordSkillController.SkillLevel = resolved;
            if (skill != null)
                skill.Level = resolved;
            return resolved;
        }

        public bool CanActivateFlyingSwordSkill()
        {
            return CanActivateSkill(
                ResolveFlyingSwordSkill(),
                flyingSwordCooldownRemaining,
                IsFlyingSwordSkillRunning,
                SkillConfig.DefaultManaCost,
                SkillConfig.DefaultRageCost);
        }

        public bool TryConsumeFlyingSwordSkillCost()
        {
            if (!TryConsumeSkillCost(
                ResolveFlyingSwordSkill(),
                flyingSwordCooldownRemaining,
                IsFlyingSwordSkillRunning,
                SkillConfig.DefaultManaCost,
                SkillConfig.DefaultRageCost,
                SkillConfig.DefaultCooldownTurns,
                out int newCooldown))
                return false;

            flyingSwordCooldownRemaining = newCooldown;
            flyingSwordOrbitTurnsRemaining = SkillConfig.FlyingSwordOrbitDurationTurns;
            return true;
        }

        public bool CanActivateSwordQiSkill()
        {
            return CanActivateSkill(
                ResolveSwordQiSkill(),
                swordQiCooldownRemaining,
                IsSwordQiSkillRunning,
                SkillConfig.SwordQiDefaultManaCost,
                SkillConfig.SwordQiDefaultRageCost);
        }

        public bool TryConsumeSwordQiSkillCost()
        {
            if (!TryConsumeSkillCost(
                ResolveSwordQiSkill(),
                swordQiCooldownRemaining,
                IsSwordQiSkillRunning,
                SkillConfig.SwordQiDefaultManaCost,
                SkillConfig.SwordQiDefaultRageCost,
                SkillConfig.SwordQiDefaultCooldownTurns,
                out int newCooldown))
                return false;

            swordQiCooldownRemaining = newCooldown;
            return true;
        }

        public bool CanActivateRockSpikeSkill()
        {
            return CanActivateSkill(
                ResolveRockSpikeSkill(),
                rockSpikeCooldownRemaining,
                IsRockSpikeSkillRunning,
                SkillConfig.RockSpikeDefaultManaCost,
                SkillConfig.RockSpikeDefaultRageCost);
        }

        public bool TryConsumeRockSpikeSkillCost()
        {
            if (!TryConsumeSkillCost(
                ResolveRockSpikeSkill(),
                rockSpikeCooldownRemaining,
                IsRockSpikeSkillRunning,
                SkillConfig.RockSpikeDefaultManaCost,
                SkillConfig.RockSpikeDefaultRageCost,
                SkillConfig.RockSpikeDefaultCooldownTurns,
                out int newCooldown))
                return false;

            rockSpikeCooldownRemaining = newCooldown;
            return true;
        }

        public bool CanActivateEllipseSwordsSkill()
        {
            return CanActivateSkill(
                ResolveEllipseSwordsSkill(),
                ellipseSwordsCooldownRemaining,
                IsEllipseSwordsSkillRunning,
                SkillConfig.EllipseSwordsDefaultManaCost,
                SkillConfig.EllipseSwordsDefaultRageCost);
        }

        public bool TryConsumeEllipseSwordsSkillCost()
        {
            if (!TryConsumeSkillCost(
                ResolveEllipseSwordsSkill(),
                ellipseSwordsCooldownRemaining,
                IsEllipseSwordsSkillRunning,
                SkillConfig.EllipseSwordsDefaultManaCost,
                SkillConfig.EllipseSwordsDefaultRageCost,
                SkillConfig.EllipseSwordsDefaultCooldownTurns,
                out int newCooldown))
                return false;

            ellipseSwordsCooldownRemaining = newCooldown;
            return true;
        }

        public bool CanActivateStarfallBarrageSkill()
        {
            return CanActivateSkill(
                ResolveStarfallBarrageSkill(),
                starfallBarrageCooldownRemaining,
                IsStarfallBarrageSkillRunning,
                SkillConfig.StarfallBarrageDefaultManaCost,
                SkillConfig.StarfallBarrageDefaultRageCost);
        }

        public bool TryConsumeStarfallBarrageSkillCost()
        {
            if (!TryConsumeSkillCost(
                ResolveStarfallBarrageSkill(),
                starfallBarrageCooldownRemaining,
                IsStarfallBarrageSkillRunning,
                SkillConfig.StarfallBarrageDefaultManaCost,
                SkillConfig.StarfallBarrageDefaultRageCost,
                SkillConfig.StarfallBarrageDefaultCooldownTurns,
                out int newCooldown))
                return false;

            starfallBarrageCooldownRemaining = newCooldown;
            return true;
        }

        public bool CanActivateThunderGodSkill()
        {
            return CanActivateSkill(
                ResolveThunderGodSkill(),
                thunderGodCooldownRemaining,
                IsThunderGodSkillRunning,
                SkillConfig.ThunderGodDefaultManaCost,
                SkillConfig.ThunderGodDefaultRageCost);
        }

        public bool TryConsumeThunderGodSkillCost()
        {
            if (!TryConsumeSkillCost(
                ResolveThunderGodSkill(),
                thunderGodCooldownRemaining,
                IsThunderGodSkillRunning,
                SkillConfig.ThunderGodDefaultManaCost,
                SkillConfig.ThunderGodDefaultRageCost,
                SkillConfig.ThunderGodDefaultCooldownTurns,
                out int newCooldown))
                return false;

            thunderGodCooldownRemaining = newCooldown;
            return true;
        }

        bool CanActivateSkill(SkillConfig skill, int cooldownRemaining, bool isRunning,
            int defaultManaCost, int defaultRageCost)
        {
            if (IsGameOver || cooldownRemaining > 0 || isRunning)
                return false;

            int requiredMana = skill != null ? skill.ManaCost : defaultManaCost;
            int requiredRage = skill != null ? skill.RageCost : defaultRageCost;
            return playerManaGemCount >= requiredMana && playerRageGemCount >= requiredRage;
        }

        bool TryConsumeSkillCost(SkillConfig skill, int cooldownRemaining, bool isRunning,
            int defaultManaCost, int defaultRageCost, int defaultCooldown,
            out int newCooldownRemaining)
        {
            if (!CanActivateSkill(skill, cooldownRemaining, isRunning, defaultManaCost, defaultRageCost))
            {
                newCooldownRemaining = cooldownRemaining;
                return false;
            }

            int requiredMana = skill != null ? skill.ManaCost : defaultManaCost;
            int requiredRage = skill != null ? skill.RageCost : defaultRageCost;
            int cooldown = skill != null ? skill.CooldownTurns : defaultCooldown;

            playerManaGemCount = Mathf.Max(0, playerManaGemCount - requiredMana);
            playerRageGemCount = Mathf.Max(0, playerRageGemCount - requiredRage);
            mana = Mathf.Clamp(mana - requiredMana, 0, GetMaxMana(BattleActor.Player));
            heroRage = Mathf.Clamp(heroRage - requiredRage, 0, MaxRage);
            newCooldownRemaining = Mathf.Max(0, cooldown);
            UpdateRageReadyVfx(BattleActor.Player);
            Refresh();
            return true;
        }

        public IEnumerator ExecuteFlyingSwordSkill(BoardManager board)
        {
            if (IsGameOver)
                yield break;

            EnsureFlyingSwordSkillSetup();
            SkillConfig skill = ResolveFlyingSwordSkill();
            int activeLevel = ResolveActiveFlyingSwordLevel();
            float atkMultiplier = skill != null
                ? skill.GetAttackGemDamageMultiplier(activeLevel)
                : 1f;

            MoveResult skillMoveResult = null;
            if (flyingSwordSkillController != null)
            {
                yield return flyingSwordSkillController.ExecuteSkill(hero, board, skill,
                    activeLevel, result => skillMoveResult = result,
                    keepOrbitingAfterSkill: true);
            }

            if (skillMoveResult != null && skillMoveResult.IsValid && !IsGameOver)
            {
                yield return ResolveMove(BattleActor.Player, skillMoveResult,
                    atkMultiplier, isSkillGemResolution: true);
            }

            StopFlyingSwordOrbitIfBattleEnded();
        }

        public IEnumerator ExecuteSwordQiSkill()
        {
            if (IsGameOver)
                yield break;

            EnsureSwordQiSkillSetup();
            SkillConfig skill = ResolveSwordQiSkill();
            if (swordQiSkillController == null)
                yield break;

            yield return swordQiSkillController.ExecuteSkill(hero, boss, skill, ApplySwordQiImpact);
        }

        IEnumerator ApplySwordQiImpact()
        {
            if (IsGameOver)
                yield break;

            combatVfx?.PlaySwordQiImpact(BattleActor.Boss);

            // GIẢ ĐỊNH TẠM: sát thương bằng 1 đòn đánh thường của hero (HeroAttackDamagePerGem),
            // tính đúng lúc kiếm khí chạm boss.
            int damage = ApplyDamage(BattleActor.Boss, HeroAttackDamagePerGem);
            if (damage > 0)
            {
                ShowResource(boss, FloatingTextType.Damage, damage);
                AddReceivedHitRage(BattleActor.Boss);
                Refresh();
                if (boss != null && bossHp > 0)
                    yield return boss.Hit();
            }

            if (IsGameOver && PlayerWon && boss != null)
                yield return boss.Death();
            StopFlyingSwordOrbitIfBattleEnded();
            Refresh();
        }

        public IEnumerator ExecuteRockSpikeSkill()
        {
            if (IsGameOver)
                yield break;

            EnsureRockSpikeSkillSetup();
            SkillConfig skill = ResolveRockSpikeSkill();
            if (rockSpikeSkillController == null)
                yield break;

            yield return rockSpikeSkillController.ExecuteSkill(hero, boss, skill, ApplyRockSpikeImpact);
        }

        IEnumerator ApplyRockSpikeImpact()
        {
            if (IsGameOver)
                yield break;

            combatVfx?.PlayHitImpact(BattleActor.Boss);

            // TẠM: sát thương bằng 1 đòn đánh thường của hero (HeroAttackDamagePerGem),
            // tính đúng lúc gai đá đạt đỉnh (frame 13).
            int damage = ApplyDamage(BattleActor.Boss, HeroAttackDamagePerGem);
            if (damage > 0)
            {
                ShowResource(boss, FloatingTextType.Damage, damage);
                AddReceivedHitRage(BattleActor.Boss);
                Refresh();
                if (boss != null && bossHp > 0)
                    yield return boss.Hit();
            }

            if (IsGameOver && PlayerWon && boss != null)
                yield return boss.Death();
            StopFlyingSwordOrbitIfBattleEnded();
            Refresh();
        }

        public IEnumerator ExecuteEllipseSwordsSkill()
        {
            if (IsGameOver)
                yield break;

            EnsureEllipseSwordsSkillSetup();
            SkillConfig skill = ResolveEllipseSwordsSkill();
            if (ellipseSwordsSkillController == null)
                yield break;

            yield return ellipseSwordsSkillController.ExecuteSkill(
                hero,
                boss,
                skill,
                ApplyEllipseSwordsImpact,
                () => IsGameOver || bossHp <= 0);
        }

        IEnumerator ApplyEllipseSwordsImpact(int swordIndex)
        {
            if (IsGameOver || bossHp <= 0)
                yield break;

            combatVfx?.PlayHitImpact(BattleActor.Boss);

            // TẠM: tổng sát thương 5 kiếm = 1 đòn đánh thường của hero (HeroAttackDamagePerGem),
            // chia đều số nguyên cho 5 kiếm, kiếm số 0 (cổng #1) lấy phần dư.
            int totalDamage = HeroAttackDamagePerGem;
            int swordCount = SkillConfig.EllipseSwordsDefaultSwordCount;
            int baseDamage = totalDamage / swordCount;
            int remainder = totalDamage % swordCount;
            int rawDamage = baseDamage + (swordIndex == 0 ? remainder : 0);

            int damage = ApplyDamage(BattleActor.Boss, rawDamage);
            if (damage > 0)
            {
                ShowResource(boss, FloatingTextType.Damage, damage);
                AddReceivedHitRage(BattleActor.Boss);
                Refresh();
                if (boss != null && bossHp > 0)
                    yield return boss.Hit();
            }

            if (IsGameOver && PlayerWon && boss != null)
                yield return boss.Death();
            StopFlyingSwordOrbitIfBattleEnded();
            Refresh();
        }

        public IEnumerator ExecuteStarfallBarrageSkill()
        {
            if (IsGameOver)
                yield break;

            EnsureStarfallBarrageSkillSetup();
            SkillConfig skill = ResolveStarfallBarrageSkill();
            if (starfallBarrageSkillController == null)
                yield break;

            yield return starfallBarrageSkillController.ExecuteSkill(hero, boss, skill, ApplyStarfallBarrageImpact);
        }

        IEnumerator ApplyStarfallBarrageImpact()
        {
            if (IsGameOver)
                yield break;

            combatVfx?.PlayHitImpact(BattleActor.Boss);

            // TẠM: sát thương phẳng = 100 (StarfallBarrageDefaultDamage),
            // tính đúng lúc vòng nổ dưới đất đạt đỉnh (frame 52). Sư Huynh chốt sau Play-test.
            int damage = ApplyDamage(BattleActor.Boss, SkillConfig.StarfallBarrageDefaultDamage);
            if (damage > 0)
            {
                ShowResource(boss, FloatingTextType.Damage, damage);
                AddReceivedHitRage(BattleActor.Boss);
                Refresh();
                if (boss != null && bossHp > 0)
                    yield return boss.Hit();
            }

            if (IsGameOver && PlayerWon && boss != null)
                yield return boss.Death();
            StopFlyingSwordOrbitIfBattleEnded();
            Refresh();
        }

        public IEnumerator ExecuteThunderGodSkill()
        {
            if (IsGameOver)
                yield break;

            EnsureThunderGodSkillSetup();
            SkillConfig skill = ResolveThunderGodSkill();
            if (thunderGodSkillController == null)
                yield break;

            yield return thunderGodSkillController.ExecuteSkill(hero, boss, skill, ApplyThunderGodImpact);
        }

        IEnumerator ApplyThunderGodImpact()
        {
            if (IsGameOver)
                yield break;

            combatVfx?.PlayHitImpact(BattleActor.Boss);

            // TẠM: sát thương phẳng = 100 (ThunderGodDefaultDamage),
            // tính đúng lúc sét đánh trúng vòng xanh dưới đất đạt đỉnh (frame 46). Sư Huynh chốt sau Play-test.
            int damage = ApplyDamage(BattleActor.Boss, SkillConfig.ThunderGodDefaultDamage);
            if (damage > 0)
            {
                ShowResource(boss, FloatingTextType.Damage, damage);
                AddReceivedHitRage(BattleActor.Boss);
                Refresh();
                if (boss != null && bossHp > 0)
                    yield return boss.Hit();
            }

            if (IsGameOver && PlayerWon && boss != null)
                yield return boss.Death();
            StopFlyingSwordOrbitIfBattleEnded();
            Refresh();
        }

        float ApplyDrain(BattleActor actor, int gemCount)
        {
            if (gemCount <= 0)
                return 0f;

            BattleActor targetActor = actor == BattleActor.Player
                ? BattleActor.Boss
                : BattleActor.Player;
            CharacterView actorView = actor == BattleActor.Player ? hero : boss;
            CharacterView targetView = targetActor == BattleActor.Player ? hero : boss;

            int targetHealth = targetActor == BattleActor.Player ? heroHp : bossHp;
            int receiverHealthRoom = actor == BattleActor.Player
                ? Mathf.Max(0, heroMaxHp - heroHp)
                : Mathf.Max(0, bossMaxHp - bossHp);
            int hpToTransfer = Mathf.Min(gemCount * drainHpPerGem,
                Mathf.Min(targetHealth, receiverHealthRoom));
            int hpLost = ApplyDamage(targetActor, hpToTransfer, false);
            int hpGained = RestoreHealth(actor, hpLost);
            ShowResource(targetView, FloatingTextType.Damage, hpLost);
            ShowResource(actorView, FloatingTextType.Heal, hpGained);
            float lifeStealDuration = 0f;
            if (hpGained > 0 && combatVfx != null && combatVfx.PlayLifeSteal(actor))
                lifeStealDuration = CombatVFXManager.LifeStealDuration;

            int targetRageBefore = GetRage(targetActor);
            int receiverRageRoom = Mathf.Max(0, GetMaxRage(actor) - GetRage(actor));
            int rageToTransfer = Mathf.Min(gemCount * drainRagePerGem,
                Mathf.Min(targetRageBefore, receiverRageRoom));
            SetRage(targetActor, targetRageBefore - rageToTransfer);
            int rageLost = targetRageBefore - GetRage(targetActor);
            int rageGained = AddRage(actor, rageLost);
            ShowResource(targetView, FloatingTextType.RageLost, rageLost);
            ShowResource(actorView, FloatingTextType.Rage, rageGained);

            int targetManaBefore = GetMana(targetActor);
            int requestedMana = Mathf.FloorToInt(GetMaxMana(targetActor)
                * drainManaPercentPerGem * gemCount / 100f);
            int receiverManaRoom = Mathf.Max(0, GetMaxMana(actor) - GetMana(actor));
            int manaToTransfer = Mathf.Min(requestedMana,
                Mathf.Min(targetManaBefore, receiverManaRoom));
            SetMana(targetActor, targetManaBefore - manaToTransfer);
            int manaLost = targetManaBefore - GetMana(targetActor);
            int manaGained = AddMana(actor, manaLost);
            ShowResource(targetView, FloatingTextType.ManaLost, manaLost);
            ShowResource(actorView, FloatingTextType.Mana, manaGained);
            Refresh();
            return lifeStealDuration;
        }

        int RestoreHealth(BattleActor actor, int amount)
        {
            if (amount <= 0) return 0;
            if (actor == BattleActor.Player)
            {
                int previous = heroHp;
                heroHp = Mathf.Min(heroMaxHp, heroHp + amount);
                if (heroHp > 0 && isSoulBurning)
                {
                    StopSoulBurn();
                    Refresh();
                }
                return heroHp - previous;
            }

            int previousBoss = bossHp;
            bossHp = Mathf.Min(bossMaxHp, bossHp + amount);
            if (bossHp > 0 && isBossSoulBurning)
            {
                StopBossSoulBurn();
                Refresh();
            }
            return bossHp - previousBoss;
        }

        int AddMana(BattleActor actor, int amount)
        {
            if (amount <= 0) return 0;
            int previous = GetMana(actor);
            SetMana(actor, previous + amount);
            return GetMana(actor) - previous;
        }

        int GetMana(BattleActor actor) => actor == BattleActor.Player ? mana : bossMana;

        int GetMaxMana(BattleActor actor) => actor == BattleActor.Player
            ? Mathf.Max(1, maxMana)
            : Mathf.Max(1, bossMaxMana);

        void SetMana(BattleActor actor, int value)
        {
            if (actor == BattleActor.Player)
                mana = Mathf.Clamp(value, 0, GetMaxMana(actor));
            else
                bossMana = Mathf.Clamp(value, 0, GetMaxMana(actor));
        }

        int AddReceivedHitRage(BattleActor actor)
        {
            if (layoutTestMode)
                return 0;
            int rageGain = actor == BattleActor.Player ? ragePerReceivedHit : bossRagePerReceivedHit;
            if (rageGain <= 0) return 0;
            return AddRage(actor, rageGain);
        }

        int AddRage(BattleActor actor, int amount)
        {
            if (layoutTestMode)
            {
                SetRage(actor, GetMaxRage(actor));
                return 0;
            }
            int previous = GetRage(actor);
            SetRage(actor, previous + Mathf.Max(0, amount));
            int gained = GetRage(actor) - previous;
            int maximum = GetMaxRage(actor);
            if (gained > 0 && previous < maximum && GetRage(actor) >= maximum)
                ShowResource(actor == BattleActor.Player ? hero : boss, FloatingTextType.CriticalReady, 1);
            return gained;
        }

        int GetRage(BattleActor actor) => actor == BattleActor.Player ? heroRage : bossRage;

        int GetMaxRage(BattleActor actor) => actor == BattleActor.Player ? MaxRage : BossMaxRage;

        bool IsCriticalEnabled(BattleActor actor) =>
            actor == BattleActor.Player || bossCriticalEnabled;

        void SetRage(BattleActor actor, int value)
        {
            if (layoutTestMode)
            {
                if (actor == BattleActor.Player)
                    heroRage = Mathf.Max(0, MaxRage);
                else
                    bossRage = Mathf.Max(0, BossMaxRage);
                UpdateRageReadyVfx(actor);
                Refresh();
                return;
            }
            if (actor == BattleActor.Player)
                heroRage = Mathf.Clamp(value, 0, MaxRage);
            else
                bossRage = Mathf.Clamp(value, 0, BossMaxRage);
            UpdateRageReadyVfx(actor);
            Refresh();
        }

        void UpdateRageReadyVfx(BattleActor actor)
        {
            int current = GetRage(actor);
            int maximum = GetMaxRage(actor);
            bool isReady = maximum > 0 && current >= maximum;
            combatVfx?.SetRageReady(actor, isReady);
            if (actor == BattleActor.Player)
            {
                boardRageFireVfx?.SetRageReady(isReady);
                if (current != loggedHeroRage || maximum != loggedHeroMaxRage)
                {
                    loggedHeroRage = current;
                    loggedHeroMaxRage = maximum;
                    Debug.Log($"[HeroRage] Current={current}; Max={maximum}; IsReady={isReady}.", this);
                }
            }
        }

        static int Count(MoveResult move, GemType type)
        {
            return move.GemCounts.TryGetValue(type, out int count) ? count : 0;
        }

        static int AddNonNegative(int value, int bonus)
        {
            long total = (long)Mathf.Max(0, value) + Mathf.Max(0, bonus);
            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        static int MultiplyNonNegative(int value, int multiplier)
        {
            long total = (long)Mathf.Max(0, value) * Mathf.Max(0, multiplier);
            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        void ShowResource(CharacterView target, FloatingTextType type, int actualChange)
        {
            if (actualChange > 0 && floatingTextManager != null)
                floatingTextManager.Show(target, type, actualChange);
        }

        void Refresh()
        {
            if (hud != null)
                hud.Render(heroHp, heroMaxHp, bossHp, bossMaxHp, heroRage, bossRage,
                    MaxRage, BossMaxRage, mana, ultimate, heroSoulHp, heroMaxSoulHp,
                    bossSoulHp, bossMaxSoulHp, soulEnabled);
        }

#if UNITY_EDITOR
        public void ConfigureDebugStats(int playerMaxHealth, int opponentMaxHealth,
            int opponentAttackDamage = 20)
        {
            heroMaxHp = Mathf.Max(1, playerMaxHealth);
            bossMaxHp = Mathf.Max(1, opponentMaxHealth);
            heroAttack = 50;
            heroSoulAttack = 50;
            heroCriticalChancePercent = 0f;
            criticalDamageMultiplier = 2f;
            heroDefense = 20;
            heroDodgePercent = 0f;
            heroAccuracyPercent = 100f;
            heroSpeed = 10;
            bossDefense = 20;
            bossDodgePercent = 0f;
            bossAccuracyPercent = 100f;
            bossSpeed = 10;
            bossAttackDamagePerGem = Mathf.Max(0, opponentAttackDamage);
            maxMana = 100;
            maxRage = 100;
            bossMaxMana = 100;
            bossMaxRage = 100;
            statsInitialized = false;
            InitializeStats();
        }

        public void SetRageForDebug(BattleActor actor, int value)
        {
            SetRage(actor, value);
        }
#endif
    }

    public sealed class FlyingSwordSkillController : MonoBehaviour
    {
        public const float DefaultOrbitSecondsPerRevolution = 1.44f;
        public const float DefaultOrbitRadiusXRatio = 0.37f;
        public const float DefaultOrbitRadiusYRatio = 0.13f;
        public const float DefaultOrbitBackAlpha = 0.55f;
        public const float DefaultOrbitEasingStrength = 0.25f;
        public const float DefaultOrbitSwordLengthRatio = 0.42f;
        public const float DefaultRowClearSweepDuration = 0.60f;
        public const float DefaultDelayBetweenRowSwords = 0.15f;

        [Header("Play-test Skill Level (Lv1 - Lv5)")]
        [SerializeField, Range(1, SkillConfig.MaxLevel)] int skillLevel = SkillConfig.DefaultLevel;

        [Header("Orbit (Kiếm bay quanh hero)")]
        [SerializeField, Min(0.2f)] float orbitSecondsPerRevolution = DefaultOrbitSecondsPerRevolution;
        [SerializeField, Min(0.05f)] float orbitRadiusXRatio = DefaultOrbitRadiusXRatio;
        [SerializeField, Min(0.05f)] float orbitRadiusYRatio = DefaultOrbitRadiusYRatio;
        [SerializeField, Range(0.1f, 1f)] float orbitBackAlpha = DefaultOrbitBackAlpha;
        [SerializeField, Range(0f, 0.45f)] float orbitEasingStrength = DefaultOrbitEasingStrength;
        [SerializeField, Min(0.1f)] float orbitSwordLengthRatio = DefaultOrbitSwordLengthRatio;
        [SerializeField, Range(0, 5)] int orbitTrailCount = 3;
        [SerializeField, Min(0.005f)] float orbitTrailPhaseStep = 0.032f;
        [SerializeField, Min(0f)] float orbitLeadInDuration = 0.20f;
        [SerializeField, Min(0f)] float orbitFadeOutDuration = 0.18f;

        [Header("Row Clear (Kiếm diệt hàng ngang)")]
        [SerializeField, Min(0.1f)] float rowClearSweepDuration = DefaultRowClearSweepDuration;
        [SerializeField, Min(0f)] float delayBetweenRowSwords = DefaultDelayBetweenRowSwords;
        [SerializeField, Min(0.5f)] float rowSwordCellWidthMultiplier = 2.2f;
        [SerializeField, Range(0, 5)] int rowTrailCount = 3;

        static readonly float[] TrailAlphaWeights = { 0.38f, 0.22f, 0.11f, 0.06f, 0.03f };
        static readonly float[] TrailScaleWeights = { 0.95f, 0.90f, 0.85f, 0.80f, 0.75f };

        readonly List<OrbitSwordInstance> activeOrbitSwords = new List<OrbitSwordInstance>();
        readonly List<GameObject> activeRowSwords = new List<GameObject>();
        CharacterView heroView;
        RectTransform backOrbitLayer;
        RectTransform frontOrbitLayer;
        RectTransform rowClearLayer;
        float orbitElapsed;
        float orbitEnvelopeAlpha = 1f;
        bool isOrbiting;
        bool isRunning;
        Coroutine orbitFadeRoutine;

        public bool IsRunning => isRunning;
        public int SkillLevel
        {
            get => Mathf.Clamp(skillLevel, 1, SkillConfig.MaxLevel);
            set => skillLevel = Mathf.Clamp(value, 1, SkillConfig.MaxLevel);
        }
        public float OrbitSecondsPerRevolution => orbitSecondsPerRevolution;
        public float DelayBetweenRowSwords => delayBetweenRowSwords;
        public float RowClearSweepDuration => rowClearSweepDuration;

        sealed class OrbitSwordInstance
        {
            public float PhaseOffset;
            public RectTransform MainRect;
            public Image MainImage;
            public RectTransform[] TrailRects;
            public Image[] TrailImages;
        }

        public void Configure(CharacterView hero)
        {
            heroView = hero;
            EnsureOrbitLayers();
        }

        void OnDisable()
        {
            StopAndClear();
        }

        public void StopAndClear()
        {
            if (orbitFadeRoutine != null)
            {
                StopCoroutine(orbitFadeRoutine);
                orbitFadeRoutine = null;
            }
            isOrbiting = false;
            isRunning = false;
            ClearOrbitSwords();
            ClearRowSwords();
        }

        public void StopOrbitImmediate()
        {
            if (orbitFadeRoutine != null)
            {
                StopCoroutine(orbitFadeRoutine);
                orbitFadeRoutine = null;
            }
            isOrbiting = false;
            ClearOrbitSwords();
        }

        public void FadeOutAndStopOrbit()
        {
            if (!isOrbiting && activeOrbitSwords.Count == 0)
                return;
            if (!isActiveAndEnabled || orbitFadeOutDuration <= 0f)
            {
                StopOrbitImmediate();
                return;
            }
            if (orbitFadeRoutine != null)
                StopCoroutine(orbitFadeRoutine);
            orbitFadeRoutine = StartCoroutine(FadeOutAndClearOrbitRoutine());
        }

        IEnumerator FadeOutAndClearOrbitRoutine()
        {
            float fadeElapsed = 0f;
            float duration = Mathf.Max(0.01f, orbitFadeOutDuration);
            float startAlpha = orbitEnvelopeAlpha;
            while (fadeElapsed < duration && isOrbiting)
            {
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                fadeElapsed += dt;
                orbitEnvelopeAlpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(fadeElapsed / duration));
                yield return null;
            }
            isOrbiting = false;
            ClearOrbitSwords();
            orbitFadeRoutine = null;
        }

        void Update()
        {
            if (!isOrbiting || activeOrbitSwords.Count == 0 || heroView == null)
                return;

            float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
            orbitElapsed += dt;
            UpdateOrbitPositions();
        }

        public static float MeasureHeroBattleHeight(CharacterView hero)
        {
            if (hero == null || hero.VisualRect == null)
                return 394.74f;

            RectTransform visualRect = hero.VisualRect;
            float unscaled = Mathf.Min(visualRect.rect.width, visualRect.rect.height);
            if (unscaled <= 1f)
                unscaled = 464.4f;
            float scaleY = Mathf.Abs(visualRect.localScale.y);
            if (scaleY <= 0.01f)
                scaleY = 0.85f;
            return unscaled * scaleY;
        }

        public static Vector2 CalculateOrbitSwordSize(CharacterView hero, Sprite swordSprite)
        {
            float heroHeight = MeasureHeroBattleHeight(hero);
            float spriteAspect = swordSprite != null && swordSprite.rect.width > 0f
                ? swordSprite.rect.height / swordSprite.rect.width
                : 0.5f;
            float swordLength = heroHeight * Mathf.Max(0.15f, DefaultOrbitSwordLengthRatio);
            return new Vector2(swordLength, swordLength * spriteAspect);
        }

        public IEnumerator ExecuteSkill(CharacterView hero, BoardManager board,
            SkillConfig config, int level, Action<MoveResult> onRowsCleared,
            bool keepOrbitingAfterSkill = false)
        {
            if (isRunning)
                yield break;

            isRunning = true;
            heroView = hero != null ? hero : heroView;
            SkillLevel = level;
            int activeLevel = SkillLevel;
            int orbitCount = config != null ? config.GetOrbitSwordCount(activeLevel) : 1;
            int rowClearCount = config != null ? config.GetRowClearCount(activeLevel) : 1;
            Sprite swordSprite = config != null ? config.Icon : SkillConfig.ResolveSwordSprite();

            if (swordSprite == null)
            {
                Debug.LogError($"[FlyingSwordSkill] Missing sword sprite at {SkillConfig.FlyingSwordAssetPath}.", this);
                isRunning = false;
                yield break;
            }

            bool wasLayoutEnabled = false;
            bool boardLocked = false;
            bool completedNormally = false;
            MoveResult moveResult = new MoveResult(new BoardPos(0, 0), new BoardPos(0, 0))
            {
                IsValid = true,
                IsDirectActivation = true,
                WasSpecialActivation = false,
                WasInitialMove = false
            };
            MatchResult matchResult = new MatchResult();

            try
            {
                if (orbitFadeRoutine != null)
                {
                    StopCoroutine(orbitFadeRoutine);
                    orbitFadeRoutine = null;
                }
                SpawnOrbitSwords(swordSprite, orbitCount);
                orbitElapsed = 0f;
                orbitEnvelopeAlpha = 1f;
                isOrbiting = true;
                UpdateOrbitPositions();

                if (orbitLeadInDuration > 0f)
                    yield return new WaitForSeconds(orbitLeadInDuration);

                if (board != null)
                {
                    board.BeginSkillRowClear(out wasLayoutEnabled);
                    boardLocked = true;
                    EnsureRowClearLayer(board);

                    List<int> selectedRows = SelectRandomDistinctRows(board.Rows, rowClearCount);
                    for (int i = 0; i < selectedRows.Count; i++)
                    {
                        int targetRow = selectedRows[i];
                        yield return SweepSingleRow(board, swordSprite, targetRow, matchResult);
                        if (i + 1 < selectedRows.Count && delayBetweenRowSwords > 0f)
                            yield return new WaitForSeconds(delayBetweenRowSwords);
                    }

                    boardLocked = false;
                    yield return board.FinishSkillRowClear(wasLayoutEnabled);
                }

                if (!keepOrbitingAfterSkill && orbitFadeOutDuration > 0f)
                {
                    float fadeElapsed = 0f;
                    while (fadeElapsed < orbitFadeOutDuration)
                    {
                        fadeElapsed += Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                        orbitEnvelopeAlpha = 1f - Mathf.Clamp01(fadeElapsed / orbitFadeOutDuration);
                        yield return null;
                    }
                }
                completedNormally = true;
            }
            finally
            {
                if (boardLocked && board != null)
                    StartCoroutine(board.FinishSkillRowClear(wasLayoutEnabled));
                if (!keepOrbitingAfterSkill || !completedNormally)
                {
                    isOrbiting = false;
                    ClearOrbitSwords();
                }
                ClearRowSwords();
                isRunning = false;
            }

            if (matchResult.ClearPositions.Count > 0)
                moveResult.AddMatch(matchResult);
            onRowsCleared?.Invoke(moveResult);
        }

        static List<int> SelectRandomDistinctRows(int totalRows, int requestedCount)
        {
            int clampedCount = Mathf.Clamp(requestedCount, 1, Mathf.Max(1, totalRows));
            List<int> pool = new List<int>(totalRows);
            for (int r = 0; r < totalRows; r++)
                pool.Add(r);

            for (int i = 0; i < clampedCount; i++)
            {
                int pick = UnityEngine.Random.Range(i, pool.Count);
                int temp = pool[i];
                pool[i] = pool[pick];
                pool[pick] = temp;
            }

            return pool.GetRange(0, clampedCount);
        }

        IEnumerator SweepSingleRow(BoardManager board, Sprite swordSprite, int row,
            MatchResult matchResult)
        {
            int columns = board.Columns;
            if (columns <= 0 || rowClearLayer == null)
                yield break;

            Vector2[] cellLocalCenters = new Vector2[columns];
            for (int col = 0; col < columns; col++)
            {
                Vector3 worldCenter = board.GetCellWorldCenter(row, col);
                cellLocalCenters[col] = rowClearLayer.InverseTransformPoint(worldCenter);
            }

            float rowY = cellLocalCenters[0].y;
            float cellWidth = columns > 1
                ? Mathf.Abs(cellLocalCenters[1].x - cellLocalCenters[0].x)
                : 125f;
            if (cellWidth <= 1f)
                cellWidth = 125f;

            float spriteAspect = swordSprite.rect.width > 0f
                ? swordSprite.rect.height / swordSprite.rect.width
                : 0.5f;
            float swordWidth = cellWidth * Mathf.Max(0.5f, rowSwordCellWidthMultiplier);
            float swordHeight = swordWidth * spriteAspect;
            Vector2 swordSize = new Vector2(swordWidth, swordHeight);

            float startX = cellLocalCenters[0].x - cellWidth * 0.75f;
            float endX = cellLocalCenters[columns - 1].x + cellWidth * 0.75f;

            GameObject swordRootObj = new GameObject($"RowClearSword_Row{row}", typeof(RectTransform));
            activeRowSwords.Add(swordRootObj);
            RectTransform swordRoot = swordRootObj.GetComponent<RectTransform>();
            swordRoot.SetParent(rowClearLayer, false);
            swordRoot.anchorMin = new Vector2(0.5f, 0.5f);
            swordRoot.anchorMax = new Vector2(0.5f, 0.5f);
            swordRoot.pivot = new Vector2(0.5f, 0.5f);
            swordRoot.sizeDelta = swordSize;
            swordRoot.anchoredPosition = new Vector2(startX, rowY);
            swordRoot.localEulerAngles = Vector3.zero;
            swordRoot.localScale = Vector3.one;

            int trailSteps = Mathf.Clamp(rowTrailCount, 0, TrailAlphaWeights.Length);
            for (int t = trailSteps - 1; t >= 0; t--)
            {
                GameObject trailObj = new GameObject($"RowTrail_{t}", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                RectTransform trailRect = trailObj.GetComponent<RectTransform>();
                trailRect.SetParent(swordRoot, false);
                trailRect.anchorMin = new Vector2(0.5f, 0.5f);
                trailRect.anchorMax = new Vector2(0.5f, 0.5f);
                trailRect.pivot = new Vector2(0.5f, 0.5f);
                trailRect.sizeDelta = swordSize * TrailScaleWeights[t];
                trailRect.anchoredPosition = new Vector2(-(t + 1) * cellWidth * 0.24f, 0f);
                trailRect.localEulerAngles = Vector3.zero;
                Image trailImage = trailObj.GetComponent<Image>();
                trailImage.sprite = swordSprite;
                trailImage.preserveAspect = true;
                trailImage.raycastTarget = false;
                trailImage.color = new Color(0.75f, 0.94f, 1f, TrailAlphaWeights[t]);
            }

            GameObject mainObj = new GameObject("SwordBody", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            RectTransform mainRect = mainObj.GetComponent<RectTransform>();
            mainRect.SetParent(swordRoot, false);
            mainRect.anchorMin = new Vector2(0.5f, 0.5f);
            mainRect.anchorMax = new Vector2(0.5f, 0.5f);
            mainRect.pivot = new Vector2(0.5f, 0.5f);
            mainRect.sizeDelta = swordSize;
            mainRect.anchoredPosition = Vector2.zero;
            mainRect.localEulerAngles = Vector3.zero;
            Image mainImage = mainObj.GetComponent<Image>();
            mainImage.sprite = swordSprite;
            mainImage.preserveAspect = true;
            mainImage.raycastTarget = false;
            mainImage.color = Color.white;

            bool[] columnCleared = new bool[columns];
            float duration = Mathf.Max(0.1f, rowClearSweepDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                elapsed += dt;
                float progress = Mathf.Clamp01(elapsed / duration);
                float currentX = Mathf.Lerp(startX, endX, progress);
                swordRoot.anchoredPosition = new Vector2(currentX, rowY);

                for (int col = 0; col < columns; col++)
                {
                    if (columnCleared[col] || currentX < cellLocalCenters[col].x)
                        continue;

                    columnCleared[col] = true;
                    EatCellIntoMatchResult(board, row, col, matchResult);
                }

                yield return null;
            }

            for (int col = 0; col < columns; col++)
            {
                if (!columnCleared[col])
                {
                    columnCleared[col] = true;
                    EatCellIntoMatchResult(board, row, col, matchResult);
                }
            }

            activeRowSwords.Remove(swordRootObj);
            if (swordRootObj != null)
                Destroy(swordRootObj);
        }

        static void EatCellIntoMatchResult(BoardManager board, int row, int col,
            MatchResult matchResult)
        {
            if (!board.EatGemAtForSkill(row, col, out GemType eatenType))
                return;

            BoardPos pos = new BoardPos(row, col);
            matchResult.ClearPositions.Add(pos);
            matchResult.GemCounts.TryGetValue(eatenType, out int currentCount);
            matchResult.GemCounts[eatenType] = currentCount + 1;
        }

        void EnsureOrbitLayers()
        {
            if (heroView == null || heroView.VisualRect == null)
                return;

            RectTransform heroRoot = heroView.transform as RectTransform;
            if (heroRoot == null)
                return;

            if (backOrbitLayer == null)
                backOrbitLayer = CreateLayerRect("FlyingSwordBackOrbitLayer", heroRoot);
            if (frontOrbitLayer == null)
                frontOrbitLayer = CreateLayerRect("FlyingSwordFrontOrbitLayer", heroRoot);

            int visualIndex = heroView.VisualRect.GetSiblingIndex();
            backOrbitLayer.SetSiblingIndex(visualIndex);
            visualIndex = heroView.VisualRect.GetSiblingIndex();
            frontOrbitLayer.SetSiblingIndex(visualIndex + 1);
        }

        void EnsureRowClearLayer(BoardManager board)
        {
            if (rowClearLayer != null)
                return;

            Transform boardRoot = board != null ? board.BoardRoot : null;
            RectTransform viewport = boardRoot != null && boardRoot.parent != null
                ? boardRoot.parent.parent as RectTransform
                : null;
            RectTransform boardArea = viewport != null
                ? viewport.parent as RectTransform
                : boardRoot as RectTransform;
            if (boardArea == null)
                return;

            rowClearLayer = CreateLayerRect("FlyingSwordRowClearLayer", boardArea);
            rowClearLayer.SetAsLastSibling();
        }

        static RectTransform CreateLayerRect(string name, RectTransform parent)
        {
            GameObject layerObj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = layerObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            return rect;
        }

        void SpawnOrbitSwords(Sprite swordSprite, int count)
        {
            ClearOrbitSwords();
            EnsureOrbitLayers();
            if (frontOrbitLayer == null || backOrbitLayer == null || swordSprite == null)
                return;

            float heroHeight = MeasureHeroBattleHeight(heroView);
            float spriteAspect = swordSprite.rect.width > 0f
                ? swordSprite.rect.height / swordSprite.rect.width
                : 0.5f;
            float swordLength = heroHeight * Mathf.Max(0.15f, orbitSwordLengthRatio);
            Vector2 swordSize = new Vector2(swordLength, swordLength * spriteAspect);
            int clampedCount = Mathf.Max(1, count);
            int trailSteps = Mathf.Clamp(orbitTrailCount, 0, TrailAlphaWeights.Length);

            for (int index = 0; index < clampedCount; index++)
            {
                OrbitSwordInstance instance = new OrbitSwordInstance
                {
                    PhaseOffset = index / (float)clampedCount,
                    TrailRects = new RectTransform[trailSteps],
                    TrailImages = new Image[trailSteps]
                };

                for (int t = trailSteps - 1; t >= 0; t--)
                {
                    RectTransform trailRect = CreateSwordImage($"OrbitSword_{index}_Trail_{t}",
                        frontOrbitLayer, swordSprite, swordSize * TrailScaleWeights[t],
                        out Image trailImg);
                    trailRect.localEulerAngles = new Vector3(0f, 0f, -90f);
                    instance.TrailRects[t] = trailRect;
                    instance.TrailImages[t] = trailImg;
                }

                RectTransform mainRect = CreateSwordImage($"OrbitSword_{index}",
                    frontOrbitLayer, swordSprite, swordSize, out Image mainImg);
                mainRect.localEulerAngles = new Vector3(0f, 0f, -90f);
                instance.MainRect = mainRect;
                instance.MainImage = mainImg;

                activeOrbitSwords.Add(instance);
            }
        }

        static RectTransform CreateSwordImage(string name, RectTransform parent, Sprite sprite,
            Vector2 size, out Image image)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        void UpdateOrbitPositions()
        {
            if (heroView == null || frontOrbitLayer == null || backOrbitLayer == null)
                return;

            RectTransform heroRoot = heroView.transform as RectTransform;
            RectTransform orbitAnchor = heroView.TorsoAnchor != null ? heroView.TorsoAnchor : heroView.VFXAnchor;
            Vector2 center = orbitAnchor != null && heroRoot != null
                ? (Vector2)heroRoot.InverseTransformPoint(orbitAnchor.position)
                : Vector2.zero;

            float heroHeight = MeasureHeroBattleHeight(heroView);
            float radiusX = orbitRadiusXRatio * heroHeight;
            float radiusY = orbitRadiusYRatio * heroHeight;
            float period = Mathf.Max(0.2f, orbitSecondsPerRevolution);

            for (int i = 0; i < activeOrbitSwords.Count; i++)
            {
                OrbitSwordInstance instance = activeOrbitSwords[i];
                float basePhase = (orbitElapsed / period) + instance.PhaseOffset;

                for (int t = instance.TrailRects.Length - 1; t >= 0; t--)
                {
                    float trailPhase = basePhase - (t + 1) * orbitTrailPhaseStep;
                    ApplyOrbitSample(instance.TrailRects[t], instance.TrailImages[t],
                        center, radiusX, radiusY, trailPhase,
                        TrailAlphaWeights[t] * orbitEnvelopeAlpha, true);
                }

                ApplyOrbitSample(instance.MainRect, instance.MainImage,
                    center, radiusX, radiusY, basePhase, orbitEnvelopeAlpha, false);
            }
        }

        void ApplyOrbitSample(RectTransform rect, Image image, Vector2 center,
            float radiusX, float radiusY, float phase, float alphaScale, bool isTrail)
        {
            if (rect == null || image == null)
                return;

            float phi = phase * Mathf.PI * 2f;
            float theta = phi + orbitEasingStrength * Mathf.Cos(phi);

            float x = center.x + radiusX * Mathf.Cos(theta);
            float y = center.y + radiusY * Mathf.Sin(theta);
            float depth = Mathf.Sin(theta);

            RectTransform targetLayer = depth > 0f ? backOrbitLayer : frontOrbitLayer;
            if (rect.parent != targetLayer)
                rect.SetParent(targetLayer, false);

            rect.anchoredPosition = new Vector2(x, y);
            rect.localEulerAngles = new Vector3(0f, 0f, -90f);

            float backBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.22f, depth));
            float depthAlpha = Mathf.Lerp(1f, orbitBackAlpha, backBlend) * alphaScale;

            image.color = isTrail
                ? new Color(0.75f, 0.94f, 1f, depthAlpha)
                : new Color(1f, 1f, 1f, depthAlpha);
        }

        void ClearOrbitSwords()
        {
            for (int i = 0; i < activeOrbitSwords.Count; i++)
            {
                OrbitSwordInstance instance = activeOrbitSwords[i];
                if (instance.MainRect != null)
                    Destroy(instance.MainRect.gameObject);
                if (instance.TrailRects != null)
                {
                    for (int t = 0; t < instance.TrailRects.Length; t++)
                        if (instance.TrailRects[t] != null)
                            Destroy(instance.TrailRects[t].gameObject);
                }
            }
            activeOrbitSwords.Clear();
        }

        void ClearRowSwords()
        {
            for (int i = 0; i < activeRowSwords.Count; i++)
                if (activeRowSwords[i] != null)
                    Destroy(activeRowSwords[i]);
            activeRowSwords.Clear();
        }
    }
}
