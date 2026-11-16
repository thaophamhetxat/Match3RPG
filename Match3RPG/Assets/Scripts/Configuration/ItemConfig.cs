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
        // Tăng ST (%), hiện chỉ hiển thị trong popup, chưa nối vào công thức sát thương trận đấu.
        [SerializeField, Min(0f)] float damagePercentBonus;
        [SerializeField, Min(0)] int ragePerGemBonus;
        // 0 = trang bị (tab Túi đồ, có thể trang bị); 1 = nguyên liệu
        // (tab Nguyên liệu, CÓ hiện trong Túi đồ trận đấu); 2 = nguyên liệu
        // (tab Nguyên liệu, KHÔNG hiện trong Túi đồ trận đấu); 3 = mảnh
        // (tab Mảnh).
        [SerializeField, Min(0)] int inventoryFlag;

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
        public float DamagePercentBonus => damagePercentBonus;
        public int RagePerGemBonus => ragePerGemBonus;
        public int InventoryFlag => inventoryFlag;

        public bool MeetsRequirement(int playerLevel) => playerLevel >= GetMinLevelForGrade(grade);

        public static int GetMinLevelForGrade(EquipmentGrade grade)
        {
            switch (grade)
            {
                case EquipmentGrade.PhapKhi:
                    return 0;
                case EquipmentGrade.BaoKhi:
                    return 1;
                case EquipmentGrade.LinhKhi:
                    return 10;
                case EquipmentGrade.DaoKhi:
                    return 20;
                case EquipmentGrade.TienKhi:
                    return 30;
                case EquipmentGrade.ThanKhi:
                    return 40;
                case EquipmentGrade.ThanhKhi:
                    return 90;
                case EquipmentGrade.DeBinh:
                    return 130;
                default:
                    return 0;
            }
        }

        public static string GetGradeDisplayName(EquipmentGrade grade)
        {
            switch (grade)
            {
                case EquipmentGrade.PhapKhi:
                    return "Pháp khí";
                case EquipmentGrade.BaoKhi:
                    return "Bảo khí";
                case EquipmentGrade.LinhKhi:
                    return "Linh khí";
                case EquipmentGrade.DaoKhi:
                    return "Đạo khí";
                case EquipmentGrade.TienKhi:
                    return "Tiên khí";
                case EquipmentGrade.ThanKhi:
                    return "Thần khí";
                case EquipmentGrade.ThanhKhi:
                    return "Thánh khí";
                case EquipmentGrade.DeBinh:
                    return "Đế binh";
                default:
                    return "Pháp khí";
            }
        }

        // Trả về màu viền ô item theo phẩm cấp (Đế binh dùng fallback trắng vì viền thật là gradient cầu vồng).
        public static Color32 GetGradeSlotBorderColor(EquipmentGrade grade)
        {
            switch (grade)
            {
                case EquipmentGrade.PhapKhi:
                    return new Color32(168, 168, 168, 255);
                case EquipmentGrade.BaoKhi:
                    return new Color32(74, 222, 128, 255);
                case EquipmentGrade.LinhKhi:
                    return new Color32(96, 165, 250, 255);
                case EquipmentGrade.DaoKhi:
                    return new Color32(192, 132, 252, 255);
                case EquipmentGrade.TienKhi:
                    return new Color32(251, 146, 60, 255);
                case EquipmentGrade.ThanKhi:
                    return new Color32(248, 113, 113, 255);
                case EquipmentGrade.ThanhKhi:
                    return new Color32(250, 204, 21, 255);
                case EquipmentGrade.DeBinh:
                    return new Color32(255, 255, 255, 255);
                default:
                    return new Color32(231, 190, 110, 255);
            }
        }

        // Trả về màu nền ô item theo phẩm cấp.
        public static Color32 GetGradeSlotBackgroundColor(EquipmentGrade grade)
        {
            switch (grade)
            {
                case EquipmentGrade.PhapKhi:
                    return new Color32(40, 42, 48, 255);
                case EquipmentGrade.BaoKhi:
                    return new Color32(20, 46, 32, 255);
                case EquipmentGrade.LinhKhi:
                    return new Color32(22, 36, 68, 255);
                case EquipmentGrade.DaoKhi:
                    return new Color32(44, 28, 68, 255);
                case EquipmentGrade.TienKhi:
                    return new Color32(68, 36, 18, 255);
                case EquipmentGrade.ThanKhi:
                    return new Color32(68, 24, 24, 255);
                case EquipmentGrade.ThanhKhi:
                    return new Color32(68, 52, 16, 255);
                case EquipmentGrade.DeBinh:
                    return new Color32(30, 28, 44, 255);
                default:
                    return new Color32(12, 27, 36, 255);
            }
        }

        // Chỉ phẩm cấp Đế binh dùng viền cầu vồng gradient.
        public static bool IsRainbowGrade(EquipmentGrade grade) => grade == EquipmentGrade.DeBinh;
    }
}
