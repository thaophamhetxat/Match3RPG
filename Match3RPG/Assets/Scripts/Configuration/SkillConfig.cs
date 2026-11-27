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
        public const string RockSpikeSkillId = "rock_spike";
        public const string RockSpikeDisplayName = "Thạch Thứ"; // TẠM: Sư Huynh chốt tên thật sau Play-test
        public const string RockSpikeAssetPath =
            "Assets/Art/VFX/Skills/RockSpike/anime-rock-spike-eruption-sheet.png";
        public const string RockSpikeDescription =
            "Kết ấn gọi 5 gai đá mọc lên từ mặt đất dưới chân boss."; // TẠM
        public const int RockSpikeDefaultManaCost = 3; // TẠM
        public const int RockSpikeDefaultRageCost = 0; // TẠM
        public const int RockSpikeDefaultCooldownTurns = 3; // TẠM
        public const int RockSpikeGridColumns = 7;
        public const int RockSpikeGridRows = 6;
        public const int RockSpikeFrameCount = RockSpikeGridColumns * RockSpikeGridRows;
        public const int RockSpikePeakFrameIndex = 12; // 1-based frame 13 (đỉnh 5 gai đá)
        public const float RockSpikePivotY = 0.36f; // Đáy gai đặc (alpha >= 64 tại y ≈ 251/392)
        public const string StarfallBarrageSkillId = "starfall_barrage";
        public const string StarfallBarrageDisplayName = "Mưa Sao Băng"; // TẠM: Sư Huynh chốt tên thật sau Play-test
        public const string StarfallBarrageAssetPath =
            "Assets/Art/VFX/Skills/starfall_barrage/starfall-barrage-sheet.png";
        public const string StarfallBarrageDescription =
            "Kết ấn gọi mưa sao băng rơi xuống đầu boss."; // TẠM
        public const int StarfallBarrageDefaultManaCost = 3; // TẠM
        public const int StarfallBarrageDefaultRageCost = 0; // TẠM
        public const int StarfallBarrageDefaultCooldownTurns = 3; // TẠM
        public const int StarfallBarrageDefaultDamage = 100; // TẠM: Sư Huynh chốt sát thương thật sau Play-test
        public const int StarfallBarrageGridColumns = 10;
        public const int StarfallBarrageGridRows = 9;
        public const int StarfallBarrageFrameCount = StarfallBarrageGridColumns * StarfallBarrageGridRows;
        public const int StarfallBarragePeakFrameIndex = 51; // 1-based frame 52 (vòng nổ dưới đất to nhất / sáng nhất, 1.70s @30fps)
        public const float StarfallBarragePivotY = 0.30f; // Tâm vòng nổ tại y ≈ 448/640 từ trên xuống (pivot_y = (640 - 448) / 640 = 0.30)
        public const string ThunderGodSkillId = "thunder_god";
        public const string ThunderGodDisplayName = "Lôi Thần"; // TẠM: Sư Huynh chốt tên thật sau Play-test
        public const string ThunderGodAssetPath =
            "Assets/Art/VFX/Skills/thunder_god/thunder-god-strike-sheet.png";
        public const string ThunderGodDescription =
            "Kết ấn gọi mây giông, sét đánh xuống đầu boss."; // TẠM
        public const int ThunderGodDefaultManaCost = 3; // TẠM
        public const int ThunderGodDefaultRageCost = 0; // TẠM
        public const int ThunderGodDefaultCooldownTurns = 3; // TẠM
        public const int ThunderGodDefaultDamage = 100; // TẠM: Sư Huynh chốt sát thương thật sau Play-test
        public const int ThunderGodGridColumns = 10;
        public const int ThunderGodGridRows = 9;
        public const int ThunderGodFrameCount = ThunderGodGridColumns * ThunderGodGridRows;
        public const int ThunderGodPeakFrameIndex = 45; // 1-based frame 46 (sét khổng lồ đánh trúng + vòng xanh dưới đất chói nhất, 1.50s @30fps)
        public const float ThunderGodPivotY = 0.26f; // Tâm vòng xanh tại y ≈ 568/768 từ trên xuống (pivot_y = (768 - 568) / 768 = 0.2604 ≈ 0.26)
        public const string EllipseSwordsSkillId = "ellipse_swords";
        public const string EllipseSwordsDisplayName = "Ngũ Kiếm Trận"; // TẠM: Sư Huynh chốt tên thật sau Play-test
        public const string EllipseSwordsAssetPath =
            "Assets/Art/VFX/Skills/ellipse_portal/ellipse_portal.png";
        public const string EllipseSwordsDescription =
            "Mở 5 cổng hình elip, 5 kiếm bay ra chém boss."; // TẠM
        public const int EllipseSwordsDefaultManaCost = 3; // TẠM
        public const int EllipseSwordsDefaultRageCost = 0; // TẠM
        public const int EllipseSwordsDefaultCooldownTurns = 3; // TẠM
        public const int EllipseSwordsDefaultSwordCount = 5; // TẠM
        public const float EllipseSwordsDefaultIntervalSeconds = 0.15f; // TẠM
        public const float EllipseSwordsDefaultFlightSeconds = 0.50f; // TẠM
        public const string SwordRainSkillId = "sword_rain";
        public const string SwordRainDisplayName = "Kiếm Vũ"; // TẠM: Sư Huynh chốt tên thật sau Play-test
        public const string SwordRainDescription =
            "Nội tại: mỗi lần đánh thường trúng boss, 3 kiếm bay từ góc trên-trái xuống boss."; // TẠM
        public const int SwordRainDefaultManaCost = 0;
        public const int SwordRainDefaultRageCost = 0;
        public const int SwordRainDefaultCooldownTurns = 0;
        public const int SwordRainDefaultSwordCount = 3; // TẠM: Sư Huynh chốt số kiếm sau Play-test
        public const float SwordRainDefaultIntervalSeconds = 0.12f; // TẠM: Sư Huynh chốt nhịp giữa các kiếm sau Play-test
        public const float SwordRainDefaultFlightSeconds = 0.40f; // TẠM
        public const int DefaultManaCost = 3;
        public const int DefaultRageCost = 0;
        public const int DefaultCooldownTurns = 8;
        public const int FlyingSwordOrbitDurationTurns = 5;
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
        [SerializeField] Sprite[] effectFrames = Array.Empty<Sprite>();

        static Sprite cachedSwordSprite;
        static Sprite cachedSwordQiSprite;
        static Sprite cachedRockSpikeSprite;
        static Sprite[] cachedRockSpikeFrames;
        static Sprite cachedStarfallBarrageSprite;
        static Sprite[] cachedStarfallBarrageFrames;
        static Sprite cachedThunderGodSprite;
        static Sprite[] cachedThunderGodFrames;
        static Sprite cachedEllipseSwordsSprite;

        public string DisplayName => string.IsNullOrEmpty(displayName)
            ? (string.Equals(StableId, SwordQiSkillId, StringComparison.Ordinal)
                ? SwordQiDisplayName
                : (string.Equals(StableId, RockSpikeSkillId, StringComparison.Ordinal)
                    ? RockSpikeDisplayName
                    : (string.Equals(StableId, StarfallBarrageSkillId, StringComparison.Ordinal)
                        ? StarfallBarrageDisplayName
                        : (string.Equals(StableId, ThunderGodSkillId, StringComparison.Ordinal)
                            ? ThunderGodDisplayName
                            : (string.Equals(StableId, EllipseSwordsSkillId, StringComparison.Ordinal)
                                ? EllipseSwordsDisplayName
                                : (string.Equals(StableId, SwordRainSkillId, StringComparison.Ordinal)
                                    ? SwordRainDisplayName
                                    : FlyingSwordDisplayName))))))
            : displayName;
        public Sprite Icon => icon != null
            ? icon
            : (string.Equals(StableId, SwordQiSkillId, StringComparison.Ordinal)
                ? ResolveSwordQiSprite()
                : (string.Equals(StableId, RockSpikeSkillId, StringComparison.Ordinal)
                    ? ResolveRockSpikeSprite()
                    : (string.Equals(StableId, StarfallBarrageSkillId, StringComparison.Ordinal)
                        ? ResolveStarfallBarrageSprite()
                        : (string.Equals(StableId, ThunderGodSkillId, StringComparison.Ordinal)
                            ? ResolveThunderGodSprite()
                            : (string.Equals(StableId, EllipseSwordsSkillId, StringComparison.Ordinal)
                                ? ResolveEllipseSwordsSprite()
                                : ResolveSwordSprite())))));
        public Sprite[] EffectFrames => effectFrames;
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
            if (string.Equals(StableId, RockSpikeSkillId, StringComparison.Ordinal))
                return string.IsNullOrEmpty(description) ? RockSpikeDescription : description;
            if (string.Equals(StableId, StarfallBarrageSkillId, StringComparison.Ordinal))
                return string.IsNullOrEmpty(description) ? StarfallBarrageDescription : description;
            if (string.Equals(StableId, ThunderGodSkillId, StringComparison.Ordinal))
                return string.IsNullOrEmpty(description) ? ThunderGodDescription : description;
            if (string.Equals(StableId, EllipseSwordsSkillId, StringComparison.Ordinal))
                return string.IsNullOrEmpty(description) ? EllipseSwordsDescription : description;
            if (string.Equals(StableId, SwordRainSkillId, StringComparison.Ordinal))
                return string.IsNullOrEmpty(description) ? SwordRainDescription : description;
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

        public static Sprite ResolveEllipseSwordsSprite()
        {
            return ResolveSpriteAsset(EllipseSwordsAssetPath, ref cachedEllipseSwordsSprite);
        }

        public static Sprite ResolveRockSpikeSprite()
        {
            if (cachedRockSpikeSprite != null)
                return cachedRockSpikeSprite;

            Sprite[] frames = ResolveRockSpikeFrames();
            if (frames != null && frames.Length > RockSpikePeakFrameIndex)
                cachedRockSpikeSprite = frames[RockSpikePeakFrameIndex];
            else if (frames != null && frames.Length > 0)
                cachedRockSpikeSprite = frames[0];
            return cachedRockSpikeSprite;
        }

        public static Sprite[] ResolveRockSpikeFrames()
        {
            if (cachedRockSpikeFrames != null
                && cachedRockSpikeFrames.Length == RockSpikeFrameCount
                && cachedRockSpikeFrames[0] != null)
                return cachedRockSpikeFrames;

#if UNITY_EDITOR
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(RockSpikeAssetPath);
            if (all != null && all.Length > 0)
            {
                System.Collections.Generic.List<Sprite> sprites =
                    new System.Collections.Generic.List<Sprite>(RockSpikeFrameCount);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] is Sprite sprite)
                        sprites.Add(sprite);
                }
                if (sprites.Count > 0)
                {
                    // Sắp xếp theo THỨ TỰ HÌNH ẢNH (trên-trái -> dưới-phải):
                    // rect.y giảm dần (hàng trên trước), rồi rect.x tăng dần (trái sang phải),
                    // không phụ thuộc vào số hậu tố _N mà Unity đánh từ dưới lên.
                    sprites.Sort((a, b) =>
                    {
                        int yCompare = b.rect.y.CompareTo(a.rect.y);
                        if (Mathf.Abs(b.rect.y - a.rect.y) > 1f)
                            return yCompare;
                        return a.rect.x.CompareTo(b.rect.x);
                    });
                    cachedRockSpikeFrames = sprites.ToArray();
                    if (cachedRockSpikeFrames.Length > RockSpikePeakFrameIndex)
                        cachedRockSpikeSprite = cachedRockSpikeFrames[RockSpikePeakFrameIndex];
                    return cachedRockSpikeFrames;
                }
            }
#endif
            return Array.Empty<Sprite>();
        }

        public static Sprite ResolveStarfallBarrageSprite()
        {
            if (cachedStarfallBarrageSprite != null)
                return cachedStarfallBarrageSprite;

            Sprite[] frames = ResolveStarfallBarrageFrames();
            if (frames != null && frames.Length > StarfallBarragePeakFrameIndex)
                cachedStarfallBarrageSprite = frames[StarfallBarragePeakFrameIndex];
            else if (frames != null && frames.Length > 0)
                cachedStarfallBarrageSprite = frames[0];
            return cachedStarfallBarrageSprite;
        }

        public static Sprite[] ResolveStarfallBarrageFrames()
        {
            if (cachedStarfallBarrageFrames != null
                && cachedStarfallBarrageFrames.Length == StarfallBarrageFrameCount
                && cachedStarfallBarrageFrames[0] != null)
                return cachedStarfallBarrageFrames;

#if UNITY_EDITOR
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(StarfallBarrageAssetPath);
            if (all != null && all.Length > 0)
            {
                System.Collections.Generic.List<Sprite> sprites =
                    new System.Collections.Generic.List<Sprite>(StarfallBarrageFrameCount);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] is Sprite sprite)
                        sprites.Add(sprite);
                }
                if (sprites.Count > 0)
                {
                    // Sắp xếp theo THỨ TỰ HÌNH ẢNH (trên-trái -> dưới-phải):
                    // rect.y giảm dần (hàng trên trước), rồi rect.x tăng dần (trái sang phải),
                    // không phụ thuộc vào số hậu tố _N mà Unity đánh từ dưới lên.
                    sprites.Sort((a, b) =>
                    {
                        int yCompare = b.rect.y.CompareTo(a.rect.y);
                        if (Mathf.Abs(b.rect.y - a.rect.y) > 1f)
                            return yCompare;
                        return a.rect.x.CompareTo(b.rect.x);
                    });
                    cachedStarfallBarrageFrames = sprites.ToArray();
                    if (cachedStarfallBarrageFrames.Length > StarfallBarragePeakFrameIndex)
                        cachedStarfallBarrageSprite = cachedStarfallBarrageFrames[StarfallBarragePeakFrameIndex];
                    return cachedStarfallBarrageFrames;
                }
            }
#endif
            return Array.Empty<Sprite>();
        }

        public static Sprite ResolveThunderGodSprite()
        {
            if (cachedThunderGodSprite != null)
                return cachedThunderGodSprite;

            Sprite[] frames = ResolveThunderGodFrames();
            if (frames != null && frames.Length > ThunderGodPeakFrameIndex)
                cachedThunderGodSprite = frames[ThunderGodPeakFrameIndex];
            else if (frames != null && frames.Length > 0)
                cachedThunderGodSprite = frames[0];
            return cachedThunderGodSprite;
        }

        public static Sprite[] ResolveThunderGodFrames()
        {
            if (cachedThunderGodFrames != null
                && cachedThunderGodFrames.Length == ThunderGodFrameCount
                && cachedThunderGodFrames[0] != null)
                return cachedThunderGodFrames;

#if UNITY_EDITOR
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(ThunderGodAssetPath);
            if (all != null && all.Length > 0)
            {
                System.Collections.Generic.List<Sprite> sprites =
                    new System.Collections.Generic.List<Sprite>(ThunderGodFrameCount);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] is Sprite sprite)
                        sprites.Add(sprite);
                }
                if (sprites.Count > 0)
                {
                    // Sắp xếp theo THỨ TỰ HÌNH ẢNH (trên-trái -> dưới-phải):
                    // rect.y giảm dần (hàng trên trước), rồi rect.x tăng dần (trái sang phải),
                    // không phụ thuộc vào số hậu tố _N mà Unity đánh từ dưới lên.
                    sprites.Sort((a, b) =>
                    {
                        int yCompare = b.rect.y.CompareTo(a.rect.y);
                        if (Mathf.Abs(b.rect.y - a.rect.y) > 1f)
                            return yCompare;
                        return a.rect.x.CompareTo(b.rect.x);
                    });
                    cachedThunderGodFrames = sprites.ToArray();
                    if (cachedThunderGodFrames.Length > ThunderGodPeakFrameIndex)
                        cachedThunderGodSprite = cachedThunderGodFrames[ThunderGodPeakFrameIndex];
                    return cachedThunderGodFrames;
                }
            }
#endif
            return Array.Empty<Sprite>();
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

        public static SkillConfig CreateRuntimeRockSpikeFallback()
        {
            SkillConfig runtime = CreateInstance<SkillConfig>();
            runtime.name = RockSpikeSkillId;
            runtime.SetStableId(RockSpikeSkillId);
            runtime.displayName = RockSpikeDisplayName;
            runtime.icon = ResolveRockSpikeSprite();
            runtime.effectFrames = ResolveRockSpikeFrames();
            runtime.description = RockSpikeDescription;
            runtime.skillType = SkillType.Active;
            runtime.targetType = SkillTargetType.SingleEnemy;
            runtime.triggerType = SkillTriggerType.Manual;
            runtime.manaCost = RockSpikeDefaultManaCost;
            runtime.rageCost = RockSpikeDefaultRageCost;
            runtime.cooldownTurns = RockSpikeDefaultCooldownTurns;
            runtime.level = DefaultLevel;
            runtime.levelTable = Array.Empty<SkillLevelConfig>();
            return runtime;
        }

        public static SkillConfig CreateRuntimeStarfallBarrageFallback()
        {
            SkillConfig runtime = CreateInstance<SkillConfig>();
            runtime.name = StarfallBarrageSkillId;
            runtime.SetStableId(StarfallBarrageSkillId);
            runtime.displayName = StarfallBarrageDisplayName;
            runtime.icon = ResolveStarfallBarrageSprite();
            runtime.effectFrames = ResolveStarfallBarrageFrames();
            runtime.description = StarfallBarrageDescription;
            runtime.skillType = SkillType.Active;
            runtime.targetType = SkillTargetType.SingleEnemy;
            runtime.triggerType = SkillTriggerType.Manual;
            runtime.manaCost = StarfallBarrageDefaultManaCost;
            runtime.rageCost = StarfallBarrageDefaultRageCost;
            runtime.cooldownTurns = StarfallBarrageDefaultCooldownTurns;
            runtime.level = DefaultLevel;
            runtime.levelTable = Array.Empty<SkillLevelConfig>();
            return runtime;
        }

        public static SkillConfig CreateRuntimeThunderGodFallback()
        {
            SkillConfig runtime = CreateInstance<SkillConfig>();
            runtime.name = ThunderGodSkillId;
            runtime.SetStableId(ThunderGodSkillId);
            runtime.displayName = ThunderGodDisplayName;
            runtime.icon = ResolveThunderGodSprite();
            runtime.effectFrames = ResolveThunderGodFrames();
            runtime.description = ThunderGodDescription;
            runtime.skillType = SkillType.Active;
            runtime.targetType = SkillTargetType.SingleEnemy;
            runtime.triggerType = SkillTriggerType.Manual;
            runtime.manaCost = ThunderGodDefaultManaCost;
            runtime.rageCost = ThunderGodDefaultRageCost;
            runtime.cooldownTurns = ThunderGodDefaultCooldownTurns;
            runtime.level = DefaultLevel;
            runtime.levelTable = Array.Empty<SkillLevelConfig>();
            return runtime;
        }

        public static SkillConfig CreateRuntimeEllipseSwordsFallback()
        {
            SkillConfig runtime = CreateInstance<SkillConfig>();
            runtime.name = EllipseSwordsSkillId;
            runtime.SetStableId(EllipseSwordsSkillId);
            runtime.displayName = EllipseSwordsDisplayName;
            runtime.icon = ResolveEllipseSwordsSprite();
            runtime.description = EllipseSwordsDescription;
            runtime.skillType = SkillType.Active;
            runtime.targetType = SkillTargetType.SingleEnemy;
            runtime.triggerType = SkillTriggerType.Manual;
            runtime.manaCost = EllipseSwordsDefaultManaCost;
            runtime.rageCost = EllipseSwordsDefaultRageCost;
            runtime.cooldownTurns = EllipseSwordsDefaultCooldownTurns;
            runtime.level = DefaultLevel;
            runtime.levelTable = Array.Empty<SkillLevelConfig>();
            return runtime;
        }

        public static SkillConfig CreateRuntimeSwordRainFallback()
        {
            SkillConfig runtime = CreateInstance<SkillConfig>();
            runtime.name = SwordRainSkillId;
            runtime.SetStableId(SwordRainSkillId);
            runtime.displayName = SwordRainDisplayName;
            runtime.icon = ResolveSwordSprite();
            runtime.description = SwordRainDescription;
            runtime.skillType = SkillType.Passive;
            runtime.targetType = SkillTargetType.SingleEnemy;
            runtime.triggerType = SkillTriggerType.OnNormalAttack;
            runtime.manaCost = SwordRainDefaultManaCost;
            runtime.rageCost = SwordRainDefaultRageCost;
            runtime.cooldownTurns = SwordRainDefaultCooldownTurns;
            runtime.level = DefaultLevel;
            runtime.levelTable = Array.Empty<SkillLevelConfig>();
            return runtime;
        }
    }
}
