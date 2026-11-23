using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Match3RPG.Configuration
{
    [Serializable]
    public struct SkillLevelConfig
    {
        [Range(1, 5)] public int level;
        [Min(1)] public int orbitSwordCount;
        [Min(1)] public int rowClearCount;
        [Min(1f)] public float attackGemDamageMultiplier;

        public SkillLevelConfig(int level, int orbitSwordCount, int rowClearCount,
            float attackGemDamageMultiplier)
        {
            this.level = level;
            this.orbitSwordCount = orbitSwordCount;
            this.rowClearCount = rowClearCount;
            this.attackGemDamageMultiplier = attackGemDamageMultiplier;
        }
    }

    [CreateAssetMenu(menuName = "Match3 RPG/Definitions/Skill")]
    public sealed class SkillConfig : StableIdConfig
    {
        public const string FlyingSwordSkillId = "flying_sword";
        public const string FlyingSwordDisplayName = "Kiếm Bay Quanh";
        public const string FlyingSwordAssetPath = "Assets/Art/VFX/Skills/FlyingSword/Sword.png";
        public const string SwordQiSkillId = "sword_qi";
        public const string SwordQiDisplayName = "Kiếm Khí";
        public const string SwordQiAssetPath = "Assets/Art/VFX/Skills/SwordQi/SwordQi_C.png";
        public const string SwordQiDescription =
            "Đứng im tại chỗ, chém ra 1 luồng kiếm khí bay tới boss.";
        public const int SwordQiDefaultManaCost = 3; // TẠM
        public const int SwordQiDefaultRageCost = 0; // TẠM
        public const int SwordQiDefaultCooldownTurns = 3; // TẠM
        public const int DefaultManaCost = 3;
        public const int DefaultRageCost = 0;
        public const int DefaultCooldownTurns = 3;
        public const int DefaultLevel = 1;
        public const int MaxLevel = 5;

        [SerializeField] string displayName = FlyingSwordDisplayName;
        [SerializeField] Sprite icon;
        [SerializeField, TextArea] string description =
            "Tạo 1 lớp kiếm khí quanh nhân vật, sau đó triệt tiêu 1 hàng kim cương bất kỳ.";
        [SerializeField] SkillType skillType = SkillType.Active;
        [SerializeField] SkillTargetType targetType = SkillTargetType.SelfAndBoard;
        [SerializeField] SkillTriggerType triggerType = SkillTriggerType.Manual;
        [SerializeField, Min(0)] int manaCost = DefaultManaCost;
        [SerializeField, Min(0)] int rageCost = DefaultRageCost;
        [SerializeField, Min(0)] int cooldownTurns = DefaultCooldownTurns;
        [SerializeField, Range(1, MaxLevel)] int level = DefaultLevel;
        [SerializeField] SkillLevelConfig[] levelTable = CreateDefaultFlyingSwordLevels();

        static Sprite cachedSwordSprite;
        static Sprite cachedSwordQiSprite;

        public string DisplayName => string.IsNullOrEmpty(displayName)
            ? (string.Equals(StableId, SwordQiSkillId, StringComparison.Ordinal)
                ? SwordQiDisplayName
                : FlyingSwordDisplayName)
            : displayName;
        public Sprite Icon => icon != null
            ? icon
            : (string.Equals(StableId, SwordQiSkillId, StringComparison.Ordinal)
                ? ResolveSwordQiSprite()
                : ResolveSwordSprite());
        public string Description => GetDescriptionForLevel(Level);
        public SkillType Type => skillType;
        public SkillTargetType TargetType => targetType;
        public SkillTriggerType TriggerType => triggerType;
        public int ManaCost => Mathf.Max(0, manaCost);
        public int RageCost => Mathf.Max(0, rageCost);
        public int CooldownTurns => Mathf.Max(0, cooldownTurns);
        public int Level
        {
            get => Mathf.Clamp(level, 1, MaxLevel);
            set => level = Mathf.Clamp(value, 1, MaxLevel);
        }
        public SkillLevelConfig[] LevelTable => levelTable;

        public int GetOrbitSwordCount(int skillLevel)
        {
            if (TryGetLevelEntry(skillLevel, out SkillLevelConfig entry)
                && entry.orbitSwordCount > 0)
                return entry.orbitSwordCount;

            int clamped = Mathf.Clamp(skillLevel, 1, MaxLevel);
            if (clamped <= 2) return 1;
            if (clamped <= 4) return 2;
            return 3;
        }

        public int GetRowClearCount(int skillLevel)
        {
            if (TryGetLevelEntry(skillLevel, out SkillLevelConfig entry)
                && entry.rowClearCount > 0)
                return entry.rowClearCount;

            int clamped = Mathf.Clamp(skillLevel, 1, MaxLevel);
            if (clamped <= 2) return 1;
            if (clamped <= 4) return 2;
            return 3;
        }

        public float GetAttackGemDamageMultiplier(int skillLevel)
        {
            if (TryGetLevelEntry(skillLevel, out SkillLevelConfig entry)
                && entry.attackGemDamageMultiplier > 0f)
                return entry.attackGemDamageMultiplier;

            int clamped = Mathf.Clamp(skillLevel, 1, MaxLevel);
            return (clamped == 2 || clamped == 4) ? 1.25f : 1f;
        }

        public string GetDescriptionForLevel(int skillLevel)
        {
            if (string.Equals(StableId, SwordQiSkillId, StringComparison.Ordinal))
                return string.IsNullOrEmpty(description) ? SwordQiDescription : description;
            if (!string.IsNullOrEmpty(StableId)
                && !string.Equals(StableId, FlyingSwordSkillId, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(description))
                return description;

            int rows = GetRowClearCount(skillLevel);
            return $"Tạo 1 lớp kiếm khí quanh nhân vật, sau đó triệt tiêu {rows} hàng kim cương bất kỳ.";
        }

        bool TryGetLevelEntry(int skillLevel, out SkillLevelConfig matched)
        {
            int clamped = Mathf.Clamp(skillLevel, 1, MaxLevel);
            if (levelTable != null)
            {
                for (int i = 0; i < levelTable.Length; i++)
                {
                    if (levelTable[i].level == clamped)
                    {
                        matched = levelTable[i];
                        return true;
                    }
                }
            }
            matched = default;
            return false;
        }

        public static SkillLevelConfig[] CreateDefaultFlyingSwordLevels()
        {
            return new[]
            {
                new SkillLevelConfig(1, 1, 1, 1f),
                new SkillLevelConfig(2, 1, 1, 1.25f),
                new SkillLevelConfig(3, 2, 2, 1f),
                new SkillLevelConfig(4, 2, 2, 1.25f),
                new SkillLevelConfig(5, 3, 3, 1f)
            };
        }

        public static Sprite ResolveSwordSprite()
        {
            return ResolveSpriteAsset(FlyingSwordAssetPath, ref cachedSwordSprite);
        }

        public static Sprite ResolveSwordQiSprite()
        {
            return ResolveSpriteAsset(SwordQiAssetPath, ref cachedSwordQiSprite);
        }

        static Sprite ResolveSpriteAsset(string assetPath, ref Sprite cache)
        {
            if (cache != null)
                return cache;
#if UNITY_EDITOR
            cache = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (cache == null)
            {
                UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                if (all != null)
                {
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i] is Sprite sprite)
                        {
                            cache = sprite;
                            break;
                        }
                    }
                }
            }
#endif
            return cache;
        }

        public static SkillConfig CreateRuntimeFlyingSwordFallback()
        {
            SkillConfig runtime = CreateInstance<SkillConfig>();
            runtime.name = FlyingSwordSkillId;
            runtime.SetStableId(FlyingSwordSkillId);
            runtime.displayName = FlyingSwordDisplayName;
            runtime.icon = ResolveSwordSprite();
            runtime.description =
                "Tạo 1 lớp kiếm khí quanh nhân vật, sau đó triệt tiêu 1 hàng kim cương bất kỳ.";
            runtime.skillType = SkillType.Active;
            runtime.targetType = SkillTargetType.SelfAndBoard;
            runtime.triggerType = SkillTriggerType.Manual;
            runtime.manaCost = DefaultManaCost;
            runtime.rageCost = DefaultRageCost;
            runtime.cooldownTurns = DefaultCooldownTurns;
            runtime.level = DefaultLevel;
            runtime.levelTable = CreateDefaultFlyingSwordLevels();
            return runtime;
        }

        public static SkillConfig CreateRuntimeSwordQiFallback()
        {
            SkillConfig runtime = CreateInstance<SkillConfig>();
            runtime.name = SwordQiSkillId;
            runtime.SetStableId(SwordQiSkillId);
            runtime.displayName = SwordQiDisplayName;
            runtime.icon = ResolveSwordQiSprite();
            runtime.description = SwordQiDescription;
            runtime.skillType = SkillType.Active;
            runtime.targetType = SkillTargetType.SingleEnemy;
            runtime.triggerType = SkillTriggerType.Manual;
            runtime.manaCost = SwordQiDefaultManaCost;
            runtime.rageCost = SwordQiDefaultRageCost;
            runtime.cooldownTurns = SwordQiDefaultCooldownTurns;
            runtime.level = DefaultLevel;
            runtime.levelTable = Array.Empty<SkillLevelConfig>();
            return runtime;
        }
    }
}
