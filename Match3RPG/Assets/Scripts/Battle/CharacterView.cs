using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public enum CharacterFacing
    {
        Left,
        Right
    }

    public sealed class CharacterView : MonoBehaviour
    {
        public enum CharacterState { Idle, Attack, SpecAttack, Hit, Dodge, Rising, Skill, Death }
        public const float HitAnimationDuration = .14f;

        [SerializeField] Image portrait;
        [SerializeField] float attackDistance = 80f;
        [SerializeField] CharacterFacing facingDirection = CharacterFacing.Right;
        [SerializeField] CharacterFacing spriteFacingDirection = CharacterFacing.Right;
        RectTransform rectTransform;
        RectTransform floatingTextAnchor;
        RectTransform vfxAnchor;
        RectTransform persistentVfxAnchor;
        RectTransform headVfxAnchor;
        RectTransform healAnchor;
        HeroAnimationDriver heroAnimation;
        Vector2 homePosition;
        Vector2 configuredVisualOffset;
        Vector3 homeVisualScale = Vector3.one;
        bool homePositionInitialized;
        bool baseVisualScaleInitialized;
        bool configuredVisualOffsetInitialized;
        float idleNormalizationScaleMultiplier = 1f;
        float attackNormalizationScaleMultiplier = 1f;
        float specAttackNormalizationScaleMultiplier = 1f;
        float skillNormalizationScaleMultiplier = 1f;
        float idleFeetPaddingFraction;
        float visualScaleTransitionDuration = HeroPresentationConfig.DefaultVisualScaleTransitionDuration;
        bool attackInPlace;
        bool idleFacesForward;
        Color portraitColor = Color.white;
        int animationVersion;

        public CharacterState State { get; private set; } = CharacterState.Idle;
        public CharacterFacing FacingDirection => facingDirection;
        public RectTransform FloatingTextAnchor => floatingTextAnchor;
        public RectTransform VFXAnchor => vfxAnchor;
        public RectTransform PersistentVFXAnchor => persistentVfxAnchor;
        public RectTransform HeadVFXAnchor => headVfxAnchor;
        public RectTransform HealAnchor => healAnchor;
        public bool HasSpriteAttackAnimation => heroAnimation != null && heroAnimation.HasAttackFrames;

        void Awake()
        {
            if (portrait == null) portrait = GetComponentInChildren<Image>();
        }

        void OnDisable()
        {
            CancelActiveAnimation();
        }

        public void Configure(Image characterPortrait, RectTransform characterVisual, float distance,
            CharacterFacing facing, CharacterFacing spriteFacing, HeroAnimationDriver animationDriver = null,
            float idleNormalizationMultiplier = 1f,
            float scaleTransitionDuration = HeroPresentationConfig.DefaultVisualScaleTransitionDuration,
            float specAttackNormalizationMultiplier = 1f,
            float skillNormalizationMultiplier = 1f,
            float idleFeetPadding = 0f,
            float attackNormalizationMultiplier = 1f,
            bool keepAttackInPlace = false,
            bool keepIdleFacingForward = false)
        {
            portrait = characterPortrait;
            rectTransform = characterVisual;
            if (rectTransform == null)
                Debug.LogError("CharacterView requires a CharacterVisual RectTransform.", this);
            attackDistance = distance;
            facingDirection = facing;
            spriteFacingDirection = spriteFacing;
            heroAnimation = animationDriver;
            idleNormalizationScaleMultiplier = Mathf.Max(.1f, idleNormalizationMultiplier);
            attackNormalizationScaleMultiplier = Mathf.Max(.1f, attackNormalizationMultiplier);
            specAttackNormalizationScaleMultiplier = Mathf.Max(.1f, specAttackNormalizationMultiplier);
            skillNormalizationScaleMultiplier = Mathf.Max(.1f, skillNormalizationMultiplier);
            idleFeetPaddingFraction = Mathf.Clamp01(idleFeetPadding);
            visualScaleTransitionDuration = Mathf.Max(0f, scaleTransitionDuration);
            attackInPlace = keepAttackInPlace;
            idleFacesForward = keepIdleFacingForward;
            if (portrait != null) portraitColor = portrait.color;
            ApplyIdleFacingDirection();
        }

        public void CaptureHomePosition()
        {
            if (State == CharacterState.Death)
                return;
            if (rectTransform == null)
                Debug.LogError("CharacterView requires a CharacterVisual RectTransform.", this);
            else
            {
                if (!configuredVisualOffsetInitialized)
                {
                    configuredVisualOffset = rectTransform.anchoredPosition;
                    configuredVisualOffsetInitialized = true;
                }
                CancelActiveAnimation();
                homePosition = rectTransform.anchoredPosition;
                if (!baseVisualScaleInitialized)
                {
                    homeVisualScale = rectTransform.localScale;
                    baseVisualScaleInitialized = true;
                }
                SetVisualScale(idleNormalizationScaleMultiplier);
                homePosition = configuredVisualOffset
                    + Vector2.down * rectTransform.rect.height * idleFeetPaddingFraction
                    * Mathf.Abs(rectTransform.localScale.y);
                rectTransform.anchoredPosition = homePosition;
                homePositionInitialized = true;
                ApplyIdleFacingDirection();
                Debug.Log($"[{name}] Home position captured: {homePosition}", this);
                Debug.Log($"[{name}] Facing={facingDirection}, BaseScale={homeVisualScale}", this);
            }
        }

        public void SetFloatingTextAnchor(RectTransform anchor)
        {
            floatingTextAnchor = anchor;
        }

        public void SetCombatVFXAnchors(RectTransform torsoAnchor, RectTransform persistentAnchor,
            RectTransform headAnchor, RectTransform healAnchor)
        {
            vfxAnchor = torsoAnchor;
            persistentVfxAnchor = persistentAnchor;
            headVfxAnchor = headAnchor;
            this.healAnchor = healAnchor;
        }

        public IEnumerator Attack()
        {
            return PlayAnimation(CharacterState.Attack);
        }

        public IEnumerator WaitForAttackImpact(int hitIndex, int hitCount)
        {
            if (heroAnimation != null)
                yield return heroAnimation.WaitForImpact(hitIndex, hitCount);
        }

        public IEnumerator BeginAttackSequence(int hitCount, float hitInterval, float hitReactionDuration)
        {
            return PlayAttackPhase(true, hitCount, hitInterval, hitReactionDuration);
        }

        public IEnumerator EndAttackSequence()
        {
            return PlayAttackPhase(false, 0, 0f, 0f);
        }

        public void CancelAttackSequence()
        {
            CancelActiveAnimation();
        }

        public IEnumerator Hit()
        {
            return PlayAnimation(CharacterState.Hit);
        }

        public IEnumerator Dodge()
        {
            return PlayAnimation(CharacterState.Dodge);
        }

        public IEnumerator Rising()
        {
            return PlayAnimation(CharacterState.Rising);
        }

        public IEnumerator Skill()
        {
            return PlaySkill();
        }

        public IEnumerator PlaySkill()
        {
            return PlayAnimation(CharacterState.Skill);
        }

        public IEnumerator PlaySpecAttack()
        {
            return PlayAnimation(CharacterState.SpecAttack);
        }

        public IEnumerator Death()
        {
            if (State == CharacterState.Death)
                yield break;
            if (rectTransform == null)
            {
                Debug.LogError("CharacterView cannot play Death without a CharacterVisual RectTransform.", this);
                yield break;
            }
            CancelActiveAnimation();
            EnsureHomePosition();
            RestoreHomeVisual();
            State = CharacterState.Death;
            heroAnimation?.PlayDeath();
            if (portrait != null)
            {
                Color color = portrait.color;
                color.a = .35f;
                portrait.color = color;
            }
            yield break;
        }

        IEnumerator PlayAnimation(CharacterState animationState)
        {
            if (State == CharacterState.Death)
                yield break;
            if (rectTransform == null)
            {
                Debug.LogError($"CharacterView cannot play {animationState} without a CharacterVisual RectTransform.", this);
                yield break;
            }
            CancelActiveAnimation();
            EnsureHomePosition();
            RestoreHomeVisual();
            int version = ++animationVersion;
            State = animationState;
            if (animationState == CharacterState.Attack)
            {
                if (attackInPlace)
                    ApplyAttackFacingDirection();
                heroAnimation?.PlayAttack();
            }
            if (animationState == CharacterState.Attack)
                Debug.Log($"[{name}] Attack start. Home={homePosition}, Current={rectTransform.anchoredPosition}", this);
            BattleCharacterAnimState skillAnimationState = BattleCharacterAnimState.Idle;
            if (heroAnimation != null && animationState == CharacterState.Skill)
                skillAnimationState = heroAnimation.PlaySkill();
            else if (heroAnimation != null && animationState == CharacterState.SpecAttack)
                skillAnimationState = heroAnimation.PlaySpecAttack();
            else if (heroAnimation != null && animationState == CharacterState.Hit)
                heroAnimation.PlayHit();
            else if (heroAnimation != null && animationState == CharacterState.Dodge)
                heroAnimation.PlayDodge();
            else if (heroAnimation != null && animationState == CharacterState.Rising)
                heroAnimation.PlayRising();
            yield return Animate(animationState, version, skillAnimationState);
        }

        IEnumerator PlayAttackPhase(bool approach, int hitCount, float hitInterval, float hitReactionDuration)
        {
            if (State == CharacterState.Death)
                yield break;
            if (rectTransform == null)
            {
                Debug.LogError("CharacterView cannot play an attack sequence without a CharacterVisual RectTransform.", this);
                yield break;
            }

            if (approach)
            {
                CancelActiveAnimation();
                EnsureHomePosition();
                RestoreHomeVisual();
                if (attackInPlace)
                    ApplyAttackFacingDirection();
            }
            else
            {
                EnsureHomePosition();
                animationVersion++;
                ApplyFacingDirection();
            }

            int version = ++animationVersion;
            State = CharacterState.Attack;
            if (approach)
            {
                heroAnimation?.PlayAttack(hitCount, hitInterval, hitReactionDuration);
                Debug.Log($"[{name}] Attack start. Home={homePosition}, Current={rectTransform.anchoredPosition}", this);
            }

            try
            {
                if (!approach && heroAnimation != null)
                    yield return heroAnimation.WaitForAttackComplete();
                if (!approach)
                {
                    RestoreIdleScaleAndFacing();
                    if (attackInPlace)
                        heroAnimation?.PlayIdle();
                }
                Vector2 target = approach && !attackInPlace
                    ? homePosition + AttackOffset(1f)
                    : homePosition;
                if (approach)
                    yield return MoveTo(target, .10f, version,
                        idleNormalizationScaleMultiplier * attackNormalizationScaleMultiplier,
                        useAttackFacing: true);
                else if (!attackInPlace)
                    yield return MoveTo(target, .14f, version);
            }
            finally
            {
                if (version == animationVersion)
                {
                    if (approach)
                    {
                        ApplyFacingDirection();
                        State = CharacterState.Attack;
                    }
                    else
                    {
                        RestoreHomeVisual();
                        State = CharacterState.Idle;
                        Debug.Log($"[{name}] Attack complete, Facing={facingDirection}, Scale={rectTransform.localScale}", this);
                    }
                }
            }
        }

        IEnumerator Animate(CharacterState animationState, int version,
            BattleCharacterAnimState skillAnimationState)
        {
            Color originalColor = portrait != null ? portrait.color : portraitColor;
            try
            {
                if (animationState == CharacterState.Attack)
                {
                    Vector2 attackTarget = attackInPlace
                        ? homePosition
                        : homePosition + AttackOffset(1f);
                    yield return MoveTo(attackTarget, .10f, version,
                        idleNormalizationScaleMultiplier * attackNormalizationScaleMultiplier,
                        useAttackFacing: true);
                    if (version != animationVersion) yield break;
                    if (heroAnimation != null)
                        yield return heroAnimation.WaitForAttackComplete();
                    if (version != animationVersion) yield break;
                    RestoreIdleScaleAndFacing();
                    if (attackInPlace)
                        heroAnimation?.PlayIdle();
                    else
                        yield return MoveTo(homePosition, .14f, version);
                }
                else if (animationState == CharacterState.Hit)
                {
                    if (heroAnimation != null)
                        yield return heroAnimation.WaitForStateComplete(BattleCharacterAnimState.Hit);
                    if (portrait != null) portrait.color = Color.white;
                    for (int i = 0; i < 4; i++)
                    {
                        if (version != animationVersion) yield break;
                        float offset = i % 2 == 0 ? 12f : -12f;
                        rectTransform.anchoredPosition = homePosition + Vector2.right * offset;
                        yield return new WaitForSeconds(.035f);
                    }
                }
                else if (animationState == CharacterState.Dodge
                    || animationState == CharacterState.Rising)
                {
                    BattleCharacterAnimState state = animationState == CharacterState.Dodge
                        ? BattleCharacterAnimState.Dodge
                        : BattleCharacterAnimState.Rising;
                    if (heroAnimation != null)
                        yield return heroAnimation.WaitForStateComplete(state);
                }
                else if (animationState == CharacterState.Skill
                    || animationState == CharacterState.SpecAttack)
                {
                    if (animationState == CharacterState.Skill && portrait != null)
                        portrait.color = new Color(1f, .88f, .45f);
                    float skillScale = skillAnimationState == BattleCharacterAnimState.Skill
                        ? skillNormalizationScaleMultiplier
                        : skillAnimationState == BattleCharacterAnimState.SpecAttack
                            ? specAttackNormalizationScaleMultiplier
                            : skillAnimationState == BattleCharacterAnimState.Attack
                                ? idleNormalizationScaleMultiplier * attackNormalizationScaleMultiplier
                                : idleNormalizationScaleMultiplier;
                    bool attackFallbackInPlace = attackInPlace
                        && skillAnimationState == BattleCharacterAnimState.Attack;
                    if (attackFallbackInPlace)
                        ApplyAttackFacingDirection();
                    Vector2 skillTarget = attackFallbackInPlace
                        ? homePosition
                        : homePosition + AttackOffset(1.25f);
                    yield return MoveTo(skillTarget, .14f, version, skillScale,
                        useAttackFacing: attackFallbackInPlace);
                    if (version != animationVersion) yield break;
                    if (attackFallbackInPlace)
                    {
                        if (heroAnimation != null)
                            yield return heroAnimation.WaitForAttackComplete();
                        if (version != animationVersion) yield break;
                        RestoreIdleScaleAndFacing();
                        heroAnimation?.PlayIdle();
                    }
                    else
                        yield return MoveTo(homePosition, .18f, version, idleNormalizationScaleMultiplier);
                    if (animationState == CharacterState.Skill
                        && skillAnimationState == BattleCharacterAnimState.Skill
                        && heroAnimation != null)
                        yield return heroAnimation.WaitForSkillComplete();
                    else if (skillAnimationState == BattleCharacterAnimState.SpecAttack
                        && heroAnimation != null)
                        yield return heroAnimation.WaitForStateComplete(BattleCharacterAnimState.SpecAttack);
                    else if (skillAnimationState == BattleCharacterAnimState.Attack
                        && heroAnimation != null)
                        yield return heroAnimation.WaitForAttackComplete();
                }
            }
            finally
            {
                if (version == animationVersion)
                {
                    RestoreHomeVisual();
                    if (portrait != null) portrait.color = originalColor;
                    if (State != CharacterState.Death) State = CharacterState.Idle;
                    if (State != CharacterState.Death)
                        heroAnimation?.PlayIdle();
                    if (animationState == CharacterState.Attack)
                        Debug.Log($"[{name}] Attack complete, Facing={facingDirection}, Scale={rectTransform.localScale}", this);
                }
            }
        }

        IEnumerator MoveTo(Vector2 target, float duration, int version, float? scaleMultiplier = null,
            bool useAttackFacing = false)
        {
            Vector2 start = rectTransform.anchoredPosition;
            Vector3 startScale = rectTransform.localScale;
            Vector3 targetScale = scaleMultiplier.HasValue
                ? (useAttackFacing
                    ? GetVisualScale(scaleMultiplier.Value)
                    : GetIdleVisualScale(scaleMultiplier.Value))
                : startScale;
            float scaleDuration = Mathf.Min(duration, visualScaleTransitionDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (version != animationVersion) yield break;
                elapsed += Time.deltaTime;
                float movementProgress = Mathf.Clamp01(elapsed / duration);
                rectTransform.anchoredPosition = Vector2.Lerp(start, target, movementProgress);
                if (scaleMultiplier.HasValue)
                {
                    float scaleProgress = scaleDuration > 0f
                        ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / scaleDuration))
                        : 1f;
                    rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, scaleProgress);
                }
                yield return null;
            }
            if (version == animationVersion)
            {
                rectTransform.anchoredPosition = target;
                if (scaleMultiplier.HasValue)
                    rectTransform.localScale = targetScale;
            }
        }

        Vector2 AttackOffset(float distanceMultiplier)
        {
            float direction = facingDirection == CharacterFacing.Right ? 1f : -1f;
            return Vector2.right * (attackDistance * direction * distanceMultiplier);
        }

        void EnsureHomePosition()
        {
            if (!homePositionInitialized) CaptureHomePosition();
        }

        void CancelActiveAnimation()
        {
            animationVersion++;

            if (homePositionInitialized && rectTransform != null)
                RestoreHomeVisual();
            if (portrait != null) portrait.color = portraitColor;
            if (State != CharacterState.Death) State = CharacterState.Idle;
            SetVisualScale(idleNormalizationScaleMultiplier);
            if (State != CharacterState.Death)
                heroAnimation?.PlayIdle();
        }

        void ApplyFacingDirection()
        {
            if (rectTransform == null) return;

            Vector3 currentScale = rectTransform.localScale;
            currentScale.x = Mathf.Abs(currentScale.x) * GetFacingScaleSign();
            rectTransform.localScale = currentScale;
        }

        void ApplyIdleFacingDirection()
        {
            if (rectTransform == null) return;

            Vector3 currentScale = rectTransform.localScale;
            float scaleSign = idleFacesForward ? 1f : GetFacingScaleSign();
            currentScale.x = Mathf.Abs(currentScale.x) * scaleSign;
            rectTransform.localScale = currentScale;
        }

        void ApplyAttackFacingDirection()
        {
            ApplyFacingDirection();
        }

        float GetFacingScaleSign()
        {
            return facingDirection == spriteFacingDirection ? 1f : -1f;
        }

        Vector3 GetVisualScale(float multiplier)
        {
            return new Vector3(Mathf.Abs(homeVisualScale.x) * multiplier * GetFacingScaleSign(),
                homeVisualScale.y * multiplier, homeVisualScale.z);
        }

        Vector3 GetIdleVisualScale(float multiplier)
        {
            float scaleSign = idleFacesForward ? 1f : GetFacingScaleSign();
            return new Vector3(Mathf.Abs(homeVisualScale.x) * multiplier * scaleSign,
                homeVisualScale.y * multiplier, homeVisualScale.z);
        }

        void SetVisualScale(float multiplier)
        {
            if (rectTransform != null && baseVisualScaleInitialized)
                rectTransform.localScale = GetIdleVisualScale(multiplier);
        }

        void RestoreIdleScaleAndFacing()
        {
            SetVisualScale(idleNormalizationScaleMultiplier);
            ApplyIdleFacingDirection();
        }

        void RestoreHomeVisual()
        {
            if (rectTransform == null) return;
            rectTransform.anchoredPosition = homePosition;
            rectTransform.localScale = GetIdleVisualScale(idleNormalizationScaleMultiplier);
            ApplyIdleFacingDirection();
        }
    }
}
