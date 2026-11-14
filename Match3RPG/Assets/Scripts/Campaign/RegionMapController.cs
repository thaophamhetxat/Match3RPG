using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;
using Match3RPG.Battle;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Match3RPG.Campaign
{
    public sealed class RegionMapController : MonoBehaviour
    {
        [SerializeField] CampaignCatalog catalog;
        [SerializeField, Min(96f)] float stageTouchTarget = 104f;
        [SerializeField] Sprite stageChallengePopupSprite;
        [SerializeField] StageChallengePopupLayoutConfig stageChallengeLayout;
        [SerializeField] GameConfigService gameConfigService;
        [SerializeField] RegionMapLayoutConfig regionMapLayout;
        [SerializeField] Canvas regionMapCanvas;
        [SerializeField] RectTransform safeAreaRoot;
        [SerializeField] RectTransform stageInfoPopupRoot;
        [SerializeField] RectTransform popupArtworkRoot;
        [SerializeField] Image popupArtworkImage;
        [SerializeField] Image dimOverlayImage;
        [SerializeField] Image heroPreviewImage;
        [SerializeField] Image bossPreviewImage;
        [SerializeField] TextMeshProUGUI powerText;
        [SerializeField] TextMeshProUGUI popupEnemyName;
        [SerializeField] TextMeshProUGUI popupStageLabel;
        [SerializeField] RectTransform rewardArea;
        [SerializeField] Button exitHitButton;
        [SerializeField] Button startHitButton;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] bool tintStagePopupHitAreas;
        [SerializeField] bool tintRegionMapBackHitArea;
#endif
        CampaignManager campaign;
        RegionData region;
        StageData selectedStage;
        BossData selectedBossData;
        RectTransform mapArtworkRect;
        RectTransform mapContentRect;
        RectTransform mapViewportRect;
        RectTransform headerRect;
        RectTransform footerRect;
        ScrollRect mapScrollRect;
        float mapArtworkHeight;
        float mapArtworkWidth;
        float mapLayoutScale = 1f;
        float mapContentHeight;
        float mapViewportHeight;
        float headerHeight;
        float footerHeight;
        RectTransform heroPreviewSlot;
        RectTransform bossPreviewSlot;
        RectTransform tooltipRoot;
        TextMeshProUGUI tooltipText;
        Coroutine tooltipRoutine;
        readonly Dictionary<string, StageMarkerView> stageMarkersById =
            new Dictionary<string, StageMarkerView>(StringComparer.Ordinal);

        public void Configure(CampaignCatalog campaignCatalog)
        {
            catalog = campaignCatalog;
        }

        void Awake()
        {
            if (catalog == null)
                catalog = Resources.Load<CampaignCatalog>("Campaign/CampaignCatalog");
            if (catalog == null)
            {
                Debug.LogError("RegionMap requires Resources/Campaign/CampaignCatalog.", this);
                return;
            }

            campaign = CampaignManager.GetOrCreate(catalog);
            region = ResolveSelectedRegion();
            if (region == null)
            {
                Debug.LogError("RegionMap could not resolve the selected Region.", this);
                return;
            }

            GameProgressService.EnsureInitialized(catalog);
            GameProgressService.RememberRegion(region.RegionId);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string resolvedSpriteName = region.MapSprite != null ? region.MapSprite.name : "(missing)";
            Debug.Log($"RegionMap binding: RegionId={region.RegionId}; "
                + $"RegionConfig={region.name}; RegionConfigPath=CampaignCatalog.Regions; "
                + $"SpriteField=RegionData.MapSprite; ResolvedSprite={resolvedSpriteName}; "
                + $"RegionDatabaseEntryCount(catalog-backed)={catalog.Regions.Count}.", this);
#endif
            StartCoroutine(BuildUIAfterCanvasLayout());
        }

        IEnumerator BuildUIAfterCanvasLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            BuildUI();
        }

        RegionData ResolveSelectedRegion()
        {
            string selectedId = campaign.SelectedRegionId;
            if (string.IsNullOrEmpty(selectedId))
                selectedId = GameProgressService.LastSelectedRegionId;
            RegionData selected = catalog.GetRegion(selectedId);
            if (selected != null && GameProgressService.IsRegionUnlocked(catalog, selected))
                return selected;

            foreach (RegionData candidate in catalog.Regions)
                if (candidate != null && GameProgressService.IsRegionUnlocked(catalog, candidate))
                {
                    campaign.SelectRegion(candidate);
                    return candidate;
                }
            return null;
        }

        void BuildUI()
        {
            if (regionMapCanvas == null || safeAreaRoot == null)
            {
                Debug.LogError("RegionMapCanvas and SafeArea references must be assigned in RegionMap.", this);
                return;
            }

            CampaignUIFactory.EnsureEventSystem();
            Image background = CampaignUIFactory.CreateImage(regionMapCanvas.transform,
                "RegionMapBackground", new Color32(10, 18, 30, 255));
            background.transform.SetSiblingIndex(0);
            RectTransform canvasRect = regionMapCanvas.transform as RectTransform;
            if (canvasRect == null)
            {
                Debug.LogError("RegionMapCanvas requires a RectTransform.", this);
                return;
            }

            Canvas.ForceUpdateCanvases();
            headerHeight = GetSpriteHeight(region.HeaderSprite, canvasRect.rect.width)
                * (regionMapLayout != null ? regionMapLayout.HeaderHeightScale : 1f);
            footerHeight = GetSpriteHeight(region.FooterSprite, canvasRect.rect.width)
                * (regionMapLayout != null ? regionMapLayout.FooterHeightScale : 1f);
            RectTransform scrollView = CampaignUIFactory.CreateRect("MapScrollView", canvasRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mapScrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            mapScrollRect.horizontal = false;
            mapScrollRect.vertical = true;
            mapScrollRect.movementType = ScrollRect.MovementType.Clamped;
            mapScrollRect.scrollSensitivity = 60f;

            RectTransform viewport = CampaignUIFactory.CreateRect("Viewport", scrollView,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mapViewportRect = viewport;
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, .001f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            mapScrollRect.viewport = viewport;

            if (region.MapLayoutPrefab != null)
            {
                GameObject layout = Instantiate(region.MapLayoutPrefab, viewport, false);
                mapContentRect = layout.GetComponent<RectTransform>();
                if (mapContentRect == null)
                {
                    Debug.LogError($"Region map layout has no RectTransform: {region.MapLayoutPrefab.name}.",
                        region.MapLayoutPrefab);
                    return;
                }
                Canvas previewCanvas = layout.GetComponent<Canvas>();
                if (previewCanvas != null)
                {
                    previewCanvas.enabled = false;
                    Destroy(previewCanvas);
                }
                mapArtworkRect = mapContentRect.Find("RegionBackground") as RectTransform;
                if (mapArtworkRect == null)
                {
                    Debug.LogError($"Region map layout is missing RegionBackground: {region.MapLayoutPrefab.name}.",
                        region.MapLayoutPrefab);
                    return;
                }
                BindStageMarkers();
            }
            else
            {
                Debug.LogWarning($"Region {region.RegionId} has no MapLayoutPrefab; using the legacy runtime map builder.",
                    region);
                RectTransform content = CampaignUIFactory.CreateRect("MapContent", viewport,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                content.pivot = new Vector2(.5f, 1f);
                mapContentRect = content;
                mapArtworkRect = CampaignUIFactory.CreateRect("RegionBackground", content,
                    new Vector2(.5f, 1f), new Vector2(.5f, 1f), Vector2.zero, Vector2.zero);
                mapArtworkRect.pivot = new Vector2(.5f, 1f);
                Image artwork = mapArtworkRect.gameObject.AddComponent<Image>();
                artwork.sprite = region.MapSprite;
                artwork.type = Image.Type.Simple;
                artwork.preserveAspect = false;
                artwork.raycastTarget = false;
                if (artwork.sprite == null)
                    Debug.LogError($"Region map sprite missing for {region.RegionId}.", region);

                RectTransform stageLayer = CampaignUIFactory.CreateRect("StagePathLayer", mapArtworkRect,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                BuildStageNodes(stageLayer);
            }
            mapScrollRect.content = mapContentRect;
            CreateRegionArtwork(canvasRect, headerHeight, footerHeight);
            CreateFooterBackHit();
            CreateTooltip(safeAreaRoot);
            BindStageInfoPopup();
            safeAreaRoot.SetAsLastSibling();
            FitMapContent();
            StartCoroutine(ScrollToCurrentStageOnce());
        }

        void BuildStageNodes(RectTransform parent)
        {
            foreach (StageData stage in region.Stages)
            {
                if (stage == null)
                    continue;
                CampaignNodeState state = GameProgressService.GetStageState(region, stage);
                Vector2 position = stage.RegionMapNodePosition;
                RectTransform nodeRect = CampaignUIFactory.CreateRect($"Stage{stage.StageNumber:00}",
                    parent, new Vector2(position.x, 1f - position.y),
                    new Vector2(position.x, 1f - position.y), Vector2.zero, Vector2.zero);
                float size = stage.IsBossStage ? stageTouchTarget + 8f : stageTouchTarget;
                nodeRect.sizeDelta = new Vector2(size, size);
                Image image = nodeRect.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, .001f);
                image.raycastTarget = true;
                Button button = nodeRect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = new ColorBlock
                {
                    normalColor = image.color,
                    highlightedColor = new Color(1f, 1f, 1f, .045f),
                    pressedColor = new Color(1f, 1f, 1f, .08f),
                    selectedColor = image.color,
                    disabledColor = image.color,
                    colorMultiplier = 1f,
                    fadeDuration = .1f
                };

                float markerScale = stage.IsBossStage ? 1.25f : 1f;
                CampaignUIFactory.CreateCenteredImage(nodeRect, "Platform",
                    new Vector2(118f * markerScale, 70f * markerScale),
                    catalog.CampaignStagePlatform, Color.white);
                CampaignUIFactory.CreateCenteredImage(nodeRect, "Glow",
                    new Vector2(104f * markerScale, 86f * markerScale),
                    catalog.CampaignStageGlow, Color.white);
                RectTransform markerRect = CampaignUIFactory.CreateRect("MarkerImage", nodeRect,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                markerRect.sizeDelta = new Vector2(72f * markerScale, 88f * markerScale);
                Image marker = markerRect.gameObject.AddComponent<Image>();
                marker.preserveAspect = true;
                marker.raycastTarget = false;

                RectTransform stateIconRect = CampaignUIFactory.CreateRect("StateIcon", markerRect,
                    new Vector2(.14f, .72f), new Vector2(.14f, .72f), Vector2.zero, Vector2.zero);
                stateIconRect.sizeDelta = new Vector2(26f, 26f);
                TextMeshProUGUI stateIcon = stateIconRect.gameObject.AddComponent<TextMeshProUGUI>();
                stateIcon.alignment = TextAlignmentOptions.Center;
                stateIcon.verticalAlignment = VerticalAlignmentOptions.Middle;
                stateIcon.raycastTarget = false;

                StageMarkerView view = nodeRect.gameObject.AddComponent<StageMarkerView>();
                view.SetStageId(stage.StageId);
                view.Configure(stage, state, catalog.CampaignAvailableStageMarker,
                    catalog.CampaignCompletedStageMarker,
                    catalog.CampaignBossStageMarker, catalog.CampaignStagePlatform,
                    catalog.CampaignStageGlow, OnStageSelected);
            }
        }

        void BindStageMarkers()
        {
            stageMarkersById.Clear();
            StageMarkerView[] markerViews = mapContentRect.GetComponentsInChildren<StageMarkerView>(true);
            foreach (StageMarkerView markerView in markerViews)
            {
                if (string.IsNullOrEmpty(markerView.StageId))
                {
                    Debug.LogError("Region map prefab contains a marker with no StageId.", markerView);
                    continue;
                }
                if (!stageMarkersById.TryAdd(markerView.StageId, markerView))
                    Debug.LogError($"Region map prefab contains duplicate StageId '{markerView.StageId}'.",
                        markerView);
            }

            foreach (StageData stage in region.Stages)
            {
                if (stage == null)
                    continue;
                if (!stageMarkersById.TryGetValue(stage.StageId, out StageMarkerView markerView))
                {
                    Debug.LogError($"Region map prefab is missing marker for StageId '{stage.StageId}'.", region);
                    continue;
                }
                markerView.Configure(stage, GameProgressService.GetStageState(region, stage),
                    catalog.CampaignAvailableStageMarker,
                    catalog.CampaignCompletedStageMarker, catalog.CampaignBossStageMarker,
                    catalog.CampaignStagePlatform, catalog.CampaignStageGlow, OnStageSelected);
            }
        }

        static float GetSpriteHeight(Sprite sprite, float width)
        {
            return sprite != null && sprite.rect.width > 0f
                ? width * sprite.rect.height / sprite.rect.width : 0f;
        }

        void CreateRegionArtwork(RectTransform parent, float headerHeight, float footerHeight)
        {
            if (region.HeaderSprite != null)
            {
                headerRect = CampaignUIFactory.CreateRect("RegionHeader", parent,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, -headerHeight), Vector2.zero);
                Image image = headerRect.gameObject.AddComponent<Image>();
                image.sprite = region.HeaderSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                headerRect.SetAsLastSibling();
            }

            if (region.FooterSprite != null)
            {
                footerRect = CampaignUIFactory.CreateRect("RegionFooter", parent,
                    Vector2.zero, new Vector2(1f, 0f),
                    Vector2.zero, new Vector2(0f, footerHeight));
                Image image = footerRect.gameObject.AddComponent<Image>();
                image.sprite = region.FooterSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                footerRect.SetAsLastSibling();
            }
        }

        void CreateFooterBackHit()
        {
            if (footerRect == null)
                return;

            RectTransform hit = CampaignUIFactory.CreateRect("BackHit", footerRect,
                Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero);
            hit.anchorMin = new Vector2(0.06f, 0.2f);
            hit.anchorMax = new Vector2(0.38f, 0.8f);
            hit.offsetMin = Vector2.zero;
            hit.offsetMax = Vector2.zero;
            Image image = hit.gameObject.AddComponent<Image>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            image.color = tintRegionMapBackHitArea
                ? new Color(1f, 0f, 0f, .25f) : new Color(1f, 1f, 1f, 0f);
#else
            image.color = new Color(1f, 1f, 1f, 0f);
#endif
            image.raycastTarget = true;
            Button back = hit.gameObject.AddComponent<Button>();
            back.targetGraphic = image;
            back.transition = Selectable.Transition.None;
            back.onClick.AddListener(ReturnToWorldMap);
            hit.SetAsLastSibling();
        }

        void CreateTooltip(Transform parent)
        {
            tooltipRoot = CampaignUIFactory.CreateRect("CampaignTooltip", parent,
                new Vector2(.5f, .27f), new Vector2(.5f, .27f), Vector2.zero, Vector2.zero);
            tooltipRoot.sizeDelta = new Vector2(650f, 226f);
            Image frame = tooltipRoot.gameObject.AddComponent<Image>();
            frame.sprite = catalog.CampaignTooltipFrame;
            frame.color = Color.white;
            frame.preserveAspect = frame.sprite != null;
            frame.raycastTarget = false;
            Outline outline = tooltipRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(180, 137, 65, 255);
            outline.effectDistance = new Vector2(2f, -2f);
            tooltipText = CampaignUIFactory.CreateText(tooltipRoot, "TooltipText", "", 22,
                new Color32(238, 230, 207, 255));
            SetAnchors(tooltipText.rectTransform, new Vector2(.12f, .20f),
                new Vector2(.88f, .80f));
            tooltipRoot.gameObject.SetActive(false);
        }

        void BindStageInfoPopup()
        {
            if (GetLayout() == null || stageChallengePopupSprite == null
                || stageInfoPopupRoot == null || popupArtworkRoot == null
                || popupArtworkImage == null || dimOverlayImage == null
                || heroPreviewImage == null || bossPreviewImage == null
                || powerText == null || popupEnemyName == null || popupStageLabel == null
                || rewardArea == null || exitHitButton == null || startHitButton == null)
            {
                Debug.LogError("Stage challenge popup scene hierarchy or references are incomplete.", this);
                return;
            }

            heroPreviewSlot = heroPreviewImage.transform.parent as RectTransform;
            bossPreviewSlot = bossPreviewImage.transform.parent as RectTransform;
            popupArtworkImage.sprite = stageChallengePopupSprite;
            dimOverlayImage.color = new Color(0f, 0f, 0f, .65f);
            dimOverlayImage.raycastTarget = true;
            ConfigureHitButton(exitHitButton, ClosePopup);
            ConfigureHitButton(startHitButton, StartSelectedStage);
            dimOverlayImage.gameObject.SetActive(false);
            stageInfoPopupRoot.gameObject.SetActive(false);
        }

        void ConfigureHitButton(Button button, UnityEngine.Events.UnityAction action)
        {
            Image image = button.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (tintStagePopupHitAreas)
                image.color = new Color(1f, 0f, 0f, .22f);
#endif
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        StageChallengePopupLayoutConfig GetLayout()
        {
            if (stageChallengeLayout == null)
                Debug.LogError("StageChallengePopupLayoutConfig is not assigned.", this);
            return stageChallengeLayout;
        }

        void ConfigureCharacterPreviews()
        {
            GameSaveData save = SaveManager.Instance.LoadOrCreate(catalog);
            PlayerLoadoutService loadoutService = new PlayerLoadoutService(
                SaveManager.Instance, gameConfigService);
            HeroData heroData = loadoutService.ResolveBattleLoadout().Hero;
            if (heroData != null)
            {
                HeroProgressSaveData progress = PlayerHeroSelectionService.GetHeroProgress(
                    save, heroData.StableId);
                ItemStatValues equipmentStats = new ItemStatValues(0, 0);
                if (gameConfigService != null)
                {
                    EquipmentService equipmentService = new EquipmentService(
                        SaveManager.Instance, gameConfigService);
                    if (equipmentService.ValidateEquipmentLoadout(out _))
                    {
                        equipmentStats = ItemStatCalculator.CalculateEquipped(
                            save.loadout, save.inventory, gameConfigService.GetItem);
                    }
                }

                RuntimeHeroStats stats = new RuntimeHeroStats(heroData, progress,
                    equipmentStats.AttackBonus, equipmentStats.MaxHealthBonus,
                    new StarChartGemInventoryService(SaveManager.Instance)
                        .GetTotalStatBonuses());
                powerText.text = CombatPowerCalculator.Calculate(stats, GetLayout())
                    .ToString("N0", CultureInfo.InvariantCulture);

                CharacterAnimationSet animationSet = heroData.AnimationSet;
                Sprite[] idleFrames = animationSet != null ? animationSet.IdleFrames : null;
                Sprite heroSprite = heroData.StaticFallbackSprite
                    ?? FirstFrame(idleFrames)
                    ?? heroData.SelectionPreviewSprite
                    ?? heroData.Portrait;
                ConfigurePreview(heroPreviewImage, heroSprite, idleFrames,
                    heroData.SourceFacing == CharacterFacing.Right, true);
            }
            else
                powerText.text = "—";

            if (selectedBossData != null)
            {
                RuntimeBossStats bossStats = new RuntimeBossStats(selectedBossData);
                popupEnemyName.text = CombatPowerCalculator.Calculate(
                    bossStats.MaxHP, bossStats.Attack, 0, GetLayout())
                    .ToString("N0", CultureInfo.InvariantCulture);
            }
            else
                popupEnemyName.text = "—";

            CharacterAnimationSet bossAnimation = selectedBossData != null
                ? selectedBossData.AnimationSet
                : null;
            Sprite[] bossFrames = bossAnimation != null ? bossAnimation.IdleFrames : null;
            Sprite bossSprite = FirstFrame(bossFrames)
                ?? (selectedBossData != null ? selectedBossData.Portrait : null);
            ConfigurePreview(bossPreviewImage, bossSprite, bossFrames,
                selectedBossData != null && selectedBossData.SourceFacing == CharacterFacing.Right,
                false);
        }

        void ConfigurePreview(Image image, Sprite fallback, Sprite[] frames,
            bool sourceFacesRight, bool shouldFaceRight)
        {
            StageCharacterPreview preview = image.GetComponent<StageCharacterPreview>();
            if (preview == null)
                preview = image.gameObject.AddComponent<StageCharacterPreview>();
            preview.Configure(image, fallback, frames, GetLayout().IdleFrameSeconds,
                sourceFacesRight, shouldFaceRight);
        }

        static Sprite FirstFrame(Sprite[] frames)
        {
            return frames != null && frames.Length > 0 ? frames[0] : null;
        }

        void BuildRewards(StageData stage, BossData boss)
        {
            for (int i = rewardArea.childCount - 1; i >= 0; i--)
                Destroy(rewardArea.GetChild(i).gameObject);
            if (rewardArea.GetComponent<HorizontalLayoutGroup>() == null)
                throw new MissingComponentException("RewardContent requires a HorizontalLayoutGroup.");

            List<RewardPreviewEntry> entries = GetRewardEntries(stage, boss);
            if (entries.Count == 0)
            {
                entries.Add(new RewardPreviewEntry(GetLayout().GoldIcon, "GOLD", "+0", string.Empty));
                entries.Add(new RewardPreviewEntry(GetLayout().ExperienceIcon, "EXP", "+0", string.Empty));
            }

            int visibleCount = Mathf.Min(entries.Count, GetLayout().MaxRewardEntries);
            for (int i = 0; i < visibleCount; i++)
            {
                RewardPreviewEntry entry = entries[i];
                RectTransform itemRoot = CampaignUIFactory.CreateRect($"Reward_{i + 1}",
                    rewardArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                LayoutElement layoutElement = itemRoot.gameObject.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1f;
                layoutElement.minWidth = 80f;

                RectTransform iconRect = CampaignUIFactory.CreateRect("Icon", itemRoot,
                    new Vector2(GetLayout().RewardIconRect.xMin, GetLayout().RewardIconRect.yMin),
                    new Vector2(GetLayout().RewardIconRect.xMax, GetLayout().RewardIconRect.yMax),
                    Vector2.zero, Vector2.zero);
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = entry.Icon;
                icon.enabled = entry.Icon != null;
                icon.color = Color.white;
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                RectTransform labelRect = CampaignUIFactory.CreateRect("RewardText", itemRoot,
                    new Vector2(GetLayout().RewardTextRect.xMin, GetLayout().RewardTextRect.yMin),
                    new Vector2(GetLayout().RewardTextRect.xMax, GetLayout().RewardTextRect.yMax),
                    Vector2.zero, Vector2.zero);
                TextMeshProUGUI label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                label.text = string.IsNullOrEmpty(entry.Status)
                    ? $"{entry.Name}\n{entry.Amount}"
                    : $"{entry.Name}\n{entry.Amount}\n{entry.Status}";
                label.font = TMP_Settings.defaultFontAsset;
                label.fontSize = GetLayout().RewardFontSize;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 40f;
                label.enableAutoSizing = true;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = GetLayout().TextColor;
                label.outlineColor = GetLayout().OutlineColor;
                label.outlineWidth = GetLayout().OutlineWidth;
                label.raycastTarget = false;
            }
        }

        List<RewardPreviewEntry> GetRewardEntries(StageData stage, BossData boss)
        {
            var entries = new List<RewardPreviewEntry>();
            if (gameConfigService == null || stage == null)
                return entries;

            string lootTableId = !string.IsNullOrEmpty(stage.LootTableId)
                ? stage.LootTableId
                : boss != null ? boss.DefaultLootTableId : string.Empty;
            LootTableConfig table = gameConfigService.GetLootTable(lootTableId);
            if (table == null)
                return entries;

            bool firstClear = !GameProgressService.IsStageCompleted(stage.StageId);
            foreach (LootTableEntryConfig entry in table.Entries)
            {
                if (entry == null)
                    continue;
                if (entry.DropChance <= 0f || entry.MinimumQuantity <= 0
                    || entry.MaximumQuantity < entry.MinimumQuantity)
                    continue;
                ItemConfig item = gameConfigService.GetItem(entry.ItemId);
                if (item == null)
                    continue;

                string amount = entry.MinimumQuantity == entry.MaximumQuantity
                    ? $"×{entry.MinimumQuantity}"
                    : $"×{entry.MinimumQuantity}-{entry.MaximumQuantity}";
                if (entry.DropChance < 1f)
                    amount += $"  {Mathf.RoundToInt(entry.DropChance * 100f)}%";
                string status = entry.FirstClearOnly
                    ? firstClear ? "FIRST CLEAR" : "CLAIMED"
                    : string.Empty;
                entries.Add(new RewardPreviewEntry(item.RewardIcon,
                    item.DisplayName, amount, status));
            }
            return entries;
        }

        readonly struct RewardPreviewEntry
        {
            public readonly Sprite Icon;
            public readonly string Name;
            public readonly string Amount;
            public readonly string Status;

            public RewardPreviewEntry(Sprite icon, string name, string amount, string status)
            {
                Icon = icon;
                Name = name;
                Amount = amount;
                Status = status;
            }
        }

        void FitMapContent()
        {
            Canvas.ForceUpdateCanvases();
            if (region.MapSprite == null)
                return;
            float width = mapViewportRect.rect.width;
            mapViewportHeight = mapViewportRect.rect.height;
            float aspect = region.MapSprite.rect.width / region.MapSprite.rect.height;
            mapArtworkWidth = Mathf.Max(width, mapViewportHeight * aspect);
            mapArtworkHeight = Mathf.Max(mapViewportHeight, mapArtworkWidth / aspect);
            mapContentHeight = mapArtworkHeight;
            if (region.MapLayoutPrefab != null)
            {
                mapLayoutScale = mapArtworkWidth / region.MapSprite.rect.width;
                mapContentRect.sizeDelta = new Vector2(region.MapSprite.rect.width - width,
                    region.MapSprite.rect.height);
                mapContentRect.localScale = Vector3.one * mapLayoutScale;
                return;
            }

            mapLayoutScale = 1f;
            mapContentRect.sizeDelta = new Vector2(mapArtworkWidth - width, mapContentHeight);
            mapArtworkRect.sizeDelta = new Vector2(mapArtworkWidth, mapArtworkHeight);
            mapArtworkRect.anchoredPosition = Vector2.zero;
        }

        IEnumerator ScrollToCurrentStageOnce()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (mapScrollRect == null || mapContentHeight <= mapViewportHeight + .5f)
                yield break;

            StageData target = null;
            StageData latestCompleted = null;
            foreach (StageData stage in region.Stages)
            {
                if (stage == null)
                    continue;
                CampaignNodeState state = GameProgressService.GetStageState(region, stage);
                if (state == CampaignNodeState.Available)
                {
                    target = stage;
                    break;
                }
                if (state == CampaignNodeState.Completed)
                    latestCompleted = stage;
            }
            if (target == null)
                target = latestCompleted;
            if (target == null && region.Stages.Count > 0)
                target = region.Stages[0];
            if (target == null)
                yield break;

            if (!stageMarkersById.TryGetValue(target.StageId, out StageMarkerView markerView))
                yield break;
            Vector3 markerLocalPosition = mapArtworkRect.InverseTransformPoint(markerView.transform.position);
            float stageTop = -markerLocalPosition.y * mapLayoutScale;
            float maxScroll = mapContentHeight - mapViewportHeight;
            float visibleCenterFromTop = headerHeight
                + (mapViewportHeight - headerHeight - footerHeight) * .5f;
            float scrollFromTop = Mathf.Clamp(stageTop - visibleCenterFromTop, 0f, maxScroll);
            mapScrollRect.verticalNormalizedPosition = 1f - scrollFromTop / maxScroll;
        }

        void OnStageSelected(StageData stage)
        {
            CampaignNodeState state = GameProgressService.GetStageState(region, stage);
            if (state == CampaignNodeState.Locked)
            {
                ShowLockedTooltip(stage);
                return;
            }

            if (tooltipRoutine != null)
                StopCoroutine(tooltipRoutine);
            tooltipRoot.gameObject.SetActive(false);
            selectedStage = stage;
            selectedBossData = campaign.ResolveBossData(stage, this);
            popupStageLabel.text = $"STAGE {stage.StageNumber:00}";
            dimOverlayImage.gameObject.SetActive(true);
            stageInfoPopupRoot.gameObject.SetActive(true);
            ConfigureCharacterPreviews();
            BuildRewards(stage, selectedBossData);
        }

        void ShowLockedTooltip(StageData stage)
        {
            tooltipText.text = stage.IsBossStage
                ? $"BOSS STAGE {stage.StageNumber:00} LOCKED\nComplete the previous stage to challenge this boss."
                : $"STAGE {stage.StageNumber:00} LOCKED\nComplete the previous stage to unlock this location.";
            tooltipRoot.gameObject.SetActive(true);
            if (tooltipRoutine != null)
                StopCoroutine(tooltipRoutine);
            tooltipRoutine = StartCoroutine(HideTooltip());
        }

        IEnumerator HideTooltip()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            tooltipRoot.gameObject.SetActive(false);
            tooltipRoutine = null;
        }

        void ClosePopup()
        {
            selectedStage = null;
            selectedBossData = null;
            if (stageInfoPopupRoot != null)
                stageInfoPopupRoot.gameObject.SetActive(false);
            if (dimOverlayImage != null)
                dimOverlayImage.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            if (stageInfoPopupRoot != null && stageInfoPopupRoot.gameObject.activeSelf)
            {
                ClosePopup();
                return;
            }
            if (tooltipRoot != null && tooltipRoot.gameObject.activeSelf)
            {
                if (tooltipRoutine != null)
                    StopCoroutine(tooltipRoutine);
                tooltipRoutine = null;
                tooltipRoot.gameObject.SetActive(false);
                return;
            }
            else
                ReturnToWorldMap();
        }

        void ReturnToWorldMap()
        {
            SceneManager.LoadScene(CampaignManager.WorldMapSceneName);
        }

        void StartSelectedStage()
        {
            if (selectedStage == null
                || !campaign.BeginStage(region, selectedStage, selectedBossData))
                Debug.LogError("Could not start the selected Campaign stage.", this);
        }

        static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
