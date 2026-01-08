using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Characters;
using Riftbourne.Combat;
using Riftbourne.Skills;
using Riftbourne.Grid;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.UI
{
    /// <summary>
    /// Bottom-center action panel with Move/Attack/Skills/End Turn buttons.
    /// Replaces keyboard shortcuts with mouse-clickable UI.
    /// </summary>
    public class ActionPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnManager turnManager;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text currentUnitText;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillsButton;
        [SerializeField] private Button endTurnButton;

        [Header("Skills Dropdown")]
        [SerializeField] private GameObject skillsDropdown;
        [SerializeField] private Transform skillsButtonContainer;
        [SerializeField] private GameObject skillButtonPrefab;

        [Header("Grid Reference")]
        [SerializeField] private GridManager gridManager;

        private Unit currentUnit;
        private CharacterMovementController currentController;
        private SkillTargetingController currentSkillController;
        private bool skillsMenuOpen = false;

        private void Awake()
        {
            if (turnManager == null)
            {
                turnManager = ManagerRegistry.Get<TurnManager>();
            }
        }

        private void Start()
        {

            // Hide skills dropdown initially
            if (skillsDropdown != null)
            {
                skillsDropdown.SetActive(false);
            }

            // Setup button listeners
            if (moveButton != null)
                moveButton.onClick.AddListener(OnMoveButtonClicked);
            if (attackButton != null)
                attackButton.onClick.AddListener(OnAttackButtonClicked);
            if (skillsButton != null)
                skillsButton.onClick.AddListener(OnSkillsButtonClicked);
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }

        private void Update()
        {
            UpdateCurrentUnit();
            UpdateButtonStates();
            
            // Update stats display every frame to show real-time HP/action changes
            if (currentUnit != null && currentUnitText != null)
            {
                UpdateUnitStatsDisplay();
            }
        }

        private void UpdateCurrentUnit()
        {
            // Track the SELECTED unit from PartyManager, not just the current turn unit
            Unit newUnit = null;
            
            if (PartyManager.Instance != null)
            {
                newUnit = PartyManager.Instance.SelectedUnit;
            }
            
            // Fallback to TurnManager's current unit if no selection
            if (newUnit == null && turnManager != null)
            {
                newUnit = turnManager.CurrentUnit;
            }

            if (newUnit != currentUnit)
            {
                currentUnit = newUnit;
                currentController = currentUnit?.GetComponent<CharacterMovementController>();
                currentSkillController = currentUnit?.GetComponent<SkillTargetingController>();

                // Update unit stats display
                if (currentUnitText != null && currentUnit != null)
                {
                    UpdateUnitStatsDisplay();
                }

                // Close skills menu when unit changes
                if (skillsDropdown != null)
                {
                    skillsDropdown.SetActive(false);
                    skillsMenuOpen = false;
                }
            }
        }

        private void UpdateButtonStates()
        {
            // Check if current unit is valid and in the active turn window
            bool canAct = false;
            
            if (currentUnit != null && currentUnit.IsPlayerControlled)
            {
                if (turnManager != null && turnManager.IsUnitInCurrentWindow(currentUnit))
                {
                    canAct = true;
                }
            }

            if (!canAct)
            {
                // Disable all buttons if not player's turn or not in window
                SetButtonInteractable(moveButton, false);
                SetButtonInteractable(attackButton, false);
                SetButtonInteractable(skillsButton, false);
                SetButtonInteractable(endTurnButton, false);
                return;
            }

            // Enable/disable based on action economy
            SetButtonInteractable(moveButton, currentUnit.MovementPointsRemaining > 0);
            SetButtonInteractable(attackButton, !currentUnit.HasActedThisTurn);
            SetButtonInteractable(skillsButton, !currentUnit.HasActedThisTurn);
            SetButtonInteractable(endTurnButton, true); // Always can end turn
        }

        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        // Button click handlers
        private void OnMoveButtonClicked()
        {
            Debug.Log("Move button clicked - Click a valid cell to move");

            if (currentUnit != null && gridManager != null)
            {
                // Use pathfinding-aware ShowMovementRange
                gridManager.ShowMovementRange(
                    currentUnit,  // Pass the unit for pathfinding
                    currentUnit.MovementPointsRemaining  // Show remaining, not max
                );
            }
        }

        private void OnAttackButtonClicked()
        {
            Debug.Log("Attack button clicked - Click an adjacent enemy to attack");

            if (currentUnit != null && gridManager != null)
            {
                gridManager.ShowAttackRange(
                    currentUnit.GridX,
                    currentUnit.GridY,
                    1 // Melee range = 1
                );
            }
        }

        private void OnSkillsButtonClicked()
        {
            Debug.Log("Skills button clicked"); // ADD THIS LINE

            if (skillsDropdown == null) return;

            skillsMenuOpen = !skillsMenuOpen;
            skillsDropdown.SetActive(skillsMenuOpen);

            if (skillsMenuOpen)
            {
                PopulateSkillsMenu();
            }
        }

        private void OnEndTurnButtonClicked()
        {
            if (turnManager != null)
            {
                Debug.Log("End Turn button clicked");

                // Clear range highlights when ending turn
                if (gridManager != null)
                {
                    gridManager.ClearRangeHighlights();
                }

                turnManager.EndTurn();
            }
        }

        private void PopulateSkillsMenu()
        {
            if (currentUnit == null || skillsButtonContainer == null) return;

            // Clear existing buttons
            foreach (Transform child in skillsButtonContainer)
            {
                Destroy(child.gameObject);
            }

            // Create button for each available skill
            List<Skill> skills = currentUnit.GetAvailableSkills();

            if (skills.Count == 0)
            {
                Debug.Log("No skills available for this unit");
                return;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                Skill skill = skills[i];
                CreateSkillButton(skill, i);
            }
        }

        private void CreateSkillButton(Skill skill, int index)
        {
            GameObject buttonObj;

            // Use prefab if provided, otherwise create simple button
            if (skillButtonPrefab != null)
            {
                buttonObj = Instantiate(skillButtonPrefab, skillsButtonContainer);
            }
            else
            {
                buttonObj = new GameObject($"Skill_{skill.SkillName}");
                buttonObj.transform.SetParent(skillsButtonContainer, false);

                RectTransform rect = buttonObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 30);
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);

                Image bg = buttonObj.AddComponent<Image>();
                bg.color = new Color(0.8f, 0.3f, 0.3f, 1f);

                Button btn = buttonObj.AddComponent<Button>();

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);

                Text text = textObj.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
            }

            // Set button text
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                string targetType = skill.CreatesGroundHazard ? "[GROUND]" : "[UNIT]";
                buttonText.text = $"{index + 1}. {skill.SkillName} {targetType} (Range: {skill.Range})";
            }

            // Add click listener
            Button button = buttonObj.GetComponent<Button>();
            if (button != null && currentSkillController != null)
            {
                button.onClick.AddListener(() => OnSkillSelected(skill));
            }
        }

        private void OnSkillSelected(Skill skill)
        {
            if (currentSkillController != null)
            {
                // Call skill targeting controller's skill selection
                currentSkillController.SelectSkillFromUI(skill);

                // Close dropdown
                if (skillsDropdown != null)
                {
                    skillsDropdown.SetActive(false);
                    skillsMenuOpen = false;
                }
            }
        }

        /// <summary>
        /// Update the unit stats display text with HP and action economy.
        /// </summary>
        private void UpdateUnitStatsDisplay()
        {
            if (currentUnit == null || currentUnitText == null) return;

            // Build status text
            string statusText = $"<b>{currentUnit.UnitName}</b> - Lv.{currentUnit.Level}\n";
            statusText += $"HP: {currentUnit.CurrentHP}/{currentUnit.MaxHP}";
            
            // Show XP progress
            int xpNeeded = currentUnit.GetXPRequiredForNextLevel();
            statusText += $" | XP: {currentUnit.CurrentXP}/{xpNeeded}";
            
            // Show SP if any
            if (currentUnit.SkillPoints > 0)
            {
                statusText += $" | SP: {currentUnit.SkillPoints}";
            }
            
            // Show movement points and action status
            statusText += $"\nMove: {currentUnit.MovementPointsRemaining}";
            
            string actStatus = currentUnit.HasActedThisTurn ? "<color=red>Used</color>" : "<color=green>Ready</color>";
            statusText += $"  |  Action: {actStatus}";

            currentUnitText.text = statusText;
        }
    }
}