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
        /// Returns a copy of the turn order list for UI display.
        /// </summary>
        public List<Unit> GetTurnOrder()
        {
            return new List<Unit>(allUnits);
        }

        /// <summary>
        /// Returns the current turn index for UI tracking.
        /// </summary>
        public int GetCurrentTurnIndex()
        {
            return currentTurnIndex;
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

                // Sort by Speed (higher goes first)
                // Tiebreaker: Player-controlled units go first if speed is tied
                allUnits.Sort((a, b) =>
                {
                    int speedCompare = b.Speed.CompareTo(a.Speed);
                    if (speedCompare != 0)
                        return speedCompare;
                    // Tied speed - player units go first
                    return b.IsPlayerControlled.CompareTo(a.IsPlayerControlled);
                });
            }
            else
            {
                Debug.Log("Using pre-configured turn order from Inspector");
            }

            Debug.Log($"TurnManager: Turn order: {string.Join(", ", allUnits.Select(u => $"{u.UnitName}(Speed:{u.Speed})"))}");

            if (allUnits.Count > 0)
            {
                currentTurnIndex = 0;
                CurrentUnit = allUnits[currentTurnIndex];

                // Calculate initial turn window
                CalculateTurnWindow();

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
        /// Register a unit that was created after TurnManager initialization.
        /// Prevents race conditions where units spawn after combat starts.
        /// </summary>
        public void RegisterUnit(Unit unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("TurnManager: Attempted to register null unit!");
                return;
            }

            // Prevent duplicate registrations
            if (allUnits.Contains(unit))
            {
                Debug.LogWarning($"TurnManager: Unit {unit.UnitName} is already registered!");
                return;
            }

            // Add unit to the list
            allUnits.Add(unit);

            // Re-sort by Speed to maintain turn order
            allUnits.Sort((a, b) =>
            {
                int speedCompare = b.Speed.CompareTo(a.Speed);
                if (speedCompare != 0)
                    return speedCompare;
                // Tied speed - player units go first
                return b.IsPlayerControlled.CompareTo(a.IsPlayerControlled);
            });

            Debug.Log($"TurnManager: Registered late unit {unit.UnitName} (Speed: {unit.Speed})");

            // If combat hasn't started yet or we're in a transition, recalculate window
            if (currentTurnWindow.Count == 0)
            {
                CalculateTurnWindow();
            }
        }

        /// <summary>
        /// Calculate which units are in the current turn window (consecutive allies starting from currentTurnIndex).
        /// </summary>
        private void CalculateTurnWindow()
        {
            currentTurnWindow.Clear();

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

                // Stop if we hit an enemy (different team)
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

            // Reset action flags for all units in new window
            //foreach (Unit unit in currentTurnWindow)
            //{
            //    unit.OnTurnStart();
            //}

            Debug.Log($"[TURN WINDOW] {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))} can act");
        }

        /// <summary>
        /// End the currently selected unit's turn.
        /// Removes that unit from turn order and adds them to the end.
        /// </summary>
        /// <param name="actingUnit">The unit ending their turn. If null, falls back to PartyManager.Instance.SelectedUnit for player units.</param>
        public void EndTurn(Unit actingUnit = null)
        {
            // If no unit provided, try to get from PartyManager (for player units)
            if (actingUnit == null)
            {
                actingUnit = PartyManager.Instance?.SelectedUnit;
            }

            if (actingUnit == null)
            {
                Debug.LogWarning("EndTurn called but no unit is selected or provided!");
                return;
            }

            // Make sure this unit is actually in the current window
            if (!currentTurnWindow.Contains(actingUnit))
            {
                Debug.LogWarning($"{actingUnit.UnitName} tried to end turn but is not in current window!");
                return;
            }

            Debug.Log($"{actingUnit.UnitName} ended their turn");

            // Remove this unit from allUnits (wherever they are)
            int unitIndex = allUnits.IndexOf(actingUnit);
            if (unitIndex >= 0)
            {
                allUnits.RemoveAt(unitIndex);

                // Add to end
                allUnits.Add(actingUnit);

                // DON'T adjust currentTurnIndex at all!
                // When we remove a unit from the list and re-add to end,
                // currentTurnIndex should stay the same because:
                // - Units in the window are tracked separately
                // - We never rely on currentTurnIndex to find "next" unit
                // - We only use it to rebuild the window when it's empty
            }

            // Remove from current window
            currentTurnWindow.Remove(actingUnit);

            Debug.Log($"Turn order now: [{string.Join(", ", allUnits.Select(u => u.UnitName))}]");
            Debug.Log($"Window now: [{string.Join(", ", currentTurnWindow.Select(u => u.UnitName))}]");

            // Check if window is now empty
            if (currentTurnWindow.Count == 0)
            {
                Debug.Log("Window empty - advancing to next window");
                AdvanceToNextWindow();
            }
            else
            {
                // Window still has units - let player choose next or AI acts
                if (currentTurnWindow[0].IsPlayerControlled)
                {
                    // Auto-select first remaining unit in window
                    if (PartyManager.Instance != null)
                    {
                        PartyManager.Instance.SelectUnit(currentTurnWindow[0]);
                        Debug.Log($"Auto-selected: {currentTurnWindow[0].UnitName}");
                    }
                }
                else
                {
                    // AI window - execute next AI unit
                    ExecuteNextAIUnit();
                }
            }

            // Check combat over
            IsCombatOver();
        }

        /// <summary>
        /// Advance to the next turn window (when current window is empty).
        /// </summary>
        private void AdvanceToNextWindow()
        {
            // Current window is empty, so currentTurnIndex should be at next unit
            // But first check if we've wrapped around
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
        /// Execute AI turn for next unit in current window.
        /// </summary>
        private void ExecuteNextAIUnit()
        {
            if (currentTurnWindow.Count == 0) return;

            Unit enemy = currentTurnWindow[0];
            Debug.Log($"[ENEMY TURN] {enemy.UnitName} is acting...");

            AIController ai = enemy.GetComponent<AIController>();
            if (ai != null)
            {
                ai.TakeTurn();
                // AI will call EndTurn() when done
            }
            else
            {
                Debug.LogWarning($"{enemy.UnitName} has no AIController!");
                // Force end turn if no AI
                allUnits.Remove(enemy);
                allUnits.Add(enemy);
                currentTurnWindow.Remove(enemy);

                if (currentTurnWindow.Count == 0)
                {
                    AdvanceToNextWindow();
                }
                else
                {
                    ExecuteNextAIUnit();
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
            // Make a copy since EndTurn() modifies the list
            List<Unit> enemiesInWindow = new List<Unit>(currentTurnWindow);

            foreach (Unit enemy in enemiesInWindow)
            {
                if (!enemy.IsAlive)
                    continue;

                Debug.Log($"[ENEMY TURN] {enemy.UnitName} is acting...");
                AIController ai = enemy.GetComponent<AIController>();
                if (ai != null)
                {
                    ai.TakeTurn();
                    // Wait for AI thinking delay + action execution
                    yield return new WaitForSeconds(1.5f);
                }
                else
                {
                    Debug.LogWarning($"{enemy.UnitName} has no AIController!");
                }

                // Note: EndTurn() is called by AI or movement completion
                // Wait a bit for it to process
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("[ENEMY WINDOW] All enemies finished");
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