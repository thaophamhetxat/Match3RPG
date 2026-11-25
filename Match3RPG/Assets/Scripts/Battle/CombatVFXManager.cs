using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public sealed class CombatVFXManager : MonoBehaviour
    {
        const float TransientLifetime = .24f;
        const float HitImpactDuration = .5f;
        const float HitImpactFrameRate = 12f;
        const float HeroShieldDropOffset = 173f;
        const float BossShieldBaseDropOffset = 176f;
        const float DefaultShieldAnchorY = 0.48f;
        const float FallbackCharacterRootHeight = 768f;
        static readonly Vector2 HitImpactSize = new Vector2(220f, 220f);
        public const float HealingAuraDuration = 2.4f;
        public const float LifeStealDuration = 1.333f;
        static Sprite radialFlashSprite;
        readonly bool[] missingSpritesLogged = new bool[17];
        CombatVFXView[,] persistentEffects;
        CombatVFXCatalog catalog;
        RectTransform transientRoot;
        CharacterView hero;
        CharacterView boss;

        void Awake()
        {
            persistentEffects = new CombatVFXView[2, missingSpritesLogged.Length];
        }

        public void Configure(CombatVFXCatalog vfxCatalog, RectTransform effectLayer,
            CharacterView heroView, CharacterView bossView)
        {
            catalog = vfxCatalog != null
                ? vfxCatalog
                : Resources.Load<CombatVFXCatalog>("CombatVFX/CombatVFXCatalog");
            transientRoot = effectLayer;
            hero = heroView;
            boss = bossView;
            if (catalog == null)
                Debug.LogError("Combat VFX catalog is missing at Resources/CombatVFX/CombatVFXCatalog.", this);
        }

        public bool PlayHeal(BattleActor actor)
        {
            CharacterView character = GetCharacter(actor);
            RectTransform anchor = character != null ? character.HealAnchor : null;
            if (anchor == null)
                return false;

            Vector3 position = anchor.position;
            return PlayFrameSequence(CombatVFXType.Heal, position,
                HealingAuraDuration, new Vector2(230f, 325f), 1f, anchor);
        }



        public bool PlayLifeSteal(BattleActor actor)
        {
            CharacterView character = GetCharacter(actor);
            CharacterView target = GetCharacter(Opponent(actor));
            RectTransform sourceAnchor = character != null ? character.VFXAnchor : null;
            RectTransform targetAnchor = target != null ? target.VFXAnchor : null;
            if (sourceAnchor == null || targetAnchor == null)
                return false;

            //Vector3 midpoint = (sourceAnchor.position + targetAnchor.position) * .5f;
            //midpoint.y -= 200f;
            //return PlayFrameSequence(CombatVFXType.LifeSteal, midpoint,
            //    LifeStealDuration, new Vector2(450f, 365f), FacingSign(character));

            Vector3 midpoint = (sourceAnchor.position + targetAnchor.position) * .5f;
            midpoint.y -= 200f;
            float length = Vector3.Distance(sourceAnchor.position, targetAnchor.position) * 0.9f;
            float height = length * 294f / 362f;
            return PlayFrameSequence(CombatVFXType.LifeSteal, midpoint,
                LifeStealDuration, new Vector2(length, height), FacingSign(character));
        }

        bool PlayFrameSequence(CombatVFXType type, Vector3 worldPosition,
            float duration, Vector2 size, float direction, RectTransform anchorOverride = null,
            float frameRate = 30f)
        {
            if (transientRoot == null || !TryGetFrames(type, out Sprite[] frames))
                return false;

            if (anchorOverride != null)
                worldPosition = anchorOverride.position;
            float normalizedPivotY = frames[0].pivot.y / frames[0].rect.height;
            float offsetY = (normalizedPivotY - .5f) * size.y;
            Vector2 position = ToTransientPosition(worldPosition);
            position.y += offsetY;
            RectTransform rect;
            Image image;
            CombatVFXView view = CreateView(transientRoot, frames[0], type, size,
                position, direction, out rect, out image);
            view.BeginFrameTransient(type, frames, frameRate, duration, null);
            return true;
        }

        bool PlayHitImpact(Vector3 worldPosition, float direction, RectTransform anchorOverride = null)
        {
            return PlayFrameSequence(CombatVFXType.HitImpact, worldPosition,
                HitImpactDuration, HitImpactSize, direction, anchorOverride, HitImpactFrameRate);
        }

        public void PlayUltimate(BattleActor actor)
        {
            PlayAt(CombatVFXType.Ultimate, GetCharacter(actor)?.VFXAnchor, .68f, 0f);
        }

        public void PlayDash(BattleActor actor)
        {
            PlayAt(CombatVFXType.Dash, GetCharacter(actor)?.VFXAnchor, .24f, FacingSign(GetCharacter(actor)));
        }

        public void PlaySwordQiImpact(BattleActor actor)
        {
            CharacterView target = GetCharacter(actor);
            RectTransform targetAnchor = target != null ? target.VFXAnchor : null;
            RectTransform hitImpactAnchor = target != null && target.TorsoAnchor != null
                ? target.TorsoAnchor : targetAnchor;
            if (targetAnchor == null || transientRoot == null)
                return;

            PlayAt(CombatVFXType.HitImpact, hitImpactAnchor, TransientLifetime, FacingSign(target));

            Vector2 position = ToTransientPosition(targetAnchor.position);
            GameObject flashObj = new GameObject("SwordQiImpactFlash", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            RectTransform flashRect = flashObj.GetComponent<RectTransform>();
            flashRect.SetParent(transientRoot, false);
            flashRect.anchorMin = flashRect.anchorMax = new Vector2(0.5f, 0.5f);
            flashRect.pivot = new Vector2(0.5f, 0.5f);
            flashRect.sizeDelta = new Vector2(280f, 280f);
            flashRect.anchoredPosition = position;
            flashRect.localScale = Vector3.one * 0.6f;

            Image flashImage = flashObj.GetComponent<Image>();
            flashImage.sprite = GetOrCreateRadialFlashSprite();
            flashImage.color = Color.white;
            flashImage.raycastTarget = false;

            StartCoroutine(AnimateImpactFlash(flashRect, flashImage, 0.24f));
        }

        static IEnumerator AnimateImpactFlash(RectTransform flashRect, Image flashImage, float duration)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.05f, duration);
            while (elapsed < safeDuration && flashRect != null && flashImage != null)
            {
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f);
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float scale = Mathf.Lerp(0.6f, 1.45f, 1f - (1f - t) * (1f - t));
                flashRect.localScale = new Vector3(scale, scale, 1f);
                Color c = flashImage.color;
                c.a = 1f - t * t;
                flashImage.color = c;
                yield return null;
            }
            if (flashRect != null)
                Destroy(flashRect.gameObject);
        }

        static Sprite GetOrCreateRadialFlashSprite()
        {
            if (radialFlashSprite != null)
                return radialFlashSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float falloff = Mathf.Clamp01(1f - dist);
                    float alpha = falloff * falloff;
                    // Pure white core blending into bright cyan-blue halo.
                    Color pixel = Color.Lerp(new Color(0.55f, 0.90f, 1f, alpha),
                        new Color(1f, 1f, 1f, alpha), falloff);
                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply(false, true);
            radialFlashSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            return radialFlashSprite;
        }

        public void PlayAttack(BattleActor actor, bool critical, CombatVFXType preferredAttack,
            bool firstHit, bool shielded)
        {
            CharacterView attacker = GetCharacter(actor);
            CharacterView target = GetCharacter(Opponent(actor));
            RectTransform sourceAnchor = attacker != null ? attacker.VFXAnchor : null;
            RectTransform targetAnchor = target != null ? target.VFXAnchor : null;
            RectTransform hitImpactAnchor = target != null && target.TorsoAnchor != null
                ? target.TorsoAnchor : targetAnchor;
            if (attacker == null || target == null)
                return;

            if (preferredAttack == CombatVFXType.Fireball || preferredAttack == CombatVFXType.AirSlash)
            {
                CombatVFXType impactType = CombatVFXType.HitImpact;
                PlayProjectile(preferredAttack, sourceAnchor, hitImpactAnchor, impactType,
                    FacingSign(attacker));
                return;
            }

            if (firstHit && actor != BattleActor.Player)
                PlayAt(CombatVFXType.Slash, targetAnchor, .23f, FacingSign(attacker));
            if (hitImpactAnchor != null)
                PlayHitImpact(hitImpactAnchor.position, FacingSign(attacker), hitImpactAnchor);
            else if (target.VisualRect != null)
                PlayHitImpact(target.VisualRect.TransformPoint(target.VisualRect.rect.center), FacingSign(attacker));
        }

        public bool IsPersistentEffectActive(BattleActor actor, CombatVFXType type)
        {
            int typeIndex = (int)type;
            return persistentEffects != null && typeIndex >= 0 && typeIndex < persistentEffects.GetLength(1)
                && persistentEffects[(int)actor, typeIndex] != null;
        }

        public void SetRageReady(BattleActor actor, bool active)
        {
            SetPersistentEffect(actor, CombatVFXType.RageReady, active);
        }

        public void ApplyStatusEffect(BattleActor actor, CombatVFXType effect)
        {
            if (IsPersistentEffectActive(actor, effect))
                return;
            if (effect == CombatVFXType.Freeze)
                PlayAt(CombatVFXType.Freeze, GetCharacter(actor)?.VFXAnchor, .42f, 0f);
            SetPersistentEffect(actor, effect, true);
        }

        public void RemoveStatusEffect(BattleActor actor, CombatVFXType effect)
        {
            SetPersistentEffect(actor, effect, false);
        }

        public void ClearAllEffects()
        {
            if (persistentEffects != null)
            {
                for (int actorIndex = 0; actorIndex < persistentEffects.GetLength(0); actorIndex++)
                {
                    for (int typeIndex = 0; typeIndex < persistentEffects.GetLength(1); typeIndex++)
                    {
                        CombatVFXView effect = persistentEffects[actorIndex, typeIndex];
                        persistentEffects[actorIndex, typeIndex] = null;
                        if (effect != null)
                            Destroy(effect.gameObject);
                    }
                }
            }

            if (transientRoot == null)
                return;
            for (int i = transientRoot.childCount - 1; i >= 0; i--)
                Destroy(transientRoot.GetChild(i).gameObject);
        }

        public void SetPersistentEffect(BattleActor actor, CombatVFXType type, bool active)
        {
            if (!IsPersistentType(type) || persistentEffects == null)
            {
                if (!IsPersistentType(type))
                    Debug.LogWarning($"{type} is a transient CombatVFXType and cannot be activated persistently.", this);
                return;
            }

            int actorIndex = (int)actor;
            int typeIndex = (int)type;
            CombatVFXView existing = persistentEffects[actorIndex, typeIndex];
            if (active && existing != null)
                return;
            if (!active)
            {
                persistentEffects[actorIndex, typeIndex] = null;
                if (existing != null)
                    Destroy(existing.gameObject);
                return;
            }

            CharacterView character = GetCharacter(actor);
            RectTransform anchor = type == CombatVFXType.Stun && character != null
                ? character.HeadVFXAnchor
                : character != null ? character.PersistentVFXAnchor : null;
            if (anchor == null)
                return;

            bool hasFrames = TryGetFrames(type, out Sprite[] frames, false);
            Sprite sprite;
            if (hasFrames)
                sprite = frames[0];
            else if (!TryGetSprite(type, out sprite))
                return;

            Vector2 size = PersistentSize(type);
            Vector2 position = Vector2.zero;
            if (hasFrames)
            {
                float normalizedPivotY = frames[0].pivot.y / frames[0].rect.height;
                position.y = (normalizedPivotY - .5f) * size.y;
            }
            if (type == CombatVFXType.RageReady)
            {
                position.y += size.y * 0.5f;
            }
            else if (type == CombatVFXType.Shield)
            {
                position.y -= GetShieldDropOffset(actor, anchor);
            }

            RectTransform rect;
            Image image;
            CombatVFXView view = CreateView(anchor, sprite, type, size,
                position, 0f, out rect, out image);
            persistentEffects[actorIndex, typeIndex] = view;
            if (hasFrames)
            {
                float frameRate = type == CombatVFXType.Shield ? 12f : 30f;
                view.BeginPersistent(type, frames, frameRate);
            }
            else
                view.BeginPersistent(type);
        }

        void PlayProjectile(CombatVFXType type, RectTransform source, RectTransform target,
            CombatVFXType impactType, float direction)
        {
            if (source == null || target == null || transientRoot == null
                || !TryGetSprite(type, out Sprite sprite))
                return;

            Vector2 start = ToTransientPosition(source.position);
            Vector2 destination = ToTransientPosition(target.position);
            float duration = Mathf.Clamp(Vector2.Distance(start, destination) / 1600f, .25f, .45f);
            RectTransform rect;
            Image image;
            CombatVFXView view = CreateView(transientRoot, sprite, type,
                type == CombatVFXType.Fireball ? new Vector2(150f, 150f) : new Vector2(200f, 132f),
                start, direction, out rect, out image);
            view.BeginTransient(type, duration, destination, true, () =>
                PlayAtPosition(impactType, destination, TransientLifetime, direction));
        }

        void PlayAt(CombatVFXType type, RectTransform anchor, float duration, float direction)
        {
            if (type == CombatVFXType.HitImpact && anchor != null
                && PlayHitImpact(anchor.position, direction, anchor))
                return;
            if (anchor == null || transientRoot == null || !TryGetSprite(type, out Sprite sprite))
                return;

            bool followsAnchor = type == CombatVFXType.Dash;
            RectTransform parent = followsAnchor ? anchor : transientRoot;
            Vector2 position = followsAnchor ? Vector2.zero : ToTransientPosition(anchor.position);
            Vector2 size = type == CombatVFXType.Heal ? new Vector2(230f, 325f)
                : type == CombatVFXType.LifeSteal ? new Vector2(240f, 195f)
                : type == CombatVFXType.Ultimate ? new Vector2(260f, 260f)
                : type == CombatVFXType.Slash ? new Vector2(230f, 170f)
                : type == CombatVFXType.Dash ? new Vector2(240f, 120f)
                : type == CombatVFXType.ShieldHit ? new Vector2(190f, 190f)
                : type == CombatVFXType.CriticalHit ? new Vector2(220f, 220f)
                : new Vector2(180f, 180f);
            RectTransform rect;
            Image image;
            CombatVFXView view = CreateView(parent, sprite, type, size, position,
                direction, out rect, out image);
            view.BeginTransient(type, duration, position, false, null);
        }

        void PlayAtPosition(CombatVFXType type, Vector2 position, float duration, float direction)
        {
            if (type == CombatVFXType.HitImpact && transientRoot != null
                && PlayHitImpact(transientRoot.TransformPoint(position), direction))
                return;
            if (transientRoot == null || !TryGetSprite(type, out Sprite sprite))
                return;

            Vector2 size = type == CombatVFXType.ShieldHit ? new Vector2(190f, 190f)
                : type == CombatVFXType.CriticalHit ? new Vector2(220f, 220f)
                : new Vector2(180f, 180f);
            RectTransform rect;
            Image image;
            CombatVFXView view = CreateView(transientRoot, sprite, type, size,
                position, direction, out rect, out image);
            view.BeginTransient(type, duration, position, false, null);
        }

        CombatVFXView CreateView(Transform parent, Sprite sprite, CombatVFXType type,
            Vector2 size, Vector2 position, float direction, out RectTransform rect, out Image image)
        {
            GameObject effect = new GameObject(type.ToString(), typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CombatVFXView));
            rect = (RectTransform)effect.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            image = effect.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            CombatVFXView view = effect.GetComponent<CombatVFXView>();
            view.Configure(sprite, size, position, direction);
            return view;
        }

        bool TryGetSprite(CombatVFXType type, out Sprite sprite)
        {
            sprite = catalog != null ? catalog.GetSprite(type) : null;
            if (sprite != null)
                return true;

            LogMissingSprite(type);
            return false;
        }

        bool TryGetFrames(CombatVFXType type, out Sprite[] frames)
        {
            return TryGetFrames(type, out frames, true);
        }

        bool TryGetFrames(CombatVFXType type, out Sprite[] frames, bool logMissing)
        {
            frames = catalog != null ? catalog.GetFrames(type) : null;
            if (frames != null && frames.Length > 0 && frames[0] != null)
                return true;

            if (logMissing)
                LogMissingSprite(type);
            return false;
        }

        void LogMissingSprite(CombatVFXType type)
        {
            int index = (int)type;
            if (index >= 0 && index < missingSpritesLogged.Length && !missingSpritesLogged[index])
            {
                missingSpritesLogged[index] = true;
                Debug.LogWarning($"Combat VFX sprite is missing for {type}; skipping this visual effect.", this);
            }
        }

        Vector2 ToTransientPosition(Vector3 worldPosition)
        {
            return transientRoot.InverseTransformPoint(worldPosition);
        }

        CharacterView GetCharacter(BattleActor actor) => actor == BattleActor.Player ? hero : boss;

        static BattleActor Opponent(BattleActor actor) =>
            actor == BattleActor.Player ? BattleActor.Boss : BattleActor.Player;

        static float FacingSign(CharacterView character)
        {
            if (character == null)
                return 1f;
            return character.FacingDirection == CharacterFacing.Right ? 1f : -1f;
        }

        static float GetShieldDropOffset(BattleActor actor, RectTransform anchor)
        {
            if (actor == BattleActor.Player)
                return HeroShieldDropOffset;

            float extraAnchorDrop = 0f;
            if (anchor != null)
            {
                RectTransform parentRect = anchor.parent as RectTransform;
                float rootHeight = parentRect != null && parentRect.rect.height > 1f
                    ? parentRect.rect.height
                    : FallbackCharacterRootHeight;
                extraAnchorDrop = (anchor.anchorMin.y - DefaultShieldAnchorY) * rootHeight;
            }
            return BossShieldBaseDropOffset + extraAnchorDrop;
        }

        static Vector2 PersistentSize(CombatVFXType type)
        {
            switch (type)
            {
                case CombatVFXType.Shield: return new Vector2(460f, 460f);
                case CombatVFXType.Stun: return new Vector2(145f, 100f);
                case CombatVFXType.Burn: return new Vector2(210f, 225f);
                case CombatVFXType.Freeze: return new Vector2(215f, 235f);
                case CombatVFXType.Poison: return new Vector2(205f, 220f);
                case CombatVFXType.RageReady: return new Vector2(320f, 360f);
                default: return new Vector2(170f, 170f);
            }
        }

        static bool IsPersistentType(CombatVFXType type) =>
            type == CombatVFXType.Shield || type == CombatVFXType.Stun
            || type == CombatVFXType.Burn || type == CombatVFXType.Freeze
            || type == CombatVFXType.Poison || type == CombatVFXType.RageReady;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Development/Play All Combat VFX")]
        void DebugPlayAllCombatVfx()
        {
            StartCoroutine(DebugShowcase());
        }

        IEnumerator DebugShowcase()
        {
            CombatVFXType[] transientTypes =
            {
                CombatVFXType.Slash, CombatVFXType.Fireball, CombatVFXType.AirSlash,
                CombatVFXType.Dash, CombatVFXType.Heal, CombatVFXType.HitImpact,
                CombatVFXType.HitPhysical, CombatVFXType.CriticalHit, CombatVFXType.Ultimate,
                CombatVFXType.LifeSteal
            };
            foreach (CombatVFXType type in transientTypes)
            {
                if (type == CombatVFXType.Fireball || type == CombatVFXType.AirSlash)
                    PlayProjectile(type, hero != null ? hero.VFXAnchor : null,
                        boss != null ? boss.VFXAnchor : null, CombatVFXType.HitImpact, 1f);
                else if (type == CombatVFXType.HitImpact)
                    PlayAt(type, boss != null ? (boss.TorsoAnchor != null ? boss.TorsoAnchor : boss.VFXAnchor) : null, TransientLifetime, 1f);
                else if (type == CombatVFXType.ShieldHit
                    || type == CombatVFXType.HitPhysical || type == CombatVFXType.CriticalHit)
                    PlayAt(type, boss != null ? boss.VFXAnchor : null, TransientLifetime, 1f);
                else if (type == CombatVFXType.LifeSteal)
                    PlayLifeSteal(BattleActor.Player);
                else
                    PlayAt(type, hero != null ? hero.VFXAnchor : null,
                        type == CombatVFXType.Heal ? 2.4f : TransientLifetime, 1f);
                yield return new WaitForSecondsRealtime(.18f);
            }

            CombatVFXType[] persistentTypes =
            {
                CombatVFXType.Shield, CombatVFXType.Stun, CombatVFXType.Burn,
                CombatVFXType.Freeze, CombatVFXType.Poison, CombatVFXType.RageReady
            };
            foreach (CombatVFXType type in persistentTypes)
                SetPersistentEffect(BattleActor.Boss, type, true);
            yield return new WaitForSecondsRealtime(2f);
            foreach (CombatVFXType type in persistentTypes)
                SetPersistentEffect(BattleActor.Boss, type, false);
        }
#endif
    }
}
