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
        [SerializeField] EquipmentGrade grade;
        [SerializeField, TextArea] string description;
        [SerializeField, Min(0f)] float attackPercentBonus;
        [SerializeField, Min(0f)] float maxHealthPercentBonus;
        [SerializeField, Min(0f)] float defensePercentBonus;
        [SerializeField, Min(0f)] float critRatePercentBonus;
        [SerializeField, Min(0)] int speedBonus;
        [SerializeField, Min(0f)] float critDamagePercentBonus;
        [SerializeField, Min(0)] int ragePerGemBonus;

        public string DisplayName => displayName;
        public Sprite RewardIcon => rewardIcon;
        public ItemType Type => itemType;
        public ItemRarity Rarity => rarity;
        public EquipmentSlot EquipmentSlot => equipmentSlot;
        public int AttackBonus => attackBonus;
        public int MaxHealthBonus => maxHealthBonus;
        public EquipmentGrade Grade => grade;
        public string Description => description;
        public float AttackPercentBonus => attackPercentBonus;
        public float MaxHealthPercentBonus => maxHealthPercentBonus;
        public float DefensePercentBonus => defensePercentBonus;
        public float CritRatePercentBonus => critRatePercentBonus;
        public int SpeedBonus => speedBonus;
        public float CritDamagePercentBonus => critDamagePercentBonus;
        public int RagePerGemBonus => ragePerGemBonus;

        public bool MeetsRequirement(int playerLevel) => playerLevel >= GetMinLevelForGrade(grade);

        public static int GetMinLevelForGrade(EquipmentGrade grade)
        {
            switch (grade)
            {
                case EquipmentGrade.PhamKhi:
                    return 0;
                case EquipmentGrade.PhapKhi:
                    return 1;
                case EquipmentGrade.LinhKhi:
                    return 10;
                case EquipmentGrade.PhapBao:
                    return 20;
                case EquipmentGrade.LinhBao:
                    return 30;
                case EquipmentGrade.ThongThienLinhBao:
                    return 40;
                case EquipmentGrade.TienKhi:
                    return 90;
                case EquipmentGrade.ThanKhi:
                    return 130;
                default:
                    return 0;
            }
        }

        public static string GetGradeDisplayName(EquipmentGrade grade)
        {
            switch (grade)
            {
                case EquipmentGrade.PhamKhi:
                    return "Phàm khí";
                case EquipmentGrade.PhapKhi:
                    return "Pháp khí";
                case EquipmentGrade.LinhKhi:
                    return "Linh khí";
                case EquipmentGrade.PhapBao:
                    return "Pháp bảo";
                case EquipmentGrade.LinhBao:
                    return "Linh bảo";
                case EquipmentGrade.ThongThienLinhBao:
                    return "Thông Thiên Linh Bảo";
                case EquipmentGrade.TienKhi:
                    return "Tiên khí";
                case EquipmentGrade.ThanKhi:
                    return "Thần khí / Đạo khí";
                default:
                    return "Phàm khí";
            }
        }
    }
}
