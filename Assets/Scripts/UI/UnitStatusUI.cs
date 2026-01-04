using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays status for a single unit (name, HP, status effects).
    /// </summary>
    public class UnitStatusUI : MonoBehaviour
    {
        private Unit unit;
        private Text nameText;
        private Text hpText;
        private Image hpBarFill;
        private Image background;

        public void Initialize(Unit targetUnit)
        {
            unit = targetUnit;
            CreateUI();
        }

        private void CreateUI()
        {
            // Background panel
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);
            background = bgObj.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(200, 60);

            // Unit name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);
            nameText = nameObj.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 16;
            nameText.alignment = TextAnchor.UpperLeft;
            nameText.color = Color.white;
            nameText.text = unit.UnitName;

            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.anchoredPosition = new Vector2(10, -5);
            nameRect.sizeDelta = new Vector2(-20, 20);

            // HP bar background
            GameObject hpBarBgObj = new GameObject("HPBarBg");
            hpBarBgObj.transform.SetParent(transform, false);
            Image hpBarBg = hpBarBgObj.AddComponent<Image>();
            hpBarBg.color = new Color(0.3f, 0, 0, 1f);

            RectTransform hpBarBgRect = hpBarBgObj.GetComponent<RectTransform>();
            hpBarBgRect.anchorMin = new Vector2(0, 0);
            hpBarBgRect.anchorMax = new Vector2(1, 0);
            hpBarBgRect.pivot = new Vector2(0, 0);
            hpBarBgRect.anchoredPosition = new Vector2(10, 10);
            hpBarBgRect.sizeDelta = new Vector2(-20, 15);

            // HP bar fill
            GameObject hpBarFillObj = new GameObject("HPBarFill");
            hpBarFillObj.transform.SetParent(hpBarBgObj.transform, false);
            hpBarFill = hpBarFillObj.AddComponent<Image>();
            hpBarFill.color = Color.green;

            RectTransform hpBarFillRect = hpBarFillObj.GetComponent<RectTransform>();
            hpBarFillRect.anchorMin = Vector2.zero;
            hpBarFillRect.anchorMax = Vector2.zero;
            hpBarFillRect.pivot = Vector2.zero;
            hpBarFillRect.anchoredPosition = Vector2.zero;
            hpBarFillRect.sizeDelta = new Vector2(180, 15);

            // HP text
            GameObject hpTextObj = new GameObject("HPText");
            hpTextObj.transform.SetParent(transform, false);
            hpText = hpTextObj.AddComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 14;
            hpText.alignment = TextAnchor.MiddleLeft;
            hpText.color = Color.white;

            RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
            hpTextRect.anchorMin = new Vector2(0, 0);
            hpTextRect.anchorMax = new Vector2(1, 1);
            hpTextRect.pivot = new Vector2(0.5f, 0.5f);
            hpTextRect.anchoredPosition = new Vector2(0, -5);
            hpTextRect.sizeDelta = Vector2.zero;
        }

        public void UpdateDisplay()
        {
            if (unit == null) return;

            // Update HP text
            hpText.text = $"  {unit.CurrentHP}/{unit.MaxHP} HP";

            // Update HP bar
            float hpPercent = (float)unit.CurrentHP / unit.MaxHP;
            RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(180 * hpPercent, 15);

            // Update HP bar color
            if (hpPercent > 0.5f)
                hpBarFill.color = Color.green;
            else if (hpPercent > 0.25f)
                hpBarFill.color = Color.yellow;
            else
                hpBarFill.color = Color.red;

            // Gray out if dead
            if (!unit.IsAlive)
            {
                nameText.color = Color.gray;
                hpText.color = Color.gray;
            }
        }

        public void SetHighlight(bool highlighted)
        {
            background.color = highlighted ?
                new Color(0.4f, 0.4f, 0.6f, 0.9f) :
                new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }
    }
}