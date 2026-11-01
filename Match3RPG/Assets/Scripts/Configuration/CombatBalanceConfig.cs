using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Configuration/Combat Balance")]
    public sealed class CombatBalanceConfig : StableIdConfig
    {
        [SerializeField, Min(0)] int invalidMovePenaltyDamage = 30;
        [SerializeField, Min(0)] int ragePerReceivedHit = 1;
        [SerializeField, Min(0)] int maximumStoredTurns = 5;
        [SerializeField, Min(1)] int ultimateThreshold = 100;
        [SerializeField, Min(0)] int skillDamage = 80;
        [SerializeField, Min(1f)] float criticalDamageMultiplier = 2f;
        [SerializeField, Min(0f)] float multiHitInterval = .15f;

        public int InvalidMovePenaltyDamage => invalidMovePenaltyDamage;
        public int RagePerReceivedHit => ragePerReceivedHit;
        public int MaximumStoredTurns => maximumStoredTurns;
        public int UltimateThreshold => ultimateThreshold;
        public int SkillDamage => skillDamage;
        public float CriticalDamageMultiplier => criticalDamageMultiplier;
        public float MultiHitInterval => multiHitInterval;
    }
}
