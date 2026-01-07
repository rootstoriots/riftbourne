using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Grid;
using Riftbourne.Combat;
using Riftbourne.Skills;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    public class CharacterMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Unit unit;

        // Skill selection state
        private Skill selectedSkill = null;
        private bool awaitingSkillTarget = false;

        // Cached camera reference
        private Camera mainCamera;

        private void Awake()
        {
            unit = GetComponent<Unit>();
        }

        private void Start()
        {
            if (gridManager == null)
            {
                gridManager = FindFirstObjectByType<GridManager>();
            }

            if (turnManager == null)
            {
                turnManager = FindFirstObjectByType<TurnManager>();
            }

            // Cache camera reference
            mainCamera = Camera.main;
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
                Debug.LogWarning($"{unit.UnitName}: Not in current turn window, cannot act");
                return;
            }

            // DEBUG: Log that this unit is actively listening for input
            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            {
                Debug.Log($"[INPUT] {unit.UnitName} is listening for input (selected and in window)");
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

            // LEFT CLICK - Movement, melee attack, or skill targeting
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleLeftClick();
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

            // SPACE or ENTER to end turn manually
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                Debug.Log("Player manually ended turn");
                turnManager.EndTurn();
            }
        }

        private void HandleLeftClick()
        {
            // Use cached camera reference with null check
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("No main camera found!");
                    return;
                }
            }

            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit))
                return;

            Vector3 hitPoint = hit.point;
            int targetX = Mathf.FloorToInt(hitPoint.x);
            int targetY = Mathf.FloorToInt(hitPoint.z);

            if (!gridManager.IsValidGridPosition(targetX, targetY))
                return;

            GridCell targetCell = gridManager.GetCell(targetX, targetY);
            Unit targetUnit = targetCell.OccupyingUnit;

            // CASE 0: Clicked on a player unit - select it for control
            if (targetUnit != null && targetUnit.IsPlayerControlled && targetUnit != unit)
            {
                PartyManager.Instance?.SelectUnit(targetUnit);
                Debug.Log($"Selected {targetUnit.UnitName} for control");
                return;
            }

            // CASE 1: Using a selected skill
            if (awaitingSkillTarget && selectedSkill != null)
            {
                UseSelectedSkill(targetX, targetY, targetUnit);
                return;
            }

            // CASE 2: Clicked on enemy unit - attack (melee or ranged)
            // Only attack if target is an enemy (not player-controlled) and not self
            if (targetUnit != null && targetUnit != unit && !targetUnit.IsPlayerControlled)
            {
                if (unit.HasActedThisTurn)
                {
                    Debug.Log("You have already acted this turn!");
                    return;
                }

                // Attempt attack - if not adjacent, show message but don't move
                if (!AttackAction.ExecuteMeleeAttack(unit, targetUnit))
                {
                    // Attack failed (likely not adjacent) - don't allow movement to enemy's position
                    Debug.Log($"Cannot attack {targetUnit.UnitName} - must be adjacent!");
                    return;
                }
                // Attack succeeded - MarkAsActed is already called by AttackAction
                return;
            }

            // CASE 3: Clicked on empty cell - move
            // Check if clicking on current position - don't count as movement
            if (targetX == unit.GridX && targetY == unit.GridY)
            {
                Debug.Log("Already at that position!");
                return;
            }

            // Calculate distance to target
            int distance = Mathf.Abs(targetX - unit.GridX) + Mathf.Abs(targetY - unit.GridY);

            // Check if we have enough movement points
            if (distance > unit.MovementPointsRemaining)
            {
                Debug.Log($"Not enough movement! Need {distance}, have {unit.MovementPointsRemaining}");
                return;
            }
            
            // Check if DESTINATION cell is occupied (can't end movement on occupied cell)
            if (targetCell.OccupyingUnit != null && targetCell.OccupyingUnit != unit)
            {
                Debug.Log($"Cannot move to ({targetX}, {targetY}) - occupied by {targetCell.OccupyingUnit.UnitName}!");
                return;
            }

            // Check if path is valid (can pass through allies but not enemies)
            if (unit.CanMoveTo(targetX, targetY))
            {
                Vector3 targetWorldPos = targetCell.WorldPosition;

                // Clear range visualizer when starting movement
                if (gridManager != null)
                {
                    gridManager.ClearRangeHighlights();
                }

                // Get the actual path to follow
                List<GridCell> path = gridManager.GetPath(unit, targetX, targetY);
                
                // If pathfinding failed, don't allow movement
                if (path == null || path.Count == 0)
                {
                    Debug.LogWarning($"Pathfinding failed for movement to ({targetX}, {targetY})");
                    return;
                }
                
                // Calculate movement cost based on actual path length (not Manhattan distance)
                // Path length is number of cells in path minus 1 (start cell doesn't count)
                int movementCost = path.Count - 1;
                
                // Verify we have enough movement points for the actual path
                if (movementCost > unit.MovementPointsRemaining)
                {
                    Debug.Log($"Not enough movement for path! Need {movementCost}, have {unit.MovementPointsRemaining}");
                    return;
                }
                
                // Start moving - points will be spent when movement completes
                unit.MoveTo(targetX, targetY, targetWorldPos, () => 
                {
                    // Movement complete callback - spend points based on actual path length
                    unit.SpendMovementPoints(movementCost);
                    
                    // Re-show movement range at new position with remaining points
                    if (gridManager != null && unit.MovementPointsRemaining > 0)
                    {
                        gridManager.ShowMovementRange(
                            unit,  // Pass the unit for pathfinding
                            unit.MovementPointsRemaining  // Show remaining movement, not max
                        );
                        Debug.Log($"Movement range updated - showing {unit.MovementPointsRemaining} remaining movement");
                    }
                }, path);  // Pass the path!
            }
            else
            {
                Debug.Log("Cannot move there - path blocked by enemies");
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

        /// <summary>
        /// Public property to check if a skill is currently selected.
        /// </summary>
        public bool IsSkillSelected => selectedSkill != null;

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

            bool success = SkillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
            if (success)
            {
                CancelSkillSelection();
            }
        }

        /// <summary>
        /// Public method for clicking enemies to attack them.
        /// </summary>
        public void AttemptAttackOnTarget(Unit targetUnit)
        {
            if (unit.HasActedThisTurn)
            {
                Debug.Log("You have already acted this turn!");
                return;
            }

            if (AttackAction.ExecuteMeleeAttack(unit, targetUnit))
            {
                unit.MarkAsActed();
            }
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
        }

        private void UseSelectedSkill(int targetX, int targetY, Unit targetUnit)
        {
            // Check if already acted this turn
            if (unit.HasActedThisTurn)
            {
                Debug.Log("You have already acted this turn!");
                return;
            }

            bool success = false;

            // Ground-targeted skill (Ignite Ground)
            if (selectedSkill.CreatesGroundHazard)
            {
                success = SkillExecutor.ExecuteGroundSkill(selectedSkill, unit, targetX, targetY);
            }
            // Unit-targeted skill (Spark)
            else if (targetUnit != null && targetUnit != unit)
            {
                success = SkillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
            }
            else
            {
                Debug.Log($"{selectedSkill.SkillName} requires a valid target!");
            }

            if (success)
            {
                CancelSkillSelection();
                // Don't end turn automatically - let player decide!
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
        }

    }
}