using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.UI
{
    public sealed class BattleHUD : MonoBehaviour
    {
        [SerializeField] Slider heroHp, bossHp, mana, ultimate;
        // TODO: Bind these optional bars when the HUD has Soul HP slots; do not generate a new layout here.
        [SerializeField] Slider heroSoulHp, bossSoulHp;
        [SerializeField] Slider heroRage, bossRage;
        [SerializeField] TMP_Text heroHpText, bossHpText, heroRageText, bossRageText, manaText, ultimateText;
        static readonly Color RageColor = new Color32(242, 112, 43, 255);
        static readonly Color RageReadyColor = new Color32(255, 190, 62, 255);

        public void Configure(Slider heroHealth, Slider bossHealth, Slider manaBar, Slider ultimateBar,
            TMP_Text heroValue, TMP_Text bossValue, TMP_Text manaValue, TMP_Text ultimateValue)
        {
            Configure(heroHealth, bossHealth, null, null, manaBar, ultimateBar,
                heroValue, bossValue, null, null, manaValue, ultimateValue);
        }

        public void Configure(Slider heroHealth, Slider bossHealth, Slider heroRageBar, Slider bossRageBar,
            Slider manaBar, Slider ultimateBar, TMP_Text heroValue, TMP_Text bossValue, TMP_Text heroRageValue,
            TMP_Text bossRageValue, TMP_Text manaValue, TMP_Text ultimateValue)
        {
            heroHp = heroHealth;
            bossHp = bossHealth;
            heroRage = heroRageBar;
            bossRage = bossRageBar;
            mana = manaBar;
            ultimate = ultimateBar;
            heroHpText = heroValue;
            bossHpText = bossValue;
            heroRageText = heroRageValue;
            bossRageText = bossRageValue;
            manaText = manaValue;
            ultimateText = ultimateValue;
        }

        public void Render(int hero, int heroMax, int boss, int bossMax, int manaValue, int ultimateValue)
        {
            Render(hero, heroMax, boss, bossMax, 0, 0, 100, manaValue, ultimateValue);
        }

        public void Render(int hero, int heroMax, int boss, int bossMax, int heroRageValue,
            int bossRageValue, int maxRage, int manaValue, int ultimateValue)
        {
            Render(hero, heroMax, boss, bossMax, heroRageValue, bossRageValue,
                maxRage, maxRage, manaValue, ultimateValue);
        }

        public void Render(int hero, int heroMax, int boss, int bossMax, int heroRageValue,
            int bossRageValue, int heroMaxRage, int bossMaxRage, int manaValue, int ultimateValue)
        {
            Render(hero, heroMax, boss, bossMax, heroRageValue, bossRageValue,
                heroMaxRage, bossMaxRage, manaValue, ultimateValue, 0, 0, 0, 0, false);
        }

        public void Render(int hero, int heroMax, int boss, int bossMax, int heroRageValue,
            int bossRageValue, int heroMaxRage, int bossMaxRage, int manaValue, int ultimateValue,
            int heroSoulValue, int heroSoulMax, int bossSoulValue, int bossSoulMax, bool soulEnabled)
        {
            Set(heroHp, hero, heroMax);
            Set(bossHp, boss, bossMax);
            SetSoul(heroSoulHp, heroSoulValue, heroSoulMax, soulEnabled);
            SetSoul(bossSoulHp, bossSoulValue, bossSoulMax, soulEnabled);
            Set(mana, manaValue, 100);
            Set(ultimate, ultimateValue, 100);
            SetRage(heroRage, heroRageText, heroRageValue, heroMaxRage);
            SetRage(bossRage, bossRageText, bossRageValue, bossMaxRage);
            int safeHeroMax = Mathf.Max(0, heroMax);
            int safeBossMax = Mathf.Max(0, bossMax);
            SetText(heroHpText, $"{(safeHeroMax > 0 ? Mathf.Clamp(hero, 0, safeHeroMax) : 0)} / {safeHeroMax}");
            SetText(bossHpText, $"{(safeBossMax > 0 ? Mathf.Clamp(boss, 0, safeBossMax) : 0)} / {safeBossMax}");
            SetText(manaText, $"{manaValue} / 100");
            SetText(ultimateText, $"{ultimateValue} / 100");
        }

        static void Set(Slider slider, int value, int max)
        {
            if (slider == null) return;
            max = Mathf.Max(0, max);
            slider.maxValue = max;
            slider.value = max > 0 ? Mathf.Clamp(value, 0, max) : 0;
        }

        static void SetSoul(Slider slider, int value, int max, bool enabled)
        {
            if (slider == null) return;
            if (slider.gameObject.activeSelf != enabled)
                slider.gameObject.SetActive(enabled);
            if (enabled)
                Set(slider, value, max);
        }

        static void SetRage(Slider slider, TMP_Text text, int value, int max)
        {
            max = Mathf.Max(0, max);
            value = max > 0 ? Mathf.Clamp(value, 0, max) : 0;
            Set(slider, value, max);
            SetText(text, max > 0 && value >= max ? "RAGE MAX" : $"{value} / {max}");
            if (slider != null && slider.fillRect != null)
            {
                Image fill = slider.fillRect.GetComponent<Image>();
                if (fill != null)
                {
                    fill.enabled = true;
                    fill.color = max > 0 && value >= max ? RageReadyColor : RageColor;
                }
            }
        }

        static void SetText(TMP_Text text, string value) { if (text != null) text.text = value; }
    }
}
