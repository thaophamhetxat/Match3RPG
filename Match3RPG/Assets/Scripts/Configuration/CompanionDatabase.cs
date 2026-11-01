using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Companion Database")]
    public sealed class CompanionDatabase : StableIdDatabase<CompanionConfig>
    {
        [SerializeField] CompanionConfig[] companions = Array.Empty<CompanionConfig>();

        public IReadOnlyList<CompanionConfig> Companions =>
            companions ?? Array.Empty<CompanionConfig>();

        protected override IEnumerable<CompanionConfig> EnumerateDefinitions() => Companions;
        protected override string GetStableId(CompanionConfig definition) => definition.StableId;
    }
}
