using UnityEngine;

namespace Match3RPG.Campaign
{
    [CreateAssetMenu(menuName = "Match3 RPG/Configuration/Region Map Layout")]
    public sealed class RegionMapLayoutConfig : ScriptableObject
    {
        [Header("Artwork")]
        [SerializeField, Min(0f), Tooltip("Multiplier for the aspect-ratio-derived header height.")]
        float headerHeightScale = 1f;
        [SerializeField, Min(0f), Tooltip("Multiplier for the aspect-ratio-derived footer height.")]
        float footerHeightScale = 1f;
        [SerializeField, Tooltip("Footer back-button hit area normalized to the footer artwork (x, y, width, height).")]
        Rect backHitRect = new Rect(.06f, 0f, .27f, 1f);

        public float HeaderHeightScale => headerHeightScale;
        public float FooterHeightScale => footerHeightScale;
        public Rect BackHitRect => backHitRect;
    }
}
