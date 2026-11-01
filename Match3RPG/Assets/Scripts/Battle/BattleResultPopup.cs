using System.Collections;
using System.Collections.Generic;
using Match3RPG.Board;
using Match3RPG.Campaign;
using Match3RPG.Configuration;
using Match3RPG.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class BattleResultPopup : MonoBehaviour
    {
        const float AnimationSeconds = .2f;
        static Sprite whiteSprite;

        [SerializeField] Sprite victorySprite;
        [SerializeField] Sprite defeatSprite;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] bool tintHitAreas;
#endif

        RectTransform victoryRoot;
        RectTransform defeatRoot;
        RectTransform victoryImageRect;
        RectTransform defeatImageRect;
        RectTransform rewardContainer;
        CanvasGroup canvasGroup;
        CanvasGroup victoryGroup;
        CanvasGroup defeatGroup;
        TurnManager turnManager;
        BoardManager board;
        BoardSelectionOverlay selectionOverlay;
        GemSpecialVFXManager specialVfx;
        BoardRageFireVFX rageFireVfx;
        CombatVFXManager combatVfx;
        FloatingTextManager floatingTexts;
        CampaignManager campaign;
        GameConfigService config;
        Coroutine animation;
        bool resultShown;
        bool isVictory;

        struct RewardDisplay
        {
            public string Label;
            public Sprite Icon;
            public Color IconColor;
            public string FallbackMark;
        }

        public void Configure(RectTransform parent, TurnManager turns, BoardManager boardManager,
            BoardSelectionOverlay selection, GemSpecialVFXManager gemVfx,
            BoardRageFireVFX rageVfx, CombatVFXManager combat,
            FloatingTextManager floatingTextManager, CampaignManager campaignManager,
            GameConfigService gameConfig, Sprite victory, Sprite defeat)
        {
            turnManager = turns;
            board = boardManager;
            selectionOverlay = selection;
            specialVfx = gemVfx;
            rageFireVfx = rageVfx;
            combatVfx = combat;
            floatingTexts = floatingTextManager;
            campaign = campaignManager;
            config = gameConfig;
            victorySprite = victory;
            defeatSprite = defeat;
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
            canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform dim = CreateRect("DimOverlay", root,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image dimImage = AddImage(dim.gameObject, new Color(0f, 0f, 0f, .6f));
            dimImage.raycastTarget = true;
            dim.SetAsFirstSibling();

            victoryRoot = CreateRect("VictoryRoot", root,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            victoryGroup = victoryRoot.gameObject.AddComponent<CanvasGroup>();
            victoryImageRect = CreateCenteredImage("VictoryImage", victoryRoot,
                victorySprite, .8f);
            rewardContainer = CreateRect("RewardContainer", victoryImageRect,
                new Vector2(.14f, .235f), new Vector2(.86f, .435f),
                Vector2.zero, Vector2.zero);
            CreateRewardRows(new RewardPackage(string.Empty, 0, 0, false,
                System.Array.Empty<RuntimeLootDrop>()));
            CreateButtonHit("ClaimHit", victoryImageRect,
                new Rect(.17f, .091f, .66f, .118f), ClaimVictory);

            defeatRoot = CreateRect("DefeatRoot", root,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            defeatGroup = defeatRoot.gameObject.AddComponent<CanvasGroup>();
            defeatImageRect = CreateFullScreenImage("DefeatImage", defeatRoot, defeatSprite);
            CreateButtonHit("RetryHit", defeatImageRect,
                new Rect(.16f, .248f, .68f, .078f), RetryBattle);
            CreateButtonHit("LeaveHit", defeatImageRect,
                new Rect(.16f, .16f, .68f, .078f), LeaveBattle);

            victoryRoot.gameObject.SetActive(false);
            defeatRoot.gameObject.SetActive(false);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        RectTransform CreateCenteredImage(string name, RectTransform parent,
            Sprite sprite, float widthFraction)
        {
            RectTransform rect = CreateRect(name, parent,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            float width = parent.rect.width * widthFraction;
            float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : .5625f;
            rect.sizeDelta = new Vector2(width, width / aspect);
            ConfigureSpriteImage(rect, sprite);
            return rect;
        }

        RectTransform CreateFullScreenImage(string name, RectTransform parent, Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            float width = parent.rect.width;
            float height = parent.rect.height;
            float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : .5625f;
            if (height <= 0f || width / height > aspect)
                width = height * aspect;
            else
                height = width / aspect;
            rect.sizeDelta = new Vector2(width, height);
            ConfigureSpriteImage(rect, sprite);
            return rect;
        }

        static void ConfigureSpriteImage(RectTransform rect, Sprite sprite)
        {
            Image image = AddImage(rect.gameObject, Color.white);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        void CreateRewardRows(RewardPackage rewards)
        {
            if (rewardContainer == null)
                return;
            for (int child = rewardContainer.childCount - 1; child >= 0; child--)
                Destroy(rewardContainer.GetChild(child).gameObject);

            List<RewardDisplay> displays = new List<RewardDisplay>();
            if (rewards == null)
            {
                displays.Add(new RewardDisplay
                {
                    Label = "Gold 0",
                    IconColor = new Color32(246, 190, 66, 255),
                    FallbackMark = "G"
                });
                displays.Add(new RewardDisplay
                {
                    Label = "EXP 0",
                    IconColor = new Color32(121, 205, 132, 255),
                    FallbackMark = "E"
                });
            }
            else
            {
                if (rewards.Gold > 0)
                    displays.Add(new RewardDisplay
                    {
                        Label = $"Gold  {rewards.Gold}",
                        IconColor = new Color32(246, 190, 66, 255),
                        FallbackMark = "G"
                    });
                if (rewards.Experience > 0)
                    displays.Add(new RewardDisplay
                    {
                        Label = $"EXP  {rewards.Experience}",
                        IconColor = new Color32(121, 205, 132, 255),
                        FallbackMark = "E"
                    });

                foreach (RuntimeLootDrop drop in rewards.ItemDrops)
                {
                    if (drop == null || drop.Quantity <= 0
                        || (drop.FirstClearOnly && !rewards.IsFirstClear))
                        continue;
                    ItemConfig item = config != null ? config.GetItem(drop.ItemId) : null;
                    string itemName = item != null && !string.IsNullOrEmpty(item.DisplayName)
                        ? item.DisplayName : drop.ItemId;
                    displays.Add(new RewardDisplay
                    {
                        Label = $"{itemName}  x{drop.Quantity}",
                        Icon = item != null ? item.RewardIcon : null,
                        IconColor = new Color32(104, 170, 219, 255),
                        FallbackMark = "I"
                    });
                }

                if (displays.Count == 0)
                {
                    displays.Add(new RewardDisplay
                    {
                        Label = $"Gold  {rewards.Gold}",
                        IconColor = new Color32(246, 190, 66, 255),
                        FallbackMark = "G"
                    });
                    displays.Add(new RewardDisplay
                    {
                        Label = $"EXP  {rewards.Experience}",
                        IconColor = new Color32(121, 205, 132, 255),
                        FallbackMark = "E"
                    });
                }
            }

            float containerHeight = Mathf.Max(1f, rewardContainer.rect.height);
            float rowHeight = Mathf.Min(46f, containerHeight / displays.Count);
            for (int index = 0; index < displays.Count; index++)
            {
                float centerY = containerHeight * .5f - rowHeight * (index + .5f);
                CreateRewardRow(displays[index], centerY, rowHeight);
            }
        }

        void CreateRewardRow(RewardDisplay reward, float centerY, float rowHeight)
        {
            RectTransform row = CreateRect("RewardRow", rewardContainer,
                new Vector2(0f, .5f), new Vector2(1f, .5f),
                new Vector2(0f, centerY - rowHeight * .5f),
                new Vector2(0f, centerY + rowHeight * .5f));
            RectTransform iconRect = CreateRect("RewardIcon", row,
                new Vector2(.06f, .5f), new Vector2(.06f, .5f),
                new Vector2(0f, -rowHeight * .38f),
                new Vector2(rowHeight * .76f, rowHeight * .38f));
            Image iconImage = AddImage(iconRect.gameObject, reward.Icon != null
                ? Color.white : reward.IconColor);
            iconImage.sprite = reward.Icon != null ? reward.Icon : whiteSprite;
            iconImage.raycastTarget = false;
            if (reward.Icon == null)
            {
                TMP_Text mark = CreateText(reward.FallbackMark, iconRect, rowHeight * .38f,
                    Vector2.zero, Vector2.one);
                mark.color = new Color32(35, 29, 22, 255);
            }
            TMP_Text label = CreateText(reward.Label, row, rowHeight * .72f,
                new Vector2(.15f, 0f), new Vector2(.96f, 1f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = rowHeight * .72f;
            label.color = new Color32(255, 232, 173, 255);
        }

        TMP_Text CreateText(string value, RectTransform parent, float size,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = CreateRect("RewardText", parent,
                anchorMin, anchorMax, new Vector2(2f, 1f), new Vector2(-2f, -1f));
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.outlineColor = new Color(.05f, .025f, .01f, .95f);
            text.outlineWidth = .16f;
            text.raycastTarget = false;
            return text;
        }

        Button CreateButtonHit(string name, RectTransform parent, Rect normalizedRect,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = CreateRect(name, parent,
                new Vector2(normalizedRect.xMin, normalizedRect.yMin),
                new Vector2(normalizedRect.xMax, normalizedRect.yMax),
                Vector2.zero, Vector2.zero);
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

        public void ShowResult(bool victory, RewardPackage rewards)
        {
            if (resultShown)
                return;
            if ((victory && victorySprite == null) || (!victory && defeatSprite == null))
            {
                Debug.LogError($"Missing {(victory ? nameof(victorySprite) : nameof(defeatSprite))} "
                    + "for Battle result popup.", this);
                return;
            }

            resultShown = true;
            isVictory = victory;
            victoryRoot.gameObject.SetActive(victory);
            defeatRoot.gameObject.SetActive(!victory);
            if (victory)
            {
                victoryImageRect.GetComponent<Image>().sprite = victorySprite;
                CreateRewardRows(rewards);
                victoryGroup.alpha = 0f;
            }
            else
            {
                defeatImageRect.GetComponent<Image>().sprite = defeatSprite;
                defeatGroup.alpha = 0f;
            }
            turnManager?.StopForBattleExit();
            board?.SetInputEnabled(false);
            selectionOverlay?.Hide();
            if (specialVfx != null) specialVfx.enabled = false;
            if (rageFireVfx != null) rageFireVfx.enabled = false;
            combatVfx?.ClearAllEffects();
            floatingTexts?.ClearActiveTexts();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            if (animation != null)
                StopCoroutine(animation);
            animation = StartCoroutine(AnimateResult(victory));
        }

        IEnumerator AnimateResult(bool victory)
        {
            CanvasGroup visibleGroup = victory ? victoryGroup : defeatGroup;
            RectTransform animatedRect = victory ? victoryImageRect : defeatImageRect;
            animatedRect.localScale = Vector3.one * .9f;
            float elapsed = 0f;
            while (elapsed < AnimationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / AnimationSeconds);
                canvasGroup.alpha = t;
                visibleGroup.alpha = t;
                animatedRect.localScale = Vector3.one * Mathf.Lerp(.9f, 1f, t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            visibleGroup.alpha = 1f;
            animatedRect.localScale = Vector3.one;
            animation = null;
        }

        void ClaimVictory()
        {
            if (!isVictory)
                return;
            if (campaign == null)
            {
                Debug.LogError("Cannot claim the victory because CampaignManager is missing.", this);
                return;
            }
            campaign.LeaveBattleForRegionMap();
        }

        void RetryBattle()
        {
            if (isVictory)
                return;
            if (campaign == null)
            {
                Debug.LogError("Cannot retry because CampaignManager is missing.", this);
                return;
            }
            campaign.RetryCurrentBattle();
        }

        void LeaveBattle()
        {
            if (isVictory)
                return;
            if (campaign == null)
            {
                Debug.LogError("Cannot leave the battle because CampaignManager is missing.", this);
                return;
            }
            campaign.LeaveBattleForRegionMap();
        }

        void Update()
        {
            if (!resultShown || Keyboard.current == null
                || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            if (isVictory)
                ClaimVictory();
            else
                LeaveBattle();
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
                    new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f));
        }
    }
}
