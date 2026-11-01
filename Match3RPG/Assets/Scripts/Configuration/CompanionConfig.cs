using System;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Companion")]
    public sealed class CompanionConfig : StableIdConfig
    {
        [SerializeField] string displayName;
        [SerializeField] string[] autoSkillIds = Array.Empty<string>();

        public string DisplayName => displayName;
        public string[] AutoSkillIds => autoSkillIds ?? Array.Empty<string>();
    }
}
