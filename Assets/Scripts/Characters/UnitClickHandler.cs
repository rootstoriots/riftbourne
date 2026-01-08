using UnityEngine;
using Riftbourne.Core;
using Riftbourne.Combat;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles mouse clicks on units in the 3D world.
    /// - Click player units to select them for control
    /// - Click enemy units to target them for attacks/skills
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class UnitClickHandler : MonoBehaviour
    {
        private Unit unit;
        private CharacterMovementController movementController;
        private SkillTargetingController skillTargetingController;
        private TurnManager turnManager;

        private void Awake()
        {
            unit = GetComponent<Unit>();
            turnManager = ManagerRegistry.Get<TurnManager>();
            
            // Ensure this unit has a collider for raycasting
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"{unit.UnitName} has UnitClickHandler but no collider! Adding CapsuleCollider.");
                gameObject.AddComponent<CapsuleCollider>();
            }
        }

        private void OnMouseDown()
        {
            if (unit == null) return;

            // If this is a player faction unit, select it for control
            if (unit.Faction == Faction.Player)
            {
                HandlePlayerUnitClick();
            }
            // If this is an enemy unit, target it for attack/skill
            else
            {
                HandleEnemyUnitClick();
            }
        }

        /// <summary>
        /// Handle clicking on a player-controlled unit.
        /// Selects the unit for control if it's in the current turn window.
        /// </summary>
        private void HandlePlayerUnitClick()
        {
            // Check if this unit is in the current turn window
            if (turnManager != null && !turnManager.IsUnitInCurrentWindow(unit))
            {
                Debug.Log($"Cannot select {unit.UnitName} - not in current turn window");
                return;
            }

            // Select this unit via PartyManager
            if (PartyManager.Instance != null)
            {
                PartyManager.Instance.SelectUnit(unit);
                Debug.Log($"Selected {unit.UnitName} by clicking in world");
            }
        }

        /// <summary>
        /// Handle clicking on an enemy unit.
        /// Attempts to target the enemy for attack or skill use.
        /// </summary>
        private void HandleEnemyUnitClick()
        {
            // Get the currently selected player unit
            Unit selectedUnit = PartyManager.Instance?.SelectedUnit;
            if (selectedUnit == null)
            {
                Debug.Log("No player unit selected - cannot target enemy");
                return;
            }

            // Get the movement controller and skill targeting controller for the selected unit
            movementController = selectedUnit.GetComponent<CharacterMovementController>();
            skillTargetingController = selectedUnit.GetComponent<SkillTargetingController>();
            
            if (movementController == null)
            {
                Debug.LogWarning($"{selectedUnit.UnitName} has no CharacterMovementController!");
                return;
            }

            // Check if a skill is selected (via SkillTargetingController)
            if (skillTargetingController != null && skillTargetingController.AwaitingSkillTarget)
            {
                // Use skill on this enemy
                skillTargetingController.UseSkillOnTarget(unit);
                Debug.Log($"Using skill on {unit.UnitName}");
            }
            else
            {
                // Attempt melee attack on this enemy
                movementController.AttemptAttackOnTarget(unit);
                Debug.Log($"Attempting to attack {unit.UnitName}");
            }
        }
    }
}
