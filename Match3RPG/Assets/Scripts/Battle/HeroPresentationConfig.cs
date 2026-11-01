using UnityEngine;

namespace Match3RPG.Battle
{
    public enum BattleCharacterAnimState
    {
        Idle,
        Attack,
        SpecAttack,
        Skill,
        Hit,
        Dodge,
        Rising,
        Death
    }

    [CreateAssetMenu(menuName = "Match3RPG/Hero Presentation")]
    public sealed class HeroPresentationConfig : ScriptableObject
    {
        public const float IdleBodyHeightPixels = 125f;
        public const float IdleFrameSizePixels = 256f;
        public const float AttackBodyHeightPixels = 157f;
        public const float AttackFrameSizePixels = 512f;
        public const float DefaultVisualScale = 1.38f;
        public const float DefaultIdleNormalizationScaleMultiplier =
            (AttackBodyHeightPixels * IdleFrameSizePixels)
            / (IdleBodyHeightPixels * AttackFrameSizePixels);
        public const float DefaultVisualScaleTransitionDuration = .1f;

        [SerializeField] string heroId = "hero_02";
        [SerializeField] Sprite staticIdleSprite;
        [SerializeField] Sprite[] idleFrames = new Sprite[0];
        [SerializeField] Sprite[] attackFrames = new Sprite[0];
        [SerializeField] AnimationClip idleClip;
        [SerializeField] AnimationClip attackClip;
        [SerializeField] AnimationClip specAttackClip;
        [SerializeField] AnimationClip skillClip;
        [SerializeField] AnimationClip deathClip;
        [SerializeField, Min(0f)] float specAttackBodyHeightPixels;
        [SerializeField, Min(0f)] float specAttackFrameSizePixels;
        [SerializeField, Min(0f)] float skillBodyHeightPixels;
        [SerializeField, Min(0f)] float skillFrameSizePixels;
        [SerializeField] RuntimeAnimatorController animatorController;
        [SerializeField, Min(1f)] float idleFramesPerSecond = 8f;
        [SerializeField, Min(1f)] float attackFramesPerSecond = 12f;
        [SerializeField, Min(.1f)] float visualScale = DefaultVisualScale;
        [SerializeField, Min(.1f)] float idleNormalizationScaleMultiplier =
            DefaultIdleNormalizationScaleMultiplier;
        [SerializeField, Min(0f)] float visualScaleTransitionDuration = DefaultVisualScaleTransitionDuration;
        [SerializeField] Vector2 visualOffset;
        [SerializeField] CharacterFacing sourceFacing = CharacterFacing.Right;

        public string HeroId => heroId;
        public Sprite StaticIdleSprite => staticIdleSprite;
        public Sprite[] IdleFrames => idleFrames;
        public Sprite[] AttackFrames => attackFrames;
        public AnimationClip IdleClip => idleClip;
        public AnimationClip AttackClip => attackClip;
        public AnimationClip SpecAttackClip => specAttackClip;
        public AnimationClip SkillClip => skillClip;
        public AnimationClip DeathClip => deathClip;
        public float SpecAttackBodyHeightPixels => specAttackBodyHeightPixels;
        public float SpecAttackFrameSizePixels => specAttackFrameSizePixels;
        public float SkillBodyHeightPixels => skillBodyHeightPixels;
        public float SkillFrameSizePixels => skillFrameSizePixels;
        public float SpecAttackNormalizationScaleMultiplier =>
            GetBodyNormalizationScaleMultiplier(specAttackBodyHeightPixels, specAttackFrameSizePixels);
        public float SkillNormalizationScaleMultiplier =>
            GetBodyNormalizationScaleMultiplier(skillBodyHeightPixels, skillFrameSizePixels);
        public RuntimeAnimatorController AnimatorController => animatorController;
        public float IdleFramesPerSecond => idleFramesPerSecond;
        public float AttackFramesPerSecond => attackFramesPerSecond;
        public float VisualScale => visualScale;
        public float IdleNormalizationScaleMultiplier => idleNormalizationScaleMultiplier;
        public float VisualScaleTransitionDuration => visualScaleTransitionDuration;
        public Vector2 VisualOffset => visualOffset;
        public CharacterFacing SourceFacing => sourceFacing;
        public bool HasAnimation => HasFrames(idleFrames)
            && idleClip != null && attackClip != null && animatorController != null;
        public bool HasAttackFrames => HasFrames(attackFrames);

        static bool HasFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0)
                return false;
            foreach (Sprite frame in frames)
                if (frame == null)
                    return false;
            return true;
        }

        static float GetBodyNormalizationScaleMultiplier(float bodyHeightPixels, float frameSizePixels)
        {
            if (bodyHeightPixels <= 0f || frameSizePixels <= 0f)
                return 1f;
            return (AttackBodyHeightPixels * frameSizePixels)
                / (AttackFrameSizePixels * bodyHeightPixels);
        }
    }
}
