using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;
using Riftbourne.Combat;
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

            for (int i = 0; i < units.Count; i++)
            {
                Debug.Log($"TurnOrderUI: Creating portrait {i} for {units[i].UnitName}");
                CreatePortrait(units[i], i);
            }

            Debug.Log($"TurnOrderUI: Created {portraits.Count} total portraits");
        }

        private void CreatePortrait(Unit unit, int index)
        {
            TurnOrderPortrait portrait = new TurnOrderPortrait();
            portrait.unit = unit;

            // Create container
            GameObject container = new GameObject($"Portrait_{unit.UnitName}");
            container.transform.SetParent(transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(portraitSize, portraitSize + 30);
            containerRect.anchoredPosition = new Vector2(index * (portraitSize + portraitSpacing), 0);

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

            portraits.Add(portrait);
            
            Debug.Log($"TurnOrderUI: Portrait created for {unit.UnitName}, container position: {containerRect.anchoredPosition}");
        }

        private void UpdateHighlights()
        {
            if (turnManager == null) return;

            Unit currentUnit = turnManager.CurrentUnit;

            foreach (var portrait in portraits)
            {
                bool isCurrent = (portrait.unit == currentUnit);

                // Highlight current unit with yellow border
                portrait.highlightBorder.color = isCurrent ?
                    new Color(1f, 1f, 0f, 1f) : // Bright yellow
                    new Color(1f, 1f, 1f, 0.2f); // Faint white

                // Make portrait brighter for current unit
                portrait.portraitImage.color = isCurrent ?
                    Color.white :
                    new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }
    }
}
