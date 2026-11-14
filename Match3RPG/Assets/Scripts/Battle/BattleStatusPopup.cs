using System.Collections;
using Match3RPG.Board;
using Match3RPG.Campaign;
using Match3RPG.Core;
using Match3RPG.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class BattleStatusPopup : MonoBehaviour
    {
        const float AnimationSeconds = .15f;
        const float PopupWidth = 1000f;
        const float PopupHeight = 1500f;
        static readonly Color Gold = new Color32(231, 190, 110, 255);

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
        RectTransform settingsHits;
        CanvasGroup canvasGroup;
        Image popupImage;
        GameObject confirmationRoot;
        Image[] settingOffOverlays = new Image[3];
        TMP_Text[] settingOffLabels = new TMP_Text[3];
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
            Image dimImage = AddImage(dim.gameObject, new Color(0f, 0f, 0f, .6f));
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

            tabFocusRect = CreateRect("TabFocusOverlay", popupRect,
                new Vector2(.073f, .764f), new Vector2(.361f, .822f),
                Vector2.zero, Vector2.zero);
            Image focusImage = AddImage(tabFocusRect.gameObject, Color.clear);
            focusImage.type = Image.Type.Sliced;
            focusImage.fillCenter = false;
            focusImage.raycastTarget = false;
            Outline focusOutline = tabFocusRect.gameObject.AddComponent<Outline>();
            focusOutline.effectColor = Gold;
            focusOutline.effectDistance = new Vector2(3f, -3f);
            focusOutline.useGraphicAlpha = false;

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
            settingsHits.gameObject.SetActive(false);

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
            settingsHits.gameObject.SetActive(tab == Tab.Settings);
            UpdateSettingsIndicators();
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
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            if (isOpen)
            {
                if (confirmationRoot != null && confirmationRoot.activeSelf)
                    CancelExitConfirmation();
                else
                    Close();
            }
            else
                Open();
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
