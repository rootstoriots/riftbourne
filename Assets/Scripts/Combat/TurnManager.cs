using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Core;

namespace Riftbourne.Combat
{
    public class TurnManager : MonoBehaviour
    {
        [Header("Turn Management")]
        [SerializeField] private List<Unit> allUnits = new List<Unit>();
        [SerializeField] private int currentTurnIndex = 0;

        // Flexible initiative window
        private List<Unit> currentTurnWindow = new List<Unit>();
        private HashSet<Unit> unitsFinishedInWindow = new HashSet<Unit>();

        // Public properties
        public Unit CurrentUnit { get; private set; }
        public bool IsPlayerTurn => CurrentUnit != null && CurrentUnit.IsPlayerControlled;

        /// <summary>
        /// Returns a copy of all units in combat for UI display.
        /// </summary>
        public List<Unit> GetAllUnits()
        {
            return new List<Unit>(allUnits);
        }

        /// <summary>
        /// Check if a unit can be controlled right now (is in the current turn window).
        /// </summary>
        public bool IsUnitInCurrentWindow(Unit unit)
        {
            return currentTurnWindow.Contains(unit);
        }

        /// <summary>
        /// Get all units in the current turn window.
        /// </summary>
        public List<Unit> GetCurrentTurnWindow()
        {
            return new List<Unit>(currentTurnWindow);
        }

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
            // If allUnits is already populated in Inspector, use that order
            // Otherwise, find units dynamically
            if (allUnits == null || allUnits.Count == 0)
            {
                // Use InstanceID sort for consistent order between runs
                Unit[] foundUnits = FindObjectsByType<Unit>(FindObjectsSortMode.InstanceID);
                allUnits = new List<Unit>();
                allUnits.AddRange(foundUnits);

                // Sort by Initiative (higher goes first)
                // Tiebreaker: Player-controlled units go first if initiative is tied
                allUnits.Sort((a, b) => 
                {
                    int initiativeCompare = b.Initiative.CompareTo(a.Initiative);
                    if (initiativeCompare != 0)
                        return initiativeCompare;
                    // Tied initiative - player units go first
                    return b.IsPlayerControlled.CompareTo(a.IsPlayerControlled);
                });
            }
            else
            {
                Debug.Log("Using pre-configured turn order from Inspector");
            }

            Debug.Log($"TurnManager: Turn order: {string.Join(", ", allUnits.Select(u => $"{u.UnitName}(Init:{u.Initiative})"))}");

            if (allUnits.Count > 0)
            {
                currentTurnIndex = 0;
                CurrentUnit = allUnits[currentTurnIndex];

                // Calculate initial turn window
                CalculateTurnWindow();

                // Don't reset action flags yet - let units act in any order
                Debug.Log($"Combat started! Turn window: {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))}");

                // Auto-select first player unit if it's in the window
                if (CurrentUnit.IsPlayerControlled && PartyManager.Instance != null)
                {
                    PartyManager.Instance.SelectUnit(CurrentUnit);
                }
            }
            else
            {
                Debug.LogWarning("TurnManager: No units found in scene!");
            }
        }

        /// <summary>
        /// Calculate which units are in the current turn window (consecutive allies starting from currentTurnIndex).
        /// </summary>
        private void CalculateTurnWindow()
        {
            currentTurnWindow.Clear();
            unitsFinishedInWindow.Clear();

            if (allUnits.Count == 0 || currentTurnIndex >= allUnits.Count)
                return;

            // Start with the current unit
            Unit firstUnit = allUnits[currentTurnIndex];
            if (!firstUnit.IsAlive)
            {
                // Skip dead units
                AdvanceToNextLivingUnit();
                return;
            }

            currentTurnWindow.Add(firstUnit);
            CurrentUnit = firstUnit;

            // Add consecutive allies
            bool isPlayerWindow = firstUnit.IsPlayerControlled;
            int index = currentTurnIndex + 1;

            while (index < allUnits.Count)
            {
                Unit unit = allUnits[index];

                // Stop if we hit an enemy (different team) or dead unit
                if (unit.IsPlayerControlled != isPlayerWindow)
                    break;

                if (!unit.IsAlive)
                {
                    index++;
                    continue; // Skip dead units but keep looking for more allies
                }

                currentTurnWindow.Add(unit);
                index++;
            }

            Debug.Log($"[TURN WINDOW] Calculated window: {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))}");
        }

        /// <summary>
        /// Mark that a unit has finished their turn in the current window.
        /// </summary>
        public void EndTurn()
        {
            // Get the currently selected unit (the one actually acting)
            Unit actingUnit = PartyManager.Instance?.SelectedUnit;

            if (actingUnit == null)
            {
                Debug.LogWarning("EndTurn called but no unit is selected!");
                return;
            }

            // Mark selected unit as finished
            if (!unitsFinishedInWindow.Contains(actingUnit))
            {
                unitsFinishedInWindow.Add(actingUnit);
                Debug.Log($"{actingUnit.UnitName} ended their turn. ({unitsFinishedInWindow.Count}/{currentTurnWindow.Count} finished)");
            }
            else
            {
                Debug.LogWarning($"{actingUnit.UnitName} already ended their turn!");
                return;
            }

            // Check if all units in window have finished
            if (unitsFinishedInWindow.Count >= currentTurnWindow.Count)
            {
                Debug.Log("All units in turn window finished - advancing to next window");
                AdvanceToNextWindow();
            }
            else
            {
                // Auto-select next unfinished unit in window (if player controlled)
                Unit nextUnit = GetNextUnfinishedUnitInWindow();
                if (nextUnit != null && nextUnit.IsPlayerControlled && PartyManager.Instance != null)
                {
                    PartyManager.Instance.SelectUnit(nextUnit);
                    Debug.Log($"Auto-selected next unfinished unit: {nextUnit.UnitName}");
                }
            }
        }

        /// <summary>
        /// Get the next unit in the window that hasn't finished their turn.
        /// </summary>
        private Unit GetNextUnfinishedUnitInWindow()
        {
            foreach (Unit unit in currentTurnWindow)
            {
                if (!unitsFinishedInWindow.Contains(unit) && unit.IsAlive)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// Advance to the next turn window.
        /// </summary>
        private void AdvanceToNextWindow()
        {
            // Move index past the current window
            currentTurnIndex += currentTurnWindow.Count;

            // Wrap around if needed
            if (currentTurnIndex >= allUnits.Count)
            {
                currentTurnIndex = 0;
                currentRound++;
                Debug.Log($"=== ROUND {currentRound} ===");

                // Update hazards at start of new round
                if (hazardManager != null && currentRound > lastHazardUpdateRound)
                {
                    hazardManager.UpdateHazards();
                    lastHazardUpdateRound = currentRound;
                    Debug.Log("Hazards updated (durations decremented)");
                }
            }

            // Calculate new turn window
            CalculateTurnWindow();

            // Check if combat is over
            if (IsCombatOver())
            {
                return;
            }

            // Reset action flags for all units in new window
            foreach (Unit unit in currentTurnWindow)
            {
                unit.OnTurnStart();
            }

            // If this is an enemy window, let AI control
            if (currentTurnWindow.Count > 0 && !currentTurnWindow[0].IsPlayerControlled)
            {
                ExecuteEnemyWindow();
            }
            else if (currentTurnWindow.Count > 0)
            {
                Debug.Log($"--- Player turn window: {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))} ---");
                // Auto-select first unit in player window
                if (PartyManager.Instance != null && currentTurnWindow[0].IsAlive)
                {
                    PartyManager.Instance.SelectUnit(currentTurnWindow[0]);
                }
            }
        }

        /// <summary>
        /// Execute AI turns for all enemies in the current window (sequentially).
        /// </summary>
        private void ExecuteEnemyWindow()
        {
            Debug.Log($"--- Enemy turn window: {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))} ---");
            StartCoroutine(ExecuteEnemyWindowSequentially());
        }

        /// <summary>
        /// Coroutine to execute enemy AI one at a time.
        /// </summary>
        private System.Collections.IEnumerator ExecuteEnemyWindowSequentially()
        {
            foreach (Unit enemy in currentTurnWindow)
            {
                if (!enemy.IsAlive)
                    continue;

                Debug.Log($"[ENEMY TURN] {enemy.UnitName} is acting...");
                AIController ai = enemy.GetComponent<AIController>();
                if (ai != null)
                {
                    ai.TakeTurn();
                    // Wait for AI thinking delay + action execution (1.5 seconds total)
                    yield return new WaitForSeconds(1.5f);
                }
                else
                {
                    Debug.LogWarning($"{enemy.UnitName} has no AIController!");
                }
            }

            // All enemies finished - advance to next window
            Debug.Log("[ENEMY WINDOW] All enemies finished, advancing...");
            AdvanceToNextWindow();
        }

        /// <summary>
        /// Skip to the next living unit (used when current unit is dead).
        /// </summary>
        private void AdvanceToNextLivingUnit()
        {
            int attempts = 0;
            do
            {
                currentTurnIndex = (currentTurnIndex + 1) % allUnits.Count;
                attempts++;

                if (attempts > allUnits.Count)
                {
                    Debug.LogError("No living units found!");
                    return;
                }
            }
            while (!allUnits[currentTurnIndex].IsAlive);

            CalculateTurnWindow();
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
