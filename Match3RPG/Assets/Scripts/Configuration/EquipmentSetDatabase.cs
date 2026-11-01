using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Databases/Equipment Set Database")]
    public sealed class EquipmentSetDatabase : StableIdDatabase<EquipmentSetConfig>
    {
        [SerializeField] EquipmentSetConfig[] equipmentSets = Array.Empty<EquipmentSetConfig>();

        public IReadOnlyList<EquipmentSetConfig> EquipmentSets =>
            equipmentSets ?? Array.Empty<EquipmentSetConfig>();

        protected override IEnumerable<EquipmentSetConfig> EnumerateDefinitions() => EquipmentSets;
        protected override string GetStableId(EquipmentSetConfig definition) => definition.StableId;
    }
}
