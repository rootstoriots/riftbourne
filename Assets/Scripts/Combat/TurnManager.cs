using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Grid;

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

        // Managers
        private HazardManager hazardManager;

        // Round tracking for hazard updates
        private int lastHazardUpdateRound = 0;
        private int currentRound = 1;

        private void Start()
        {
            hazardManager = FindFirstObjectByType<HazardManager>();
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

                // Apply burn damage at start of first turn
                CurrentUnit.OnTurnStart();

                Debug.Log($"Combat started! {CurrentUnit.UnitName}'s turn.");
            }
            else
            {
                Debug.LogWarning("TurnManager: No units found in scene!");
            }
        }

        /// <summary>
        /// End the current unit's turn and advance to the next unit.
        /// </summary>
        public void EndTurn()
        {
            Debug.Log($"{CurrentUnit.UnitName} ended their turn.");

            // Apply hazard damage to unit ending their turn (BEFORE moving to next unit)
            if (hazardManager != null && CurrentUnit.IsAlive)
            {
                hazardManager.ApplyHazardDamage(CurrentUnit, CurrentUnit.GridX, CurrentUnit.GridY);
            }

            // Check if unit died from hazard damage
            if (IsCombatOver())
            {
                return;
            }

            // Move to next unit
            do
            {
                currentTurnIndex = (currentTurnIndex + 1) % allUnits.Count;
                CurrentUnit = allUnits[currentTurnIndex];
            }
            while (!CurrentUnit.IsAlive && allUnits.Count > 0);

            // Check if we completed a full round (returned to index 0)
            if (currentTurnIndex == 0)
            {
                currentRound++;
                Debug.Log($"=== ROUND {currentRound} ===");
            }

            // Check if combat is over after finding next unit
            if (IsCombatOver())
            {
                return;
            }

            // Update hazards only ONCE per round (when we start a new round)
            if (hazardManager != null && currentRound > lastHazardUpdateRound)
            {
                hazardManager.UpdateHazards();
                lastHazardUpdateRound = currentRound;
                Debug.Log("Hazards updated (durations decremented)");
            }

            Debug.Log($"--- {CurrentUnit.UnitName}'s turn! ---");

            // Apply burn damage at start of turn
            CurrentUnit.OnTurnStart();

            // If unit died from burn, end turn immediately
            if (!CurrentUnit.IsAlive)
            {
                Debug.Log($"{CurrentUnit.UnitName} died from burn damage!");
                EndTurn();
                return;
            }

            // Auto-end turn for enemy units (until AI is implemented)
            if (!CurrentUnit.IsPlayerControlled)
            {
                Invoke(nameof(EndTurn), 0.5f);
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