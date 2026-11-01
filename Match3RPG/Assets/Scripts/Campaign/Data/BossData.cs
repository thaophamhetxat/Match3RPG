using System;
using Match3RPG.Battle;
using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Campaign.Data
{
    [Flags]
    public enum BossStatusEffectCapability
    {
        None = 0,
        Poison = 1 << 0,
        Burn = 1 << 1,
        Freeze = 1 << 2,
        Stun = 1 << 3,
        Weaken = 1 << 4
    }

    [CreateAssetMenu(menuName = "Match3 RPG/Campaign/Boss Data")]
    public sealed class BossData : ScriptableObject
    {
        [SerializeField] string bossId;
        [SerializeField] string displayName;
        [SerializeField] Sprite portrait;
        [SerializeField] CharacterAnimationSet animationSet;
        [SerializeField, Min(1)] int maxHP = 1000;
        [SerializeField, Min(0)] int attack = 20;
        [SerializeField, Min(1)] int maxRage = 100;
        [SerializeField, Min(0)] int startingRage;
        [SerializeField, Min(0)] int ragePerOrangeGem = 5;
        [SerializeField, Min(0)] int ragePerReceivedHit = 1;
        [SerializeField] bool criticalEnabled = true;
        [SerializeField, Min(1f)] float criticalDamageMultiplier = 2f;
        [SerializeField] BossStatusEffectCapability statusEffectCapabilities;
        [SerializeField] CombatVFXType preferredAttackVfx = CombatVFXType.Slash;
        [SerializeField] string defaultLootTableId;
        [SerializeField, Min(0.1f)] float visualScale = 1f;
        [SerializeField] Vector2 visualOffset;
        [SerializeField] CharacterFacing sourceFacing = CharacterFacing.Right;
        [SerializeField] Vector2 vfxAnchor = new Vector2(.5f, .48f);
        [SerializeField] Vector2 headVfxAnchor = new Vector2(.5f, .82f);
        [SerializeField] Vector2 floatingTextAnchor = new Vector2(.5f, .88f);

        public string BossId => bossId;
        public string StableId => bossId;
        public bool HasValidStableId => StableIdRules.IsValid(bossId);
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public CharacterAnimationSet AnimationSet => animationSet;
        public int MaxHP => maxHP;
        public int Attack => attack;
        public int MaxRage => maxRage;
        public int StartingRage => startingRage;
        public int RagePerOrangeGem => ragePerOrangeGem;
        public int RagePerReceivedHit => ragePerReceivedHit;
        public bool CriticalEnabled => criticalEnabled;
        public float CriticalDamageMultiplier => criticalDamageMultiplier;
        public BossStatusEffectCapability StatusEffectCapabilities => statusEffectCapabilities;
        public CombatVFXType PreferredAttackVfx => preferredAttackVfx;
        public string DefaultLootTableId => defaultLootTableId;
        public float VisualScale => visualScale;
        public Vector2 VisualOffset => visualOffset;
        public CharacterFacing SourceFacing => sourceFacing;
        public Vector2 VfxAnchor => vfxAnchor;
        public Vector2 HeadVfxAnchor => headVfxAnchor;
        public Vector2 FloatingTextAnchor => floatingTextAnchor;

        public void Configure(string id, string title, Sprite bossPortrait, int maximumHealth,
            int attackDamage, int rageMaximum, int initialRage, int orangeRageGain,
            int receivedHitRageGain, bool canCritical, float criticalMultiplier,
            BossStatusEffectCapability effectCapabilities,
            CombatVFXType preferredAttackVfx = CombatVFXType.Slash,
            float visualScale = 1f, Vector2 visualOffset = default,
            CharacterFacing sourceFacing = CharacterFacing.Right,
            CharacterAnimationSet characterAnimationSet = null)
        {
            bossId = id;
            displayName = title;
            portrait = bossPortrait;
            maxHP = Mathf.Max(1, maximumHealth);
            attack = Mathf.Max(0, attackDamage);
            maxRage = Mathf.Max(1, rageMaximum);
            startingRage = Mathf.Clamp(initialRage, 0, maxRage);
            ragePerOrangeGem = Mathf.Max(0, orangeRageGain);
            ragePerReceivedHit = Mathf.Max(0, receivedHitRageGain);
            criticalEnabled = canCritical;
            criticalDamageMultiplier = Mathf.Max(1f, criticalMultiplier);
            statusEffectCapabilities = effectCapabilities;
            this.preferredAttackVfx = preferredAttackVfx;
            this.visualScale = Mathf.Max(.1f, visualScale);
            this.visualOffset = visualOffset;
            this.sourceFacing = sourceFacing;
            animationSet = characterAnimationSet;
        }

        public void SetAnimationSet(CharacterAnimationSet characterAnimationSet)
        {
            animationSet = characterAnimationSet;
        }

        public void SetDefaultLootTableId(string stableId)
        {
            defaultLootTableId = stableId;
        }
    }
}
