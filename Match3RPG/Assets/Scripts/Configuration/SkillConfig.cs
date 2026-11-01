using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Skill")]
    public sealed class SkillConfig : StableIdConfig
    {
        [SerializeField] string displayName;
        [SerializeField] SkillType skillType;
        [SerializeField] SkillTargetType targetType;
        [SerializeField] SkillTriggerType triggerType;

        public string DisplayName => displayName;
        public SkillType Type => skillType;
        public SkillTargetType TargetType => targetType;
        public SkillTriggerType TriggerType => triggerType;
    }
}
