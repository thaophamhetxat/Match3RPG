using System;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Loadout")]
    public sealed class LoadoutConfig : StableIdConfig
    {
        [SerializeField] string heroId;
        [SerializeField] string[] itemIds = Array.Empty<string>();
        [SerializeField] string[] skillIds = Array.Empty<string>();
        [SerializeField, Min(0)] int maxPetSlots = 1;
        [SerializeField, Min(0)] int maxCompanionSlots = 1;
        [SerializeField, Min(0)] int maxSkillSlots = 4;

        public string HeroId => heroId;
        public string[] ItemIds => itemIds ?? Array.Empty<string>();
        public string[] SkillIds => skillIds ?? Array.Empty<string>();
        public int MaxPetSlots => Math.Max(0, maxPetSlots);
        public int MaxCompanionSlots => Math.Max(0, maxCompanionSlots);
        public int MaxSkillSlots => Math.Max(0, maxSkillSlots);
    }
}
