using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Match3RPG.Battle
{
    public enum FloatingTextType
    {
        Damage,
        Critical,
        CriticalReady,
        Heal,
        Mana,
        ManaLost,
        RageLost,
        Shield,
        ShieldLost,
        Experience,
        Gold,
        Ultimate,
        UltimateSpent,
        Rage,
        SoulDamage,
        SoulCritical
    }

    public sealed class FloatingTextManager : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float lifetime = .85f;
        [SerializeField, Min(0f)] float riseDistance = 48f;
        [SerializeField, Min(0f)] float stackSpacing = 30f;
        [SerializeField] TMP_FontAsset fontAsset;

        readonly Stack<TextMeshProUGUI> pool = new Stack<TextMeshProUGUI>();
        readonly List<ActiveText> activeTexts = new List<ActiveText>();
        RectTransform overlayRect;

        sealed class ActiveText
        {
            public CharacterView Owner;
            public TextMeshProUGUI Text;
        }

        public void Configure(TMP_FontAsset configuredFont = null)
        {
            overlayRect = (RectTransform)transform;
            if (configuredFont != null) fontAsset = configuredFont;
            if (fontAsset == null) fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset == null)
            {
                Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtinFont != null) fontAsset = TMP_FontAsset.CreateFontAsset(builtinFont);
            }

            if (fontAsset == null)
                Debug.LogError("FloatingTextManager requires a TextMeshPro font asset or an available runtime font.", this);
        }

        public void Show(CharacterView owner, FloatingTextType type, int amount)
        {
            if (amount <= 0) return;
            if (overlayRect == null) Configure();
            if (fontAsset == null || owner == null || owner.FloatingTextAnchor == null)
            {
                Debug.LogError("Floating text requires a font asset, character, and character anchor.", this);
                return;
            }

            int stackIndex = 0;
            for (int i = 0; i < activeTexts.Count; i++)
                if (activeTexts[i].Owner == owner)
                    stackIndex++;

            TextMeshProUGUI text = pool.Count > 0 ? pool.Pop() : CreateText();
            text.text = Format(type, amount);
            text.color = ColorFor(type);
            text.alpha = 1f;
            text.gameObject.SetActive(true);

            Vector3 worldPosition = owner.FloatingTextAnchor.position;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPosition, null,
                    out Vector2 localPosition))
            {
                Debug.LogError("Unable to convert the character anchor to floating-text coordinates.", this);
                text.gameObject.SetActive(false);
                pool.Push(text);
                return;
            }

            text.rectTransform.anchoredPosition = localPosition + Vector2.up * (stackIndex * stackSpacing);
            ActiveText active = new ActiveText { Owner = owner, Text = text };
            activeTexts.Add(active);
            StartCoroutine(Animate(active, text.rectTransform.anchoredPosition, text.color));
        }

        public void ClearActiveTexts()
        {
            StopAllCoroutines();
            foreach (ActiveText active in activeTexts)
                if (active.Text != null)
                    active.Text.gameObject.SetActive(false);
            activeTexts.Clear();
        }

        TextMeshProUGUI CreateText()
        {
            GameObject textObject = new GameObject("FloatingText", typeof(RectTransform), typeof(CanvasRenderer));
            textObject.transform.SetParent(overlayRect, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(280f, 56f);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.fontSize = 32f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.gameObject.SetActive(false);
            return text;
        }

        IEnumerator Animate(ActiveText active, Vector2 startPosition, Color color)
        {
            float duration = Mathf.Max(.1f, lifetime);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = progress * (2f - progress);
                active.Text.rectTransform.anchoredPosition = startPosition + Vector2.up * (riseDistance * easedProgress);
                active.Text.color = new Color(color.r, color.g, color.b, 1f - progress);
                yield return null;
            }

            activeTexts.Remove(active);
            active.Text.gameObject.SetActive(false);
            pool.Push(active.Text);
        }

        static string Format(FloatingTextType type, int amount)
        {
            switch (type)
            {
                case FloatingTextType.Damage: return $"-{amount} HP";
                case FloatingTextType.Critical: return $"-{amount} HP!";
                case FloatingTextType.SoulDamage: return $"-{amount} SOUL";
                case FloatingTextType.SoulCritical: return $"-{amount} SOUL!";
                case FloatingTextType.CriticalReady: return "CRITICAL!";
                case FloatingTextType.Heal: return $"+{amount} HP";
                case FloatingTextType.Mana: return $"+{amount} MANA";
                case FloatingTextType.ManaLost: return $"-{amount} MANA";
                case FloatingTextType.RageLost: return $"-{amount} RAGE";
                case FloatingTextType.Shield: return $"+{amount} SHIELD";
                case FloatingTextType.ShieldLost: return $"-{amount} SHIELD";
                case FloatingTextType.Experience: return $"+{amount} EXP";
                case FloatingTextType.Gold: return $"+{amount} GOLD";
                case FloatingTextType.Ultimate: return $"+{amount} ULT";
                case FloatingTextType.UltimateSpent: return $"-{amount} ULT";
                case FloatingTextType.Rage: return $"+{amount} RAGE";
                default: return amount.ToString();
            }
        }

        static Color ColorFor(FloatingTextType type)
        {
            switch (type)
            {
                case FloatingTextType.Damage: return new Color32(255, 104, 90, 255);
                case FloatingTextType.Critical: return new Color32(255, 211, 72, 255);
                case FloatingTextType.SoulDamage: return new Color32(242, 242, 255, 255);
                case FloatingTextType.SoulCritical: return new Color32(255, 255, 255, 255);
                case FloatingTextType.CriticalReady: return new Color32(255, 211, 72, 255);
                case FloatingTextType.Heal: return new Color32(104, 235, 143, 255);
                case FloatingTextType.Mana: return new Color32(100, 190, 255, 255);
                case FloatingTextType.ManaLost: return new Color32(130, 198, 226, 255);
                case FloatingTextType.RageLost: return new Color32(190, 160, 150, 255);
                case FloatingTextType.Shield:
                case FloatingTextType.ShieldLost: return new Color32(112, 218, 238, 255);
                case FloatingTextType.Experience: return new Color32(124, 230, 131, 255);
                case FloatingTextType.Gold: return new Color32(255, 214, 93, 255);
                case FloatingTextType.Ultimate: return new Color32(203, 137, 255, 255);
                case FloatingTextType.UltimateSpent: return new Color32(155, 112, 205, 255);
                case FloatingTextType.Rage: return new Color32(255, 150, 55, 255);
                default: return Color.white;
            }
        }
    }
}
