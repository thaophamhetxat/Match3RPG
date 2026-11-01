using System;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Item Progression")]
    public sealed class ItemProgressionConfig : StableIdConfig
    {
        [SerializeField] string itemId;
        [SerializeField] int[] experienceThresholds = Array.Empty<int>();

        public string ItemId => itemId;
        public int[] ExperienceThresholds => experienceThresholds ?? Array.Empty<int>();
    }
}
