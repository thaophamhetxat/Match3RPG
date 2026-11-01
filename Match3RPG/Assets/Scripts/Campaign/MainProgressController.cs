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
        GameObject cultivationPanel;
        GameObject skillsPlaceholder;
        GameObject starChartContent;
        Button skillsTabButton;
        Button starChartTabButton;
        TMP_Text realmButtonLabel;
        TMP_Text starActionStatus;
        TMP_Text starActionHint;
        TMP_Text breakthroughRequirementLabel;
        GameObject breakthroughPopup;
        RectTransform starChartGraphicRect;
        MainProgressChartGraphic starChartGraphic;
        StarChartGemInventoryService starChartGemInventory;
        readonly TMP_Text[] baseGemInventoryCountLabels = new TMP_Text[7];
        int selectedStarChartRealmIndex;
        int selectedStarChartLevel;
        bool selectedStarChartLevelIsHeroLevel;

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

        readonly struct RealmDefinition
        {
            public readonly string Name;
            public readonly string Description;
            public readonly int MinLevel;
            public readonly int MaxLevel;

            public RealmDefinition(string name, string description, int minLevel,
                int maxLevel)
            {
                Name = name;
                Description = description;
                MinLevel = minLevel;
                MaxLevel = maxLevel;
            }
        }

        static readonly RealmDefinition[] Realms =
        {
            new RealmDefinition("Luyện Khí", "tân thủ", 1, 9),
            new RealmDefinition("Trúc Cơ", "nhập môn", 10, 19),
            new RealmDefinition("Kết Đan", "cao thủ", 20, 29),
            new RealmDefinition("Nguyên Anh", "lão quái", 30, 39),
            new RealmDefinition("Hóa Thần", "bán thần", 40, 49),
            new RealmDefinition("Luyện Hư", "Linh Giới", 50, 59),
            new RealmDefinition("Hợp Thể", "Linh Giới", 60, 69),
            new RealmDefinition("Đại Thừa", "Linh Giới", 70, 79),
            new RealmDefinition("Độ Kiếp", "phi thăng", 80, 89),
            new RealmDefinition("Chân Tiên", string.Empty, 90, 99),
            new RealmDefinition("Kim Tiên", string.Empty, 100, 109),
            new RealmDefinition("Thái Ất", string.Empty, 110, 119),
            new RealmDefinition("Đại La", string.Empty, 120, 129),
            new RealmDefinition("Đạo Tổ", "max", 130, 139)
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
            RectTransform overlay = CreateOverlay("CharacterPanelOverlay");
            RectTransform panel = CreatePanelCard(overlay, "CharacterPanel");
            AddLabel(panel, "Title", "Nhân vật", 48, Gold,
                new Vector2(.2f, .93f), new Vector2(.8f, .99f));
            Button close = CreatePanelButton(panel, "CloseButton", "X",
                new Vector2(72f, 72f), new Vector2(.91f, .95f), Gold);
            close.onClick.AddListener(() => ClosePanel(overlay.gameObject));

            RectTransform heroRect = CreateAnchoredRect(panel, "CharacterPortrait",
                new Vector2(.5f, .48f), new Vector2(.5f, .48f), Vector2.zero, Vector2.zero);
            heroRect.sizeDelta = new Vector2(430f, 560f);
            Image heroImage = heroRect.gameObject.AddComponent<Image>();
            heroImage.sprite = presentation != null ? presentation.MainProgressCharacterSprite : null;
            heroImage.preserveAspect = true;
            heroImage.color = heroImage.sprite != null
                ? Color.white : new Color(1f, 1f, 1f, .15f);
            heroImage.raycastTarget = false;

            AddEquipmentSlot(panel, "Mũ", -.36f, .70f);
            AddEquipmentSlot(panel, "Giáp", -.36f, .49f);
            AddEquipmentSlot(panel, "Giày", -.36f, .28f);
            AddEquipmentSlot(panel, "Vũ khí", .36f, .70f);
            AddEquipmentSlot(panel, "Pháp bảo", .36f, .49f);
            AddEquipmentSlot(panel, "Nhẫn", .36f, .28f);

            RectTransform statsCard = CreateAnchoredRect(panel, "HeroStats",
                new Vector2(.06f, .105f), new Vector2(.94f, .235f),
                Vector2.zero, Vector2.zero);
            SetImage(statsCard.gameObject, new Color32(14, 20, 29, 238), false);
            Outline statsOutline = statsCard.gameObject.AddComponent<Outline>();
            statsOutline.effectColor = Gold;
            statsOutline.effectDistance = new Vector2(2f, -2f);
            RuntimeHeroStats? stats = LoadSelectedHeroStats();
            AddLabel(statsCard, "HP", stats.HasValue
                    ? $"HP  {stats.Value.MaxHP.ToString("N0", CultureInfo.InvariantCulture)}"
                    : "HP  —",
                28, Color.white, new Vector2(.02f, .48f), new Vector2(.34f, .90f));
            AddLabel(statsCard, "ATK", stats.HasValue
                    ? $"ATK  {stats.Value.Attack.ToString("N0", CultureInfo.InvariantCulture)}"
                    : "ATK  —",
                28, Color.white, new Vector2(.34f, .48f), new Vector2(.66f, .90f));
            AddLabel(statsCard, "DEF", "DEF  —", 28, Color.white,
                new Vector2(.66f, .48f), new Vector2(.98f, .90f));
            AddLabel(statsCard, "DefUnavailable", "Chưa có dữ liệu DEF",
                16, MutedGold, new Vector2(.54f, .08f), new Vector2(.98f, .48f));

            Button inventory = CreatePanelButton(panel, "InventoryButton", "Túi đồ",
                new Vector2(190f, 72f), new Vector2(.82f, .055f), Gold);
            inventory.onClick.AddListener(() => Debug.Log("[MainProgress] Clicked=Inventory"));
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        void AddEquipmentSlot(Transform parent, string slotName, float x, float y)
        {
            Button slot = CreatePanelButton(parent, $"Equipment_{slotName}", slotName,
                new Vector2(158f, 148f), new Vector2(.5f + x, y),
                new Color32(34, 41, 52, 245));
            Outline outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(170, 139, 83, 255);
            outline.effectDistance = new Vector2(2f, -2f);
            slot.onClick.AddListener(() =>
                Debug.Log($"[MainProgress] Clicked=EquipmentSlot:{slotName}"));
        }

        RectTransform BuildCultivationPanel()
        {
            RectTransform overlay = CreateOverlay("CultivationPanelOverlay");
            RectTransform panel = CreatePanelCard(overlay, "CultivationPanel");
            Button back = CreatePanelButton(panel, "BackButton", "←",
                new Vector2(76f, 72f), new Vector2(.09f, .95f), Gold);
            back.onClick.AddListener(() => ClosePanel(overlay.gameObject));
            AddLabel(panel, "Title", "Tinh đồ", 42, Gold,
                new Vector2(.2f, .93f), new Vector2(.8f, .99f));

            skillsTabButton = CreatePanelButton(panel, "SkillsTab", "SKILLS",
                new Vector2(300f, 68f), new Vector2(.34f, .87f),
                new Color32(39, 45, 54, 255));
            starChartTabButton = CreatePanelButton(panel, "StarChartTab", "STAR CHART",
                new Vector2(300f, 68f), new Vector2(.66f, .87f), Gold);
            skillsTabButton.onClick.AddListener(() => SelectCultivationTab(true));
            starChartTabButton.onClick.AddListener(() => SelectCultivationTab(false));

            CreateGemInventoryRow(panel);

            RuntimeHeroStats? stats = LoadSelectedHeroStats();
            int heroLevel = stats.HasValue
                ? Mathf.Clamp(stats.Value.Level, Realms[0].MinLevel,
                    Realms[Realms.Length - 1].MaxLevel)
                : 0;
            SelectStarChartRealm(FindRealmIndex(heroLevel), heroLevel, stats.HasValue);

            Button previousRealm = CreatePanelButton(panel, "PreviousRealm",
                "‹", new Vector2(76f, 76f), new Vector2(.19f, .715f),
                new Color32(34, 41, 52, 255));
            previousRealm.onClick.AddListener(SelectPreviousStarChartRealm);
            Button realmButton = CreatePanelButton(panel, "RealmSelector",
                GetRealmButtonLabel(), new Vector2(500f, 88f),
                new Vector2(.5f, .715f), new Color32(34, 41, 52, 255));
            realmButtonLabel = realmButton.GetComponentInChildren<TMP_Text>(true);
            realmButton.interactable = false;
            Button nextRealm = CreatePanelButton(panel, "NextRealm",
                "›", new Vector2(76f, 76f), new Vector2(.81f, .715f),
                new Color32(34, 41, 52, 255));
            nextRealm.onClick.AddListener(SelectNextStarChartRealm);
            AddLabel(panel, "RealmSelectorHint",
                "Chuyển tab cảnh giới · Tiến độ sao mỗi tab riêng",
                16, MutedGold, new Vector2(.10f, .679f), new Vector2(.90f, .708f));

            starChartContent = CreateAnchoredRect(panel, "StarChartContent",
                new Vector2(.04f, .29f), new Vector2(.96f, .67f),
                Vector2.zero, Vector2.zero).gameObject;
            SetImage(starChartContent, new Color32(14, 20, 29, 175), false);
            Outline chartOutline = starChartContent.AddComponent<Outline>();
            chartOutline.effectColor = new Color32(123, 102, 69, 220);
            chartOutline.effectDistance = new Vector2(1f, -1f);
            starChartGraphicRect = CreateAnchoredRect(starChartContent.transform,
                "ChartGraphic", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            starChartGraphicRect.gameObject.AddComponent<CanvasRenderer>();
            starChartGraphic = starChartGraphicRect.gameObject
                .AddComponent<MainProgressChartGraphic>();
            starChartGraphic.color = Color.white;
            starChartGraphic.StarClicked += SelectStarChartNode;
            RefreshSelectedRealmStars();
            CreateStarChartArtwork();

            skillsPlaceholder = CreateAnchoredRect(panel, "SkillsPlaceholder",
                new Vector2(.04f, .29f), new Vector2(.96f, .67f),
                Vector2.zero, Vector2.zero).gameObject;
            SetImage(skillsPlaceholder, new Color32(14, 20, 29, 245), false);
            AddLabel(skillsPlaceholder.transform, "Placeholder",
                "Kỹ năng chưa có gameplay.",
                28, MutedGold, new Vector2(.08f, .35f), new Vector2(.92f, .65f));

            RectTransform starActions = CreateAnchoredRect(panel, "StarActions",
                new Vector2(.06f, .055f), new Vector2(.94f, .255f),
                Vector2.zero, Vector2.zero);
            SetImage(starActions.gameObject, new Color32(14, 20, 29, 238), false);
            Outline actionOutline = starActions.gameObject.AddComponent<Outline>();
            actionOutline.effectColor = Gold;
            actionOutline.effectDistance = new Vector2(2f, -2f);
            AddLabel(starActions, "StarActionsTitle", "Thắp sao", 30, Gold,
                new Vector2(.05f, .70f), new Vector2(.95f, .96f));
            starActionHint = AddLabel(starActions, "StarActionsHint",
                "Chọn một nốt sao để xem chi phí.",
                18, MutedGold, new Vector2(.05f, .44f), new Vector2(.95f, .68f));
            UpdateStarActionDetails();
            starActionStatus = AddLabel(starActions, "StarActionStatus",
                "Chưa thay đổi dữ liệu sao.", 16, Color.white,
                new Vector2(.05f, .27f), new Vector2(.95f, .44f));
            Button reset = CreatePanelButton(starActions, "ResetButton", "RESET",
                new Vector2(150f, 58f), new Vector2(.18f, .12f),
                new Color32(74, 54, 44, 255));
            reset.onClick.AddListener(ResetStarsPlaceholder);
            Button light = CreatePanelButton(starActions, "LightStarButton", "THẮP SAO",
                new Vector2(190f, 58f), new Vector2(.50f, .12f), Gold);
            light.onClick.AddListener(LightSelectedStar);
            Button breakthrough = CreatePanelButton(starActions, "BreakthroughButton",
                "ĐỘT PHÁ", new Vector2(210f, 58f), new Vector2(.82f, .12f),
                new Color32(164, 112, 55, 255));
            breakthrough.onClick.AddListener(TryOpenBreakthroughPopup);
            breakthroughPopup = BuildBreakthroughPopup(panel).gameObject;
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
                new Vector2(.035f, .775f), new Vector2(.965f, .835f),
                Vector2.zero, Vector2.zero);
            SetImage(row.gameObject, new Color32(14, 20, 29, 210), false);

            for (int index = 0; index < StarChartGemTypes.Length; index++)
            {
                float slotWidth = 1f / StarChartGemTypes.Length;
                float slotCenter = (index + .5f) * slotWidth;
                Sprite sprite = GetSpriteAt(starChartBaseGemSprites, index);
                if (sprite == null)
                    Debug.LogError($"Tinh đồ thiếu sprite base gem {StarChartGemTypes[index]}.", this);
                else
                {
                    float iconX = slotCenter - slotWidth * .25f;
                    RectTransform iconRect = CreateAnchoredRect(row,
                        $"GemIcon_{StarChartGemTypes[index]}",
                        new Vector2(iconX, .57f),
                        new Vector2(iconX, .57f),
                        Vector2.zero, Vector2.zero);
                    iconRect.sizeDelta = new Vector2(42f, 42f);
                    Image icon = iconRect.gameObject.AddComponent<Image>();
                    icon.sprite = sprite;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }

                int gemCount = index < baseGemDisplayInventory.Length
                    ? baseGemDisplayInventory[index] : 0;
                baseGemInventoryCountLabels[index] = AddLabel(row,
                    $"GemCount_{StarChartGemTypes[index]}",
                    gemCount.ToString(CultureInfo.InvariantCulture), 20, Color.white,
                    new Vector2(slotCenter - slotWidth * .04f, .49f),
                    new Vector2(slotCenter + slotWidth * .46f, .84f));
                AddLabel(row, $"GemName_{StarChartGemTypes[index]}",
                    StarChartGemNames[index], 13, MutedGold,
                    new Vector2(index / (float)StarChartGemTypes.Length, .04f),
                    new Vector2((index + 1f) / StarChartGemTypes.Length, .40f));

                if (index < StarChartGemTypes.Length - 1)
                {
                    float separatorX = (index + 1f) / StarChartGemTypes.Length;
                    AddLabel(row, $"GemSeparator_{index}", "|", 18, MutedGold,
                        new Vector2(separatorX - .015f, .42f),
                        new Vector2(separatorX + .015f, .92f));
                }
            }
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

        void SelectStarChartRealm(int realmIndex, int heroLevel, bool isHeroLevel)
        {
            selectedStarChartRealmIndex = Mathf.Clamp(realmIndex, 0, Realms.Length - 1);
            selectedStarChartLevel = heroLevel;
            selectedStarChartLevelIsHeroLevel = isHeroLevel;
        }

        void SelectNextStarChartRealm()
        {
            if (selectedStarChartRealmIndex >= Realms.Length - 1)
                return;
            SelectStarChartRealm(selectedStarChartRealmIndex + 1,
                Realms[selectedStarChartRealmIndex + 1].MinLevel, false);
            RefreshRealmHeaderAndStars();
        }

        void SelectPreviousStarChartRealm()
        {
            if (selectedStarChartRealmIndex <= 0)
                return;
            int previousRealm = selectedStarChartRealmIndex - 1;
            SelectStarChartRealm(selectedStarChartRealmIndex - 1,
                Realms[previousRealm].MaxLevel, false);
            RefreshRealmHeaderAndStars();
        }

        void RefreshRealmHeaderAndStars()
        {
            RealmDefinition realm = Realms[selectedStarChartRealmIndex];
            if (realmButtonLabel != null)
                realmButtonLabel.text = GetRealmButtonLabel();
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
                status.Requirement.TargetLevel, true);
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
            if (starActionStatus != null)
                starActionStatus.text = message;
            Debug.Log($"[MainProgress] {message}", this);
        }

        string GetRealmButtonLabel()
        {
            RealmDefinition realm = Realms[selectedStarChartRealmIndex];
            string description = string.IsNullOrEmpty(realm.Description)
                ? string.Empty : $" ({realm.Description})";
            string levelText = selectedStarChartLevel > 0
                ? $"Lv{selectedStarChartLevel.ToString(CultureInfo.InvariantCulture)}"
                : "Lv—";
            string actualLevelMarker = selectedStarChartLevelIsHeroLevel
                ? string.Empty : " · Xem Tinh đồ";
            return $"{realm.Name}{description} · Lv{realm.MinLevel}-{realm.MaxLevel}\n"
                + $"{GetCultivationPeriod(selectedStarChartRealmIndex, selectedStarChartLevel)}"
                + $" • {levelText}{actualLevelMarker}";
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

        void ResetStarsPlaceholder()
        {
            Debug.Log("[MainProgress] Clicked=StarChartReset; no star data was changed.", this);
            if (starActionStatus != null)
                starActionStatus.text = "RESET chỉ là giao diện · chưa đổi dữ liệu sao.";
        }

        void SelectStarChartNode(int branchIndex, int starIndex)
        {
            selectedStarChartBranchIndex = branchIndex;
            int nextStar = litStarsPerBranch[branchIndex];
            if (nextStar >= MainProgressChartGraphic.StarsPerBranch)
            {
                starChartGraphic.SetSelectedStar(branchIndex,
                    MainProgressChartGraphic.StarsPerBranch - 1);
                UpdateStarActionDetails();
                if (starActionStatus != null)
                    starActionStatus.text = "Nhánh này đã thắp đủ 10 sao.";
                return;
            }

            starChartGraphic.SetSelectedStar(branchIndex, nextStar);
            UpdateStarActionDetails();
            if (starActionStatus != null)
                starActionStatus.text = starIndex != nextStar
                    ? $"Thắp theo thứ tự · sao kế tiếp là nốt {nextStar + 1}."
                    : "Sẵn sàng thắp sao này.";
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
            if (starActionStatus != null)
                starActionStatus.text = "Chọn THẮP SAO để thắp nốt đang sáng viền.";
        }

        void UpdateStarActionDetails()
        {
            if (starActionHint == null)
                return;
            int litCount = litStarsPerBranch[selectedStarChartBranchIndex];
            string branchName = StarChartGemNames[selectedStarChartBranchIndex];
            if (litCount >= MainProgressChartGraphic.StarsPerBranch)
            {
                starActionHint.text = $"{branchName} · Đã thắp đủ 10/10 sao.";
                return;
            }

            int cost = starChartGemInventory.GetStarCost(selectedStarChartRealmIndex,
                litCount);
            int available = starChartGemInventory.GetBaseGemCount(
                StarChartGemTypes[selectedStarChartBranchIndex]);
            starActionHint.text = $"{branchName} · Sao {litCount + 1}/10 · "
                + $"Chi phí {cost} gem · Có {available}";
        }

        void LightSelectedStar()
        {
            GemType gemType = StarChartGemTypes[selectedStarChartBranchIndex];
            int nextStar = litStarsPerBranch[selectedStarChartBranchIndex];
            if (nextStar >= MainProgressChartGraphic.StarsPerBranch)
            {
                if (starActionStatus != null)
                    starActionStatus.text = "Nhánh này đã thắp đủ 10 sao.";
                return;
            }

            bool lit = starChartGemInventory.TryLightNextStar(
                selectedStarChartRealmIndex, gemType, out int cost,
                out int missingGems);
            if (!lit)
            {
                if (starActionStatus != null)
                    starActionStatus.text = missingGems > 0
                        ? $"Chưa đủ gem · thiếu {missingGems} {StarChartGemNames[selectedStarChartBranchIndex]}."
                        : "Không thể thắp thêm sao trong nhánh này.";
                return;
            }

            RefreshBaseGemInventory();
            RefreshSelectedRealmStars();
            if (starActionStatus != null)
                starActionStatus.text = $"Đã thắp sao {nextStar + 1} · đã trừ {cost} "
                    + $"{StarChartGemNames[selectedStarChartBranchIndex]}.";
        }

        void SelectCultivationTab(bool showSkills)
        {
            if (skillsPlaceholder != null)
                skillsPlaceholder.SetActive(showSkills);
            if (starChartContent != null)
                starChartContent.SetActive(!showSkills);
            if (skillsTabButton != null)
                skillsTabButton.GetComponent<Image>().color = showSkills
                    ? Gold : new Color32(39, 45, 54, 255);
            if (starChartTabButton != null)
                starChartTabButton.GetComponent<Image>().color = showSkills
                    ? new Color32(39, 45, 54, 255) : Gold;
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
