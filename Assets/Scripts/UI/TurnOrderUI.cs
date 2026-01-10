using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;
using Riftbourne.Combat;
using Riftbourne.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays turn order based on actual turn sequence, not windows.
    /// Shows which units have finished their turns and removes them from queue.
    /// </summary>
    public class TurnOrderUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnManager turnManager;

        [Header("Layout")]
        [SerializeField] private float portraitSize = 70f;
        [SerializeField] private float portraitSpacing = 10f;
        [SerializeField] private float leftPadding = 183.33f;
        [SerializeField] private float rightPadding = 128.33f;
        [SerializeField] private float topPadding = 15f;

        [Header("Animation")]
        [SerializeField] private float animationDuration = 0.3f;

        private List<TurnOrderPortrait> portraits = new List<TurnOrderPortrait>();
        private HashSet<Unit> lastKnownFinishedUnits = new HashSet<Unit>();
        private bool isAnimating = false;

        private void Awake()
        {
            if (turnManager == null)
            {
                turnManager = ManagerRegistry.Get<TurnManager>();
            }
        }

        private void OnEnable()
        {
            if (turnManager != null)
            {
                turnManager.OnTurnWindowChanged += OnTurnWindowChanged;
                turnManager.OnUnitTurnEnded += OnUnitTurnEnded;
            }
        }

        private void OnDisable()
        {
            if (turnManager != null)
            {
                turnManager.OnTurnWindowChanged -= OnTurnWindowChanged;
                turnManager.OnUnitTurnEnded -= OnUnitTurnEnded;
            }
        }

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
            if (turnManager == null)
            {
                Debug.LogError("TurnOrderUI: Could not find TurnManager!");
                return;
            }

            StartCoroutine(InitializeDelayed());
        }

        private IEnumerator InitializeDelayed()
        {
            yield return null;
            CreateAllPortraits();
            UpdateTurnOrderLayout(immediate: true);
        }

        private void Update()
        {
            UpdateHighlights();
            // CheckForFinishedUnitsChange() is now event-driven, but we keep Update() for highlights
        }

        /// <summary>
        /// Track which units have finished their turns by checking who's NOT in the current window anymore.
        /// </summary>
        private HashSet<Unit> GetFinishedUnits()
        {
            HashSet<Unit> finishedUnits = new HashSet<Unit>();

            if (turnManager == null) return finishedUnits;

            List<Unit> fullTurnOrder = turnManager.GetTurnOrder();
            List<Unit> currentWindow = turnManager.GetCurrentTurnWindow();
            int windowStartIndex = turnManager.GetCurrentTurnIndex();

            // All units BEFORE the current window start have finished
            for (int i = 0; i < windowStartIndex; i++)
            {
                finishedUnits.Add(fullTurnOrder[i]);
            }

            // Units AT or AFTER window start but NOT in current window have finished
            // (These are units who were in the window but ended their turn)
            for (int i = windowStartIndex; i < fullTurnOrder.Count; i++)
            {
                Unit unit = fullTurnOrder[i];
                if (!currentWindow.Contains(unit))
                {
                    finishedUnits.Add(unit);
                }
            }

            return finishedUnits;
        }

        /// <summary>
        /// Event handler for turn window changes. Updates the layout when units finish their turns.
        /// </summary>
        private void OnTurnWindowChanged(List<Unit> currentWindow)
        {
            if (isAnimating)
            {
                // If animating, queue the update for after animation completes
                // But don't skip entirely - we need to update eventually
                return;
            }

            HashSet<Unit> currentFinished = GetFinishedUnits();

            // Always update if finished units changed, or if window is empty (new round starting)
            bool shouldUpdate = !currentFinished.SetEquals(lastKnownFinishedUnits) || 
                               (currentWindow.Count == 0 && lastKnownFinishedUnits.Count > 0);

            if (shouldUpdate)
            {
                Debug.Log($"Finished units changed from {lastKnownFinishedUnits.Count} to {currentFinished.Count}");
                Debug.Log($"  Finished: [{string.Join(", ", currentFinished.Select(u => u.UnitName))}]");
                Debug.Log($"  Current window: [{string.Join(", ", currentWindow.Select(u => u.UnitName))}]");

                lastKnownFinishedUnits = new HashSet<Unit>(currentFinished);
                UpdateTurnOrderLayout(immediate: false);
            }
        }

        /// <summary>
        /// Event handler for when a unit ends their turn.
        /// </summary>
        private void OnUnitTurnEnded(Unit unit)
        {
            // Trigger layout update when a unit ends their turn
            OnTurnWindowChanged(turnManager?.GetCurrentTurnWindow() ?? new List<Unit>());
        }

        private void CreateAllPortraits()
        {
            if (turnManager == null) return;

            List<Unit> units = turnManager.GetAllUnits();
            Debug.Log($"TurnOrderUI: Creating {units.Count} portraits");

            foreach (Unit unit in units)
            {
                CreatePortrait(unit);
            }
        }

        private int CalculateMaxVisiblePortraits()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            float availableWidth = rectTransform.rect.width - leftPadding - rightPadding;

            int maxPortraits = Mathf.FloorToInt((availableWidth + portraitSpacing) / (portraitSize + portraitSpacing));

            return Mathf.Max(1, maxPortraits);
        }

        /// <summary>
        /// Build the display queue: full turn order, MINUS units who have finished, wrapping to next round.
        /// </summary>
        private void UpdateTurnOrderLayout(bool immediate)
        {
            if (turnManager == null) return;

            List<Unit> fullTurnOrder = turnManager.GetTurnOrder();
            HashSet<Unit> finishedUnits = GetFinishedUnits();
            int maxVisible = CalculateMaxVisiblePortraits();

            Debug.Log($"=== UPDATE LAYOUT === Finished: {finishedUnits.Count}, Max Visible: {maxVisible}");

            // Build display queue from full turn order, skipping finished units, wrapping around
            List<TurnOrderPortrait> visiblePortraits = new List<TurnOrderPortrait>();
            int displayCount = 0;

            // Start from beginning of turn order and loop until we have enough portraits
            for (int roundOffset = 0; roundOffset < 2 && displayCount < maxVisible; roundOffset++)
            {
                for (int i = 0; i < fullTurnOrder.Count && displayCount < maxVisible; i++)
                {
                    Unit unit = fullTurnOrder[i];

                    // Skip finished units (only in first round)
                    if (roundOffset == 0 && finishedUnits.Contains(unit))
                    {
                        continue;
                    }

                    // Skip dead units
                    if (!unit.IsAlive)
                    {
                        continue;
                    }

                    TurnOrderPortrait portrait = portraits.Find(p => p.unit == unit);
                    if (portrait != null)
                    {
                        visiblePortraits.Add(portrait);
                        string roundLabel = roundOffset == 0 ? "This Round" : "Next Round";
                        Debug.Log($"  Position {displayCount}: {unit.UnitName} ({roundLabel})");
                        displayCount++;
                    }
                }
            }

            // Position portraits
            if (immediate)
            {
                PositionPortraitsImmediate(visiblePortraits);
            }
            else
            {
                StartCoroutine(AnimateLayoutChange(visiblePortraits));
            }

            // Hide non-visible portraits
            foreach (TurnOrderPortrait portrait in portraits)
            {
                if (!visiblePortraits.Contains(portrait))
                {
                    portrait.portraitObject.SetActive(false);
                }
            }
        }

        private void PositionPortraitsImmediate(List<TurnOrderPortrait> visiblePortraits)
        {
            if (visiblePortraits.Count == 0) return;

            // Calculate total width of all portraits
            float totalWidth = (visiblePortraits.Count * portraitSize) + ((visiblePortraits.Count - 1) * portraitSpacing);
            
            // Since the RectTransform pivot is at center (0.5), anchoredPosition.x is relative to center
            // To center the group: start from -totalWidth/2, then add half portrait size to center first portrait
            float startX = -totalWidth / 2f + portraitSize / 2f;
            
            for (int i = 0; i < visiblePortraits.Count; i++)
            {
                TurnOrderPortrait portrait = visiblePortraits[i];
                portrait.portraitObject.SetActive(true);

                float xPos = startX + (i * (portraitSize + portraitSpacing));
                float yPos = -topPadding;

                RectTransform rect = portrait.portraitObject.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(xPos, yPos);
            }
        }

        private IEnumerator AnimateLayoutChange(List<TurnOrderPortrait> visiblePortraits)
        {
            isAnimating = true;

            Dictionary<TurnOrderPortrait, Vector2> targetPositions = new Dictionary<TurnOrderPortrait, Vector2>();
            Dictionary<TurnOrderPortrait, Vector2> startPositions = new Dictionary<TurnOrderPortrait, Vector2>();

            // Calculate total width of all portraits
            float totalWidth = (visiblePortraits.Count * portraitSize) + ((visiblePortraits.Count - 1) * portraitSpacing);
            
            // Since the RectTransform pivot is at center (0.5), anchoredPosition.x is relative to center
            // To center the group: start from -totalWidth/2, then add half portrait size to center first portrait
            float startX = -totalWidth / 2f + portraitSize / 2f;
            
            for (int i = 0; i < visiblePortraits.Count; i++)
            {
                TurnOrderPortrait portrait = visiblePortraits[i];
                portrait.portraitObject.SetActive(true);

                RectTransform rect = portrait.portraitObject.GetComponent<RectTransform>();
                startPositions[portrait] = rect.anchoredPosition;

                float targetX = startX + (i * (portraitSize + portraitSpacing));
                float targetY = -topPadding;
                targetPositions[portrait] = new Vector2(targetX, targetY);
            }

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                t = t * t * (3f - 2f * t);

                foreach (TurnOrderPortrait portrait in visiblePortraits)
                {
                    if (portrait.portraitObject.activeSelf)
                    {
                        RectTransform rect = portrait.portraitObject.GetComponent<RectTransform>();
                        rect.anchoredPosition = Vector2.Lerp(startPositions[portrait], targetPositions[portrait], t);
                    }
                }

                yield return null;
            }

            // Snap to final
            foreach (TurnOrderPortrait portrait in visiblePortraits)
            {
                if (portrait.portraitObject.activeSelf)
                {
                    RectTransform rect = portrait.portraitObject.GetComponent<RectTransform>();
                    rect.anchoredPosition = targetPositions[portrait];
                }
            }

            isAnimating = false;
            Debug.Log("Animation complete!");
        }

        private void CreatePortrait(Unit unit)
        {
            TurnOrderPortrait portrait = new TurnOrderPortrait();
            portrait.unit = unit;

            GameObject container = new GameObject($"Portrait_{unit.UnitName}");
            container.transform.SetParent(transform, false);
            container.SetActive(false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            float containerHeight = portraitSize + 30;
            containerRect.sizeDelta = new Vector2(portraitSize, containerHeight);

            portrait.portraitObject = container;

            // Highlight border
            GameObject borderObj = new GameObject("HighlightBorder");
            borderObj.transform.SetParent(container.transform, false);
            portrait.highlightBorder = borderObj.AddComponent<Image>();
            portrait.highlightBorder.color = new Color(1f, 1f, 0f, 0f);

            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 1f);
            borderRect.anchorMax = new Vector2(0.5f, 1f);
            borderRect.pivot = new Vector2(0.5f, 1f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(portraitSize + 8, portraitSize + 8);

            // Portrait image
            GameObject imageObj = new GameObject("Portrait");
            imageObj.transform.SetParent(container.transform, false);
            portrait.portraitImage = imageObj.AddComponent<Image>();

            // Use unit's portrait if assigned, otherwise fallback to PortraitGenerator
            if (unit.Portrait != null)
            {
                portrait.portraitImage.sprite = unit.Portrait;
            }
            else
            {
                if (unit.IsPlayerControlled)
                    portrait.portraitImage.sprite = PortraitGenerator.GetPlayerPortrait();
                else
                    portrait.portraitImage.sprite = PortraitGenerator.GetEnemyPortrait();
            }

            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 1f);
            imageRect.anchorMax = new Vector2(0.5f, 1f);
            imageRect.pivot = new Vector2(0.5f, 1f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(portraitSize, portraitSize);

            // Name
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
            nameRect.anchoredPosition = new Vector2(0, -portraitSize - 2);
            nameRect.sizeDelta = new Vector2(portraitSize, 15);

            // HP bar
            GameObject hpBgObj = new GameObject("HPBarBackground");
            hpBgObj.transform.SetParent(container.transform, false);
            portrait.hpBarBackground = hpBgObj.AddComponent<Image>();
            portrait.hpBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            RectTransform hpBgRect = hpBgObj.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0.5f, 1f);
            hpBgRect.anchorMax = new Vector2(0.5f, 1f);
            hpBgRect.pivot = new Vector2(0.5f, 1f);
            hpBgRect.anchoredPosition = new Vector2(0, -portraitSize - 20);
            hpBgRect.sizeDelta = new Vector2(portraitSize - 10, 5);

            GameObject hpFillObj = new GameObject("HPBarFill");
            hpFillObj.transform.SetParent(hpBgObj.transform, false);
            portrait.hpBarFill = hpFillObj.AddComponent<Image>();
            portrait.hpBarFill.color = Color.green;

            RectTransform hpFillRect = hpFillObj.GetComponent<RectTransform>();
            hpFillRect.anchorMin = new Vector2(0f, 0.5f);
            hpFillRect.anchorMax = new Vector2(1f, 0.5f);
            hpFillRect.pivot = new Vector2(0f, 0.5f);
            hpFillRect.anchoredPosition = Vector2.zero;
            hpFillRect.sizeDelta = new Vector2(0, 5);

            // Clickable
            Button portraitButton = container.AddComponent<Button>();
            portraitButton.onClick.AddListener(() => OnPortraitClicked(unit));
            portraitButton.targetGraphic = portrait.portraitImage;

            ColorBlock colors = portraitButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            portraitButton.colors = colors;

            portraits.Add(portrait);
        }

        private void OnPortraitClicked(Unit unit)
        {
            if (unit != null && unit.IsPlayerControlled)
            {
                PartyManager.Instance?.SelectUnit(unit);
            }
        }

        private void UpdateHighlights()
        {
            if (turnManager == null) return;

            Unit currentUnit = turnManager.CurrentUnit;
            Unit selectedUnit = PartyManager.Instance?.SelectedUnit;
            List<Unit> currentWindow = turnManager.GetCurrentTurnWindow();

            foreach (var portrait in portraits)
            {
                if (!portrait.portraitObject.activeSelf) continue;

                bool isCurrent = (portrait.unit == currentUnit);
                bool isSelected = (portrait.unit == selectedUnit);
                bool isInCurrentWindow = currentWindow.Contains(portrait.unit);

                if (isSelected)
                {
                    portrait.highlightBorder.color = new Color(1f, 1f, 1f, 1f);
                }
                else if (isInCurrentWindow)
                {
                    portrait.highlightBorder.color = new Color(0f, 1f, 1f, 0.5f);
                }
                else if (isCurrent)
                {
                    portrait.highlightBorder.color = new Color(1f, 1f, 0f, 0.5f);
                }
                else
                {
                    portrait.highlightBorder.color = new Color(1f, 1f, 1f, 0.2f);
                }

                portrait.portraitImage.color = isSelected ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);

                if (portrait.unit != null && portrait.hpBarFill != null)
                {
                    float hpPercent = (float)portrait.unit.CurrentHP / portrait.unit.MaxHP;
                    portrait.hpBarFill.fillAmount = hpPercent;

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