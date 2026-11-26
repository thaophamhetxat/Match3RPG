using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using Match3RPG.Battle;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using Match3RPG.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Match3RPG.Campaign
{
    public sealed class MainProgressController : MonoBehaviour
    {
        const bool DebugHitboxVisualizationEnabled = false;
        const float LoadingDurationSeconds = 2f;
        const float LoadingFadeSeconds = 0.3f;
        const int LegacyInventoryItemCount = 4;
        const int TotalInventoryItemCount = 32;
        const int MaxEquipmentUpgradeLevel = 15;
        const int MaxEquipmentQualityTier = 3;
        const string BlackDragonSetDetailText = "Bộ Hắc Long (hệ ATK) — 6 món: Kiếm, Mũ, Giáp, Găng, Giầy, Nhẫn.";
        const string MeditateAnimatorResourcePath = "MainProgress/Hero2_Meditate";
        const string SoulHarvestRedSheetAssetPath = "Assets/Art/VFX/Campaign/SouldH/soul-harvest-red-sheet.png";
        const string SoulHarvestRedSheetResourcePath = "VFX/Campaign/SouldH/soul-harvest-red-sheet";
        const float SoulHarvestOrbFrameRate = 30f;
        const float SoulHarvestOrbAspectRatio = 520f / 576f;
        const float SoulHarvestOrbMinScale = 0.6f;
        const float SoulHarvestOrbMaxScale = 2.0f;
        const float SoulHarvestOrbZoomStep = 0.2f;

        // Thiên phú chỉ để hiển thị mốc nâng cấp, KHÔNG cộng thêm bonus riêng vào chỉ số.
        static readonly (int milestoneLevel, string text, Vector2 anchorMin, Vector2 anchorMax)[] EquipmentTalentMilestones =
        {
            (1, "+1: Tăng 10% ATK", new Vector2(0.091f, 0.315f), new Vector2(0.909f, 0.35f)),
            // TODO (Thao): chốt giá trị thật cho mốc +15
            (15, "+15: Tăng 20% HP", new Vector2(0.091f, 0.278f), new Vector2(0.909f, 0.313f))
        };

        static readonly string[] BlackDragonItemIds =
        {
            "black_dragon_sword",
            "black_dragon_helmet",
            "black_dragon_armor",
            "black_dragon_gauntlet",
            "black_dragon_boots",
            "black_dragon_ring"
        };

        static readonly string[] LegacyItemIds =
        {
            "azure_dragon", "blue_potion", "immortal_peach", "sun_spirit_orb"
        };

        static readonly string[] SpiritStoneItemIds =
        {
            "linh_thach_ha_pham", "linh_thach_trung_pham",
            "linh_thach_thuong_pham", "linh_thach_cuc_pham"
        };

        static readonly string[] WeaponItemIds =
        {
            "quat_xep_ma_thuat",
            "thanh_kiem",
            "ma_kiem_hong_ngoc",
            "quyen_truong_bang_gia",
            "quyen_truong_hac_am",
            "thuong_ba_kich_bong_dem",
            "dao_gam_ma_thuat",
            "qua_cau_ma_thuat_chiem_tinh",
            "cap_song_dao_nguyet_anh",
            "thai_duong_than_kiem",
            "bao_kiem_bang_pha_le",
            "ma_kiem_nguyet_bac",
            "dao_ban_rong_uon_cong",
            "than_kiem_long_vu",
            "quat_xep_hoang_gia_hac_kim",
            "ma_kiem_bang_tinh",
            "cu_kiem_thiet_giap",
            "quyen_truong_mat_troi_co"
        };

        [SerializeField] MainProgressCharacterPresentation presentation;
        [SerializeField] TMP_FontAsset starChartFont;
        [SerializeField] Sprite inventoryItemSlot;
        [SerializeField] Sprite[] inventoryItemSprites = new Sprite[4];
        [SerializeField] Sprite[] starChartBaseGemSprites = new Sprite[7];
        [SerializeField] Sprite[] starChartSpecialGemSprites = new Sprite[7];
        [SerializeField] Sprite[] soulHarvestRedFrames;

        static Sprite tabFocusWhiteSprite;
        static Sprite starPolygonSprite;
        static Sprite rainbowBorderSprite;
        static Sprite chamferedPlaqueSprite;
        static Sprite bevelTriangleLeftSprite;
        static Sprite bevelTriangleRightSprite;
        static readonly Dictionary<int, Sprite> roundedRectSprites = new Dictionary<int, Sprite>();

        static readonly (Vector2 min, Vector2 max)[] InventoryTabAnchors =
        {
            (new Vector2(.052f, .822f), new Vector2(.341f, .867f)),
            (new Vector2(.358f, .822f), new Vector2(.645f, .867f)),
            (new Vector2(.661f, .822f), new Vector2(.950f, .867f))
        };

        static readonly (Vector2 min, Vector2 max)[] CharacterTabAnchors =
        {
            (new Vector2(.038f, .818f), new Vector2(.260f, .864f)),
            (new Vector2(.273f, .818f), new Vector2(.493f, .864f)),
            (new Vector2(.507f, .818f), new Vector2(.728f, .864f)),
            (new Vector2(.742f, .818f), new Vector2(.962f, .864f))
        };

        // Mở rộng khung focus tab Túi đồ bằng đúng hit area để 9-slice hiển thị rõ hình chữ nhật
        static readonly (Vector2 min, Vector2 max)[] InventoryTabFocusAnchors =
        {
            (new Vector2(.052f, .822f), new Vector2(.341f, .867f)),
            (new Vector2(.358f, .822f), new Vector2(.645f, .867f)),
            (new Vector2(.661f, .822f), new Vector2(.950f, .867f))
        };

        // Mở rộng khung focus tab Nhân vật bằng đúng hit area để 9-slice hiển thị rõ hình chữ nhật
        static readonly (Vector2 min, Vector2 max)[] CharacterTabFocusAnchors =
        {
            (new Vector2(.038f, .818f), new Vector2(.260f, .864f)),
            (new Vector2(.273f, .818f), new Vector2(.493f, .864f)),
            (new Vector2(.507f, .818f), new Vector2(.728f, .864f)),
            (new Vector2(.742f, .818f), new Vector2(.962f, .864f))
        };

        Canvas canvas;
        RectTransform safeAreaRect;
        RectTransform contentRoot;
        Image backgroundImage;
        RectTransform characterLayer;
        RectTransform meditationAnchor;
        Image meditationVisual;
        readonly List<Button> mainNavigationButtons = new List<Button>();
        GameObject characterPanel;
        GameObject inventoryPanel;
        GameObject inventoryItemPopup;
        GameObject cultivationPanel;
        TMP_Text bannerPlayerNameLabel;
        TMP_Text bannerLevelExpRealmLabel;
        GameObject renamePopup;
        TMP_InputField renameInputField;
        GameObject skillsPlaceholder;
        GameObject starChartUIGroup;
        GameObject starChartContent;
        Image inventoryBackgroundImage;
        Image cultivationBackgroundImage;
        Image inventoryPopupBackgroundImage;
        Image inventoryPopupItemImage;
        TMP_Text inventoryPopupItemName;
        TMP_Text inventoryPopupItemEffect;
        GameObject inventoryUpgradePopup;
        int inventoryUpgradeItemIndex;
        ItemConfig inventoryUpgradeItemConfig;
        bool inventoryUpgradeFromStatus;
        int inventoryUpgradeTabMode;
        Button inventoryUpgradeEnhanceTabButton, inventoryUpgradeQualityTabButton;
        TMP_Text inventoryUpgradeEnhanceTabLabel, inventoryUpgradeQualityTabLabel;
        GameObject inventoryUpgradeEnhanceContent, inventoryUpgradeQualityContent;
        TMP_Text inventoryUpgradeLevelLabel;
        Button inventoryUpgradeEnhanceActionButton;
        TMP_Text inventoryUpgradeEnhanceActionLabel;
        TMP_Text inventoryUpgradeQualityLabel;
        readonly Image[] inventoryUpgradeQualityStarImages = new Image[4];
        readonly Outline[] inventoryUpgradeQualityStarOutlines = new Outline[4];
        Button inventoryUpgradeQualityActionButton;
        TMP_Text inventoryUpgradeQualityActionLabel;
        GameObject spiritStoneCombinePopup;
        int spiritStoneCombineMode;
        int spiritStoneCombineGrade;
        int spiritStoneCombineQuantity;
        Button spiritStoneCombineTabButton, spiritStoneDismantleTabButton;
        TMP_Text spiritStoneCombineTabLabel, spiritStoneDismantleTabLabel;
        Button[] spiritStoneCombineGradeButtons = new Button[4];
        TMP_Text[] spiritStoneCombineGradeQuantityLabels = new TMP_Text[4];
        Image spiritStoneCombineSourceIcon, spiritStoneCombineTargetIcon;
        TMP_Text spiritStoneCombineSourceName, spiritStoneCombineTargetName;
        TMP_Text spiritStoneCombineSourceQuantity, spiritStoneCombineTargetResult;
        TMP_Text spiritStoneCombineQuantityLabel;
        Slider spiritStoneCombineSlider;
        Button spiritStoneCombineConfirmButton;
        TMP_Text spiritStoneCombineConfirmLabel;
        Button spiritStoneCombineMinusButton, spiritStoneCombinePlusButton;
        RectTransform inventoryTabFocusRect;
        RectTransform characterTabFocusRect;
        readonly Button[] characterTabButtons = new Button[4];
        Button[] inventoryTabButtons = new Button[3];
        readonly Button[] inventoryItemButtons = new Button[TotalInventoryItemCount];
        readonly GameObject[] inventoryItemContents = new GameObject[TotalInventoryItemCount];
        readonly Outline[] inventorySlotOutlines = new Outline[TotalInventoryItemCount];
        readonly GameObject[] inventoryItemEquippedLabels = new GameObject[TotalInventoryItemCount];
        float inventoryGridCellSize;
        float inventoryGridSlotSize;
        float inventoryGridRowPitch;
        float inventoryGridXOffset;
        float inventoryGridTopOffset;
        Sprite[] resolvedInventoryItemSprites;
        TMP_Text realmButtonLabel;
        Image realmProgressFillImage;
        TMP_Text realmProgressLabel;
        TMP_Text starActionStatus;
        TMP_Text starActionHint;
        ScrollRect starChartDetailsScrollRect;
        RectTransform starChartDetailsContent;
        RectTransform starActionHintRect;
        RectTransform starActionStatusRect;
        TMP_Text breakthroughRequirementLabel;
        Button previousRealmButton;
        Button nextRealmButton;
        GameObject breakthroughPopup;
        Button breakthroughButton;
        TMP_Text breakthroughButtonLabel;
        Image breakthroughGlowImage;
        Button breakthroughConfirmButton;
        RectTransform starChartGraphicRect;
        ScrollRect starChartZoomScrollRect;
        RectTransform soulHarvestOrbRect;
        Image soulHarvestOrbImage;
        Button soulHarvestOrbButton;
        Sprite[] resolvedSoulHarvestRedFrames;
        Sprite[] activeSoulHarvestOrbFrames;
        float soulHarvestOrbElapsed;
        int soulHarvestOrbFrameIndex = -1;
        RectTransform starNodePopup;
        TMP_Text starNodePopupText;
        Button starNodePopupCloseButton;
        Button starNodePopupUpgradeButton;
        TMP_Text starNodePopupUpgradeLabel;
        int popupStarBranchIndex = -1;
        int popupStarIndex = -1;
        Button totalBuffButton;
        GameObject totalBuffPopup;
        TMP_Text totalBuffPopupText;
        float currentStarChartZoom = 1f;
        MainProgressChartGraphic starChartGraphic;
        StarChartGemInventoryService starChartGemInventory;
        readonly TMP_Text[] baseGemInventoryCountLabels = new TMP_Text[7];
        readonly Outline[] baseGemSlotOutlines = new Outline[7];
        readonly Image[] baseGemSlotGlowImages = new Image[7];
        readonly TMP_Text[] characterStatValueLabels = new TMP_Text[12];
        readonly Image[] equippedSlotIcons = new Image[6];
        // Thứ tự đúng bằng thứ tự 6 dòng AddEquipmentSlot trong
        // BuildCharacterPanel: 0 Mũ, 1 Giáp, 2 Giày, 3 Vũ khí, 4 Pháp bảo, 5 Găng.
        static readonly EquipmentSlot[] StatusSlotEquipmentSlots =
        {
            EquipmentSlot.Head,      // 0 Mũ
            EquipmentSlot.Body,      // 1 Giáp
            EquipmentSlot.Feet,      // 2 Giày
            EquipmentSlot.Weapon,    // 3 Vũ khí
            EquipmentSlot.Accessory, // 4 Pháp bảo (Nhẫn Hắc Long là Accessory)
            EquipmentSlot.Gloves     // 5 Găng
        };
        int selectedStarChartRealmIndex;
        int selectedStarChartLevel;
        int selectedInventoryTab;
        int selectedCharacterTab = 1;

        readonly int[] baseGemDisplayInventory = new int[7];
        readonly int[] litStarsPerBranch = new int[7];
        int selectedStarChartBranchIndex;

        static readonly GemType[] StarChartGemTypes =
        {
            GemType.Attack,
            GemType.Mana,
            GemType.ExpDrain,
            GemType.Rage,
            GemType.Health,
            GemType.Shield,
            GemType.Soul
        };

        static readonly string[] StarChartGemNames =
        {
            "Lửa Đỏ", "Giọt Nước", "Sen Tím", "Mặt Trời",
            "Lá Xanh", "Hộ Thuẫn", "Lửa Trắng"
        };

        static readonly string[] StarChartStatDescriptions =
        {
            "+1 ATK",
            "+1 Né, +0.5 Chính xác, +1 Speed",
            "+1 EXP / viên Sen Tím",
            "+1% Dame crit, +1% DEF",
            "+50 HP",
            "+10 giá trị gem Hộ Thuẫn",
            "+1 ATK soul"
        };

        readonly struct RealmDefinition
        {
            public readonly string Name;
            public readonly int MinLevel;
            public readonly int MaxLevel;

            public RealmDefinition(string name, int minLevel, int maxLevel)
            {
                Name = name;
                MinLevel = minLevel;
                MaxLevel = maxLevel;
            }
        }

        static readonly RealmDefinition[] Realms =
        {
            new RealmDefinition("Luyện Khí", 1, 9),
            new RealmDefinition("Trúc Cơ", 10, 19),
            new RealmDefinition("Kết Đan", 20, 29),
            new RealmDefinition("Nguyên Anh", 30, 39),
            new RealmDefinition("Hóa Thần", 40, 49),
            new RealmDefinition("Luyện Hư", 50, 59),
            new RealmDefinition("Hợp Thể", 60, 69),
            new RealmDefinition("Đại Thừa", 70, 79),
            new RealmDefinition("Độ Kiếp", 80, 89),
            new RealmDefinition("Chân Tiên", 90, 99),
            new RealmDefinition("Kim Tiên", 100, 109),
            new RealmDefinition("Thái Ất", 110, 119),
            new RealmDefinition("Đại La", 120, 129),
            new RealmDefinition("Đạo Tổ", 130, 139)
        };

        static readonly Color Gold = new Color32(231, 190, 110, 255);
        static readonly Color MutedGold = new Color32(190, 169, 124, 255);

        void Awake()
        {
            presentation ??= MainProgressCharacterPresentation.LoadDefault();
            starChartGemInventory = new StarChartGemInventoryService(SaveManager.Instance);
            RefreshBaseGemInventory();
            BuildLoadingScreen();
            BuildUI();
            ApplyStaticCharacterPresentation();
            RefreshBannerInfo();
        }

        void BuildLoadingScreen()
        {
            Sprite loadingSprite = Resources.Load<Sprite>("MainProgress/UI/loadingScreen");
            if (loadingSprite == null)
            {
                Debug.LogError("MainProgress failed to load sprite Resources/MainProgress/UI/loadingScreen.", this);
                return;
            }

            Canvas loadingCanvas = CampaignUIFactory.CreateCanvas("LoadingScreenCanvas");
            loadingCanvas.sortingOrder = 100;

            CanvasGroup loadingCanvasGroup = loadingCanvas.gameObject.AddComponent<CanvasGroup>();
            loadingCanvasGroup.alpha = 1f;
            loadingCanvasGroup.blocksRaycasts = true;
            loadingCanvasGroup.interactable = false;

            RectTransform backdropRect = CampaignUIFactory.CreateRect("LoadingBackdrop",
                loadingCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdropImage = backdropRect.gameObject.AddComponent<Image>();
            backdropImage.color = new Color32(12, 27, 36, 255);
            backdropImage.raycastTarget = true;

            RectTransform loadingImageRect = CampaignUIFactory.CreateRect("LoadingArtwork",
                loadingCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image loadingImage = loadingImageRect.gameObject.AddComponent<Image>();
            loadingImage.sprite = loadingSprite;
            loadingImage.color = Color.white;
            loadingImage.type = Image.Type.Simple;
            loadingImage.preserveAspect = true;
            loadingImage.raycastTarget = false;

            RectTransform trackRect = CampaignUIFactory.CreateRect("LoadingProgressTrack",
                loadingCanvas.transform, new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.12f),
                Vector2.zero, Vector2.zero);
            Image trackImage = trackRect.gameObject.AddComponent<Image>();
            trackImage.color = new Color32(12, 27, 36, 255);
            trackImage.raycastTarget = false;
            Outline trackOutline = trackRect.gameObject.AddComponent<Outline>();
            trackOutline.effectColor = Gold;
            trackOutline.effectDistance = new Vector2(2f, -2f);

            EnsureTabFocusWhiteSprite();
            RectTransform fillRect = CampaignUIFactory.CreateRect("LoadingProgressFill",
                trackRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.sprite = tabFocusWhiteSprite;
            fillImage.color = Gold;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;

            TMP_Text loadingLabel = AddLabel(loadingCanvas.transform, "LoadingProgressLabel",
                "Đang tải... 0%", 24, Gold, new Vector2(0.15f, 0.125f), new Vector2(0.85f, 0.155f),
                TextAlignmentOptions.Center);
            if (starChartFont != null)
                loadingLabel.font = starChartFont;

            StartCoroutine(RunLoadingScreenRoutine(loadingCanvas.gameObject,
                loadingCanvasGroup, fillImage, loadingLabel));
        }

        IEnumerator RunLoadingScreenRoutine(GameObject loadingRoot,
            CanvasGroup loadingCanvasGroup, Image fillImage, TMP_Text loadingLabel)
        {
            float progress = 0f;
            while (progress < 1f)
            {
                yield return null;
                if (loadingRoot == null)
                    yield break;

                progress = Mathf.Clamp01(progress + Time.unscaledDeltaTime / LoadingDurationSeconds);
                if (fillImage != null)
                    fillImage.fillAmount = progress;
                if (loadingLabel != null)
                {
                    int percent = Mathf.RoundToInt(progress * 100f);
                    loadingLabel.text = $"Đang tải... {percent}%";
                }
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < LoadingFadeSeconds)
            {
                yield return null;
                if (loadingRoot == null)
                    yield break;

                fadeElapsed += Time.unscaledDeltaTime;
                if (loadingCanvasGroup != null)
                    loadingCanvasGroup.alpha = 1f - Mathf.Clamp01(fadeElapsed / LoadingFadeSeconds);
            }

            if (loadingRoot != null)
                Destroy(loadingRoot);
        }

        void OnEnable()
        {
            RefreshBannerInfo();
        }

        void Update()
        {
            UpdateSoulHarvestOrbAnimation();
        }

        void BuildUI()
        {
            EnsureSceneCamera();

            canvas = CampaignUIFactory.CreateCanvas("MainProgressCanvas");
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            safeAreaRect = CampaignUIFactory.CreateSafeAreaRoot(canvas.transform);

            contentRoot = CampaignUIFactory.CreateRect("MainProgressContent", safeAreaRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform backgroundLayer = CampaignUIFactory.CreateRect("BackgroundLayer", canvas.transform,
    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
backgroundLayer.SetAsFirstSibling();
            backgroundImage = backgroundLayer.gameObject.AddComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = true;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.sprite = Resources.Load<Sprite>("MainProgress/Progress/MoonlitLakeBackground") ?? (presentation != null ? presentation.ProgressBackground : null);
            backgroundImage.color = Color.white;

            if (backgroundImage.sprite == null)
                backgroundImage.color = new Color32(14, 19, 20, 255);

            float screenW = GetCanvasScreenWidth();

            RectTransform heroBanner = CampaignUIFactory.CreateRect("HeroBanner", contentRoot,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            heroBanner.pivot = new Vector2(0.5f, 1f);
            heroBanner.offsetMin = new Vector2(21.33f, -102.80f);
            heroBanner.offsetMax = new Vector2(-443.33f, 75f);      
            Image heroBannerImage = heroBanner.gameObject.AddComponent<Image>();
            heroBannerImage.raycastTarget = false;
            heroBannerImage.preserveAspect = true;
            heroBannerImage.type = Image.Type.Simple;
            heroBannerImage.color = Color.white;
            Sprite heroBannerSprite = Resources.Load<Sprite>("MainProgress/UI/HeroBanner");
            if (heroBannerSprite == null)
            {
                Debug.LogError("MainProgress failed to load sprite Resources/MainProgress/UI/HeroBanner.", this);
                heroBanner.gameObject.SetActive(false);
            }
            else
            {
                heroBannerImage.sprite = heroBannerSprite;
            }

            CreateNavigationHitArea(heroBanner, "AvatarHitArea",
                new Vector2(0.01f, 0.05f), new Vector2(0.33f, 0.95f), Color.magenta,
                OpenRenamePopup);

            bannerPlayerNameLabel = AddLabel(heroBanner, "BannerPlayerName", string.Empty, 26, Gold,
                   new Vector2(0.34f, 0.40f), new Vector2(0.95f, 0.63f), TextAlignmentOptions.MidlineLeft);
            if (starChartFont != null)
                bannerPlayerNameLabel.font = starChartFont;

            bannerLevelExpRealmLabel = AddLabel(heroBanner, "BannerLevelExpRealm", string.Empty, 18, Gold,
                 new Vector2(0.34f, 0.18f), new Vector2(0.95f, 0.35f), TextAlignmentOptions.MidlineLeft);
            if (starChartFont != null)
                bannerLevelExpRealmLabel.font = starChartFont;

            characterLayer = CampaignUIFactory.CreateRect("CharacterLayer", contentRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            characterLayer.SetAsLastSibling();
            characterLayer.anchoredPosition = Vector2.zero;

            meditationAnchor = CampaignUIFactory.CreateRect("MeditationAnchor", characterLayer,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            meditationAnchor.sizeDelta = new Vector2(560f, 560f);
            meditationAnchor.anchoredPosition = Vector2.zero;
            if (presentation != null)
                meditationAnchor.anchoredPosition = new Vector2((presentation.LotusAnchor.x - .5f) * 560f, (presentation.LotusAnchor.y - .5f) * 150f);

            meditationVisual = CreateVisualImage("MeditationCharacter", meditationAnchor, null);
            meditationVisual.raycastTarget = false;
            meditationVisual.preserveAspect = true;
            meditationVisual.color = AttachMeditateAnimator(meditationVisual)
                ? Color.white
                : new Color(1f, 1f, 1f, 0f);

            CreateNavigationButtons(screenW);
        }

        float GetCanvasScreenWidth()
        {
            Canvas targetCanvas = contentRoot != null ? contentRoot.GetComponentInParent<Canvas>() ?? canvas : canvas;
            CanvasScaler scaler = targetCanvas != null
                ? targetCanvas.GetComponent<CanvasScaler>()
                : null;
            float scaleFactor = 0f;
            if (scaler != null
                && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                && scaler.referenceResolution.x > 0f
                && scaler.referenceResolution.y > 0f)
            {
                float widthScale = Screen.width / scaler.referenceResolution.x;
                float heightScale = Screen.height / scaler.referenceResolution.y;
                float match = Mathf.Clamp01(scaler.matchWidthOrHeight);
                scaleFactor = Mathf.Pow(widthScale, 1f - match)
                    * Mathf.Pow(heightScale, match);
            }

            if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor)
                || scaleFactor <= 0f)
            {
                scaleFactor = targetCanvas != null
                    && targetCanvas.scaleFactor > 0f
                    && !float.IsNaN(targetCanvas.scaleFactor)
                    && !float.IsInfinity(targetCanvas.scaleFactor)
                    ? targetCanvas.scaleFactor
                    : 1f;
            }

            return Screen.width / scaleFactor;
        }

        void ApplyStaticCharacterPresentation()
        {
            if (presentation == null)
            {
                Debug.LogWarning("MainProgress presentation data is unavailable; the static character cannot be resolved.", this);
                return;
            }

            if (meditationVisual == null)
                return;

            float finalScale = Mathf.Max(.1f, presentation.MainProgressCharacterScale);
            meditationVisual.color = meditationVisual.GetComponent<Animator>() != null
                ? Color.white
                : new Color(1f, 1f, 1f, 0f);
            meditationVisual.rectTransform.localScale = Vector3.one * finalScale;
            meditationVisual.rectTransform.anchoredPosition = presentation.MainProgressCharacterOffset;
        }

        bool AttachMeditateAnimator(Image targetImage)
        {
            if (targetImage == null)
                return false;

            RuntimeAnimatorController meditateController =
                Resources.Load<RuntimeAnimatorController>(MeditateAnimatorResourcePath);
            if (meditateController == null)
            {
                Debug.LogError(
                    $"MainProgress failed to load animator controller Resources/{MeditateAnimatorResourcePath}.",
                    this);
                return false;
            }

            Animator animator = targetImage.gameObject.GetComponent<Animator>();
            if (animator == null)
                animator = targetImage.gameObject.AddComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError(
                    $"MainProgress failed to attach Animator to {targetImage.gameObject.name}.",
                    this);
                return false;
            }
            animator.runtimeAnimatorController = meditateController;
            return true;
        }

        Image CreateVisualImage(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = CampaignUIFactory.CreateRect(name, parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = new Vector2(519.36f, 498.66f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            return image;
        }

        void CreateNavigationButtons(float screenW)
        {
            RectTransform navButtonsStrip = CampaignUIFactory.CreateRect("NavButtonsStrip", contentRoot,
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            navButtonsStrip.SetAsLastSibling();
            navButtonsStrip.pivot = new Vector2(0.5f, 0f);
            navButtonsStrip.offsetMin = new Vector2(40.33f, 46.13f);
            navButtonsStrip.offsetMax = new Vector2(-46.13f, 349.76f);

            Image navButtonsImage = navButtonsStrip.gameObject.AddComponent<Image>();
            navButtonsImage.raycastTarget = false;
            navButtonsImage.preserveAspect = true;
            navButtonsImage.type = Image.Type.Simple;
            navButtonsImage.color = Color.white;
            Sprite navButtonsSprite = Resources.Load<Sprite>("MainProgress/UI/NavButtons");
            if (navButtonsSprite == null)
            {
                Debug.LogError("MainProgress failed to load sprite Resources/MainProgress/UI/NavButtons.", this);
                navButtonsStrip.gameObject.SetActive(false);
            }
            else
            {
                navButtonsImage.sprite = navButtonsSprite;
            }

            mainNavigationButtons.Add(CreateNavigationHitArea(navButtonsStrip, "StatusHitArea",
                new Vector2(.02f, 0f), new Vector2(.2378f, .7334f), Color.red,
                OpenCharacterPanel));

            mainNavigationButtons.Add(CreateNavigationHitArea(navButtonsStrip, "CultivationHitArea",
                new Vector2(.2636f, 0f), new Vector2(.7265f, .4077f), Color.green,
                OpenCharacterScreen));

            mainNavigationButtons.Add(CreateNavigationHitArea(navButtonsStrip, "AdventureHitArea",
                new Vector2(.762f, 0f), new Vector2(.98f, .7334f), Color.blue,
                () =>
                {
                    CloseRenamePopup();
                    Debug.Log("[MainProgress] Clicked=Adventure");
                    SceneManager.LoadScene(CampaignManager.WorldMapSceneName);
                }));
        }

        Button CreateNavigationHitArea(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color debugColor, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform hitAreaRect = CampaignUIFactory.CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            hitAreaRect.offsetMin = Vector2.zero;
            hitAreaRect.offsetMax = Vector2.zero;

            Image hitImage = hitAreaRect.gameObject.AddComponent<Image>();
            hitImage.raycastTarget = true;
            hitImage.color = DebugHitboxVisualizationEnabled ? new Color(debugColor.r, debugColor.g, debugColor.b, .22f) : new Color(1f, 1f, 1f, 0f);
            hitImage.preserveAspect = false;
            hitImage.type = Image.Type.Simple;
            hitImage.sprite = null;

            Button button = hitAreaRect.gameObject.AddComponent<Button>();
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            return button;
        }

        void OpenCharacterPanel()
        {
            if (characterPanel == null)
                characterPanel = BuildCharacterPanel().gameObject;
            RefreshCharacterStats();
            RefreshEquippedSlotIcons();
            SetPanelOpen(characterPanel);
        }

        void OpenCharacterScreen()
        {
            if (cultivationPanel == null)
                cultivationPanel = BuildCultivationPanel().gameObject;
            RefreshBaseGemInventory();
            SetPanelOpen(cultivationPanel);
            SelectCharacterTab(1);
        }

        void OpenInventoryPanel(GameObject characterOverlay)
        {
            if (characterOverlay != null)
                characterOverlay.SetActive(false);
            if (inventoryPanel == null)
                inventoryPanel = BuildInventoryPanel().gameObject;
            SetPanelOpen(inventoryPanel);
            SelectInventoryTab(0);
        }

        void SetPanelOpen(GameObject panel)
        {
            CloseRenamePopup();
            foreach (Button button in mainNavigationButtons)
                if (button != null)
                    button.interactable = false;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        void ClosePanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
            foreach (Button button in mainNavigationButtons)
                if (button != null)
                    button.interactable = true;
            RefreshBannerInfo();
        }

        RectTransform BuildCharacterPanel()
        {
            RectTransform overlay = CreateAnchoredRect(canvas.transform,
                "CharacterPanelOverlay", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            Image backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, .78f);
            backdrop.raycastTarget = true;
            Sprite statusSprite = Resources.Load<Sprite>("MainProgress/UI/statusInfo");
            if (statusSprite == null)
                Debug.LogError("MainProgress failed to load sprite Resources/MainProgress/UI/statusInfo.", this);
            RectTransform artworkRect = CreateFullScreenArtwork(overlay,
                statusSprite, "CharacterArtwork",
                out Image artworkImage, fitToScreen: true);
            overlay.SetAsLastSibling();

            RectTransform heroRect = CreateAnchoredRect(artworkRect, "CharacterPortrait",
                new Vector2(.300f, .680f), new Vector2(.700f, .880f),
                Vector2.zero, Vector2.zero);
            Image heroImage = heroRect.gameObject.AddComponent<Image>();
            heroImage.preserveAspect = true;
            heroImage.raycastTarget = false;
            heroImage.color = AttachMeditateAnimator(heroImage)
                ? Color.white
                : new Color(1f, 1f, 1f, .15f);

            // 6 ô trang bị 2 bên vòng tròn chân dung + nhãn tên loại bên dưới mỗi ô (cột trái dịch phải 12px)
            (int index, string name, float minX, float minY, float maxX, float maxY, float labelMinY, float labelMaxY)[] equipmentSlots =
            {
                (0, "Mũ",       .086f, .826f, .193f, .876f, .813f, .824f),
                (1, "Giáp",     .086f, .756f, .193f, .806f, .743f, .754f),
                (2, "Giày",     .086f, .686f, .193f, .735f, .672f, .683f),
                (3, "Vũ khí",   .797f, .826f, .904f, .876f, .813f, .824f),
                (4, "Pháp bảo", .797f, .756f, .904f, .806f, .743f, .754f),
                (5, "Găng",     .797f, .686f, .904f, .735f, .672f, .683f),
            };
            foreach (var slot in equipmentSlots)
            {
                AddEquipmentSlot(artworkRect, slot.index, slot.name,
                    slot.minX, slot.minY, slot.maxX, slot.maxY);
                TMP_Text slotLabel = AddLabel(artworkRect, $"EquipmentLabel_{slot.name}",
                    slot.name, 20, Gold,
                    new Vector2(slot.minX, slot.labelMinY),
                    new Vector2(slot.maxX, slot.labelMaxY),
                    TextAlignmentOptions.Center);
                slotLabel.raycastTarget = false;
            }

            const float closeMinX = .870f;
            const float closeMaxX = .977f;
            const float closeMinY = .891f;
            const float closeMaxY = .948f;
            CreateCharacterHitButton(artworkRect, "CloseButton",
                closeMinX, closeMinY, closeMaxX, closeMaxY,
                () => ClosePanel(overlay.gameObject));

            const float firstStatRowCenter = .5025f;
            const float statRowSpacing = .0291f;
            const float statRowHeight = .0291f;
            for (int index = 0; index < characterStatValueLabels.Length; index++)
            {
                float centerY = firstStatRowCenter - index * statRowSpacing;
                characterStatValueLabels[index] = AddLabel(artworkRect,
                    $"CharacterStatValue_{index}", "—", 24, Color.white,
                    new Vector2(.70f, centerY - statRowHeight * .5f),
                    new Vector2(.91f, centerY + statRowHeight * .5f),
                    TextAlignmentOptions.MidlineRight);
            }

            CreateCharacterHitButton(artworkRect, "InventoryButton",
                .31f, .077f, .69f, .139f,
                () => OpenInventoryPanel(overlay.gameObject));
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        // Tạo ô trang bị gồm vùng bấm, nền tối viền vàng và icon trang bị bên trên
        void AddEquipmentSlot(Transform parent, int slotIndex, string slotName,
            float minX, float minY, float maxX, float maxY)
        {
            Button button = CreateCharacterHitButton(parent, $"Equipment_{slotName}",
                minX, minY, maxX, maxY,
                () => OpenStatusItemPopup(slotIndex));
            RectTransform buttonRect = button.GetComponent<RectTransform>();

            // Nền ô trang bị (tạo trước icon để nằm dưới icon)
            RectTransform bgRect = CreateAnchoredRect(buttonRect, "SlotBackground",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImage = bgRect.gameObject.AddComponent<Image>();
            bgImage.color = new Color32(12, 27, 36, 255);
            bgImage.raycastTarget = false;
            Outline bgOutline = bgRect.gameObject.AddComponent<Outline>();
            bgOutline.effectColor = Gold;
            bgOutline.effectDistance = new Vector2(2f, -2f);

            // Căn giữa icon trang bị đều 4 phía trong ô
            RectTransform iconRect = CreateAnchoredRect(buttonRect, "EquippedIcon",
                new Vector2(.08f, .08f), new Vector2(.92f, .92f),
                Vector2.zero, Vector2.zero);
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.gameObject.SetActive(false);
            if (slotIndex >= 0 && slotIndex < equippedSlotIcons.Length)
                equippedSlotIcons[slotIndex] = iconImage;
        }

        static Button CreateCharacterHitButton(Transform parent, string name,
            float minX, float minY, float maxX, float maxY, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateAnchoredRect(parent, name,
                new Vector2(minX, minY), new Vector2(maxX, maxY),
                Vector2.zero, Vector2.zero);
            Image hitImage = rect.gameObject.AddComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0f);
            hitImage.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            return button;
        }

        void RefreshCharacterStats()
        {
            RuntimeHeroStats? stats = LoadSelectedHeroStats();
            if (!stats.HasValue)
            {
                for (int index = 0; index < characterStatValueLabels.Length; index++)
                    if (characterStatValueLabels[index] != null)
                        characterStatValueLabels[index].text = "—";
                return;
            }

            RuntimeHeroStats heroStats = stats.Value;
            string[] values =
            {
                FormatStat(heroStats.Attack),
                FormatStat(heroStats.SoulAttack),
                FormatStat(heroStats.Defense),
                FormatStat(heroStats.MaxHP),
                FormatStat(heroStats.MaxSoulHP),
                FormatPercent(heroStats.CriticalChancePercent),
                FormatPercent(heroStats.CriticalDamageMultiplier * 100f),
                FormatPercent(heroStats.DodgePercent),
                FormatPercent(heroStats.AccuracyPercent),
                FormatStat(heroStats.Speed),
                "10",
                "5"
            };
            for (int index = 0; index < characterStatValueLabels.Length; index++)
                if (characterStatValueLabels[index] != null)
                    characterStatValueLabels[index].text = values[index];
        }

        void RefreshEquippedSlotIcons()
        {
            // Ẩn hết trước, rồi chỉ hiện ô nào có đồ đang mặc.
            for (int i = 0; i < equippedSlotIcons.Length; i++)
                if (equippedSlotIcons[i] != null)
                    equippedSlotIcons[i].gameObject.SetActive(false);

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
                return;

            SaveManager saveManager = SaveManager.Instance;
            EquipmentService equipmentService = new EquipmentService(saveManager, config);
            InventoryService inventoryService = new InventoryService(saveManager, config);
            foreach (EquipmentLoadoutSaveData entry in equipmentService.GetEquipmentLoadout())
            {
                if (entry == null || string.IsNullOrEmpty(entry.itemInstanceId))
                    continue;
                ItemInstanceSaveData instance = inventoryService.GetEquipmentInstance(
                    entry.itemInstanceId);
                if (instance == null)
                    continue;
                ItemConfig itemConfig = config.GetItem(instance.itemId);
                if (itemConfig == null)
                {
                    Debug.LogError($"[MainProgress] Missing ItemConfig for stableId '{instance.itemId}' in GameConfigService.", this);
                    continue;
                }
                int slotIndex = Array.IndexOf(StatusSlotEquipmentSlots, itemConfig.EquipmentSlot);
                if (slotIndex < 0 || slotIndex >= equippedSlotIcons.Length)
                    continue;
                Image iconImage = equippedSlotIcons[slotIndex];
                if (iconImage == null || itemConfig.RewardIcon == null)
                    continue;
                iconImage.sprite = itemConfig.RewardIcon;
                iconImage.gameObject.SetActive(true);
            }
        }

        void OpenStatusItemPopup(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= StatusSlotEquipmentSlots.Length)
                return;
            // Tìm ItemConfig đang mặc ở ô này (đi đúng luồng
            // RefreshEquippedSlotIcons: loadout → instance → GetItem).
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
                return;
            SaveManager saveManager = SaveManager.Instance;
            EquipmentService equipmentService = new EquipmentService(saveManager, config);
            InventoryService inventoryService = new InventoryService(saveManager, config);
            EquipmentSlot wantedSlot = StatusSlotEquipmentSlots[slotIndex];
            ItemConfig equippedConfig = null;
            foreach (EquipmentLoadoutSaveData entry in equipmentService.GetEquipmentLoadout())
            {
                if (entry == null || string.IsNullOrEmpty(entry.itemInstanceId))
                    continue;
                ItemInstanceSaveData instance = inventoryService.GetEquipmentInstance(
                    entry.itemInstanceId);
                if (instance == null)
                    continue;
                ItemConfig itemConfig = config.GetItem(instance.itemId);
                if (itemConfig != null && itemConfig.EquipmentSlot == wantedSlot)
                {
                    equippedConfig = itemConfig;
                    break;
                }
            }
            if (equippedConfig == null)
                return; // Ô trống: không mở popup.
            int itemIndex = GetInventoryItemIndex(equippedConfig.StableId);
            if (itemIndex < 0 || itemIndex >= TotalInventoryItemCount)
                return;
            OpenInventoryItemPopup(itemIndex, fromStatus: true);
        }

        static string FormatStat(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        static string FormatPercent(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        RectTransform BuildInventoryPanel()
        {
            RectTransform overlay = CreateFullScreenOverlay("InventoryPanelOverlay",
                new Color32(4, 28, 22, 255));
            Sprite tuidoSprite = Resources.Load<Sprite>("MainProgress/UI/TuidoSingle");
            if (tuidoSprite == null)
                Debug.LogError("MainProgress failed to load sprite Resources/MainProgress/UI/TuidoSingle.", this);
            RectTransform artwork = CreateFullScreenArtwork(overlay,
                tuidoSprite, "InventoryArtwork",
                out inventoryBackgroundImage, fitToScreen: true);

            inventoryTabFocusRect = CreateTabFocusOverlay(artwork,
                "InventoryTabFocusOverlay",
                InventoryTabFocusAnchors[0].min, InventoryTabFocusAnchors[0].max);

            CreateCharacterHitButton(artwork, "InventoryCloseButton",
                .882f, .895f, .996f, .947f,
                () => ClosePanel(overlay.gameObject));

            inventoryTabButtons[0] = CreateCharacterHitButton(artwork,
                "InventoryTab_Items", .052f, .822f, .341f, .867f,
                () => SelectInventoryTab(0));
            inventoryTabButtons[1] = CreateCharacterHitButton(artwork,
                "InventoryTab_Fragment", .358f, .822f, .645f, .867f,
                () => SelectInventoryTab(1));
            inventoryTabButtons[2] = CreateCharacterHitButton(artwork,
                "InventoryTab_Materials", .661f, .822f, .950f, .867f,
                () => SelectInventoryTab(2));

            RectTransform viewportRect = CreateAnchoredRect(artwork, "InventoryGridViewport",
                new Vector2(.043f, .259f), new Vector2(.956f, .805f),
                new Vector2(0f, 2f), new Vector2(0f, -2f));
            viewportRect.offsetMin = new Vector2(0f, 2f);
            viewportRect.offsetMax = new Vector2(0f, -2f);
            Image viewportImage = viewportRect.gameObject.AddComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;
            Mask viewportMask = viewportRect.gameObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            ScrollRect scrollRect = viewportRect.gameObject.AddComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 1f;

            Canvas targetCanvas = artwork.GetComponentInParent<Canvas>() ?? canvas;
            CanvasScaler scaler = targetCanvas != null
                ? targetCanvas.GetComponent<CanvasScaler>()
                : null;
            float scaleFactor = 0f;
            if (scaler != null
                && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                && scaler.referenceResolution.x > 0f
                && scaler.referenceResolution.y > 0f)
            {
                float widthScale = Screen.width / scaler.referenceResolution.x;
                float heightScale = Screen.height / scaler.referenceResolution.y;
                float match = Mathf.Clamp01(scaler.matchWidthOrHeight);
                scaleFactor = Mathf.Pow(widthScale, 1f - match)
                    * Mathf.Pow(heightScale, match);
            }

            if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor)
                || scaleFactor <= 0f)
            {
                scaleFactor = targetCanvas != null
                    && targetCanvas.scaleFactor > 0f
                    && !float.IsNaN(targetCanvas.scaleFactor)
                    && !float.IsInfinity(targetCanvas.scaleFactor)
                    ? targetCanvas.scaleFactor
                    : 1f;
            }

            float screenW = Screen.width / scaleFactor;
            float screenH = Screen.height / scaleFactor;
            const int columns = 5;
            const int rows = 7;
            int totalSlots = columns * rows;
            float viewportWidth = (0.956f - 0.043f) * screenW;
            float viewportHeight = (0.805f - 0.259f) * screenH - 4f;
            float widthCellSize = (0.956f - 0.043f) * screenW / columns;
            const float gridTopMargin = 40f;
            const float gridBottomMargin = 40f;
            float heightCellSize = (viewportHeight - gridTopMargin - gridBottomMargin - 30f)
                / (6f * 0.97f + 0.725f);
            float cellSize = Mathf.Min(widthCellSize, heightCellSize);
            float slotSize = cellSize * 0.725f;
            float rowPitch = cellSize * 0.97f;
            float gridSpan = 6f * rowPitch + slotSize + 30f;
            float gridTopOffset = (viewportHeight - gridSpan) * 0.5f;
            float gridXOffset = (viewportWidth - columns * cellSize) * 0.5f;
            inventoryGridCellSize = cellSize;
            inventoryGridSlotSize = slotSize;
            inventoryGridRowPitch = rowPitch;
            inventoryGridXOffset = gridXOffset;
            inventoryGridTopOffset = gridTopOffset;
            float contentHeight = viewportHeight;

            RectTransform contentRect = CreateAnchoredRect(viewportRect, "InventoryGridContent",
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentHeight);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            Sprite[] allItemSprites = GetResolvedInventoryItemSprites();

            for (int index = 0; index < totalSlots; index++)
            {
                int capturedIndex = index;
                int row = index / columns;
                int column = index % columns;
                RectTransform slotRect = CreateAnchoredRect(contentRect,
                    $"InventorySlot_{index}",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    Vector2.zero, Vector2.zero);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = new Vector2(slotSize, slotSize);
                slotRect.anchoredPosition = new Vector2(
                    gridXOffset + (column + 0.5f) * cellSize,
                    -(gridTopOffset + (row + 0.5f) * rowPitch));

                Image slotImage = slotRect.gameObject.AddComponent<Image>();
                slotImage.color = new Color32(12, 27, 36, 255);
                slotImage.raycastTarget = true;
                Outline slotOutline = slotRect.gameObject.AddComponent<Outline>();
                slotOutline.effectColor = Gold;
                slotOutline.effectDistance = new Vector2(2f, -2f);

                Button slotButton = slotRect.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slotImage;
                slotButton.transition = Selectable.Transition.None;

                if (index < allItemSprites.Length
                    && GetSpriteAt(allItemSprites, index) != null)
                {
                    ItemConfig slotItemConfig = GetBlackDragonItemConfig(index) ?? GetWeaponItemConfig(index);
                    if (slotItemConfig != null)
                        ApplyGradeSlotStyle(slotRect, slotImage, slotOutline, slotItemConfig.Grade);

                    Sprite itemSprite = allItemSprites[index];
                    RectTransform itemRect = CreateAnchoredRect(slotRect,
                        "ItemArtwork", new Vector2(.12f, .12f),
                        new Vector2(.88f, .88f), Vector2.zero, Vector2.zero);
                    Image itemImage = itemRect.gameObject.AddComponent<Image>();
                    itemImage.sprite = itemSprite;
                    itemImage.preserveAspect = true;
                    itemImage.raycastTarget = false;

                    RectTransform markRect = CreateAnchoredRect(slotRect,
                        $"InventoryItemEquipped_{index}",
                        new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(2f, -28f), new Vector2(28f, -2f));
                    Image markBg = markRect.gameObject.AddComponent<Image>();
                    markBg.color = new Color32(15, 15, 20, 230);
                    markBg.raycastTarget = false;
                    Outline markOutline = markRect.gameObject.AddComponent<Outline>();
                    markOutline.effectColor = Gold;
                    markOutline.effectDistance = new Vector2(1f, -1f);
                    TextMeshProUGUI markText = CampaignUIFactory.CreateText(
                        markRect, "Text", "E", 18, Color.white, TextAlignmentOptions.Center);
                    markText.fontStyle = FontStyles.Bold;
                    markText.raycastTarget = false;
                    markRect.gameObject.SetActive(false);

                    slotButton.onClick.AddListener(
                        () => OpenInventoryItemPopup(capturedIndex));
                    inventoryItemButtons[index] = slotButton;
                    inventoryItemContents[index] = itemRect.gameObject;
                    inventorySlotOutlines[index] = slotOutline;
                    inventoryItemEquippedLabels[index] = markRect.gameObject;
                }
            }

            overlay.gameObject.SetActive(false);
            return overlay;
        }

        void SelectInventoryTab(int tabIndex)
        {
            selectedInventoryTab = Mathf.Clamp(tabIndex, 0, 2);
            if (inventoryTabFocusRect != null)
            {
                inventoryTabFocusRect.anchorMin = InventoryTabFocusAnchors[selectedInventoryTab].min;
                inventoryTabFocusRect.anchorMax = InventoryTabFocusAnchors[selectedInventoryTab].max;
                inventoryTabFocusRect.offsetMin = Vector2.zero;
                inventoryTabFocusRect.offsetMax = Vector2.zero;
            }
            for (int index = 0; index < inventoryTabButtons.Length; index++)
                if (inventoryTabButtons[index] != null)
                    inventoryTabButtons[index].interactable = index != selectedInventoryTab;
            for (int index = 0; index < inventoryItemButtons.Length; index++)
            {
                if (inventoryItemButtons[index] != null)
                    inventoryItemButtons[index].interactable =
                        IsItemVisibleOnTab(index, selectedInventoryTab);
                if (inventoryItemContents[index] != null)
                    inventoryItemContents[index].SetActive(
                        IsItemVisibleOnTab(index, selectedInventoryTab));
            }
            // Cap nhat lai mau o theo trang thai an/hien cua item tren tab dang chon
            RefreshInventorySlotStyles();
            RepackInventoryGrid();
            RefreshInventoryGridEquippedVisuals();
        }

        // Tra mau o ve mac dinh khi item bi an hoac to mau cap bac khi trang bi dang hien
        void RefreshInventorySlotStyles()
        {
            for (int index = 0; index < TotalInventoryItemCount; index++)
            {
                Button button = inventoryItemButtons[index];
                if (button == null)
                    continue;

                Image slotImage = button.targetGraphic as Image;
                Outline slotOutline = inventorySlotOutlines[index];
                RectTransform slotRect = button.GetComponent<RectTransform>();
                ItemConfig config = GetBlackDragonItemConfig(index) ?? GetWeaponItemConfig(index);

                if (IsItemVisibleOnTab(index, selectedInventoryTab) && config != null)
                {
                    ApplyGradeSlotStyle(slotRect, slotImage, slotOutline, config.Grade);
                }
                else
                {
                    if (slotImage != null)
                    {
                        slotImage.color = new Color32(12, 27, 36, 255);
                        slotImage.sprite = null;
                        slotImage.type = Image.Type.Simple;
                    }
                    if (slotOutline != null)
                    {
                        slotOutline.enabled = true;
                        slotOutline.effectColor = Gold;
                    }
                }
            }
        }

        void RepackInventoryGrid()
        {
            // Xếp 10 ô item (index 0-9) theo thứ tự hiển thị: item hiện ở
            // tab này xếp trước (từ ô 0), item ẩn ở tab này xếp sau.
            int displayPosition = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int index = 0; index < TotalInventoryItemCount; index++)
                {
                    bool visible = IsItemVisibleOnTab(index, selectedInventoryTab);
                    if ((pass == 0) != visible)
                        continue;
                    if (inventoryItemButtons[index] == null)
                        continue;
                    int row = displayPosition / 5;
                    int column = displayPosition % 5;
                    float x = inventoryGridXOffset
                        + (column + 0.5f) * inventoryGridCellSize;
                    float y = -(inventoryGridTopOffset
                        + (row + 0.5f) * inventoryGridRowPitch);
                    // Dời cả ô (khung + icon + nút bấm đi cùng nhau).
                    RectTransform slotRect = inventoryItemButtons[index]
                        .GetComponent<RectTransform>();
                    if (slotRect != null)
                        slotRect.anchoredPosition = new Vector2(x, y);
                    displayPosition++;
                }
            }
        }

        void RefreshInventoryGridEquippedVisuals()
        {
            for (int index = 0; index < TotalInventoryItemCount; index++)
            {
                ItemConfig itemConfig = GetInventoryItemConfig(index);
                bool isEquipped = selectedInventoryTab == 0
                    && itemConfig != null
                    && IsItemEquipped(itemConfig, out _);

                if (inventorySlotOutlines[index] != null)
                {
                    inventorySlotOutlines[index].effectDistance = isEquipped
                        ? new Vector2(3f, -3f)
                        : new Vector2(2f, -2f);
                }

                if (inventoryItemEquippedLabels[index] != null)
                    inventoryItemEquippedLabels[index].SetActive(isEquipped);
            }
        }

        Sprite[] GetResolvedInventoryItemSprites()
        {
            if (resolvedInventoryItemSprites != null
                && resolvedInventoryItemSprites.Length == TotalInventoryItemCount)
                return resolvedInventoryItemSprites;

            resolvedInventoryItemSprites = new Sprite[TotalInventoryItemCount];
            for (int index = 0; index < LegacyInventoryItemCount; index++)
                resolvedInventoryItemSprites[index] = GetSpriteAt(inventoryItemSprites, index);

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            for (int offset = 0; offset < BlackDragonItemIds.Length; offset++)
            {
                string stableId = BlackDragonItemIds[offset];
                ItemConfig itemConfig = config != null ? config.GetItem(stableId) : null;
                if (itemConfig == null)
                {
                    Debug.LogError($"[MainProgress] Missing ItemConfig for stableId '{stableId}' in GameConfigService.", this);
                    continue;
                }

                resolvedInventoryItemSprites[LegacyInventoryItemCount + offset] = itemConfig.RewardIcon;
            }

            for (int offset = 0; offset < SpiritStoneItemIds.Length; offset++)
            {
                string stableId = SpiritStoneItemIds[offset];
                ItemConfig itemConfig = config != null ? config.GetItem(stableId) : null;
                if (itemConfig == null)
                {
                    Debug.LogError($"[MainProgress] Missing ItemConfig for stableId '{stableId}' in GameConfigService.", this);
                    continue;
                }

                resolvedInventoryItemSprites[LegacyInventoryItemCount + BlackDragonItemIds.Length + offset] = itemConfig.RewardIcon;
            }

            for (int offset = 0; offset < WeaponItemIds.Length; offset++)
            {
                string stableId = WeaponItemIds[offset];
                ItemConfig itemConfig = config != null ? config.GetItem(stableId) : null;
                if (itemConfig == null)
                {
                    Debug.LogError($"[MainProgress] Missing ItemConfig for stableId '{stableId}' in GameConfigService.", this);
                    continue;
                }

                resolvedInventoryItemSprites[LegacyInventoryItemCount + BlackDragonItemIds.Length + SpiritStoneItemIds.Length + offset] = itemConfig.RewardIcon;
            }

            return resolvedInventoryItemSprites;
        }

        ItemConfig GetBlackDragonItemConfig(int itemIndex)
        {
            int offset = itemIndex - LegacyInventoryItemCount;
            if (offset < 0 || offset >= BlackDragonItemIds.Length)
                return null;

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            return config != null ? config.GetItem(BlackDragonItemIds[offset]) : null;
        }

        ItemConfig GetLegacyItemConfig(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= LegacyInventoryItemCount)
                return null;
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            return config != null ? config.GetItem(LegacyItemIds[itemIndex]) : null;
        }

        ItemConfig GetSpiritStoneItemConfig(int itemIndex)
        {
            int offset = itemIndex - LegacyInventoryItemCount - BlackDragonItemIds.Length;
            if (offset < 0 || offset >= SpiritStoneItemIds.Length)
                return null;
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            return config != null ? config.GetItem(SpiritStoneItemIds[offset]) : null;
        }

        ItemConfig GetWeaponItemConfig(int itemIndex)
        {
            int offset = itemIndex - LegacyInventoryItemCount - BlackDragonItemIds.Length - SpiritStoneItemIds.Length;
            if (offset < 0 || offset >= WeaponItemIds.Length)
                return null;
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            return config != null ? config.GetItem(WeaponItemIds[offset]) : null;
        }

        ItemConfig GetInventoryItemConfig(int itemIndex)
        {
            ItemConfig config = GetBlackDragonItemConfig(itemIndex);
            if (config != null)
                return config;
            config = GetLegacyItemConfig(itemIndex);
            if (config != null)
                return config;
            config = GetWeaponItemConfig(itemIndex);
            if (config != null)
                return config;
            return GetSpiritStoneItemConfig(itemIndex);
        }

        int GetInventoryItemIndex(string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
                return -1;
            for (int index = 0; index < TotalInventoryItemCount; index++)
            {
                ItemConfig itemConfig = GetInventoryItemConfig(index);
                if (itemConfig != null
                    && string.Equals(itemConfig.StableId, stableId, StringComparison.Ordinal))
                    return index;
            }
            return -1;
        }

        int GetInventoryItemFlag(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= TotalInventoryItemCount)
                return 0;
            if (itemIndex < LegacyInventoryItemCount)
            {
                ItemConfig legacyConfig = GetLegacyItemConfig(itemIndex);
                if (legacyConfig != null)
                    return legacyConfig.InventoryFlag;
                switch (itemIndex)
                {
                    case 0: return 2;
                    case 1: return 1;
                    case 2: return 1;
                    default: return 2;
                }
            }
            ItemConfig itemConfig = GetInventoryItemConfig(itemIndex);
            return itemConfig != null ? itemConfig.InventoryFlag : 0;
        }

        bool IsItemVisibleOnTab(int itemIndex, int tabIndex)
        {
            int flag = GetInventoryItemFlag(itemIndex);
            switch (tabIndex)
            {
                case 0: return flag == 0;            // Túi đồ: chỉ trang bị
                case 1: return flag == 3;            // Mảnh: chỉ mảnh
                case 2: return flag == 1 || flag == 2; // Nguyên liệu: cả 2 loại
                default: return false;
            }
        }

        string GetInventoryItemName(int itemIndex)
        {
            string[] itemNames =
            {
                "08_azure_dragon",
                "02_blue_potion",
                "03_immortal_peach",
                "06_sun_spirit_orb"
            };
            if (itemIndex >= 0 && itemIndex < itemNames.Length)
            {
                ItemConfig legacyConfig = GetLegacyItemConfig(itemIndex);
                if (legacyConfig != null
                    && !string.IsNullOrEmpty(legacyConfig.DisplayName))
                    return legacyConfig.DisplayName;
                return itemNames[itemIndex];
            }

            ItemConfig itemConfig = GetInventoryItemConfig(itemIndex);
            return itemConfig != null && !string.IsNullOrEmpty(itemConfig.DisplayName)
                ? itemConfig.DisplayName
                : string.Empty;
        }

        void OpenInventoryItemPopup(int itemIndex, bool fromStatus = false)
        {
            Sprite[] allItemSprites = GetResolvedInventoryItemSprites();
            if ((!fromStatus && !IsItemVisibleOnTab(itemIndex, selectedInventoryTab))
                || itemIndex < 0 || itemIndex >= allItemSprites.Length
                || GetSpriteAt(allItemSprites, itemIndex) == null)
                return;

            if (inventoryItemPopup != null)
                Destroy(inventoryItemPopup);

            int popupType = GetInventoryItemPopupType(itemIndex);
            Transform popupParent = fromStatus ? characterPanel.transform : inventoryPanel.transform;
            RectTransform popup = CreateFullScreenOverlay(
                $"InventoryItemPopup_{GetInventoryItemName(itemIndex)}",
                new Color32(3, 19, 16, 230), popupParent);
            inventoryItemPopup = popup.gameObject;

            RectTransform panel = CreateAnchoredRect(popup, "PopupPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(900f, 1100f);
            panel.anchoredPosition = Vector2.zero;
            inventoryPopupBackgroundImage = panel.gameObject.AddComponent<Image>();
            inventoryPopupBackgroundImage.color = new Color32(12, 27, 36, 255);
            inventoryPopupBackgroundImage.raycastTarget = true;
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = Gold;
            panelOutline.effectDistance = new Vector2(3f, -3f);

            ItemConfig itemConfig = GetBlackDragonItemConfig(itemIndex) ?? GetWeaponItemConfig(itemIndex);
            if (itemConfig != null)
            {
                ItemInstanceSaveData existingInstance = FindInventoryItemInstance(
                    itemConfig, out bool isEquipped);
                int upgradeLevel = existingInstance != null
                    ? Mathf.Max(0, existingInstance.upgradeLevel)
                    : 0;
                int qualityTier = existingInstance != null
                    ? Mathf.Clamp(existingInstance.qualityTier, 0, MaxEquipmentQualityTier)
                    : 0;
                string displayTitle = GetInventoryItemName(itemIndex);

                Color topGradientColor = new Color32(96, 165, 250, 255);
                Color bottomGradientColor = new Color32(167, 139, 250, 255);
                inventoryPopupItemName = AddLabel(panel, "PopupItemName",
                    displayTitle, 58, Color.white,
                    new Vector2(0.05f, 0.905f), new Vector2(0.95f, 0.965f),
                    TextAlignmentOptions.Center);
                inventoryPopupItemName.fontStyle = FontStyles.Bold;
                inventoryPopupItemName.textWrappingMode = TextWrappingModes.NoWrap;
                inventoryPopupItemName.enableAutoSizing = true;
                inventoryPopupItemName.fontSizeMin = 30f;
                inventoryPopupItemName.fontSizeMax = 58f;
                inventoryPopupItemName.enableVertexGradient = true;
                inventoryPopupItemName.colorGradient = new VertexGradient(
                    topGradientColor, topGradientColor, bottomGradientColor, bottomGradientColor);
                if (!inventoryPopupItemName.enableVertexGradient)
                    inventoryPopupItemName.color = topGradientColor;

                TMP_Text gradeSlotLabel = AddLabel(panel, "PopupItemGradeSlot",
                    FormatItemQualityGradeLine(itemConfig, qualityTier), 34,
                    new Color32(239, 68, 68, 255),
                    new Vector2(0.08f, 0.85f), new Vector2(0.48f, 0.88f),
                    TextAlignmentOptions.MidlineLeft);
                gradeSlotLabel.fontStyle = FontStyles.Bold;
                gradeSlotLabel.textWrappingMode = TextWrappingModes.NoWrap;
                gradeSlotLabel.enableAutoSizing = false;

                Color32 purpleBorderColor = new Color32(139, 92, 246, 255);
                RectTransform contentBoxRect = CreateAnchoredRect(panel,
                    "PopupContentBox", new Vector2(0.056f, 0.236f),
                    new Vector2(0.944f, 0.825f), Vector2.zero, Vector2.zero);
                Image contentBoxImage = contentBoxRect.gameObject.AddComponent<Image>();
                contentBoxImage.sprite = GetRoundedRectSprite(18);
                contentBoxImage.type = Image.Type.Sliced;
                contentBoxImage.color = new Color32(19, 28, 46, 255);
                contentBoxImage.raycastTarget = false;
                Outline contentBoxOutline = contentBoxRect.gameObject.AddComponent<Outline>();
                contentBoxOutline.effectColor = purpleBorderColor;
                contentBoxOutline.effectDistance = new Vector2(3f, -3f);

                RectTransform iconFrameRect = CreateAnchoredRect(panel,
                    "PopupItemIconFrame", new Vector2(0.091f, 0.644f),
                    new Vector2(0.28f, 0.798f), Vector2.zero, Vector2.zero);
                Image iconFrameImage = iconFrameRect.gameObject.AddComponent<Image>();
                iconFrameImage.sprite = GetRoundedRectSprite(14);
                iconFrameImage.type = Image.Type.Sliced;
                iconFrameImage.color = new Color32(10, 16, 30, 255);
                iconFrameImage.raycastTarget = false;
                Outline iconOutline = iconFrameRect.gameObject.AddComponent<Outline>();
                iconOutline.effectColor = purpleBorderColor;
                iconOutline.effectDistance = new Vector2(3f, -3f);
                ApplyGradeSlotStyle(iconFrameRect, iconFrameImage, iconOutline, itemConfig.Grade);

                RectTransform itemImageRect = CreateAnchoredRect(iconFrameRect,
                    "PopupItemArtwork", new Vector2(0.08f, 0.08f),
                    new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
                inventoryPopupItemImage = itemImageRect.gameObject.AddComponent<Image>();
                inventoryPopupItemImage.sprite = allItemSprites[itemIndex];
                inventoryPopupItemImage.preserveAspect = true;
                inventoryPopupItemImage.raycastTarget = false;

                if (isEquipped)
                {
                    RectTransform equippedBadgeRect = CreateAnchoredRect(iconFrameRect,
                        "PopupEquippedBadge", new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(-8f, -30f), new Vector2(36f, 8f));
                    Image equippedBadgeBg = equippedBadgeRect.gameObject.AddComponent<Image>();
                    equippedBadgeBg.sprite = GetRoundedRectSprite(9);
                    equippedBadgeBg.type = Image.Type.Sliced;
                    equippedBadgeBg.color = new Color32(15, 15, 20, 255);
                    equippedBadgeBg.raycastTarget = false;
                    Outline equippedBadgeOutline = equippedBadgeRect.gameObject.AddComponent<Outline>();
                    equippedBadgeOutline.effectColor = Gold;
                    equippedBadgeOutline.effectDistance = new Vector2(2f, -2f);
                    TMP_Text equippedBadgeText = AddLabel(equippedBadgeRect, "Label", "E",
                        26, Color.white, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
                    equippedBadgeText.fontStyle = FontStyles.Bold;
                    equippedBadgeText.enableAutoSizing = false;
                    equippedBadgeText.raycastTarget = false;
                }

                if (upgradeLevel > 0)
                {
                    RectTransform upgradeBadgeRect = CreateAnchoredRect(iconFrameRect,
                        "PopupUpgradeBadge", new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-50f, -8f), new Vector2(8f, 30f));
                    Image upgradeBadgeBg = upgradeBadgeRect.gameObject.AddComponent<Image>();
                    upgradeBadgeBg.sprite = GetRoundedRectSprite(9);
                    upgradeBadgeBg.type = Image.Type.Sliced;
                    upgradeBadgeBg.color = new Color32(15, 15, 20, 255);
                    upgradeBadgeBg.raycastTarget = false;
                    Outline upgradeBadgeOutline = upgradeBadgeRect.gameObject.AddComponent<Outline>();
                    upgradeBadgeOutline.effectColor = Gold;
                    upgradeBadgeOutline.effectDistance = new Vector2(2f, -2f);
                    TMP_Text upgradeBadgeText = AddLabel(upgradeBadgeRect, "Label",
                        $"+{upgradeLevel}", 26, Gold,
                        Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
                    upgradeBadgeText.fontStyle = FontStyles.Bold;
                    upgradeBadgeText.enableAutoSizing = false;
                    upgradeBadgeText.raycastTarget = false;
                }

                CreateEquipmentQualityStarsRow(panel, "PopupQualityStarsRow",
                    new Vector2(0.091f, 0.596f), new Vector2(0.28f, 0.630f),
                    qualityTier, null, null);

                BuildPopupStatsBlock(panel, itemConfig, upgradeLevel, qualityTier);

                inventoryPopupItemEffect = AddLabel(panel, "PopupItemEffect",
                    itemConfig.Description, 29, Color.white,
                    new Vector2(0.091f, 0.436f), new Vector2(0.909f, 0.509f),
                    TextAlignmentOptions.TopLeft);
                inventoryPopupItemEffect.textWrappingMode = TextWrappingModes.Normal;
                inventoryPopupItemEffect.enableAutoSizing = false;

                TMP_Text talentHeaderLabel = AddLabel(panel, "PopupTalentHeader",
                    "Thiên phú", 32, Color.white,
                    new Vector2(0.091f, 0.36f), new Vector2(0.909f, 0.389f),
                    TextAlignmentOptions.MidlineLeft);
                talentHeaderLabel.fontStyle = FontStyles.Bold;
                talentHeaderLabel.enableAutoSizing = false;

                Color activeMilestoneColor = new Color32(96, 165, 250, 255);
                Color inactiveMilestoneColor = new Color32(156, 163, 175, 255);
                for (int i = 0; i < EquipmentTalentMilestones.Length; i++)
                {
                    var milestone = EquipmentTalentMilestones[i];
                    Color milestoneColor = upgradeLevel >= milestone.milestoneLevel
                        ? activeMilestoneColor
                        : inactiveMilestoneColor;
                    TMP_Text milestoneLabel = AddLabel(panel,
                        $"PopupTalentMilestone_{milestone.milestoneLevel}",
                        milestone.text, 30, milestoneColor,
                        milestone.anchorMin, milestone.anchorMax,
                        TextAlignmentOptions.MidlineLeft);
                    milestoneLabel.enableAutoSizing = false;
                }

                if (Array.IndexOf(BlackDragonItemIds, itemConfig.StableId) >= 0)
                {
                    TMP_Text setDetailLabel = AddLabel(panel, "PopupItemSetDetail",
                        BlackDragonSetDetailText, 18, new Color32(190, 195, 205, 255),
                        new Vector2(0.06f, 0.207f), new Vector2(0.94f, 0.229f),
                        TextAlignmentOptions.MidlineLeft);
                    setDetailLabel.enableAutoSizing = false;
                }

                HeroProgressSaveData progress = LoadSelectedHeroProgress();
                int playerLevel = progress != null ? Math.Max(1, progress.level) : 1;
                bool meetsRequirement = itemConfig.MeetsRequirement(playerLevel);
                int minLevel = ItemConfig.GetMinLevelForGrade(itemConfig.Grade);
                string realmName = minLevel > 0 ? Realms[FindRealmIndex(minLevel)].Name : string.Empty;
                string requirementText = minLevel <= 0
                    ? "Không yêu cầu cảnh giới"
                    : meetsRequirement
                        ? $"Yêu cầu: {realmName} (Lv.{minLevel})"
                        : $"Chưa đủ điều kiện · Yêu cầu: {realmName} (Lv.{minLevel})";
                Color requirementColor = meetsRequirement
                    ? Color.white
                    : new Color32(220, 110, 110, 255);

                RectTransform requirementPillRect = CreateAnchoredRect(panel,
                    "PopupItemRequirementPill", new Vector2(0.287f, 0.147f),
                    new Vector2(0.713f, 0.202f), Vector2.zero, Vector2.zero);
                Image requirementPillImage = requirementPillRect.gameObject.AddComponent<Image>();
                requirementPillImage.sprite = GetRoundedRectSprite(30);
                requirementPillImage.type = Image.Type.Sliced;
                requirementPillImage.color = new Color32(91, 91, 110, 255);
                requirementPillImage.raycastTarget = false;

                TMP_Text requirementLabel = AddLabel(requirementPillRect, "PopupItemRequirement",
                    $"* {requirementText}", 30, requirementColor,
                    new Vector2(0.04f, 0f), new Vector2(0.96f, 1f),
                    TextAlignmentOptions.Center);
                requirementLabel.textWrappingMode = TextWrappingModes.NoWrap;
                requirementLabel.enableAutoSizing = true;
                requirementLabel.fontSizeMin = 18f;
                requirementLabel.fontSizeMax = 30f;

                RectTransform buttonsContainer = CreateAnchoredRect(panel,
                    "PopupActionButtons", new Vector2(0.056f, 0.038f),
                    new Vector2(0.944f, 0.111f), Vector2.zero, Vector2.zero);

                if (fromStatus)
                {
                    CreateEquipmentPopupActionButton(buttonsContainer, "PopupUpgradeButton", "Nâng cấp",
                        new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                        new Vector2(0f, 0f), new Vector2(-6f, 0f),
                        upgradeLevel < MaxEquipmentUpgradeLevel, false,
                        () => OpenInventoryUpgradePopup(itemIndex, itemConfig, fromStatus: true));

                    CreateEquipmentPopupActionButton(buttonsContainer, "PopupUnequipButton", "Tháo",
                        new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                        new Vector2(6f, 0f), new Vector2(0f, 0f),
                        isEquipped, false,
                        () => UnequipInventoryPopupItem(itemConfig));
                }
                else
                {
                    CreateEquipmentPopupActionButton(buttonsContainer, "PopupEquipButton", "Trang bị",
                        new Vector2(0f, 0f), new Vector2(1f / 3f, 1f),
                        new Vector2(0f, 0f), new Vector2(-8f, 0f),
                        !isEquipped && meetsRequirement, true,
                        () =>
                        {
                            EquipInventoryPopupItem(itemIndex, itemConfig);
                            OpenInventoryItemPopup(itemIndex, fromStatus: false);
                        });

                    CreateEquipmentPopupActionButton(buttonsContainer, "PopupUpgradeButton", "Nâng cấp",
                        new Vector2(1f / 3f, 0f), new Vector2(2f / 3f, 1f),
                        new Vector2(4f, 0f), new Vector2(-4f, 0f),
                        upgradeLevel < MaxEquipmentUpgradeLevel, false,
                        () => OpenInventoryUpgradePopup(itemIndex, itemConfig, fromStatus: false));

                    CreateEquipmentPopupActionButton(buttonsContainer, "PopupUnequipButton", "Tháo",
                        new Vector2(2f / 3f, 0f), new Vector2(1f, 1f),
                        new Vector2(8f, 0f), new Vector2(0f, 0f),
                        isEquipped, false,
                        () =>
                        {
                            UnequipInventoryPopupItem(itemConfig);
                            OpenInventoryItemPopup(itemIndex, fromStatus: false);
                        });
                }
            }
            else
            {
                RectTransform iconFrameRect = CreateAnchoredRect(panel,
                    "PopupItemIconFrame", new Vector2(0.06f, 0.80f),
                    new Vector2(0.26f, 0.94f), Vector2.zero, Vector2.zero);
                Image iconFrameImage = iconFrameRect.gameObject.AddComponent<Image>();
                iconFrameImage.color = new Color32(12, 27, 36, 255);
                iconFrameImage.raycastTarget = false;
                Outline iconOutline = iconFrameRect.gameObject.AddComponent<Outline>();
                iconOutline.effectColor = Gold;
                iconOutline.effectDistance = new Vector2(2f, -2f);

                RectTransform itemImageRect = CreateAnchoredRect(iconFrameRect,
                    "PopupItemArtwork", new Vector2(0.08f, 0.08f),
                    new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
                inventoryPopupItemImage = itemImageRect.gameObject.AddComponent<Image>();
                inventoryPopupItemImage.sprite = allItemSprites[itemIndex];
                inventoryPopupItemImage.preserveAspect = true;
                inventoryPopupItemImage.raycastTarget = false;

                inventoryPopupItemName = AddLabel(panel, "PopupItemName",
                    GetInventoryItemName(itemIndex), 30, Gold,
                    new Vector2(0.30f, 0.85f), new Vector2(0.94f, 0.94f),
                    TextAlignmentOptions.MidlineLeft);
                inventoryPopupItemName.textWrappingMode = TextWrappingModes.NoWrap;
                inventoryPopupItemName.enableAutoSizing = true;
                inventoryPopupItemName.fontSizeMin = 18f;
                inventoryPopupItemName.fontSizeMax = 30f;

                ItemConfig displayConfig = GetInventoryItemConfig(itemIndex);
                string effectText = displayConfig != null
                    && !string.IsNullOrEmpty(displayConfig.Description)
                    ? displayConfig.Description
                    : "Chưa có dữ liệu — chờ bổ sung";

                inventoryPopupItemEffect = AddLabel(panel, "PopupItemEffect",
                    effectText, 20, Color.white,
                    new Vector2(0.06f, 0.38f), new Vector2(0.77f, 0.77f),
                    TextAlignmentOptions.TopLeft);
                inventoryPopupItemEffect.enableAutoSizing = false;

                if (popupType == 0)
                {
                    RectTransform buttonsContainer = CreateAnchoredRect(panel,
                        "PopupActionButtons", new Vector2(0.06f, 0.06f),
                        new Vector2(0.94f, 0.15f), Vector2.zero, Vector2.zero);
                    CreatePopupActionButton(buttonsContainer, "PopupUseButton", "Sử dụng",
                        new Vector2(1f / 3f, 0f), new Vector2(2f / 3f, 1f),
                        Vector2.zero, Vector2.zero,
                        true,
                        () => LogInventoryPopupAction("Sử dụng", itemIndex));
                }

                if (GetSpiritStoneItemConfig(itemIndex) != null)
                {
                    RectTransform buttonsContainer = CreateAnchoredRect(panel,
                        "PopupCombineButtons", new Vector2(0.06f, 0.06f),
                        new Vector2(0.94f, 0.15f), Vector2.zero, Vector2.zero);
                    CreatePopupActionButton(buttonsContainer, "PopupCombineButton",
                        "Hợp thành", new Vector2(0f, 0f), new Vector2(1f, 1f),
                        Vector2.zero, Vector2.zero, true,
                        () => OpenSpiritStoneCombinePopup(itemIndex));
                }
            }

            Button closeButton = CreateCharacterHitButton(panel, "PopupCloseButton",
                0.90f, 0.91f, 0.975f, 0.975f, CloseInventoryItemPopup);
            ConfigureOutlinedButton(closeButton, new Color32(62, 45, 37, 255), Gold, 2f);
            TMP_Text closeLabel = AddLabel(panel, "PopupCloseButtonText", "X", 26, Gold,
                new Vector2(0.90f, 0.91f), new Vector2(0.975f, 0.975f), TextAlignmentOptions.Center);
            closeLabel.raycastTarget = false;
        }

        void OpenSpiritStoneCombinePopup(int itemIndex)
        {
            if (spiritStoneCombinePopup != null)
                Destroy(spiritStoneCombinePopup);
            spiritStoneCombinePopup = null;

            CloseInventoryItemPopup();

            RectTransform popup = CreateFullScreenOverlay(
                "SpiritStoneCombinePopup",
                new Color32(3, 19, 16, 230), inventoryPanel.transform);
            spiritStoneCombinePopup = popup.gameObject;

            RectTransform panel = CreateAnchoredRect(popup, "CombinePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(900f, 1350f);
            panel.anchoredPosition = Vector2.zero;
            Image panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = new Color32(12, 27, 36, 255);
            panelBg.raycastTarget = true;
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = Gold;
            panelOutline.effectDistance = new Vector2(3f, -3f);

            Button closeButton = CreateCharacterHitButton(panel, "CombineCloseButton",
                0.90f, 0.93f, 0.975f, 0.985f, CloseSpiritStoneCombinePopup);
            ConfigureOutlinedButton(closeButton, new Color32(62, 45, 37, 255), Gold, 2f);
            TMP_Text closeLabel = AddLabel(panel, "CombineCloseButtonText", "X", 26, Gold,
                new Vector2(0.90f, 0.93f), new Vector2(0.975f, 0.985f), TextAlignmentOptions.Center);
            closeLabel.raycastTarget = false;

            AddLabel(panel, "CombineTitle", "Hợp thành linh thạch",
                28, Gold, new Vector2(0.06f, 0.925f), new Vector2(0.88f, 0.985f),
                TextAlignmentOptions.Center);

            RectTransform tabContainer = CreateAnchoredRect(panel, "CombineTabContainer",
                new Vector2(0.06f, 0.845f), new Vector2(0.94f, 0.905f),
                Vector2.zero, Vector2.zero);
            spiritStoneCombineTabButton = CreatePopupActionButton(tabContainer, "CombineTabButton", "Hợp thành",
                new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(-4f, 0f), true,
                () => SetSpiritStoneCombineMode(0));
            spiritStoneDismantleTabButton = CreatePopupActionButton(tabContainer, "DismantleTabButton", "Phân giải",
                new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                new Vector2(4f, 0f), new Vector2(0f, 0f), true,
                () => SetSpiritStoneCombineMode(1));
            spiritStoneCombineTabLabel = spiritStoneCombineTabButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            spiritStoneDismantleTabLabel = spiritStoneDismantleTabButton.transform.Find("Label")?.GetComponent<TMP_Text>();

            RectTransform gradeContainer = CreateAnchoredRect(panel, "CombineGradeContainer",
                new Vector2(0.06f, 0.71f), new Vector2(0.94f, 0.83f),
                Vector2.zero, Vector2.zero);

            for (int g = 0; g < 4; g++)
            {
                float xMin = g * 0.25f;
                float xMax = (g + 1) * 0.25f;
                Vector2 offsetMin = new Vector2(g == 0 ? 0f : 4f, 0f);
                Vector2 offsetMax = new Vector2(g == 3 ? 0f : -4f, 0f);

                RectTransform btnRect = CreateAnchoredRect(gradeContainer, $"GradeButton_{g}",
                    new Vector2(xMin, 0f), new Vector2(xMax, 1f), offsetMin, offsetMax);
                Image btnImg = btnRect.gameObject.AddComponent<Image>();
                btnImg.color = new Color32(12, 27, 36, 255);
                btnImg.raycastTarget = true;

                Button btn = btnRect.gameObject.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.transition = Selectable.Transition.None;
                int capturedGrade = g;
                btn.onClick.AddListener(() => SelectSpiritStoneCombineGrade(capturedGrade));

                RectTransform iconRect = CreateAnchoredRect(btnRect, "Icon",
                    new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.92f),
                    Vector2.zero, Vector2.zero);
                Image iconImg = iconRect.gameObject.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                ItemConfig stoneConfig = GetSpiritStoneItemConfig(LegacyInventoryItemCount + BlackDragonItemIds.Length + g);
                if (stoneConfig != null)
                    iconImg.sprite = stoneConfig.RewardIcon;

                TMP_Text qtyLabel = AddLabel(btnRect, "QuantityLabel", "0",
                    18, Color.white, new Vector2(0f, 0.02f), new Vector2(1f, 0.28f),
                    TextAlignmentOptions.Center);
                qtyLabel.raycastTarget = false;
                qtyLabel.enableAutoSizing = false;

                spiritStoneCombineGradeButtons[g] = btn;
                spiritStoneCombineGradeQuantityLabels[g] = qtyLabel;
            }

            RectTransform exchangeContainer = CreateAnchoredRect(panel, "CombineExchangeContainer",
                new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.68f),
                Vector2.zero, Vector2.zero);

            RectTransform sourceFrame = CreateAnchoredRect(exchangeContainer, "SourceIconFrame",
                new Vector2(0.02f, 0.45f), new Vector2(0.36f, 0.98f),
                Vector2.zero, Vector2.zero);
            Image sourceFrameImg = sourceFrame.gameObject.AddComponent<Image>();
            sourceFrameImg.color = new Color32(12, 27, 36, 255);
            sourceFrameImg.raycastTarget = false;
            Outline sourceOutline = sourceFrame.gameObject.AddComponent<Outline>();
            sourceOutline.effectColor = Gold;
            sourceOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform sourceIconRect = CreateAnchoredRect(sourceFrame, "SourceIcon",
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f),
                Vector2.zero, Vector2.zero);
            spiritStoneCombineSourceIcon = sourceIconRect.gameObject.AddComponent<Image>();
            spiritStoneCombineSourceIcon.preserveAspect = true;
            spiritStoneCombineSourceIcon.raycastTarget = false;

            RectTransform targetFrame = CreateAnchoredRect(exchangeContainer, "TargetIconFrame",
                new Vector2(0.64f, 0.45f), new Vector2(0.98f, 0.98f),
                Vector2.zero, Vector2.zero);
            Image targetFrameImg = targetFrame.gameObject.AddComponent<Image>();
            targetFrameImg.color = new Color32(12, 27, 36, 255);
            targetFrameImg.raycastTarget = false;
            Outline targetOutline = targetFrame.gameObject.AddComponent<Outline>();
            targetOutline.effectColor = Gold;
            targetOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform targetIconRect = CreateAnchoredRect(targetFrame, "TargetIcon",
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f),
                Vector2.zero, Vector2.zero);
            spiritStoneCombineTargetIcon = targetIconRect.gameObject.AddComponent<Image>();
            spiritStoneCombineTargetIcon.preserveAspect = true;
            spiritStoneCombineTargetIcon.raycastTarget = false;

            TMP_Text arrowLabel = AddLabel(exchangeContainer, "ExchangeArrow", "→",
                48, Gold, new Vector2(0.38f, 0.55f), new Vector2(0.62f, 0.85f),
                TextAlignmentOptions.Center);
            arrowLabel.raycastTarget = false;

            spiritStoneCombineSourceName = AddLabel(exchangeContainer, "SourceName", "",
                20, Color.white, new Vector2(0.02f, 0.28f), new Vector2(0.36f, 0.42f),
                TextAlignmentOptions.Center);
            spiritStoneCombineSourceName.raycastTarget = false;

            spiritStoneCombineTargetName = AddLabel(exchangeContainer, "TargetName", "",
                20, Color.white, new Vector2(0.64f, 0.28f), new Vector2(0.98f, 0.42f),
                TextAlignmentOptions.Center);
            spiritStoneCombineTargetName.raycastTarget = false;

            spiritStoneCombineSourceQuantity = AddLabel(exchangeContainer, "SourceQuantity", "",
                20, Color.white, new Vector2(0.02f, 0.08f), new Vector2(0.36f, 0.26f),
                TextAlignmentOptions.Center);
            spiritStoneCombineSourceQuantity.raycastTarget = false;

            spiritStoneCombineTargetResult = AddLabel(exchangeContainer, "TargetResult", "",
                20, Color.white, new Vector2(0.64f, 0.08f), new Vector2(0.98f, 0.26f),
                TextAlignmentOptions.Center);
            spiritStoneCombineTargetResult.raycastTarget = false;
            spiritStoneCombineTargetResult.enableAutoSizing = true;
            spiritStoneCombineTargetResult.fontSizeMin = 12f;
            spiritStoneCombineTargetResult.fontSizeMax = 20f;

            RectTransform sliderContainer = CreateAnchoredRect(panel, "CombineSliderContainer",
                new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.40f),
                Vector2.zero, Vector2.zero);

            spiritStoneCombineQuantityLabel = AddLabel(sliderContainer, "CombineQuantityLabel", "1",
                28, Color.white, new Vector2(0.35f, 0.55f), new Vector2(0.65f, 1f),
                TextAlignmentOptions.Center);
            spiritStoneCombineQuantityLabel.raycastTarget = false;

            spiritStoneCombineMinusButton = CreatePopupActionButton(sliderContainer, "CombineMinusButton",
                "−", new Vector2(0f, 0f), new Vector2(0.12f, 0.50f),
                Vector2.zero, Vector2.zero, true,
                () => ChangeSpiritStoneCombineQuantity(-1));

            spiritStoneCombinePlusButton = CreatePopupActionButton(sliderContainer, "CombinePlusButton",
                "+", new Vector2(0.88f, 0f), new Vector2(1f, 0.50f),
                Vector2.zero, Vector2.zero, true,
                () => ChangeSpiritStoneCombineQuantity(1));

            RectTransform sliderRect = CreateAnchoredRect(sliderContainer, "CombineSlider",
                new Vector2(0.14f, 0.05f), new Vector2(0.86f, 0.45f),
                Vector2.zero, Vector2.zero);
            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.wholeNumbers = true;
            slider.direction = Slider.Direction.LeftToRight;

            RectTransform bgRect = CreateAnchoredRect(sliderRect, "Background",
                new Vector2(0f, 0.25f), new Vector2(1f, 0.75f),
                Vector2.zero, Vector2.zero);
            Image bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color32(45, 48, 54, 255);
            bgImg.raycastTarget = false;

            RectTransform fillAreaRect = CreateAnchoredRect(sliderRect, "Fill Area",
                new Vector2(0f, 0.25f), new Vector2(1f, 0.75f),
                new Vector2(5f, 0f), new Vector2(-5f, 0f));
            RectTransform fillRect = CreateAnchoredRect(fillAreaRect, "Fill",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero);
            Image fillImg = fillRect.gameObject.AddComponent<Image>();
            fillImg.color = Gold;
            fillImg.raycastTarget = false;
            slider.fillRect = fillRect;

            RectTransform handleAreaRect = CreateAnchoredRect(sliderRect, "Handle Slide Area",
                Vector2.zero, Vector2.one,
                new Vector2(10f, 0f), new Vector2(-10f, 0f));
            RectTransform handleRect = CreateAnchoredRect(handleAreaRect, "Handle",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(-10f, 0f), new Vector2(10f, 0f));
            Image handleImg = handleRect.gameObject.AddComponent<Image>();
            handleImg.color = Color.white;
            handleImg.raycastTarget = true;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            slider.onValueChanged.AddListener(value => OnSpiritStoneCombineSliderChanged(value));
            spiritStoneCombineSlider = slider;

            TMP_Text freeNote = AddLabel(panel, "CombineFreeNote", "Không tốn phí",
                18, new Color32(150, 150, 150, 255),
                new Vector2(0.06f, 0.21f), new Vector2(0.94f, 0.26f),
                TextAlignmentOptions.Center);
            freeNote.raycastTarget = false;

            RectTransform bottomButtonsContainer = CreateAnchoredRect(panel, "CombineBottomButtons",
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.18f),
                Vector2.zero, Vector2.zero);

            CreatePopupActionButton(bottomButtonsContainer, "CombineCancelButton", "Hủy",
                new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(-4f, 0f), true,
                CloseSpiritStoneCombinePopup);

            spiritStoneCombineConfirmButton = CreatePopupActionButton(bottomButtonsContainer, "CombineConfirmButton", "Hợp thành",
                new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                new Vector2(4f, 0f), new Vector2(0f, 0f), true,
                ConfirmSpiritStoneCombine);
            spiritStoneCombineConfirmLabel = spiritStoneCombineConfirmButton.transform.Find("Label")?.GetComponent<TMP_Text>();

            spiritStoneCombineMode = 0;
            spiritStoneCombineGrade = Mathf.Clamp(
                itemIndex - LegacyInventoryItemCount - BlackDragonItemIds.Length,
                0, 3);
            spiritStoneCombineQuantity = 1;

            RefreshSpiritStoneCombinePopup();
        }

        void CloseSpiritStoneCombinePopup()
        {
            if (spiritStoneCombinePopup != null)
                Destroy(spiritStoneCombinePopup);
            spiritStoneCombinePopup = null;
        }

        void SetSpiritStoneCombineMode(int mode)
        {
            if (mode == spiritStoneCombineMode)
                return;
            spiritStoneCombineMode = mode;
            if (spiritStoneCombineMode == 0 && spiritStoneCombineGrade == 3)
                spiritStoneCombineGrade = 2;
            else if (spiritStoneCombineMode == 1 && spiritStoneCombineGrade == 0)
                spiritStoneCombineGrade = 1;

            spiritStoneCombineQuantity = 1;
            RefreshSpiritStoneCombinePopup();
        }

        void SelectSpiritStoneCombineGrade(int grade)
        {
            if (spiritStoneCombineMode == 0 && grade == 3)
                return;
            if (spiritStoneCombineMode == 1 && grade == 0)
                return;

            spiritStoneCombineGrade = grade;
            spiritStoneCombineQuantity = 1;
            RefreshSpiritStoneCombinePopup();
        }

        void ChangeSpiritStoneCombineQuantity(int delta)
        {
            SaveManager saveManager = SaveManager.Instance;
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            InventoryService inventoryService = new InventoryService(
                saveManager, config);
            int owned = inventoryService.GetQuantity(SpiritStoneItemIds[spiritStoneCombineGrade]);
            int maxOperations = (spiritStoneCombineMode == 0) ? owned / 100 : owned;
            if (maxOperations <= 0)
                return;

            spiritStoneCombineQuantity = Mathf.Clamp(
                spiritStoneCombineQuantity + delta, 1, maxOperations);
            RefreshSpiritStoneCombinePopup();
        }

        void OnSpiritStoneCombineSliderChanged(float value)
        {
            spiritStoneCombineQuantity = Mathf.Max(0, (int)value);
            RefreshSpiritStoneCombinePopup();
        }

        void RefreshSpiritStoneCombinePopup()
        {
            if (spiritStoneCombinePopup == null)
                return;

            SaveManager saveManager = SaveManager.Instance;
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            InventoryService inventoryService = new InventoryService(
                saveManager, config);

            int[] owned = new int[4];
            for (int g = 0; g < 4; g++)
                owned[g] = inventoryService.GetQuantity(SpiritStoneItemIds[g]);

            int mode = spiritStoneCombineMode;
            int grade = spiritStoneCombineGrade;
            int targetGrade = (mode == 0) ? grade + 1 : grade - 1;
            bool canExchange = (mode == 0) ? (grade >= 0 && grade <= 2) : (grade >= 1 && grade <= 3);

            int maxOperations = 0;
            if (canExchange)
                maxOperations = (mode == 0) ? owned[grade] / 100 : owned[grade];

            if (maxOperations <= 0)
                spiritStoneCombineQuantity = 0;
            else
                spiritStoneCombineQuantity = Mathf.Clamp(spiritStoneCombineQuantity, 1, maxOperations);

            int required = (mode == 0) ? 100 * spiritStoneCombineQuantity : spiritStoneCombineQuantity;
            int result = (mode == 0) ? spiritStoneCombineQuantity : 100 * spiritStoneCombineQuantity;

            Color32 activeTabFill = new Color32(12, 27, 36, 255);
            Color32 inactiveTabFill = new Color32(45, 48, 54, 245);
            Color32 inactiveTabBorder = new Color32(110, 115, 125, 255);
            Color inactiveTabText = new Color32(150, 150, 150, 255);

            if (mode == 0)
            {
                ConfigureOutlinedButton(spiritStoneCombineTabButton, activeTabFill, Gold, 2f);
                if (spiritStoneCombineTabLabel != null) spiritStoneCombineTabLabel.color = Color.white;
                ConfigureOutlinedButton(spiritStoneDismantleTabButton, inactiveTabFill, inactiveTabBorder, 2f);
                if (spiritStoneDismantleTabLabel != null) spiritStoneDismantleTabLabel.color = inactiveTabText;
            }
            else
            {
                ConfigureOutlinedButton(spiritStoneCombineTabButton, inactiveTabFill, inactiveTabBorder, 2f);
                if (spiritStoneCombineTabLabel != null) spiritStoneCombineTabLabel.color = inactiveTabText;
                ConfigureOutlinedButton(spiritStoneDismantleTabButton, activeTabFill, Gold, 2f);
                if (spiritStoneDismantleTabLabel != null) spiritStoneDismantleTabLabel.color = Color.white;
            }

            for (int g = 0; g < 4; g++)
            {
                Button btn = spiritStoneCombineGradeButtons[g];
                if (btn == null)
                    continue;

                TMP_Text lbl = spiritStoneCombineGradeQuantityLabels[g];
                if (lbl != null)
                    lbl.text = owned[g].ToString();

                Outline outline = btn.GetComponent<Outline>();
                if (outline == null)
                    outline = btn.gameObject.AddComponent<Outline>();

                bool disabledInMode = (mode == 0 && g == 3) || (mode == 1 && g == 0);
                Image btnImg = btn.GetComponent<Image>();

                if (disabledInMode)
                {
                    btn.interactable = false;
                    if (btnImg != null) btnImg.color = new Color32(45, 48, 54, 255);
                    if (lbl != null) lbl.color = new Color32(150, 150, 150, 255);
                    outline.effectColor = new Color32(110, 115, 125, 255);
                    outline.effectDistance = new Vector2(2f, -2f);
                }
                else
                {
                    btn.interactable = true;
                    if (btnImg != null) btnImg.color = new Color32(12, 27, 36, 255);
                    if (lbl != null) lbl.color = Color.white;

                    if (g == grade)
                    {
                        outline.effectColor = Gold;
                        outline.effectDistance = new Vector2(3f, -3f);
                    }
                    else
                    {
                        outline.effectColor = Gold;
                        outline.effectDistance = new Vector2(2f, -2f);
                    }
                }
            }

            int sourceIndex = LegacyInventoryItemCount + BlackDragonItemIds.Length + grade;
            ItemConfig sourceConfig = GetSpiritStoneItemConfig(sourceIndex);

            if (canExchange)
            {
                int targetIndex = LegacyInventoryItemCount + BlackDragonItemIds.Length + targetGrade;
                ItemConfig targetConfig = GetSpiritStoneItemConfig(targetIndex);
                if (sourceConfig == null || targetConfig == null)
                {
                    Debug.LogError($"[MainProgress] Missing SpiritStoneItemConfig for grade {grade} or {targetGrade}.", this);
                    return;
                }

                if (spiritStoneCombineSourceIcon != null)
                    spiritStoneCombineSourceIcon.sprite = sourceConfig.RewardIcon;
                if (spiritStoneCombineTargetIcon != null)
                    spiritStoneCombineTargetIcon.sprite = targetConfig.RewardIcon;
                if (spiritStoneCombineSourceName != null)
                    spiritStoneCombineSourceName.text = sourceConfig.DisplayName;
                if (spiritStoneCombineTargetName != null)
                    spiritStoneCombineTargetName.text = targetConfig.DisplayName;

                int displayRequired = (spiritStoneCombineQuantity == 0) ? ((mode == 0) ? 100 : 1) : required;
                if (spiritStoneCombineSourceQuantity != null)
                {
                    spiritStoneCombineSourceQuantity.text = $"{owned[grade]} / {displayRequired}";
                    spiritStoneCombineSourceQuantity.color = (owned[grade] >= displayRequired && spiritStoneCombineQuantity > 0)
                        ? new Color32(0, 200, 0, 255)
                        : new Color32(255, 60, 60, 255);
                }

                if (spiritStoneCombineTargetResult != null)
                {
                    spiritStoneCombineTargetResult.text = $"nhận {result}";
                    spiritStoneCombineTargetResult.color = Color.white;
                }
            }
            else
            {
                if (spiritStoneCombineSourceIcon != null && sourceConfig != null)
                    spiritStoneCombineSourceIcon.sprite = sourceConfig.RewardIcon;
                if (spiritStoneCombineTargetIcon != null)
                    spiritStoneCombineTargetIcon.sprite = null;
                if (spiritStoneCombineSourceName != null && sourceConfig != null)
                    spiritStoneCombineSourceName.text = sourceConfig.DisplayName;
                if (spiritStoneCombineTargetName != null)
                    spiritStoneCombineTargetName.text = string.Empty;

                if (spiritStoneCombineSourceQuantity != null)
                {
                    spiritStoneCombineSourceQuantity.text = $"{owned[grade]} / —";
                    spiritStoneCombineSourceQuantity.color = new Color32(150, 150, 150, 255);
                }

                if (spiritStoneCombineTargetResult != null)
                {
                    spiritStoneCombineTargetResult.text = (mode == 0)
                        ? "Cực phẩm là cấp cao nhất — không thể hợp thành thêm"
                        : "Hạ phẩm là cấp thấp nhất — không thể phân giải";
                    spiritStoneCombineTargetResult.color = new Color32(255, 60, 60, 255);
                }
            }

            if (spiritStoneCombineQuantityLabel != null)
                spiritStoneCombineQuantityLabel.text = spiritStoneCombineQuantity.ToString();

            if (spiritStoneCombineSlider != null)
            {
                spiritStoneCombineSlider.minValue = maxOperations > 0 ? 1 : 0;
                spiritStoneCombineSlider.maxValue = maxOperations;
                spiritStoneCombineSlider.SetValueWithoutNotify(spiritStoneCombineQuantity);
                spiritStoneCombineSlider.interactable = maxOperations > 0;
            }

            if (spiritStoneCombineMinusButton != null)
                spiritStoneCombineMinusButton.interactable = spiritStoneCombineQuantity > 1;
            if (spiritStoneCombinePlusButton != null)
                spiritStoneCombinePlusButton.interactable = spiritStoneCombineQuantity < maxOperations;

            if (spiritStoneCombineConfirmLabel != null)
                spiritStoneCombineConfirmLabel.text = (mode == 0) ? "Hợp thành" : "Phân giải";

            bool canConfirm = canExchange && maxOperations >= 1 && spiritStoneCombineQuantity >= 1
                && owned[grade] >= required;
            if (spiritStoneCombineConfirmButton != null)
            {
                spiritStoneCombineConfirmButton.interactable = canConfirm;
                Color32 confirmFill = canConfirm
                    ? new Color32(12, 27, 36, 255)
                    : new Color32(45, 48, 54, 245);
                Color32 confirmBorder = canConfirm
                    ? (Color32)Gold
                    : new Color32(110, 115, 125, 255);
                ConfigureOutlinedButton(spiritStoneCombineConfirmButton, confirmFill, confirmBorder, 2f);
                if (spiritStoneCombineConfirmLabel != null)
                    spiritStoneCombineConfirmLabel.color = canConfirm ? Color.white : new Color32(150, 150, 150, 255);
            }
        }

        void ConfirmSpiritStoneCombine()
        {
            int mode = spiritStoneCombineMode;
            int grade = spiritStoneCombineGrade;
            int targetGrade = (mode == 0) ? grade + 1 : grade - 1;
            bool canExchange = (mode == 0) ? (grade >= 0 && grade <= 2) : (grade >= 1 && grade <= 3);
            if (!canExchange || targetGrade < 0 || targetGrade > 3)
            {
                Debug.LogError($"[MainProgress] Invalid combine/dismantle target grade: mode={mode}, grade={grade}, targetGrade={targetGrade}.", this);
                return;
            }

            SaveManager saveManager = SaveManager.Instance;
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            InventoryService inventoryService = new InventoryService(
                saveManager, config);

            int owned = inventoryService.GetQuantity(SpiritStoneItemIds[grade]);
            int maxOperations = (mode == 0) ? owned / 100 : owned;
            int quantity = spiritStoneCombineQuantity;
            int required = (mode == 0) ? 100 * quantity : quantity;
            int result = (mode == 0) ? quantity : 100 * quantity;

            if (quantity < 1 || maxOperations < 1 || owned < required)
            {
                Debug.LogError($"[MainProgress] Cannot combine: quantity={quantity}, maxOperations={maxOperations}, owned={owned}, required={required}.", this);
                return;
            }

            string sourceId = SpiritStoneItemIds[grade];
            string targetId = SpiritStoneItemIds[targetGrade];

            bool removed = inventoryService.RemoveStackItem(sourceId, required);
            if (!removed)
            {
                Debug.LogError($"[MainProgress] RemoveStackItem failed for sourceId '{sourceId}' with quantity {required}.", this);
                return;
            }

            bool added = inventoryService.AddStackItem(targetId, result);
            if (!added)
            {
                Debug.LogError($"[MainProgress] AddStackItem failed for targetId '{targetId}' with quantity {result}. Source items were already removed!", this);
            }

            RefreshSpiritStoneCombinePopup();
        }

        static void BuildPopupStatsBlock(Transform panel, ItemConfig itemConfig, int upgradeLevel, int qualityTier = 0)
        {
            RectTransform statsBlock = CreateAnchoredRect(panel, "PopupStatsBlock",
                new Vector2(0.313f, 0.527f), new Vector2(0.909f, 0.798f),
                Vector2.zero, Vector2.zero);

            ItemStatValues stats = ItemStatCalculator.Calculate(itemConfig, upgradeLevel, qualityTier);
            List<(string label, string value)> rows = new List<(string label, string value)>();
            if (stats.AttackPercentBonus > 0f)
                rows.Add(("ATK", $"+{stats.AttackPercentBonus.ToString("0.##", CultureInfo.InvariantCulture)}%"));
            if (stats.MaxHealthPercentBonus > 0f)
                rows.Add(("HP tối đa", $"+{stats.MaxHealthPercentBonus.ToString("0.##", CultureInfo.InvariantCulture)}%"));
            if (stats.CritRatePercentBonus > 0f)
                rows.Add(("Tỷ lệ Crit", $"+{stats.CritRatePercentBonus.ToString("0.##", CultureInfo.InvariantCulture)}%"));
            if (stats.CritDamagePercentBonus > 0f)
                rows.Add(("ST Bạo", $"+{stats.CritDamagePercentBonus.ToString("0.##", CultureInfo.InvariantCulture)}%"));
            if (stats.SpeedBonus > 0)
                rows.Add(("Tốc độ", $"+{stats.SpeedBonus.ToString(CultureInfo.InvariantCulture)}"));
            if (stats.DamagePercentBonus > 0f)
                rows.Add(("Tăng ST", $"+{stats.DamagePercentBonus.ToString("0.##", CultureInfo.InvariantCulture)}%"));
            if (stats.RagePerGemBonus > 0)
                rows.Add(("Nộ", $"+{stats.RagePerGemBonus.ToString(CultureInfo.InvariantCulture)} Nộ / viên Mặt Trời"));
            if (stats.DefensePercentBonus > 0f)
                rows.Add(("Giáp", $"+{stats.DefensePercentBonus.ToString("0.##", CultureInfo.InvariantCulture)}%"));

            const float rowHeight = 48f;
            for (int index = 0; index < rows.Count; index++)
            {
                RectTransform rowRect = CreateAnchoredRect(statsBlock,
                    $"PopupStatRow_{index}", new Vector2(0f, 1f), new Vector2(1f, 1f),
                    Vector2.zero, Vector2.zero);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(0f, rowHeight);
                rowRect.anchoredPosition = new Vector2(0f, -index * rowHeight);

                TMP_Text leftLabel = AddLabel(rowRect, "StatLabel", rows[index].label,
                    30, Color.white, new Vector2(0f, 0f), new Vector2(0.65f, 1f),
                    TextAlignmentOptions.MidlineLeft);
                leftLabel.enableAutoSizing = false;

                TMP_Text rightValue = AddLabel(rowRect, "StatValue", rows[index].value,
                    30, Gold, new Vector2(0.35f, 0f), new Vector2(1f, 1f),
                    TextAlignmentOptions.MidlineRight);
                rightValue.enableAutoSizing = false;
            }
        }

        static Button CreatePopupActionButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool enabled, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateAnchoredRect(parent, name,
                anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.interactable = enabled;
            if (enabled && onClick != null)
                button.onClick.AddListener(onClick);

            Color32 fillColor = enabled
                ? new Color32(12, 27, 36, 255)
                : new Color32(45, 48, 54, 245);
            Color32 borderColor = enabled
                ? (Color32)Gold
                : new Color32(110, 115, 125, 255);
            Color textColor = enabled
                ? Color.white
                : new Color32(150, 150, 150, 255);

            ConfigureOutlinedButton(button, fillColor, borderColor, 2f);
            TMP_Text buttonLabel = AddLabel(rect, "Label", label, 24, textColor,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            buttonLabel.enableAutoSizing = false;
            buttonLabel.raycastTarget = false;
            return button;
        }

        static Button CreateEquipmentPopupActionButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool enabled, bool isPrimaryBlue, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateAnchoredRect(parent, name,
                anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetRoundedRectSprite(14);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.interactable = enabled;
            if (enabled && onClick != null)
                button.onClick.AddListener(onClick);

            Color32 fillColor;
            Color textColor;
            if (enabled)
            {
                fillColor = isPrimaryBlue
                    ? new Color32(37, 99, 235, 255)
                    : new Color32(243, 244, 246, 255);
                textColor = isPrimaryBlue
                    ? Color.white
                    : (Color)new Color32(17, 24, 39, 255);
            }
            else
            {
                fillColor = new Color32(107, 114, 128, 255);
                textColor = new Color32(209, 213, 219, 255);
            }

            image.color = fillColor;
            TMP_Text buttonLabel = AddLabel(rect, "Label", label, 32, textColor,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.enableAutoSizing = false;
            buttonLabel.raycastTarget = false;
            return button;
        }

        static string GetEquipmentQualityDisplayName(int qualityTier)
        {
            switch (Mathf.Clamp(qualityTier, 0, MaxEquipmentQualityTier))
            {
                case 1:
                    return "Trung phẩm";
                case 2:
                    return "Thượng phẩm";
                case 3:
                    return "Cực phẩm";
                default:
                    return "Hạ phẩm";
            }
        }

        static string FormatItemQualityGradeLine(ItemConfig itemConfig, int qualityTier)
        {
            string qualityName = GetEquipmentQualityDisplayName(qualityTier);
            string gradeName = ItemConfig.GetGradeDisplayName(itemConfig.Grade);
            return $"{qualityName} - {gradeName}";
        }

        static string GetEquipmentSlotDisplayName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return "Vũ khí";
                case EquipmentSlot.Head:
                    return "Mũ";
                case EquipmentSlot.Body:
                    return "Giáp";
                case EquipmentSlot.Gloves:
                    return "Găng";
                case EquipmentSlot.Feet:
                    return "Giầy";
                case EquipmentSlot.Accessory:
                    return "Nhẫn";
                default:
                    return string.Empty;
            }
        }

        static string FormatItemGradeAndSlotLine(ItemConfig itemConfig)
        {
            return FormatItemQualityGradeLine(itemConfig, 0);
        }

        static RectTransform CreateEquipmentQualityStarsRow(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, int qualityTier,
            Image[] outStarImages, Outline[] outStarOutlines,
            float starSize = 30f, float starSpacing = 40f)
        {
            RectTransform rowRect = CreateAnchoredRect(parent, name,
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Sprite starSprite = GetStarPolygonSprite();

            for (int i = 0; i < 4; i++)
            {
                RectTransform starRect = CreateAnchoredRect(rowRect,
                    $"QualityStar_{i}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                starRect.sizeDelta = new Vector2(starSize, starSize);
                starRect.anchoredPosition = new Vector2((i - 1.5f) * starSpacing, 0f);

                Image starImage = starRect.gameObject.AddComponent<Image>();
                starImage.sprite = starSprite;
                starImage.preserveAspect = true;
                starImage.raycastTarget = false;

                Outline starOutline = starRect.gameObject.AddComponent<Outline>();
                starOutline.effectColor = new Color32(120, 125, 135, 255);
                starOutline.effectDistance = new Vector2(2f, -2f);

                if (outStarImages != null && i < outStarImages.Length)
                    outStarImages[i] = starImage;
                if (outStarOutlines != null && i < outStarOutlines.Length)
                    outStarOutlines[i] = starOutline;

                ApplyQualityStarVisual(starImage, starOutline, i < Mathf.Clamp(qualityTier, 0, MaxEquipmentQualityTier) + 1);
            }

            return rowRect;
        }

        static void ApplyQualityStarVisual(Image starImage, Outline starOutline, bool isLit)
        {
            if (starImage != null)
            {
                starImage.color = isLit
                    ? new Color32(255, 200, 60, 255)
                    : new Color32(35, 40, 55, 255);
            }

            if (starOutline != null)
                starOutline.enabled = !isLit;
        }

        static void ApplyGradeSlotStyle(
            RectTransform frameRect,
            Image frameImage,
            Outline frameOutline,
            EquipmentGrade grade)
        {
            if (frameRect == null || frameImage == null)
                return;

            if (!ItemConfig.IsRainbowGrade(grade))
            {
                frameImage.color = ItemConfig.GetGradeSlotBackgroundColor(grade);
                if (frameOutline != null)
                {
                    frameOutline.enabled = true;
                    frameOutline.effectColor = ItemConfig.GetGradeSlotBorderColor(grade);
                }
                return;
            }

            frameImage.sprite = GetRainbowBorderSprite();
            frameImage.type = Image.Type.Simple;
            frameImage.color = Color.white;
            if (frameOutline != null)
                frameOutline.enabled = false;

            // Dung lai SlotBackground neu da ton tai de tranh tao trung khi doi tab nhieu lan
            Transform existingBg = frameRect.Find("SlotBackground");
            if (existingBg != null)
            {
                Image existingBgImage = existingBg.GetComponent<Image>();
                if (existingBgImage != null)
                    existingBgImage.color = ItemConfig.GetGradeSlotBackgroundColor(grade);
                return;
            }

            RectTransform bgRect = CreateAnchoredRect(frameRect, "SlotBackground",
                Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            bgRect.SetSiblingIndex(0);
            Image bgImage = bgRect.gameObject.AddComponent<Image>();
            bgImage.color = ItemConfig.GetGradeSlotBackgroundColor(grade);
            bgImage.raycastTarget = false;
        }

        static Sprite GetRainbowBorderSprite()
        {
            if (rainbowBorderSprite != null)
                return rainbowBorderSprite;

            const int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            for (int x = 0; x < size; x++)
            {
                float hue = x / (float)(size - 1);
                Color32 col = Color.HSVToRGB(hue, 0.9f, 1.0f);
                for (int y = 0; y < size; y++)
                    pixels[y * size + x] = col;
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            rainbowBorderSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f));
            return rainbowBorderSprite;
        }

        static Sprite GetRoundedRectSprite(int radius)
        {
            int clampedRadius = Mathf.Max(2, radius);
            if (roundedRectSprites.TryGetValue(clampedRadius, out Sprite cached) && cached != null)
                return cached;

            int size = clampedRadius * 2 + 8;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            float r = clampedRadius;
            float minInner = r;
            float maxInner = size - r;
            for (int y = 0; y < size; y++)
            {
                float py = y + 0.5f;
                float dy = py < minInner ? (minInner - py) : (py > maxInner ? (py - maxInner) : 0f);
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float dx = px < minInner ? (minInner - px) : (px > maxInner ? (px - maxInner) : 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(r + 0.5f - dist);
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            float border = clampedRadius + 2f;
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            roundedRectSprites[clampedRadius] = sprite;
            return sprite;
        }

        static Sprite GetChamferedPlaqueSprite()
        {
            if (chamferedPlaqueSprite != null)
                return chamferedPlaqueSprite;

            const int size = 48;
            const float chamfer = 12f;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float py = y + 0.5f;
                float cy = Mathf.Min(py, size - py);
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float cx = Mathf.Min(px, size - px);
                    float alpha = Mathf.Clamp01((cx + cy - chamfer) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            const float border = 14f;
            chamferedPlaqueSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            return chamferedPlaqueSprite;
        }

        static Sprite GetBevelTriangleSprite(bool pointLeft)
        {
            if (pointLeft && bevelTriangleLeftSprite != null)
                return bevelTriangleLeftSprite;
            if (!pointLeft && bevelTriangleRightSprite != null)
                return bevelTriangleRightSprite;

            const int width = 24;
            const int height = 48;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[width * height];
            float halfH = height * 0.5f;
            for (int y = 0; y < height; y++)
            {
                float py = y + 0.5f;
                float normY = Mathf.Abs(py - halfH) / halfH;
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float edgeX = pointLeft ? normY * width : (1f - normY) * width;
                    float dist = pointLeft ? (px - edgeX) : (edgeX - px);
                    float alpha = Mathf.Clamp01(dist + 0.5f);
                    pixels[y * width + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f);
            if (pointLeft)
                bevelTriangleLeftSprite = sprite;
            else
                bevelTriangleRightSprite = sprite;
            return sprite;
        }

        static Sprite GetStarPolygonSprite()
        {
            if (starPolygonSprite != null)
                return starPolygonSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            const float outerRadius = 28f;
            const float innerRadius = 11f;
            Vector2[] vertices = new Vector2[10];
            for (int k = 0; k < 10; k++)
            {
                float angle = Mathf.PI * 0.5f + k * (Mathf.PI / 5f);
                float radius = (k % 2 == 0) ? outerRadius : innerRadius;
                vertices[k] = new Vector2(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius);
            }

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int oy = 0; oy < 4; oy++)
                    {
                        float sy = y + (oy + 0.5f) * 0.25f;
                        for (int ox = 0; ox < 4; ox++)
                        {
                            float sx = x + (ox + 0.5f) * 0.25f;
                            if (IsPointInPolygon(new Vector2(sx, sy), vertices))
                                hits++;
                        }
                    }

                    byte a = (byte)Mathf.RoundToInt((hits / 16f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            starPolygonSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return starPolygonSprite;
        }

        static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int count = polygon.Length;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];
                if (((pi.y > point.y) != (pj.y > point.y))
                    && (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        void OpenInventoryUpgradePopup(int itemIndex, ItemConfig itemConfig, bool fromStatus = false)
        {
            if (itemConfig == null)
                return;

            if (inventoryUpgradePopup != null)
                Destroy(inventoryUpgradePopup);
            inventoryUpgradePopup = null;

            inventoryUpgradeItemIndex = itemIndex;
            inventoryUpgradeItemConfig = itemConfig;
            inventoryUpgradeFromStatus = fromStatus;
            inventoryUpgradeTabMode = 0;

            Transform popupParent = fromStatus ? characterPanel.transform : inventoryPanel.transform;
            RectTransform popup = CreateFullScreenOverlay(
                "InventoryUpgradePopup",
                new Color32(3, 19, 16, 230), popupParent);
            inventoryUpgradePopup = popup.gameObject;

            RectTransform panel = CreateAnchoredRect(popup, "UpgradePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(900f, 700f);
            panel.anchoredPosition = Vector2.zero;
            Image panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = new Color32(12, 27, 36, 255);
            panelBg.raycastTarget = true;
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = Gold;
            panelOutline.effectDistance = new Vector2(3f, -3f);

            Button closeButton = CreateCharacterHitButton(panel, "UpgradeCloseButton",
                0.90f, 0.89f, 0.975f, 0.975f, CloseInventoryUpgradePopup);
            ConfigureOutlinedButton(closeButton, new Color32(62, 45, 37, 255), Gold, 2f);
            TMP_Text closeLabel = AddLabel(panel, "UpgradeCloseButtonText", "X", 26, Gold,
                new Vector2(0.90f, 0.89f), new Vector2(0.975f, 0.975f), TextAlignmentOptions.Center);
            closeLabel.raycastTarget = false;

            TMP_Text titleLabel = AddLabel(panel, "UpgradeTitle",
                GetInventoryItemName(itemIndex), 30, Gold,
                new Vector2(0.06f, 0.88f), new Vector2(0.88f, 0.97f),
                TextAlignmentOptions.Center);
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 18f;
            titleLabel.fontSizeMax = 30f;

            RectTransform tabContainer = CreateAnchoredRect(panel, "UpgradeTabContainer",
                new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.84f),
                Vector2.zero, Vector2.zero);
            inventoryUpgradeEnhanceTabButton = CreatePopupActionButton(
                tabContainer, "EnhanceTabButton", "Cường hoá",
                new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(-4f, 0f), true,
                () => SelectInventoryUpgradeTab(0));
            inventoryUpgradeQualityTabButton = CreatePopupActionButton(
                tabContainer, "QualityTabButton", "Nâng phẩm",
                new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                new Vector2(4f, 0f), new Vector2(0f, 0f), true,
                () => SelectInventoryUpgradeTab(1));
            inventoryUpgradeEnhanceTabLabel = inventoryUpgradeEnhanceTabButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            inventoryUpgradeQualityTabLabel = inventoryUpgradeQualityTabButton.transform.Find("Label")?.GetComponent<TMP_Text>();

            RectTransform enhanceContentRect = CreateAnchoredRect(panel, "EnhanceTabContent",
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.70f),
                Vector2.zero, Vector2.zero);
            inventoryUpgradeEnhanceContent = enhanceContentRect.gameObject;

            inventoryUpgradeLevelLabel = AddLabel(enhanceContentRect, "EnhanceLevelLabel",
                "Cấp +0 / +15", 34, Color.white,
                new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.78f),
                TextAlignmentOptions.Center);
            inventoryUpgradeLevelLabel.fontStyle = FontStyles.Bold;
            inventoryUpgradeLevelLabel.enableAutoSizing = false;

            TMP_Text enhanceNoteLabel = AddLabel(enhanceContentRect, "EnhanceNoteLabel",
                "Mỗi cấp cường hoá: tất cả chỉ số +10%", 26, new Color32(190, 195, 205, 255),
                new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.50f),
                TextAlignmentOptions.Center);
            enhanceNoteLabel.enableAutoSizing = false;

            inventoryUpgradeEnhanceActionButton = CreatePopupActionButton(
                enhanceContentRect, "EnhanceConfirmButton", "Nâng cấp",
                new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.24f),
                Vector2.zero, Vector2.zero, true,
                () => UpgradeInventoryPopupItem(
                    inventoryUpgradeItemIndex,
                    inventoryUpgradeItemConfig,
                    inventoryUpgradeFromStatus));
            inventoryUpgradeEnhanceActionLabel = inventoryUpgradeEnhanceActionButton.transform.Find("Label")?.GetComponent<TMP_Text>();

            RectTransform qualityContentRect = CreateAnchoredRect(panel, "QualityTabContent",
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.70f),
                Vector2.zero, Vector2.zero);
            inventoryUpgradeQualityContent = qualityContentRect.gameObject;

            inventoryUpgradeQualityLabel = AddLabel(qualityContentRect, "QualityCurrentLabel",
                "Hạ phẩm", 34, new Color32(239, 68, 68, 255),
                new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.92f),
                TextAlignmentOptions.Center);
            inventoryUpgradeQualityLabel.fontStyle = FontStyles.Bold;
            inventoryUpgradeQualityLabel.enableAutoSizing = false;

            CreateEquipmentQualityStarsRow(qualityContentRect, "UpgradeQualityStarsRow",
                new Vector2(0.25f, 0.50f), new Vector2(0.75f, 0.68f),
                0, inventoryUpgradeQualityStarImages, inventoryUpgradeQualityStarOutlines,
                36f, 48f);

            TMP_Text qualityNoteLabel = AddLabel(qualityContentRect, "QualityNoteLabel",
                "Mỗi lần nâng phẩm thành công: tất cả chỉ số +10%", 26,
                new Color32(190, 195, 205, 255),
                new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.46f),
                TextAlignmentOptions.Center);
            qualityNoteLabel.enableAutoSizing = false;

            inventoryUpgradeQualityActionButton = CreatePopupActionButton(
                qualityContentRect, "QualityConfirmButton", "Nâng phẩm",
                new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.24f),
                Vector2.zero, Vector2.zero, true,
                () => UpgradeQualityInventoryPopupItem(
                    inventoryUpgradeItemIndex,
                    inventoryUpgradeItemConfig,
                    inventoryUpgradeFromStatus));
            inventoryUpgradeQualityActionLabel = inventoryUpgradeQualityActionButton.transform.Find("Label")?.GetComponent<TMP_Text>();

            RefreshInventoryUpgradePopup();
        }

        void CloseInventoryUpgradePopup()
        {
            if (inventoryUpgradePopup != null)
                Destroy(inventoryUpgradePopup);
            inventoryUpgradePopup = null;

            if (inventoryUpgradeItemConfig != null)
                OpenInventoryItemPopup(inventoryUpgradeItemIndex, inventoryUpgradeFromStatus);
        }

        void SelectInventoryUpgradeTab(int tabMode)
        {
            int clampedMode = Mathf.Clamp(tabMode, 0, 1);
            if (inventoryUpgradeTabMode == clampedMode)
                return;
            inventoryUpgradeTabMode = clampedMode;
            RefreshInventoryUpgradePopup();
        }

        void RefreshInventoryUpgradePopup()
        {
            if (inventoryUpgradePopup == null || inventoryUpgradeItemConfig == null)
                return;

            ItemInstanceSaveData existingInstance = FindInventoryItemInstance(
                inventoryUpgradeItemConfig, out _);
            int upgradeLevel = existingInstance != null
                ? Mathf.Clamp(existingInstance.upgradeLevel, 0, MaxEquipmentUpgradeLevel)
                : 0;
            int qualityTier = existingInstance != null
                ? Mathf.Clamp(existingInstance.qualityTier, 0, MaxEquipmentQualityTier)
                : 0;

            Color32 activeTabFill = new Color32(28, 52, 68, 255);
            Color32 inactiveTabFill = new Color32(45, 48, 54, 245);
            Color32 inactiveTabBorder = new Color32(110, 115, 125, 255);
            Color inactiveTabText = new Color32(150, 150, 150, 255);

            if (inventoryUpgradeTabMode == 0)
            {
                ConfigureOutlinedButton(inventoryUpgradeEnhanceTabButton, activeTabFill, Gold, 2.5f);
                if (inventoryUpgradeEnhanceTabLabel != null)
                    inventoryUpgradeEnhanceTabLabel.color = Color.white;
                ConfigureOutlinedButton(inventoryUpgradeQualityTabButton, inactiveTabFill, inactiveTabBorder, 2f);
                if (inventoryUpgradeQualityTabLabel != null)
                    inventoryUpgradeQualityTabLabel.color = inactiveTabText;
            }
            else
            {
                ConfigureOutlinedButton(inventoryUpgradeEnhanceTabButton, inactiveTabFill, inactiveTabBorder, 2f);
                if (inventoryUpgradeEnhanceTabLabel != null)
                    inventoryUpgradeEnhanceTabLabel.color = inactiveTabText;
                ConfigureOutlinedButton(inventoryUpgradeQualityTabButton, activeTabFill, Gold, 2.5f);
                if (inventoryUpgradeQualityTabLabel != null)
                    inventoryUpgradeQualityTabLabel.color = Color.white;
            }

            if (inventoryUpgradeEnhanceContent != null)
                inventoryUpgradeEnhanceContent.SetActive(inventoryUpgradeTabMode == 0);
            if (inventoryUpgradeQualityContent != null)
                inventoryUpgradeQualityContent.SetActive(inventoryUpgradeTabMode == 1);

            if (inventoryUpgradeLevelLabel != null)
                inventoryUpgradeLevelLabel.text = $"Cấp +{upgradeLevel} / +{MaxEquipmentUpgradeLevel}";

            bool canEnhance = upgradeLevel < MaxEquipmentUpgradeLevel;
            if (inventoryUpgradeEnhanceActionButton != null)
            {
                inventoryUpgradeEnhanceActionButton.interactable = canEnhance;
                Color32 fill = canEnhance ? new Color32(12, 27, 36, 255) : inactiveTabFill;
                Color32 border = canEnhance ? (Color32)Gold : inactiveTabBorder;
                ConfigureOutlinedButton(inventoryUpgradeEnhanceActionButton, fill, border, 2f);
                if (inventoryUpgradeEnhanceActionLabel != null)
                    inventoryUpgradeEnhanceActionLabel.color = canEnhance ? Color.white : inactiveTabText;
            }

            if (inventoryUpgradeQualityLabel != null)
                inventoryUpgradeQualityLabel.text = GetEquipmentQualityDisplayName(qualityTier);

            int litStars = qualityTier + 1;
            for (int i = 0; i < 4; i++)
            {
                ApplyQualityStarVisual(
                    inventoryUpgradeQualityStarImages[i],
                    inventoryUpgradeQualityStarOutlines[i],
                    i < litStars);
            }

            bool canUpgradeQuality = qualityTier < MaxEquipmentQualityTier;
            if (inventoryUpgradeQualityActionButton != null)
            {
                inventoryUpgradeQualityActionButton.interactable = canUpgradeQuality;
                Color32 fill = canUpgradeQuality ? new Color32(12, 27, 36, 255) : inactiveTabFill;
                Color32 border = canUpgradeQuality ? (Color32)Gold : inactiveTabBorder;
                ConfigureOutlinedButton(inventoryUpgradeQualityActionButton, fill, border, 2f);
                if (inventoryUpgradeQualityActionLabel != null)
                    inventoryUpgradeQualityActionLabel.color = canUpgradeQuality ? Color.white : inactiveTabText;
            }
        }

        ItemInstanceSaveData FindInventoryItemInstance(ItemConfig itemConfig, out bool isEquipped)
        {
            isEquipped = false;
            if (itemConfig == null)
                return null;

            if (IsItemEquipped(itemConfig, out ItemInstanceSaveData equippedInstance))
            {
                isEquipped = true;
                return equippedInstance;
            }

            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager.CurrentSave ?? saveManager.LoadOrCreate();
            if (save?.inventory?.itemInstances == null)
                return null;

            foreach (ItemInstanceSaveData instance in save.inventory.itemInstances)
            {
                if (instance != null && string.Equals(instance.itemId, itemConfig.StableId,
                    StringComparison.Ordinal))
                    return instance;
            }

            return null;
        }

        bool IsItemEquipped(ItemConfig itemConfig, out ItemInstanceSaveData equippedInstance)
        {
            equippedInstance = null;
            if (itemConfig == null || !InventoryService.IsEquippable(itemConfig))
                return false;

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
                return false;

            SaveManager saveManager = SaveManager.Instance;
            EquipmentService equipmentService = new EquipmentService(saveManager, config);
            InventoryService inventoryService = new InventoryService(saveManager, config);
            foreach (EquipmentLoadoutSaveData entry in equipmentService.GetEquipmentLoadout())
            {
                if (entry == null || string.IsNullOrEmpty(entry.itemInstanceId))
                    continue;

                ItemInstanceSaveData instance = inventoryService.GetEquipmentInstance(
                    entry.itemInstanceId);
                if (instance != null
                    && string.Equals(instance.itemId, itemConfig.StableId, StringComparison.Ordinal))
                {
                    equippedInstance = instance;
                    return true;
                }
            }

            return false;
        }

        ItemInstanceSaveData GetOrCreateEquipmentInstance(ItemConfig itemConfig,
            InventoryService inventoryService)
        {
            ItemInstanceSaveData existing = FindInventoryItemInstance(itemConfig, out _);
            if (existing != null)
                return existing;
            return inventoryService.AddEquipmentInstance(itemConfig.StableId);
        }

        void EquipInventoryPopupItem(int itemIndex, ItemConfig itemConfig)
        {
            if (itemConfig == null)
                return;

            HeroProgressSaveData progress = LoadSelectedHeroProgress();
            int playerLevel = progress != null ? Math.Max(1, progress.level) : 1;
            if (!itemConfig.MeetsRequirement(playerLevel))
                return;

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
                return;

            SaveManager saveManager = SaveManager.Instance;
            InventoryService inventoryService = new InventoryService(saveManager, config);
            EquipmentService equipmentService = new EquipmentService(saveManager, config);
            ItemInstanceSaveData instance = GetOrCreateEquipmentInstance(itemConfig, inventoryService);
            if (instance == null)
                return;

            if (!equipmentService.Equip(instance.instanceId, itemConfig.EquipmentSlot))
            {
                equipmentService.Unequip(itemConfig.EquipmentSlot);
                equipmentService.Equip(instance.instanceId, itemConfig.EquipmentSlot);
            }

            saveManager.SaveNow();
            CloseInventoryItemPopup();
            RefreshInventoryGridEquippedVisuals();
            RefreshCharacterStats();
            RefreshEquippedSlotIcons();
        }

        void UnequipInventoryPopupItem(ItemConfig itemConfig)
        {
            if (itemConfig == null || !IsItemEquipped(itemConfig, out _))
                return;

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
                return;

            SaveManager saveManager = SaveManager.Instance;
            EquipmentService equipmentService = new EquipmentService(saveManager, config);
            equipmentService.Unequip(itemConfig.EquipmentSlot);
            saveManager.SaveNow();
            CloseInventoryItemPopup();
            RefreshInventoryGridEquippedVisuals();
            RefreshCharacterStats();
            RefreshEquippedSlotIcons();
        }

        void UpgradeInventoryPopupItem(int itemIndex, ItemConfig itemConfig, bool fromStatus = false)
        {
            if (itemConfig == null)
                return;

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
                return;

            SaveManager saveManager = SaveManager.Instance;
            InventoryService inventoryService = new InventoryService(saveManager, config);
            ItemInstanceSaveData instance = GetOrCreateEquipmentInstance(itemConfig, inventoryService);
            if (instance == null || instance.upgradeLevel >= MaxEquipmentUpgradeLevel)
                return;

            instance.upgradeLevel = Mathf.Min(MaxEquipmentUpgradeLevel,
                Mathf.Max(0, instance.upgradeLevel) + 1);
            saveManager.SaveNow();
            RefreshInventoryGridEquippedVisuals();
            RefreshCharacterStats();
            RefreshEquippedSlotIcons();
            OpenInventoryItemPopup(itemIndex, fromStatus);
            if (inventoryUpgradePopup != null)
            {
                inventoryUpgradePopup.transform.SetAsLastSibling();
                RefreshInventoryUpgradePopup();
            }
        }

        void UpgradeQualityInventoryPopupItem(int itemIndex, ItemConfig itemConfig, bool fromStatus = false)
        {
            if (itemConfig == null)
                return;

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
                return;

            SaveManager saveManager = SaveManager.Instance;
            InventoryService inventoryService = new InventoryService(saveManager, config);
            ItemInstanceSaveData instance = GetOrCreateEquipmentInstance(itemConfig, inventoryService);
            if (instance == null || instance.qualityTier >= MaxEquipmentQualityTier)
                return;

            instance.qualityTier = Mathf.Min(MaxEquipmentQualityTier,
                Mathf.Max(0, instance.qualityTier) + 1);
            saveManager.SaveNow();
            RefreshInventoryGridEquippedVisuals();
            RefreshCharacterStats();
            RefreshEquippedSlotIcons();
            OpenInventoryItemPopup(itemIndex, fromStatus);
            if (inventoryUpgradePopup != null)
            {
                inventoryUpgradePopup.transform.SetAsLastSibling();
                RefreshInventoryUpgradePopup();
            }
        }

        static int GetInventoryItemPopupType(int itemIndex)
        {
            switch (itemIndex)
            {
                case 1:
                case 2:
                    return 0;
                case 0:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    return 2;
                default:
                    return 2;
            }
        }

        void LogInventoryPopupAction(string actionName, int itemIndex)
        {
            Debug.Log($"[MainProgress] InventoryAction={actionName} "
                + $"Item={GetInventoryItemName(itemIndex)}; Gameplay=NULL.", this);
        }

        void CloseInventoryItemPopup()
        {
            if (inventoryItemPopup != null)
                Destroy(inventoryItemPopup);
            inventoryItemPopup = null;
        }

        RectTransform CreateFullScreenOverlay(string name, Color backdropColor,
            Transform parent = null)
        {
            RectTransform overlay = CreateAnchoredRect(parent != null ? parent : canvas.transform,
                name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = backdropColor;
            backdrop.raycastTarget = true;
            overlay.SetAsLastSibling();
            return overlay;
        }

        static RectTransform CreateFullScreenArtwork(Transform parent, Sprite sprite,
            string name, out Image artworkImage, bool fitToScreen = false)
        {
            RectTransform artwork = CreateAnchoredRect(parent, name,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            artworkImage = artwork.gameObject.AddComponent<Image>();
            if (sprite != null)
                artworkImage.sprite = sprite;
            artworkImage.color = Color.white;
            artworkImage.preserveAspect = true;
            artworkImage.raycastTarget = false;

            if (fitToScreen)
            {
                Canvas parentCanvas = parent.GetComponentInParent<Canvas>();
                CanvasScaler scaler = parentCanvas != null
                    ? parentCanvas.GetComponent<CanvasScaler>()
                    : null;
                float scaleFactor = 0f;
                if (scaler != null
                    && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                    && scaler.referenceResolution.x > 0f
                    && scaler.referenceResolution.y > 0f)
                {
                    float widthScale = Screen.width / scaler.referenceResolution.x;
                    float heightScale = Screen.height / scaler.referenceResolution.y;
                    float match = Mathf.Clamp01(scaler.matchWidthOrHeight);
                    scaleFactor = Mathf.Pow(widthScale, 1f - match)
                        * Mathf.Pow(heightScale, match);
                }

                if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor)
                    || scaleFactor <= 0f)
                {
                    scaleFactor = parentCanvas != null
                        && parentCanvas.scaleFactor > 0f
                        && !float.IsNaN(parentCanvas.scaleFactor)
                        && !float.IsInfinity(parentCanvas.scaleFactor)
                        ? parentCanvas.scaleFactor
                        : 1f;
                }

                float screenW = Screen.width / scaleFactor;
                float screenH = Screen.height / scaleFactor;
                bool validScreenSize = screenW > 0f && screenH > 0f
                    && screenW <= 10000f && screenH <= 10000f
                    && !float.IsNaN(screenW) && !float.IsNaN(screenH)
                    && !float.IsInfinity(screenW) && !float.IsInfinity(screenH);
                if (validScreenSize)
                {
                    artwork.anchorMin = new Vector2(.5f, .5f);
                    artwork.anchorMax = new Vector2(.5f, .5f);
                    artwork.sizeDelta = new Vector2(screenW, screenH);
                    artwork.anchoredPosition = Vector2.zero;
                    return artwork;
                }
            }

            if (sprite == null)
                return artwork;

            AspectRatioFitter aspectFitter =
                artwork.gameObject.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            return artwork;
        }

        static void EnsureTabFocusWhiteSprite()
        {
            if (tabFocusWhiteSprite == null)
                tabFocusWhiteSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0f, 0f, 4f, 4f), new Vector2(.5f, .5f),
                    100f / 6f, 0u, SpriteMeshType.FullRect,
                    new Vector4(1f, 1f, 1f, 1f));
        }

        // Tạo khung focus tab dùng sprite 9-slice; fallback về Outline vàng nếu thiếu sprite
        static RectTransform CreateTabFocusOverlay(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform focusRect = CreateAnchoredRect(parent, name,
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image focusImage = focusRect.gameObject.AddComponent<Image>();
            Sprite focusSprite = Resources.Load<Sprite>("MainProgress/UI/focus_frame_9slice");
            if (focusSprite == null)
            {
                Debug.LogError("MainProgress failed to load sprite Resources/MainProgress/UI/focus_frame_9slice.");
                EnsureTabFocusWhiteSprite();
                focusImage.sprite = tabFocusWhiteSprite;
                focusImage.color = Color.clear;
                focusImage.type = Image.Type.Sliced;
                focusImage.fillCenter = false;
                focusImage.raycastTarget = false;
                Outline focusOutline = focusRect.gameObject.AddComponent<Outline>();
                focusOutline.effectColor = Gold;
                focusOutline.effectDistance = new Vector2(3f, -3f);
                focusOutline.useGraphicAlpha = false;
            }
            else
            {
                focusImage.sprite = focusSprite;
                focusImage.color = Color.white;
                focusImage.type = Image.Type.Sliced;
                focusImage.fillCenter = false;
                focusImage.raycastTarget = false;
            }
            return focusRect;
        }

        RectTransform BuildCultivationPanel()
        {
            RectTransform overlay = CreateFullScreenOverlay("CultivationPanelOverlay",
                new Color32(4, 28, 22, 255));
            Sprite characterSprite = Resources.Load<Sprite>("MainProgress/UI/CharacterSingle");
            if (characterSprite == null)
                Debug.LogError("MainProgress failed to load sprite Resources/MainProgress/UI/CharacterSingle.", this);
            RectTransform artwork = CreateFullScreenArtwork(overlay,
                characterSprite, "CultivationArtwork",
                out cultivationBackgroundImage, fitToScreen: true);

            characterTabFocusRect = CreateTabFocusOverlay(artwork,
                "CharacterTabFocusOverlay",
                CharacterTabFocusAnchors[1].min, CharacterTabFocusAnchors[1].max);

            CreateCharacterHitButton(artwork, "CultivationCloseButton",
                .890f, .893f, 1.000f, .946f,
                () => ClosePanel(overlay.gameObject));
            characterTabButtons[0] = CreateCharacterHitButton(artwork, "CharacterTab_Skills",
                .038f, .818f, .260f, .864f,
                () => SelectCharacterTab(0));
            characterTabButtons[1] = CreateCharacterHitButton(artwork, "CharacterTab_Cultivation",
                .273f, .818f, .493f, .864f,
                () => SelectCharacterTab(1));
            characterTabButtons[2] = CreateCharacterHitButton(artwork, "CharacterTab_Companion",
                .507f, .818f, .728f, .864f,
                () => SelectCharacterTab(2));
            characterTabButtons[3] = CreateCharacterHitButton(artwork, "CharacterTab_Pet",
                .742f, .818f, .962f, .864f,
                () => SelectCharacterTab(3));

            starChartUIGroup = CreateAnchoredRect(artwork, "StarChartUI",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            CreateGemInventoryRow(starChartUIGroup.transform);

            RuntimeHeroStats? stats = LoadSelectedHeroStats();
            int heroLevel = stats.HasValue
                ? Mathf.Clamp(stats.Value.Level, Realms[0].MinLevel,
                    Realms[Realms.Length - 1].MaxLevel)
                : 0;
            SelectStarChartRealm(FindRealmIndex(heroLevel), heroLevel);

            float realmSelectorMinY = .665f;
            float realmSelectorMaxY = .71f;
            float realmSelectorLeft = .26f;
            float realmSelectorRight = .74f;

            previousRealmButton = CreateCharacterHitButton(starChartUIGroup.transform, "PreviousRealm",
                .16f, realmSelectorMinY, .25f, realmSelectorMaxY, SelectPreviousStarChartRealm);
            TMP_Text previousRealmArrow = AddLabel(starChartUIGroup.transform, "PreviousRealmArrow",
                "◁", 42, Gold, new Vector2(.16f, realmSelectorMinY),
                new Vector2(.25f, realmSelectorMaxY), TextAlignmentOptions.Center);
            ApplyStarChartTextStyle(previousRealmArrow, 42);

            RectTransform realmSelectorFrame = CreateAnchoredRect(starChartUIGroup.transform,
                "RealmSelectorFrame", new Vector2(realmSelectorLeft, realmSelectorMinY),
                new Vector2(realmSelectorRight, realmSelectorMaxY), Vector2.zero, Vector2.zero);
            Image realmSelectorBackground = realmSelectorFrame.gameObject.AddComponent<Image>();
            realmSelectorBackground.sprite = GetChamferedPlaqueSprite();
            realmSelectorBackground.type = Image.Type.Sliced;
            realmSelectorBackground.color = new Color32(12, 27, 36, 255);
            realmSelectorBackground.raycastTarget = false;
            Outline realmSelectorOutline = realmSelectorFrame.gameObject.AddComponent<Outline>();
            realmSelectorOutline.effectColor = Gold;
            realmSelectorOutline.effectDistance = new Vector2(3f, -3f);

            RectTransform realmBevelLeft = CreateAnchoredRect(realmSelectorFrame,
                "RealmBevelLeft", new Vector2(-.024f, .14f), new Vector2(.006f, .86f),
                Vector2.zero, Vector2.zero);
            Image realmBevelLeftImage = realmBevelLeft.gameObject.AddComponent<Image>();
            realmBevelLeftImage.sprite = GetBevelTriangleSprite(true);
            realmBevelLeftImage.color = new Color32(12, 27, 36, 255);
            realmBevelLeftImage.raycastTarget = false;
            Outline realmBevelLeftOutline = realmBevelLeft.gameObject.AddComponent<Outline>();
            realmBevelLeftOutline.effectColor = Gold;
            realmBevelLeftOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform realmBevelRight = CreateAnchoredRect(realmSelectorFrame,
                "RealmBevelRight", new Vector2(.994f, .14f), new Vector2(1.024f, .86f),
                Vector2.zero, Vector2.zero);
            Image realmBevelRightImage = realmBevelRight.gameObject.AddComponent<Image>();
            realmBevelRightImage.sprite = GetBevelTriangleSprite(false);
            realmBevelRightImage.color = new Color32(12, 27, 36, 255);
            realmBevelRightImage.raycastTarget = false;
            Outline realmBevelRightOutline = realmBevelRight.gameObject.AddComponent<Outline>();
            realmBevelRightOutline.effectColor = Gold;
            realmBevelRightOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform realmSelectorInnerBorder = CreateAnchoredRect(realmSelectorFrame,
                "RealmSelectorInnerBorder", new Vector2(.035f, .10f), new Vector2(.965f, .90f),
                Vector2.zero, Vector2.zero);
            Image realmSelectorInnerImage = realmSelectorInnerBorder.gameObject.AddComponent<Image>();
            realmSelectorInnerImage.sprite = GetChamferedPlaqueSprite();
            realmSelectorInnerImage.type = Image.Type.Sliced;
            realmSelectorInnerImage.color = new Color32(22, 38, 48, 255);
            realmSelectorInnerImage.raycastTarget = false;
            Outline realmSelectorInnerOutline = realmSelectorInnerBorder.gameObject.AddComponent<Outline>();
            realmSelectorInnerOutline.effectColor = MutedGold;
            realmSelectorInnerOutline.effectDistance = new Vector2(1.5f, -1.5f);

            realmButtonLabel = AddLabel(realmSelectorFrame, "RealmSelector",
                GetRealmButtonLabel(), 30, Gold, new Vector2(.04f, .12f),
                new Vector2(.96f, .88f), TextAlignmentOptions.Center);
            realmButtonLabel.enableAutoSizing = true;
            realmButtonLabel.fontSizeMin = 20;
            realmButtonLabel.fontSizeMax = 32;
            realmButtonLabel.overflowMode = TextOverflowModes.Overflow;
            realmButtonLabel.enableWordWrapping = false;
            ApplyStarChartTextStyle(realmButtonLabel, 30);

            nextRealmButton = CreateCharacterHitButton(starChartUIGroup.transform, "NextRealm",
                .75f, realmSelectorMinY, .84f, realmSelectorMaxY, SelectNextStarChartRealm);
            TMP_Text nextRealmArrow = AddLabel(starChartUIGroup.transform, "NextRealmArrow",
                "▷", 42, Gold, new Vector2(.75f, realmSelectorMinY),
                new Vector2(.84f, realmSelectorMaxY), TextAlignmentOptions.Center);
            ApplyStarChartTextStyle(nextRealmArrow, 42);
            RefreshRealmNavigationButtons();

            CreateRealmProgressBar(starChartUIGroup.transform);

            starChartContent = CreateAnchoredRect(starChartUIGroup.transform, "StarChartContent",
                new Vector2(.09f, .31f), new Vector2(.91f, .63f),
                Vector2.zero, Vector2.zero).gameObject;
            Image chartViewportImage = starChartContent.AddComponent<Image>();
            chartViewportImage.color = Color.white;
            chartViewportImage.raycastTarget = false;
            Mask chartMask = starChartContent.AddComponent<Mask>();
            chartMask.showMaskGraphic = false;
            starChartZoomScrollRect = starChartContent.AddComponent<ScrollRect>();
            starChartZoomScrollRect.viewport = starChartContent.GetComponent<RectTransform>();
            starChartZoomScrollRect.horizontal = true;
            starChartZoomScrollRect.vertical = true;
            starChartZoomScrollRect.movementType = ScrollRect.MovementType.Clamped;
            starChartZoomScrollRect.scrollSensitivity = 35f;
            starChartGraphicRect = CreateAnchoredRect(starChartContent.transform,
                "ChartGraphic", new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, Vector2.zero);
            starChartGraphicRect.pivot = new Vector2(.5f, .5f);
            Canvas.ForceUpdateCanvases();
            starChartGraphicRect.sizeDelta = starChartContent.GetComponent<RectTransform>().rect.size;
            starChartGraphicRect.gameObject.AddComponent<CanvasRenderer>();
            starChartGraphic = starChartGraphicRect.gameObject
                .AddComponent<MainProgressChartGraphic>();
            starChartGraphic.color = Color.white;
            starChartGraphic.StarClicked += SelectStarChartNode;
            starChartZoomScrollRect.content = starChartGraphicRect;
            CreateStarChartArtwork();
            // Ẩn cây nhánh tinh đồ (giữ lại code/GameObject để có thể bật lại sau nếu cần),
            // thay bằng hiệu ứng quả cầu Soul Harvest ở giữa khu vực chart.
            starChartGraphicRect.gameObject.SetActive(false);
            starChartZoomScrollRect.enabled = false;
            CreateSoulHarvestOrb(starChartContent.transform);
            SetStarChartZoom(1f);
            RefreshSelectedRealmStars();
            CreateStarChartZoomControls(starChartUIGroup.transform);
            CreateStarNodePopup();
            CreateTotalBuffPopup();

            skillsPlaceholder = CreateAnchoredRect(artwork, "SkillsPlaceholder",
                new Vector2(.08f, .25f), new Vector2(.92f, .66f),
                Vector2.zero, Vector2.zero).gameObject;
            Image skillsHitImage = skillsPlaceholder.AddComponent<Image>();
            skillsHitImage.color = new Color(1f, 1f, 1f, 0f);
            skillsHitImage.raycastTarget = true;
            Button skillsPlaceholderButton = skillsPlaceholder.AddComponent<Button>();
            skillsPlaceholderButton.targetGraphic = skillsHitImage;
            skillsPlaceholderButton.transition = Selectable.Transition.None;
            skillsPlaceholderButton.onClick.AddListener(
                () => Debug.Log("[MainProgress] Clicked=SkillsPlaceholder; Gameplay=NULL.", this));

            CreateStarChartDetailsPanel(starChartUIGroup.transform);
            starActionHint = CreateStarChartDetailLabel("StarActionsHint",
                "Chọn một nốt sao để xem chi phí.", 21, out starActionHintRect);
            starActionStatus = CreateStarChartDetailLabel("StarActionStatus",
                "Chưa thay đổi dữ liệu sao.", 19, out starActionStatusRect);
            UpdateStarActionDetails();
            RefreshStarChartDetailsLayout();
            breakthroughPopup = BuildBreakthroughPopup(overlay).gameObject;
            breakthroughPopup.SetActive(false);
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        RectTransform BuildBreakthroughPopup(Transform parent)
        {
            RectTransform popup = CreateAnchoredRect(parent, "BreakthroughPopup",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdrop = popup.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, .82f);
            backdrop.raycastTarget = true;
            RectTransform card = CreateAnchoredRect(popup, "BreakthroughCard",
                new Vector2(.10f, .28f), new Vector2(.90f, .72f),
                Vector2.zero, Vector2.zero);
            SetImage(card.gameObject, new Color32(24, 30, 40, 255), false);
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);

            AddLabel(card, "BreakthroughTitle", "Đột phá cảnh giới",
                34, Gold, new Vector2(.08f, .76f), new Vector2(.92f, .96f));
            breakthroughRequirementLabel = AddLabel(card,
                "BreakthroughRequirements", string.Empty, 22, Color.white,
                new Vector2(.08f, .34f), new Vector2(.92f, .75f));
            Button cancel = CreatePanelButton(card, "BreakthroughCancel",
                "QUAY LẠI", new Vector2(190f, 64f),
                new Vector2(.28f, .16f), new Color32(74, 54, 44, 255));
            cancel.onClick.AddListener(() => breakthroughPopup.SetActive(false));
            Button confirm = CreatePanelButton(card, "BreakthroughConfirm",
                "ĐỘT PHÁ", new Vector2(220f, 64f),
                new Vector2(.72f, .16f), Gold);
            confirm.onClick.AddListener(ConfirmBreakthrough);
            breakthroughConfirmButton = confirm;
            return popup;
        }

        void CreateGemInventoryRow(Transform parent)
        {
            RectTransform row = CreateAnchoredRect(parent, "BaseGemInventory",
                new Vector2(.105f, .71f), new Vector2(.895f, .805f),
                Vector2.zero, Vector2.zero);

            const int gemCount = 7;
            float slotWidth = 1f / gemCount;
            Sprite slotBgSprite = GetRoundedRectSprite(12);
            Sprite slotGlowSprite = GetRoundedRectSprite(14);
            for (int index = 0; index < StarChartGemTypes.Length; index++)
            {
                float slotCenter = (index + .5f) * slotWidth;
                float slotMinX = slotCenter - slotWidth * .48f;
                float slotMaxX = slotCenter + slotWidth * .48f;
                Sprite sprite = GetSpriteAt(starChartBaseGemSprites, index);
                if (sprite == null)
                {
                    Debug.LogError($"Tinh đồ thiếu sprite base gem {StarChartGemTypes[index]}.", this);
                    continue;
                }

                RectTransform slotGlowRect = CreateAnchoredRect(row,
                    $"GemSlotGlow_{StarChartGemTypes[index]}",
                    new Vector2(slotMinX, .02f),
                    new Vector2(slotMaxX, .98f),
                    new Vector2(-2f, -2f), new Vector2(2f, 2f));
                Image slotGlowImage = slotGlowRect.gameObject.AddComponent<Image>();
                slotGlowImage.sprite = slotGlowSprite;
                slotGlowImage.type = Image.Type.Sliced;
                slotGlowImage.color = new Color32(231, 190, 110, 65);
                slotGlowImage.raycastTarget = false;
                baseGemSlotGlowImages[index] = slotGlowImage;

                RectTransform slotBgRect = CreateAnchoredRect(row,
                    $"GemSlotBg_{StarChartGemTypes[index]}",
                    new Vector2(slotMinX, .02f),
                    new Vector2(slotMaxX, .98f),
                    Vector2.zero, Vector2.zero);
                Image slotBgImage = slotBgRect.gameObject.AddComponent<Image>();
                slotBgImage.sprite = slotBgSprite;
                slotBgImage.type = Image.Type.Sliced;
                slotBgImage.color = new Color32(10, 16, 24, 200);
                slotBgImage.raycastTarget = false;
                Outline slotOutline = slotBgRect.gameObject.AddComponent<Outline>();
                slotOutline.effectColor = new Color32(90, 105, 120, 80);
                slotOutline.effectDistance = new Vector2(1f, -1f);
                baseGemSlotOutlines[index] = slotOutline;

                const float iconWidth = .075f;
                RectTransform iconRect = CreateAnchoredRect(row,
                    $"GemIcon_{StarChartGemTypes[index]}",
                    new Vector2(slotCenter - iconWidth * .5f, .34f),
                    new Vector2(slotCenter + iconWidth * .5f, .96f),
                    Vector2.zero, Vector2.zero);
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                int gemValue = index < baseGemDisplayInventory.Length
                    ? baseGemDisplayInventory[index] : 0;
                baseGemInventoryCountLabels[index] = AddLabel(row,
                    $"GemCount_{StarChartGemTypes[index]}",
                    gemValue.ToString(CultureInfo.InvariantCulture), 25, Color.white,
                    new Vector2(slotCenter - slotWidth * .45f, .06f),
                    new Vector2(slotCenter + slotWidth * .45f, .42f));
                ApplyStarChartTextStyle(baseGemInventoryCountLabels[index], 25);

                int branchIndex = index;
                CreateCharacterHitButton(row, $"GemSlotButton_{StarChartGemTypes[index]}",
                    slotMinX, .02f, slotMaxX, .98f,
                    () => SelectStarChartBranch(branchIndex));
            }

            RefreshGemSlotSelectionVisuals();
        }

        void SelectStarChartBranch(int branchIndex)
        {
            if (branchIndex < 0 || branchIndex >= StarChartGemTypes.Length)
                return;
            HideStarNodePopup();
            selectedStarChartBranchIndex = branchIndex;
            RefreshSelectedRealmStars();
        }

        void RefreshGemSlotSelectionVisuals()
        {
            for (int index = 0; index < StarChartGemTypes.Length; index++)
            {
                bool isSelected = index == selectedStarChartBranchIndex;
                if (baseGemSlotOutlines[index] != null)
                {
                    baseGemSlotOutlines[index].effectColor = isSelected
                        ? Gold
                        : new Color32(90, 105, 120, 80);
                    baseGemSlotOutlines[index].effectDistance = isSelected
                        ? new Vector2(2.5f, -2.5f)
                        : new Vector2(1f, -1f);
                }

                if (baseGemSlotGlowImages[index] != null)
                    baseGemSlotGlowImages[index].enabled = isSelected;
            }
        }

        void CreateRealmProgressBar(Transform parent)
        {
            RectTransform bar = CreateAnchoredRect(parent, "RealmProgressBar",
                new Vector2(.23f, .635f), new Vector2(.77f, .66f),
                Vector2.zero, Vector2.zero);
            Image trackImage = bar.gameObject.AddComponent<Image>();
            trackImage.sprite = GetRoundedRectSprite(6);
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color32(10, 16, 24, 255);
            trackImage.raycastTarget = false;
            Outline trackOutline = bar.gameObject.AddComponent<Outline>();
            trackOutline.effectColor = MutedGold;
            trackOutline.effectDistance = new Vector2(1f, -1f);

            EnsureTabFocusWhiteSprite();
            RectTransform fillRect = CreateAnchoredRect(bar, "RealmProgressFill",
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            realmProgressFillImage = fillRect.gameObject.AddComponent<Image>();
            realmProgressFillImage.sprite = tabFocusWhiteSprite;
            realmProgressFillImage.color = Gold;
            realmProgressFillImage.type = Image.Type.Filled;
            realmProgressFillImage.fillMethod = Image.FillMethod.Horizontal;
            realmProgressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            realmProgressFillImage.fillAmount = 0f;
            realmProgressFillImage.raycastTarget = false;

            realmProgressLabel = AddLabel(bar, "RealmProgressLabel",
                string.Empty, 20, Color.white, Vector2.zero, Vector2.one,
                TextAlignmentOptions.Center);
            ApplyStarChartTextStyle(realmProgressLabel, 20);
            Outline labelOutline = realmProgressLabel.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color32(10, 16, 24, 230);
            labelOutline.effectDistance = new Vector2(1.2f, -1.2f);
            RefreshRealmProgressBar();
        }

        void RefreshRealmProgressBar()
        {
            int litStars = 0;
            for (int index = 0; index < litStarsPerBranch.Length; index++)
                litStars += litStarsPerBranch[index];
            int requiredStars = StarChartGemTypes.Length * MainProgressChartGraphic.StarsPerBranch;
            if (realmProgressFillImage != null)
                realmProgressFillImage.fillAmount = requiredStars > 0
                    ? Mathf.Clamp01((float)litStars / requiredStars)
                    : 0f;
            if (realmProgressLabel != null)
                realmProgressLabel.text = $"{litStars}/{requiredStars}";
        }

        void CreateStarChartDetailsPanel(Transform parent)
        {
            RectTransform viewport = CreateAnchoredRect(parent,
                "StarChartDetailsViewport", new Vector2(.115f, .184f),
                new Vector2(.885f, .24f), Vector2.zero, Vector2.zero);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            Mask viewportMask = viewport.gameObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            starChartDetailsScrollRect =
                viewport.gameObject.AddComponent<ScrollRect>();
            starChartDetailsScrollRect.viewport = viewport;
            starChartDetailsScrollRect.horizontal = false;
            starChartDetailsScrollRect.vertical = true;
            starChartDetailsScrollRect.movementType =
                ScrollRect.MovementType.Clamped;
            starChartDetailsScrollRect.scrollSensitivity = 24f;

            starChartDetailsContent = CreateAnchoredRect(viewport,
                "StarChartDetailsContent", new Vector2(0f, 1f),
                new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            starChartDetailsContent.pivot = new Vector2(.5f, 1f);
            starChartDetailsScrollRect.content = starChartDetailsContent;
        }

        TMP_Text CreateStarChartDetailLabel(string objectName, string value,
            int fontSize, out RectTransform labelRect)
        {
            labelRect = CreateAnchoredRect(starChartDetailsContent,
                objectName, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TMP_Text text = CampaignUIFactory.CreateText(labelRect, "Text",
                value, fontSize, Color.white, TextAlignmentOptions.TopLeft);
            ApplyStarChartTextStyle(text, fontSize);
            return text;
        }

        void ApplyStarChartTextStyle(TMP_Text text, int fontSize)
        {
            if (text == null)
                return;
            if (starChartFont != null)
                text.font = starChartFont;
            else if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        void RefreshStarChartDetailsLayout()
        {
            if (starChartDetailsScrollRect == null
                || starChartDetailsContent == null
                || starActionHint == null
                || starActionStatus == null)
                return;

            Canvas.ForceUpdateCanvases();
            float viewportHeight = starChartDetailsScrollRect.viewport.rect.height;
            starActionHint.ForceMeshUpdate();
            starActionStatus.ForceMeshUpdate();
            float hintHeight = Mathf.Max(30f, starActionHint.preferredHeight + 8f);
            float statusHeight = Mathf.Max(28f,
                starActionStatus.preferredHeight + 8f);
            const float topPadding = 5f;
            const float rowSpacing = 4f;
            const float bottomPadding = 5f;
            float contentHeight = topPadding + hintHeight + rowSpacing
                + statusHeight + bottomPadding;

            starChartDetailsContent.sizeDelta =
                new Vector2(0f, Mathf.Max(viewportHeight, contentHeight));
            SetStarChartDetailLabelRect(starActionHintRect,
                topPadding, hintHeight);
            SetStarChartDetailLabelRect(starActionStatusRect,
                topPadding + hintHeight + rowSpacing, statusHeight);
            starChartDetailsScrollRect.vertical =
                contentHeight > viewportHeight + 1f;
            if (!starChartDetailsScrollRect.vertical)
                starChartDetailsContent.anchoredPosition = Vector2.zero;
        }

        static void SetStarChartDetailLabelRect(RectTransform labelRect,
            float top, float height)
        {
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(.5f, 1f);
            labelRect.offsetMin = new Vector2(8f, -top - height);
            labelRect.offsetMax = new Vector2(-8f, -top);
        }

        void RefreshBaseGemInventory()
        {
            for (int index = 0; index < StarChartGemTypes.Length; index++)
            {
                int count = starChartGemInventory.GetBaseGemCount(StarChartGemTypes[index]);
                baseGemDisplayInventory[index] = count;
                if (baseGemInventoryCountLabels[index] != null)
                    baseGemInventoryCountLabels[index].text =
                        count.ToString(CultureInfo.InvariantCulture);
            }
        }

        void CreateStarChartArtwork()
        {
            Canvas.ForceUpdateCanvases();
            Rect chartRect = starChartGraphicRect.rect;
            float unit = Mathf.Min(chartRect.width, chartRect.height);

            for (int branch = 0; branch < StarChartGemTypes.Length; branch++)
            {
                Sprite specialSprite = GetSpriteAt(starChartSpecialGemSprites, branch);
                if (specialSprite == null)
                {
                    Debug.LogError($"Tinh đồ thiếu sprite điểm cuối cho nhánh "
                        + $"{StarChartGemTypes[branch]}.", this);
                    continue;
                }

                Vector2 endPosition = MainProgressChartGraphic.GetStarPosition(chartRect,
                    branch, MainProgressChartGraphic.StarsPerBranch - 1);
                Vector2 endAnchor = new Vector2(
                    (endPosition.x - chartRect.xMin) / chartRect.width,
                    (endPosition.y - chartRect.yMin) / chartRect.height);
                AddChartImage($"BranchEndpoint_{StarChartGemTypes[branch]}",
                    specialSprite, endAnchor, unit * .075f);
            }
        }

        void CreateStarChartZoomControls(Transform parent)
        {
            CreateStarChartCircleButton(parent, "StarChartZoomIn",
                new Vector2(.875f, .47f), new Vector2(.935f, .53f), "+",
                32, () => SetStarChartZoom(currentStarChartZoom + SoulHarvestOrbZoomStep));
            CreateStarChartCircleButton(parent, "StarChartZoomOut",
                new Vector2(.875f, .40f), new Vector2(.935f, .46f), "−",
                32, () => SetStarChartZoom(currentStarChartZoom - SoulHarvestOrbZoomStep));

            totalBuffButton = CreateStarChartControlButton(parent, "TotalBuffButton",
                new Vector2(.13f, .25f), new Vector2(.47f, .292f), "Tổng chỉ số",
                20, OpenTotalBuffPopup);

            RectTransform breakthroughGlowRect = CreateAnchoredRect(parent,
                "BreakthroughButtonGlow", new Vector2(.522f, .244f),
                new Vector2(.878f, .298f), Vector2.zero, Vector2.zero);
            breakthroughGlowImage = breakthroughGlowRect.gameObject.AddComponent<Image>();
            breakthroughGlowImage.sprite = GetRoundedRectSprite(18);
            breakthroughGlowImage.type = Image.Type.Sliced;
            breakthroughGlowImage.color = new Color32(255, 214, 100, 95);
            breakthroughGlowImage.raycastTarget = false;

            breakthroughButton = CreateStarChartControlButton(parent, "BreakthroughButton",
                new Vector2(.53f, .25f), new Vector2(.87f, .292f), "ĐỘT PHÁ",
                20, TryOpenBreakthroughPopup);
            ConfigureOutlinedButton(breakthroughButton, new Color32(28, 35, 44, 245), Gold, 3.5f);
            Transform breakthroughLabelTransform = parent != null ? parent.Find("BreakthroughButtonLabel") : null;
            breakthroughButtonLabel = breakthroughLabelTransform != null
                ? breakthroughLabelTransform.GetComponent<TMP_Text>()
                : null;
            UpdateBreakthroughButtonState();
        }

        Button CreateStarChartCircleButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label, int fontSize,
            UnityEngine.Events.UnityAction onClick)
        {
            Button button = CreateCharacterHitButton(parent, name,
                anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y, onClick);
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = CampaignUIFactory.GetFallbackMarkerSprite();
                image.preserveAspect = true;
                image.color = new Color32(18, 25, 31, 220);
                image.raycastTarget = true;
            }
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            TMP_Text labelText = AddLabel(button.transform, $"{name}Label", label, fontSize,
                Gold, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            ApplyStarChartTextStyle(labelText, fontSize);
            labelText.raycastTarget = false;
            return button;
        }

        Button CreateStarChartControlButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label, int fontSize,
            UnityEngine.Events.UnityAction onClick)
        {
            Button button = CreateCharacterHitButton(parent, name,
                anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y, onClick);
            ConfigureOutlinedButton(button, new Color32(28, 35, 44, 245), Gold, 2.5f);
            TMP_Text labelText = AddLabel(parent, $"{name}Label", label, fontSize,
                Color.white, anchorMin, anchorMax, TextAlignmentOptions.Center);
            labelText.raycastTarget = false;
            return button;
        }

        static void ConfigureOutlinedButton(Button button, Color32 backgroundColor,
            Color32 borderColor, float borderThickness)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = backgroundColor;
                image.raycastTarget = true;
            }

            button.transition = Selectable.Transition.None;
            Outline outline = button.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(borderThickness, -borderThickness);
        }

        void CreateStarNodePopup()
        {
            starNodePopup = CreateAnchoredRect(starChartUIGroup.transform,
                "StarNodePopup", new Vector2(.5f, .47f), new Vector2(.5f, .47f),
                Vector2.zero, Vector2.zero);
            starNodePopup.sizeDelta = new Vector2(440f, 300f);
            starNodePopup.anchoredPosition = Vector2.zero;
            Image popupBackground = starNodePopup.gameObject.AddComponent<Image>();
            popupBackground.color = new Color32(18, 22, 28, 245);
            popupBackground.raycastTarget = true;
            Outline outline = starNodePopup.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 214, 100, 255);
            outline.effectDistance = new Vector2(3f, -3f);

            starNodePopupText = CreateAnchoredLabel(starNodePopup, "StarNodePopupText",
                string.Empty, 28, Color.white, new Vector2(.08f, .34f), new Vector2(.84f, .92f),
                out TMP_Text popupText, TextAlignmentOptions.TopLeft);
            starNodePopupText = popupText;
            ApplyStarChartTextStyle(starNodePopupText, 28);
            starNodePopupCloseButton = CreateCharacterHitButton(starNodePopup, "StarNodePopupClose",
                .86f, .82f, .98f, .98f, HideStarNodePopup);
            Image closeImage = starNodePopupCloseButton.GetComponent<Image>();
            closeImage.color = new Color32(62, 45, 37, 255);
            Outline closeOutline = starNodePopupCloseButton.gameObject.AddComponent<Outline>();
            closeOutline.effectColor = Gold;
            closeOutline.effectDistance = new Vector2(2f, -2f);
            TMP_Text closeLabel = AddLabel(starNodePopup, "StarNodePopupCloseText", "X", 26, Gold,
                new Vector2(.86f, .82f), new Vector2(.98f, .98f), TextAlignmentOptions.Center);
            closeLabel.raycastTarget = false;

            starNodePopupUpgradeButton = CreateCharacterHitButton(starNodePopup,
                "StarNodePopupUpgrade", .30f, .06f, .70f, .27f,
                UpgradePopupStar);
            Image upgradeImage = starNodePopupUpgradeButton.GetComponent<Image>();
            upgradeImage.color = new Color32(142, 106, 45, 255);
            Outline upgradeOutline = starNodePopupUpgradeButton.gameObject.AddComponent<Outline>();
            upgradeOutline.effectColor = Gold;
            upgradeOutline.effectDistance = new Vector2(2f, -2f);
            starNodePopupUpgradeLabel = AddLabel(starNodePopup,
                "StarNodePopupUpgradeLabel", "Nâng cấp", 24, Color.white,
                new Vector2(.30f, .06f), new Vector2(.70f, .27f),
                TextAlignmentOptions.Center);
            starNodePopupUpgradeLabel.raycastTarget = false;
            starNodePopup.gameObject.SetActive(false);
        }

        void CreateTotalBuffPopup()
        {
            totalBuffPopup = CreateAnchoredRect(starChartUIGroup.transform, "TotalBuffPopup",
                new Vector2(.25f, .28f), new Vector2(.75f, .58f),
                Vector2.zero, Vector2.zero).gameObject;
            Image popupBackground = totalBuffPopup.AddComponent<Image>();
            popupBackground.color = new Color32(16, 20, 28, 230);
            Outline popupOutline = totalBuffPopup.AddComponent<Outline>();
            popupOutline.effectColor = Gold;
            popupOutline.effectDistance = new Vector2(3f, -3f);
            totalBuffPopupText = AddLabel(totalBuffPopup.transform, "TotalBuffPopupText",
                string.Empty, 22, Color.white,
                new Vector2(.06f, .15f), new Vector2(.94f, .82f),
                TextAlignmentOptions.TopLeft);
            ApplyStarChartTextStyle(totalBuffPopupText, 22);
            Button closeButton = CreatePanelButton(totalBuffPopup.transform, "TotalBuffCloseButton",
                "ĐÓNG", new Vector2(180f, 52f), new Vector2(.5f, .08f), new Color32(74, 54, 44, 255));
            closeButton.onClick.AddListener(CloseTotalBuffPopup);
            totalBuffPopup.SetActive(false);
        }

        void SetStarChartZoom(float targetZoom)
        {
            currentStarChartZoom = Mathf.Clamp(targetZoom,
                SoulHarvestOrbMinScale, SoulHarvestOrbMaxScale);
            if (soulHarvestOrbRect != null)
                soulHarvestOrbRect.localScale = Vector3.one * currentStarChartZoom;
        }

        readonly struct SoulHarvestOrbStyle
        {
            public readonly Sprite[] Frames;
            public readonly Color TintColor;

            public SoulHarvestOrbStyle(Sprite[] frames, Color tintColor)
            {
                Frames = frames;
                TintColor = tintColor;
            }
        }

        void CreateSoulHarvestOrb(Transform parent)
        {
            if (parent == null)
                return;

            soulHarvestOrbRect = CreateAnchoredRect(parent, "SoulHarvestOrb",
                new Vector2(.04f, .04f), new Vector2(.96f, .96f),
                Vector2.zero, Vector2.zero);
            soulHarvestOrbRect.pivot = new Vector2(.5f, .5f);

            // Project chưa có material additive sẵn cho VFX nên dùng material mặc định của UI Image.
            soulHarvestOrbImage = soulHarvestOrbRect.gameObject.AddComponent<Image>();
            soulHarvestOrbImage.preserveAspect = true;
            soulHarvestOrbImage.raycastTarget = false;
            soulHarvestOrbImage.color = Color.white;

            AspectRatioFitter aspectFitter = soulHarvestOrbRect.gameObject.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = SoulHarvestOrbAspectRatio;

            // Nút bấm trong suốt bao phủ 60% đường kính tâm quả cầu (anchors .20–.80).
            soulHarvestOrbButton = CreateCharacterHitButton(soulHarvestOrbRect,
                "SoulHarvestOrbButton", .20f, .20f, .80f, .80f,
                OpenSelectedBranchUpgradePopup);

            soulHarvestOrbElapsed = 0f;
            soulHarvestOrbFrameIndex = -1;
            RefreshSoulHarvestOrbVisual();
        }

        Sprite[] ResolveSoulHarvestRedFrames()
        {
            if (soulHarvestRedFrames != null && soulHarvestRedFrames.Length > 0)
                return soulHarvestRedFrames;
            if (resolvedSoulHarvestRedFrames != null && resolvedSoulHarvestRedFrames.Length > 0)
                return resolvedSoulHarvestRedFrames;

            List<Sprite> loaded = new List<Sprite>();
#if UNITY_EDITOR
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SoulHarvestRedSheetAssetPath);
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Sprite sprite)
                        loaded.Add(sprite);
                }
            }
#endif
            if (loaded.Count == 0)
            {
                Sprite[] resourceSprites = Resources.LoadAll<Sprite>(SoulHarvestRedSheetResourcePath);
                if (resourceSprites != null && resourceSprites.Length > 0)
                    loaded.AddRange(resourceSprites);
            }

            if (loaded.Count > 1)
                loaded.Sort(CompareSpriteFrameOrder);

            resolvedSoulHarvestRedFrames = loaded.ToArray();
            if (resolvedSoulHarvestRedFrames.Length == 0)
            {
                Debug.LogWarning(
                    $"MainProgress failed to load Soul Harvest orb frames from {SoulHarvestRedSheetAssetPath}.",
                    this);
            }

            return resolvedSoulHarvestRedFrames;
        }

        static int CompareSpriteFrameOrder(Sprite a, Sprite b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            int indexA = ExtractTrailingFrameIndex(a.name);
            int indexB = ExtractTrailingFrameIndex(b.name);
            if (indexA != indexB)
                return indexA.CompareTo(indexB);
            return string.CompareOrdinal(a.name, b.name);
        }

        static int ExtractTrailingFrameIndex(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return int.MaxValue;

            int end = spriteName.Length - 1;
            while (end >= 0 && !char.IsDigit(spriteName[end]))
                end--;
            if (end < 0)
                return int.MaxValue;

            int start = end;
            while (start > 0 && char.IsDigit(spriteName[start - 1]))
                start--;

            return int.TryParse(spriteName.Substring(start, end - start + 1),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : int.MaxValue;
        }

        // Ánh xạ loại ngọc -> hiệu ứng quả cầu Soul Harvest.
        // Hiện mới nối sheet thật SoulHarvest_Red cho viên đỏ (GemType.Attack / Lửa Đỏ).
        // Các viên khác TẠM dùng chung sheet đỏ + tint nhân màu bằng Image.color; sẽ thay bằng sheet màu riêng sau.
        SoulHarvestOrbStyle ResolveSoulHarvestOrbStyle(GemType gemType)
        {
            Sprite[] redFrames = ResolveSoulHarvestRedFrames();
            switch (gemType)
            {
                case GemType.Attack:
                    return new SoulHarvestOrbStyle(redFrames, Color.white);
                case GemType.Mana:
                    return new SoulHarvestOrbStyle(redFrames, new Color32(94, 167, 255, 255));
                case GemType.ExpDrain:
                    return new SoulHarvestOrbStyle(redFrames, new Color32(195, 130, 255, 255));
                case GemType.Rage:
                    return new SoulHarvestOrbStyle(redFrames, new Color32(255, 215, 110, 255));
                case GemType.Health:
                    return new SoulHarvestOrbStyle(redFrames, new Color32(100, 235, 155, 255));
                case GemType.Shield:
                    return new SoulHarvestOrbStyle(redFrames, new Color32(156, 218, 240, 255));
                case GemType.Soul:
                    return new SoulHarvestOrbStyle(redFrames, new Color32(230, 235, 245, 255));
                default:
                    return new SoulHarvestOrbStyle(redFrames, Color.white);
            }
        }

        void RefreshSoulHarvestOrbVisual()
        {
            if (soulHarvestOrbImage == null)
                return;

            int branchIndex = Mathf.Clamp(selectedStarChartBranchIndex, 0, StarChartGemTypes.Length - 1);
            SoulHarvestOrbStyle style = ResolveSoulHarvestOrbStyle(StarChartGemTypes[branchIndex]);
            activeSoulHarvestOrbFrames = style.Frames;
            soulHarvestOrbImage.color = style.TintColor;

            if (activeSoulHarvestOrbFrames != null && activeSoulHarvestOrbFrames.Length > 0)
            {
                int frameIndex = soulHarvestOrbFrameIndex >= 0
                    ? soulHarvestOrbFrameIndex % activeSoulHarvestOrbFrames.Length
                    : 0;
                soulHarvestOrbFrameIndex = frameIndex;
                soulHarvestOrbImage.sprite = activeSoulHarvestOrbFrames[frameIndex];
                soulHarvestOrbImage.enabled = true;
            }
            else
            {
                soulHarvestOrbImage.enabled = false;
            }
        }

        void UpdateSoulHarvestOrbAnimation()
        {
            if (soulHarvestOrbImage == null
                || !soulHarvestOrbImage.gameObject.activeInHierarchy
                || activeSoulHarvestOrbFrames == null
                || activeSoulHarvestOrbFrames.Length == 0)
                return;

            soulHarvestOrbElapsed += Time.unscaledDeltaTime;
            int frameIndex = Mathf.FloorToInt(soulHarvestOrbElapsed * SoulHarvestOrbFrameRate)
                % activeSoulHarvestOrbFrames.Length;
            if (frameIndex != soulHarvestOrbFrameIndex)
            {
                soulHarvestOrbFrameIndex = frameIndex;
                soulHarvestOrbImage.sprite = activeSoulHarvestOrbFrames[frameIndex];
            }
        }

        void OpenSelectedBranchUpgradePopup()
        {
            int branchIndex = Mathf.Clamp(selectedStarChartBranchIndex, 0, StarChartGemTypes.Length - 1);
            int nextStar = litStarsPerBranch[branchIndex];
            if (nextStar >= MainProgressChartGraphic.StarsPerBranch)
            {
                int maxStarIndex = MainProgressChartGraphic.StarsPerBranch - 1;
                UpdateStarActionDetails();
                SetStarChartStatus("Nhánh này đã thắp đủ 10 sao · "
                    + StarChartStatDescriptions[branchIndex] + ".");
                ShowStarNodePopup(branchIndex, maxStarIndex);
                return;
            }

            UpdateStarActionDetails();
            ShowStarNodePopup(branchIndex, nextStar);
        }

        void OpenTotalBuffPopup()
        {
            HideStarNodePopup();
            if (totalBuffPopup == null || totalBuffPopupText == null)
                return;

            StarChartStatBonuses totals = starChartGemInventory.GetTotalStatBonuses();
            string summary = BuildTotalBuffSummary(totals);
            totalBuffPopupText.text = summary;
            totalBuffPopup.SetActive(true);
            totalBuffPopup.transform.SetAsLastSibling();
        }

        void CloseTotalBuffPopup()
        {
            if (totalBuffPopup != null)
                totalBuffPopup.SetActive(false);
        }

        string BuildTotalBuffSummary(StarChartStatBonuses totals)
        {
            if (totals.AttackStars == 0 && totals.ManaStars == 0
                && totals.ExperienceStars == 0 && totals.RageStars == 0
                && totals.HealthStars == 0 && totals.ShieldStars == 0
                && totals.SoulStars == 0)
            {
                return "Chưa thắp sao nào\n\nLửa Đỏ: 0\nGiọt Nước: 0\nSen Tím: 0\nMặt Trời: 0\nLá Xanh: 0\nHộ Thuẫn: 0\nLửa Trắng: 0";
            }

            return string.Join("\n",
                new[]
                {
                    $"Lửa Đỏ: +{totals.AttackStars} ATK",
                    $"Giọt Nước: +{totals.ManaStars} Né · +{(totals.ManaStars * 0.5f).ToString("0.##", CultureInfo.InvariantCulture)} Chính xác · +{totals.ManaStars} Speed",
                    $"Sen Tím: +{totals.ExperienceStars} EXP / viên Sen Tím",
                    $"Mặt Trời: +{totals.RageStars}% Dame crit · +{totals.RageStars}% DEF",
                    $"Lá Xanh: +{totals.HealthStars * 50} HP",
                    $"Hộ Thuẫn: +{totals.ShieldStars * 10} giá trị gem Hộ Thuẫn",
                    $"Lửa Trắng: +{totals.SoulStars} ATK soul"
                });
        }

        void HideStarNodePopup()
        {
            if (starNodePopup != null)
                starNodePopup.gameObject.SetActive(false);
        }

        void HideStarChartPopups()
        {
            HideStarNodePopup();
            CloseTotalBuffPopup();
        }

        void ShowStarNodePopup(int branchIndex, int starIndex)
        {
            CloseTotalBuffPopup();
            if (starNodePopup == null || starNodePopupText == null)
                return;
            if (branchIndex < 0 || branchIndex >= StarChartGemNames.Length)
            {
                HideStarNodePopup();
                return;
            }

            popupStarBranchIndex = branchIndex;
            popupStarIndex = starIndex;
            int litCount = litStarsPerBranch[branchIndex];
            bool lit = starIndex < litCount;
            bool isNextStar = starIndex == litCount
                && litCount < MainProgressChartGraphic.StarsPerBranch;
            int available = starChartGemInventory.GetBaseGemCount(StarChartGemTypes[branchIndex]);
            int cost = isNextStar
                ? starChartGemInventory.GetStarCost(selectedStarChartRealmIndex, litCount)
                : 0;
            string status = lit ? "Đã thắp"
                : isNextStar ? "Sẵn sàng" : "Chưa tới lượt";
            string costLine = isNextStar
                ? $"\nChi phí: {cost} gem · Có {available}"
                : string.Empty;
            if (isNextStar && available < cost)
                status = "Chưa đủ gem";
            starNodePopupText.text =
                $"{StarChartGemNames[branchIndex]} · Sao {starIndex + 1}\n"
                + $"{status}\n{StarChartStatDescriptions[branchIndex]}{costLine}";
            starNodePopup.anchoredPosition = Vector2.zero;
            UpdateStarNodePopupUpgradeState(lit, isNextStar && available >= cost);
            starNodePopup.gameObject.SetActive(true);
            starNodePopup.SetAsLastSibling();
        }

        void UpdateStarNodePopupUpgradeState(bool isLit, bool canUpgrade)
        {
            if (starNodePopupUpgradeButton == null || starNodePopupUpgradeLabel == null)
                return;

            starNodePopupUpgradeButton.interactable = !isLit && canUpgrade;
            starNodePopupUpgradeLabel.text = isLit ? "Đã thắp" : "Nâng cấp";
            Color buttonColor = isLit || !canUpgrade
                ? new Color32(75, 79, 84, 235)
                : new Color32(142, 106, 45, 255);
            starNodePopupUpgradeButton.GetComponent<Image>().color = buttonColor;
            starNodePopupUpgradeLabel.color = isLit || !canUpgrade
                ? new Color32(170, 170, 170, 255)
                : Color.white;
        }

        void UpgradePopupStar()
        {
            int branchIndex = popupStarBranchIndex;
            int starIndex = popupStarIndex;
            if (branchIndex < 0 || branchIndex >= StarChartGemTypes.Length)
                return;

            if (starIndex != litStarsPerBranch[branchIndex]
                || litStarsPerBranch[branchIndex] >= MainProgressChartGraphic.StarsPerBranch)
            {
                ShowStarNodePopup(branchIndex, starIndex);
                return;
            }

            selectedStarChartBranchIndex = branchIndex;
            LightSelectedStar();
            ShowStarNodePopup(branchIndex, starIndex);
        }

        void AddChartImage(string objectName, Sprite sprite, Vector2 anchor, float size)
        {
            if (sprite == null)
                return;
            RectTransform imageRect = CreateAnchoredRect(starChartGraphicRect,
                objectName, anchor, anchor, Vector2.zero, Vector2.zero);
            imageRect.sizeDelta = new Vector2(size, size);
            Image image = imageRect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static Sprite GetSpriteAt(Sprite[] sprites, int index)
        {
            return sprites != null && index >= 0 && index < sprites.Length
                ? sprites[index] : null;
        }

        void SelectStarChartRealm(int realmIndex, int heroLevel)
        {
            selectedStarChartRealmIndex = Mathf.Clamp(realmIndex, 0, Realms.Length - 1);
            selectedStarChartLevel = heroLevel;
        }

        void SelectNextStarChartRealm()
        {
            int currentRealmIndex = GetCurrentHeroRealmIndex();
            if (selectedStarChartRealmIndex >= currentRealmIndex)
                return;
            int nextRealmIndex = selectedStarChartRealmIndex + 1;
            HeroProgressSaveData progress = LoadSelectedHeroProgress();
            int level = nextRealmIndex == currentRealmIndex && progress != null
                ? progress.level : Realms[nextRealmIndex].MaxLevel;
            SelectStarChartRealm(nextRealmIndex, level);
            RefreshRealmHeaderAndStars();
        }

        void SelectPreviousStarChartRealm()
        {
            int previousRealmIndex = selectedStarChartRealmIndex - 1;
            if (previousRealmIndex < 0
                || previousRealmIndex >= GetCurrentHeroRealmIndex()
                || !IsRealmMaxed(previousRealmIndex))
                return;
            SelectStarChartRealm(previousRealmIndex,
                Realms[previousRealmIndex].MaxLevel);
            RefreshRealmHeaderAndStars();
        }

        void RefreshRealmHeaderAndStars()
        {
            RealmDefinition realm = Realms[selectedStarChartRealmIndex];
            if (realmButtonLabel != null)
                realmButtonLabel.text = GetRealmButtonLabel();
            RefreshRealmNavigationButtons();
            RefreshSelectedRealmStars();
            Debug.Log($"[MainProgress] ViewingStarChartRealm={realm.Name} "
                + $"LevelRange={realm.MinLevel}-{realm.MaxLevel}.", this);
        }

        void TryOpenBreakthroughPopup()
        {
            HeroProgressSaveData progress = LoadSelectedHeroProgress();
            if (progress == null)
                return;

            int currentRealmIndex = FindRealmIndex(progress.level);
            if (currentRealmIndex >= Realms.Length - 1)
            {
                SetBreakthroughStatus("Đạo Tổ là cảnh giới tối đa, không còn đột phá.");
                return;
            }
            if (selectedStarChartRealmIndex != currentRealmIndex)
            {
                SetBreakthroughStatus($"Hãy chuyển về tab {Realms[currentRealmIndex].Name} "
                    + "để đột phá cảnh giới hiện tại.");
                return;
            }

            StarChartBreakthroughStatus status =
                starChartGemInventory.GetBreakthroughStatus(currentRealmIndex, progress);
            StarChartBreakthroughRequirement requirement = status.Requirement;
            int currentStandard = requirement.StandardDiamonds - status.MissingStandardDiamonds;
            int currentSpecial = requirement.SpecialDiamonds - status.MissingSpecialDiamonds;
            string standardLine = status.MissingStandardDiamonds > 0
                ? $"Kim cương Thường: {requirement.StandardDiamonds.ToString("N0", CultureInfo.InvariantCulture)} (có {currentStandard.ToString("N0", CultureInfo.InvariantCulture)}, thiếu {status.MissingStandardDiamonds.ToString("N0", CultureInfo.InvariantCulture)})"
                : $"Kim cương Thường: {requirement.StandardDiamonds.ToString("N0", CultureInfo.InvariantCulture)} (có {currentStandard.ToString("N0", CultureInfo.InvariantCulture)})";
            string specialLine = status.MissingSpecialDiamonds > 0
                ? $"Kim cương Đặc Biệt: {requirement.SpecialDiamonds.ToString("N0", CultureInfo.InvariantCulture)} (có {currentSpecial.ToString("N0", CultureInfo.InvariantCulture)}, thiếu {status.MissingSpecialDiamonds.ToString("N0", CultureInfo.InvariantCulture)})"
                : $"Kim cương Đặc Biệt: {requirement.SpecialDiamonds.ToString("N0", CultureInfo.InvariantCulture)} (có {currentSpecial.ToString("N0", CultureInfo.InvariantCulture)})";
            breakthroughRequirementLabel.text =
                $"{requirement.CurrentRealmName} Lv{requirement.RequiredLevel} → "
                + $"{requirement.NextRealmName} Lv{requirement.TargetLevel}\n"
                + $"{standardLine}\n"
                + $"{specialLine}\n"
                + $"Cấp độ: Lv{progress.level} / cần Lv{requirement.RequiredLevel} · Sao: {status.LitStarCount}/70";
            breakthroughPopup.SetActive(true);
            breakthroughPopup.transform.SetAsLastSibling();
            if (breakthroughConfirmButton != null)
            {
                breakthroughConfirmButton.interactable = status.CanBreakthrough;
                Image confirmImage = breakthroughConfirmButton.GetComponent<Image>();
                if (confirmImage != null)
                    confirmImage.color = status.CanBreakthrough
                        ? Gold
                        : new Color32(74, 74, 78, 255);
            }
        }

        void ConfirmBreakthrough()
        {
            HeroProgressSaveData progress = LoadSelectedHeroProgress();
            if (progress == null)
                return;

            int currentRealmIndex = FindRealmIndex(progress.level);
            if (currentRealmIndex >= Realms.Length - 1
                || currentRealmIndex != selectedStarChartRealmIndex
                || !starChartGemInventory.TryBreakthrough(currentRealmIndex,
                    progress, out StarChartBreakthroughStatus status))
            {
                breakthroughPopup.SetActive(false);
                SetBreakthroughStatus(currentRealmIndex < Realms.Length - 1
                    ? DescribeBreakthroughShortages(
                        starChartGemInventory.GetBreakthroughStatus(
                            currentRealmIndex, progress), progress.level)
                    : "Đạo Tổ là cảnh giới tối đa, không còn đột phá.");
                return;
            }

            breakthroughPopup.SetActive(false);
            RefreshBaseGemInventory();
            SelectStarChartRealm(currentRealmIndex + 1,
                status.Requirement.TargetLevel);
            RefreshRealmHeaderAndStars();
            SetBreakthroughStatus(
                $"Đột phá thành công · {status.Requirement.NextRealmName} "
                + $"Lv{status.Requirement.TargetLevel}.");
            UpdateBreakthroughButtonState();
        }

        void UpdateBreakthroughButtonState()
        {
            if (breakthroughButton == null)
                return;

            HeroProgressSaveData progress = LoadSelectedHeroProgress();
            if (progress == null)
            {
                breakthroughButton.interactable = false;
                ConfigureOutlinedButton(breakthroughButton, new Color32(45, 48, 54, 245),
                    new Color32(110, 115, 125, 255), 2.5f);
                if (breakthroughButtonLabel != null)
                    breakthroughButtonLabel.color = new Color32(150, 150, 150, 255);
                return;
            }

            int currentRealmIndex = FindRealmIndex(progress.level);
            bool enabled = selectedStarChartRealmIndex == currentRealmIndex
                && currentRealmIndex < Realms.Length - 1
                && starChartGemInventory.GetBreakthroughStatus(currentRealmIndex, progress).StarsReady;
            breakthroughButton.interactable = enabled;
            if (enabled)
            {
                ConfigureOutlinedButton(breakthroughButton, new Color32(28, 35, 44, 245), Gold, 3.5f);
                if (breakthroughGlowImage != null)
                    breakthroughGlowImage.color = new Color32(255, 214, 100, 120);
                if (breakthroughButtonLabel != null)
                    breakthroughButtonLabel.color = Color.white;
            }
            else
            {
                ConfigureOutlinedButton(breakthroughButton, new Color32(45, 48, 54, 245),
                    new Color32(180, 150, 90, 255), 3f);
                if (breakthroughGlowImage != null)
                    breakthroughGlowImage.color = new Color32(231, 190, 110, 48);
                if (breakthroughButtonLabel != null)
                    breakthroughButtonLabel.color = new Color32(185, 185, 185, 255);
            }
        }

        string DescribeBreakthroughShortages(StarChartBreakthroughStatus status,
            int currentLevel)
        {
            List<string> shortages = new List<string>();
            if (!status.LevelReady)
                shortages.Add($"level cần Lv{status.Requirement.RequiredLevel}, hiện Lv{currentLevel}");
            if (!status.StarsReady)
                shortages.Add($"sao cần 70/70, hiện {status.LitStarCount}/70");
            if (status.MissingStandardDiamonds > 0)
                shortages.Add($"thiếu {status.MissingStandardDiamonds.ToString("N0", CultureInfo.InvariantCulture)} "
                    + "kim cương Thường");
            if (status.MissingSpecialDiamonds > 0)
                shortages.Add($"thiếu {status.MissingSpecialDiamonds.ToString("N0", CultureInfo.InvariantCulture)} "
                    + "kim cương Đặc Biệt");
            return "Chưa đủ điều kiện đột phá: " + string.Join(" · ", shortages) + ".";
        }

        void SetBreakthroughStatus(string message)
        {
            SetStarChartStatus(message);
            Debug.Log($"[MainProgress] {message}", this);
        }

        void SetStarChartStatus(string message)
        {
            if (starActionStatus == null)
                return;
            starActionStatus.text = message;
            RefreshStarChartDetailsLayout();
        }

        string GetRealmButtonLabel()
        {
            RealmDefinition realm = Realms[selectedStarChartRealmIndex];
            return $"{realm.Name} • "
                + GetCultivationPeriod(selectedStarChartRealmIndex, selectedStarChartLevel);
        }

        int GetCurrentHeroRealmIndex()
        {
            HeroProgressSaveData progress = LoadSelectedHeroProgress();
            return progress == null ? 0 : FindRealmIndex(progress.level);
        }

        bool IsRealmMaxed(int realmIndex)
        {
            int totalStars = 0;
            foreach (int branchStars in starChartGemInventory.GetRealmStarCounts(realmIndex))
                totalStars += branchStars;
            return totalStars == 70;
        }

        void RefreshRealmNavigationButtons()
        {
            int currentRealmIndex = GetCurrentHeroRealmIndex();
            if (previousRealmButton != null)
                previousRealmButton.interactable = selectedStarChartRealmIndex > 0
                    && selectedStarChartRealmIndex - 1 < currentRealmIndex
                    && IsRealmMaxed(selectedStarChartRealmIndex - 1);
            if (nextRealmButton != null)
                nextRealmButton.interactable = selectedStarChartRealmIndex < currentRealmIndex;
        }

        static int FindRealmIndex(int level)
        {
            if (level <= 0)
                return 0;
            for (int index = 0; index < Realms.Length; index++)
                if (level >= Realms[index].MinLevel && level <= Realms[index].MaxLevel)
                    return index;
            return Realms.Length - 1;
        }

        static string GetCultivationPeriod(int realmIndex, int level)
        {
            RealmDefinition realm = Realms[realmIndex];
            if (level <= 0)
                return "Chưa có kỳ";
            if (realmIndex == 0)
            {
                if (level <= 3) return "Sơ kỳ";
                if (level <= 6) return "Trung kỳ";
                if (level <= 8) return "Hậu kỳ";
                return "Đại viên mãn";
            }

            int levelInRealm = level - realm.MinLevel;
            if (levelInRealm <= 2) return "Sơ kỳ";
            if (levelInRealm <= 5) return "Trung kỳ";
            if (levelInRealm <= 8) return "Hậu kỳ";
            return "Đại viên mãn / Đỉnh phong";
        }

        void SelectStarChartNode(int branchIndex, int starIndex)
        {
            if (branchIndex < 0)
            {
                HideStarNodePopup();
                return;
            }

            selectedStarChartBranchIndex = branchIndex;
            RefreshGemSlotSelectionVisuals();
            int nextStar = litStarsPerBranch[branchIndex];
            bool clickedLitStar = starIndex < nextStar;
            if (nextStar >= MainProgressChartGraphic.StarsPerBranch)
            {
                starChartGraphic.SetSelectedStar(branchIndex,
                    clickedLitStar ? starIndex : MainProgressChartGraphic.StarsPerBranch - 1);
                UpdateStarActionDetails();
                SetStarChartStatus($"Nhánh này đã thắp đủ 10 sao · "
                    + StarChartStatDescriptions[branchIndex] + ".");
                ShowStarNodePopup(branchIndex,
                    clickedLitStar ? starIndex : MainProgressChartGraphic.StarsPerBranch - 1);
                return;
            }

            int displayedStar = clickedLitStar ? starIndex : nextStar;
            starChartGraphic.SetSelectedStar(branchIndex, displayedStar);
            UpdateStarActionDetails();
            SetStarChartStatus(clickedLitStar
                ? $"Sao đã thắp · {StarChartStatDescriptions[branchIndex]}."
                : starIndex == 0 && nextStar == 0
                    ? "Mốc 1 đang sáng để hiển thị, chưa tính là đã thắp. "
                        + "Bấm THẮP SAO để thắp thật và trừ gem."
                    : starIndex != nextStar
                        ? $"Thắp theo thứ tự · sao kế tiếp là nốt {nextStar + 1}."
                        : "Sẵn sàng thắp sao này.");
            ShowStarNodePopup(branchIndex, displayedStar);
        }

        void RefreshSelectedRealmStars()
        {
            int[] savedCounts = starChartGemInventory.GetRealmStarCounts(
                selectedStarChartRealmIndex);
            System.Array.Copy(savedCounts, litStarsPerBranch, litStarsPerBranch.Length);
            if (starChartGraphic != null)
            {
                starChartGraphic.SetLitStarCounts(litStarsPerBranch);
                int starIndex = Mathf.Min(litStarsPerBranch[selectedStarChartBranchIndex],
                    MainProgressChartGraphic.StarsPerBranch - 1);
                starChartGraphic.SetSelectedStar(selectedStarChartBranchIndex, starIndex);
            }
            RefreshGemSlotSelectionVisuals();
            RefreshRealmProgressBar();
            RefreshSoulHarvestOrbVisual();
            UpdateStarActionDetails();
            SetStarChartStatus(
                "Bấm vào tâm quả cầu để xem Lv và nâng cấp sao.");
            UpdateBreakthroughButtonState();
        }

        void UpdateStarActionDetails()
        {
            if (starActionHint == null)
                return;
            int litCount = litStarsPerBranch[selectedStarChartBranchIndex];
            string branchName = StarChartGemNames[selectedStarChartBranchIndex];
            if (litCount >= MainProgressChartGraphic.StarsPerBranch)
            {
                starActionHint.text = $"{branchName} · Đã thắp đủ 10/10 sao · "
                    + StarChartStatDescriptions[selectedStarChartBranchIndex];
                RefreshStarChartDetailsLayout();
                return;
            }

            int cost = starChartGemInventory.GetStarCost(selectedStarChartRealmIndex,
                litCount);
            int available = starChartGemInventory.GetBaseGemCount(
                StarChartGemTypes[selectedStarChartBranchIndex]);
            starActionHint.text = $"{branchName} · Sao {litCount + 1}/10 · "
                + $"{StarChartStatDescriptions[selectedStarChartBranchIndex]}\n"
                + $"Chi phí {cost} gem · Có {available}";
            RefreshStarChartDetailsLayout();
        }

        void LightSelectedStar()
        {
            GemType gemType = StarChartGemTypes[selectedStarChartBranchIndex];
            int nextStar = litStarsPerBranch[selectedStarChartBranchIndex];
            if (nextStar >= MainProgressChartGraphic.StarsPerBranch)
            {
                SetStarChartStatus("Nhánh này đã thắp đủ 10 sao.");
                return;
            }

            bool lit = starChartGemInventory.TryLightNextStar(
                selectedStarChartRealmIndex, gemType, out int cost,
                out int missingGems);
            if (!lit)
            {
                SetStarChartStatus(missingGems > 0
                    ? $"Chưa đủ gem · thiếu {missingGems} {StarChartGemNames[selectedStarChartBranchIndex]}."
                    : "Không thể thắp thêm sao trong nhánh này.");
                return;
            }

            RefreshBaseGemInventory();
            RefreshSelectedRealmStars();
            SetStarChartStatus($"Đã thắp sao {nextStar + 1} · đã trừ {cost} "
                + $"{StarChartGemNames[selectedStarChartBranchIndex]}.");
        }

        void SelectCharacterTab(int tabIndex)
        {
            selectedCharacterTab = Mathf.Clamp(tabIndex, 0, 3);
            HideStarChartPopups();
            if (characterTabFocusRect != null)
            {
                characterTabFocusRect.anchorMin = CharacterTabFocusAnchors[selectedCharacterTab].min;
                characterTabFocusRect.anchorMax = CharacterTabFocusAnchors[selectedCharacterTab].max;
                characterTabFocusRect.offsetMin = Vector2.zero;
                characterTabFocusRect.offsetMax = Vector2.zero;
            }
            for (int index = 0; index < characterTabButtons.Length; index++)
                if (characterTabButtons[index] != null)
                    characterTabButtons[index].interactable = index != selectedCharacterTab;
            if (skillsPlaceholder != null)
                skillsPlaceholder.SetActive(selectedCharacterTab == 0);
            if (starChartUIGroup != null)
                starChartUIGroup.SetActive(selectedCharacterTab == 1);
            if (selectedCharacterTab == 1)
            {
                if (starChartGraphicRect != null)
                    starChartGraphicRect.gameObject.SetActive(false);
                RefreshBaseGemInventory();
                RefreshGemSlotSelectionVisuals();
                RefreshRealmProgressBar();
                RefreshSoulHarvestOrbVisual();
                UpdateBreakthroughButtonState();
            }
        }

        RectTransform CreateOverlay(string name)
        {
            RectTransform overlay = CreateAnchoredRect(contentRoot, name,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, .78f);
            backdrop.raycastTarget = true;
            overlay.SetAsLastSibling();
            return overlay;
        }

        RectTransform CreatePanelCard(Transform parent, string name)
        {
            RectTransform panel = CreateAnchoredRect(parent, name,
                new Vector2(.035f, .025f), new Vector2(.965f, .975f),
                Vector2.zero, Vector2.zero);
            SetImage(panel.gameObject, new Color32(24, 30, 40, 255), false);
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);
            return panel;
        }

        Button CreatePanelButton(Transform parent, string name, string label,
            Vector2 size, Vector2 anchorPosition, Color color)
        {
            Button button = CampaignUIFactory.CreateButton(parent, name, label, size,
                color, Color.white, 24);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = anchorPosition;
            rect.anchoredPosition = Vector2.zero;
            return button;
        }

        string GetPlayerName()
        {
            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager.CurrentSave ?? saveManager.LoadOrCreate();
            if (save != null && !string.IsNullOrWhiteSpace(save.playerName))
                return save.playerName;
            return GameSaveData.DefaultPlayerName;
        }

        void RefreshBannerInfo()
        {
            if (bannerPlayerNameLabel != null)
                bannerPlayerNameLabel.text = GetPlayerName();

            if (bannerLevelExpRealmLabel != null)
            {
                HeroProgressSaveData progress = LoadSelectedHeroProgress();
                int level = progress != null ? Math.Max(1, progress.level) : 1;
                int exp = progress != null ? Math.Max(0, progress.experience) : 0;
                int threshold = HeroLevelProgression.GetExperienceToNextLevel(level);
                int expPercent = threshold > 0
                    ? Mathf.Clamp((int)Math.Round((double)exp / threshold * 100.0), 0, 100)
                    : 100;
                int realmIndex = FindRealmIndex(level);
                string realmName = Realms[realmIndex].Name;
                bannerLevelExpRealmLabel.text = $"Lv.{level} · {expPercent}% · Cảnh Giới: {realmName}";
            }
        }

        void OpenRenamePopup()
        {
            if (renamePopup == null)
                renamePopup = BuildRenamePopup(canvas.transform).gameObject;

            string currentName = GetPlayerName();
            if (renameInputField != null)
            {
                renameInputField.text = currentName;
                renameInputField.ActivateInputField();
            }

            renamePopup.SetActive(true);
            renamePopup.transform.SetAsLastSibling();
        }

        void CloseRenamePopup()
        {
            if (renamePopup != null)
                renamePopup.SetActive(false);
        }

        void ConfirmRename()
        {
            if (renameInputField == null)
                return;

            string entered = renameInputField.text != null ? renameInputField.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(entered))
                return;

            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager.CurrentSave ?? saveManager.LoadOrCreate();
            save.playerName = entered;
            saveManager.SaveNow();

            if (bannerPlayerNameLabel != null)
                bannerPlayerNameLabel.text = entered;

            CloseRenamePopup();
        }

        RectTransform BuildRenamePopup(Transform parent)
        {
            RectTransform popup = CreateAnchoredRect(parent, "RenamePopup",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdrop = popup.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, .82f);
            backdrop.raycastTarget = true;

            RectTransform card = CreateAnchoredRect(popup, "RenameCard",
                new Vector2(.10f, .36f), new Vector2(.90f, .64f),
                Vector2.zero, Vector2.zero);
            SetImage(card.gameObject, new Color32(24, 30, 40, 255), false);
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);

            AddLabel(card, "RenameTitle", "Đổi Tên", 32, Gold,
                new Vector2(.08f, .74f), new Vector2(.92f, .94f));

            Button closeX = CreatePanelButton(card, "RenameCloseX", "X",
                new Vector2(44f, 44f), new Vector2(.92f, .85f), new Color32(74, 54, 44, 255));
            closeX.onClick.AddListener(CloseRenamePopup);

            renameInputField = CreateRenameInputField(card, "RenameInputField",
                new Vector2(.10f, .44f), new Vector2(.90f, .66f));

            Button cancel = CreatePanelButton(card, "RenameCancel", "HỦY",
                new Vector2(170f, 54f), new Vector2(.28f, .18f), new Color32(74, 54, 44, 255));
            cancel.onClick.AddListener(CloseRenamePopup);

            Button confirm = CreatePanelButton(card, "RenameConfirm", "XÁC NHẬN",
                new Vector2(190f, 54f), new Vector2(.72f, .18f), Gold);
            confirm.onClick.AddListener(ConfirmRename);

            popup.gameObject.SetActive(false);
            return popup;
        }

        TMP_InputField CreateRenameInputField(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform inputRoot = CreateAnchoredRect(parent, name,
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image bg = inputRoot.gameObject.AddComponent<Image>();
            bg.color = new Color32(14, 18, 24, 255);
            Outline outline = inputRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = MutedGold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform textArea = CreateAnchoredRect(inputRoot, "Text Area",
                Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f));
            textArea.gameObject.AddComponent<RectMask2D>();

            RectTransform placeholderRect = CreateAnchoredRect(textArea, "Placeholder",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI placeholder = placeholderRect.gameObject.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Nhập tên người chơi...";
            placeholder.fontSize = 24;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            placeholder.raycastTarget = false;
            if (starChartFont != null)
                placeholder.font = starChartFont;
            else if (TMP_Settings.defaultFontAsset != null)
                placeholder.font = TMP_Settings.defaultFontAsset;

            RectTransform textRect = CreateAnchoredRect(textArea, "Text",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI textComponent = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 24;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.raycastTarget = false;
            if (starChartFont != null)
                textComponent.font = starChartFont;
            else if (TMP_Settings.defaultFontAsset != null)
                textComponent.font = TMP_Settings.defaultFontAsset;

            TMP_InputField inputField = inputRoot.gameObject.AddComponent<TMP_InputField>();
            inputField.textViewport = textArea;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholder;
            inputField.fontAsset = textComponent.font;
            inputField.pointSize = 24;
            inputField.characterLimit = 20;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.interactable = true;
            inputField.targetGraphic = bg;
            inputField.caretColor = Gold;
            inputField.selectionColor = new Color32(231, 190, 110, 80);
            inputField.onSubmit.AddListener(_ => ConfirmRename());

            return inputField;
        }

        static RectTransform CreateAnchoredRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            return CampaignUIFactory.CreateRect(name, parent, anchorMin, anchorMax,
                offsetMin, offsetMax);
        }

        static TMP_Text AddLabel(Transform parent, string name, string value, int fontSize,
            Color color, Vector2 anchorMin, Vector2 anchorMax,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = CampaignUIFactory.CreateRect(name, parent, anchorMin,
                anchorMax, Vector2.zero, Vector2.zero);
            return CampaignUIFactory.CreateText(rect, "Text", value, fontSize, color,
                alignment);
        }

        static TMP_Text CreateAnchoredLabel(Transform parent, string name,
            string value, int fontSize, Color color, Vector2 anchorMin,
            Vector2 anchorMax, out TMP_Text text,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            text = AddLabel(parent, name, value, fontSize, color,
                anchorMin, anchorMax, alignment);
            return text;
        }

        static void SetImage(GameObject target, Color color, bool raycastTarget)
        {
            Image image = target.GetComponent<Image>();
            if (image == null)
                image = target.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
        }

        RuntimeHeroStats? LoadSelectedHeroStats()
        {
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
            {
                Debug.LogError("GameConfigService is missing at Resources/Configuration/GameConfigService.", this);
                return null;
            }
            CampaignCatalog catalog = Resources.Load<CampaignCatalog>(
                "Campaign/CampaignCatalog");
            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager.LoadOrCreate(catalog);
            PlayerLoadoutService loadoutService = new PlayerLoadoutService(saveManager, config);
            HeroData hero = loadoutService.ResolveBattleLoadout().Hero;
            if (hero == null)
            {
                Debug.LogError("Cannot show MainProgress hero stats: selected HeroData is missing.", this);
                return null;
            }

            HeroProgressSaveData progress = PlayerHeroSelectionService.GetHeroProgress(
                save, hero.StableId);
            ItemStatValues equipmentStats = new ItemStatValues(0, 0);
            EquipmentService equipmentService = new EquipmentService(saveManager, config);
            if (equipmentService.ValidateEquipmentLoadout(out _))
                equipmentStats = ItemStatCalculator.CalculateEquipped(save.loadout,
                    save.inventory, config.GetItem);
            return new RuntimeHeroStats(hero, progress, equipmentStats.AttackBonus,
                equipmentStats.MaxHealthBonus,
                starChartGemInventory.GetTotalStatBonuses(),
                equipmentStats.AttackPercentBonus,
                equipmentStats.MaxHealthPercentBonus,
                equipmentStats.DefensePercentBonus,
                equipmentStats.CritRatePercentBonus,
                equipmentStats.SpeedBonus,
                equipmentStats.CritDamagePercentBonus);
        }

        HeroProgressSaveData LoadSelectedHeroProgress()
        {
            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
            {
                Debug.LogError("GameConfigService is missing at Resources/Configuration/GameConfigService.", this);
                return null;
            }

            CampaignCatalog catalog = Resources.Load<CampaignCatalog>(
                "Campaign/CampaignCatalog");
            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager.LoadOrCreate(catalog);
            PlayerLoadoutService loadoutService = new PlayerLoadoutService(
                saveManager, config);
            HeroData hero = loadoutService.ResolveBattleLoadout().Hero;
            if (hero == null)
            {
                Debug.LogError("Cannot resolve selected HeroData for breakthrough.", this);
                return null;
            }

            HeroProgressSaveData progress = PlayerHeroSelectionService.GetHeroProgress(
                save, hero.StableId);
            if (progress != null)
                return progress;

            progress = new HeroProgressSaveData { heroId = hero.StableId, level = 1 };
            save.heroes.Add(progress);
            saveManager.SaveNow();
            return progress;
        }

        void EnsureSceneCamera()
        {
            if (Camera.main != null)
                return;

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.tag = "MainCamera";
        }
    }
}
