using System;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Hero Progression")]
    public sealed class HeroProgressionConfig : StableIdConfig
    {
        [SerializeField] string heroId;
        [SerializeField] int[] experienceThresholds = Array.Empty<int>();

        public string HeroId => heroId;
        public int[] ExperienceThresholds => experienceThresholds ?? Array.Empty<int>();
    }
}
