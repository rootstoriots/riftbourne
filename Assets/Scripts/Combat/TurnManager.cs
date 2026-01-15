using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Core;

namespace Riftbourne.Combat
{
    public class TurnManager : MonoBehaviour
    {
        private static TurnManager instance;
        public static TurnManager Instance => instance;
        
        [Header("Turn Management")]
        [SerializeField] private List<Unit> allUnits = new List<Unit>();
        [SerializeField] private int currentTurnIndex = 0;

        // Flexible initiative window
        private List<Unit> currentTurnWindow = new List<Unit>();

        // Events
        /// <summary>
        /// Event raised when the current turn window changes (units that can act).
        /// </summary>
        public event Action<List<Unit>> OnTurnWindowChanged;

        /// <summary>
        /// Event raised when the current unit changes.
        /// </summary>
        public event Action<Unit> OnCurrentUnitChanged;

        /// <summary>
        /// Event raised when a unit ends their turn.
        /// </summary>
        public event Action<Unit> OnUnitTurnEnded;

        // Public properties
        public Unit CurrentUnit { get; private set; }
        public bool IsPlayerTurn => CurrentUnit != null && CurrentUnit.IsPlayerControlled;
        public bool IsInCombat => combatInitialized && !IsCombatOver();

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
        
        // Encounter data for win condition checking
        private EncounterData currentEncounter;

        // Coroutine tracking to prevent multiple enemy window coroutines
        private Coroutine currentEnemyWindowCoroutine;

        [Header("Initialization Settings")]
        [Tooltip("If true, automatically initializes combat on Start (for backward compatibility)")]
        [SerializeField] private bool autoInitializeOnStart = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            ManagerRegistry.Register(this);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            GameEvents.OnUnitDied -= HandleUnitDied;
            
            // Cancel any running enemy window coroutine
            if (currentEnemyWindowCoroutine != null)
            {
                StopCoroutine(currentEnemyWindowCoroutine);
                currentEnemyWindowCoroutine = null;
            }
            
            if (instance == this)
            {
                instance = null;
            }
            
            ManagerRegistry.Unregister(this);
        }

        private void Start()
        {
            hazardManager = ManagerRegistry.Get<HazardManager>();
            if (hazardManager == null)
            {
                hazardManager = FindFirstObjectByType<HazardManager>();
                if (hazardManager == null)
                {
                    Debug.LogError("TurnManager: HazardManager not found in scene! Make sure HazardManager GameObject exists.");
                }
            }
            
            // Subscribe to unit death events
            GameEvents.OnUnitDied += HandleUnitDied;
            
            // Ensure all factions are registered before initializing combat
            RegisterAllFactionsFromUnits();
            
            // Only auto-initialize if flag is set (for backward compatibility)
            if (autoInitializeOnStart)
            {
                // Delay combat initialization to ensure all units are fully created and initialized
                // This is especially important when units are created dynamically (e.g., from CharacterState)
                // Wait one frame to allow BattleSceneInitializer and unit Start() methods to complete
                StartCoroutine(DelayedCombatInitialization());
            }
        }
        
        /// <summary>
        /// Delay combat initialization to ensure all units are fully created and initialized.
        /// This fixes issues where units created from CharacterState aren't ready when TurnManager initializes.
        /// </summary>
        private System.Collections.IEnumerator DelayedCombatInitialization()
        {
            // Wait multiple frames to ensure all Start() methods have run and units have registered themselves
            yield return null;
            yield return null;
            
            // Additional delay to ensure unit components (unitStats, unitEquipment) are fully initialized
            yield return new WaitForSeconds(0.2f);
            
            // Check if units have registered themselves - if so, use that list
            // Otherwise, find units manually
            if (allUnits.Count == 0)
            {
                Debug.Log("TurnManager: No units registered yet, finding units manually...");
                InitializeCombat();
            }
            else
            {
                Debug.Log($"TurnManager: {allUnits.Count} units already registered. Re-sorting by speed...");
                // Re-sort the registered units to ensure correct order
                ReSortUnitsBySpeed();
                // Initialize combat with the registered units
                FinalizeCombatInitialization();
            }
        }
        
        /// <summary>
        /// Handle unit death - check for victory if enemy died.
        /// </summary>
        private void HandleUnitDied(Unit unit)
        {
            if (unit == null) return;
            
            // If an enemy died, process loot and check if combat is over
            if (unit.Faction != Faction.Player)
            {
                Debug.Log($"TurnManager: Enemy {unit.UnitName} died. Checking if combat is over...");
                
                // Process loot from enemy using EnemyLoot component
                if (LootManager.Instance != null)
                {
                    EnemyLoot enemyLoot = unit.GetComponent<EnemyLoot>();
                    if (enemyLoot != null)
                    {
                        LootData lootData = enemyLoot.GenerateLoot();
                        LootManager.Instance.AddLoot(lootData);
                    }
                    else
                    {
                        Debug.LogWarning($"TurnManager: Enemy {unit.UnitName} died but has no EnemyLoot component. No loot will be dropped.");
                    }
                }
                
                // Small delay to ensure unit is properly removed from lists
                StartCoroutine(CheckCombatOverAfterDeath());
            }
        }
        
        /// <summary>
        /// Coroutine to check if combat is over after a unit death.
        /// Small delay ensures the unit is properly removed from lists before checking.
        /// </summary>
        private System.Collections.IEnumerator CheckCombatOverAfterDeath()
        {
            yield return new WaitForSeconds(0.1f);
            IsCombatOver();
        }
        
        /// <summary>
        /// Re-sort all units by speed. Called after all units have registered themselves.
        /// </summary>
        private void ReSortUnitsBySpeed()
        {
            if (allUnits == null || allUnits.Count == 0) return;
            
            // Log unit speeds before sorting for debugging
            Debug.Log($"TurnManager: Re-sorting {allUnits.Count} units by speed:");
            foreach (var unit in allUnits)
            {
                int equipmentBonus = unit.UnitEquipment != null ? unit.UnitEquipment.GetTotalEquipmentBonus(StatType.Speed) : 0;
                Debug.Log($"  - {unit.UnitName}: Speed = {unit.Speed} (Equipment bonus: {equipmentBonus})");
            }
            
            // Sort by Speed (higher goes first)
            // Tiebreaker: Player faction goes first if speed is tied
            allUnits.Sort((a, b) =>
            {
                int speedA = a.Speed;
                int speedB = b.Speed;
                int speedCompare = speedB.CompareTo(speedA);
                if (speedCompare != 0)
                    return speedCompare;
                // Tied speed - player faction goes first
                if (a.Faction == Faction.Player && b.Faction != Faction.Player)
                    return -1;
                if (a.Faction != Faction.Player && b.Faction == Faction.Player)
                    return 1;
                return 0;
            });
            
            Debug.Log($"TurnManager: Re-sorted turn order: {string.Join(", ", allUnits.Select(u => $"{u.UnitName}(Speed:{u.Speed})"))}");
        }
        
        /// <summary>
        /// Finalize combat initialization after units are sorted.
        /// </summary>
        private void FinalizeCombatInitialization()
        {
            combatInitialized = true;
            
            if (allUnits.Count > 0)
            {
                currentTurnIndex = 0;
                CurrentUnit = allUnits[currentTurnIndex];

                // Calculate initial turn window
                CalculateTurnWindow();

                Debug.Log($"Combat started! Turn window: {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))}");

                // Handle initial turn based on faction
                if (currentTurnWindow.Count > 0)
                {
                    if (CurrentUnit.Faction == Faction.Player)
                    {
                        // Player turn - auto-select first unit
                        if (PartyManager.Instance != null)
                        {
                            PartyManager.Instance.SelectUnit(CurrentUnit);
                        }
                        Debug.Log($"--- Player turn window: {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))} ---");
                    }
                    else
                    {
                        // Enemy turn - execute AI
                        ExecuteEnemyWindow();
                    }
                }
            }
            else
            {
                Debug.LogWarning("TurnManager: No units found in scene!");
            }
        }

        /// <summary>
        /// Register all faction data from units in the scene to ensure proper initialization.
        /// This prevents premature combat end due to unregistered factions.
        /// </summary>
        private void RegisterAllFactionsFromUnits()
        {
            FactionRegistry registry = Resources.Load<FactionRegistry>("FactionRegistry");
            if (registry == null)
            {
                Debug.LogWarning("TurnManager: FactionRegistry not found in Resources! Factions may not be properly initialized.");
                return;
            }

            // Find all units and register their factions
            Unit[] allUnitsInScene = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            HashSet<FactionData> factionsToRegister = new HashSet<FactionData>();

            foreach (Unit unit in allUnitsInScene)
            {
                if (unit != null && unit.FactionData != null)
                {
                    factionsToRegister.Add(unit.FactionData);
                }
            }

            // Register all unique factions
            foreach (var faction in factionsToRegister)
            {
                registry.RegisterFaction(faction);
            }

            // Build lookup to ensure enum mappings are created
            registry.BuildLookup();

            // Apply relationships to FactionRelationship component
            FactionRelationship factionRel = FactionRelationship.Instance ?? FindFirstObjectByType<FactionRelationship>();
            if (factionRel != null)
            {
                registry.ApplyRelationshipsTo(factionRel);
            }

            Debug.Log($"TurnManager: Registered {factionsToRegister.Count} factions from units in scene.");
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

                // Log unit speeds before sorting for debugging
                Debug.Log($"TurnManager: Found {allUnits.Count} units. Speeds before sorting:");
                foreach (var unit in allUnits)
                {
                    int equipmentBonus = unit.UnitEquipment != null ? unit.UnitEquipment.GetTotalEquipmentBonus(StatType.Speed) : 0;
                    Debug.Log($"  - {unit.UnitName}: Speed = {unit.Speed} (Equipment bonus: {equipmentBonus})");
                }

                // Sort by Speed (higher goes first)
                // Tiebreaker: Player faction goes first if speed is tied
                allUnits.Sort((a, b) =>
                {
                    int speedA = a.Speed;
                    int speedB = b.Speed;
                    int speedCompare = speedB.CompareTo(speedA);
                    if (speedCompare != 0)
                        return speedCompare;
                    // Tied speed - player faction goes first
                    if (a.Faction == Faction.Player && b.Faction != Faction.Player)
                        return -1;
                    if (a.Faction != Faction.Player && b.Faction == Faction.Player)
                        return 1;
                    return 0;
                });
            }
            else
            {
                Debug.Log("Using pre-configured turn order from Inspector");
            }

            Debug.Log($"TurnManager: Turn order: {string.Join(", ", allUnits.Select(u => $"{u.UnitName}(Speed:{u.Speed})"))}");

            FinalizeCombatInitialization();
        }

        /// <summary>
        /// Unregister a unit from turn management.
        /// </summary>
        public void UnregisterUnit(Unit unit)
        {
            if (unit == null) return;

            int unitIndex = allUnits.IndexOf(unit);
            if (unitIndex >= 0)
            {
                allUnits.RemoveAt(unitIndex);
                Debug.Log($"TurnManager: Unregistered {unit.UnitName} (was at index {unitIndex}, currentTurnIndex: {currentTurnIndex})");
                
                // Adjust currentTurnIndex if the removed unit was before or at the current index
                if (unitIndex <= currentTurnIndex)
                {
                    // If we removed the unit at currentTurnIndex, the next unit is now at that position
                    // So we don't need to increment - currentTurnIndex already points to the next unit
                    // But if we removed a unit before currentTurnIndex, we need to decrement
                    if (unitIndex < currentTurnIndex)
                    {
                        currentTurnIndex--;
                    }
                    // If unitIndex == currentTurnIndex, currentTurnIndex now points to the next unit (correct)
                }
                
                // Remove from current window if present
                currentTurnWindow.Remove(unit);
                
                // If current unit was removed, advance to next
                if (CurrentUnit == unit)
                {
                    CurrentUnit = null;
                    if (currentTurnWindow.Count > 0)
                    {
                        CurrentUnit = currentTurnWindow[0];
                    }
                    else
                    {
                        AdvanceToNextWindow();
                    }
                }
            }
        }

        private bool combatInitialized = false;

        /// <summary>
        /// Initialize combat with a specific list of units.
        /// Clears existing state and sets up combat with the provided units.
        /// </summary>
        public void InitializeCombat(List<Unit> allUnits)
        {
            InitializeCombat(allUnits, null);
        }
        
        /// <summary>
        /// Initialize combat with a specific list of units and encounter data.
        /// Clears existing state and sets up combat with the provided units.
        /// </summary>
        public void InitializeCombat(List<Unit> allUnits, EncounterData encounter)
        {
            if (allUnits == null)
            {
                Debug.LogError("TurnManager.InitializeCombat: allUnits list is null!");
                return;
            }

            currentEncounter = encounter;

            // Clear existing state
            this.allUnits.Clear();
            currentTurnWindow.Clear();
            currentTurnIndex = 0;
            currentRound = 1;
            CurrentUnit = null;
            combatInitialized = false;

            // Set new unit list
            this.allUnits = new List<Unit>(allUnits);

            Debug.Log($"TurnManager.InitializeCombat: Initializing with {this.allUnits.Count} units");

            // Ensure all factions are registered
            RegisterAllFactionsFromUnits();

            // Sort units by speed (using existing logic)
            ReSortUnitsBySpeed();

            // Finalize initialization
            FinalizeCombatInitialization();
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

            Debug.Log($"TurnManager: Registered unit {unit.UnitName} (Speed: {unit.Speed}). Total units: {allUnits.Count}");

            // If combat hasn't been initialized yet, don't sort - wait for DelayedCombatInitialization
            // If combat is already initialized, re-sort immediately
            if (combatInitialized)
            {
                // Re-sort by Speed to maintain turn order
                ReSortUnitsBySpeed();
                
                // Recalculate window if needed
                if (currentTurnWindow.Count == 0)
                {
                    CalculateTurnWindow();
                }
            }
            // Otherwise, DelayedCombatInitialization will handle the sorting
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

            // Add consecutive allies (same faction)
            Faction windowFaction = firstUnit.Faction;
            int index = currentTurnIndex + 1;

            while (index < allUnits.Count)
            {
                Unit unit = allUnits[index];

                // Stop if we hit a different faction
                if (unit.Faction != windowFaction)
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
            // This resets movement points and action flags at the start of their turn window
            // Only call OnTurnStart if the unit is not currently moving (to avoid interfering with movement)
            foreach (Unit unit in currentTurnWindow)
            {
                if (!unit.IsMoving)
                {
                    unit.OnTurnStart();
                }
                else
                {
                    // Unit is still moving - delay OnTurnStart until movement completes
                    // This prevents interference with ongoing movement when rounds wrap
                    Debug.Log($"[TURN WINDOW] {unit.UnitName} is still moving, will reset on turn start after movement completes");
                    // Start coroutine to call OnTurnStart when movement completes
                    StartCoroutine(DelayedOnTurnStart(unit));
                }
            }

            Debug.Log($"[TURN WINDOW] {string.Join(", ", currentTurnWindow.Select(u => u.UnitName))} can act");

            // Raise events (both local and global)
            var windowCopy = new List<Unit>(currentTurnWindow);
            OnTurnWindowChanged?.Invoke(windowCopy);
            GameEvents.RaiseTurnWindowChanged(windowCopy);
            OnCurrentUnitChanged?.Invoke(CurrentUnit);
            GameEvents.RaiseCurrentUnitChanged(CurrentUnit);
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
                if (PartyManager.Instance == null)
                {
                    Debug.LogWarning("EndTurn called but PartyManager.Instance is null!");
                    return;
                }
                actingUnit = PartyManager.Instance.SelectedUnit;
            }

            if (actingUnit == null)
            {
                Debug.LogWarning("EndTurn called but no unit is selected or provided!");
                return;
            }

            // Make sure this unit is actually in the current window
            if (currentTurnWindow == null || !currentTurnWindow.Contains(actingUnit))
            {
                Debug.LogWarning($"{actingUnit.UnitName} tried to end turn but is not in current window!");
                return;
            }

            Debug.Log($"{actingUnit.UnitName} ended their turn");

            // Apply hazard damage if unit is ending their turn on a hazard
            // NOTE: This applies to ALL units (both player and enemy/AI units)
            if (hazardManager == null)
            {
                Debug.LogWarning($"[TURN END] HazardManager is null, cannot check for hazards for {actingUnit.UnitName}");
            }
            else
            {
                GridManager gridManager = ManagerRegistry.Get<GridManager>();
                if (gridManager == null)
                {
                    Debug.LogWarning($"[TURN END] GridManager is null, cannot check for hazards for {actingUnit.UnitName}");
                }
                else if (!gridManager.IsValidGridPosition(actingUnit.GridX, actingUnit.GridY))
                {
                    Debug.LogWarning($"[TURN END] {actingUnit.UnitName} has invalid grid position ({actingUnit.GridX}, {actingUnit.GridY})");
                }
                else
                {
                    Debug.Log($"[TURN END] {actingUnit.UnitName} checking for hazards at ({actingUnit.GridX}, {actingUnit.GridY})");
                    GridCell currentCell = gridManager.GetCell(actingUnit.GridX, actingUnit.GridY);
                    if (currentCell == null)
                    {
                        Debug.LogWarning($"[TURN END] {actingUnit.UnitName} - Cell at ({actingUnit.GridX}, {actingUnit.GridY}) is null!");
                    }
                    else if (currentCell.Hazard == null)
                    {
                        Debug.Log($"[TURN END] {actingUnit.UnitName} at ({actingUnit.GridX}, {actingUnit.GridY}) - no hazard on cell");
                    }
                    else
                    {
                        Debug.Log($"[TURN END] {actingUnit.UnitName} ending turn on {currentCell.Hazard.Type} hazard at ({actingUnit.GridX}, {actingUnit.GridY})");
                        hazardManager.ApplyHazardDamageToUnit(actingUnit, currentCell);
                    }
                }
            }

            // Raise event for unit ending turn (both local and global)
            OnUnitTurnEnded?.Invoke(actingUnit);
            GameEvents.RaiseUnitTurnEnded(actingUnit);

            // Remove this unit from allUnits (wherever they are)
            int unitIndex = allUnits.IndexOf(actingUnit);
            if (unitIndex >= 0)
            {
                allUnits.RemoveAt(unitIndex);

                // Add to end
                allUnits.Add(actingUnit);

                // Adjust currentTurnIndex if the removed unit was before or at the current index
                // If we removed a unit at or before currentTurnIndex, we need to adjust
                // because all units after it shifted left by one position
                if (unitIndex <= currentTurnIndex)
                {
                    // If we removed the unit at currentTurnIndex, the next unit is now at that position
                    // So we don't need to increment - currentTurnIndex already points to the next unit
                    // But if we removed a unit before currentTurnIndex, we need to decrement
                    if (unitIndex < currentTurnIndex)
                    {
                        currentTurnIndex--;
                    }
                    // If unitIndex == currentTurnIndex, currentTurnIndex now points to the next unit (correct)
                }
            }

            // Remove from current window
            currentTurnWindow.Remove(actingUnit);

            Debug.Log($"Turn order now: [{string.Join(", ", allUnits.Select(u => u.UnitName))}]");
            Debug.Log($"Window now: [{string.Join(", ", currentTurnWindow.Select(u => u.UnitName))}]");
            Debug.Log($"Current turn index: {currentTurnIndex} (points to: {(currentTurnIndex < allUnits.Count ? allUnits[currentTurnIndex].UnitName : "OUT OF BOUNDS")})");

            // Check if window is now empty
            if (currentTurnWindow.Count == 0)
            {
                Debug.Log("Window empty - advancing to next window");
                // currentTurnIndex already points to the next unit (we adjusted it above)
                // So we can directly advance to the next window without incrementing
                AdvanceToNextWindow();
                // Note: AdvanceToNextWindow() will call CalculateTurnWindow() which raises OnTurnWindowChanged
                // So we don't need to raise the event here with an empty window
            }
            else
            {
                // Window still has units - raise event for window change (unit removed) - both local and global
                var windowCopy = new List<Unit>(currentTurnWindow);
                OnTurnWindowChanged?.Invoke(windowCopy);
                GameEvents.RaiseTurnWindowChanged(windowCopy);
                
                // Let player choose next or AI acts
                if (currentTurnWindow[0].Faction == Faction.Player)
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
            // Current window is empty, so advance to next unit
            // Check if we've wrapped around to a new round
            if (currentTurnIndex >= allUnits.Count)
            {
                currentTurnIndex = 0;
                currentRound++;
                Debug.Log($"=== ROUND {currentRound} ===");

                // Update hazards at start of new round
                if (currentRound > lastHazardUpdateRound)
                {
                    // Ensure hazardManager is available
                    if (hazardManager == null)
                    {
                        hazardManager = ManagerRegistry.Get<HazardManager>();
                        if (hazardManager == null)
                        {
                            hazardManager = FindFirstObjectByType<HazardManager>();
                        }
                    }
                    
                    if (hazardManager != null)
                    {
                        hazardManager.UpdateHazards();
                        lastHazardUpdateRound = currentRound;
                        Debug.Log("Hazards updated (durations decremented)");
                    }
                    else
                    {
                        Debug.LogWarning("TurnManager: Cannot update hazards - HazardManager not available");
                    }
                }
            }
            
            // Ensure we don't go out of bounds
            if (allUnits.Count == 0)
            {
                Debug.LogWarning("TurnManager: No units remaining! Combat should have ended.");
                return;
            }
            
            if (currentTurnIndex >= allUnits.Count)
            {
                Debug.LogWarning($"TurnManager: currentTurnIndex ({currentTurnIndex}) is out of bounds (allUnits.Count: {allUnits.Count})! Resetting to 0.");
                currentTurnIndex = 0;
            }
            
            // Ensure currentTurnIndex is valid
            if (currentTurnIndex < 0)
            {
                Debug.LogWarning($"TurnManager: currentTurnIndex ({currentTurnIndex}) is negative! Resetting to 0.");
                currentTurnIndex = 0;
            }

            // Calculate new turn window
            CalculateTurnWindow();

            // Check if combat is over
            if (IsCombatOver())
            {
                return;
            }

            // If this is a non-player window, let AI control
            if (currentTurnWindow.Count > 0 && currentTurnWindow[0].Faction != Faction.Player)
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
            
            // Stop any existing coroutine to prevent multiple coroutines running simultaneously
            if (currentEnemyWindowCoroutine != null)
            {
                StopCoroutine(currentEnemyWindowCoroutine);
                Debug.LogWarning("[ENEMY WINDOW] Stopped existing enemy window coroutine before starting new one");
            }
            
            currentEnemyWindowCoroutine = StartCoroutine(ExecuteEnemyWindowSequentially());
        }

        /// <summary>
        /// Coroutine to execute enemy AI one at a time.
        /// </summary>
        private System.Collections.IEnumerator ExecuteEnemyWindowSequentially()
        {
            // Make a copy of the original window - this is the window we're processing
            // Store original positions to detect when enemies are moved to end (EndTurn called)
            List<Unit> enemiesToProcess = new List<Unit>(currentTurnWindow);
            Dictionary<Unit, int> originalPositions = new Dictionary<Unit, int>();
            
            // Record original positions
            for (int i = 0; i < enemiesToProcess.Count; i++)
            {
                originalPositions[enemiesToProcess[i]] = allUnits.IndexOf(enemiesToProcess[i]);
            }
            
            Debug.Log($"[ENEMY WINDOW] Processing {enemiesToProcess.Count} enemies: [{string.Join(", ", enemiesToProcess.Select(u => u.UnitName))}]");

            foreach (Unit enemy in enemiesToProcess)
            {
                if (!enemy.IsAlive)
                {
                    Debug.Log($"[ENEMY WINDOW] {enemy.UnitName} is dead, ending turn automatically");
                    // Dead enemies should still be moved to end of turn order
                    if (allUnits.Contains(enemy))
                    {
                        EndTurn(enemy);
                    }
                    continue;
                }

                Debug.Log($"[ENEMY TURN] {enemy.UnitName} is acting...");
                AIController ai = enemy.GetComponent<AIController>();
                if (ai != null)
                {
                    // Use event-based completion instead of polling
                    bool turnCompleted = false;
                    System.Action<Unit> onTurnCompleteHandler = (unit) =>
                    {
                        if (unit == enemy)
                        {
                            turnCompleted = true;
                        }
                    };
                    
                    // Subscribe to turn completion event
                    ai.OnTurnComplete += onTurnCompleteHandler;
                    
                    // Start the AI turn
                    ai.TakeTurn();
                    
                    // Wait for turn completion with timeout
                    float maxWaitTime = 5f;
                    float elapsedTime = 0f;
                    float checkInterval = 0.1f;
                    
                    while (!turnCompleted && elapsedTime < maxWaitTime)
                    {
                        yield return new WaitForSeconds(checkInterval);
                        elapsedTime += checkInterval;
                    }
                    
                    // Unsubscribe from event
                    ai.OnTurnComplete -= onTurnCompleteHandler;
                    
                    if (!turnCompleted)
                    {
                        Debug.LogWarning($"[ENEMY WINDOW] {enemy.UnitName} took too long ({elapsedTime}s), cancelling turn and forcing end");
                        // Cancel the AI turn and force end if it's taking too long
                        ai.CancelTurn();
                        if (allUnits.Contains(enemy) && currentTurnWindow.Contains(enemy))
                        {
                            EndTurn(enemy);
                        }
                    }
                    else
                    {
                        Debug.Log($"[ENEMY WINDOW] {enemy.UnitName} completed their turn");
                    }
                }
                else
                {
                    Debug.LogWarning($"{enemy.UnitName} has no AIController! Ending turn automatically.");
                    // Force end turn if no AI
                    if (allUnits.Contains(enemy))
                    {
                        EndTurn(enemy);
                    }
                }

                // Small delay between enemy turns for visual clarity
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"[ENEMY WINDOW] All {enemiesToProcess.Count} enemies in window processed.");
            
            // Clear coroutine reference when done
            currentEnemyWindowCoroutine = null;
        }

        /// <summary>
        /// Coroutine to call OnTurnStart for a unit when it finishes moving.
        /// </summary>
        private System.Collections.IEnumerator DelayedOnTurnStart(Unit unit)
        {
            if (unit == null)
            {
                yield break;
            }

            // Wait until unit stops moving
            float maxWaitTime = 10f; // Safety timeout
            float elapsedTime = 0f;
            float checkInterval = 0.1f;

            while (unit.IsMoving && elapsedTime < maxWaitTime)
            {
                yield return new WaitForSeconds(checkInterval);
                elapsedTime += checkInterval;
            }

            if (elapsedTime >= maxWaitTime)
            {
                Debug.LogWarning($"[TURN WINDOW] {unit.UnitName} took too long to finish moving, calling OnTurnStart anyway");
            }

            // Verify unit is still valid and in the current window before calling OnTurnStart
            if (unit != null && unit.IsAlive && currentTurnWindow.Contains(unit))
            {
                Debug.Log($"[TURN WINDOW] Calling delayed OnTurnStart for {unit.UnitName}");
                unit.OnTurnStart();
            }
            else
            {
                Debug.Log($"[TURN WINDOW] Skipping delayed OnTurnStart for {unit?.UnitName ?? "NULL"} - unit is null, dead, or no longer in window");
            }
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
        /// Check if combat is over based on win conditions.
        /// Uses FactionRelationship to properly determine hostile factions.
        /// </summary>
        public bool IsCombatOver()
        {
            if (allUnits == null || allUnits.Count == 0)
            {
                return true; // No units = combat over
            }

            // Get victory condition from encounter data
            VictoryCondition victoryCondition = VictoryCondition.KillAll; // Default
            int turnLimit = 0;
            if (currentEncounter != null)
            {
                victoryCondition = currentEncounter.VictoryCondition;
                turnLimit = currentEncounter.TurnLimit;
            }

            // Check win condition based on type
            switch (victoryCondition)
            {
                case VictoryCondition.KillAll:
                    return CheckKillAllCondition();
                    
                case VictoryCondition.SurviveXRounds:
                    return CheckSurviveRoundsCondition(turnLimit);
                    
                case VictoryCondition.ProtectTarget:
                    return CheckProtectTargetCondition();
                    
                case VictoryCondition.ReachLocation:
                    return CheckReachLocationCondition();
                    
                case VictoryCondition.Custom:
                    // Custom conditions not yet implemented
                    return CheckKillAllCondition(); // Fallback to KillAll
                    
                default:
                    return CheckKillAllCondition(); // Default fallback
            }
        }
        
        /// <summary>
        /// Check KillAll win condition (default).
        /// </summary>
        private bool CheckKillAllCondition()
        {
            FactionRelationship factionRel = FactionRelationship.Instance ?? FindFirstObjectByType<FactionRelationship>();
            if (factionRel == null)
            {
                // FactionRelationship not found - use simple faction check as fallback
                bool hasNonPlayerUnits = false;
                bool hasPlayerUnits = false;
                
                foreach (Unit unit in allUnits)
                {
                    if (unit == null || !unit.IsAlive) continue;
                    
                    if (unit.Faction == Faction.Player || (unit.FactionData != null && unit.FactionData.IsPlayerFaction))
                    {
                        hasPlayerUnits = true;
                    }
                    else
                    {
                        hasNonPlayerUnits = true;
                    }
                }
                
                // Combat is over if player faction is eliminated
                if (!hasPlayerUnits)
                {
                    Debug.Log("Combat Over: Player faction defeated! (FactionRelationship not available, using simple check)");
                    GameEvents.RaiseCombatEnded(false);
                    return true;
                }
                
                // Combat is over if no non-player units remain
                if (!hasNonPlayerUnits)
                {
                    Debug.Log("Combat Over: All non-player units eliminated! Player faction victorious! (FactionRelationship not available, using simple check)");
                    GameEvents.RaiseCombatEnded(true);
                    return true;
                }
                
                return false;
            }

            // Count alive units and check factions
            bool playerFactionAlive = false;
            HashSet<Faction> hostileFactions = new HashSet<Faction>();

            foreach (Unit unit in allUnits)
            {
                if (unit == null || !unit.IsAlive) continue;

                Faction unitFaction = unit.Faction;

                // Check if this is player faction
                if (unitFaction == Faction.Player || (unit.FactionData != null && unit.FactionData.IsPlayerFaction))
                {
                    playerFactionAlive = true;
                }
                else
                {
                    // Check if this faction is hostile to player
                    if (factionRel.AreHostile(Faction.Player, unitFaction))
                    {
                        hostileFactions.Add(unitFaction);
                    }
                }
            }

            // Check if player faction is eliminated
            if (!playerFactionAlive)
            {
                Debug.Log("Combat Over: Player faction defeated!");
                GameEvents.RaiseCombatEnded(false);
                return true;
            }

            // Check if all hostile factions are eliminated
            if (hostileFactions.Count == 0 && playerFactionAlive)
            {
                Debug.Log("Combat Over: All hostile factions eliminated! Player faction victorious!");
                GameEvents.RaiseCombatEnded(true);
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Check SurviveXRounds win condition.
        /// </summary>
        private bool CheckSurviveRoundsCondition(int roundsToSurvive)
        {
            // Check if player faction is eliminated (defeat condition)
            bool playerFactionAlive = false;
            foreach (Unit unit in allUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (unit.Faction == Faction.Player || (unit.FactionData != null && unit.FactionData.IsPlayerFaction))
                {
                    playerFactionAlive = true;
                    break;
                }
            }
            
            if (!playerFactionAlive)
            {
                Debug.Log("Combat Over: Player faction defeated!");
                GameEvents.RaiseCombatEnded(false);
                return true;
            }
            
            // Check if survived required rounds
            if (roundsToSurvive > 0 && currentRound > roundsToSurvive)
            {
                Debug.Log($"Combat Over: Survived {roundsToSurvive} rounds! Player faction victorious!");
                GameEvents.RaiseCombatEnded(true);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Check ProtectTarget win condition.
        /// TODO: Requires target unit reference in EncounterData.
        /// </summary>
        private bool CheckProtectTargetCondition()
        {
            // For now, fallback to KillAll
            // TODO: Implement when EncounterData has target unit reference
            Debug.LogWarning("ProtectTarget win condition not yet fully implemented - using KillAll fallback");
            return CheckKillAllCondition();
        }
        
        /// <summary>
        /// Check ReachLocation win condition.
        /// TODO: Requires target position in EncounterData.
        /// </summary>
        private bool CheckReachLocationCondition()
        {
            // For now, fallback to KillAll
            // TODO: Implement when EncounterData has target position reference
            Debug.LogWarning("ReachLocation win condition not yet fully implemented - using KillAll fallback");
            return CheckKillAllCondition();
        }
    }
}