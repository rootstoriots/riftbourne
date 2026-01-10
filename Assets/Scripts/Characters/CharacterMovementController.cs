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

        // Skill targeting controller reference
        private SkillTargetingController skillTargetingController;

        // Camera service reference
        private CameraService cameraService;

        private void Awake()
        {
            unit = GetComponent<Unit>();
            skillTargetingController = GetComponent<SkillTargetingController>();
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
                // Only log warning once per frame to avoid spam
                return;
            }

            // LEFT CLICK - Movement, melee attack, or skill targeting
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Debug.Log($"[MOVEMENT] {unit.UnitName} handling left click for movement/attack");
                HandleLeftClick();
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
            // Get camera - use CameraService if available, otherwise fallback to Camera.main
            Camera cam = null;
            if (cameraService != null && cameraService.MainCamera != null)
            {
                cam = cameraService.MainCamera;
            }
            else
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                Debug.LogWarning("No main camera found!");
                return;
            }

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // Raycast with a longer distance to ensure we hit something
            if (!Physics.Raycast(ray, out hit, 100f))
            {
                Debug.Log("[MOVEMENT] Raycast didn't hit anything");
                return;
            }

            Debug.Log($"[MOVEMENT] Raycast hit: {hit.collider.name} at {hit.point}");

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

            // CASE 1: Using a selected skill (delegate to SkillTargetingController)
            if (skillTargetingController != null && skillTargetingController.AwaitingSkillTarget)
            {
                skillTargetingController.HandleSkillTargeting(targetX, targetY, targetUnit);
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

                AttackAction attackAction = ManagerRegistry.Get<AttackAction>();
                // Fallback to Instance property which will auto-create if needed
                if (attackAction == null)
                {
                    attackAction = AttackAction.Instance;
                }
                
                if (attackAction == null)
                {
                    Debug.LogWarning("AttackAction is null!");
                    return;
                }

                // Check if unit has ranged weapon and target is in range
                bool hasRangedWeapon = unit.AttackRange > 1;
                int dx = Mathf.Abs(unit.GridX - targetUnit.GridX);
                int dy = Mathf.Abs(unit.GridY - targetUnit.GridY);
                int attackDistance = Mathf.Max(dx, dy); // Chebyshev distance
                bool inRangedRange = attackDistance <= unit.AttackRange;
                bool isAdjacent = attackDistance == 1;

                // Try ranged attack if unit has ranged weapon and target is in range
                if (hasRangedWeapon && inRangedRange)
                {
                    if (attackAction.ExecuteRangedAttack(unit, targetUnit))
                    {
                        // Attack succeeded - MarkAsActed is already called by AttackAction
                        return;
                    }
                    // Ranged attack failed for some reason, fall through to melee attempt
                }

                // Try melee attack if adjacent
                if (isAdjacent)
                {
                    if (attackAction.ExecuteMeleeAttack(unit, targetUnit))
                    {
                        // Attack succeeded - MarkAsActed is already called by AttackAction
                        return;
                    }
                }

                // Attack failed - show appropriate message
                if (hasRangedWeapon && !inRangedRange)
                {
                    Debug.Log($"Cannot attack {targetUnit.UnitName} - out of range! Distance: {attackDistance}, Range: {unit.AttackRange}");
                }
                else if (!isAdjacent)
                {
                    Debug.Log($"Cannot attack {targetUnit.UnitName} - must be adjacent!");
                }
                else
                {
                    Debug.Log($"Cannot attack {targetUnit.UnitName}!");
                }
                return;
            }

            // CASE 3: Clicked on empty cell - move
            // Validate unit's current grid position before attempting movement
            if (!gridManager.IsValidGridPosition(unit.GridX, unit.GridY))
            {
                Debug.LogError($"Cannot move {unit.UnitName} - unit has invalid grid position ({unit.GridX}, {unit.GridY})! World position: {unit.transform.position}");
                return;
            }
            
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
                
                // Calculate movement cost based on actual path length
                // Path includes start and end cells, so cost is path.Count - 1 (don't count start cell)
                // Example: Moving from (0,0) to (2,0) gives path [start(0,0), (1,0), end(2,0)] = 3 cells, cost = 2
                int movementCost = path.Count > 0 ? path.Count - 1 : 0;
                
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
                    // CRITICAL: Always spend movement points, even if unit is in an invalid state
                    // This prevents movement points from getting "locked" when movement is interrupted
                    if (unit != null && unit.IsAlive)
                    {
                        bool pointsSpent = unit.SpendMovementPoints(movementCost);
                        if (!pointsSpent)
                        {
                            // If normal spending failed (e.g., unit acted during movement), force-spend to prevent infinite movement bug
                            Debug.LogWarning($"{unit.UnitName} movement completed but SpendMovementPoints returned false! Force-spending {movementCost} points to prevent infinite movement bug.");
                            unit.ForceSpendMovementPoints(movementCost);
                        }
                        
                        // Re-show movement range at new position with remaining points
                        if (gridManager != null && unit.MovementPointsRemaining > 0)
                        {
                            gridManager.ShowMovementRange(
                                unit,  // Pass the unit for pathfinding
                                unit.MovementPointsRemaining  // Show remaining movement, not max
                            );
                            Debug.Log($"Movement range updated - showing {unit.MovementPointsRemaining} remaining movement");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{unit?.UnitName ?? "NULL"} movement completed but unit is null or dead - cannot spend movement points!");
                    }
                }, path);  // Pass the path!
            }
            else
            {
                Debug.Log("Cannot move there - path blocked by enemies");
            }
        }

        /// <summary>
        /// Public method for clicking enemies to attack them.
        /// Tries ranged attack first if available, then melee.
        /// </summary>
        public void AttemptAttackOnTarget(Unit targetUnit)
        {
            if (unit.HasActedThisTurn)
            {
                Debug.Log("You have already acted this turn!");
                return;
            }

            AttackAction attackAction = ManagerRegistry.Get<AttackAction>();
            // Fallback to Instance property which will auto-create if needed
            if (attackAction == null)
            {
                attackAction = AttackAction.Instance;
            }
            
            if (attackAction == null)
            {
                Debug.LogWarning("AttackAction is null!");
                return;
            }

            // Check if unit has ranged weapon and target is in range
            bool hasRangedWeapon = unit.AttackRange > 1;
            int dx = Mathf.Abs(unit.GridX - targetUnit.GridX);
            int dy = Mathf.Abs(unit.GridY - targetUnit.GridY);
            int distance = Mathf.Max(dx, dy); // Chebyshev distance
            bool inRangedRange = distance <= unit.AttackRange;
            bool isAdjacent = distance == 1;

            // Try ranged attack if unit has ranged weapon and target is in range
            if (hasRangedWeapon && inRangedRange)
            {
                if (attackAction.ExecuteRangedAttack(unit, targetUnit))
                {
                    // Attack succeeded - MarkAsActed is already called by AttackAction
                    return;
                }
            }

            // Try melee attack if adjacent
            if (isAdjacent && attackAction.ExecuteMeleeAttack(unit, targetUnit))
            {
                // Attack succeeded - MarkAsActed is already called by AttackAction
                return;
            }

            // Attack failed
            Debug.Log($"Cannot attack {targetUnit.UnitName}!");
        }

    }
}