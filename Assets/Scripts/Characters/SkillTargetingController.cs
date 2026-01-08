using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Grid;
using Riftbourne.Combat;
using Riftbourne.Skills;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles skill selection and targeting for a unit.
    /// Separated from movement controller for better separation of concerns.
    /// </summary>
    public class SkillTargetingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Unit unit;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TurnManager turnManager;

        // Skill selection state
        private Skill selectedSkill = null;
        private bool awaitingSkillTarget = false;

        // Camera service reference
        private CameraService cameraService;

        // Public properties
        public bool IsSkillSelected => selectedSkill != null;
        public bool AwaitingSkillTarget => awaitingSkillTarget;

        private void Awake()
        {
            unit = GetComponent<Unit>();
        }

        private void Start()
        {
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
            }

            if (turnManager == null)
            {
                turnManager = ManagerRegistry.Get<TurnManager>();
            }

            // Get camera service
            cameraService = CameraService.Instance;
        }

        private void Update()
        {
            // Only process input if this unit is the currently selected party member
            if (PartyManager.Instance == null || PartyManager.Instance.SelectedUnit != unit)
            {
                return;
            }

            // Only process input if this unit is in the current turn window
            if (turnManager == null || !turnManager.IsUnitInCurrentWindow(unit))
            {
                return;
            }

            // Number key selection (1-9) when skill menu is open
            if (awaitingSkillTarget && Keyboard.current != null)
            {
                List<Skill> availableSkills = unit.GetAvailableSkills();

                if (Keyboard.current.digit1Key.wasPressedThisFrame && availableSkills.Count >= 1)
                    SelectSkill(availableSkills[0]);
                if (Keyboard.current.digit2Key.wasPressedThisFrame && availableSkills.Count >= 2)
                    SelectSkill(availableSkills[1]);
                if (Keyboard.current.digit3Key.wasPressedThisFrame && availableSkills.Count >= 3)
                    SelectSkill(availableSkills[2]);
                if (Keyboard.current.digit4Key.wasPressedThisFrame && availableSkills.Count >= 4)
                    SelectSkill(availableSkills[3]);
                if (Keyboard.current.digit5Key.wasPressedThisFrame && availableSkills.Count >= 5)
                    SelectSkill(availableSkills[4]);
            }

            // RIGHT CLICK - Skill selection menu
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleRightClick();
            }

            // ESC to cancel skill selection
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelSkillSelection();
            }
        }

        /// <summary>
        /// Handle skill targeting when left clicking during skill selection.
        /// Called by CharacterMovementController when a skill is selected.
        /// </summary>
        public void HandleSkillTargeting(int targetX, int targetY, Unit targetUnit)
        {
            if (!awaitingSkillTarget || selectedSkill == null)
                return;

            UseSelectedSkill(targetX, targetY, targetUnit);
        }

        /// <summary>
        /// Public method for UI to trigger skill selection.
        /// </summary>
        public void SelectSkillFromUI(Skill skill)
        {
            SelectSkill(skill);
        }

        private void SelectSkill(Skill skill)
        {
            selectedSkill = skill;
            awaitingSkillTarget = true;
            Debug.Log($">>> Selected: {skill.SkillName} - Click target to use (ESC to cancel) <<<");
            
            // Show skill range visualization
            if (gridManager != null && unit != null)
            {
                // Validate unit's grid position before showing range
                if (gridManager.IsValidGridPosition(unit.GridX, unit.GridY))
                {
                    gridManager.ShowSkillRange(unit.GridX, unit.GridY, skill.Range);
                    Debug.Log($"Showing skill range: {skill.Range} (Manhattan distance)");
                }
                else
                {
                    Debug.LogWarning($"Cannot show skill range - unit {unit.UnitName} has invalid grid position ({unit.GridX}, {unit.GridY})");
                }
            }
        }

        private void HandleRightClick()
        {
            // Show skill selection menu
            List<Skill> availableSkills = unit.GetAvailableSkills();

            if (availableSkills.Count == 0)
            {
                Debug.Log("No skills available!");
                return;
            }

            // Show menu and wait for number key press
            Debug.Log("=== AVAILABLE SKILLS ===");
            for (int i = 0; i < availableSkills.Count; i++)
            {
                string groundTarget = availableSkills[i].CreatesGroundHazard ? " [GROUND]" : " [UNIT]";
                Debug.Log($"Press [{i + 1}]: {availableSkills[i].SkillName}{groundTarget} (Range: {availableSkills[i].Range})");
            }
            Debug.Log("ESC to cancel.");

            // Set flag to listen for number keys
            awaitingSkillTarget = true;
            selectedSkill = null; // Don't auto-select!
        }

        private void UseSelectedSkill(int targetX, int targetY, Unit targetUnit)
        {
            // Validate selected skill
            if (selectedSkill == null)
            {
                Debug.LogError("UseSelectedSkill: No skill selected!");
                return;
            }

            // Check if already acted this turn
            if (unit.HasActedThisTurn)
            {
                Debug.Log("You have already acted this turn!");
                return;
            }

            bool success = false;
            SkillExecutor skillExecutor = ManagerRegistry.Get<SkillExecutor>();
            
            if (skillExecutor == null)
            {
                Debug.LogError("SkillExecutor not found!");
                return;
            }

            // Ground-targeted skill (Ignite Ground) - can target empty cells or occupied cells
            if (selectedSkill.CreatesGroundHazard)
            {
                // Ground hazards can be placed on any valid cell (empty or occupied)
                success = skillExecutor.ExecuteGroundSkill(selectedSkill, unit, targetX, targetY);
            }
            // Unit-targeted skill (Spark) - requires a valid unit target
            else if (targetUnit != null && targetUnit != unit)
            {
                success = skillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
            }
            else
            {
                Debug.Log($"{selectedSkill.SkillName} requires a valid target!");
            }

            if (success)
            {
                // Clear skill range visualization after successful use
                if (gridManager != null)
                {
                    gridManager.ClearRangeHighlights();
                }
                CancelSkillSelection();
                // Don't end turn automatically - let player decide!
            }
        }

        /// <summary>
        /// Public method for clicking enemies to use skills on them.
        /// </summary>
        public void UseSkillOnTarget(Unit targetUnit)
        {
            if (selectedSkill == null || !awaitingSkillTarget)
            {
                Debug.LogWarning("No skill selected!");
                return;
            }

            if (unit.HasActedThisTurn)
            {
                Debug.Log("You have already acted this turn!");
                return;
            }

            if (selectedSkill.CreatesGroundHazard)
            {
                Debug.Log($"{selectedSkill.SkillName} requires ground target, not unit target!");
                return;
            }

            SkillExecutor skillExecutor = ManagerRegistry.Get<SkillExecutor>();
            if (skillExecutor != null)
            {
                bool success = skillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
                if (success)
                {
                    // Clear skill range visualization after successful use
                    if (gridManager != null)
                    {
                        gridManager.ClearRangeHighlights();
                    }
                    CancelSkillSelection();
                }
            }
        }

        private void CancelSkillSelection()
        {
            if (awaitingSkillTarget)
            {
                Debug.Log("Skill selection cancelled");
            }
            selectedSkill = null;
            awaitingSkillTarget = false;
            
            // Clear skill range visualization
            if (gridManager != null)
            {
                gridManager.ClearRangeHighlights();
            }
        }
    }
}

