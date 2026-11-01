using UnityEngine;

namespace Match3RPG.Configuration
{
    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Item")]
    public sealed class ItemConfig : StableIdConfig
    {
        [SerializeField] string displayName;
        [SerializeField] Sprite rewardIcon;
        [SerializeField] ItemType itemType;
        [SerializeField] ItemRarity rarity;
        [SerializeField] EquipmentSlot equipmentSlot;
        [SerializeField, Min(0)] int attackBonus;
        [SerializeField, Min(0)] int maxHealthBonus;

        public string DisplayName => displayName;
        public Sprite RewardIcon => rewardIcon;
        public ItemType Type => itemType;
        public ItemRarity Rarity => rarity;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public int AttackBonus => attackBonus;
        public int MaxHealthBonus => maxHealthBonus;
    }
}
