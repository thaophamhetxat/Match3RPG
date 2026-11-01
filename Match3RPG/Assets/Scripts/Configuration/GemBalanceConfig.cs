using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Configuration/Gem Balance")]
    public sealed class GemBalanceConfig : StableIdConfig
    {
        [SerializeField, Min(0)] int attackDamagePerGem = 10;
        [SerializeField, Min(0)] int manaPerGem = 10;
        [SerializeField, Min(0)] int experiencePerGem = 1;
        [SerializeField, Min(0)] int healthPerGem = 50;
        [SerializeField, Min(0)] int ragePerGem = 5;
        [SerializeField, Min(0)] int shieldPerGem = 50;
        [SerializeField, Min(0)] int drainHpPerGem = 50;
        [SerializeField, Min(0)] int drainRagePerGem = 5;
        [SerializeField, Min(0f)] float drainManaPercentPerGem = 1f;

        public int AttackDamagePerGem => attackDamagePerGem;
        public int ManaPerGem => manaPerGem;
        public int ExperiencePerGem => experiencePerGem;
        public int HealthPerGem => healthPerGem;
        public int RagePerGem => ragePerGem;
        public int ShieldPerGem => shieldPerGem;
        public int DrainHpPerGem => drainHpPerGem;
        public int DrainRagePerGem => drainRagePerGem;
        public float DrainManaPercentPerGem => drainManaPercentPerGem;
    }
}
