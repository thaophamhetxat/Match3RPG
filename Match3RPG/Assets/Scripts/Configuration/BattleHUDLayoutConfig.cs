using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Match3RPG.Configuration
{
    [Serializable]
    public struct BattleHUDTextStyle
    {
        [SerializeField, Min(1)] int fontSize;
        [SerializeField] bool autoSize;
        [SerializeField, Min(1)] int autoSizeMin;
        [SerializeField, Min(1)] int autoSizeMax;
        [SerializeField] Color color;
        [SerializeField] Color outlineColor;
        [SerializeField, Min(0f)] float outlineWidth;
        [SerializeField] TextAlignmentOptions alignment;

        public int FontSize => fontSize;
        public bool AutoSize => autoSize;
        public int AutoSizeMin => autoSizeMin;
        public int AutoSizeMax => autoSizeMax;
        public Color Color => color;
        public Color OutlineColor => outlineColor;
        public float OutlineWidth => outlineWidth;
        public TextAlignmentOptions Alignment => alignment;

        public BattleHUDTextStyle(int fontSize, bool autoSize, int autoSizeMin,
            int autoSizeMax, Color color, Color outlineColor, float outlineWidth,
            TextAlignmentOptions alignment)
        {
            this.fontSize = fontSize;
            this.autoSize = autoSize;
            this.autoSizeMin = autoSizeMin;
            this.autoSizeMax = autoSizeMax;
            this.color = color;
            this.outlineColor = outlineColor;
            this.outlineWidth = outlineWidth;
            this.alignment = alignment;
        }
    }

    [Serializable]
    public struct BattleHUDLayoutValues
    {
        public Rect HeroNameSlot;
        public Rect BossNameSlot;
        public BattleHUDTextStyle HeroNameStyle;
        public BattleHUDTextStyle BossNameStyle;
        public Rect HeroHpChannel;
        public Rect BossHpChannel;
        public Rect HeroSoulHpChannel;
        public Rect BossSoulHpChannel;
        public Vector2 HeroHpBarPosition;
        public Vector2 BossHpBarPosition;
        public Vector2 HeroHpBarSize;
        public float HpFillInset;
        public Rect HeroRageChannel;
        public Rect BossRageChannel;
        public float RageFillInset;
        public BattleHUDTextStyle HpValueStyle;
        public BattleHUDTextStyle RageValueStyle;
        public Vector2 TurnInfoAnchorPosition;
        public Vector2 TurnCountPosition;
        public Vector2 TurnCountSize;
        public Vector2 TimerPosition;
        public Vector2 TimerSize;
        public BattleHUDTextStyle TurnCountStyle;
        public BattleHUDTextStyle TimerStyle;
        public Color TimerWarningColor;
        public Color TimerDangerColor;
        public Vector2 MenuHitAreaPosition;
        public Vector2 MenuHitAreaSize;
    }

    [CreateAssetMenu(menuName = "Match3 RPG/Configuration/Battle HUD Layout")]
    public sealed class BattleHUDLayoutConfig : ScriptableObject
    {
        static readonly Rect DefaultHeroNameSlot = new Rect(.10f, .918f, .28f, .014f);
        static readonly Rect DefaultHeroHpChannel = new Rect(.075f, .896875f, .38f, .0625f);
        static readonly Rect DefaultHeroSoulHpChannel = new Rect(.075f, .8785f, .38f, .0625f);
        static readonly Rect DefaultHeroRageChannel = new Rect(.095f, .86025f, .38f, .0625f);

        [Header("Names")]
        [SerializeField, Tooltip("Hero name slot in normalized BattleLayoutReferenceRoot coordinates (0..1). Boss is mirrored automatically.")]
        Rect heroNameSlot = DefaultHeroNameSlot;
        [SerializeField, Tooltip("Optional Boss name offset in normalized BattleLayoutReferenceRoot coordinates (0..1).")]
        Vector2 bossNameOffsetNormalized;
        [SerializeField] BattleHUDTextStyle heroNameStyle = new BattleHUDTextStyle(24, true,
            16, 24, new Color32(245, 190, 88, 255), new Color(.03f, .025f, .02f, .9f),
            .12f, TextAlignmentOptions.MidlineLeft);
        [SerializeField] BattleHUDTextStyle bossNameStyle = new BattleHUDTextStyle(24, true,
            16, 24, new Color32(245, 190, 88, 255), new Color(.03f, .025f, .02f, .9f),
            .12f, TextAlignmentOptions.MidlineRight);

        [Header("HP Bar")]
        [SerializeField, Tooltip("Hero HP channel in normalized BattleLayoutReferenceRoot coordinates (0..1). Boss is mirrored automatically.")]
        Rect heroHpChannel = DefaultHeroHpChannel;
        [SerializeField, Tooltip("Hero Soul HP channel between HP and Rage in normalized BattleLayoutReferenceRoot coordinates (0..1). Boss is mirrored automatically.")]
        Rect heroSoulHpChannel = DefaultHeroSoulHpChannel;
        [SerializeField, Tooltip("Hero HP bar anchored position in reference pixels (1080x1920 BattleLayoutReferenceRoot). Boss X is mirrored automatically.")]
        Vector2 heroHpBarPosition = new Vector2(-282.57f, 942.1729f);
        [SerializeField, Tooltip("Optional Boss HP bar offset in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 bossHpBarOffsetReferencePixels;
        [SerializeField, Tooltip("Hero HP bar size in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 heroHpBarSize = new Vector2(385.6318f, 122.1733f);
        [SerializeField, Min(0f), Tooltip("Inset between HP fill and bar edges in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        float hpFillInset = 3f;
        [SerializeField] BattleHUDTextStyle hpValueStyle = new BattleHUDTextStyle(18, true,
            14, 18, Color.white, new Color(.03f, .025f, .02f, .9f), .18f,
            TextAlignmentOptions.Center);

        [Header("Rage Bar")]
        [SerializeField, Tooltip("Hero Rage channel in normalized BattleLayoutReferenceRoot coordinates (0..1). Boss is mirrored automatically.")]
        Rect heroRageChannel = DefaultHeroRageChannel;
        [SerializeField, Tooltip("Optional Boss Rage channel offset in normalized BattleLayoutReferenceRoot coordinates (0..1).")]
        Vector2 bossRageOffsetNormalized;
        [SerializeField, Min(0f), Tooltip("Rage fill inset in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        float rageFillInset = 3f;
        [SerializeField] BattleHUDTextStyle rageValueStyle = new BattleHUDTextStyle(13, true,
            10, 13, Color.white, new Color(.03f, .025f, .02f, .9f), .18f,
            TextAlignmentOptions.Center);

        [Header("Turn Info")]
        [SerializeField, Tooltip("TurnInfoAnchor position in normalized BattleLayoutReferenceRoot coordinates (0..1).")]
        Vector2 turnInfoAnchorPosition = new Vector2(.5f, .535f);
        [SerializeField, Tooltip("Turn count anchored position in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 turnCountPosition = new Vector2(-250f, -6f);
        [SerializeField, Tooltip("Turn count text size in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 turnCountSize = new Vector2(140f, 58f);
        [SerializeField, Tooltip("Timer anchored position in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 timerPosition = new Vector2(435f, -6f);
        [SerializeField, Tooltip("Timer text size in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 timerSize = new Vector2(90f, 58f);
        [SerializeField] BattleHUDTextStyle turnCountStyle = new BattleHUDTextStyle(28, true,
            20, 28, new Color32(245, 190, 88, 255), new Color(.03f, .025f, .02f, .9f),
            .18f, TextAlignmentOptions.Left);
        [SerializeField] BattleHUDTextStyle timerStyle = new BattleHUDTextStyle(28, true,
            20, 28, Color.white, new Color(.03f, .025f, .02f, .9f), .18f,
            TextAlignmentOptions.Center);
        [SerializeField, Tooltip("Timer color when more than 5 seconds remain.")]
        Color timerNormalColor = Color.white;
        [SerializeField, Tooltip("Timer color when 3-5 seconds remain, inclusive.")]
        Color timerWarningColor = Color.yellow;
        [SerializeField, Tooltip("Timer color when 3 seconds or less remain.")]
        Color timerDangerColor = Color.red;

        [Header("Menu Hit Area")]
        [SerializeField, Tooltip("Menu hit area center in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 menuHitAreaPosition = new Vector2(-419f, -1060f);
        [SerializeField, Tooltip("Menu hit area size in reference pixels (1080x1920 BattleLayoutReferenceRoot).")]
        Vector2 menuHitAreaSize = new Vector2(188f, 171f);

#if UNITY_EDITOR
        public static event Action<BattleHUDLayoutConfig> LayoutChanged;
        static bool invalidValuesWarningLogged;

        void OnValidate()
        {
            if (!invalidValuesWarningLogged)
            {
                string invalidValues = GetInvalidValues();
                if (!string.IsNullOrEmpty(invalidValues))
                {
                    Debug.LogWarning($"Battle HUD layout has zero/transparent values: {invalidValues}", this);
                    invalidValuesWarningLogged = true;
                }
            }

            if (Application.isPlaying)
                LayoutChanged?.Invoke(this);
        }

        string GetInvalidValues()
        {
            var invalid = new StringBuilder();
            AppendInvalidSize(invalid, nameof(heroNameSlot), heroNameSlot.width, heroNameSlot.height);
            AppendInvalidSize(invalid, nameof(heroHpChannel), heroHpChannel.width, heroHpChannel.height);
            AppendInvalidSize(invalid, nameof(heroSoulHpChannel), heroSoulHpChannel.width,
                heroSoulHpChannel.height);
            AppendInvalidSize(invalid, nameof(heroHpBarSize), heroHpBarSize.x, heroHpBarSize.y);
            AppendInvalidSize(invalid, nameof(heroRageChannel), heroRageChannel.width, heroRageChannel.height);
            AppendInvalidSize(invalid, nameof(turnCountSize), turnCountSize.x, turnCountSize.y);
            AppendInvalidSize(invalid, nameof(timerSize), timerSize.x, timerSize.y);
            AppendInvalidSize(invalid, nameof(menuHitAreaSize), menuHitAreaSize.x, menuHitAreaSize.y);
            AppendInvalidStyle(invalid, nameof(heroNameStyle), heroNameStyle);
            AppendInvalidStyle(invalid, nameof(bossNameStyle), bossNameStyle);
            AppendInvalidStyle(invalid, nameof(hpValueStyle), hpValueStyle);
            AppendInvalidStyle(invalid, nameof(rageValueStyle), rageValueStyle);
            AppendInvalidStyle(invalid, nameof(turnCountStyle), turnCountStyle);
            AppendInvalidStyle(invalid, nameof(timerStyle), timerStyle);
            return invalid.ToString().TrimEnd(',', ' ');
        }

        static void AppendInvalidSize(StringBuilder invalid, string field, float width, float height)
        {
            if (width <= 0f || height <= 0f)
                invalid.Append(field).Append(", ");
        }

        static void AppendInvalidStyle(StringBuilder invalid, string field, BattleHUDTextStyle style)
        {
            if (style.FontSize <= 0 || (style.AutoSize
                    && (style.AutoSizeMin <= 0 || style.AutoSizeMax <= 0))
                || style.Color.a <= 0f)
                invalid.Append(field).Append(", ");
        }
#endif

        public BattleHUDLayoutValues GetValues()
        {
            Rect heroName = heroNameSlot;
            Rect heroHp = heroHpChannel;
            Rect heroSoulHp = heroSoulHpChannel;
            Rect heroRage = heroRageChannel;
            return new BattleHUDLayoutValues
            {
                HeroNameSlot = heroName,
                BossNameSlot = Mirror(heroName, bossNameOffsetNormalized),
                HeroNameStyle = heroNameStyle,
                BossNameStyle = bossNameStyle,
                HeroHpChannel = heroHp,
                BossHpChannel = Mirror(heroHp, Vector2.zero),
                HeroSoulHpChannel = heroSoulHp,
                BossSoulHpChannel = Mirror(heroSoulHp, Vector2.zero),
                HeroHpBarPosition = heroHpBarPosition,
                BossHpBarPosition = new Vector2(-heroHpBarPosition.x, heroHpBarPosition.y)
                    + bossHpBarOffsetReferencePixels,
                HeroHpBarSize = heroHpBarSize,
                HpFillInset = hpFillInset,
                HeroRageChannel = heroRage,
                BossRageChannel = Mirror(heroRage, bossRageOffsetNormalized),
                RageFillInset = rageFillInset,
                HpValueStyle = hpValueStyle,
                RageValueStyle = rageValueStyle,
                TurnInfoAnchorPosition = turnInfoAnchorPosition,
                TurnCountPosition = turnCountPosition,
                TurnCountSize = turnCountSize,
                TimerPosition = timerPosition,
                TimerSize = timerSize,
                TurnCountStyle = turnCountStyle,
                TimerStyle = timerStyle,
                TimerWarningColor = timerWarningColor,
                TimerDangerColor = timerDangerColor,
                MenuHitAreaPosition = menuHitAreaPosition,
                MenuHitAreaSize = menuHitAreaSize
            };
        }

        public static BattleHUDLayoutValues DefaultValues => new BattleHUDLayoutValues
        {
            HeroNameSlot = DefaultHeroNameSlot,
            BossNameSlot = Mirror(DefaultHeroNameSlot, Vector2.zero),
            HeroNameStyle = new BattleHUDTextStyle(24, true, 16, 24,
                new Color32(245, 190, 88, 255), new Color(.03f, .025f, .02f, .9f),
                .12f, TextAlignmentOptions.MidlineLeft),
            BossNameStyle = new BattleHUDTextStyle(24, true, 16, 24,
                new Color32(245, 190, 88, 255), new Color(.03f, .025f, .02f, .9f),
                .12f, TextAlignmentOptions.MidlineRight),
            HeroHpChannel = DefaultHeroHpChannel,
            BossHpChannel = Mirror(DefaultHeroHpChannel, Vector2.zero),
            HeroSoulHpChannel = DefaultHeroSoulHpChannel,
            BossSoulHpChannel = Mirror(DefaultHeroSoulHpChannel, Vector2.zero),
            HeroHpBarPosition = new Vector2(-282.57f, 942.1729f),
            BossHpBarPosition = new Vector2(282.57f, 942.1729f),
            HeroHpBarSize = new Vector2(385.6318f, 122.1733f),
            HpFillInset = 3f,
            HeroRageChannel = DefaultHeroRageChannel,
            BossRageChannel = Mirror(DefaultHeroRageChannel, Vector2.zero),
            RageFillInset = 3f,
            HpValueStyle = new BattleHUDTextStyle(18, true, 14, 18, Color.white,
                new Color(.03f, .025f, .02f, .9f), .18f, TextAlignmentOptions.Center),
            RageValueStyle = new BattleHUDTextStyle(13, true, 10, 13, Color.white,
                new Color(.03f, .025f, .02f, .9f), .18f, TextAlignmentOptions.Center),
            TurnInfoAnchorPosition = new Vector2(.5f, .535f),
            TurnCountPosition = new Vector2(-250f, -6f),
            TurnCountSize = new Vector2(140f, 58f),
            TimerPosition = new Vector2(435f, -6f),
            TimerSize = new Vector2(90f, 58f),
            TurnCountStyle = new BattleHUDTextStyle(28, true, 20, 28,
                new Color32(245, 190, 88, 255), new Color(.03f, .025f, .02f, .9f),
                .18f, TextAlignmentOptions.Left),
            TimerStyle = new BattleHUDTextStyle(28, true, 20, 28, Color.white,
                new Color(.03f, .025f, .02f, .9f), .18f, TextAlignmentOptions.Center),
            TimerWarningColor = Color.yellow,
            TimerDangerColor = Color.red,
            MenuHitAreaPosition = new Vector2(-419f, -1060f),
            MenuHitAreaSize = new Vector2(188f, 171f)
        };

        static Rect Mirror(Rect rect, Vector2 offset)
        {
            return new Rect(1f - rect.xMax + offset.x, rect.y + offset.y,
                rect.width, rect.height);
        }
    }
}
