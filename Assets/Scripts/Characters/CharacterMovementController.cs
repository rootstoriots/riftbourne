using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Grid;
using Riftbourne.Combat;
using Riftbourne.Skills;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    public class CharacterMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Unit unit;

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
        }

        private void Update()
        {
            // Only process input during this unit's turn
            if (turnManager == null || turnManager.CurrentUnit != unit)
            {
                return;
            }

            // Check for mouse click
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Vector3 hitPoint = hit.point;
                    int targetX = Mathf.FloorToInt(hitPoint.x);
                    int targetY = Mathf.FloorToInt(hitPoint.z);

                    if (!gridManager.IsValidGridPosition(targetX, targetY))
                    {
                        return;
                    }

                    GridCell targetCell = gridManager.GetCell(targetX, targetY);
                    Unit targetUnit = targetCell.OccupyingUnit;

                    // Clicked on a unit (enemy or ally)
                    if (targetUnit != null && targetUnit != unit)
                    {
                        // Get all available skills (from equipment + mastered)
                        List<Skill> availableSkills = unit.GetAvailableSkills();

                        // Try to use first available skill
                        if (availableSkills.Count > 0)
                        {
                            Skill firstSkill = availableSkills[0];

                            if (SkillExecutor.ExecuteSkill(firstSkill, unit, targetUnit))
                            {
                                // Skill cast successful - end turn
                                turnManager.EndTurn();
                                return;
                            }
                        }

                        // Fall back to melee attack if skill failed or no skills
                        if (AttackAction.ExecuteMeleeAttack(unit, targetUnit))
                        {
                            turnManager.EndTurn();
                        }
                    }
                    else
                    {
                        // Clicked on empty cell - try to move there
                        if (unit.CanMoveTo(targetX, targetY))
                        {
                            Vector3 targetWorldPos = targetCell.WorldPosition;
                            unit.MoveTo(targetX, targetY, targetWorldPos);
                            turnManager.EndTurn();
                        }
                        else
                        {
                            Debug.Log("Cannot move there - out of range or cell occupied");
                        }
                    }
                }
            }
        }

        private void HandleMouseClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if we clicked on another unit
                Unit clickedUnit = hit.collider.GetComponent<Unit>();
                if (clickedUnit != null && clickedUnit != unit)
                {
                    // Clicked on another unit - try to attack
                    TryAttackUnit(clickedUnit);
                    return;
                }

                // Otherwise, try to move to the clicked position
                Vector3 hitPoint = hit.point;
                int targetX = Mathf.FloorToInt(hitPoint.x);
                int targetY = Mathf.FloorToInt(hitPoint.z);

                if (gridManager.IsValidGridPosition(targetX, targetY))
                {
                    gridManager.SelectCell(targetX, targetY);
                    TryMoveToSelectedCell();
                }
            }
        }

        private void TryMoveToSelectedCell()
        {
            GridCell selectedCell = gridManager.GetSelectedCell();
            if (selectedCell == null) return;

            // Check if unit can move to this cell (within range and not moving)
            if (unit.CanMoveTo(selectedCell.X, selectedCell.Y))
            {
                Vector3 targetWorldPos = new Vector3(selectedCell.X + 0.5f, 0.5f, selectedCell.Y + 0.5f);
                unit.MoveTo(selectedCell.X, selectedCell.Y, targetWorldPos);

                // After moving, end turn
                if (turnManager != null)
                {
                    turnManager.EndTurn();
                }
            }
            else
            {
                Debug.Log("Cannot move to that cell (out of range or already moving)");
            }
        }

        private void TryAttackUnit(Unit target)
        {
            bool attackSuccessful = AttackAction.ExecuteMeleeAttack(unit, target);

            if (attackSuccessful)
            {
                // After attacking, end turn
                if (turnManager != null)
                {
                    turnManager.EndTurn();
                }
            }
        }
    }
}