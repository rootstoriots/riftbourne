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

        // Skill selection state
        private Skill selectedSkill = null;
        private bool awaitingSkillTarget = false;

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
        }

        private void HandleLeftClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
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

            // CASE 1: Using a selected skill
            if (awaitingSkillTarget && selectedSkill != null)
            {
                UseSelectedSkill(targetX, targetY, targetUnit);
                return;
            }

            // CASE 2: Clicked on enemy unit - melee attack
            if (targetUnit != null && targetUnit != unit)
            {
                if (AttackAction.ExecuteMeleeAttack(unit, targetUnit))
                {
                    turnManager.EndTurn();
                }
                return;
            }

            // CASE 3: Clicked on empty cell - move
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

        private void SelectSkill(Skill skill)
        {
            selectedSkill = skill;
            awaitingSkillTarget = true;
            Debug.Log($">>> Selected: {skill.SkillName} - Click target to use (ESC to cancel) <<<");
        }

        private void UseSelectedSkill(int targetX, int targetY, Unit targetUnit)
        {
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
                turnManager.EndTurn();
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