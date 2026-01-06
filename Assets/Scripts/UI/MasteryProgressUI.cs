using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;
using Riftbourne.Skills;
using System.Collections.Generic;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays skill mastery progress for a unit in screen-space UI.
    /// Shows "Skill Name: X/Y" for skills being learned.
    /// </summary>
    public class MasteryProgressUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Unit trackedUnit;

        [Header("UI Settings")]
        [SerializeField] private Vector2 screenPosition = new Vector2(10, 10); // Top-left corner

        private Canvas canvas;
        private GameObject panel;
        private Text progressText;

        private void Start()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            // Find or create main canvas
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MasteryCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create panel for mastery progress
            panel = new GameObject("MasteryProgressPanel");
            panel.transform.SetParent(canvas.transform);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1); // Top-left anchor
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = screenPosition;
            panelRect.sizeDelta = new Vector2(250, 100);

            // Add background
            Image bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);

            // Create text
            GameObject textObj = new GameObject("ProgressText");
            textObj.transform.SetParent(panel.transform);

            progressText = textObj.AddComponent<Text>();
            progressText.fontSize = 14;
            progressText.alignment = TextAnchor.UpperLeft;
            progressText.color = Color.white;

            // Use built-in font
            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtinFont != null)
            {
                progressText.font = builtinFont;
            }

            RectTransform textRect = progressText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20, -20); // 10px margin on all sides
            textRect.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            UpdateProgressText();
        }

        private void UpdateProgressText()
        {
            if (trackedUnit == null || progressText == null) return;

            // Get all available skills
            List<Skill> availableSkills = trackedUnit.GetAvailableSkills();

            if (availableSkills.Count == 0)
            {
                progressText.text = "No skills available";
                progressText.color = Color.gray;
                return;
            }

            string displayText = $"SKILL PROGRESS (SP: {trackedUnit.SkillPoints}):\n\n";
            Color textColor = Color.white;

            foreach (var skill in availableSkills)
            {
                bool isMastered = trackedUnit.IsSkillMastered(skill);

                if (isMastered)
                {
                    displayText += $"✓ {skill.SkillName}: MASTERED!\n";
                    textColor = Color.green; // If any skill is mastered, make whole panel green
                }
                else
                {
                    displayText += $"{skill.SkillName}: {skill.MasteryCost} SP to master\n";
                    if (textColor != Color.green) // Keep green if something is mastered
                    {
                        textColor = Color.cyan;
                    }
                }
            }

            progressText.text = displayText.TrimEnd();
            progressText.color = textColor;
        }
    }
}