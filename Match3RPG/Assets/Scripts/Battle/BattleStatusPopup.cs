using System.Collections;
using System.Collections.Generic;
using Match3RPG.Board;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Core;
using Match3RPG.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Match3RPG.Battle
{
    public sealed class BattleStatusPopup : MonoBehaviour
    {
        const float AnimationSeconds = .15f;
        const float PopupWidth = 1000f;
        const float PopupHeight = 1500f;
        const float SkillTooltipDurationSeconds = 2.5f;
        const string SkillInsufficientSpiritMessage = "Không đủ tinh lực để sử dụng kỹ năng";
        static readonly Color Gold = new Color32(231, 190, 110, 255);
        static readonly Color SkillConditionReadyColor = new Color32(74, 222, 128, 255);
        static readonly Color SkillConditionUnavailableColor = new Color32(156, 163, 175, 255);
        static readonly GemType[] SkillCounterGemTypes =
        {
            GemType.Mana,
            GemType.Attack,
            GemType.Soul,
            GemType.Rage
        };
        static readonly string[] SettingIconResourceNames =
        {
            "MainProgress/Settings/settings_sound_256",     // index 0 = Âm Thanh
            "MainProgress/Settings/settings_music_256",     // index 1 = Nhạc Nền
            "MainProgress/Settings/settings_vibration_256"  // index 2 = Rung
        };
        static readonly string[] LegacyItemIds =
        {
            "azure_dragon",
            "blue_potion",
            "immortal_peach",
            "sun_spirit_orb"
        };

        enum Tab
        {
            Bag,
            Skills,
            Settings
        }

        static Sprite whiteSprite;
        RectTransform popupRect;
        RectTransform popupImageRect;
        RectTransform tabFocusRect;
        RectTransform bagContentRoot;
        RectTransform skillsContentRoot;
        RectTransform settingsHits;
        GameObject itemPopupRoot;
        CanvasGroup canvasGroup;
        Image popupImage;
        GameObject confirmationRoot;
        Image[] settingOffOverlays = new Image[3];
        TMP_Text[] settingOffLabels = new TMP_Text[3];
        sealed class SkillRowWidgets
        {
            public Button RowButton;
            public Image RowBackground;
            public Image IconImage;
            public Image CooldownPizzaOverlay;
            public TMP_Text LevelBadgeText;
            public TMP_Text DescriptionText;
            public Image ManaCostIcon;
            public TMP_Text ManaCostText;
            public RectTransform RageCostIconRect;
            public Image RageCostIcon;
            public RectTransform ConditionTextRect;
            public TMP_Text ConditionText;
        }

        readonly Image[] skillGemCounterIconImages = new Image[4];
        readonly TMP_Text[] skillGemCounterTextLabels = new TMP_Text[4];
        ScrollRect skillsScrollRect;
        SkillRowWidgets flyingSwordRow;
        SkillRowWidgets swordQiRow;
        float skillRowRightTextWidth = 575f;
        RectTransform skillTooltipRoot;
        TMP_Text skillTooltipText;
        Coroutine skillTooltipRoutine;
        TurnManager turnManager;
        BoardManager board;
        BoardSelectionOverlay selectionOverlay;
        GemSpecialVFXManager specialVfx;
        BoardRageFireVFX rageFireVfx;
        CombatVFXManager combatVfx;
        FloatingTextManager floatingTexts;
        CampaignManager campaign;
        Coroutine animation;
        Tab activeTab;
        bool isOpen;
        bool closing;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] bool tintHitAreas;
#endif

        public void Configure(RectTransform parent,
            TurnManager turns, BoardManager boardManager, BoardSelectionOverlay selection,
            GemSpecialVFXManager gemVfx, BoardRageFireVFX rageVfx,
            CombatVFXManager combat, FloatingTextManager floatingTextManager,
            CampaignManager campaignManager)
        {
            turnManager = turns;
            board = boardManager;
            selectionOverlay = selection;
            specialVfx = gemVfx;
            rageFireVfx = rageVfx;
            combatVfx = combat;
            floatingTexts = floatingTextManager;
            campaign = campaignManager;
            BuildHierarchy(parent);
        }

        void BuildHierarchy(RectTransform parent)
        {
            EnsureWhiteSprite();
            RectTransform root = transform as RectTransform;
            root.SetParent(parent, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            RectTransform dim = CreateRect("DimOverlay", root,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image dimImage = AddImage(dim.gameObject, new Color(0f, 0f, 0f, .8f));
            dimImage.raycastTarget = true;
            dim.SetAsFirstSibling();

            popupRect = CreateRect("PopupRoot", root,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(-PopupWidth * .5f, -PopupHeight * .5f),
                new Vector2(PopupWidth * .5f, PopupHeight * .5f));
            canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            popupImageRect = CreateRect("PopupImage", popupRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            popupImage = AddImage(popupImageRect.gameObject, Color.white);
            popupImage.type = Image.Type.Simple;
            popupImage.preserveAspect = true;
            popupImage.raycastTarget = false;
            Sprite popupSprite = Resources.Load<Sprite>("Battle/UI/PauseMenu_Single");
            if (popupSprite == null)
                Debug.LogError("BattleStatusPopup failed to load sprite Resources/Battle/UI/PauseMenu_Single.", this);
            else
                popupImage.sprite = popupSprite;

            // Khung focus tab dùng sprite 9-slice; fallback về Outline vàng nếu thiếu sprite
            tabFocusRect = CreateRect("TabFocusOverlay", popupRect,
                new Vector2(.073f, .764f), new Vector2(.361f, .822f),
                Vector2.zero, Vector2.zero);
            Sprite focusSprite = Resources.Load<Sprite>("MainProgress/UI/focus_frame_9slice");
            if (focusSprite == null)
            {
                Debug.LogError("BattleStatusPopup failed to load sprite Resources/MainProgress/UI/focus_frame_9slice.", this);
                Image focusImage = AddImage(tabFocusRect.gameObject, Color.clear);
                focusImage.type = Image.Type.Sliced;
                focusImage.fillCenter = false;
                focusImage.raycastTarget = false;
                Outline focusOutline = tabFocusRect.gameObject.AddComponent<Outline>();
                focusOutline.effectColor = Gold;
                focusOutline.effectDistance = new Vector2(3f, -3f);
                focusOutline.useGraphicAlpha = false;
            }
            else
            {
                Image focusImage = AddImage(tabFocusRect.gameObject, Color.white);
                focusImage.sprite = focusSprite;
                focusImage.type = Image.Type.Sliced;
                focusImage.fillCenter = false;
                focusImage.raycastTarget = false;
            }

            CreateButtonHit("TabBagHit", popupRect, new Rect(.073f, .764f, .288f, .058f),
                () => SetTab(Tab.Bag));
            CreateButtonHit("TabSkillsHit", popupRect, new Rect(.376f, .764f, .257f, .058f),
                () => SetTab(Tab.Skills));
            CreateButtonHit("TabSettingsHit", popupRect, new Rect(.647f, .764f, .279f, .058f),
                () => SetTab(Tab.Settings));
            CreateButtonHit("CloseHit", popupRect, new Rect(.871f, .855f, .101f, .068f), Close);

            settingsHits = CreateRect("SettingsContentHits", popupRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateSettingButton("SoundHit", 0, new Rect(.105f, .445f, .38f, .26f));
            CreateSettingButton("MusicHit", 1, new Rect(.515f, .445f, .38f, .26f));
            CreateSettingButton("VibrationHit", 2, new Rect(.105f, .165f, .38f, .26f));
            Button exitButton = CreateButtonHit("ExitGameHit", settingsHits,
                new Rect(.515f, .165f, .38f, .26f), ShowExitConfirmation);
            StyleSettingButton(exitButton);
            Sprite exitSprite = Resources.Load<Sprite>("MainProgress/Settings/settings_exit_256");
            if (exitSprite == null)
                Debug.LogError("BattleStatusPopup failed to load sprite MainProgress/Settings/settings_exit_256.", this);
            else
            {
                RectTransform iconRect = CreateRect("Icon", exitButton.transform,
                    new Vector2(.15f, .15f), new Vector2(.85f, .85f), Vector2.zero, Vector2.zero);
                Image iconImage = AddImage(iconRect.gameObject, Color.white);
                iconImage.sprite = exitSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            settingsHits.gameObject.SetActive(false);

            BuildBagTabContent();
            BuildSkillsTabContent();
            BuildExitConfirmation(root);
            popupRect.SetSiblingIndex(1);
            SetTab(Tab.Bag);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            popupRect.localScale = Vector3.one * .94f;
        }

        void CreateSettingButton(string name, int settingIndex, Rect normalizedRect)
        {
            Button button = CreateButtonHit(name, settingsHits, normalizedRect,
                () => ToggleSetting(settingIndex));
            StyleSettingButton(button);
            Sprite iconSprite = Resources.Load<Sprite>(SettingIconResourceNames[settingIndex]);
            if (iconSprite == null)
                Debug.LogError("BattleStatusPopup failed to load sprite " + SettingIconResourceNames[settingIndex] + ".", this);
            else
            {
                RectTransform iconRect = CreateRect("Icon", button.transform,
                    new Vector2(.15f, .15f), new Vector2(.85f, .85f), Vector2.zero, Vector2.zero);
                Image iconImage = AddImage(iconRect.gameObject, Color.white);
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            RectTransform overlayRect = CreateRect("OffOverlay", button.transform,
                new Vector2(.08f, .08f), new Vector2(.92f, .92f), Vector2.zero, Vector2.zero);
            Image overlay = AddImage(overlayRect.gameObject, new Color(0f, 0f, 0f, .62f));
            overlay.raycastTarget = false;
            TMP_Text badge = CreateText("TẮT", overlayRect, 24f,
                new Vector2(.67f, .72f), new Vector2(.96f, .96f));
            badge.color = new Color32(255, 226, 164, 255);
            badge.enableAutoSizing = true;
            badge.fontSizeMin = 16f;
            badge.fontSizeMax = 24f;
            settingOffOverlays[settingIndex] = overlay;
            settingOffLabels[settingIndex] = badge;
        }

        static void StyleSettingButton(Button button)
        {
            Image image = button.GetComponent<Image>();
            image.color = new Color32(12, 27, 36, 255);
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        Button CreateButtonHit(string name, RectTransform parent, Rect normalizedRect,
            UnityEngine.Events.UnityAction action)
        {
            Vector2 min = new Vector2(normalizedRect.xMin, normalizedRect.yMin);
            Vector2 max = new Vector2(normalizedRect.xMax, normalizedRect.yMax);
            RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            Image image = AddImage(rect.gameObject, Color.clear);
            image.raycastTarget = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (tintHitAreas)
                image.color = new Color(1f, 0f, 0f, .22f);
#endif
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            return button;
        }

        void BuildExitConfirmation(RectTransform parent)
        {
            RectTransform confirm = CreateRect("ExitConfirmation", parent,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image shade = AddImage(confirm.gameObject, new Color(0f, 0f, 0f, .6f));
            shade.raycastTarget = true;
            RectTransform panel = CreateRect("ConfirmationPanel", confirm,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(-390f, -185f), new Vector2(390f, 185f));
            AddImage(panel.gameObject, new Color32(20, 42, 37, 255));
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(245, 190, 88, 255);
            outline.effectDistance = new Vector2(3f, -3f);

            TMP_Text message = CreateText("Thoát trận? Tiến trình trận này sẽ không được tính.",
                panel, 30f, new Vector2(.08f, .43f), new Vector2(.92f, .9f));
            message.enableAutoSizing = true;
            message.fontSizeMin = 22f;
            message.fontSizeMax = 30f;
            CreateConfirmButton("CancelHit", panel, "Hủy", new Rect(.12f, .08f, .32f, .25f),
                CancelExitConfirmation);
            CreateConfirmButton("ConfirmExitHit", panel, "Thoát", new Rect(.56f, .08f, .32f, .25f),
                ExitBattleWithoutRewards);
            confirmationRoot = confirm.gameObject;
            confirmationRoot.SetActive(false);
        }

        void CreateConfirmButton(string name, RectTransform parent, string label,
            Rect normalizedRect, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButtonHit(name, parent, normalizedRect, action);
            Image image = button.GetComponent<Image>();
            image.color = new Color32(106, 70, 32, 255);
            TMP_Text text = CreateText(label, (RectTransform)button.transform,
                28f, Vector2.zero, Vector2.one);
            text.raycastTarget = false;
        }

        TMP_Text CreateText(string value, RectTransform parent, float size,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = CreateRect("Text", parent, anchorMin, anchorMax,
                new Vector2(4f, 2f), new Vector2(-4f, -2f));
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.outlineColor = new Color(.05f, .025f, .01f, .95f);
            text.outlineWidth = .2f;
            text.raycastTarget = false;
            return text;
        }

        void SetTab(Tab tab)
        {
            activeTab = tab;
            if (tabFocusRect != null)
            {
                if (tab == Tab.Bag)
                {
                    tabFocusRect.anchorMin = new Vector2(.073f, .764f);
                    tabFocusRect.anchorMax = new Vector2(.361f, .822f);
                }
                else if (tab == Tab.Skills)
                {
                    tabFocusRect.anchorMin = new Vector2(.376f, .764f);
                    tabFocusRect.anchorMax = new Vector2(.633f, .822f);
                }
                else
                {
                    tabFocusRect.anchorMin = new Vector2(.647f, .764f);
                    tabFocusRect.anchorMax = new Vector2(.926f, .822f);
                }
                tabFocusRect.offsetMin = Vector2.zero;
                tabFocusRect.offsetMax = Vector2.zero;
            }
            if (bagContentRoot != null)
                bagContentRoot.gameObject.SetActive(tab == Tab.Bag);
            if (skillsContentRoot != null)
                skillsContentRoot.gameObject.SetActive(tab == Tab.Skills);
            settingsHits.gameObject.SetActive(tab == Tab.Settings);
            if (tab == Tab.Skills)
            {
                if (skillsScrollRect != null)
                    skillsScrollRect.verticalNormalizedPosition = 1f;
                RefreshSkillsTab();
            }
            else
                HideSkillTooltip();
            UpdateSettingsIndicators();
            CloseItemPopup();
        }

        void ToggleSetting(int index)
        {
            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager.CurrentSave ?? saveManager.LoadOrCreate();
            save.settings = save.settings ?? new SettingsSaveData();
            if (index == 0) save.settings.soundEnabled = !save.settings.soundEnabled;
            else if (index == 1) save.settings.musicEnabled = !save.settings.musicEnabled;
            else if (index == 2) save.settings.vibrationEnabled = !save.settings.vibrationEnabled;
            saveManager.SaveNow();
            UpdateSettingsIndicators();
        }

        void UpdateSettingsIndicators()
        {
            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager != null ? saveManager.CurrentSave : null;
            SettingsSaveData settings = save != null ? save.settings : null;
            bool[] enabled =
            {
                settings == null || settings.soundEnabled,
                settings == null || settings.musicEnabled,
                settings == null || settings.vibrationEnabled
            };
            for (int i = 0; i < settingOffOverlays.Length; i++)
            {
                bool showOff = !enabled[i];
                if (settingOffOverlays[i] != null)
                    settingOffOverlays[i].gameObject.SetActive(showOff);
                if (settingOffLabels[i] != null)
                    settingOffLabels[i].gameObject.SetActive(showOff);
            }
        }

        void ShowExitConfirmation()
        {
            if (confirmationRoot != null)
                confirmationRoot.SetActive(true);
        }

        void CancelExitConfirmation()
        {
            if (confirmationRoot != null)
                confirmationRoot.SetActive(false);
        }

        void ExitBattleWithoutRewards()
        {
            CloseItemPopup();
            if (confirmationRoot != null)
                confirmationRoot.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            isOpen = false;
            closing = false;
            if (animation != null)
            {
                StopCoroutine(animation);
                animation = null;
            }

            if (turnManager != null)
                turnManager.StopForBattleExit();
            if (board != null)
                board.SetInputEnabled(false);
            if (selectionOverlay != null)
                selectionOverlay.Hide();
            if (specialVfx != null)
                specialVfx.enabled = false;
            if (rageFireVfx != null)
                rageFireVfx.enabled = false;
            if (combatVfx != null)
                combatVfx.ClearAllEffects();
            if (floatingTexts != null)
                floatingTexts.ClearActiveTexts();
            if (Time.timeScale != 1f)
                Time.timeScale = 1f;
            if (campaign == null)
            {
                Debug.LogError("Cannot exit the Battle because CampaignManager is missing.", this);
                return;
            }
            campaign.LeaveBattleForRegionMap();
        }

        public void Open()
        {
            if (isOpen || closing || turnManager == null || !turnManager.PauseForStatusPopup())
                return;
            isOpen = true;
            CloseItemPopup();
            SetTab(Tab.Bag);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            if (animation != null)
                StopCoroutine(animation);
            animation = StartCoroutine(Animate(true));
        }

        void Close()
        {
            if (!isOpen || closing)
                return;
            HideSkillTooltip();
            CloseItemPopup();
            if (confirmationRoot != null)
                confirmationRoot.SetActive(false);
            closing = true;
            canvasGroup.interactable = false;
            if (animation != null)
                StopCoroutine(animation);
            animation = StartCoroutine(Animate(false));
        }

        IEnumerator Animate(bool opening)
        {
            if (opening)
            {
                canvasGroup.alpha = 0f;
                popupRect.localScale = Vector3.one * .94f;
            }
            float elapsed = 0f;
            while (elapsed < AnimationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / AnimationSeconds);
                canvasGroup.alpha = opening ? t : 1f - t;
                popupRect.localScale = Vector3.one * Mathf.Lerp(
                    opening ? .94f : 1f, opening ? 1f : .94f, t);
                yield return null;
            }
            canvasGroup.alpha = opening ? 1f : 0f;
            popupRect.localScale = opening ? Vector3.one : Vector3.one * .94f;
            animation = null;
            if (!opening)
            {
                isOpen = false;
                closing = false;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                turnManager?.ResumeFromStatusPopup();
            }
        }

        void Update()
        {
            if (isOpen && activeTab == Tab.Skills)
                RefreshSkillsTab();

            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            if (isOpen)
            {
                if (itemPopupRoot != null)
                    CloseItemPopup();
                else if (confirmationRoot != null && confirmationRoot.activeSelf)
                    CancelExitConfirmation();
                else
                    Close();
            }
            else
                Open();
        }

        void BuildSkillsTabContent()
        {
            skillsContentRoot = CreateRect("SkillsContent", popupRect,
                new Vector2(0.068f, 0.124f), new Vector2(0.931f, 0.749f),
                Vector2.zero, Vector2.zero);

            // (1) Ô đếm gem cố định ở góc phải trên của tab Skills (không cuộn theo danh sách)
            RectTransform gemCounterBox = CreateRect("SkillGemCounterBox", skillsContentRoot,
                new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            gemCounterBox.pivot = new Vector2(1f, 1f);
            gemCounterBox.sizeDelta = new Vector2(440f, 54f);
            gemCounterBox.anchoredPosition = new Vector2(-24f, -10f);
            Image counterBg = AddImage(gemCounterBox.gameObject, new Color32(12, 27, 36, 255));
            counterBg.raycastTarget = false;
            Outline counterOutline = gemCounterBox.gameObject.AddComponent<Outline>();
            counterOutline.effectColor = Gold;
            counterOutline.effectDistance = new Vector2(2f, -2f);

            const float counterSlotWidth = 104f;
            const float counterLeftPadding = 14f;
            for (int i = 0; i < SkillCounterGemTypes.Length; i++)
            {
                GemType gemType = SkillCounterGemTypes[i];
                float slotX = counterLeftPadding + i * counterSlotWidth;

                RectTransform gemIconRect = CreateRect($"GemCounterIcon_{gemType}", gemCounterBox,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                gemIconRect.pivot = new Vector2(0f, 0.5f);
                gemIconRect.sizeDelta = new Vector2(36f, 36f);
                gemIconRect.anchoredPosition = new Vector2(slotX, 0f);
                Image gemIconImage = gemIconRect.gameObject.AddComponent<Image>();
                gemIconImage.sprite = ResolveGemSprite(gemType);
                gemIconImage.color = Color.white;
                gemIconImage.preserveAspect = true;
                gemIconImage.raycastTarget = false;
                skillGemCounterIconImages[i] = gemIconImage;

                RectTransform gemCountRect = CreateRect($"GemCounterValue_{gemType}", gemCounterBox,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                gemCountRect.pivot = new Vector2(0f, 0.5f);
                gemCountRect.sizeDelta = new Vector2(58f, 44f);
                gemCountRect.anchoredPosition = new Vector2(slotX + 40f, 0f);
                TMP_Text gemCountText = CreateText("0", gemCountRect, 22f, Vector2.zero, Vector2.one);
                gemCountText.fontStyle = FontStyles.Bold;
                gemCountText.alignment = TextAlignmentOptions.MidlineLeft;
                gemCountText.enableAutoSizing = true;
                gemCountText.fontSizeMin = 14f;
                gemCountText.fontSizeMax = 22f;
                gemCountText.color = Color.white;
                skillGemCounterTextLabels[i] = gemCountText;
            }

            // (2) Danh sách skill cuộn dọc (ScrollRect) bên dưới ô đếm gem cố định
            RectTransform viewportRect = CreateRect("SkillsScrollViewport", skillsContentRoot,
                Vector2.zero, Vector2.one, new Vector2(0f, 10f), new Vector2(0f, -74f));
            Image viewportImage = AddImage(viewportRect.gameObject, new Color(1f, 1f, 1f, 0.001f));
            viewportImage.raycastTarget = true;
            viewportRect.gameObject.AddComponent<RectMask2D>();

            skillsScrollRect = viewportRect.gameObject.AddComponent<ScrollRect>();
            skillsScrollRect.horizontal = false;
            skillsScrollRect.vertical = true;
            skillsScrollRect.movementType = ScrollRect.MovementType.Clamped;
            skillsScrollRect.inertia = true;
            skillsScrollRect.decelerationRate = 0.135f;
            skillsScrollRect.scrollSensitivity = 36f;
            skillsScrollRect.viewport = viewportRect;

            const int totalRows = 4; // 2 skill thật + 2 ô kỹ năng trống
            const float rowWidth = 815f;
            const float rowHeight = 220f; // Thu nhỏ ~15% từ 258f xuống 220f
            const float rowSpacing = 26f;
            const float topMargin = 8f;
            const float bottomMargin = 16f;
            const float iconSize = 170f;
            const float iconLeftMargin = 24f;
            float totalContentHeight = topMargin + totalRows * rowHeight
                + (totalRows - 1) * rowSpacing + bottomMargin;

            RectTransform contentRect = CreateRect("SkillsScrollContent", viewportRect,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, totalContentHeight);
            skillsScrollRect.content = contentRect;

            for (int rowIndex = 0; rowIndex < totalRows; rowIndex++)
            {
                RectTransform rowRect = CreateRect($"SkillRow_{rowIndex}", contentRect,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(rowWidth, rowHeight);
                rowRect.anchoredPosition = new Vector2(0f,
                    -(topMargin + rowIndex * (rowHeight + rowSpacing)));

                if (rowIndex == 0)
                {
                    flyingSwordRow = BuildSkillRow(
                        rowRect,
                        rowWidth,
                        iconSize,
                        iconLeftMargin,
                        SkillConfig.ResolveSwordSprite(),
                        "Tạo 1 lớp kiếm khí quanh nhân vật, sau đó triệt tiêu 1 hàng kim cương bất kỳ.",
                        OnFlyingSwordSkillClicked);
                    continue;
                }

                if (rowIndex == 1)
                {
                    swordQiRow = BuildSkillRow(
                        rowRect,
                        rowWidth,
                        iconSize,
                        iconLeftMargin,
                        SkillConfig.ResolveSwordQiSprite(),
                        SkillConfig.SwordQiDescription,
                        OnSwordQiSkillClicked);
                    continue;
                }

                Image emptyRowBg = AddImage(rowRect.gameObject, new Color32(12, 27, 36, 165));
                emptyRowBg.raycastTarget = true;
                Outline emptyRowOutline = rowRect.gameObject.AddComponent<Outline>();
                emptyRowOutline.effectColor = new Color32(231, 190, 110, 110);
                emptyRowOutline.effectDistance = new Vector2(2f, -2f);

                RectTransform emptyIconFrame = CreateRect("EmptySkillIconFrame", rowRect,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                emptyIconFrame.pivot = new Vector2(0f, 0.5f);
                emptyIconFrame.sizeDelta = new Vector2(iconSize, iconSize);
                emptyIconFrame.anchoredPosition = new Vector2(iconLeftMargin, 0f);
                Image emptyIconBg = AddImage(emptyIconFrame.gameObject, new Color32(16, 35, 46, 165));
                emptyIconBg.raycastTarget = false;
                Outline emptyIconOutline = emptyIconFrame.gameObject.AddComponent<Outline>();
                emptyIconOutline.effectColor = new Color32(231, 190, 110, 110);
                emptyIconOutline.effectDistance = new Vector2(2f, -2f);

                TMP_Text emptyQuestionText = CreateText("?", emptyIconFrame, 56f,
                    Vector2.zero, Vector2.one);
                emptyQuestionText.fontStyle = FontStyles.Bold;
                emptyQuestionText.color = new Color32(156, 163, 175, 150);
            }

            CreateSkillTooltip();
            skillsContentRoot.gameObject.SetActive(false);
            RefreshSkillsTab();
        }

        SkillRowWidgets BuildSkillRow(
            RectTransform rowRect,
            float rowWidth,
            float iconSize,
            float iconLeftMargin,
            Sprite defaultIcon,
            string defaultDescription,
            UnityEngine.Events.UnityAction onClick)
        {
            SkillRowWidgets widgets = new SkillRowWidgets();

            Image rowBg = AddImage(rowRect.gameObject, new Color32(12, 27, 36, 255));
            rowBg.raycastTarget = true;
            Outline rowOutline = rowRect.gameObject.AddComponent<Outline>();
            rowOutline.effectColor = Gold;
            rowOutline.effectDistance = new Vector2(2f, -2f);

            widgets.RowBackground = rowBg;
            widgets.RowButton = rowRect.gameObject.AddComponent<Button>();
            widgets.RowButton.targetGraphic = rowBg;
            widgets.RowButton.transition = Selectable.Transition.None;
            widgets.RowButton.onClick.AddListener(onClick);

            // Phần (1): 1 ảnh kỹ năng bên trái (ô vuông 170x170)
            RectTransform iconFrameRect = CreateRect("SkillIconFrame", rowRect,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            iconFrameRect.pivot = new Vector2(0f, 0.5f);
            iconFrameRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconFrameRect.anchoredPosition = new Vector2(iconLeftMargin, 0f);
            Image iconFrameBg = AddImage(iconFrameRect.gameObject, new Color32(16, 35, 46, 255));
            iconFrameBg.raycastTarget = false;
            Outline iconOutline = iconFrameRect.gameObject.AddComponent<Outline>();
            iconOutline.effectColor = Gold;
            iconOutline.effectDistance = new Vector2(2f, -2f);
            iconFrameRect.gameObject.AddComponent<RectMask2D>();

            RectTransform artworkRect = CreateRect("SkillArtwork", iconFrameRect,
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f),
                Vector2.zero, Vector2.zero);
            widgets.IconImage = artworkRect.gameObject.AddComponent<Image>();
            widgets.IconImage.sprite = defaultIcon;
            widgets.IconImage.color = Color.white;
            widgets.IconImage.preserveAspect = true;
            widgets.IconImage.raycastTarget = false;

            // Lớp phủ xám HÌNH TRÒN kiểu bánh pizza từ tâm ô ảnh (12 giờ, chiều kim đồng hồ)
            RectTransform pizzaRect = CreateRect("CooldownPizzaOverlay", iconFrameRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            pizzaRect.pivot = new Vector2(0.5f, 0.5f);
            widgets.CooldownPizzaOverlay = AddImage(pizzaRect.gameObject,
                new Color(0.05f, 0.07f, 0.09f, 0.72f));
            widgets.CooldownPizzaOverlay.type = Image.Type.Filled;
            widgets.CooldownPizzaOverlay.fillMethod = Image.FillMethod.Radial360;
            widgets.CooldownPizzaOverlay.fillOrigin = (int)Image.Origin360.Top;
            widgets.CooldownPizzaOverlay.fillClockwise = false;
            widgets.CooldownPizzaOverlay.fillAmount = 0f;
            widgets.CooldownPizzaOverlay.raycastTarget = false;
            widgets.CooldownPizzaOverlay.gameObject.SetActive(false);

            // Badge "Lv.1" ở góc dưới cùng bên phải trên ảnh kỹ năng, cùng kiểu badge "E" trên item
            RectTransform levelBadgeRect = CreateRect("SkillLevelBadge", iconFrameRect,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-54f, 2f), new Vector2(-2f, 28f));
            Image levelBadgeBg = AddImage(levelBadgeRect.gameObject, new Color32(15, 15, 20, 230));
            levelBadgeBg.raycastTarget = false;
            Outline levelBadgeOutline = levelBadgeRect.gameObject.AddComponent<Outline>();
            levelBadgeOutline.effectColor = Gold;
            levelBadgeOutline.effectDistance = new Vector2(1f, -1f);
            widgets.LevelBadgeText = CreateText("Lv.1", levelBadgeRect, 18f,
                Vector2.zero, Vector2.one);
            widgets.LevelBadgeText.fontStyle = FontStyles.Bold;
            widgets.LevelBadgeText.enableAutoSizing = true;
            widgets.LevelBadgeText.fontSizeMin = 12f;
            widgets.LevelBadgeText.fontSizeMax = 18f;

            // Phần (2): 1 mô tả kỹ năng bên phải
            float rightTextLeft = iconLeftMargin + iconSize + 22f;
            float rightTextWidth = rowWidth - rightTextLeft - 24f;
            skillRowRightTextWidth = rightTextWidth;
            RectTransform descRect = CreateRect("SkillDescription", rowRect,
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            descRect.pivot = new Vector2(0f, 1f);
            descRect.sizeDelta = new Vector2(rightTextWidth, 124f);
            descRect.anchoredPosition = new Vector2(rightTextLeft, -18f);
            widgets.DescriptionText = CreateText(
                defaultDescription,
                descRect, 23f, Vector2.zero, Vector2.one);
            widgets.DescriptionText.alignment = TextAlignmentOptions.TopLeft;
            widgets.DescriptionText.enableAutoSizing = true;
            widgets.DescriptionText.fontSizeMin = 18f;
            widgets.DescriptionText.fontSizeMax = 24f;
            widgets.DescriptionText.color = Color.white;

            // Phần (3): dòng điều kiện kỹ năng: "[icon mana] 3   [icon nộ] 0 • Hồi chiêu: 3 turn"
            RectTransform condRect = CreateRect("SkillConditions", rowRect,
                new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
            condRect.pivot = new Vector2(0f, 0f);
            condRect.sizeDelta = new Vector2(rightTextWidth, 44f);
            condRect.anchoredPosition = new Vector2(rightTextLeft, 18f);

            RectTransform manaIconRect = CreateRect("ManaCostIcon", condRect,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            manaIconRect.pivot = new Vector2(0f, 0.5f);
            manaIconRect.sizeDelta = new Vector2(32f, 32f);
            manaIconRect.anchoredPosition = new Vector2(0f, 0f);
            widgets.ManaCostIcon = manaIconRect.gameObject.AddComponent<Image>();
            widgets.ManaCostIcon.sprite = ResolveGemSprite(GemType.Mana);
            widgets.ManaCostIcon.color = Color.white;
            widgets.ManaCostIcon.preserveAspect = true;
            widgets.ManaCostIcon.raycastTarget = false;

            RectTransform manaCostTextRect = CreateRect("ManaCostText", condRect,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            manaCostTextRect.pivot = new Vector2(0f, 0.5f);
            manaCostTextRect.sizeDelta = new Vector2(44f, 44f);
            manaCostTextRect.anchoredPosition = new Vector2(38f, 0f);
            widgets.ManaCostText = CreateText("3", manaCostTextRect, 22f,
                Vector2.zero, Vector2.one);
            widgets.ManaCostText.alignment = TextAlignmentOptions.MidlineLeft;
            widgets.ManaCostText.enableAutoSizing = false;
            widgets.ManaCostText.color = SkillConditionUnavailableColor;

            widgets.RageCostIconRect = CreateRect("RageCostIcon", condRect,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            widgets.RageCostIconRect.pivot = new Vector2(0f, 0.5f);
            widgets.RageCostIconRect.sizeDelta = new Vector2(32f, 32f);
            widgets.RageCostIconRect.anchoredPosition = new Vector2(74f, 0f);
            widgets.RageCostIcon = widgets.RageCostIconRect.gameObject.AddComponent<Image>();
            widgets.RageCostIcon.sprite = ResolveGemSprite(GemType.Rage);
            widgets.RageCostIcon.color = Color.white;
            widgets.RageCostIcon.preserveAspect = true;
            widgets.RageCostIcon.raycastTarget = false;

            widgets.ConditionTextRect = CreateRect("RageAndCooldownText", condRect,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            widgets.ConditionTextRect.pivot = new Vector2(0f, 0.5f);
            widgets.ConditionTextRect.sizeDelta = new Vector2(rightTextWidth - 112f, 44f);
            widgets.ConditionTextRect.anchoredPosition = new Vector2(112f, 0f);
            widgets.ConditionText = CreateText(
                "0 • Hồi chiêu: 3 turn",
                widgets.ConditionTextRect, 22f, Vector2.zero, Vector2.one);
            widgets.ConditionText.alignment = TextAlignmentOptions.MidlineLeft;
            widgets.ConditionText.enableAutoSizing = true;
            widgets.ConditionText.fontSizeMin = 16f;
            widgets.ConditionText.fontSizeMax = 22f;
            widgets.ConditionText.color = SkillConditionUnavailableColor;

            return widgets;
        }

        void CreateSkillTooltip()
        {
            skillTooltipRoot = CreateRect("CampaignTooltip", popupRect,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            skillTooltipRoot.sizeDelta = new Vector2(650f, 160f);
            skillTooltipRoot.anchoredPosition = Vector2.zero;
            Sprite tooltipFrameSprite = ResolveTooltipFrameSprite();
            Image frame = skillTooltipRoot.gameObject.AddComponent<Image>();
            frame.sprite = tooltipFrameSprite != null ? tooltipFrameSprite : whiteSprite;
            frame.color = tooltipFrameSprite != null
                ? Color.white
                : (Color)new Color32(12, 27, 36, 242);
            frame.preserveAspect = tooltipFrameSprite != null;
            frame.raycastTarget = false;
            Outline outline = skillTooltipRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(180, 137, 65, 255);
            outline.effectDistance = new Vector2(2f, -2f);
            skillTooltipText = CreateText(SkillInsufficientSpiritMessage, skillTooltipRoot,
                24f, new Vector2(0.10f, 0.20f), new Vector2(0.90f, 0.80f));
            skillTooltipText.color = new Color32(238, 230, 207, 255);
            skillTooltipText.enableAutoSizing = true;
            skillTooltipText.fontSizeMin = 18f;
            skillTooltipText.fontSizeMax = 24f;
            skillTooltipRoot.gameObject.SetActive(false);
        }

        Sprite ResolveTooltipFrameSprite()
        {
            if (campaign != null && campaign.Catalog != null
                && campaign.Catalog.CampaignTooltipFrame != null)
                return campaign.Catalog.CampaignTooltipFrame;
            CampaignCatalog catalog = Resources.Load<CampaignCatalog>("Campaign/CampaignCatalog");
            return catalog != null ? catalog.CampaignTooltipFrame : null;
        }

        void ShowSkillUnavailableNotification()
        {
            if (skillTooltipRoot == null || skillTooltipText == null)
                return;
            skillTooltipText.text = SkillInsufficientSpiritMessage;
            skillTooltipRoot.gameObject.SetActive(true);
            skillTooltipRoot.SetAsLastSibling();
            if (skillTooltipRoutine != null)
                StopCoroutine(skillTooltipRoutine);
            skillTooltipRoutine = StartCoroutine(HideSkillTooltipRoutine());
        }

        IEnumerator HideSkillTooltipRoutine()
        {
            yield return new WaitForSecondsRealtime(SkillTooltipDurationSeconds);
            if (skillTooltipRoot != null)
                skillTooltipRoot.gameObject.SetActive(false);
            skillTooltipRoutine = null;
        }

        void HideSkillTooltip()
        {
            if (skillTooltipRoutine != null)
            {
                StopCoroutine(skillTooltipRoutine);
                skillTooltipRoutine = null;
            }
            if (skillTooltipRoot != null)
                skillTooltipRoot.gameObject.SetActive(false);
        }

        Sprite ResolveGemSprite(GemType type)
        {
            if (board != null)
            {
                Sprite boardSprite = board.GetGemSprite(type);
                if (boardSprite != null)
                    return boardSprite;
            }
#if UNITY_EDITOR
            string assetPath = GemVisualDatabase.ExpectedBaseMapping(type);
            Sprite editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (editorSprite != null)
                return editorSprite;
#endif
            return null;
        }

        void RefreshSkillsTab()
        {
            BattleController battle = turnManager != null ? turnManager.Battle : null;
            int[] gemCounts =
            {
                battle != null ? battle.PlayerManaGemCount : 0,
                battle != null ? battle.PlayerAttackGemCount : 0,
                battle != null ? battle.PlayerSoulGemCount : 0,
                battle != null ? battle.PlayerRageGemCount : 0
            };
            for (int i = 0; i < SkillCounterGemTypes.Length; i++)
            {
                if (skillGemCounterIconImages[i] != null && skillGemCounterIconImages[i].sprite == null)
                    skillGemCounterIconImages[i].sprite = ResolveGemSprite(SkillCounterGemTypes[i]);
                if (skillGemCounterTextLabels[i] != null)
                    skillGemCounterTextLabels[i].text = gemCounts[i].ToString();
            }

            if (flyingSwordRow != null)
            {
                SkillConfig flyingSwordSkill = battle != null ? battle.FlyingSwordSkill : null;
                int level = battle != null ? battle.FlyingSwordSkillLevel : SkillConfig.DefaultLevel;
                string fallbackDescription =
                    $"Tạo 1 lớp kiếm khí quanh nhân vật, sau đó triệt tiêu {(level <= 2 ? 1 : level <= 4 ? 2 : 3)} hàng kim cương bất kỳ.";
                RefreshSkillRow(
                    flyingSwordRow,
                    flyingSwordSkill,
                    SkillConfig.ResolveSwordSprite(),
                    level,
                    SkillConfig.DefaultManaCost,
                    SkillConfig.DefaultRageCost,
                    SkillConfig.DefaultCooldownTurns,
                    battle != null ? battle.FlyingSwordCooldownRemaining : 0,
                    fallbackDescription,
                    turnManager != null && turnManager.CanActivateManualSkill);
            }

            if (swordQiRow != null)
            {
                SkillConfig swordQiSkill = battle != null ? battle.SwordQiSkill : null;
                int level = battle != null ? battle.SwordQiSkillLevel : SkillConfig.DefaultLevel;
                RefreshSkillRow(
                    swordQiRow,
                    swordQiSkill,
                    SkillConfig.ResolveSwordQiSprite(),
                    level,
                    SkillConfig.SwordQiDefaultManaCost,
                    SkillConfig.SwordQiDefaultRageCost,
                    SkillConfig.SwordQiDefaultCooldownTurns,
                    battle != null ? battle.SwordQiCooldownRemaining : 0,
                    SkillConfig.SwordQiDescription,
                    turnManager != null && turnManager.CanActivateSwordQiSkill);
            }
        }

        void RefreshSkillRow(
            SkillRowWidgets widgets,
            SkillConfig skill,
            Sprite fallbackIcon,
            int level,
            int defaultManaCost,
            int defaultRageCost,
            int defaultCooldownTurns,
            int cooldownRemaining,
            string fallbackDescription,
            bool canActivate)
        {
            if (widgets == null || widgets.DescriptionText == null || widgets.ConditionText == null)
                return;

            int manaCost = skill != null ? skill.ManaCost : defaultManaCost;
            int rageCost = skill != null ? skill.RageCost : defaultRageCost;
            int cooldownTurns = skill != null ? skill.CooldownTurns : defaultCooldownTurns;

            if (widgets.IconImage != null && widgets.IconImage.sprite == null)
                widgets.IconImage.sprite = skill != null && skill.Icon != null ? skill.Icon : fallbackIcon;
            if (widgets.ManaCostIcon != null && widgets.ManaCostIcon.sprite == null)
                widgets.ManaCostIcon.sprite = ResolveGemSprite(GemType.Mana);
            if (widgets.RageCostIcon != null && widgets.RageCostIcon.sprite == null)
                widgets.RageCostIcon.sprite = ResolveGemSprite(GemType.Rage);
            if (widgets.LevelBadgeText != null)
                widgets.LevelBadgeText.text = $"Lv.{level}";

            string description = skill != null
                ? skill.GetDescriptionForLevel(level)
                : fallbackDescription;
            widgets.DescriptionText.text = description;

            if (widgets.RowButton != null)
                widgets.RowButton.interactable = true;

            Color conditionColor = canActivate
                ? SkillConditionReadyColor
                : SkillConditionUnavailableColor;
            string manaCostStr = manaCost.ToString();
            if (widgets.ManaCostText != null)
            {
                widgets.ManaCostText.text = manaCostStr;
                widgets.ManaCostText.color = conditionColor;
            }
            float manaTextWidth = Mathf.Max(16f, manaCostStr.Length * 14f);
            float rageIconX = 38f + manaTextWidth + 18f;
            if (widgets.RageCostIconRect != null)
                widgets.RageCostIconRect.anchoredPosition = new Vector2(rageIconX, 0f);
            float condTextX = rageIconX + 38f;
            if (widgets.ConditionTextRect != null)
            {
                widgets.ConditionTextRect.anchoredPosition = new Vector2(condTextX, 0f);
                widgets.ConditionTextRect.sizeDelta = new Vector2(
                    Mathf.Max(100f, skillRowRightTextWidth - condTextX), 44f);
            }

            string cooldownLabel = cooldownRemaining > 0
                ? $"Hồi chiêu: còn {cooldownRemaining}/{cooldownTurns} turn"
                : $"Hồi chiêu: {cooldownTurns} turn";
            widgets.ConditionText.text = $"{rageCost} • {cooldownLabel}";
            widgets.ConditionText.color = conditionColor;
            widgets.DescriptionText.color = canActivate
                ? Color.white
                : new Color32(180, 186, 196, 255);

            if (widgets.CooldownPizzaOverlay != null)
            {
                if (cooldownRemaining > 0 && cooldownTurns > 0)
                {
                    float fill = Mathf.Clamp01(cooldownRemaining / (float)cooldownTurns);
                    widgets.CooldownPizzaOverlay.fillAmount = fill;
                    if (!widgets.CooldownPizzaOverlay.gameObject.activeSelf)
                        widgets.CooldownPizzaOverlay.gameObject.SetActive(true);
                }
                else
                {
                    widgets.CooldownPizzaOverlay.fillAmount = 0f;
                    if (widgets.CooldownPizzaOverlay.gameObject.activeSelf)
                        widgets.CooldownPizzaOverlay.gameObject.SetActive(false);
                }
            }
        }

        void OnFlyingSwordSkillClicked()
        {
            HandleSkillClicked(
                turnManager != null && turnManager.CanActivateManualSkill,
                () => turnManager.TryActivateFlyingSwordSkill());
        }

        void OnSwordQiSkillClicked()
        {
            HandleSkillClicked(
                turnManager != null && turnManager.CanActivateSwordQiSkill,
                () => turnManager.TryActivateSwordQiSkill());
        }

        void HandleSkillClicked(bool canActivate, System.Func<bool> tryActivate)
        {
            if (turnManager == null || !canActivate || tryActivate == null || !tryActivate())
            {
                ShowSkillUnavailableNotification();
                RefreshSkillsTab();
                return;
            }

            CloseImmediateForSkill();
        }

        void CloseImmediateForSkill()
        {
            HideSkillTooltip();
            CloseItemPopup();
            if (confirmationRoot != null)
                confirmationRoot.SetActive(false);
            if (animation != null)
            {
                StopCoroutine(animation);
                animation = null;
            }
            isOpen = false;
            closing = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (popupRect != null)
                popupRect.localScale = Vector3.one * .94f;
        }

        void BuildBagTabContent()
        {
            bagContentRoot = CreateRect("BagContent", popupRect,
                new Vector2(0.068f, 0.124f), new Vector2(0.931f, 0.749f),
                Vector2.zero, Vector2.zero);

            GameConfigService config = Resources.Load<GameConfigService>(
                "Configuration/GameConfigService");
            if (config == null)
            {
                Debug.LogError("BattleStatusPopup failed to load Configuration/GameConfigService.", this);
                return;
            }

            List<ItemConfig> consumables = new List<ItemConfig>();
            for (int i = 0; i < LegacyItemIds.Length; i++)
            {
                string stableId = LegacyItemIds[i];
                ItemConfig item = config.GetItem(stableId);
                if (item == null)
                {
                    Debug.LogError($"BattleStatusPopup failed to find ItemConfig for '{stableId}'.", this);
                    continue;
                }
                if (item.InventoryFlag == 1)
                    consumables.Add(item);
            }

            const int columns = 6;
            float contentWidth = (0.931f - 0.068f) * PopupWidth;
            float contentHeight = (0.749f - 0.124f) * PopupHeight;
            const float gridTopMargin = 40f;
            const float gridBottomMargin = 40f;
            float widthCellSize = contentWidth / columns;
            float heightCellSize = (contentHeight - gridTopMargin - gridBottomMargin - 30f)
                / (6f * 0.97f + 0.725f);
            float cellSize = Mathf.Min(widthCellSize, heightCellSize);
            float slotSize = cellSize * 0.725f;
            float rowPitch = cellSize * 0.97f;
            float gridSpan = 6f * rowPitch + slotSize + 30f;
            float gridTopOffset = (contentHeight - gridSpan) * 0.5f;
            float gridXOffset = (contentWidth - columns * cellSize) * 0.5f;

            for (int displayIndex = 0; displayIndex < consumables.Count; displayIndex++)
            {
                ItemConfig itemConfig = consumables[displayIndex];
                int row = displayIndex / columns;
                int column = displayIndex % columns;

                RectTransform slotRect = CreateRect($"BagSlot_{displayIndex}", bagContentRoot,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = new Vector2(slotSize, slotSize);
                slotRect.anchoredPosition = new Vector2(
                    gridXOffset + (column + 0.5f) * cellSize,
                    -(gridTopOffset + (row + 0.5f) * rowPitch));

                Image slotImage = AddImage(slotRect.gameObject, new Color32(12, 27, 36, 255));
                slotImage.raycastTarget = true;
                Outline slotOutline = slotRect.gameObject.AddComponent<Outline>();
                slotOutline.effectColor = Gold;
                slotOutline.effectDistance = new Vector2(2f, -2f);

                Button slotButton = slotRect.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slotImage;
                slotButton.transition = Selectable.Transition.None;
                slotButton.onClick.AddListener(() => OpenItemPopup(itemConfig));

                RectTransform itemRect = CreateRect("ItemArtwork", slotRect,
                    new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f),
                    Vector2.zero, Vector2.zero);
                Image itemImage = itemRect.gameObject.AddComponent<Image>();
                itemImage.sprite = itemConfig.RewardIcon;
                itemImage.preserveAspect = true;
                itemImage.raycastTarget = false;

                RectTransform nameRect = CreateRect($"BagItemName_{displayIndex}", bagContentRoot,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                nameRect.pivot = new Vector2(0.5f, 1f);
                nameRect.sizeDelta = new Vector2(cellSize * 0.96f, 26f);
                nameRect.anchoredPosition = new Vector2(
                    gridXOffset + (column + 0.5f) * cellSize,
                    -(gridTopOffset + (row + 0.5f) * rowPitch + slotSize * 0.5f + 4f));
                TMP_Text nameText = CreateText(itemConfig.DisplayName, nameRect, 18f,
                    Vector2.zero, Vector2.one);
                nameText.enableAutoSizing = true;
                nameText.fontSizeMin = 12f;
                nameText.fontSizeMax = 20f;
            }
        }

        void OpenItemPopup(ItemConfig item)
        {
            if (item == null)
                return;

            CloseItemPopup();

            RectTransform overlay = CreateRect("ItemPopupOverlay", popupRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.SetAsLastSibling();
            Image shade = AddImage(overlay.gameObject, new Color(0f, 0f, 0f, 0.72f));
            shade.raycastTarget = true;
            itemPopupRoot = overlay.gameObject;

            RectTransform panel = CreateRect("PopupPanel", overlay,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-360f, -420f), new Vector2(360f, 420f));
            AddImage(panel.gameObject, new Color32(12, 27, 36, 255));
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = Gold;
            panelOutline.effectDistance = new Vector2(3f, -3f);

            Button closeButton = CreateButtonHit("PopupCloseButton", panel,
                new Rect(0.88f, 0.89f, 0.08f, 0.07f), CloseItemPopup);
            Image closeImage = closeButton.GetComponent<Image>();
            closeImage.color = new Color32(62, 45, 37, 255);
            Outline closeOutline = closeButton.gameObject.AddComponent<Outline>();
            closeOutline.effectColor = Gold;
            closeOutline.effectDistance = new Vector2(2f, -2f);
            TMP_Text closeText = CreateText("X", (RectTransform)closeButton.transform, 26f,
                Vector2.zero, Vector2.one);
            closeText.color = Gold;

            RectTransform iconFrameRect = CreateRect("PopupItemIconFrame", panel,
                new Vector2(0.08f, 0.72f), new Vector2(0.32f, 0.92f),
                Vector2.zero, Vector2.zero);
            AddImage(iconFrameRect.gameObject, new Color32(12, 27, 36, 255));
            Outline iconOutline = iconFrameRect.gameObject.AddComponent<Outline>();
            iconOutline.effectColor = Gold;
            iconOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform artworkRect = CreateRect("PopupItemArtwork", iconFrameRect,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f),
                Vector2.zero, Vector2.zero);
            Image artworkImage = artworkRect.gameObject.AddComponent<Image>();
            artworkImage.sprite = item.RewardIcon;
            artworkImage.preserveAspect = true;
            artworkImage.raycastTarget = false;

            TMP_Text nameLabel = CreateText(item.DisplayName, panel, 30f,
                new Vector2(0.36f, 0.80f), new Vector2(0.86f, 0.92f));
            nameLabel.color = Gold;
            nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 18f;
            nameLabel.fontSizeMax = 30f;

            string descriptionText = string.IsNullOrEmpty(item.Description)
                ? "Chưa có dữ liệu — chờ bổ sung"
                : item.Description;
            TMP_Text effectLabel = CreateText(descriptionText, panel, 22f,
                new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.68f));
            effectLabel.color = Color.white;
            effectLabel.alignment = TextAlignmentOptions.TopLeft;
            effectLabel.enableAutoSizing = false;

            Button useButton = CreateButtonHit("PopupUseButton", panel,
                new Rect(0.28f, 0.08f, 0.44f, 0.12f),
                () => UseItemFromBattle(item));
            Image useImage = useButton.GetComponent<Image>();
            useImage.color = new Color32(62, 45, 37, 255);
            Outline useOutline = useButton.gameObject.AddComponent<Outline>();
            useOutline.effectColor = Gold;
            useOutline.effectDistance = new Vector2(2f, -2f);
            TMP_Text useText = CreateText("Sử dụng", (RectTransform)useButton.transform, 28f,
                Vector2.zero, Vector2.one);
            useText.color = Gold;
        }

        void UseItemFromBattle(ItemConfig item)
        {
            if (item != null)
                Debug.Log($"[BattleStatusPopup] Sử dụng item: {item.DisplayName} (stableId: {item.StableId})");
            CloseItemPopup();
        }

        void CloseItemPopup()
        {
            if (itemPopupRoot != null)
            {
                Destroy(itemPopupRoot);
                itemPopupRoot = null;
            }
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 min,
            Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(.5f, .5f);
            return rect;
        }

        static Image AddImage(GameObject target, Color color)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            return image;
        }

        static void EnsureWhiteSprite()
        {
            if (whiteSprite == null)
                whiteSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0f, 0f, 4f, 4f), new Vector2(.5f, .5f),
                    100f / 6f, 0u, SpriteMeshType.FullRect,
                    new Vector4(1f, 1f, 1f, 1f));
        }
    }
}
