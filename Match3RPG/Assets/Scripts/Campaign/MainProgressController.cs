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

namespace Match3RPG.Campaign
{
    public sealed class MainProgressController : MonoBehaviour
    {
        const bool DebugHitboxVisualizationEnabled = false;

        [SerializeField] MainProgressCharacterPresentation presentation;
        [SerializeField] TMP_FontAsset starChartFont;
        [SerializeField] Sprite characterStatusBackground;
        [SerializeField] Sprite[] inventoryScreenBackgrounds = new Sprite[5];
        [SerializeField] Sprite inventoryItemSlot;
        [SerializeField] Sprite[] inventoryItemSprites = new Sprite[4];
        [SerializeField] Sprite[] inventoryItemPopupBackgrounds = new Sprite[3];
        [SerializeField] Sprite[] starChartBaseGemSprites = new Sprite[7];
        [SerializeField] Sprite[] starChartSpecialGemSprites = new Sprite[7];

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
        GameObject skillsPlaceholder;
        GameObject starChartUIGroup;
        GameObject starChartContent;
        Image inventoryBackgroundImage;
        Image cultivationBackgroundImage;
        Image inventoryPopupBackgroundImage;
        Image inventoryPopupItemImage;
        TMP_Text inventoryPopupItemName;
        TMP_Text inventoryPopupItemEffect;
        Button skillsTabButton;
        Button starChartTabButton;
        Button[] inventoryTabButtons = new Button[3];
        readonly Button[] inventoryItemButtons = new Button[4];
        readonly GameObject[] inventoryItemContents = new GameObject[4];
        readonly GameObject[] inventoryItemNameLabels = new GameObject[4];
        TMP_Text realmButtonLabel;
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
        RectTransform starChartGraphicRect;
        ScrollRect starChartZoomScrollRect;
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
        readonly TMP_Text[] characterStatValueLabels = new TMP_Text[12];
        int selectedStarChartRealmIndex;
        int selectedStarChartLevel;
        int selectedInventoryTab;

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
            BuildUI();
            ApplyStaticCharacterPresentation();
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

            RectTransform backgroundLayer = CampaignUIFactory.CreateRect("BackgroundLayer", contentRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backgroundImage = backgroundLayer.gameObject.AddComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = true;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.sprite = presentation != null ? presentation.ProgressBackground : null;
            backgroundImage.color = Color.white;

            if (backgroundImage.sprite == null)
                backgroundImage.color = new Color32(14, 19, 20, 255);

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

            meditationVisual = CreateVisualImage("MeditationCharacter", meditationAnchor, presentation != null ? presentation.MainProgressCharacterSprite : null);
            meditationVisual.raycastTarget = false;
            meditationVisual.preserveAspect = true;
            meditationVisual.color = Color.white;

            CreateNavigationButtons();
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
            meditationVisual.sprite = presentation.MainProgressCharacterSprite;
            meditationVisual.color = Color.white;
            meditationVisual.rectTransform.localScale = Vector3.one * finalScale;
            meditationVisual.rectTransform.anchoredPosition = presentation.MainProgressCharacterOffset;
        }

        Image CreateVisualImage(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = CampaignUIFactory.CreateRect(name, parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = new Vector2(420f, 420f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            return image;
        }

        void CreateNavigationButtons()
        {
            RectTransform navLayer = CampaignUIFactory.CreateRect("NavigationHitAreas", contentRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            navLayer.SetAsLastSibling();

            mainNavigationButtons.Add(CreateNavigationHitArea(navLayer, "StatusHitArea",
                new Vector2(.08f, .12f), new Vector2(.30f, .28f), Color.red,
                OpenCharacterPanel));

            mainNavigationButtons.Add(CreateNavigationHitArea(navLayer, "CultivationHitArea",
                new Vector2(.35f, .12f), new Vector2(.65f, .28f), Color.green,
                OpenCultivationPanel));

            mainNavigationButtons.Add(CreateNavigationHitArea(navLayer, "AdventureHitArea", new Vector2(.68f, .12f), new Vector2(.92f, .28f), Color.blue,
                () =>
                {
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
            SetPanelOpen(characterPanel);
        }

        void OpenCultivationPanel()
        {
            if (cultivationPanel == null)
                cultivationPanel = BuildCultivationPanel().gameObject;
            RefreshBaseGemInventory();
            SetPanelOpen(cultivationPanel);
            SelectCultivationTab(false);
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
        }

        RectTransform BuildCharacterPanel()
        {
            RectTransform overlay = CreateAnchoredRect(canvas.transform,
                "CharacterPanelOverlay", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            Image backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, .78f);
            backdrop.raycastTarget = true;
            RectTransform artworkRect = CreateAnchoredRect(overlay, "CharacterArtwork",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image artworkImage = artworkRect.gameObject.AddComponent<Image>();
            if (characterStatusBackground == null)
                Debug.LogError("CharacterStatus_BG sprite is not assigned on MainProgressController.", this);
            artworkImage.sprite = characterStatusBackground;
            artworkImage.preserveAspect = false;
            artworkImage.raycastTarget = false;
            overlay.SetAsLastSibling();

            RectTransform heroRect = CreateAnchoredRect(artworkRect, "CharacterPortrait",
                new Vector2(.29f, .65f), new Vector2(.71f, .87f),
                Vector2.zero, Vector2.zero);
            Image heroImage = heroRect.gameObject.AddComponent<Image>();
            heroImage.sprite = presentation != null ? presentation.MainProgressCharacterSprite : null;
            heroImage.preserveAspect = true;
            heroImage.color = heroImage.sprite != null
                ? Color.white : new Color(1f, 1f, 1f, .15f);
            heroImage.raycastTarget = false;

            AddEquipmentSlot(artworkRect, "Mũ", .105f, .81f, .28f, .877f);
            AddEquipmentSlot(artworkRect, "Giáp", .105f, .716f, .28f, .783f);
            AddEquipmentSlot(artworkRect, "Giày", .105f, .619f, .28f, .687f);
            AddEquipmentSlot(artworkRect, "Vũ khí", .718f, .81f, .893f, .877f);
            AddEquipmentSlot(artworkRect, "Pháp bảo", .718f, .716f, .893f, .783f);
            AddEquipmentSlot(artworkRect, "Nhẫn", .718f, .619f, .893f, .687f);

            const float closeMinX = .865f;
            const float closeMaxX = .982f;
            const float closeMinY = .895f;
            const float closeMaxY = .955f;
            CreateCharacterHitButton(artworkRect, "CloseButton",
                closeMinX, closeMinY, closeMaxX, closeMaxY,
                () => ClosePanel(overlay.gameObject));

            const float firstStatRowCenter = .519f;
            const float statRowSpacing = .0275f;
            const float statRowHeight = .0275f;
            const float lowerStatRowsOffset = 6f / 1920f;
            for (int index = 0; index < characterStatValueLabels.Length; index++)
            {
                float centerY = firstStatRowCenter - index * statRowSpacing;
                if (index >= 4)
                    centerY -= lowerStatRowsOffset;
                characterStatValueLabels[index] = AddLabel(artworkRect,
                    $"CharacterStatValue_{index}", "—", 24, Color.white,
                    new Vector2(.70f, centerY - statRowHeight * .5f),
                    new Vector2(.91f, centerY + statRowHeight * .5f),
                    TextAlignmentOptions.MidlineRight);
            }

            CreateCharacterHitButton(artworkRect, "InventoryButton",
                .31f, .10f, .69f, .15f,
                () => OpenInventoryPanel(overlay.gameObject));
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        void AddEquipmentSlot(Transform parent, string slotName,
            float minX, float minY, float maxX, float maxY)
        {
            CreateCharacterHitButton(parent, $"Equipment_{slotName}",
                minX, minY, maxX, maxY,
                () => Debug.Log($"[MainProgress] Clicked=EquipmentSlot:{slotName}"));
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
            int rageStars = starChartGemInventory.GetTotalStatBonuses().RageStars;
            float baseDefense = 20f + (heroStats.Level - 1);
            int defense = Mathf.CeilToInt(baseDefense * (1f + .01f * rageStars));
            string[] values =
            {
                FormatStat(heroStats.Attack),
                FormatStat(heroStats.SoulAttack),
                FormatStat(defense),
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
            RectTransform artwork = CreateFullScreenArtwork(overlay,
                GetSpriteAt(inventoryScreenBackgrounds, 0), "InventoryArtwork",
                out inventoryBackgroundImage, fitToScreen: true);
            CreateCharacterHitButton(artwork, "InventoryCloseButton",
                .875f, .90f, .985f, .99f,
                () => ClosePanel(overlay.gameObject));

            inventoryTabButtons[0] = CreateCharacterHitButton(artwork,
                "InventoryTab_Items", .08f, .81f, .36f, .87f,
                () => SelectInventoryTab(0));
            inventoryTabButtons[1] = CreateCharacterHitButton(artwork,
                "InventoryTab_Fragment", .38f, .81f, .64f, .87f,
                () => SelectInventoryTab(1));
            inventoryTabButtons[2] = CreateCharacterHitButton(artwork,
                "InventoryTab_Materials", .66f, .81f, .93f, .87f,
                () => SelectInventoryTab(2));

            const int columns = 5;
            const int rows = 7;
            const float gridLeft = .10f;
            const float gridWidth = .80f;
            const float columnStep = gridWidth / columns;
            const float firstRowY = .755f;
            const float rowStep = .079f;
            for (int index = 0; index < columns * rows; index++)
            {
                int capturedIndex = index;
                int row = index / columns;
                int column = index % columns;
                float centerX = gridLeft + (column + .5f) * columnStep;
                float centerY = firstRowY - row * rowStep;
                RectTransform slotRect = CreateAnchoredRect(artwork,
                    $"InventorySlot_{index}",
                    new Vector2(centerX - .058f, centerY - .03f),
                    new Vector2(centerX + .058f, centerY + .03f),
                    Vector2.zero, Vector2.zero);
                Image slotImage = slotRect.gameObject.AddComponent<Image>();
                slotImage.sprite = inventoryItemSlot;
                slotImage.preserveAspect = true;
                slotImage.raycastTarget = true;
                Button slotButton = slotRect.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slotImage;
                slotButton.transition = Selectable.Transition.None;
                if (index < inventoryItemSprites.Length
                    && GetSpriteAt(inventoryItemSprites, index) != null)
                {
                    Sprite itemSprite = inventoryItemSprites[index];
                    RectTransform itemRect = CreateAnchoredRect(slotRect,
                        "ItemArtwork", new Vector2(.12f, .12f),
                        new Vector2(.88f, .88f), Vector2.zero, Vector2.zero);
                    Image itemImage = itemRect.gameObject.AddComponent<Image>();
                    itemImage.sprite = itemSprite;
                    itemImage.preserveAspect = true;
                    itemImage.raycastTarget = false;
                    TMP_Text itemName = AddLabel(artwork, $"InventoryItemName_{index}",
                        GetInventoryItemName(index), 12, Color.white,
                        new Vector2(centerX - columnStep * .48f, centerY - .052f),
                        new Vector2(centerX + columnStep * .48f, centerY - .032f));
                    slotButton.onClick.AddListener(
                        () => OpenInventoryItemPopup(capturedIndex));
                    inventoryItemButtons[index] = slotButton;
                    inventoryItemContents[index] = itemRect.gameObject;
                    inventoryItemNameLabels[index] = itemName.gameObject;
                }
            }

            overlay.gameObject.SetActive(false);
            return overlay;
        }

        void SelectInventoryTab(int tabIndex)
        {
            selectedInventoryTab = Mathf.Clamp(tabIndex, 0, 2);
            if (inventoryBackgroundImage != null)
                inventoryBackgroundImage.sprite = GetSpriteAt(inventoryScreenBackgrounds,
                    selectedInventoryTab);
            for (int index = 0; index < inventoryTabButtons.Length; index++)
                if (inventoryTabButtons[index] != null)
                    inventoryTabButtons[index].interactable = index != selectedInventoryTab;
            for (int index = 0; index < inventoryItemButtons.Length; index++)
            {
                if (inventoryItemButtons[index] != null)
                    inventoryItemButtons[index].interactable = selectedInventoryTab == 0;
                if (inventoryItemContents[index] != null)
                    inventoryItemContents[index].SetActive(selectedInventoryTab == 0);
                if (inventoryItemNameLabels[index] != null)
                    inventoryItemNameLabels[index].SetActive(selectedInventoryTab == 0);
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
            return itemIndex >= 0 && itemIndex < itemNames.Length
                ? itemNames[itemIndex] : string.Empty;
        }

        void OpenInventoryItemPopup(int itemIndex)
        {
            if (selectedInventoryTab != 0 || itemIndex < 0
                || itemIndex >= inventoryItemSprites.Length
                || GetSpriteAt(inventoryItemSprites, itemIndex) == null)
                return;

            if (inventoryItemPopup != null)
                Destroy(inventoryItemPopup);

            int popupType = GetInventoryItemPopupType(itemIndex);
            Sprite popupSprite = GetSpriteAt(inventoryItemPopupBackgrounds, popupType);
            RectTransform popup = CreateFullScreenOverlay(
                $"InventoryItemPopup_{GetInventoryItemName(itemIndex)}",
                new Color32(3, 19, 16, 230), inventoryPanel.transform);
            inventoryItemPopup = popup.gameObject;
            RectTransform artwork = CreateFullScreenArtwork(popup, popupSprite,
                "PopupArtwork", out inventoryPopupBackgroundImage);

            RectTransform itemImageRect = CreateAnchoredRect(artwork,
                "PopupItemArtwork", new Vector2(.105f, .665f),
                new Vector2(.365f, .845f), Vector2.zero, Vector2.zero);
            inventoryPopupItemImage = itemImageRect.gameObject.AddComponent<Image>();
            inventoryPopupItemImage.sprite = inventoryItemSprites[itemIndex];
            inventoryPopupItemImage.preserveAspect = true;
            inventoryPopupItemImage.raycastTarget = false;
            inventoryPopupItemName = AddLabel(artwork, "PopupItemName",
                GetInventoryItemName(itemIndex), 24, Color.white,
                new Vector2(.39f, .77f), new Vector2(.91f, .85f),
                TextAlignmentOptions.MidlineLeft);
            inventoryPopupItemEffect = AddLabel(artwork, "PopupItemEffect",
                "Tác dụng / chỉ số: NULL", 20, Color.white,
                new Vector2(.12f, .34f), new Vector2(.88f, .58f),
                TextAlignmentOptions.TopLeft);

            CreateCharacterHitButton(artwork, "PopupCloseButton",
                .865f, .875f, .985f, .99f,
                () => CloseInventoryItemPopup());
            if (popupType == 0)
            {
                CreateCharacterHitButton(artwork, "PopupUseButton",
                    .29f, .095f, .72f, .185f,
                    () => LogInventoryPopupAction("Sử dụng", itemIndex));
            }
            else if (popupType == 1)
            {
                CreateCharacterHitButton(artwork, "PopupEquipButton",
                    .12f, .09f, .49f, .18f,
                    () => LogInventoryPopupAction("Trang bị", itemIndex));
                CreateCharacterHitButton(artwork, "PopupEnhanceButton",
                    .51f, .09f, .88f, .18f,
                    () => LogInventoryPopupAction("Cường hóa", itemIndex));
            }
            else
            {
                CreateCharacterHitButton(artwork, "PopupEnhanceButton",
                    .12f, .09f, .49f, .18f,
                    () => LogInventoryPopupAction("Cường hóa", itemIndex));
                CreateCharacterHitButton(artwork, "PopupUnequipButton",
                    .51f, .09f, .88f, .18f,
                    () => LogInventoryPopupAction("Gỡ", itemIndex));
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

        RectTransform BuildCultivationPanel()
        {
            RectTransform overlay = CreateFullScreenOverlay("CultivationPanelOverlay",
                new Color32(4, 28, 22, 255));
            RectTransform artwork = CreateFullScreenArtwork(overlay,
                GetSpriteAt(inventoryScreenBackgrounds, 4), "CultivationArtwork",
                out cultivationBackgroundImage);
            CreateCharacterHitButton(artwork, "CultivationCloseButton",
                .875f, .90f, .985f, .99f,
                () => ClosePanel(overlay.gameObject));
            skillsTabButton = CreateCharacterHitButton(artwork, "SkillsTab",
                .105f, .82f, .49f, .88f,
                () => SelectCultivationTab(true));
            starChartTabButton = CreateCharacterHitButton(artwork, "StarChartTab",
                .51f, .82f, .90f, .88f,
                () => SelectCultivationTab(false));

            starChartUIGroup = CreateAnchoredRect(artwork, "StarChartUI",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            CreateGemInventoryRow(starChartUIGroup.transform);

            RuntimeHeroStats? stats = LoadSelectedHeroStats();
            int heroLevel = stats.HasValue
                ? Mathf.Clamp(stats.Value.Level, Realms[0].MinLevel,
                    Realms[Realms.Length - 1].MaxLevel)
                : 0;
            SelectStarChartRealm(FindRealmIndex(heroLevel), heroLevel);

            previousRealmButton = CreateCharacterHitButton(starChartUIGroup.transform, "PreviousRealm",
                .11f, .672f, .21f, .704f, SelectPreviousStarChartRealm);
            CreateAnchoredLabel(starChartUIGroup.transform, "PreviousRealmArrow", "‹", 24, Color.white,
                new Vector2(.11f, .672f), new Vector2(.21f, .704f), out TMP_Text previousRealmArrow);
            CreateAnchoredLabel(starChartUIGroup.transform, "RealmSelector", GetRealmButtonLabel(),
                24, Color.white, new Vector2(.22f, .672f),
                new Vector2(.78f, .704f), out realmButtonLabel);
            nextRealmButton = CreateCharacterHitButton(starChartUIGroup.transform, "NextRealm",
                .79f, .672f, .89f, .704f, SelectNextStarChartRealm);
            CreateAnchoredLabel(starChartUIGroup.transform, "NextRealmArrow", "›", 24, Color.white,
                new Vector2(.79f, .672f), new Vector2(.89f, .704f), out TMP_Text nextRealmArrow);
            ApplyStarChartTextStyle(previousRealmArrow, 28);
            ApplyStarChartTextStyle(realmButtonLabel, 24);
            ApplyStarChartTextStyle(nextRealmArrow, 28);
            RefreshRealmNavigationButtons();

            starChartContent = CreateAnchoredRect(starChartUIGroup.transform, "StarChartContent",
                new Vector2(.09f, .31f), new Vector2(.91f, .632f),
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
            SetStarChartZoom(1f);
            RefreshSelectedRealmStars();
            CreateStarChartArtwork();
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
            CreateCharacterHitButton(starChartUIGroup.transform, "LightStarButton",
                .10f, .085f, .47f, .155f, LightSelectedStar);
            CreateCharacterHitButton(starChartUIGroup.transform, "BreakthroughButton",
                .53f, .085f, .90f, .155f, TryOpenBreakthroughPopup);
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
            return popup;
        }

        void CreateGemInventoryRow(Transform parent)
        {
            RectTransform row = CreateAnchoredRect(parent, "BaseGemInventory",
                new Vector2(.105f, .71f), new Vector2(.895f, .805f),
                Vector2.zero, Vector2.zero);

            const int gemCount = 7;
            float slotWidth = 1f / gemCount;
            for (int index = 0; index < StarChartGemTypes.Length; index++)
            {
                float slotCenter = (index + .5f) * slotWidth;
                Sprite sprite = GetSpriteAt(starChartBaseGemSprites, index);
                if (sprite == null)
                {
                    Debug.LogError($"Tinh đồ thiếu sprite base gem {StarChartGemTypes[index]}.", this);
                    continue;
                }

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
            }
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
            totalBuffButton = CreateStarChartControlButton(parent, "TotalBuffButton",
                new Vector2(.313f, .25f), new Vector2(.517f, .292f), "Tổng chỉ số",
                20, OpenTotalBuffPopup);
            CreateStarChartControlButton(parent, "StarChartZoomOut",
                new Vector2(.528f, .25f), new Vector2(.602f, .292f), "−",
                32, () => SetStarChartZoom(currentStarChartZoom - .2f));
            CreateStarChartControlButton(parent, "StarChartZoomIn",
                new Vector2(.613f, .25f), new Vector2(.687f, .292f), "+",
                32, () => SetStarChartZoom(currentStarChartZoom + .2f));
        }

        Button CreateStarChartControlButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label, int fontSize,
            UnityEngine.Events.UnityAction onClick)
        {
            Button button = CreateCharacterHitButton(parent, name,
                anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y, onClick);
            Image image = button.GetComponent<Image>();
            image.color = new Color32(28, 35, 44, 245);
            image.raycastTarget = true;
            button.transition = Selectable.Transition.None;
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, -2f);
            TMP_Text labelText = AddLabel(parent, $"{name}Label", label, fontSize,
                Color.white, anchorMin, anchorMax, TextAlignmentOptions.Center);
            labelText.raycastTarget = false;
            return button;
        }

        void CreateStarNodePopup()
        {
            starNodePopup = CreateAnchoredRect(starChartGraphicRect,
                "StarNodePopup", new Vector2(.5f, .5f), new Vector2(.5f, .5f),
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
                new Vector2(.18f, .18f), new Vector2(.82f, .82f),
                Vector2.zero, Vector2.zero).gameObject;
            Image popupBackground = totalBuffPopup.AddComponent<Image>();
            popupBackground.color = new Color32(16, 20, 28, 230);
            Outline popupOutline = totalBuffPopup.AddComponent<Outline>();
            popupOutline.effectColor = Gold;
            popupOutline.effectDistance = new Vector2(3f, -3f);
            totalBuffPopupText = AddLabel(totalBuffPopup.transform, "TotalBuffPopupText",
                string.Empty, 20, Color.white,
                new Vector2(.06f, .08f), new Vector2(.94f, .88f),
                TextAlignmentOptions.TopLeft);
            ApplyStarChartTextStyle(totalBuffPopupText, 20);
            Button closeButton = CreatePanelButton(totalBuffPopup.transform, "TotalBuffCloseButton",
                "ĐÓNG", new Vector2(180f, 52f), new Vector2(.5f, .06f), new Color32(74, 54, 44, 255));
            closeButton.onClick.AddListener(CloseTotalBuffPopup);
            totalBuffPopup.SetActive(false);
        }

        void SetStarChartZoom(float targetZoom)
        {
            if (starChartGraphicRect == null || starChartContent == null)
                return;
            currentStarChartZoom = Mathf.Clamp(targetZoom, .6f, 2f);
            RectTransform viewport = starChartContent.GetComponent<RectTransform>();
            starChartGraphicRect.sizeDelta = viewport.rect.size * currentStarChartZoom;
            if (starChartZoomScrollRect != null)
            {
                starChartZoomScrollRect.StopMovement();
                starChartZoomScrollRect.normalizedPosition = Vector2.zero;
            }
        }

        void OpenTotalBuffPopup()
        {
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

        void ShowStarNodePopup(int branchIndex, int starIndex)
        {
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
            if (!status.CanBreakthrough)
            {
                SetBreakthroughStatus(DescribeBreakthroughShortages(status, progress.level));
                return;
            }

            StarChartBreakthroughRequirement requirement = status.Requirement;
            breakthroughRequirementLabel.text =
                $"{requirement.CurrentRealmName} Lv{requirement.RequiredLevel} → "
                + $"{requirement.NextRealmName} Lv{requirement.TargetLevel}\n\n"
                + $"Kim cương Thường: {requirement.StandardDiamonds.ToString("N0", CultureInfo.InvariantCulture)}\n"
                + $"Kim cương Đặc Biệt: {requirement.SpecialDiamonds.ToString("N0", CultureInfo.InvariantCulture)}\n"
                + "Tiến độ sao cảnh giới hiện tại: 70/70";
            breakthroughPopup.SetActive(true);
            breakthroughPopup.transform.SetAsLastSibling();
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
            UpdateStarActionDetails();
            SetStarChartStatus(
                "Mốc 1 sáng hiển thị trên cả 7 nhánh; tiến độ thật không đổi.");
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

        void SelectCultivationTab(bool showSkills)
        {
            if (skillsPlaceholder != null)
                skillsPlaceholder.SetActive(showSkills);
            if (starChartUIGroup != null)
                starChartUIGroup.SetActive(!showSkills);
            if (cultivationBackgroundImage != null)
                cultivationBackgroundImage.sprite = GetSpriteAt(
                    inventoryScreenBackgrounds, showSkills ? 3 : 4);
            if (skillsTabButton != null)
                skillsTabButton.interactable = !showSkills;
            if (starChartTabButton != null)
                starChartTabButton.interactable = showSkills;
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
                starChartGemInventory.GetTotalStatBonuses());
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
