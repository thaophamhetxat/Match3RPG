using System;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Equipment Set")]
    public sealed class EquipmentSetConfig : StableIdConfig
    {
        [SerializeField] string[] itemIds = Array.Empty<string>();

        public string[] ItemIds => itemIds ?? Array.Empty<string>();
    }
}
