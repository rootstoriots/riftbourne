using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;
using Riftbourne.Combat;
using Riftbourne.Core;
using System.Collections;
using System.Collections.Generic;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays turn order at top-right with unit portraits.
    /// Highlights the current unit.
    /// </summary>
    public class TurnOrderUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnManager turnManager;

        [Header("Layout")]
        [SerializeField] private float portraitSize = 80f;
        [SerializeField] private float portraitSpacing = 10f;

        private List<TurnOrderPortrait> portraits = new List<TurnOrderPortrait>();

        private class TurnOrderPortrait
        {
            public Unit unit;
            public GameObject portraitObject;
            public Image portraitImage;
            public Image highlightBorder;
            public Text nameText;
            public Image hpBarBackground;
            public Image hpBarFill;
        }

        private void Start()
        {
            Debug.Log("TurnOrderUI: Start() called");

            if (turnManager == null)
            {
                Debug.Log("TurnOrderUI: TurnManager was null, finding it...");
                turnManager = FindFirstObjectByType<TurnManager>();
            }

            if (turnManager == null)
            {
                Debug.LogError("TurnOrderUI: Could not find TurnManager!");
                return;
            }

            Debug.Log($"TurnOrderUI: TurnManager found: {turnManager.name}");

            // Wait one frame for TurnManager to initialize its units list
            StartCoroutine(CreateTurnOrderDelayed());
        }

        private IEnumerator CreateTurnOrderDelayed()
        {
            Debug.Log("TurnOrderUI: Waiting one frame for TurnManager to initialize...");
            yield return null; // Wait one frame

            CreateTurnOrderDisplay();
        }

        private void Update()
        {
            UpdateHighlights();
        }

        private void CreateTurnOrderDisplay()
        {
            Debug.Log("TurnOrderUI: CreateTurnOrderDisplay() called");

            if (turnManager == null)
            {
                Debug.LogError("TurnOrderUI: TurnManager is null in CreateTurnOrderDisplay!");
                return;
            }

            List<Unit> units = turnManager.GetAllUnits();
            Debug.Log($"TurnOrderUI: Got {units.Count} units from TurnManager");

            // Auto-scale portrait size based on unit count
            float scaledPortraitSize = portraitSize;
            if (units.Count > 10)
            {
                // Shrink portraits for large battles
                scaledPortraitSize = Mathf.Max(50f, portraitSize * (10f / units.Count));
                Debug.Log($"TurnOrderUI: Scaled portrait size from {portraitSize} to {scaledPortraitSize} for {units.Count} units");
            }

            // Calculate required width and adjust parent RectTransform
            float requiredWidth = units.Count * (scaledPortraitSize + portraitSpacing);
            RectTransform parentRect = GetComponent<RectTransform>();
            if (parentRect != null)
            {
                parentRect.sizeDelta = new Vector2(requiredWidth, parentRect.sizeDelta.y);
                Debug.Log($"TurnOrderUI: Adjusted panel width to {requiredWidth}px");
            }

            for (int i = 0; i < units.Count; i++)
            {
                Debug.Log($"TurnOrderUI: Creating portrait {i} for {units[i].UnitName}");
                CreatePortrait(units[i], i, scaledPortraitSize);
            }

            Debug.Log($"TurnOrderUI: Created {portraits.Count} total portraits");
        }

        private void CreatePortrait(Unit unit, int index, float portraitSize)
        {
            TurnOrderPortrait portrait = new TurnOrderPortrait();
            portrait.unit = unit;

            // Create container
            GameObject container = new GameObject($"Portrait_{unit.UnitName}");
            container.transform.SetParent(transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(portraitSize, portraitSize + 30);
            
            // Horizontal layout - all in one row
            float xPos = index * (portraitSize + portraitSpacing);
            float yPos = 0;
            
            containerRect.anchoredPosition = new Vector2(xPos, yPos);

            portrait.portraitObject = container;

            // Create highlight border (background)
            GameObject borderObj = new GameObject("HighlightBorder");
            borderObj.transform.SetParent(container.transform, false);
            portrait.highlightBorder = borderObj.AddComponent<Image>();
            portrait.highlightBorder.color = new Color(1f, 1f, 0f, 0f); // Transparent yellow

            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 1f);
            borderRect.anchorMax = new Vector2(0.5f, 1f);
            borderRect.pivot = new Vector2(0.5f, 1f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(portraitSize + 8, portraitSize + 8);

            // Create portrait image
            GameObject imageObj = new GameObject("Portrait");
            imageObj.transform.SetParent(container.transform, false);
            portrait.portraitImage = imageObj.AddComponent<Image>();

            // Assign placeholder sprite
            if (unit.IsPlayerControlled)
            {
                portrait.portraitImage.sprite = PortraitGenerator.GetPlayerPortrait();
            }
            else
            {
                portrait.portraitImage.sprite = PortraitGenerator.GetEnemyPortrait();
            }

            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 1f);
            imageRect.anchorMax = new Vector2(0.5f, 1f);
            imageRect.pivot = new Vector2(0.5f, 1f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(portraitSize, portraitSize);

            // Create name label
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(container.transform, false);
            portrait.nameText = nameObj.AddComponent<Text>();
            portrait.nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            portrait.nameText.fontSize = 12;
            portrait.nameText.alignment = TextAnchor.MiddleCenter;
            portrait.nameText.color = Color.white;
            portrait.nameText.text = unit.UnitName;

            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0, -portraitSize - 5);
            nameRect.sizeDelta = new Vector2(portraitSize, 20);

            // Create HP bar background
            GameObject hpBgObj = new GameObject("HPBarBackground");
            hpBgObj.transform.SetParent(container.transform, false);
            portrait.hpBarBackground = hpBgObj.AddComponent<Image>();
            portrait.hpBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark gray

            RectTransform hpBgRect = hpBgObj.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0.5f, 1f);
            hpBgRect.anchorMax = new Vector2(0.5f, 1f);
            hpBgRect.pivot = new Vector2(0.5f, 1f);
            hpBgRect.anchoredPosition = new Vector2(0, -portraitSize - 25);
            hpBgRect.sizeDelta = new Vector2(portraitSize - 10, 6);

            // Create HP bar fill
            GameObject hpFillObj = new GameObject("HPBarFill");
            hpFillObj.transform.SetParent(hpBgObj.transform, false);
            portrait.hpBarFill = hpFillObj.AddComponent<Image>();
            portrait.hpBarFill.color = Color.green;

            RectTransform hpFillRect = hpFillObj.GetComponent<RectTransform>();
            hpFillRect.anchorMin = new Vector2(0f, 0.5f);
            hpFillRect.anchorMax = new Vector2(1f, 0.5f);
            hpFillRect.pivot = new Vector2(0f, 0.5f);
            hpFillRect.anchoredPosition = Vector2.zero;
            hpFillRect.sizeDelta = new Vector2(0, 6);

            // Make portrait clickable
            Button portraitButton = container.AddComponent<Button>();
            portraitButton.onClick.AddListener(() => OnPortraitClicked(unit));

            // Add visual feedback
            portraitButton.targetGraphic = portrait.portraitImage;
            ColorBlock colors = portraitButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            portraitButton.colors = colors;

            portraits.Add(portrait);

            Debug.Log($"TurnOrderUI: Portrait created for {unit.UnitName}, container position: {containerRect.anchoredPosition}");
        }

        private void OnPortraitClicked(Unit unit)
        {
            if (unit != null && unit.IsPlayerControlled)
            {
                PartyManager.Instance?.SelectUnit(unit);
                Debug.Log($"Portrait clicked: Selected {unit.UnitName}");
            }
        }

        private void UpdateHighlights()
        {
            if (turnManager == null) return;

            Unit currentUnit = turnManager.CurrentUnit;
            Unit selectedUnit = PartyManager.Instance?.SelectedUnit;

            foreach (var portrait in portraits)
            {
                bool isCurrent = (portrait.unit == currentUnit);
                bool isSelected = (portrait.unit == selectedUnit);

                // Yellow border for current turn, white for selected
                if (isSelected)
                {
                    portrait.highlightBorder.color = new Color(1f, 1f, 1f, 1f); // White for selected
                }
                else if (isCurrent)
                {
                    portrait.highlightBorder.color = new Color(1f, 1f, 0f, 0.5f); // Faint yellow for current turn
                }
                else
                {
                    portrait.highlightBorder.color = new Color(1f, 1f, 1f, 0.2f); // Very faint white
                }

                // Make portrait brighter for selected unit
                portrait.portraitImage.color = isSelected ?
                    Color.white :
                    new Color(0.7f, 0.7f, 0.7f, 1f);

                // Update HP bar
                if (portrait.unit != null && portrait.hpBarFill != null)
                {
                    float hpPercent = (float)portrait.unit.CurrentHP / portrait.unit.MaxHP;
                    portrait.hpBarFill.fillAmount = hpPercent;

                    // Color based on HP percentage
                    if (hpPercent > 0.6f)
                        portrait.hpBarFill.color = Color.green;
                    else if (hpPercent > 0.3f)
                        portrait.hpBarFill.color = Color.yellow;
                    else
                        portrait.hpBarFill.color = Color.red;
                }
            }
        }
    }
}
