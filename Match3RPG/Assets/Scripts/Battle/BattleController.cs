using System.Collections;
using Match3RPG.Board;
using Match3RPG.Campaign.Data;
using Match3RPG.Core;
using Match3RPG.Persistence;
using Match3RPG.UI;
using UnityEngine;

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
        [SerializeField, Range(0f, 100f)] private float heroDodgePercent;
        [SerializeField, Min(0f)] private float heroAccuracyPercent = 100f;
        [SerializeField, Min(0)] private int heroSpeed = 10;
        [SerializeField, Min(0)] private int experienceStars;
        [SerializeField, Min(0)] private int healthStars;
        [SerializeField, Min(0)] private int shieldStars;
        [SerializeField, Min(0)] private int bossAttackDamagePerGem;
        [SerializeField, Min(1)] private int bossMaxRage = 100;
        [SerializeField, Min(0)] private int bossStartingRage;
        [SerializeField, Min(0)] private int bossRagePerGem = 5;
        [SerializeField, Min(1)] private int bossMaxMana = 100;
        [SerializeField, Min(0)] private int bossRagePerReceivedHit = 1;
        [SerializeField] private bool bossCriticalEnabled = true;
        [SerializeField, Min(1f)] private float bossCriticalDamageMultiplier = 2f;
        [SerializeField] private CombatVFXType bossAttackVfx = CombatVFXType.Slash;
        [SerializeField, Min(0f)] private float multiHitInterval = .15f;
        [SerializeField] private bool showReceivedHitRageText;
        [Header("Temporary layout test mode")]
        [SerializeField] private bool layoutTestMode = false;

        int heroHp, bossHp, heroSoulHp, bossSoulHp, mana, ultimate, bossMana, bossUltimate;
        int heroMaxSoulHp, bossMaxSoulHp;
        int heroShield, heroShieldTurns;
        int heroRage, bossRage;
        int loggedHeroRage = int.MinValue;
        int loggedHeroMaxRage = int.MinValue;
        bool statsInitialized;
        bool soulEnabled;
        bool soulDamageEnabled;
        int heroBattleEntryLevel = 1;
        bool heroProgressionConfigured;
        PlayerProgressionService progressionService;
        StarChartGemInventoryService starChartGemInventory;

        public int InvalidMovePenaltyCount { get; private set; }
        public bool IsGameOver => heroHp <= 0 || bossHp <= 0
            || (soulEnabled && (heroSoulHp <= 0 || bossSoulHp <= 0));
        public bool PlayerWon => bossHp <= 0 || (soulEnabled && bossSoulHp <= 0);
        public bool SoulEnabled => soulEnabled;
        public int HeroBattleEntryLevel => heroBattleEntryLevel;
        public float BossHealthRatio => bossMaxHp <= 0 ? 0f : (float)bossHp / bossMaxHp;
        public float BossRageRatio => (float)bossRage / BossMaxRage;
        public int HeroHealth => heroHp;
        public int HeroSoulHealth => heroSoulHp;
        public int BossSoulHealth => bossSoulHp;
        public int HeroShield => heroShield;
        public int BossHealth => bossHp;
        public int HeroMaxHealth => heroMaxHp;
        public int BossMaxHealth => bossMaxHp;
        public int HeroAttackDamagePerGem => AddNonNegative(heroAttack, BoardAttackBonusPerGem);
        public int HeroSoulAttackDamagePerGem => AddNonNegative(heroSoulAttack, BoardAttackBonusPerGem);
        public int HeroExperiencePerGem => AddNonNegative(1, experienceStars);
        public int HeroShieldPerGem => AddNonNegative(shieldPerGem,
            MultiplyNonNegative(shieldStars, 10));
        public float HeroCriticalChancePercent => heroCriticalChancePercent;
        public float HeroDodgePercent => heroDodgePercent;
        public float HeroAccuracyPercent => heroAccuracyPercent;
        public int HeroSpeed => heroSpeed;
        public int BossAttackDamagePerGem => bossAttackDamagePerGem;
        public int HeroMana => mana;
        public int HeroUltimate => ultimate;
        public int BossMana => bossMana;
        public int BossUltimate => bossUltimate;
        public int HeroRage => heroRage;
        public int BossRage => bossRage;
        public int MaxRage => Mathf.Max(1, maxRage);
        public int BossMaxRage => Mathf.Max(1, bossMaxRage);

        void Awake() => InitializeStats();

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
                bossMaxRage = stats.MaxRage;
                bossStartingRage = stats.StartingRage;
                bossMaxMana = stats.MaxMana;
                bossRagePerReceivedHit = stats.RagePerReceivedHit;
                bossCriticalEnabled = stats.CriticalEnabled;
                bossCriticalDamageMultiplier = stats.CriticalDamageMultiplier;
                bossAttackVfx = stats.AttackVfx;
            }
            else if (bossData != null)
            {
                bossMaxHp = Mathf.Max(1, bossData.MaxHP);
                bossMaxSoulHp = SoulHealthCalculator.CalculateBossMaxHealth(bossMaxHp, 1);
                bossAttackDamagePerGem = Mathf.Max(0, bossData.Attack);
                bossMaxRage = Mathf.Max(1, bossData.MaxRage);
                bossStartingRage = Mathf.Clamp(bossData.StartingRage, 0, bossMaxRage);
                bossRagePerReceivedHit = Mathf.Max(0, bossData.RagePerReceivedHit);
                bossCriticalEnabled = bossData.CriticalEnabled;
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
            heroRage = 0;
            bossRage = Mathf.Clamp(bossStartingRage, 0, BossMaxRage);
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

        public IEnumerator ResolveMove(BattleActor actor, MoveResult move)
        {
            if (move == null || !move.IsValid || IsGameOver) yield break;
            if (layoutTestMode)
            {
                ApplyLayoutTestState();
                Refresh();
            }

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
                int previousHealth = heroHp;
                heroHp = Mathf.Min(heroMaxHp, heroHp
                    + healthGems * AddNonNegative(BaseHealthPerGem, healthStars));
                if (heroHp > previousHealth && combatVfx != null
                    && combatVfx.PlayHeal(BattleActor.Player))
                    precedingVfxDuration = CombatVFXManager.HealingAuraDuration;
                ShowResource(hero, FloatingTextType.Heal, heroHp - previousHealth);
                int previousMana = mana;
                mana = Mathf.Min(maxMana, mana + manaGems * manaPerGem);
                ShowResource(hero, FloatingTextType.Mana, mana - previousMana);
                AddExperience(experienceGems);
                AddGemRage(BattleActor.Player, rageGems);
                AddShield(shieldGems);
                precedingVfxDuration = Mathf.Max(precedingVfxDuration,
                    ApplyDrain(BattleActor.Player, experienceGems));

                if (attackGems > 0 || soulGems > 0)
                {
                    if (precedingVfxDuration > 0f)
                        yield return new WaitForSeconds(precedingVfxDuration);
                    yield return ResolveAttackAction(BattleActor.Player, attackGems, 0, soulGems);
                }

                if (ultimate >= 100 && bossHp > 0 && !IsGameOver)
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
                if (bossHp > previousBossHealth && combatVfx != null
                    && combatVfx.PlayHeal(BattleActor.Boss))
                    precedingVfxDuration = CombatVFXManager.HealingAuraDuration;
                ShowResource(boss, FloatingTextType.Heal, bossHp - previousBossHealth);
                int previousBossMana = bossMana;
                bossMana = Mathf.Min(bossMaxMana, bossMana + manaGems * manaPerGem);
                ShowResource(boss, FloatingTextType.Mana, bossMana - previousBossMana);
                AddGemRage(BattleActor.Boss, rageGems);
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

            if ((bossHp == 0 || (soulEnabled && bossSoulHp == 0)) && boss != null)
                yield return boss.Death();
            if ((heroHp == 0 || (soulEnabled && heroSoulHp == 0)) && hero != null)
                yield return hero.Death();
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
            if ((heroHp == 0 || (soulEnabled && heroSoulHp == 0)) && hero != null)
                yield return hero.Death();
            Refresh();
        }

        IEnumerator ResolveAttackAction(BattleActor actor, int hitCount, int bonusDamage,
            int soulHitCount = 0)
        {
            int totalHitCount = hitCount + soulHitCount;
            CharacterView attacker = actor == BattleActor.Player ? hero : boss;
            BattleActor targetActor = actor == BattleActor.Player ? BattleActor.Boss : BattleActor.Player;
            CharacterView target = targetActor == BattleActor.Player ? hero : boss;
            bool rageCritical = totalHitCount > 0 && IsCriticalEnabled(actor)
                && GetRage(actor) >= GetMaxRage(actor);
            bool critical = rageCritical || (totalHitCount > 0
                && actor == BattleActor.Player
                && UnityEngine.Random.value * 100f < heroCriticalChancePercent);
            CombatVFXType attackVfx = actor == BattleActor.Player
                ? CombatVFXType.Slash : bossAttackVfx;
            bool rangedAttack = attackVfx == CombatVFXType.Fireball
                || attackVfx == CombatVFXType.AirSlash;
            int attackDamage = actor == BattleActor.Player
                ? HeroAttackDamagePerGem : bossAttackDamagePerGem;
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
            try
            {
                if (attacker != null && !rangedAttack)
                {
                    if (attackVfx == CombatVFXType.Dash)
                        combatVfx?.PlayDash(actor);
                    float hitReactionDuration = target != null ? CharacterView.HitAnimationDuration : 0f;
                    yield return attacker.BeginAttackSequence(totalHitCount, multiHitInterval, hitReactionDuration);
                }

                bool firstSuccessfulHit = true;
                for (int hit = 0; hit < totalHitCount; hit++)
                {
                    if (IsGameOver) break;
                    if (actor == BattleActor.Player && attacker != null
                        && attacker.HasSpriteAttackAnimation)
                        yield return attacker.WaitForAttackImpact(hit, totalHitCount);
                    if (IsGameOver) break;
                    int shieldBefore = targetActor == BattleActor.Player ? heroShield : 0;
                    bool soulHit = hit >= hitCount;
                    int damage = soulHit
                        ? ApplySoulDamage(targetActor, soulDamagePerHit)
                        : ApplyDamage(targetActor, damagePerHit);
                    int shieldAbsorbed = targetActor == BattleActor.Player ? shieldBefore - heroShield : 0;
                    if (damage <= 0 && shieldAbsorbed <= 0) break;

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
                    if (hit + 1 < totalHitCount && !IsGameOver && multiHitInterval > 0f)
                        yield return new WaitForSecondsRealtime(multiHitInterval);
                }

                if (!IsGameOver && bonusDamage > 0)
                {
                    int shieldBefore = targetActor == BattleActor.Player ? heroShield : 0;
                    int damage = ApplyDamage(targetActor, bonusDamage);
                    int shieldAbsorbed = targetActor == BattleActor.Player ? shieldBefore - heroShield : 0;
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
            }
            finally
            {
                if (!recovered && attacker != null)
                    attacker.CancelAttackSequence();
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

        int ApplyDamage(BattleActor target, int amount, bool shieldCanAbsorb = true)
        {
            if (layoutTestMode)
                return 0;
            if (amount <= 0 || IsGameOver) return 0;
            amount = AbsorbShield(target, amount, shieldCanAbsorb);
            if (amount <= 0) return 0;
            int previous = target == BattleActor.Player ? heroHp : bossHp;
            if (target == BattleActor.Player)
                heroHp = Mathf.Max(0, heroHp - amount);
            else
                bossHp = Mathf.Max(0, bossHp - amount);
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
            amount = AbsorbShield(target, amount, true);
            if (amount <= 0) return 0;
            int previous = target == BattleActor.Player ? heroSoulHp : bossSoulHp;
            if (target == BattleActor.Player)
                heroSoulHp = Mathf.Max(0, heroSoulHp - amount);
            else
                bossSoulHp = Mathf.Max(0, bossSoulHp - amount);
            int current = target == BattleActor.Player ? heroSoulHp : bossSoulHp;
            return previous - current;
        }

        int AbsorbShield(BattleActor target, int amount, bool shieldCanAbsorb)
        {
            if (target != BattleActor.Player || !shieldCanAbsorb || heroShield <= 0)
                return amount;
            int absorbed = Mathf.Min(heroShield, amount);
            heroShield -= absorbed;
            if (heroShield <= 0)
            {
                heroShield = 0;
                combatVfx?.SetPersistentEffect(BattleActor.Player, CombatVFXType.Shield, false);
            }
            ShowResource(hero, FloatingTextType.ShieldLost, absorbed);
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

        void AddShield(int gemCount)
        {
            if (gemCount <= 0) return;
            int effectiveShieldPerGem = HeroShieldPerGem;
            long totalShield = (long)heroShield + (long)gemCount * effectiveShieldPerGem;
            heroShield = totalShield >= int.MaxValue ? int.MaxValue : (int)totalShield;
            heroShieldTurns = 2;
            if (heroShield > 0)
                combatVfx?.SetPersistentEffect(BattleActor.Player, CombatVFXType.Shield, true);
            long gained = (long)gemCount * effectiveShieldPerGem;
            ShowResource(hero, FloatingTextType.Shield,
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
                return heroHp - previous;
            }

            int previousBoss = bossHp;
            bossHp = Mathf.Min(bossMaxHp, bossHp + amount);
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
            heroDodgePercent = 0f;
            heroAccuracyPercent = 100f;
            heroSpeed = 10;
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
}
