using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;

namespace Riftbourne.Combat
{
    public class TurnManager : MonoBehaviour
    {
        [Header("Turn Management")]
        [SerializeField] private List<Unit> allUnits = new List<Unit>();
        [SerializeField] private int currentTurnIndex = 0;

        // Public properties
        public Unit CurrentUnit { get; private set; }
        public bool IsPlayerTurn => CurrentUnit != null && CurrentUnit.IsPlayerControlled;

        private void Start()
        {
            InitializeCombat();
        }

        /// <summary>
        /// Find all units in scene and set up turn order.
        /// </summary>
        private void InitializeCombat()
        {
            // Find all units in the scene
            Unit[] foundUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            allUnits.Clear();
            allUnits.AddRange(foundUnits);

            // Sort so player-controlled units go first
            allUnits.Sort((a, b) => b.IsPlayerControlled.CompareTo(a.IsPlayerControlled));

            Debug.Log($"TurnManager: Found {allUnits.Count} units in scene");

            if (allUnits.Count > 0)
            {
                currentTurnIndex = 0;
                CurrentUnit = allUnits[currentTurnIndex];
                Debug.Log($"Combat started! {CurrentUnit.UnitName}'s turn.");
            }
            else
            {
                Debug.LogWarning("TurnManager: No units found in scene!");
            }
        }

        /// <summary>
        /// End current unit's turn and advance to next unit.
        /// </summary>
        public void EndTurn()
        {
            if (allUnits.Count == 0) return;

            Debug.Log($"{CurrentUnit.UnitName} ended their turn.");

            // Check if combat is over before advancing turn
            if (IsCombatOver())
            {
                return; // Stop processing turns
            }

            // Move to next unit, loop back to start if at end
            currentTurnIndex = (currentTurnIndex + 1) % allUnits.Count;
            CurrentUnit = allUnits[currentTurnIndex];

            Debug.Log($"{CurrentUnit.UnitName}'s turn!");

            // Skip dead units
            if (!CurrentUnit.IsAlive)
            {
                Debug.Log($"{CurrentUnit.UnitName} is defeated, skipping turn.");
                EndTurn(); // Recursive call to skip to next alive unit
                return;
            }

            // Auto-end enemy turns for now (no AI yet)
            if (!CurrentUnit.IsPlayerControlled)
            {
                Debug.Log($"{CurrentUnit.UnitName} (AI) automatically ends turn.");
                Invoke(nameof(EndTurn), 0.5f); // Wait half second then end turn
            }
        }

        /// <summary>
        /// Check if combat is over (only one team remaining).
        /// </summary>
        public bool IsCombatOver()
        {
            int alivePlayerUnits = 0;
            int aliveEnemyUnits = 0;

            foreach (Unit unit in allUnits)
            {
                if (unit.IsAlive)
                {
                    if (unit.IsPlayerControlled)
                        alivePlayerUnits++;
                    else
                        aliveEnemyUnits++;
                }
            }

            if (alivePlayerUnits == 0)
            {
                Debug.Log("Combat Over: Player defeated!");
                return true;
            }

            if (aliveEnemyUnits == 0)
            {
                Debug.Log("Combat Over: Player victorious!");
                return true;
            }

            return false;
        }
    }
}