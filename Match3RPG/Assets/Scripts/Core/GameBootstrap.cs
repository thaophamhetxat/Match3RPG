using Match3RPG.Battle;
using Match3RPG.Board;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Persistence;
using Match3RPG.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Match3RPG.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        const int RuntimeBoardSize = BoardManager.DefaultBoardSize;
        const float BattleLayoutAspectRatio = 722f / 1587f;
        const float HeroVisualScaleMultiplier = .85f;
        const float BossVisualScaleMultiplier = .83f;
        const float BoardCenterY = .314f;
        // Normalized bounds of the painted 7x7 cells in the 1440x3200 BattleLayout_V2.
        static readonly Rect BattleLayoutGridRegion = Rect.MinMaxRect(
            128f / 1440f, 1f - 2765f / 3200f,
            1317f / 1440f, 1f - 1591f / 3200f);
        const float CharacterGroundY = .545f;
        static readonly Color Gold = new Color32(245, 190, 88, 255);
        static readonly Color[] GemColors =
        {
            new Color32(240, 85, 76, 255),
            new Color32(80, 167, 232, 255),
            new Color32(91, 196, 117, 255),
            new Color32(248, 203, 79, 255),
            new Color32(255, 126, 177, 255),
            new Color32(255, 142, 48, 255),
            new Color32(190, 225, 238, 255),
            new Color32(216, 160, 255, 255),
            new Color32(169, 99, 222, 255),
            new Color32(46, 48, 56, 255)
        };
    #pragma warning disable 0649
        [SerializeField] BattleHUDLayoutConfig hudLayoutConfig;
        [SerializeField] Sprite[] gemV2BaseSprites = new Sprite[10];
        [SerializeField] Sprite[] gemV2SpecialSprites = new Sprite[7];
        [SerializeField] Sprite rainbowActiveSprite;
        [SerializeField] Sprite horizontalBeamSprite;
        [SerializeField] Sprite verticalBeamSprite;
        [SerializeField] Sprite purpleBurstSprite;
        [SerializeField] Sprite prismaticBurstSprite;
        [SerializeField] Sprite boardBackgroundSprite;
        [SerializeField] Sprite boardFrameSprite;
        [SerializeField] Sprite turnPanelBackgroundSprite;
        [SerializeField] Sprite timerIconSprite;
        [SerializeField] Sprite barBackgroundSprite;
        [SerializeField] Sprite barFrameSprite;
        [SerializeField] Sprite buttonNormalSprite;
        [SerializeField] Sprite buttonPressedSprite;
        [SerializeField] Sprite battleBackgroundSprite;
        [SerializeField] Sprite battleLayoutV2Sprite;
        [SerializeField] Sprite bossIdleSprite;
        [SerializeField] Sprite tabBagSprite;
        [SerializeField] Sprite tabSkillsSprite;
        [SerializeField] Sprite tabSettingsSprite;
        [SerializeField] Sprite victorySprite;
        [SerializeField] Sprite defeatSprite;
        [SerializeField] Sprite heroHpBarSprite;
        [SerializeField] Sprite bossHpBarSprite;
        [SerializeField] Sprite soulHpBarSprite;
        [SerializeField] Sprite rageHpBarSprite;
    #pragma warning restore 0649
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] bool tintHpFillForAlignment;
        [SerializeField] bool validateGemVisualAlignmentOnStartup;
#endif
        RectTransform canvasRect;
        RectTransform contentRootRect;
        RectTransform heroAnchorRect;
        RectTransform bossAnchorRect;
        RectTransform heroHudAnchorRect;
        RectTransform bossHudAnchorRect;
        RectTransform turnInfoAnchorRect;
        RectTransform turnInfoLayerRect;
        RectTransform menuHitAreaRect;
        RectTransform bottomActionAnchorRect;
        RectTransform battleAreaRect;
        RectTransform boardAreaRect;
        RectTransform bottomAreaRect;
        RectTransform battleLayoutReferenceRoot;
        Image battleLayoutV2BackgroundImage;
        RectTransform boardRootRect;
        RectTransform gemViewportRect;
        RectTransform selectionOverlayRect;
        BoardSelectionOverlay boardSelectionOverlay;
        BoardOutsideClickCatcher boardOutsideClickCatcher;
        RectTransform cellBackgroundGridRect;
        RectTransform boardVfxLayerRect;
        RectTransform boardFrameRect;
        RectTransform boardRageFireVfxRect;
        GridLayoutGroup boardGrid;
        GridLayoutGroup cellBackgroundGrid;
        BoardRageFireVFX boardRageFireVfx;
        BoardManager boardManager;
        BoardCellView[,] boardCells;
        CharacterView heroCharacterView;
        CharacterView bossCharacterView;
        Slider heroHpBar;
        Slider heroSoulHpBar;
        Slider heroRageBar;
        Slider bossHpBar;
        Slider bossSoulHpBar;
        Slider bossRageBar;
        TMP_Text heroNameText;
        TMP_Text bossNameText;
        TMP_Text turnCountText;
        TMP_Text timerText;
        TMP_Text heroHpValueText;
        TMP_Text bossHpValueText;
        TMP_Text heroHpSegmentText;
        TMP_Text bossHpSegmentText;
        TMP_Text heroRageValueText;
        TMP_Text bossRageValueText;
        TurnIndicator turnIndicator;
        Sprite barFillSprite;
        BattleHUDLayoutValues hudLayoutValues;
        bool hudLayoutFallbackWarningLogged;
        bool invalidHudChannelsWarningLogged;
        Vector2 lastCanvasSize = new Vector2(-1f, -1f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool battleLayoutDiagnosticsLogged;
        bool gemVisualAlignmentValidated;
        bool gemVisualAlignmentValidationScheduled;
#endif

        void Awake()
        {
            BattleHUDPreviewMarker.CleanupScenePreview();
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            ResolveBattleBarSprites();
            BuildRuntimeGame();
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            BattleHUDLayoutConfig.LayoutChanged += OnHudLayoutConfigChanged;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            BattleHUDLayoutConfig.LayoutChanged -= OnHudLayoutConfigChanged;
#endif
        }

#if UNITY_EDITOR
        void OnHudLayoutConfigChanged(BattleHUDLayoutConfig changedConfig)
        {
            if (changedConfig == hudLayoutConfig)
                ApplyHudLayout();
        }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Development/Play Hero Attack Animation")]
        void PlayHeroAttackAnimation()
        {
            if (heroCharacterView == null || !heroCharacterView.HasSpriteAttackAnimation)
            {
                Debug.LogError("[Hero2] Forced Attack test cannot run: Hero animation is not ready.", this);
                return;
            }

            StartCoroutine(heroCharacterView.Attack());
        }
#endif

        void ResolveBattleBarSprites()
        {
            heroHpBarSprite = ResolveBattleBarSprite(heroHpBarSprite,
                "Assets/Art/Battle/BarHP/Bar_HPNormal_Hero.png",
                "Battle/BarHP/Bar_HPNormal_Hero");
            bossHpBarSprite = ResolveBattleBarSprite(bossHpBarSprite,
                "Assets/Art/Battle/BarHP/Bar_HPNormal_Boss.png",
                "Battle/BarHP/Bar_HPNormal_Boss");
            soulHpBarSprite = ResolveBattleBarSprite(soulHpBarSprite,
                "Assets/Art/Battle/BarHP/Bar_HPSoul.png",
                "Battle/BarHP/Bar_HPSoul");
            rageHpBarSprite = ResolveBattleBarSprite(rageHpBarSprite,
                "Assets/Art/Battle/BarHP/Bar_Rage.png",
                "Battle/BarHP/Bar_Rage");

            if (heroHpBarSprite == null || bossHpBarSprite == null || soulHpBarSprite == null || rageHpBarSprite == null)
            {
                Debug.LogWarning("Battle HUD bar sprites are still unresolved. Expected Bar_HPNormal_Hero, " +
                    "Bar_HPNormal_Boss, Bar_HPSoul, and Bar_Rage under Assets/Art/Battle/BarHP or under Assets/Resources/Battle/BarHP.", this);
            }
        }

        static Sprite ResolveBattleBarSprite(Sprite assignedSprite, string editorAssetPath, string resourcesPath)
        {
            if (assignedSprite != null)
                return assignedSprite;
#if UNITY_EDITOR
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(editorAssetPath);
            if (sprite != null)
                return sprite;
#endif
            if (!string.IsNullOrEmpty(resourcesPath))
            {
                Sprite loaded = Resources.Load<Sprite>(resourcesPath);
                if (loaded != null)
                    return loaded;

                string fileName = resourcesPath.Substring(resourcesPath.LastIndexOf('/') + 1);
                loaded = Resources.Load<Sprite>(fileName);
                if (loaded != null)
                    return loaded;
            }
            return null;
        }

        static string GetObjectPath(Transform transform)
        {
            if (transform == null)
                return "(null)";
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return "/" + path;
        }

        void LogBattleLayoutRuntimeDiagnostics()
        {
            GameBootstrap[] bootstraps =
                FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include);
            bool spriteIsNull = battleLayoutV2Sprite == null;
            string sceneName = SceneManager.GetActiveScene().name;
            string objectPath = GetObjectPath(transform);
            string spriteName = battleLayoutV2Sprite != null ? battleLayoutV2Sprite.name : "(null)";
            string message = $"[BattleLayoutRuntime] Scene={sceneName} ObjectPath={objectPath} "
                + $"SpriteField=battleLayoutV2Sprite SpriteIsNull={spriteIsNull} "
                + $"SpriteName={spriteName} BootstrapCount={bootstraps.Length} "
                + "Config=GameBootstrap scene object ExpectedSpritePath=Assets/Art/Battle/Layouts/BattleLayout_V2/BattleLayout_V2.png";

            if (spriteIsNull)
                Debug.LogError(message, this);
            else
                Debug.Log(message, this);
        }

        void BuildRuntimeGame()
        {
            hudLayoutValues = ResolveHudLayoutValues();
            LogBattleLayoutRuntimeDiagnostics();
            CampaignManager campaign = CampaignManager.GetOrCreate(
                Resources.Load<CampaignCatalog>("Campaign/CampaignCatalog"));
            StageData currentStage = campaign.CurrentStage;
            int bossLevel = 1;
            if (currentStage != null && campaign.Catalog != null)
            {
                int regionIndex = campaign.Catalog.GetRegionIndex(currentStage.RegionId);
                if (regionIndex < 0)
                    Debug.LogError($"Campaign stage '{currentStage.StageId}' has no matching region in the catalog.", this);
                else
                    bossLevel = regionIndex * 10 + currentStage.StageNumber;
            }
            BossData battleBossData = campaign.HasActiveCampaignBattle
                ? campaign.SelectedBossData
                : campaign.ResolveDevelopmentBossData(this);
            if (campaign.HasActiveCampaignBattle && battleBossData == null)
                battleBossData = campaign.ResolveBossData(currentStage, this);
            GameConfigService configService =
                Resources.Load<GameConfigService>("Configuration/GameConfigService");
            if (configService == null)
                Debug.LogError("GameConfigService is missing at Resources/Configuration/GameConfigService.", this);
            SaveManager saveManager = SaveManager.Instance;
            GameSaveData save = saveManager.LoadOrCreate(campaign.Catalog);
            PlayerLoadoutService playerLoadoutService =
                new PlayerLoadoutService(saveManager, configService);
            BattleLoadout battleLoadout = playerLoadoutService.ResolveBattleLoadout();
            HeroData selectedHeroData = battleLoadout.Hero;
            HeroProgressSaveData heroProgress = selectedHeroData != null
                ? PlayerHeroSelectionService.GetHeroProgress(save, selectedHeroData.StableId)
                : null;
            ItemStatValues equipmentStats = new ItemStatValues(0, 0);
            if (configService != null)
            {
                EquipmentService equipmentService = new EquipmentService(saveManager, configService);
                if (equipmentService.ValidateEquipmentLoadout(out System.Collections.Generic.IReadOnlyList<string> errors))
                    equipmentStats = ItemStatCalculator.CalculateEquipped(
                        save.loadout, save.inventory, configService.GetItem);
                else
                    foreach (string error in errors)
                        Debug.LogError($"Invalid equipment loadout: {error}", this);
            }
            RuntimeHeroStats? runtimeHeroStats = selectedHeroData != null
                ? new RuntimeHeroStats(selectedHeroData, heroProgress,
                    equipmentStats.AttackBonus, equipmentStats.MaxHealthBonus,
                    new StarChartGemInventoryService(saveManager).GetTotalStatBonuses())
                : (RuntimeHeroStats?)null;
            RuntimeBossStats? runtimeBossStats = battleBossData != null
                ? new RuntimeBossStats(battleBossData, bossLevel)
                : (RuntimeBossStats?)null;
            CharacterAnimationSet heroAnimationSet = selectedHeroData != null
                ? selectedHeroData.AnimationSet
                : null;
            bool heroAnimationReady = heroAnimationSet != null
                && heroAnimationSet.IsReady
                && heroAnimationSet.HasAttackFrames
                && heroAnimationSet.DeathClip != null
                && heroAnimationSet.HasIdleFrames;
            if (selectedHeroData == null)
                Debug.LogError("[HeroAnimationInit] Hero2 HeroData is unavailable. "
                    + "Run Tools > Characters > Build Character Content.", this);
            else if (!heroAnimationReady)
                Debug.LogError($"[HeroAnimationInit] Hero2 '{selectedHeroData.name}' is incomplete. "
                    + "Idle, Attack frames, Attack clip, controller, and Death clip are required.", this);
            else
                LogDevelopment($"[HeroAnimationInit] HeroId={selectedHeroData.HeroId}; "
                    + $"HeroData={selectedHeroData.name}; "
                    + $"CharacterAnimationSet={heroAnimationSet.name}; "
                    + $"IdleFrames={heroAnimationSet.GetFrameCount(BattleCharacterAnimState.Idle)}; "
                    + $"AttackFrames={heroAnimationSet.GetFrameCount(BattleCharacterAnimState.Attack)}; "
                    + $"SpecAttackFrames={heroAnimationSet.GetFrameCount(BattleCharacterAnimState.SpecAttack)}; "
                    + $"Skill={(heroAnimationSet.SkillClip != null ? "Available" : "Unavailable")}; "
                    + $"Hit={(heroAnimationSet.HitClip != null ? "Available" : "Unavailable")}; "
                    + $"Dodge={(heroAnimationSet.DodgeClip != null ? "Available" : "Unavailable")}; "
                    + $"Rising={(heroAnimationSet.RisingClip != null ? "Available" : "Unavailable")}; "
                    + $"DeathFrames={heroAnimationSet.GetFrameCount(BattleCharacterAnimState.Death)}");
            Sprite battleBossSprite = battleBossData != null && battleBossData.Portrait != null
                ? battleBossData.Portrait
                : bossIdleSprite;
            if (battleBossData != null && battleBossData.Portrait == null)
                Debug.LogWarning($"BossData '{battleBossData.BossId}' has no portrait; using the development Boss sprite.", this);
            CharacterAnimationSet bossAnimationSet = battleBossData != null
                ? battleBossData.AnimationSet
                : null;
            Sprite bossVisualSprite = bossAnimationSet != null
                && bossAnimationSet.IdleFrames != null
                && bossAnimationSet.IdleFrames.Length > 0
                    ? bossAnimationSet.IdleFrames[0]
                    : battleBossSprite;
            Canvas canvas = CreateCanvas();
            canvasRect = (RectTransform)canvas.transform;
            contentRootRect = CreateRect("BattleContentRoot", canvas.transform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            battleLayoutReferenceRoot = CreateRect("BattleLayoutReferenceRoot", contentRootRect,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            battleLayoutReferenceRoot.pivot = new Vector2(.5f, .5f);
            battleLayoutReferenceRoot.sizeDelta = new Vector2(1080f, 2400f);
            battleLayoutReferenceRoot.anchoredPosition = Vector2.zero;
            battleLayoutReferenceRoot.localScale = Vector3.one;
            Image boardTapImage = AddImage(battleLayoutReferenceRoot.gameObject, Color.clear);
            boardTapImage.raycastTarget = true;
            boardOutsideClickCatcher = battleLayoutReferenceRoot.gameObject
                .AddComponent<BoardOutsideClickCatcher>();
            RectTransform layoutLayer = CreateRect("BattleLayoutLayer", battleLayoutReferenceRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image layoutImage = CreateDecorativeImage(layoutLayer, "BattleLayoutV2Background",
                battleLayoutV2Sprite, Vector2.zero, Vector2.one, Color.white, false);
            battleLayoutV2BackgroundImage = layoutImage;
            if (battleLayoutV2Sprite == null)
            {
                Debug.LogError("[BattleLayoutRuntime] BattleLayout_V2 is not assigned on the authoritative GameBootstrap. "
                    + $"Scene={SceneManager.GetActiveScene().name} ObjectPath={GetObjectPath(transform)} "
                + "SpriteField=battleLayoutV2Sprite "
                    + "ExpectedSpritePath=Assets/Art/Battle/Layouts/BattleLayout_V2/BattleLayout_V2.png.", this);
            }
            else
                layoutImage.type = Image.Type.Simple;

            battleAreaRect = CreateRect("CharacterLayer", battleLayoutReferenceRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            heroAnchorRect = CreateRect("HeroAnchor", battleAreaRect,
                new Vector2(.25f, .74f), new Vector2(.25f, .74f), Vector2.zero, Vector2.zero);
            bossAnchorRect = CreateRect("BossAnchor", battleAreaRect,
                new Vector2(.75f, .74f), new Vector2(.75f, .74f), Vector2.zero, Vector2.zero);
            RectTransform effectLayer = CreateRect("VFXLayer", battleLayoutReferenceRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CombatVFXManager combatVfx = effectLayer.gameObject.AddComponent<CombatVFXManager>();

            Sprite configuredHeroIdle = selectedHeroData != null
                ? selectedHeroData.StaticFallbackSprite
                    ?? (heroAnimationSet != null && heroAnimationSet.IdleFrames != null
                        && heroAnimationSet.IdleFrames.Length > 0
                            ? heroAnimationSet.IdleFrames[0]
                            : null)
                : null;
            heroCharacterView = CreateCharacter(heroAnchorRect, "HeroCharacterRoot", "H",
                new Color32(61, 154, 158, 255), Vector2.zero, Vector2.one, configuredHeroIdle,
                CharacterFacing.Right,
                selectedHeroData != null ? selectedHeroData.SourceFacing : CharacterFacing.Right,
                visualScale: selectedHeroData != null
                    ? selectedHeroData.VisualScale * HeroVisualScaleMultiplier
                    : HeroVisualScaleMultiplier,
                    visualOffset: new Vector2(0f, 40f),
                attackDistance: 12f,
                characterAnimationSet: heroAnimationReady ? heroAnimationSet : null,
                idleNormalizationMultiplier: selectedHeroData != null
                    ? selectedHeroData.IdleNormalizationScaleMultiplier
                    : 1f,
                vfxAnchorPosition: selectedHeroData != null
                    ? selectedHeroData.VfxAnchor : new Vector2(.5f, .48f),
                headVfxAnchorPosition: selectedHeroData != null
                    ? selectedHeroData.HeadVfxAnchor : new Vector2(.5f, .82f),
                floatingAnchorPosition: selectedHeroData != null
                    ? selectedHeroData.FloatingTextAnchor : new Vector2(.5f, .88f),
                idleFeetPaddingFraction: GetIdleFeetPaddingFraction(heroAnimationSet));
            bossCharacterView = CreateCharacter(bossAnchorRect, "BossCharacterRoot", "B",
                new Color32(204, 91, 78, 255), Vector2.zero, Vector2.one, bossVisualSprite,
                CharacterFacing.Left,
                battleBossData != null ? battleBossData.SourceFacing : CharacterFacing.Left,
                (battleBossData != null ? battleBossData.VisualScale : 1f) * BossVisualScaleMultiplier,
                battleBossData != null ? battleBossData.VisualOffset : Vector2.zero,
                characterAnimationSet: bossAnimationSet,
                vfxAnchorPosition: battleBossData != null
                    ? battleBossData.VfxAnchor : new Vector2(.5f, .48f),
                headVfxAnchorPosition: battleBossData != null
                    ? battleBossData.HeadVfxAnchor : new Vector2(.5f, .82f),
                floatingAnchorPosition: battleBossData != null
                    ? battleBossData.FloatingTextAnchor : new Vector2(.5f, .88f),
                idleFeetPaddingFraction: GetIdleFeetPaddingFraction(bossAnimationSet));
            combatVfx.Configure(Resources.Load<CombatVFXCatalog>("CombatVFX/CombatVFXCatalog"),
                effectLayer, heroCharacterView, bossCharacterView);

            RectTransform battleHudLayer = CreateRect("BattleHUDLayer", battleLayoutReferenceRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            heroHudAnchorRect = CreateRect("HeroHUDAnchor", battleHudLayer,
                new Vector2(.045f, .865f), new Vector2(.405f, .975f), Vector2.zero, Vector2.zero);
            bossHudAnchorRect = CreateRect("BossHUDAnchor", battleHudLayer,
                new Vector2(.595f, .865f), new Vector2(.955f, .975f), Vector2.zero, Vector2.zero);
            string heroName = selectedHeroData != null
                && !string.IsNullOrWhiteSpace(selectedHeroData.DisplayName)
                    ? selectedHeroData.DisplayName.ToUpperInvariant()
                    : "HERO";
            heroNameText = CreateRuntimeText(battleLayoutReferenceRoot, "HeroName", heroName,
                hudLayoutValues.HeroNameSlot.min, hudLayoutValues.HeroNameSlot.max,
                hudLayoutValues.HeroNameStyle.FontSize,
                hudLayoutValues.HeroNameStyle.Alignment, hudLayoutValues.HeroNameStyle.Color);
            ApplyTextStyle(heroNameText, hudLayoutValues.HeroNameStyle);
            string bossName = battleBossData != null && !string.IsNullOrWhiteSpace(battleBossData.DisplayName)
                ? battleBossData.DisplayName.ToUpperInvariant()
                : "BOSS";
            bossNameText = CreateRuntimeText(battleLayoutReferenceRoot, "BossName", bossName,
                hudLayoutValues.BossNameSlot.min, hudLayoutValues.BossNameSlot.max,
                hudLayoutValues.BossNameStyle.FontSize,
                hudLayoutValues.BossNameStyle.Alignment, hudLayoutValues.BossNameStyle.Color);
            ApplyTextStyle(bossNameText, hudLayoutValues.BossNameStyle);
            string stageLabel = currentStage != null ? $"STAGE {currentStage.StageNumber}" : "STAGE 1";
            CreateRuntimeText(battleHudLayer, "StageText", stageLabel,
                new Vector2(.42f, .90f), new Vector2(.58f, .97f), 18,
                TextAlignmentOptions.Center, Color.white);

            heroHpBar = CreateBar(battleLayoutReferenceRoot, "HeroHPBar", Vector2.zero,
                Vector2.one, new Color32(89, 197, 128, 255), heroHpBarSprite, out heroHpValueText,
                hudLayoutValues.HpValueStyle);
            heroHpSegmentText = CreateHpSegmentText(heroHpBar.transform,
                "HeroHPSegmentCount", true);
            heroSoulHpBar = CreateBar(battleLayoutReferenceRoot, "HeroSoulHPBar", Vector2.zero,
                Vector2.one, new Color32(139, 128, 224, 255), soulHpBarSprite, out _,
                hudLayoutValues.RageValueStyle);
            heroRageBar = CreateBar(battleLayoutReferenceRoot, "HeroRageBar", Vector2.zero,
                Vector2.one, new Color32(242, 112, 43, 255), rageHpBarSprite, out heroRageValueText,
                hudLayoutValues.RageValueStyle);
            bossHpBar = CreateBar(battleLayoutReferenceRoot, "BossHPBar", Vector2.zero,
                Vector2.one, new Color32(226, 91, 78, 255), bossHpBarSprite, out bossHpValueText,
                hudLayoutValues.HpValueStyle);
            bossHpSegmentText = CreateHpSegmentText(bossHpBar.transform,
                "BossHPSegmentCount", false);
            bossSoulHpBar = CreateBar(battleLayoutReferenceRoot, "BossSoulHPBar", Vector2.zero,
                Vector2.one, new Color32(139, 128, 224, 255), soulHpBarSprite, out _,
                hudLayoutValues.RageValueStyle);
            bossRageBar = CreateBar(battleLayoutReferenceRoot, "BossRageBar", Vector2.zero,
                Vector2.one, new Color32(242, 112, 43, 255), rageHpBarSprite, out bossRageValueText,
                hudLayoutValues.RageValueStyle);
            turnInfoLayerRect = CreateRect("TurnInfoLayer", battleLayoutReferenceRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            turnInfoAnchorRect = CreateRect("TurnInfoAnchor", turnInfoLayerRect,
                hudLayoutValues.TurnInfoAnchorPosition, hudLayoutValues.TurnInfoAnchorPosition,
                Vector2.zero, Vector2.zero);
            TMP_Text stateText = CreateTurnText(turnInfoAnchorRect, "StateText",
                Vector2.zero, Vector2.one, hudLayoutValues.TurnCountStyle);
            turnCountText = CreateTurnText(turnInfoAnchorRect, "TurnsText",
                hudLayoutValues.TurnCountPosition, hudLayoutValues.TurnCountSize,
                hudLayoutValues.TurnCountStyle);
            timerText = CreateTurnText(turnInfoAnchorRect, "TimerText",
                hudLayoutValues.TimerPosition, hudLayoutValues.TimerSize,
                hudLayoutValues.TimerStyle);
            stateText.gameObject.SetActive(false);
            turnIndicator = turnInfoAnchorRect.gameObject.AddComponent<TurnIndicator>();
            turnIndicator.Configure(stateText, turnCountText, timerText);
            turnIndicator.ConfigureTimerColors(hudLayoutValues.TimerStyle.Color,
                hudLayoutValues.TimerWarningColor, hudLayoutValues.TimerDangerColor);

            RectTransform boardArea = CreateRect("BoardAnchor", battleLayoutReferenceRoot,
                new Vector2(.5f, BoardCenterY), new Vector2(.5f, BoardCenterY),
                Vector2.zero, Vector2.zero);
            boardAreaRect = boardArea;
            boardFrameRect = CreateRect("BoardFrameAnchor", boardArea,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            boardRageFireVfxRect = CreateRect("RageFrameVFX", boardArea,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            boardRageFireVfx = boardRageFireVfxRect.gameObject.AddComponent<BoardRageFireVFX>();
            RectTransform gemViewport = CreateRect("BoardViewport", boardArea,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            gemViewportRect = gemViewport;
            gemViewportRect.localScale = Vector3.one;
            gemViewport.gameObject.AddComponent<RectMask2D>();
            RectTransform cellBackgroundGridObject = CreateRect("CellBackgroundGrid", gemViewport,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cellBackgroundGridRect = cellBackgroundGridObject;
            cellBackgroundGridRect.localScale = Vector3.one;
            cellBackgroundGrid = cellBackgroundGridObject.gameObject.AddComponent<GridLayoutGroup>();
            ConfigureGridLayout(cellBackgroundGrid);
            boardCells = BuildStableCellBackgrounds(cellBackgroundGridObject);

            RectTransform gemAnimationLayer = CreateRect("GemAnimationLayer", gemViewport,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            gemAnimationLayer.localScale = Vector3.one;
            RectTransform boardRoot = CreateRect("CellGrid", gemAnimationLayer,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            boardRootRect = boardRoot;
            boardRootRect.localScale = Vector3.one;
            boardGrid = boardRoot.gameObject.AddComponent<GridLayoutGroup>();
            ConfigureGridLayout(boardGrid);
            boardVfxLayerRect = CreateRect("BoardVFXLayer", gemViewport,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            selectionOverlayRect = CreateRect("SelectionOverlayLayer", boardArea,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            selectionOverlayRect.localScale = Vector3.one;
            selectionOverlayRect.SetAsLastSibling();
            boardSelectionOverlay = selectionOverlayRect.gameObject
                .AddComponent<BoardSelectionOverlay>();
            boardSelectionOverlay.Configure(gemViewport);
            GemSpecialVFXManager specialVfx = gemViewport.gameObject.AddComponent<GemSpecialVFXManager>();
            specialVfx.Configure(gemViewport, boardRoot, purpleBurstSprite,
                prismaticBurstSprite, boardCells, boardVfxLayerRect);
            ValidateGemViewportHierarchy(boardRoot);

            bottomActionAnchorRect = CreateRect("BottomActionLayer", battleLayoutReferenceRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bottomAreaRect = CreateRect("BottomControlsAnchor", bottomActionAnchorRect,
                new Vector2(.5f, .09f), new Vector2(.5f, .09f), Vector2.zero, Vector2.zero);
            RectTransform bottomHud = CreateRect("BottomHUD", bottomAreaRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform manaPanel = CreateRect("ManaPanel", bottomHud,
                new Vector2(.20f, .72f), new Vector2(.46f, .98f), Vector2.zero, Vector2.zero);
            RectTransform ultimatePanel = CreateRect("UltimatePanel", bottomHud,
                new Vector2(.54f, .72f), new Vector2(.80f, .98f), Vector2.zero, Vector2.zero);
            Slider manaBar = CreateBar(manaPanel, "Mana",
                new Vector2(0f, .18f), new Vector2(1f, .62f),
                new Color32(80, 167, 232, 255), out TMP_Text manaValue, 14, 2f);
            manaBar.gameObject.SetActive(false);
            Slider ultimateBar = CreateBar(ultimatePanel, "Ultimate",
                new Vector2(0f, .18f), new Vector2(1f, .62f),
                new Color32(177, 112, 226, 255), out TMP_Text ultimateValue, 14, 2f);
            ultimateBar.gameObject.SetActive(false);
            boardRageFireVfx.Configure(boardFrameRect, 1f);
            Canvas.ForceUpdateCanvases();
            UpdateResponsiveBoardLayout();
            ApplyHudLayout();

            RectTransform hudObject = CreateRect("BattleHUD", battleHudLayer,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BattleHUD hud = hudObject.gameObject.AddComponent<BattleHUD>();
            hud.Configure(heroHpBar, bossHpBar, heroSoulHpBar, bossSoulHpBar,
                heroRageBar, bossRageBar, manaBar, ultimateBar, heroHpValueText,
                bossHpValueText, heroRageValueText, bossRageValueText, manaValue, ultimateValue,
                heroHpSegmentText, bossHpSegmentText);

            GameObject boardObject = new GameObject("BoardManager");
            BoardManager board = boardObject.AddComponent<BoardManager>();
            boardManager = board;
            Sprite[] gemSprites = ResolveGemSprites();
            Sprite[] specialSprites = ResolveGemV2SpecialSprites(gemSprites);
            GemVisualDatabase gemVisuals = new GemVisualDatabase(gemSprites,
                specialSprites, rainbowActiveSprite);
            GemView gemPrefab = CreateGemTemplate(gemVisuals);
            board.Configure(boardRoot, gemPrefab, gemSprites, RuntimeBoardSize, RuntimeBoardSize,
                specialVfx, gemVisuals, boardCells, boardSelectionOverlay,
                runtimeHeroStats.HasValue);
            boardOutsideClickCatcher.Configure(board);

            GameObject battleObject = new GameObject("BattleController");
            BattleController controller = battleObject.AddComponent<BattleController>();
            RectTransform floatingTextLayer = CreateRect("FloatingTextLayer", battleLayoutReferenceRoot,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            floatingTextLayer.SetAsLastSibling();
            FloatingTextManager floatingTexts = floatingTextLayer.gameObject.AddComponent<FloatingTextManager>();
            floatingTexts.Configure();
            controller.Configure(heroCharacterView, bossCharacterView, hud, floatingTexts,
                battleBossData, combatVfx, boardRageFireVfx, selectedHeroData,
                runtimeHeroStats, runtimeBossStats);

            GameObject bossAIObject = new GameObject("BossAI");
            BossAI bossAI = bossAIObject.AddComponent<BossAI>();
            GameObject turnManagerObject = new GameObject("TurnManager");
            TurnManager turnManager = turnManagerObject.AddComponent<TurnManager>();
            turnManager.Configure(board, controller, bossAI, turnIndicator);

            GameObject statusPopupObject = new GameObject("BattleStatusPopup", typeof(RectTransform));
            BattleStatusPopup statusPopup = statusPopupObject.AddComponent<BattleStatusPopup>();
            statusPopup.Configure(canvasRect, tabBagSprite,
                tabSkillsSprite, tabSettingsSprite, turnManager, board,
                boardSelectionOverlay, specialVfx, boardRageFireVfx, combatVfx,
                floatingTexts, campaign);
            menuHitAreaRect = CreateMenuHitArea(battleLayoutReferenceRoot, statusPopup);
            statusPopup.transform.SetAsLastSibling();

            GameObject resultPopupObject = new GameObject("BattleResultPopup", typeof(RectTransform));
            BattleResultPopup resultPopup = resultPopupObject.AddComponent<BattleResultPopup>();
            resultPopup.Configure(canvasRect, turnManager, board,
                boardSelectionOverlay, specialVfx, boardRageFireVfx, combatVfx,
                floatingTexts, campaign, configService, victorySprite, defeatSprite);
            resultPopupObject.transform.SetAsLastSibling();
            turnManagerObject.AddComponent<CampaignBattleBridge>()
                .Configure(turnManager, resultPopup);

            CreateEventSystem();
        }

        static void LogDevelopment(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message);
#endif
        }

        void LateUpdate()
        {
            UpdateResponsiveBoardLayout();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (validateGemVisualAlignmentOnStartup && !gemVisualAlignmentValidated
                && !gemVisualAlignmentValidationScheduled && boardManager != null
                && boardManager.IsBoardReady && boardCells != null
                && battleLayoutReferenceRoot != null)
            {
                gemVisualAlignmentValidationScheduled = true;
                StartCoroutine(ValidateGemVisualAlignmentAfterLayout());
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator ValidateGemVisualAlignmentAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            if (boardGrid != null && boardGrid.transform is RectTransform gridRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            Canvas.ForceUpdateCanvases();
            ValidateGemVisualAlignment();
        }

        void ValidateGemVisualAlignment()
        {
            if (gemVisualAlignmentValidated || boardManager == null || !boardManager.IsBoardReady
                || boardCells == null || battleLayoutReferenceRoot == null)
                return;

            var gems = boardManager.GetGemsInLayoutOrder();
            if (gems.Count != RuntimeBoardSize * RuntimeBoardSize)
            {
                Debug.LogWarning($"Gem visual center alignment check expected "
                    + $"{RuntimeBoardSize * RuntimeBoardSize} Gems but found {gems.Count}.", this);
                gemVisualAlignmentValidated = true;
                return;
            }

            float maximumError = 0f;
            GemView worstGem = null;
            foreach (GemView gem in gems)
            {
                BoardCellView cell = boardCells[gem.Position.Row, gem.Position.Col];
                float error = gem.GetVisualCenterErrorIn(
                    battleLayoutReferenceRoot, cell.RectTransform);
                if (error > maximumError)
                {
                    maximumError = error;
                    worstGem = gem;
                }
            }

            if (maximumError > 1f && worstGem != null)
                Debug.LogWarning($"Gem visual center alignment check found a maximum offset "
                    + $"of {maximumError:F2} reference pixels at "
                    + $"Cell ({worstGem.Position.Row},{worstGem.Position.Col}).", worstGem);
            else
                Debug.Log($"Validated {gems.Count} Gem/Cell centers; maximum error "
                    + $"{maximumError:F3} reference pixels.");
            gemVisualAlignmentValidated = true;
        }
#endif

        void UpdateResponsiveBoardLayout()
        {
            if (canvasRect == null || contentRootRect == null || battleLayoutReferenceRoot == null || heroAnchorRect == null
                || bossAnchorRect == null || turnInfoAnchorRect == null || boardAreaRect == null
                || bottomAreaRect == null || boardRootRect == null || gemViewportRect == null
                || boardGrid == null || cellBackgroundGridRect == null || cellBackgroundGrid == null)
                return;

            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;
            if (canvasWidth <= 0f || canvasHeight <= 0f) return;
            if (Mathf.Abs(lastCanvasSize.x - canvasWidth) < .5f && Mathf.Abs(lastCanvasSize.y - canvasHeight) < .5f) return;
            lastCanvasSize = new Vector2(canvasWidth, canvasHeight);

            contentRootRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);
            battleLayoutReferenceRoot.anchorMin = new Vector2(.5f, .5f);
            battleLayoutReferenceRoot.anchorMax = new Vector2(.5f, .5f);
            battleLayoutReferenceRoot.pivot = new Vector2(.5f, .5f);
            battleLayoutReferenceRoot.sizeDelta = new Vector2(1080f, 2400f);
            battleLayoutReferenceRoot.anchoredPosition = Vector2.zero;
            float uniformFitScale = Mathf.Min(canvasWidth / 1080f, canvasHeight / 1920f);
            battleLayoutReferenceRoot.localScale = new Vector3(uniformFitScale, uniformFitScale, 1f);

            if (battleLayoutV2BackgroundImage != null)
            {
                battleLayoutV2BackgroundImage.preserveAspect = false;
                battleLayoutV2BackgroundImage.raycastTarget = false;
                battleLayoutV2BackgroundImage.rectTransform.anchorMin = Vector2.zero;
                battleLayoutV2BackgroundImage.rectTransform.anchorMax = Vector2.one;
                battleLayoutV2BackgroundImage.rectTransform.pivot = new Vector2(.5f, .5f);
                battleLayoutV2BackgroundImage.rectTransform.offsetMin = Vector2.zero;
                battleLayoutV2BackgroundImage.rectTransform.offsetMax = Vector2.zero;
                battleLayoutV2BackgroundImage.rectTransform.anchoredPosition = Vector2.zero;
                battleLayoutV2BackgroundImage.rectTransform.localScale = Vector3.one;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!battleLayoutDiagnosticsLogged)
            {
                float sourceWidth = battleLayoutV2BackgroundImage != null && battleLayoutV2BackgroundImage.sprite != null
                    ? battleLayoutV2BackgroundImage.sprite.rect.width
                    : 0f;
                float sourceHeight = battleLayoutV2BackgroundImage != null && battleLayoutV2BackgroundImage.sprite != null
                    ? battleLayoutV2BackgroundImage.sprite.rect.height
                    : 0f;
                float sourceAspect = sourceWidth > 0f && sourceHeight > 0f ? sourceWidth / sourceHeight : 0f;
                Debug.Log("[BattleLayoutRuntime] "
                    + $"viewportWidth={canvasWidth:F1} viewportHeight={canvasHeight:F1} "
                    + "referenceWidth=1080 referenceHeight=2400 "
                    + $"uniformFitScale={uniformFitScale:F4} "
                    + $"BattleLayoutReferenceRootFinalSize=({battleLayoutReferenceRoot.rect.width:F1}, {battleLayoutReferenceRoot.rect.height:F1}) "
                    + $"BattleLayoutV2BackgroundFinalSize=({(battleLayoutV2BackgroundImage != null ? battleLayoutV2BackgroundImage.rectTransform.rect.width : 0f):F1}, {(battleLayoutV2BackgroundImage != null ? battleLayoutV2BackgroundImage.rectTransform.rect.height : 0f):F1}) "
                    + $"sourceWidth={sourceWidth} sourceHeight={sourceHeight} sourceAspect={sourceAspect:F4} targetAspect={0.5625:F4} "
                    + $"PreserveAspect={battleLayoutV2BackgroundImage != null && battleLayoutV2BackgroundImage.preserveAspect} AspectRatioFitter=disabled");
                battleLayoutDiagnosticsLogged = true;
            }
#endif

            float contentWidth = canvasWidth;
            float contentHeight = canvasHeight;

            heroAnchorRect.sizeDelta = new Vector2(contentWidth * .43f, contentHeight * .40f);
            bossAnchorRect.sizeDelta = new Vector2(contentWidth * .43f, contentHeight * .40f);
            turnInfoAnchorRect.sizeDelta = new Vector2(contentWidth, contentHeight * .055f);

            float side = Mathf.Min(contentWidth * .86f, contentHeight * .35f);
            if (side <= 0f) return;

            boardAreaRect.sizeDelta = new Vector2(contentWidth, side + 20f);
            boardFrameRect.sizeDelta = Vector2.one * (side + 20f);
            bottomAreaRect.sizeDelta = new Vector2(contentWidth, contentHeight * .17f);
            Rect referenceRect = battleLayoutReferenceRoot.rect;
            gemViewportRect.sizeDelta = new Vector2(
                referenceRect.width * BattleLayoutGridRegion.width,
                referenceRect.height * BattleLayoutGridRegion.height);
            Vector2 gridCenterInReference = new Vector2(
                (BattleLayoutGridRegion.center.x - .5f) * referenceRect.width,
                (BattleLayoutGridRegion.center.y - .5f) * referenceRect.height);
            gemViewportRect.anchoredPosition = gridCenterInReference
                - (Vector2)boardAreaRect.localPosition;
            gemViewportRect.localScale = Vector3.one;
            selectionOverlayRect.sizeDelta = gemViewportRect.sizeDelta;
            selectionOverlayRect.anchoredPosition = gemViewportRect.anchoredPosition;
            selectionOverlayRect.localScale = Vector3.one;

            boardGrid.constraintCount = RuntimeBoardSize;
            cellBackgroundGrid.constraintCount = RuntimeBoardSize;
            boardGrid.spacing = Vector2.zero;
            cellBackgroundGrid.spacing = Vector2.zero;
            boardGrid.padding = new RectOffset();
            cellBackgroundGrid.padding = new RectOffset();
            Canvas.ForceUpdateCanvases();
            RectTransform gridRect = boardRootRect;
            float cellWidth = (gridRect.rect.width
                - boardGrid.spacing.x * (RuntimeBoardSize - 1)) / RuntimeBoardSize;
            float cellHeight = (gridRect.rect.height
                - boardGrid.spacing.y * (RuntimeBoardSize - 1)) / RuntimeBoardSize;
            float cellSize = Mathf.Min(cellWidth, cellHeight);
            boardGrid.cellSize = new Vector2(cellWidth, cellHeight);
            cellBackgroundGrid.cellSize = boardGrid.cellSize;
            boardRootRect.localScale = Vector3.one;
            cellBackgroundGridRect.localScale = Vector3.one;
            if (boardRageFireVfx != null && boardFrameRect != null)
                boardRageFireVfx.Configure(boardFrameRect, cellSize);
            LayoutRebuilder.ForceRebuildLayoutImmediate(boardRootRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(cellBackgroundGridRect);
            Canvas.ForceUpdateCanvases();
            if (heroCharacterView != null) heroCharacterView.CaptureHomePosition();
            if (bossCharacterView != null) bossCharacterView.CaptureHomePosition();
        }

        BoardCellView[,] BuildStableCellBackgrounds(RectTransform parent)
        {
            BoardCellView[,] cells = new BoardCellView[RuntimeBoardSize, RuntimeBoardSize];
            for (int row = 0; row < RuntimeBoardSize; row++)
            for (int column = 0; column < RuntimeBoardSize; column++)
            {
                GameObject cell = new GameObject($"CellBackground_{row}_{column}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BoardCellView));
                cell.transform.SetParent(parent, false);
                cell.transform.localScale = Vector3.one;
                Image background = cell.GetComponent<Image>();
                background.color = Color.clear;
                background.raycastTarget = false;
                BoardCellView cellView = cell.GetComponent<BoardCellView>();
                cellView.Initialize(new Match3RPG.Core.BoardPos(row, column));
                cells[row, column] = cellView;
            }
            return cells;
        }

        static void ConfigureGridLayout(GridLayoutGroup layout)
        {
            layout.transform.localScale = Vector3.one;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = RuntimeBoardSize;
            layout.spacing = Vector2.zero;
            layout.padding = new RectOffset();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        }

        void ValidateGemViewportHierarchy(RectTransform boardRoot)
        {
            if (gemViewportRect.GetComponent<RectMask2D>() == null)
                Debug.LogError("BoardViewport requires a RectMask2D to clip falling Gems.", gemViewportRect);
            if (selectionOverlayRect.parent != boardAreaRect
                || selectionOverlayRect.GetSiblingIndex() != boardAreaRect.childCount - 1)
                Debug.LogError("SelectionOverlayLayer must be the last sibling under BoardAnchor.", selectionOverlayRect);
            if (selectionOverlayRect.GetComponentInParent<RectMask2D>() != null)
                Debug.LogError("SelectionOverlayLayer must stay outside BoardViewport's RectMask2D.", selectionOverlayRect);
            if (boardRoot.parent == null || boardRoot.parent.name != "GemAnimationLayer"
                || boardRoot.parent.parent != gemViewportRect)
                Debug.LogError("CellGrid and GemAnimationLayer must remain inside BoardViewport.", boardRoot);
            if (boardFrameRect.parent != boardAreaRect)
                Debug.LogError("BoardFrameAnchor must remain inside BoardAnchor.", boardFrameRect);
            if (boardRageFireVfxRect == null
                || boardRageFireVfxRect.parent != boardAreaRect
                || !boardRageFireVfxRect.gameObject.activeInHierarchy
                || boardRageFireVfxRect.GetSiblingIndex() <= boardFrameRect.GetSiblingIndex()
                || boardRageFireVfxRect.GetSiblingIndex() >= gemViewportRect.GetSiblingIndex())
                Debug.LogError("Board Rage fire must render between the board frame and BoardViewport.", boardRageFireVfxRect);
        }

        static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        CharacterView CreateCharacter(Transform parent, string objectName, string initial, Color fallbackColor,
            Vector2 min, Vector2 max, Sprite characterSprite, CharacterFacing facingDirection,
            CharacterFacing spriteFacingDirection, float visualScale = 1f, Vector2 visualOffset = default,
            float attackDistance = 72f, CharacterAnimationSet characterAnimationSet = null,
            float idleNormalizationMultiplier = 1f,
            float scaleTransitionDuration = HeroPresentationConfig.DefaultVisualScaleTransitionDuration,
            float specAttackNormalizationMultiplier = 1f, float skillNormalizationMultiplier = 1f,
            Vector2? vfxAnchorPosition = null, Vector2? headVfxAnchorPosition = null,
            Vector2? floatingAnchorPosition = null, float idleFeetPaddingFraction = 0f)
        {
            RectTransform characterRoot = CreateRect(objectName, parent, min, max, Vector2.zero, Vector2.zero);
            RectTransform visualRect = CreateRect("CharacterVisual", characterRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            visualRect.pivot = new Vector2(.5f, 0f);
            float configuredScale = visualScale;
            Vector2 configuredOffset = visualOffset;
            visualRect.localScale = Vector3.one * Mathf.Max(.1f, configuredScale);
            visualRect.anchoredPosition = configuredOffset;
            Image portrait = AddImage(visualRect.gameObject, characterSprite != null ? Color.white : fallbackColor);
            portrait.sprite = characterSprite != null ? characterSprite : CreateCircleSprite();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            HeroAnimationDriver animationDriver = null;
            if (characterAnimationSet != null && characterAnimationSet.IsReady)
            {
                Animator animator = visualRect.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = characterAnimationSet.AnimatorController;
                animationDriver = visualRect.gameObject.AddComponent<HeroAnimationDriver>();
                animationDriver.Configure(animator, characterAnimationSet);
                if (!animationDriver.IsReady)
                    animationDriver = null;
            }
            CharacterView view = characterRoot.gameObject.AddComponent<CharacterView>();
            view.Configure(portrait, visualRect, attackDistance,
                facingDirection, spriteFacingDirection, animationDriver,
                idleNormalizationMultiplier, scaleTransitionDuration,
                specAttackNormalizationMultiplier, skillNormalizationMultiplier,
                idleFeetPaddingFraction);
            RectTransform floatingTextAnchor = CreateRect("FloatingTextAnchor", characterRoot,
                floatingAnchorPosition ?? new Vector2(.5f, .88f),
                floatingAnchorPosition ?? new Vector2(.5f, .88f), Vector2.zero, Vector2.zero);
            view.SetFloatingTextAnchor(floatingTextAnchor);
            Text placeholder = CreateText(visualRect, "Initial", initial, new Vector2(.1f, .1f), new Vector2(.9f, .9f), 72, TextAnchor.MiddleCenter, Color.white);
            float textScaleX = facingDirection == spriteFacingDirection ? 1f : -1f;
            placeholder.rectTransform.localScale = new Vector3(textScaleX, 1f, 1f);
            placeholder.enabled = characterSprite == null;
            RectTransform vfxAnchor = CreateRect("VFXAnchor", visualRect,
                vfxAnchorPosition ?? new Vector2(.5f, .48f),
                vfxAnchorPosition ?? new Vector2(.5f, .48f), Vector2.zero, Vector2.zero);
            RectTransform persistentVfxAnchor = CreateRect("PersistentVFXAnchor", characterRoot,
                vfxAnchorPosition ?? new Vector2(.5f, .48f),
                vfxAnchorPosition ?? new Vector2(.5f, .48f), Vector2.zero, Vector2.zero);
            RectTransform headVfxAnchor = CreateRect("HeadVFXAnchor", characterRoot,
                headVfxAnchorPosition ?? new Vector2(.5f, .82f),
                headVfxAnchorPosition ?? new Vector2(.5f, .82f), Vector2.zero, Vector2.zero);
            vfxAnchor.SetAsLastSibling();
            persistentVfxAnchor.SetAsLastSibling();
            headVfxAnchor.SetAsLastSibling();
            view.SetCombatVFXAnchors(vfxAnchor, persistentVfxAnchor, headVfxAnchor);
            if (characterSprite == null)
                Debug.LogWarning($"Character sprite missing for {objectName}; using the existing placeholder.", this);
            return view;
        }

        static float GetIdleFeetPaddingFraction(CharacterAnimationSet animationSet)
        {
            if (animationSet == null)
                return 0f;
            switch (animationSet.CharacterId)
            {
                case "Hero2": return 16f / 256f;
                case "Boss01": return 53f / 256f;
                case "Boss02": return 10f / 324f;
                default: return 0f;
            }
        }

        Slider CreateBar(Transform parent, string name, Vector2 min, Vector2 max, Color fillColor,
            out TMP_Text valueText, int fontSize = 19, float fillInset = 3f)
        {
            return CreateBar(parent, name, min, max, fillColor, null, out valueText,
                fontSize, fillInset);
        }

        Slider CreateBar(Transform parent, string name, Vector2 min, Vector2 max, Color fillColor,
            Sprite fillSprite, out TMP_Text valueText, int fontSize = 19, float fillInset = 3f)
        {
            RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            Image background = AddImage(rect.gameObject, Color.clear);
            background.raycastTarget = false;
            Slider slider = rect.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.targetGraphic = background;
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;
            RectTransform fillArea = CreateRect("FillArea", rect, Vector2.zero, Vector2.one,
                Vector2.one * fillInset, Vector2.one * -fillInset);
            RectTransform fill = CreateRect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImage = AddImage(fill.gameObject, Color.white);
            if (fillSprite != null)
                ApplyRoundedBarSprite(fillImage, fillSprite, Color.white, true);
            else
            {
                fillImage.sprite = GetBarFillSprite();
                fillImage.type = Image.Type.Sliced;
                fillImage.fillCenter = true;
                fillImage.raycastTarget = false;
            }
            fillImage.color = Color.white;
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            slider.fillRect = fill;
            slider.direction = Slider.Direction.LeftToRight;
            valueText = CreateRuntimeText(rect, "Value", "", Vector2.zero, Vector2.one,
                fontSize, TextAlignmentOptions.Center, Color.white);
            valueText.raycastTarget = false;
            valueText.outlineColor = new Color(.03f, .025f, .02f, .9f);
            valueText.outlineWidth = .18f;
            ApplyRoundedBarTrack(background, fillSprite, new Color32(42, 46, 61, 255));
            RectMask2D fillMask = fillArea.GetComponent<RectMask2D>();
            if (fillMask == null)
                fillMask = fillArea.gameObject.AddComponent<RectMask2D>();
            fillMask.padding = Vector4.zero;
            fillMask.softness = Vector2Int.zero;
            return slider;
        }

        TMP_Text CreateHpSegmentText(Transform parent, string name, bool onLeft)
        {
            float labelSize = hudLayoutValues.HeroHpBarSize.y * 2f;
            float anchorX = onLeft ? 0f : 1f;
            Vector2 anchor = new Vector2(anchorX, .5f);
            RectTransform rect = CreateRect(name, parent, anchor, anchor,
                new Vector2(-labelSize * .5f, -labelSize * .5f+ 19f),
                new Vector2(labelSize * .5f, labelSize * .5f+ 19f));
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontStyle = FontStyles.Bold;
            text.text = "0";
            text.fontSize = 40f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 40f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(61, 43, 26, 255);
            text.outlineColor = new Color(1f, 1f, 1f, .9f);
            text.outlineWidth = .08f;
            text.raycastTarget = false;
            return text;
        }

        Sprite ResolveBarSpriteForSlider(Slider slider)
        {
            if (slider == null)
                return null;
            if (slider == heroHpBar)
                return heroHpBarSprite;
            if (slider == bossHpBar)
                return bossHpBarSprite;
            if (slider == heroSoulHpBar || slider == bossSoulHpBar)
                return soulHpBarSprite;
            if (slider == heroRageBar || slider == bossRageBar)
                return rageHpBarSprite;
            return null;
        }

        static void ApplyRoundedBarTrack(Image trackImage, Sprite fillSprite, Color trackColor)
        {
            if (trackImage == null)
                return;
            trackImage.type = Image.Type.Sliced;
            trackImage.fillCenter = true;
            if (fillSprite != null)
                trackImage.sprite = fillSprite;
            trackImage.color = trackColor;
            trackImage.raycastTarget = false;
        }

        static void ApplyRoundedBarSprite(Image barImage, Sprite barSprite, Color fillColor, bool setFillCenter)
        {
            if (barImage == null)
                return;
            barImage.type = Image.Type.Sliced;
            barImage.fillCenter = setFillCenter;
            barImage.color = Color.white;
            barImage.raycastTarget = false;
            if (barSprite != null)
                barImage.sprite = barSprite;
        }

        Slider CreateBar(Transform parent, string name, Vector2 min, Vector2 max,
            Color fillColor, Sprite fillSprite, out TMP_Text valueText, BattleHUDTextStyle valueTextStyle)
        {
            Slider slider = CreateBar(parent, name, min, max, fillColor, fillSprite,
                out valueText, valueTextStyle.FontSize);
            ApplyTextStyle(valueText, valueTextStyle);
            return slider;
        }

        Sprite GetBarFillSprite()
        {
            if (barFillSprite == null)
            {
                Texture2D whiteTexture = Texture2D.whiteTexture;
                barFillSprite = Sprite.Create(whiteTexture,
                    new Rect(0f, 0f, whiteTexture.width, whiteTexture.height),
                    new Vector2(.5f, .5f), 100f);
                barFillSprite.name = "RuntimeHUDWhiteFill";
            }
            return barFillSprite;
        }

        BattleHUDLayoutValues ResolveHudLayoutValues()
        {
            BattleHUDLayoutValues values;
            if (hudLayoutConfig != null)
                values = hudLayoutConfig.GetValues();
            else
                values = BattleHUDLayoutConfig.DefaultValues;

            BattleHUDLayoutValues configuredValues = values;
            BattleHUDLayoutValues defaults = BattleHUDLayoutConfig.DefaultValues;
            bool invalidSoulChannels = !HasArea(values.HeroSoulHpChannel)
                || !HasArea(values.BossSoulHpChannel);
            bool invalidRageChannels = !HasArea(values.HeroRageChannel)
                || !HasArea(values.BossRageChannel);
            if (invalidSoulChannels)
            {
                values.HeroSoulHpChannel = defaults.HeroSoulHpChannel;
                values.BossSoulHpChannel = defaults.BossSoulHpChannel;
            }
            if (invalidRageChannels)
            {
                values.HeroRageChannel = defaults.HeroRageChannel;
                values.BossRageChannel = defaults.BossRageChannel;
            }
            if ((invalidSoulChannels || invalidRageChannels)
                && !invalidHudChannelsWarningLogged)
            {
                string configName = hudLayoutConfig != null
                    ? hudLayoutConfig.name : "built-in defaults";
                Debug.LogWarning($"Battle HUD layout '{configName}' has invalid Soul/Rage channels; "
                    + $"raw channels: HeroSoul={configuredValues.HeroSoulHpChannel}, "
                    + $"BossSoul={configuredValues.BossSoulHpChannel}, HeroRage={configuredValues.HeroRageChannel}, "
                    + $"BossRage={configuredValues.BossRageChannel}; using built-in channel defaults for those bars.", this);
                invalidHudChannelsWarningLogged = true;
            }

            if (hudLayoutConfig == null && !hudLayoutFallbackWarningLogged)
            {
                Debug.LogWarning("BattleHUDLayoutConfig is not assigned; using the built-in layout defaults.", this);
                hudLayoutFallbackWarningLogged = true;
            }

            return values;
        }

        static bool HasArea(Rect channel) => channel.width > 0f && channel.height > 0f;

        void ApplyHudLayout()
        {
            hudLayoutValues = ResolveHudLayoutValues();
            if (battleLayoutReferenceRoot == null)
                return;

            ApplyNormalizedRect(heroNameText != null ? heroNameText.rectTransform : null,
                hudLayoutValues.HeroNameSlot);
            ApplyNormalizedRect(bossNameText != null ? bossNameText.rectTransform : null,
                hudLayoutValues.BossNameSlot);
            ApplyTextStyle(heroNameText, hudLayoutValues.HeroNameStyle);
            ApplyTextStyle(bossNameText, hudLayoutValues.BossNameStyle);

            ConfigurePaintedBar(heroHpBar, hudLayoutValues.HeroHpChannel, false,
                hudLayoutValues.HpValueStyle, hudLayoutValues.HpFillInset,
                hudLayoutValues.HeroHpBarPosition, hudLayoutValues.HeroHpBarSize);
            ConfigurePaintedBar(bossHpBar, hudLayoutValues.BossHpChannel, true,
                hudLayoutValues.HpValueStyle, hudLayoutValues.HpFillInset,
                hudLayoutValues.BossHpBarPosition, hudLayoutValues.HeroHpBarSize);
            ConfigurePaintedBar(heroSoulHpBar, hudLayoutValues.HeroSoulHpChannel, false,
                hudLayoutValues.RageValueStyle, hudLayoutValues.RageFillInset, showTrack: true);
            ConfigurePaintedBar(bossSoulHpBar, hudLayoutValues.BossSoulHpChannel, true,
                hudLayoutValues.RageValueStyle, hudLayoutValues.RageFillInset, showTrack: true);
            ConfigurePaintedBar(heroRageBar, hudLayoutValues.HeroRageChannel, false,
                hudLayoutValues.RageValueStyle, hudLayoutValues.RageFillInset, showTrack: true);
            ConfigurePaintedBar(bossRageBar, hudLayoutValues.BossRageChannel, true,
                hudLayoutValues.RageValueStyle, hudLayoutValues.RageFillInset, showTrack: true);

            if (turnInfoAnchorRect != null)
            {
                turnInfoAnchorRect.anchorMin = hudLayoutValues.TurnInfoAnchorPosition;
                turnInfoAnchorRect.anchorMax = hudLayoutValues.TurnInfoAnchorPosition;
                turnInfoAnchorRect.anchoredPosition = Vector2.zero;
            }
            ApplyTurnTextRect(turnCountText, hudLayoutValues.TurnCountPosition,
                hudLayoutValues.TurnCountSize, hudLayoutValues.TurnCountStyle);
            ApplyTurnTextRect(timerText, hudLayoutValues.TimerPosition,
                hudLayoutValues.TimerSize, hudLayoutValues.TimerStyle);
            ApplyTextStyle(heroHpValueText, hudLayoutValues.HpValueStyle);
            ApplyTextStyle(bossHpValueText, hudLayoutValues.HpValueStyle);
            ApplyTextStyle(heroRageValueText, hudLayoutValues.RageValueStyle);
            ApplyTextStyle(bossRageValueText, hudLayoutValues.RageValueStyle);
            turnIndicator?.ConfigureTimerColors(hudLayoutValues.TimerStyle.Color,
                hudLayoutValues.TimerWarningColor, hudLayoutValues.TimerDangerColor);

            if (menuHitAreaRect != null)
            {
                menuHitAreaRect.anchorMin = new Vector2(.5f, .5f);
                menuHitAreaRect.anchorMax = new Vector2(.5f, .5f);
                menuHitAreaRect.anchoredPosition = hudLayoutValues.MenuHitAreaPosition;
                menuHitAreaRect.sizeDelta = hudLayoutValues.MenuHitAreaSize;
            }
        }

        static void ApplyNormalizedRect(RectTransform rectTransform, Rect normalizedRect)
        {
            if (rectTransform == null)
                return;
            rectTransform.anchorMin = normalizedRect.min;
            rectTransform.anchorMax = normalizedRect.max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        static void ApplyTurnTextRect(TMP_Text text, Vector2 position, Vector2 size,
            BattleHUDTextStyle style)
        {
            if (text == null)
                return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.offsetMin = position - size * .5f;
            rect.offsetMax = position + size * .5f;
            ApplyTextStyle(text, style);
        }

        void ConfigurePaintedBar(Slider slider, Rect normalizedChannel, bool drainRight,
            BattleHUDTextStyle valueTextStyle, float fillInset, Vector2? fixedPosition = null,
            Vector2? fixedSize = null, bool showTrack = false)
        {
            if (slider == null || battleLayoutReferenceRoot == null)
                return;

            RectTransform barRect = slider.transform as RectTransform;
            if (barRect.parent != battleLayoutReferenceRoot)
                barRect.SetParent(battleLayoutReferenceRoot, false);
            barRect.pivot = new Vector2(.5f, .5f);
            if (fixedPosition.HasValue && fixedSize.HasValue)
            {
                barRect.anchorMin = new Vector2(.5f, .5f);
                barRect.anchorMax = new Vector2(.5f, .5f);
                barRect.anchoredPosition = fixedPosition.Value;
                barRect.sizeDelta = fixedSize.Value;
            }
            else
            {
                barRect.anchorMin = new Vector2(normalizedChannel.xMin, normalizedChannel.yMin);
                barRect.anchorMax = new Vector2(normalizedChannel.xMax, normalizedChannel.yMax);
                barRect.anchoredPosition = Vector2.zero;
                barRect.sizeDelta = Vector2.zero;
            }
            barRect.localScale = Vector3.one;
            barRect.localRotation = Quaternion.identity;

            Image fillImage = slider.fillRect != null
                ? slider.fillRect.GetComponent<Image>()
                : null;
            Image trackImage = slider.GetComponent<Image>();
            foreach (Image image in slider.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = false;
                if (image == fillImage)
                {
                    image.enabled = true;
                    continue;
                }
                if (image == trackImage)
                {
                    image.enabled = true;
                    image.color = new Color32(42, 46, 61, 255);
                    continue;
                }
                image.enabled = false;
            }

            slider.targetGraphic = null;
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.direction = drainRight
                ? Slider.Direction.RightToLeft
                : Slider.Direction.LeftToRight;
            if (fillImage != null)
            {
                fillImage.enabled = true;
                Sprite fillSprite = ResolveBarSpriteForSlider(slider);
                ApplyRoundedBarSprite(fillImage, fillSprite != null ? fillSprite : GetBarFillSprite(),
                    Color.white, true);
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = drainRight
                    ? (int)Image.OriginHorizontal.Right
                    : (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = 1f;
                fillImage.raycastTarget = false;
                Color fillColor = fillImage.color;
                if (fillColor.a <= 0f)
                {
                    fillColor.a = 1f;
                    fillImage.color = fillColor;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (tintHpFillForAlignment)
                    fillImage.color = new Color(1f, .2f, .8f, .55f);
#endif
            }

            RectTransform fillArea = slider.fillRect != null
                ? slider.fillRect.parent as RectTransform
                : null;
            if (fillArea != null)
            {
                fillArea.anchorMin = Vector2.zero;
                fillArea.anchorMax = Vector2.one;
                fillArea.offsetMin = Vector2.one * fillInset;
                fillArea.offsetMax = Vector2.one * -fillInset;
                RectMask2D fillMask = fillArea.GetComponent<RectMask2D>();
                if (fillMask == null)
                    fillMask = fillArea.gameObject.AddComponent<RectMask2D>();
                fillMask.padding = Vector4.zero;
                fillMask.softness = Vector2Int.zero;
            }

            TMP_Text valueText = slider.GetComponentInChildren<TMP_Text>(true);
            if (valueText != null)
            {
                RectTransform textRect = valueText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
                textRect.localScale = Vector3.one;
                ApplyTextStyle(valueText, valueTextStyle);
            }

            LogPaintedBarAudit(slider, fillImage, valueText);
        }

        void LogPaintedBarAudit(Slider slider, Image fillImage, TMP_Text valueText)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RectTransform rect = slider.transform as RectTransform;
            RectTransform fillRect = slider.fillRect as RectTransform;
            string path = GetObjectPath(slider.transform);
            Image trackImage = slider.GetComponent<Image>();
            string fillInfo = fillImage != null
                ? $"ImageType={fillImage.type} FillMethod={fillImage.fillMethod} "
                    + $"FillOrigin={fillImage.fillOrigin} Raycast={fillImage.raycastTarget}"
                : "FillImage=(missing)";
            string trackInfo = trackImage != null
                ? $"TrackImageEnabled={trackImage.enabled} TrackColor={trackImage.color}"
                : "TrackImage=(missing)";
            string textInfo = valueText != null ? $"Text={GetObjectPath(valueText.transform)}" : "Text=(missing)";
            Debug.Log($"Painted HUD bar audit: Path={path} Parent={rect.parent.name} "
                + $"Anchors={rect.anchorMin}->{rect.anchorMax} Position={rect.anchoredPosition} "
                + $"Size={rect.rect.size} {trackInfo} "
                + $"FillRect={(fillRect != null ? fillRect.rect.size.ToString() : "(missing)")} "
                + $"{fillInfo} {textInfo}.", slider);
#endif
        }

        Sprite[] ResolveGemSprites()
        {
            GemType[] gemTypes =
            {
                GemType.Attack, GemType.Mana, GemType.ExpDrain, GemType.Rage,
                GemType.Health, GemType.Shield, GemType.Soul, GemType.Rainbow,
                GemType.Lightning, GemType.Bagua
            };
            Sprite[] sprites = new Sprite[gemTypes.Length];
            for (int index = 0; index < gemTypes.Length; index++)
            {
                GemType type = gemTypes[index];
                int spriteSlot = GemSpriteSlot(type);
                if (gemV2BaseSprites != null && spriteSlot < gemV2BaseSprites.Length)
                    sprites[spriteSlot] = gemV2BaseSprites[spriteSlot];
                if (sprites[spriteSlot] == null)
                {
                    Debug.LogWarning($"Gem V2 sprite is missing for {type}: "
                        + $"{GemVisualDatabase.ExpectedBaseMapping(type)}. "
                        + "Using a runtime fallback for this battle.", this);
                    sprites[spriteSlot] = CreateCircleGemSprite(GemColors[spriteSlot]);
                }
            }
            return sprites;
        }

        static int GemSpriteSlot(GemType type)
        {
            switch (type)
            {
                case GemType.Attack: return 0;
                case GemType.Mana: return 1;
                case GemType.ExpDrain: return 2;
                case GemType.Rage: return 3;
                case GemType.Health: return 4;
                case GemType.Shield: return 5;
                case GemType.Soul: return 6;
                case GemType.Rainbow: return 7;
                case GemType.Lightning: return 8;
                case GemType.Bagua: return 9;
                default: return 0;
            }
        }

        Sprite[] ResolveGemV2SpecialSprites(Sprite[] baseSprites)
        {
            string[] expected =
            {
                "Assets/Art/Gems/V2/Special/Base_01_Sword_special.png",
                "Assets/Art/Gems/V2/Special/Base_02_YinYang_special.png",
                "Assets/Art/Gems/V2/Special/Base_03_Star_special.png",
                "Assets/Art/Gems/V2/Special/Base_04_GoldIngot_special.png",
                "Assets/Art/Gems/V2/Special/Base_05_Heart_special.png",
                "Assets/Art/Gems/V2/Special/Base_06_Fire_special.png",
                "Assets/Art/Gems/V2/Special/Base_07_Diamond_special.png"
            };
            if (gemV2SpecialSprites == null || gemV2SpecialSprites.Length != expected.Length)
            {
                int actualLength = gemV2SpecialSprites != null ? gemV2SpecialSprites.Length : 0;
                Debug.LogWarning($"Gem V2 special sprite array must contain exactly {expected.Length} "
                    + $"sprites, but contains {actualLength}. Missing entries will be reported when used.", this);
            }

            Sprite[] sprites = new Sprite[expected.Length];
            for (int index = 0; index < sprites.Length; index++)
            {
                if (gemV2SpecialSprites != null && index < gemV2SpecialSprites.Length)
                    sprites[index] = gemV2SpecialSprites[index];
                if (sprites[index] == null)
                {
                    Debug.LogWarning($"Gem V2 special sprite is missing: {expected[index]}. "
                        + $"Special Gems of type {(GemType)index} will not fall back to regular art.", this);
                }
            }
            return sprites;
        }

        static void SetOptionalSprite(Image target, Sprite sprite, string assetName, bool retainColorFallback)
        {
            if (sprite == null)
            {
                Debug.LogWarning($"Optional art sprite is not assigned: {assetName}. Using the default UI appearance.");
                if (!retainColorFallback) target.enabled = false;
                return;
            }
            target.sprite = sprite;
            target.color = Color.white;
            target.type = Image.Type.Simple;
        }

        static Image CreateDecorativeImage(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max,
            Color fallbackColor, bool preserveAspect = false)
        {
            RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            Image image = AddImage(rect.gameObject, fallbackColor);
            image.raycastTarget = false;
            image.preserveAspect = false;
            if (sprite == null)
            {
                image.enabled = false;
                Debug.LogWarning($"Optional art sprite is not assigned: {name}. Decorative layer will be skipped.");
            }
            else
            {
                image.sprite = sprite;
                image.color = Color.white;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }
            return image;
        }

        void CreatePlaceholderButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            Image image = AddImage(rect.gameObject, new Color32(34, 44, 43, 255));
            SetOptionalSprite(image, buttonNormalSprite, "Button_Normal", true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState spriteState = button.spriteState;
            spriteState.pressedSprite = buttonPressedSprite;
            button.spriteState = spriteState;
            if (buttonPressedSprite == null)
                Debug.LogWarning("Optional art sprite is not assigned: Button_Pressed. Button uses its normal sprite.");
            button.interactable = false;
            Text text = CreateText(rect, "Label", label, Vector2.zero, Vector2.one, 18, TextAnchor.MiddleCenter, Color.white);
            text.raycastTarget = false;
        }

        static Text CreateText(Transform parent, string name, string content, Vector2 min, Vector2 max, int size, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        TMP_Text CreateRuntimeText(Transform parent, string name, string content, Vector2 min,
            Vector2 max, int size, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            if (text.font == null)
                Debug.LogError("Battle runtime text requires the TextMeshPro default font asset.", parent);
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10, size - 6);
            text.fontSizeMax = size;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        static void ApplyTextStyle(TMP_Text text, BattleHUDTextStyle style)
        {
            if (text == null)
                return;
            text.fontSize = style.FontSize;
            text.enableAutoSizing = style.AutoSize;
            text.fontSizeMin = style.AutoSizeMin;
            text.fontSizeMax = style.AutoSizeMax;
            text.color = style.Color;
            text.outlineColor = style.OutlineColor;
            text.outlineWidth = style.OutlineWidth;
            text.alignment = style.Alignment;
            text.raycastTarget = false;
        }

        TMP_Text CreateTurnText(Transform parent, string name, Vector2 anchoredPosition,
            Vector2 size, BattleHUDTextStyle style)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), anchoredPosition - size * .5f,
                anchoredPosition + size * .5f);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            if (text.font == null)
                Debug.LogError("Turn information requires the TextMeshPro default font asset.", parent);
            text.fontSize = style.FontSize;
            ApplyTextStyle(text, style);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        RectTransform CreateMenuHitArea(Transform parent, BattleStatusPopup statusPopup)
        {
            RectTransform rect = CreateRect("MenuHitArea", parent,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                hudLayoutValues.MenuHitAreaPosition - hudLayoutValues.MenuHitAreaSize * .5f,
                hudLayoutValues.MenuHitAreaPosition + hudLayoutValues.MenuHitAreaSize * .5f);
            Image image = AddImage(rect.gameObject, Color.clear);
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.interactable = statusPopup != null;
            if (statusPopup == null)
            {
                Debug.LogError("The Battle Menu hit area requires BattleStatusPopup.", rect);
                return rect;
            }
            button.onClick.AddListener(statusPopup.Open);
            return rect;
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        static Image AddImage(GameObject target, Color color)
        {
            Image image = target.AddComponent<Image>();
            image.color = color;
            return image;
        }

        GemView CreateGemTemplate(GemVisualDatabase visualDatabase)
        {
            GameObject storage = new GameObject("RuntimePrefabStorage");
            GameObject gem = new GameObject("Gem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            gem.transform.SetParent(storage.transform, false);
            Image hitArea = gem.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = true;
            RectTransform visualRoot = CreateRect("GemVisualRoot", gem.transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = CreateGemLayer(visualRoot, "GemSprite",
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, Vector2.zero, null, true);
            Image horizontal = CreateGemLayer(visualRoot, "HorizontalFallback",
                new Vector2(.18f, .44f), new Vector2(.82f, .56f),
                Vector2.zero, Vector2.zero, null, true, new Color32(245, 235, 185, 255));
            Image vertical = CreateGemLayer(visualRoot, "VerticalFallback",
                new Vector2(.44f, .18f), new Vector2(.56f, .82f),
                Vector2.zero, Vector2.zero, null, true, new Color32(245, 235, 185, 255));
            Button button = gem.GetComponent<Button>();
            button.targetGraphic = hitArea;
            button.transition = Selectable.Transition.ColorTint;
            GemView view = gem.GetComponent<GemView>();
            view.ConfigureArt(null, image, horizontal, vertical, null,
                visualRoot, visualDatabase);
            storage.SetActive(false);
            return view;
        }

        static Image CreateGemLayer(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin,
            Vector2 offsetMax, Sprite sprite, bool decorative, Color? tint = null)
        {
            RectTransform rect = CreateRect(name, parent, min, max, offsetMin, offsetMax);
            Image image = AddImage(rect.gameObject, tint ?? Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            image.fillAmount = 1f;
            image.raycastTarget = !decorative;
            if (sprite == null && name != "GemSprite"
                && name != "HorizontalFallback" && name != "VerticalFallback")
            {
                image.enabled = false;
                Debug.LogWarning($"Optional art sprite is not assigned: {name}. Overlay will be disabled.");
            }
            return image;
        }

        static void CreateEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule input = eventSystem.AddComponent<InputSystemUIInputModule>();
            input.AssignDefaultActions();
        }

        static Sprite CreateDiamondSprite(Color color)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int distance = Mathf.Abs(x - size / 2) + Mathf.Abs(y - size / 2);
                if (distance <= 29) pixels[y * size + x] = distance > 25 ? color * .62f : color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * .5f, size * .5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= 29) pixels[y * size + x] = distance > 25 ? new Color(.12f, .16f, .16f, 1) : Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        static Sprite CreateCircleGemSprite(Color color)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * .5f, size * .5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= 29f)
                    pixels[y * size + x] = distance > 25f
                        ? new Color32(190, 78, 27, 255)
                        : color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }
    }
}