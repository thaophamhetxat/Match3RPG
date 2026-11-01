using Match3RPG.Battle;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Campaign.Data
{
    [CreateAssetMenu(menuName = "Match3 RPG/Campaign/Hero Data")]
    public sealed class HeroData : ScriptableObject
    {
        [SerializeField] string heroId;
        [SerializeField] string displayName;
        [SerializeField] Sprite portrait;
        [SerializeField] Sprite selectionPreviewSprite;
        [SerializeField] Sprite staticFallbackSprite;
        [SerializeField] CharacterAnimationSet animationSet;
        [SerializeField, Min(1)] int baseMaxHP = 500;
        [SerializeField, Min(0)] int baseAttack = 20;
        [SerializeField, Min(1)] int baseMana = 100;
        [SerializeField, Min(1)] int baseRage = 100;
        [SerializeField, Min(.1f)] float visualScale = 1f;
        [SerializeField] Vector2 visualOffset;
        [SerializeField] CharacterFacing sourceFacing = CharacterFacing.Right;
        [SerializeField, Min(.1f)] float idleNormalizationScaleMultiplier = 1f;
        [SerializeField] Vector2 vfxAnchor = new Vector2(.5f, .48f);
        [SerializeField] Vector2 headVfxAnchor = new Vector2(.5f, .82f);
        [SerializeField] Vector2 floatingTextAnchor = new Vector2(.5f, .88f);

        public string HeroId => heroId;
        public string StableId => heroId;
        public bool HasValidStableId => StableIdRules.IsValid(heroId);
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public Sprite SelectionPreviewSprite => selectionPreviewSprite;
        public Sprite StaticFallbackSprite => staticFallbackSprite;
        public CharacterAnimationSet AnimationSet => animationSet;
        public int BaseMaxHP => baseMaxHP;
        public int BaseAttack => baseAttack;
        public int BaseMana => baseMana;
        public int BaseRage => baseRage;
        public float VisualScale => visualScale;
        public Vector2 VisualOffset => visualOffset;
        public CharacterFacing SourceFacing => sourceFacing;
        public float IdleNormalizationScaleMultiplier => idleNormalizationScaleMultiplier;
        public Vector2 VfxAnchor => vfxAnchor;
        public Vector2 HeadVfxAnchor => headVfxAnchor;
        public Vector2 FloatingTextAnchor => floatingTextAnchor;

        public void Configure(string id, string title, Sprite heroPortrait,
            Sprite preview, Sprite fallback, CharacterAnimationSet characterAnimationSet,
            int maximumHealth, int attack, int mana, int rage, float displayScale,
            Vector2 offset, CharacterFacing facing, float idleNormalization = 1f,
            Vector2? vfxAnchorPosition = null, Vector2? headVfxAnchorPosition = null,
            Vector2? floatingAnchorPosition = null)
        {
            heroId = id;
            displayName = title;
            portrait = heroPortrait;
            selectionPreviewSprite = preview;
            staticFallbackSprite = fallback;
            animationSet = characterAnimationSet;
            baseMaxHP = Mathf.Max(1, maximumHealth);
            baseAttack = Mathf.Max(0, attack);
            baseMana = Mathf.Max(1, mana);
            baseRage = Mathf.Max(1, rage);
            visualScale = Mathf.Max(.1f, displayScale);
            visualOffset = offset;
            sourceFacing = facing;
            idleNormalizationScaleMultiplier = Mathf.Max(.1f, idleNormalization);
            vfxAnchor = vfxAnchorPosition ?? new Vector2(.5f, .48f);
            headVfxAnchor = headVfxAnchorPosition ?? new Vector2(.5f, .82f);
            floatingTextAnchor = floatingAnchorPosition ?? new Vector2(.5f, .88f);
        }
    }
}
