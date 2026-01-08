using UnityEngine;
using UnityEngine.UI;
using Riftbourne.Characters;
using Riftbourne.Combat;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.UI
{
    /// <summary>
    /// Manages the combat UI panels showing unit status.
    /// Player units on left, enemy units on right.
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private Transform playerPanel;
        [SerializeField] private Transform enemyPanel;

        [Header("Prefab")]
        [SerializeField] private GameObject unitStatusPrefab;

        private Dictionary<Unit, UnitStatusUI> unitStatusDisplays = new Dictionary<Unit, UnitStatusUI>();
        private TurnManager turnManager;

        private void Awake()
        {
            turnManager = ManagerRegistry.Get<TurnManager>();
        }

        private void OnEnable()
        {
            // Subscribe to GameEvents for decoupled event handling
            GameEvents.OnCurrentUnitChanged += OnCurrentUnitChanged;
            
            // Also subscribe to TurnManager for backward compatibility
            if (turnManager != null)
            {
                turnManager.OnCurrentUnitChanged += OnCurrentUnitChanged;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from GameEvents
            GameEvents.OnCurrentUnitChanged -= OnCurrentUnitChanged;
            
            // Unsubscribe from TurnManager
            if (turnManager != null)
            {
                turnManager.OnCurrentUnitChanged -= OnCurrentUnitChanged;
            }
        }

        private void Start()
        {
            CreateUIPanels();
            FindAndRegisterUnits();
        }

        private void Update()
        {
            UpdateAllUnitDisplays();
            // HighlightCurrentUnit() is now event-driven
        }

        /// <summary>
        /// Event handler for when the current unit changes.
        /// </summary>
        private void OnCurrentUnitChanged(Unit newUnit)
        {
            HighlightCurrentUnit();
        }

        private void CreateUIPanels()
        {
            // Panels will be created in the scene manually via Canvas
            // This method is a placeholder for now
        }

        private void FindAndRegisterUnits()
        {
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);

            foreach (Unit unit in allUnits)
            {
                CreateUnitStatusDisplay(unit);
            }
        }

        private void CreateUnitStatusDisplay(Unit unit)
        {
            Transform parentPanel = unit.IsPlayerControlled ? playerPanel : enemyPanel;

            GameObject statusObj = new GameObject($"{unit.UnitName}_Status");
            statusObj.transform.SetParent(parentPanel, false);

            // CRITICAL: Add RectTransform for UI layout
            RectTransform rect = statusObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 60); // Set size for layout group

            UnitStatusUI statusUI = statusObj.AddComponent<UnitStatusUI>();
            statusUI.Initialize(unit);

            unitStatusDisplays[unit] = statusUI;
        }

        private void UpdateAllUnitDisplays()
        {
            foreach (var kvp in unitStatusDisplays)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    kvp.Value.UpdateDisplay();
                }
            }
        }

        private void HighlightCurrentUnit()
        {
            if (turnManager == null) return;

            foreach (var kvp in unitStatusDisplays)
            {
                bool isCurrentUnit = (kvp.Key == turnManager.CurrentUnit);
                kvp.Value.SetHighlight(isCurrentUnit);
            }
        }
    }
}