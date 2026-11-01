using System;
using System.Collections.Generic;
using Match3RPG.Campaign.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Match3RPG.Campaign
{
    public sealed class WorldMapController : MonoBehaviour
    {
        [SerializeField] CampaignCatalog catalog;
        [SerializeField, Min(90f)] float regionTouchTarget = 96f;

        CampaignManager campaign;
        RectTransform canvasRect;
        RectTransform viewportRect;
        RectTransform mapRect;
        ScrollRect scrollRect;
        Vector3 mapBaseScale = Vector3.one;
        float mapFitScale = 1f;
        readonly Dictionary<string, WorldMapRegionMarkerView> regionMarkersById =
            new Dictionary<string, WorldMapRegionMarkerView>(StringComparer.Ordinal);

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
                Debug.LogError("WorldMap requires Resources/Campaign/CampaignCatalog.", this);
                return;
            }

            campaign = CampaignManager.GetOrCreate(catalog);
            BuildUI();
        }

        void BuildUI()
        {
            Canvas canvas = CampaignUIFactory.CreateCanvas("WorldMapCanvas");
            canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                Debug.LogError("WorldMap Canvas requires a RectTransform.", canvas);
                return;
            }

            viewportRect = CampaignUIFactory.CreateRect("WorldMapViewport", canvasRect,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewportRect.SetAsFirstSibling();
            Image viewportRaycastSurface = viewportRect.gameObject.AddComponent<Image>();
            viewportRaycastSurface.color = Color.clear;
            viewportRaycastSurface.raycastTarget = true;
            RectMask2D mask = viewportRect.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            mask.softness = Vector2Int.zero;

            scrollRect = viewportRect.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.inertia = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.viewport = viewportRect;

            RectTransform markerLayer;
            if (catalog.WorldMapMapLayoutPrefab != null)
            {
                GameObject layout = Instantiate(catalog.WorldMapMapLayoutPrefab, viewportRect, false);
                mapRect = layout.GetComponent<RectTransform>();
                if (mapRect == null)
                {
                    Debug.LogError("WorldMap layout prefab root requires a RectTransform.",
                        catalog.WorldMapMapLayoutPrefab);
                    return;
                }
                Canvas previewCanvas = layout.GetComponent<Canvas>();
                if (previewCanvas != null)
                {
                    previewCanvas.enabled = false;
                    Destroy(previewCanvas);
                }
                if (mapRect.Find("WorldMapBackground") == null)
                {
                    Debug.LogError("WorldMap layout prefab is missing WorldMapBackground.",
                        catalog.WorldMapMapLayoutPrefab);
                    return;
                }
                markerLayer = mapRect.Find("RegionMarkersLayer") as RectTransform;
                if (markerLayer == null)
                {
                    Debug.LogError("WorldMap layout prefab is missing RegionMarkersLayer.",
                        catalog.WorldMapMapLayoutPrefab);
                    return;
                }
                mapBaseScale = mapRect.localScale;
                BindWorldMapNodes();
            }
            else
            {
                mapRect = CampaignUIFactory.CreateRect("MapContent", viewportRect,
                    new Vector2(.5f, 1f), new Vector2(.5f, 1f), Vector2.zero, Vector2.zero);
                mapRect.pivot = new Vector2(.5f, 1f);
                RectTransform backgroundRect = CampaignUIFactory.CreateRect("WorldMapBackground", mapRect,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image mapImage = backgroundRect.gameObject.AddComponent<Image>();
                mapImage.raycastTarget = false;
                mapImage.preserveAspect = false;
                mapImage.sprite = catalog.WorldMapSprite;
                if (mapImage.sprite == null)
                    Debug.LogError("Campaign catalog has no World Map sprite assigned.", catalog);

                markerLayer = CampaignUIFactory.CreateRect("RegionMarkersLayer", mapRect,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                CreateWorldMapNodes(markerLayer);
            }

            scrollRect.content = mapRect;
            FitMapToCanvas();
            CreateWorldMapOverlays();
            Canvas.ForceUpdateCanvases();
            FocusCurrentRegion();
        }

        void BindWorldMapNodes()
        {
            regionMarkersById.Clear();
            WorldMapRegionMarkerView[] markerViews = mapRect.GetComponentsInChildren<WorldMapRegionMarkerView>(true);
            foreach (WorldMapRegionMarkerView markerView in markerViews)
            {
                if (string.IsNullOrEmpty(markerView.RegionId))
                {
                    Debug.LogError("WorldMap layout contains a marker with no RegionId.", markerView);
                    continue;
                }
                if (!regionMarkersById.TryAdd(markerView.RegionId, markerView))
                    Debug.LogError($"WorldMap layout contains duplicate RegionId '{markerView.RegionId}'.",
                        markerView);
            }

            foreach (RegionData region in catalog.Regions)
            {
                if (region == null)
                    continue;
                if (!regionMarkersById.TryGetValue(region.RegionId, out WorldMapRegionMarkerView markerView))
                {
                    Debug.LogError($"WorldMap layout is missing marker for RegionId '{region.RegionId}'.", catalog);
                    continue;
                }
                markerView.Configure(region, GameProgressService.GetRegionState(catalog, region),
                    IsCurrentRegion(region), null, catalog.CampaignLockedStageMarker,
                    catalog.CampaignAvailableStageMarker, catalog.CampaignCompletedStageMarker,
                    catalog.CampaignStageGlow, OnRegionSelected);
            }
        }

        void CreateWorldMapOverlays()
        {
            CreateWorldMapOverlay("WorldMapHeader", catalog.WorldMapHeaderSprite, true);
            RectTransform footer = CreateWorldMapOverlay("WorldMapFooter",
                catalog.WorldMapFooterSprite, false);
            if (footer != null)
                CreateWorldMapBackHit(footer);
        }

        RectTransform CreateWorldMapOverlay(string objectName, Sprite sprite, bool isHeader)
        {
            if (sprite == null)
                return null;

            float height = sprite.rect.width > 0f
                ? canvasRect.rect.width * sprite.rect.height / sprite.rect.width
                : 0f;
            Vector2 anchor = isHeader ? Vector2.up : Vector2.zero;
            Vector2 offsetMin = isHeader ? new Vector2(0f, -height) : Vector2.zero;
            Vector2 offsetMax = isHeader ? Vector2.zero : new Vector2(0f, height);
            RectTransform overlay = CampaignUIFactory.CreateRect(objectName, canvasRect,
                new Vector2(0f, anchor.y), new Vector2(1f, anchor.y), offsetMin, offsetMax);
            Image image = overlay.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            overlay.SetAsLastSibling();
            return overlay;
        }

        void CreateWorldMapBackHit(RectTransform footer)
        {
            RectTransform hit = CampaignUIFactory.CreateRect("BackHit", footer,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            hit.anchorMin = new Vector2(0.06f, 0.2f);
            hit.anchorMax = new Vector2(0.38f, 0.8f);
            hit.offsetMin = Vector2.zero;
            hit.offsetMax = Vector2.zero;

            Image image = hit.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            Button back = hit.gameObject.AddComponent<Button>();
            back.targetGraphic = image;
            back.transition = Selectable.Transition.None;
            back.onClick.AddListener(ReturnToMainProgress);
            hit.SetAsLastSibling();
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            ReturnToMainProgress();
        }

        void ReturnToMainProgress()
        {
            campaign.LoadMainProgress();
        }

        void CreateWorldMapNodes(RectTransform nodeLayer)
        {
            foreach (RegionData region in catalog.Regions)
            {
                if (region == null)
                    continue;

                CampaignNodeState state = GameProgressService.GetRegionState(catalog, region);
                Vector2 normalizedPosition = region.WorldMapNodePosition;
                Vector2 nodeAnchor = new Vector2(normalizedPosition.x, 1f - normalizedPosition.y);
                RectTransform nodeRect = CampaignUIFactory.CreateRect($"RegionNode_{region.RegionId}",
                    nodeLayer, nodeAnchor, nodeAnchor,
                    Vector2.zero, Vector2.zero);
                nodeRect.sizeDelta = new Vector2(regionTouchTarget, regionTouchTarget);

                RectTransform hitAreaRect = CampaignUIFactory.CreateRect("HitArea", nodeRect,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                hitAreaRect.sizeDelta = new Vector2(regionTouchTarget, regionTouchTarget);
                Image hitArea = hitAreaRect.gameObject.AddComponent<Image>();
                hitArea.color = new Color(1f, 1f, 1f, .001f);
                hitArea.raycastTarget = true;
                Button button = hitAreaRect.gameObject.AddComponent<Button>();
                button.targetGraphic = hitArea;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = new ColorBlock
                {
                    normalColor = hitArea.color,
                    highlightedColor = new Color(1f, 1f, 1f, .04f),
                    pressedColor = new Color(1f, 1f, 1f, .08f),
                    selectedColor = hitArea.color,
                    disabledColor = hitArea.color,
                    colorMultiplier = 1f,
                    fadeDuration = .1f
                };

                CreateMarkerVisuals(nodeRect);
                WorldMapRegionMarkerView markerView = nodeRect.gameObject.AddComponent<WorldMapRegionMarkerView>();
                bool isCurrent = IsCurrentRegion(region);
                markerView.Configure(region, state, isCurrent, button,
                    catalog.CampaignLockedStageMarker, catalog.CampaignAvailableStageMarker,
                    catalog.CampaignCompletedStageMarker, catalog.CampaignStageGlow, OnRegionSelected);
                regionMarkersById[region.RegionId] = markerView;
            }
        }

        void CreateMarkerVisuals(RectTransform nodeRect)
        {
            RectTransform markerRect = CampaignUIFactory.CreateRect("MarkerVisual", nodeRect,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            markerRect.sizeDelta = new Vector2(68f, 68f);
            CampaignUIFactory.CreateCenteredImage(markerRect, "OuterGlow",
                new Vector2(68f, 68f), catalog.CampaignStageGlow, Color.white);
            CampaignUIFactory.CreateCenteredImage(markerRect, "PulseEffect",
                new Vector2(44f, 44f), catalog.CampaignStageGlow, Color.white);
            CampaignUIFactory.CreateCenteredImage(markerRect, "MarkerBody",
                new Vector2(46f, 56f), catalog.CampaignAvailableStageMarker, Color.white);
            CampaignUIFactory.CreateCenteredImage(markerRect, "InnerCore",
                new Vector2(12f, 20f), catalog.CampaignStageGlow, Color.white);
            Image lockImage = CampaignUIFactory.CreateCenteredImage(markerRect, "LockVisual",
                new Vector2(14f, 17f), catalog.CampaignLockedStageMarker, Color.white);
            lockImage.rectTransform.anchoredPosition = new Vector2(0f, -17f);
            markerRect.Find("MarkerBody").SetAsLastSibling();
            markerRect.Find("InnerCore").SetAsLastSibling();
            markerRect.Find("LockVisual").SetAsLastSibling();
        }

        bool IsCurrentRegion(RegionData region)
        {
            RegionData currentRegion = catalog.GetRegion(GameProgressService.LastSelectedRegionId);
            if (currentRegion == null)
                currentRegion = catalog.GetRegion("region_01");
            return region == currentRegion;
        }

        void FitMapToCanvas()
        {
            Canvas.ForceUpdateCanvases();
            float viewportWidth = viewportRect.rect.width;
            float viewportHeight = viewportRect.rect.height;
            Sprite map = catalog.WorldMapSprite;
            if (viewportWidth <= 0f || viewportHeight <= 0f || map == null)
                return;

            float sourceWidth = Mathf.Max(1f, mapRect.rect.width * Mathf.Abs(mapBaseScale.x));
            float sourceHeight = Mathf.Max(1f, mapRect.rect.height * Mathf.Abs(mapBaseScale.y));
            float sourceAspect = sourceWidth / sourceHeight;
            float contentWidth = Mathf.Max(viewportWidth, viewportHeight * sourceAspect);
            mapFitScale = contentWidth / sourceWidth;
            mapRect.localScale = mapBaseScale * mapFitScale;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(mapRect);
        }

        void FocusCurrentRegion()
        {
            if (mapRect == null || viewportRect == null || catalog == null)
                return;

            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();

            RegionData currentRegion = catalog.GetRegion(GameProgressService.LastSelectedRegionId);
            if (currentRegion == null)
                currentRegion = catalog.GetRegion("region_01");
            if (currentRegion == null)
                return;

            if (!regionMarkersById.TryGetValue(currentRegion.RegionId,
                    out WorldMapRegionMarkerView currentMarker))
                return;
            Vector3 markerPosition = mapRect.InverseTransformPoint(currentMarker.transform.position);
            float targetY = -markerPosition.y * Mathf.Abs(mapRect.localScale.y);
            float scaledMapHeight = mapRect.rect.height * Mathf.Abs(mapRect.localScale.y);
            float viewportHeight = viewportRect.rect.height;
            float maxOffset = Mathf.Max(0f, scaledMapHeight - viewportHeight);
            float desiredOffset = Mathf.Clamp(targetY - viewportHeight * 0.5f, 0f, maxOffset);
            scrollRect.verticalNormalizedPosition = maxOffset > 0f
                ? 1f - desiredOffset / maxOffset
                : 1f;
        }

        void OnRegionSelected(RegionData region)
        {
            if (!campaign.SelectRegion(region))
                return;
            UnityEngine.SceneManagement.SceneManager.LoadScene(CampaignManager.RegionMapSceneName);
        }

        [ContextMenu("Reset Campaign Progress")]
        void ResetProgress()
        {
            GameProgressService.Reset(catalog);
            FocusCurrentRegion();
            UnityEngine.SceneManagement.SceneManager.LoadScene(CampaignManager.WorldMapSceneName);
        }
    }
}
