using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.UI
{
    public sealed class BattleHUD : MonoBehaviour
    {
        [SerializeField] Slider heroHp, bossHp, mana, ultimate;
        [SerializeField] Slider heroSoulHp, bossSoulHp;
        [SerializeField] Slider heroRage, bossRage;
        [SerializeField] TMP_Text heroHpText, bossHpText, heroRageText, bossRageText, manaText, ultimateText;
        [SerializeField] TMP_Text heroHpSegmentText, bossHpSegmentText;
        const int HpSegmentSize = 10000;
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
            Configure(heroHealth, bossHealth, null, null, heroRageBar, bossRageBar,
                manaBar, ultimateBar, heroValue, bossValue, heroRageValue, bossRageValue,
                manaValue, ultimateValue);
        }

        public void Configure(Slider heroHealth, Slider bossHealth, Slider heroSoulHealth,
            Slider bossSoulHealth, Slider heroRageBar, Slider bossRageBar,
            Slider manaBar, Slider ultimateBar, TMP_Text heroValue, TMP_Text bossValue,
            TMP_Text heroRageValue, TMP_Text bossRageValue, TMP_Text manaValue,
            TMP_Text ultimateValue)
        {
            Configure(heroHealth, bossHealth, heroSoulHealth, bossSoulHealth,
                heroRageBar, bossRageBar, manaBar, ultimateBar, heroValue, bossValue,
                heroRageValue, bossRageValue, manaValue, ultimateValue, null, null);
        }

        public void Configure(Slider heroHealth, Slider bossHealth, Slider heroSoulHealth,
            Slider bossSoulHealth, Slider heroRageBar, Slider bossRageBar,
            Slider manaBar, Slider ultimateBar, TMP_Text heroValue, TMP_Text bossValue,
            TMP_Text heroRageValue, TMP_Text bossRageValue, TMP_Text manaValue,
            TMP_Text ultimateValue, TMP_Text heroSegmentValue, TMP_Text bossSegmentValue)
        {
            heroHp = heroHealth;
            bossHp = bossHealth;
            heroSoulHp = heroSoulHealth;
            bossSoulHp = bossSoulHealth;
            heroRage = heroRageBar;
            bossRage = bossRageBar;
            mana = manaBar;
            ultimate = ultimateBar;
            heroHpText = heroValue;
            bossHpText = bossValue;
            heroHpSegmentText = heroSegmentValue;
            bossHpSegmentText = bossSegmentValue;
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
            SetHealth(heroHp, hero, heroMax, heroHpText, heroHpSegmentText);
            SetHealth(bossHp, boss, bossMax, bossHpText, bossHpSegmentText);
            SetSoul(heroSoulHp, heroSoulValue, heroSoulMax);
            SetSoul(bossSoulHp, bossSoulValue, bossSoulMax);
            Set(mana, manaValue, 100);
            Set(ultimate, ultimateValue, 100);
            SetRage(heroRage, heroRageText, heroRageValue, heroMaxRage);
            SetRage(bossRage, bossRageText, bossRageValue, bossMaxRage);
            SetText(manaText, $"{manaValue} / 100");
            SetText(ultimateText, $"{ultimateValue} / 100");
        }

        static void SetHealth(Slider slider, int remaining, int max, TMP_Text valueText,
            TMP_Text segmentText)
        {
            int safeMax = Mathf.Max(0, max);
            int safeRemaining = safeMax > 0 ? Mathf.Clamp(remaining, 0, safeMax) : 0;
            bool split = safeMax > HpSegmentSize;
            int segmentCount = split ? Mathf.FloorToInt(safeRemaining / (float)HpSegmentSize) : 0;
            int segmentValue = safeRemaining % HpSegmentSize;
            if (segmentValue == 0 && safeRemaining > 0)
                segmentValue = HpSegmentSize;
            int segmentMax = split ? HpSegmentSize : safeMax;
            int displayedValue = split ? segmentValue : safeRemaining;

            Set(slider, displayedValue, segmentMax);
            SetText(valueText, $"{displayedValue} / {segmentMax}");
            SetText(segmentText, segmentCount.ToString());
        }

        static void Set(Slider slider, int value, int max)
        {
            if (slider == null) return;
            max = Mathf.Max(0, max);
            slider.maxValue = max;
            float clamped = max > 0 ? Mathf.Clamp(value, 0, max) : 0f;
            slider.value = clamped;

            if (slider.fillRect != null)
            {
                Image fill = slider.fillRect.GetComponent<Image>();
                if (fill != null)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = slider.direction == Slider.Direction.RightToLeft
                        ? (int)Image.OriginHorizontal.Right
                        : (int)Image.OriginHorizontal.Left;
                    fill.fillAmount = max > 0 ? clamped / max : 0f;
                }
            }
            else
            {
                Image fill = slider.GetComponentInChildren<Image>(true);
                if (fill != null && fill.transform != slider.transform)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = slider.direction == Slider.Direction.RightToLeft
                        ? (int)Image.OriginHorizontal.Right
                        : (int)Image.OriginHorizontal.Left;
                    fill.fillAmount = max > 0 ? clamped / max : 0f;
                }
            }
        }

        static void SetSoul(Slider slider, int value, int max)
        {
            if (slider == null) return;
            if (!slider.gameObject.activeSelf)
                slider.gameObject.SetActive(true);
            Set(slider, value, max);
        }

        static void SetRage(Slider slider, TMP_Text text, int value, int max)
        {
            max = Mathf.Max(0, max);
            value = max > 0 ? Mathf.Clamp(value, 0, max) : 0;
            Set(slider, value, max);
            SetText(text, $"{value} / {max}");
        }

        static void SetText(TMP_Text text, string value) { if (text != null) text.text = value; }
    }
}
