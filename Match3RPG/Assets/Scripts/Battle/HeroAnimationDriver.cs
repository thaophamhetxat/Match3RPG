using System;
using System.Collections;
using UnityEngine;

namespace Match3RPG.Battle
{
    [DisallowMultipleComponent]
    public sealed class HeroAnimationDriver : MonoBehaviour
    {
        static readonly int AttackTrigger = Animator.StringToHash("Attack");
        static readonly int DeathTrigger = Animator.StringToHash("Death");
        static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
        static readonly int SpecAttackState = Animator.StringToHash("Base Layer.SpecAttack");
        static readonly int SkillState = Animator.StringToHash("Base Layer.Skill");
        static readonly int HitState = Animator.StringToHash("Base Layer.Hit");
        static readonly int DodgeState = Animator.StringToHash("Base Layer.Dodge");
        static readonly int RisingState = Animator.StringToHash("Base Layer.Rising");
        static readonly int DeathState = Animator.StringToHash("Base Layer.Death");
        const float DefaultAttackImpactNormalizedTime = .52f;

        Animator animator;
        string characterId = "Hero";
        AnimationClip attackClip;
        AnimationClip specAttackClip;
        AnimationClip skillClip;
        AnimationClip hitClip;
        AnimationClip dodgeClip;
        AnimationClip risingClip;
        AnimationClip deathClip;
        bool attackFramesAvailable;
        float attackImpactNormalizedTime = DefaultAttackImpactNormalizedTime;
        BattleCharacterAnimState currentState = BattleCharacterAnimState.Idle;
        int attackVersion;
        int lastObservedStateHash;
        bool attackRequestLogged;
        bool idleToAttackLogged;
        bool attackToIdleLogged;

        public bool IsReady => animator != null && animator.runtimeAnimatorController != null
            && attackClip != null && animator.HasState(0, IdleState)
            && animator.HasState(0, AttackState);
        public bool HasAttackFrames => IsReady && attackFramesAvailable;
        public bool IsAttackPlaying => IsReady
            && animator.GetCurrentAnimatorStateInfo(0).fullPathHash == AttackState;
        public int ImpactCallbackCount { get; private set; }
        public event Action<int> AttackImpact;
        public event Action<int> SkillImpact;

        public void Configure(Animator characterAnimator, HeroPresentationConfig presentation)
        {
            ConfigureAnimator(characterAnimator,
                presentation != null ? presentation.AttackClip : null,
                presentation != null ? presentation.SpecAttackClip : null,
                presentation != null ? presentation.SkillClip : null,
                null, null, null,
                presentation != null ? presentation.DeathClip : null,
                presentation != null && presentation.HasAttackFrames,
                DefaultAttackImpactNormalizedTime);
        }

        public void Configure(Animator characterAnimator, CharacterAnimationSet animations)
        {
            characterId = animations != null ? animations.CharacterId : "Character";
            ConfigureAnimator(characterAnimator,
                animations != null ? animations.AttackClip : null,
                animations != null ? animations.SpecAttackClip : null,
                animations != null ? animations.SkillClip : null,
                animations != null ? animations.HitClip : null,
                animations != null ? animations.DodgeClip : null,
                animations != null ? animations.RisingClip : null,
                animations != null ? animations.DeathClip : null,
                animations != null && animations.HasAttackFrames,
                animations != null
                    ? animations.AttackImpactNormalizedTime
                    : DefaultAttackImpactNormalizedTime);
        }

        void ConfigureAnimator(Animator characterAnimator, AnimationClip attack,
            AnimationClip specAttack, AnimationClip skill, AnimationClip hit,
            AnimationClip dodge, AnimationClip rising, AnimationClip death,
            bool hasAttackFrames, float singleHitImpactNormalizedTime)
        {
            animator = characterAnimator;
            attackClip = attack;
            specAttackClip = specAttack;
            skillClip = skill;
            hitClip = hit;
            dodgeClip = dodge;
            risingClip = rising;
            deathClip = death;
            attackFramesAvailable = hasAttackFrames;
            attackImpactNormalizedTime = Mathf.Clamp01(singleHitImpactNormalizedTime);
            if (!IsReady)
            {
                Debug.LogWarning($"{characterId} animation controller is unavailable; the static portrait will be used.", this);
                return;
            }
            if (!attackFramesAvailable)
                Debug.LogWarning($"{characterId} Attack frames are unavailable; Attack and Skill fallback will remain in Idle.", this);

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.speed = 1f;
            animator.Play(IdleState, 0, 0f);
            animator.Update(0f);
            lastObservedStateHash = IdleState;
        }

        void Update()
        {
            if (!IsReady)
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            int observedStateHash = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0).fullPathHash
                : state.fullPathHash;
            if (observedStateHash == lastObservedStateHash)
                return;

            if (observedStateHash == AttackState && !idleToAttackLogged)
            {
                idleToAttackLogged = true;
                LogDevelopment($"[{characterId}] Idle -> Attack transition observed.");
            }
            else if (observedStateHash == IdleState
                && lastObservedStateHash == AttackState && !attackToIdleLogged)
            {
                attackToIdleLogged = true;
                LogDevelopment($"[{characterId}] Attack -> Idle transition observed.");
            }

            lastObservedStateHash = observedStateHash;
        }

        public void PlayAttack(int hitCount, float hitInterval, float hitReactionDuration)
        {
            if (!IsReady || currentState == BattleCharacterAnimState.Death)
                return;
            if (!attackRequestLogged)
            {
                attackRequestLogged = true;
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                LogDevelopment($"[{characterId}] PlayAttack requested; controller="
                    + $"{animator.runtimeAnimatorController.name}, currentState={GetStateName(state)}, "
                    + $"trigger=Attack ({(HasAttackTrigger() ? "available" : "missing")}).");
            }
            if (!attackFramesAvailable)
            {
                Debug.LogError($"[{characterId}]: PlayAttack was requested but no valid Attack frames are assigned.", this);
                PlayIdle();
                return;
            }

            attackVersion++;
            currentState = BattleCharacterAnimState.Attack;
            animator.ResetTrigger(AttackTrigger);
            float firstImpact = hitCount == 3 ? .28f : hitCount > 3 ? .18f : .52f;
            float impactsDuration = Mathf.Max(0, hitCount - 1)
                * (Mathf.Max(0f, hitInterval) + Mathf.Max(0f, hitReactionDuration));
            float requiredDuration = impactsDuration / (1f - firstImpact);
            animator.speed = requiredDuration > attackClip.length
                ? attackClip.length / requiredDuration
                : 1f;
            animator.SetTrigger(AttackTrigger);
        }

        public void PlayAttack()
        {
            PlayAttack(1, 0f, 0f);
        }

        public void PlayIdle()
        {
            if (!IsReady || currentState == BattleCharacterAnimState.Death)
                return;

            attackVersion++;
            currentState = BattleCharacterAnimState.Idle;
            animator.ResetTrigger(AttackTrigger);
            animator.speed = 1f;
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == IdleState
                && !animator.IsInTransition(0))
                return;
            animator.Play(IdleState, 0, 0f);
        }

        public BattleCharacterAnimState PlaySkill()
        {
            if (!IsReady || currentState == BattleCharacterAnimState.Death)
                return currentState;
            if (skillClip != null && animator.HasState(0, SkillState))
                return PlayOptionalState(BattleCharacterAnimState.Skill, SkillState, skillClip);
            return PlaySpecAttack();
        }

        public BattleCharacterAnimState PlaySpecAttack()
        {
            if (!IsReady || currentState == BattleCharacterAnimState.Death)
                return currentState;
            if (specAttackClip != null && animator.HasState(0, SpecAttackState))
                return PlayOptionalState(BattleCharacterAnimState.SpecAttack, SpecAttackState, specAttackClip);
            if (attackClip != null && attackFramesAvailable)
            {
                PlayAttack();
                return BattleCharacterAnimState.Attack;
            }

            PlayIdle();
            return BattleCharacterAnimState.Idle;
        }

        public BattleCharacterAnimState PlayHit()
        {
            return PlayOptionalState(BattleCharacterAnimState.Hit, HitState, hitClip);
        }

        public BattleCharacterAnimState PlayDodge()
        {
            return PlayOptionalState(BattleCharacterAnimState.Dodge, DodgeState, dodgeClip);
        }

        public BattleCharacterAnimState PlayRising()
        {
            return PlayOptionalState(BattleCharacterAnimState.Rising, RisingState, risingClip);
        }

        public IEnumerator WaitForStateComplete(BattleCharacterAnimState state)
        {
            AnimationClip clip = GetClip(state);
            int stateHash = GetStateHash(state);
            if (!IsReady || currentState != state || clip == null || !animator.HasState(0, stateHash))
                yield break;

            int version = attackVersion;
            float elapsed = 0f;
            while (version == attackVersion && currentState == state && elapsed < clip.length + .25f)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.fullPathHash != stateHash && !animator.IsInTransition(0))
                    yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public void PlayDeath()
        {
            if (!IsReady)
                return;

            attackVersion++;
            currentState = BattleCharacterAnimState.Death;
            animator.ResetTrigger(AttackTrigger);
            animator.ResetTrigger(DeathTrigger);
            if (deathClip != null && animator.HasState(0, DeathState))
            {
                animator.speed = 1f;
                animator.SetTrigger(DeathTrigger);
                animator.Play(DeathState, 0, 0f);
            }
            else
                animator.speed = 0f;
        }

        public IEnumerator WaitForSkillComplete()
        {
            if (!IsReady || currentState != BattleCharacterAnimState.Skill || skillClip == null)
                yield break;

            int version = attackVersion;
            float elapsed = 0f;
            while (version == attackVersion && currentState == BattleCharacterAnimState.Skill
                && elapsed < skillClip.length + .25f)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash != SkillState && !animator.IsInTransition(0))
                    yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator WaitForImpact(int hitIndex, int hitCount)
        {
            if (!IsReady || !attackFramesAvailable)
                yield break;

            int version = attackVersion;
            float threshold = GetImpactThreshold(hitIndex, hitCount, attackImpactNormalizedTime);
            float elapsed = 0f;
            while (version == attackVersion && elapsed < attackClip.length)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == AttackState && state.normalizedTime >= threshold)
                {
                    OnAttackImpact(Mathf.Clamp(hitIndex, 0, 2));
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator WaitForAttackComplete()
        {
            if (!IsReady || !attackFramesAvailable)
                yield break;

            int version = attackVersion;
            float elapsed = 0f;
            while (version == attackVersion && elapsed < attackClip.length + .25f)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash != AttackState && !animator.IsInTransition(0))
                    yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (version == attackVersion)
                PlayIdle();
        }

        public void OnAttackImpact(int impactIndex)
        {
            ImpactCallbackCount++;
            AttackImpact?.Invoke(impactIndex);
        }

        public void OnSkillImpact(int impactIndex = 0)
        {
            SkillImpact?.Invoke(impactIndex);
        }

        BattleCharacterAnimState PlayOptionalState(BattleCharacterAnimState state, int stateHash,
            AnimationClip clip = null)
        {
            if (!IsReady || currentState == BattleCharacterAnimState.Death)
                return currentState;
            if (clip == null || !animator.HasState(0, stateHash))
                return BattleCharacterAnimState.Idle;
            attackVersion++;
            currentState = state;
            animator.ResetTrigger(AttackTrigger);
            animator.speed = 1f;
            animator.Play(stateHash, 0, 0f);
            return state;
        }

        AnimationClip GetClip(BattleCharacterAnimState state)
        {
            switch (state)
            {
                case BattleCharacterAnimState.Skill: return skillClip;
                case BattleCharacterAnimState.SpecAttack: return specAttackClip;
                case BattleCharacterAnimState.Hit: return hitClip;
                case BattleCharacterAnimState.Dodge: return dodgeClip;
                case BattleCharacterAnimState.Rising: return risingClip;
                default: return null;
            }
        }

        static int GetStateHash(BattleCharacterAnimState state)
        {
            switch (state)
            {
                case BattleCharacterAnimState.Skill: return SkillState;
                case BattleCharacterAnimState.SpecAttack: return SpecAttackState;
                case BattleCharacterAnimState.Hit: return HitState;
                case BattleCharacterAnimState.Dodge: return DodgeState;
                case BattleCharacterAnimState.Rising: return RisingState;
                default: return 0;
            }
        }

        static float GetImpactThreshold(int hitIndex, int hitCount, float singleHitImpactNormalizedTime)
        {
            if (hitCount <= 1)
                return Mathf.Clamp01(singleHitImpactNormalizedTime);
            if (hitCount == 3)
                return hitIndex == 0 ? .28f : hitIndex == 1 ? .52f : .78f;
            return Mathf.Lerp(.18f, .84f, hitIndex / (float)(hitCount - 1));
        }

        bool HasAttackTrigger()
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == AttackTrigger
                    && parameter.type == AnimatorControllerParameterType.Trigger)
                    return true;
            return false;
        }

        static string GetStateName(AnimatorStateInfo state)
        {
            if (state.fullPathHash == IdleState) return "Idle";
            if (state.fullPathHash == AttackState) return "Attack";
            if (state.fullPathHash == SpecAttackState) return "SpecAttack";
            if (state.fullPathHash == SkillState) return "Skill";
            if (state.fullPathHash == HitState) return "Hit";
            if (state.fullPathHash == DodgeState) return "Dodge";
            if (state.fullPathHash == RisingState) return "Rising";
            if (state.fullPathHash == DeathState) return "Death";
            return state.fullPathHash.ToString();
        }

        static void LogDevelopment(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message);
#endif
        }
    }
}
