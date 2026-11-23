using System;
using System.Collections;
using Match3RPG.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class SwordQiSkillController : MonoBehaviour
    {
        public const float DefaultProjectileSpeed = 1125f;
        public const float DefaultHeightHeroRatio = 0.95f;

        [Header("Play-test Skill Level (TẠM: chỉ Lv1)")]
        [SerializeField, Range(1, 1)] int skillLevel = SkillConfig.DefaultLevel;

        [Header("Sword Qi Projectile (Kiếm Khí)")]
        [SerializeField, Min(50f)] float projectileSpeed = DefaultProjectileSpeed;
        [SerializeField, Min(0.1f)] float heightHeroRatio = DefaultHeightHeroRatio;

        RectTransform projectileLayer;
        GameObject activeProjectile;
        bool isRunning;

        public bool IsRunning => isRunning;
        public int SkillLevel
        {
            get => Mathf.Clamp(skillLevel, 1, 1);
            set => skillLevel = Mathf.Clamp(value, 1, 1);
        }
        public float ProjectileSpeed => projectileSpeed;

        void OnDisable()
        {
            StopAndClear();
        }

        public void StopAndClear()
        {
            isRunning = false;
            if (activeProjectile != null)
            {
                Destroy(activeProjectile);
                activeProjectile = null;
            }
        }

        public IEnumerator ExecuteSkill(
            CharacterView hero,
            CharacterView boss,
            SkillConfig config,
            Func<IEnumerator> onImpact)
        {
            if (isRunning)
                yield break;

            isRunning = true;
            Sprite swordQiSprite = config != null ? config.Icon : SkillConfig.ResolveSwordQiSprite();
            if (swordQiSprite == null)
                swordQiSprite = SkillConfig.ResolveSwordQiSprite();

            if (swordQiSprite == null)
            {
                Debug.LogError($"[SwordQiSkill] Missing sprite at {SkillConfig.SwordQiAssetPath}.", this);
                isRunning = false;
                yield break;
            }

            bool heroAttackDone = false;
            bool impactReleased = false;
            Coroutine heroRoutine = null;

            try
            {
                if (hero != null)
                {
                    heroRoutine = StartCoroutine(RunHeroAttackInPlace(
                        hero,
                        () => impactReleased = true,
                        () => heroAttackDone = true));

                    while (!impactReleased && !heroAttackDone)
                        yield return null;
                }

                EnsureProjectileLayer(hero, boss);
                if (projectileLayer != null)
                {
                    yield return FlyCrescentToBoss(hero, boss, swordQiSprite);
                }

                if (onImpact != null)
                    yield return onImpact();

                while (heroRoutine != null && !heroAttackDone)
                    yield return null;
            }
            finally
            {
                if (activeProjectile != null)
                {
                    Destroy(activeProjectile);
                    activeProjectile = null;
                }
                isRunning = false;
            }
        }

        static IEnumerator RunHeroAttackInPlace(
            CharacterView hero,
            Action onImpactPoint,
            Action onFinished)
        {
            try
            {
                yield return hero.PlayAttackInPlaceWithoutLunge(onImpactPoint);
            }
            finally
            {
                onImpactPoint?.Invoke();
                onFinished?.Invoke();
            }
        }

        IEnumerator FlyCrescentToBoss(CharacterView hero, CharacterView boss, Sprite swordQiSprite)
        {
            Vector2 startLocal = ResolveAnchorLocal(hero, new Vector2(-270f, 0f));
            Vector2 bossLocal = ResolveAnchorLocal(boss, new Vector2(270f, 0f));
            // Kiếm khí bay ngang từ hero tới boss.
            Vector2 endLocal = new Vector2(bossLocal.x, startLocal.y);

            float heroHeight = FlyingSwordSkillController.MeasureHeroBattleHeight(hero);
            float displayHeight = heroHeight * Mathf.Max(0.1f, heightHeroRatio);
            float spriteAspect = swordQiSprite.rect.height > 0f
                ? swordQiSprite.rect.width / swordQiSprite.rect.height
                : (1115f / 1572f);
            Vector2 crescentSize = new Vector2(displayHeight * spriteAspect, displayHeight);

            GameObject projectileObj = new GameObject(
                "SwordQiCrescent",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            activeProjectile = projectileObj;

            RectTransform projectileRect = projectileObj.GetComponent<RectTransform>();
            projectileRect.SetParent(projectileLayer, false);
            projectileRect.anchorMin = new Vector2(0.5f, 0.5f);
            projectileRect.anchorMax = new Vector2(0.5f, 0.5f);
            projectileRect.pivot = new Vector2(0.5f, 0.5f);
            projectileRect.sizeDelta = crescentSize;
            projectileRect.anchoredPosition = startLocal;
            projectileRect.localEulerAngles = Vector3.zero;
            projectileRect.localScale = Vector3.one;

            Image projectileImage = projectileObj.GetComponent<Image>();
            projectileImage.sprite = swordQiSprite;
            projectileImage.preserveAspect = true;
            projectileImage.raycastTarget = false;
            projectileImage.color = Color.white;

            float distance = Vector2.Distance(startLocal, endLocal);
            float duration = Mathf.Max(0.05f, distance / Mathf.Max(50f, projectileSpeed));
            float elapsed = 0f;
            int nextGhostIndex = 0;

            while (elapsed < duration && projectileObj != null)
            {
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                elapsed += dt;
                float progress = Mathf.Clamp01(elapsed / duration);
                projectileRect.anchoredPosition = Vector2.Lerp(startLocal, endLocal, progress);

                while (nextGhostIndex < AfterimageEffect.DistanceFractions.Length
                    && progress >= AfterimageEffect.DistanceFractions[nextGhostIndex])
                {
                    float fraction = AfterimageEffect.DistanceFractions[nextGhostIndex];
                    Vector2 ghostPos = Vector2.Lerp(startLocal, endLocal, fraction);
                    float ghostAlpha = AfterimageEffect.GetInitialAlphaForStep(nextGhostIndex);
                    AfterimageEffect.SpawnUIGhost(
                        projectileRect,
                        projectileImage,
                        projectileLayer,
                        projectileRect.GetSiblingIndex(),
                        ghostAlpha,
                        AfterimageEffect.DefaultFadeDuration,
                        ghostPos,
                        projectileRect.localScale);
                    nextGhostIndex++;
                }

                yield return null;
            }

            if (projectileObj != null)
            {
                Destroy(projectileObj);
                if (activeProjectile == projectileObj)
                    activeProjectile = null;
            }
        }

        Vector2 ResolveAnchorLocal(CharacterView character, Vector2 fallback)
        {
            if (character == null || projectileLayer == null)
                return fallback;

            RectTransform anchor = character.VFXAnchor != null
                ? character.VFXAnchor
                : (character.VisualRect != null ? character.VisualRect : character.transform as RectTransform);
            if (anchor == null)
                return fallback;

            return projectileLayer.InverseTransformPoint(anchor.position);
        }

        void EnsureProjectileLayer(CharacterView hero, CharacterView boss)
        {
            if (projectileLayer != null)
            {
                projectileLayer.SetAsLastSibling();
                return;
            }

            Transform characterLayer = null;
            if (hero != null && hero.transform.parent != null)
                characterLayer = hero.transform.parent.parent;
            else if (boss != null && boss.transform.parent != null)
                characterLayer = boss.transform.parent.parent;

            RectTransform parentRect = characterLayer as RectTransform;
            if (parentRect == null)
                return;

            GameObject layerObj = new GameObject("SwordQiLayer", typeof(RectTransform));
            projectileLayer = layerObj.GetComponent<RectTransform>();
            projectileLayer.SetParent(parentRect, false);
            projectileLayer.anchorMin = Vector2.zero;
            projectileLayer.anchorMax = Vector2.one;
            projectileLayer.offsetMin = Vector2.zero;
            projectileLayer.offsetMax = Vector2.zero;
            projectileLayer.pivot = new Vector2(0.5f, 0.5f);
            projectileLayer.localScale = Vector3.one;
            projectileLayer.SetAsLastSibling();
        }
    }
}
