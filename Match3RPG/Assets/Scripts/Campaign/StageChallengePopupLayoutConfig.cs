using UnityEngine;

namespace Match3RPG.Campaign
{
    [CreateAssetMenu(menuName = "Match3 RPG/Configuration/Stage Challenge Popup Layout")]
    public sealed class StageChallengePopupLayoutConfig : ScriptableObject
    {
        [Header("Popup")]
        [SerializeField, Range(.5f, 1f), Tooltip("Maximum popup width as a fraction of the safe screen width.")]
        float maxWidthFraction = .9f;
        [SerializeField, Range(.5f, 1f), Tooltip("Maximum popup height as a fraction of the safe screen height.")]
        float maxHeightFraction = .94f;
        [SerializeField, Tooltip("Character and text placement rectangles normalized to the popup artwork (0..1, bottom-left origin).")]
        Rect heroPreviewRect = new Rect(.08f, .5f, .38f, .28f);
        [SerializeField] Rect bossPreviewRect = new Rect(.54f, .5f, .38f, .28f);
        [SerializeField] Rect heroPowerTextRect = new Rect(.175f, .425f, .265f, .045f);
        [SerializeField] Rect bossNameTextRect = new Rect(.60f, .425f, .30f, .045f);
        [SerializeField] Rect rewardAreaRect = new Rect(.09f, .285f, .82f, .12f);
        [SerializeField, Tooltip("Reward icon placement inside each reward cell (0..1).")]
        Rect rewardIconRect = new Rect(.02f, .12f, .28f, .76f);
        [SerializeField, Tooltip("Reward label placement inside each reward cell (0..1).")]
        Rect rewardTextRect = new Rect(.31f, .04f, .68f, .92f);
        [SerializeField] Rect stageLabelRect = new Rect(.24f, .23f, .52f, .045f);
        [SerializeField, Tooltip("Button hit rectangles normalized to the popup artwork (0..1, bottom-left origin).")]
        Rect exitHitRect = new Rect(.075f, .132f, .425f, .094f);
        [SerializeField] Rect startHitRect = new Rect(.485f, .132f, .445f, .094f);

        [Header("Text")]
        [SerializeField, Min(1f)] float powerFontSize = 25f;
        [SerializeField, Min(1f)] float bossNameFontSize = 22f;
        [SerializeField, Min(1f)] float rewardFontSize = 17f;
        [SerializeField, Min(1f)] float stageFontSize = 24f;
        [SerializeField] Color textColor = new Color32(255, 235, 190, 255);
        [SerializeField] Color outlineColor = new Color(.035f, .02f, .01f, .9f);
        [SerializeField, Range(0f, 1f)] float outlineWidth = .16f;

        [Header("Character previews")]
        [SerializeField, Min(.02f), Tooltip("Seconds between idle frames.")]
        float idleFrameSeconds = .12f;
        [Header("Rewards")]
        [SerializeField, Min(1)] int maxRewardEntries = 6;
        [SerializeField] Sprite goldIcon;
        [SerializeField] Sprite experienceIcon;
        [SerializeField, Min(0f)] float combatPowerHpWeight = 1f;
        [SerializeField, Min(0f)] float combatPowerAttackWeight = 10f;
        [SerializeField, Min(0f)] float combatPowerLevelWeight = 100f;

        public float MaxWidthFraction => maxWidthFraction;
        public float MaxHeightFraction => maxHeightFraction;
        public Rect HeroPreviewRect => heroPreviewRect;
        public Rect BossPreviewRect => bossPreviewRect;
        public Rect HeroPowerTextRect => heroPowerTextRect;
        public Rect BossNameTextRect => bossNameTextRect;
        public Rect RewardAreaRect => rewardAreaRect;
        public Rect RewardIconRect => rewardIconRect;
        public Rect RewardTextRect => rewardTextRect;
        public Rect StageLabelRect => stageLabelRect;
        public Rect ExitHitRect => exitHitRect;
        public Rect StartHitRect => startHitRect;
        public float PowerFontSize => powerFontSize;
        public float BossNameFontSize => bossNameFontSize;
        public float RewardFontSize => rewardFontSize;
        public float StageFontSize => stageFontSize;
        public Color TextColor => textColor;
        public Color OutlineColor => outlineColor;
        public float OutlineWidth => outlineWidth;
        public float IdleFrameSeconds => idleFrameSeconds;
        public int MaxRewardEntries => maxRewardEntries;
        public Sprite GoldIcon => goldIcon;
        public Sprite ExperienceIcon => experienceIcon;
        public float CombatPowerHpWeight => combatPowerHpWeight;
        public float CombatPowerAttackWeight => combatPowerAttackWeight;
        public float CombatPowerLevelWeight => combatPowerLevelWeight;
    }
}
