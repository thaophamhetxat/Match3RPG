using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Pet Database")]
    public sealed class PetDatabase : StableIdDatabase<PetConfig>
    {
        [SerializeField] PetConfig[] pets = Array.Empty<PetConfig>();

        public IReadOnlyList<PetConfig> Pets => pets ?? Array.Empty<PetConfig>();

        protected override IEnumerable<PetConfig> EnumerateDefinitions() => Pets;
        protected override string GetStableId(PetConfig definition) => definition.StableId;
    }
}
