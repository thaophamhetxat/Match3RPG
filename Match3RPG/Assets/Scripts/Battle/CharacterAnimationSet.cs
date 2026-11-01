using UnityEngine;

namespace Match3RPG.Battle
{
    [CreateAssetMenu(menuName = "Match3RPG/Character Animation Set")]
    public sealed class CharacterAnimationSet : ScriptableObject
    {
        [SerializeField] string characterId;
        [SerializeField] RuntimeAnimatorController animatorController;
        [SerializeField] AnimationClip idleClip;
        [SerializeField] AnimationClip attackClip;
        [SerializeField] AnimationClip specAttackClip;
        [SerializeField] AnimationClip skillClip;
        [SerializeField] AnimationClip hitClip;
        [SerializeField] AnimationClip dodgeClip;
        [SerializeField] AnimationClip risingClip;
        [SerializeField] AnimationClip deathClip;
        [SerializeField] Sprite[] idleFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] attackFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0)] int specAttackFrameCount;
        [SerializeField, Min(0)] int skillFrameCount;
        [SerializeField, Min(0)] int hitFrameCount;
        [SerializeField, Min(0)] int dodgeFrameCount;
        [SerializeField, Min(0)] int risingFrameCount;
        [SerializeField, Min(0)] int deathFrameCount;

        public string CharacterId => characterId;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public AnimationClip IdleClip => idleClip;
        public AnimationClip AttackClip => attackClip;
        public AnimationClip SpecAttackClip => specAttackClip;
        public AnimationClip SkillClip => skillClip;
        public AnimationClip HitClip => hitClip;
        public AnimationClip DodgeClip => dodgeClip;
        public AnimationClip RisingClip => risingClip;
        public AnimationClip DeathClip => deathClip;
        public Sprite[] IdleFrames => idleFrames;
        public Sprite[] AttackFrames => attackFrames;
        public bool IsReady => animatorController != null && idleClip != null && attackClip != null;
        public bool HasIdleFrames => HasValidFrames(idleFrames);
        public bool HasAttackFrames => HasValidFrames(attackFrames);

        public int GetFrameCount(BattleCharacterAnimState state)
        {
            switch (state)
            {
                case BattleCharacterAnimState.Idle:
                    return idleFrames != null ? idleFrames.Length : 0;
                case BattleCharacterAnimState.Attack:
                    return attackFrames != null ? attackFrames.Length : 0;
                case BattleCharacterAnimState.SpecAttack: return specAttackFrameCount;
                case BattleCharacterAnimState.Skill: return skillFrameCount;
                case BattleCharacterAnimState.Hit: return hitFrameCount;
                case BattleCharacterAnimState.Dodge: return dodgeFrameCount;
                case BattleCharacterAnimState.Rising: return risingFrameCount;
                case BattleCharacterAnimState.Death: return deathFrameCount;
                default: return 0;
            }
        }

        public AnimationClip GetClip(BattleCharacterAnimState state)
        {
            switch (state)
            {
                case BattleCharacterAnimState.Idle: return idleClip;
                case BattleCharacterAnimState.Attack: return attackClip;
                case BattleCharacterAnimState.SpecAttack: return specAttackClip;
                case BattleCharacterAnimState.Skill: return skillClip;
                case BattleCharacterAnimState.Hit: return hitClip;
                case BattleCharacterAnimState.Dodge: return dodgeClip;
                case BattleCharacterAnimState.Rising: return risingClip;
                case BattleCharacterAnimState.Death: return deathClip;
                default: return null;
            }
        }

        public void Configure(string id, RuntimeAnimatorController controller,
            AnimationClip idle, AnimationClip attack, AnimationClip specAttack,
            AnimationClip skill, AnimationClip hit, AnimationClip dodge,
            AnimationClip rising, AnimationClip death, Sprite[] idleSprites,
            Sprite[] attackSprites, int specAttackFrames, int skillFrames,
            int hitFrames, int dodgeFrames, int risingFrames, int deathFrames)
        {
            characterId = id;
            animatorController = controller;
            idleClip = idle;
            attackClip = attack;
            specAttackClip = specAttack;
            skillClip = skill;
            hitClip = hit;
            dodgeClip = dodge;
            risingClip = rising;
            deathClip = death;
            idleFrames = idleSprites ?? System.Array.Empty<Sprite>();
            attackFrames = attackSprites ?? System.Array.Empty<Sprite>();
            specAttackFrameCount = Mathf.Max(0, specAttackFrames);
            skillFrameCount = Mathf.Max(0, skillFrames);
            hitFrameCount = Mathf.Max(0, hitFrames);
            dodgeFrameCount = Mathf.Max(0, dodgeFrames);
            risingFrameCount = Mathf.Max(0, risingFrames);
            deathFrameCount = Mathf.Max(0, deathFrames);
        }

        static bool HasValidFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0)
                return false;
            foreach (Sprite frame in frames)
                if (frame == null)
                    return false;
            return true;
        }
    }
}
