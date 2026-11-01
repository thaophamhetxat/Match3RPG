using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Skill Database")]
    public sealed class SkillDatabase : StableIdDatabase<SkillConfig>
    {
        [SerializeField] SkillConfig[] skills = Array.Empty<SkillConfig>();

        public IReadOnlyList<SkillConfig> Skills => skills ?? Array.Empty<SkillConfig>();

        protected override IEnumerable<SkillConfig> EnumerateDefinitions() => Skills;
        protected override string GetStableId(SkillConfig definition) => definition.StableId;
    }
}
