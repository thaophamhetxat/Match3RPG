using System;
using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Battle
{
    public readonly struct RuntimeBossStats
    {
        public string BossId { get; }
        public int Level { get; }
        public int MaxHP { get; }
        public int MaxSoulHP { get; }
        public int Attack { get; }
        public int MaxRage { get; }
        public int StartingRage { get; }
        public int RagePerGem { get; }
        public int RagePerReceivedHit { get; }
        public bool CriticalEnabled { get; }
        public float CriticalDamageMultiplier { get; }
        public CombatVFXType AttackVfx { get; }
        public int MaxMana => 100;

        public RuntimeBossStats(BossData config, int level = 1)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            BossId = config.StableId;
            Level = Math.Max(1, level);
            MaxHP = Math.Max(1, config.MaxHP);
            MaxSoulHP = SoulHealthCalculator.CalculateBossMaxHealth(MaxHP, Level);
            Attack = Math.Max(0, config.Attack);
            MaxRage = Math.Max(1, config.MaxRage);
            StartingRage = Mathf.Clamp(config.StartingRage, 0, MaxRage);
            RagePerGem = Math.Max(0, config.RagePerOrangeGem);
            RagePerReceivedHit = Math.Max(0, config.RagePerReceivedHit);
            CriticalEnabled = config.CriticalEnabled;
            CriticalDamageMultiplier = Math.Max(1f, config.CriticalDamageMultiplier);
            AttackVfx = config.PreferredAttackVfx;
        }
    }
}
