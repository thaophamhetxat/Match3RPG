using System.Collections.Generic;
using Match3RPG.Campaign.Data;

namespace Match3RPG.Battle
{
    public sealed class BattleLoadout
    {
        readonly IReadOnlyList<string> petIds;
        readonly IReadOnlyList<string> skillIds;

        public HeroData Hero { get; }
        public string SelectedHeroId => Hero != null ? Hero.StableId : string.Empty;
        public IReadOnlyList<string> PetIds => petIds;
        public string EquippedPetId => petIds.Count > 0 ? petIds[0] : string.Empty;
        public IReadOnlyList<string> EquippedPetIds => petIds;
        public string CompanionId { get; }
        public string EquippedCompanionId => CompanionId;
        public IReadOnlyList<string> SkillIds => skillIds;
        public IReadOnlyList<string> EquippedSkillIds => skillIds;

        public BattleLoadout(HeroData hero, IEnumerable<string> equippedPetIds,
            string equippedCompanionId, IEnumerable<string> equippedSkillIds)
        {
            Hero = hero;
            petIds = new List<string>(equippedPetIds ?? new string[0]).AsReadOnly();
            CompanionId = equippedCompanionId ?? string.Empty;
            skillIds = new List<string>(equippedSkillIds ?? new string[0]).AsReadOnly();
        }
    }
}
