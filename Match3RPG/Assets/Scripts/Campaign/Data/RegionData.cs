using System.Collections.Generic;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Campaign.Data
{
    [CreateAssetMenu(menuName = "Match3 RPG/Campaign/Region Data")]
    public sealed class RegionData : ScriptableObject
    {
        [SerializeField] string regionId;
        [SerializeField] string displayName;
        [SerializeField] Sprite mapSprite;
        [SerializeField] Sprite headerSprite;
        [SerializeField] Sprite footerSprite;
        [SerializeField] GameObject mapLayoutPrefab;
        [SerializeField] Vector2 worldMapNodePosition;
        [SerializeField] StageData[] stages = new StageData[10];
        [SerializeField] string nextRegionId;
        [SerializeField] Color themeColor = Color.white;
        [SerializeField] bool isFinalRegion;

        public string RegionId => regionId;
        public string StableId => regionId;
        public bool HasValidStableId => StableIdRules.IsValid(regionId);
        public string DisplayName => displayName;
        public Sprite MapSprite => mapSprite;
        public Sprite HeaderSprite => headerSprite;
        public Sprite FooterSprite => footerSprite;
        public GameObject MapLayoutPrefab => mapLayoutPrefab;
        public Vector2 WorldMapNodePosition => worldMapNodePosition;
        public IReadOnlyList<StageData> Stages => stages;
        public string NextRegionId => nextRegionId;
        public Color ThemeColor => themeColor;
        public bool IsFinalRegion => isFinalRegion;

        public StageData GetStage(int stageNumber)
        {
            if (stages == null || stageNumber < 1 || stageNumber > stages.Length)
                return null;
            return stages[stageNumber - 1];
        }

        public void Configure(string id, string title, Sprite background, Vector2 worldNode,
            StageData[] regionStages, string nextId, Color theme, bool finalRegion,
            Sprite header = null, Sprite footer = null, GameObject layoutPrefab = null)
        {
            regionId = id;
            displayName = title;
            mapSprite = background;
            headerSprite = header;
            footerSprite = footer;
            mapLayoutPrefab = layoutPrefab;
            worldMapNodePosition = worldNode;
            stages = regionStages;
            nextRegionId = nextId;
            themeColor = theme;
            isFinalRegion = finalRegion;
        }
    }
}
