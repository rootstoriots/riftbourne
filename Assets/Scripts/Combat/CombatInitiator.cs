using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Core;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Orchestrates battle setup from EncounterData.
    /// Handles grid generation, unit spawning, positioning, hazards, and obstructions.
    /// </summary>
    public class CombatInitiator : MonoBehaviour
    {
        [Header("Encounter Configuration")]
        [Tooltip("Encounter data to use (can be set in Inspector for testing, or loaded from SceneTransitionData at runtime)")]
        [SerializeField] private EncounterData encounterData;
        
        [Tooltip("If true, automatically loads encounter from SceneTransitionData on Start (for exploration->battle transitions)")]
        [SerializeField] private bool loadFromSceneTransitionData = true;
        
        [Tooltip("If true and encounterData is assigned, initiates combat immediately on Start (for testing)")]
        [SerializeField] private bool autoInitiateOnStart = false;

        [Header("References")]
        [Tooltip("Unit prefab to use for enemy spawning (if not specified in EncounterData)")]
        [SerializeField] private GameObject defaultEnemyPrefab;

        // Managers
        private GridManager gridManager;
        private TurnManager turnManager;
        private PartyManager partyManager;
        private HazardManager hazardManager;

        // Tracking
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private List<Unit> allCombatUnits = new List<Unit>();

        private void Awake()
        {
            // Get manager references
            gridManager = ManagerRegistry.Get<GridManager>();
            if (gridManager == null)
            {
                gridManager = GridManager.Instance;
            }

            turnManager = ManagerRegistry.Get<TurnManager>();
            if (turnManager == null)
            {
                turnManager = TurnManager.Instance;
            }
            if (turnManager == null)
            {
                Debug.LogWarning("CombatInitiator.Awake: TurnManager not found. Will retry in InitiateCombat.");
            }

            partyManager = ManagerRegistry.Get<PartyManager>();
            if (partyManager == null)
            {
                partyManager = PartyManager.Instance;
            }

            hazardManager = ManagerRegistry.Get<HazardManager>();
            if (hazardManager == null)
            {
                hazardManager = FindFirstObjectByType<HazardManager>();
            }
        }

        private void Start()
        {
            // Priority 1: Load from SceneTransitionData (for exploration->battle transitions)
            if (loadFromSceneTransitionData)
            {
                SceneTransitionData transitionData = SceneTransitionData.Instance;
                if (transitionData != null && transitionData.EncounterData != null)
                {
                    encounterData = transitionData.EncounterData;
                    Debug.Log($"CombatInitiator: Loaded encounter '{encounterData.name}' from SceneTransitionData");
                }
            }

            // Priority 2: Use Inspector-assigned encounter (for testing)
            if (encounterData != null)
            {
                if (autoInitiateOnStart || loadFromSceneTransitionData)
                {
                    // Generate grid FIRST (before BattleSceneInitializer tries to use it)
                    // Then delay unit positioning to let BattleSceneInitializer create units
                    StartCoroutine(DelayedInitiateCombat());
                }
            }
            else
            {
                Debug.LogWarning("CombatInitiator: No encounter data assigned and none found in SceneTransitionData. Battle will not be initialized.");
                Debug.LogWarning("CombatInitiator: Grid will not be generated. BattleSceneInitializer may fail if it needs a grid.");
            }
        }

        /// <summary>
        /// Delay combat initiation to ensure proper execution order:
        /// 1. Generate grid immediately (so BattleSceneInitializer can use it)
        /// 2. Wait for BattleSceneInitializer to create units
        /// 3. Then position units and initialize combat
        /// </summary>
        private System.Collections.IEnumerator DelayedInitiateCombat()
        {
            // STEP 1: Generate grid IMMEDIATELY so BattleSceneInitializer can use it
            if (gridManager != null && encounterData != null)
            {
                gridManager.ClearGrid();
                gridManager.GenerateGrid(encounterData.GridWidth, encounterData.GridHeight, encounterData.GridOriginPosition);
                Debug.Log($"CombatInitiator: Grid generated early ({encounterData.GridWidth}x{encounterData.GridHeight}) for BattleSceneInitializer");
            }

            // STEP 2: Wait for BattleSceneInitializer.Start() to run and create units
            yield return null; // Wait one frame
            yield return null; // Wait another frame to ensure Start() methods have run
            
            // STEP 3: Additional delay to ensure units are fully initialized
            yield return new WaitForSeconds(0.2f);
            
            // STEP 4: Now complete the battle setup (position units, spawn enemies, etc.)
            InitiateCombat(encounterData);
        }

        /// <summary>
        /// Main method to initiate combat from EncounterData.
        /// Orchestrates the entire battle setup sequence.
        /// </summary>
        public void InitiateCombat(EncounterData encounter)
        {
            if (encounter == null)
            {
                Debug.LogError("CombatInitiator.InitiateCombat: EncounterData is null!");
                return;
            }

            Debug.Log($"CombatInitiator: Initiating combat from encounter '{encounter.name}'");

            // Validate managers
            if (gridManager == null)
            {
                Debug.LogError("CombatInitiator: GridManager not available!");
                return;
            }

            // Retry getting managers if they weren't found in Awake
            if (turnManager == null)
            {
                turnManager = ManagerRegistry.Get<TurnManager>();
                if (turnManager == null)
                {
                    turnManager = TurnManager.Instance;
                }
                if (turnManager == null)
                {
                    turnManager = FindFirstObjectByType<TurnManager>();
                }
            }

            if (partyManager == null)
            {
                partyManager = ManagerRegistry.Get<PartyManager>();
                if (partyManager == null)
                {
                    partyManager = PartyManager.Instance;
                }
                if (partyManager == null)
                {
                    partyManager = FindFirstObjectByType<PartyManager>();
                }
            }

            // PartyManager is required for positioning player units
            if (partyManager == null)
            {
                Debug.LogError("CombatInitiator: PartyManager not available! Make sure PartyManager GameObject exists in the scene.");
                return;
            }

            // TurnManager is only needed at the end for combat initialization
            // We'll check again later, but continue with setup for now
            if (turnManager == null)
            {
                Debug.LogWarning("CombatInitiator: TurnManager not found yet. Will retry when initializing combat. Continuing with enemy spawning...");
            }

            if (hazardManager == null)
            {
                Debug.LogWarning("CombatInitiator: HazardManager not available - hazards will not be created!");
            }

            // Clear existing state
            ClearPreviousBattle();

            // Setup sequence
            // 1. Clear and generate grid (if not already generated)
            // Note: Grid may have been generated early in DelayedInitiateCombat() for BattleSceneInitializer
            if (gridManager != null)
            {
                // Check if grid already exists with correct dimensions
                bool gridNeedsRegeneration = gridManager.GridWidth != encounter.GridWidth || 
                                            gridManager.GridHeight != encounter.GridHeight;
                
                if (gridNeedsRegeneration)
                {
                    gridManager.ClearGrid();
                    gridManager.GenerateGrid(encounter.GridWidth, encounter.GridHeight, encounter.GridOriginPosition);
                    Debug.Log($"CombatInitiator: Grid regenerated ({encounter.GridWidth}x{encounter.GridHeight}) at {encounter.GridOriginPosition}");
                }
                else
                {
                    Debug.Log($"CombatInitiator: Grid already exists with correct dimensions, skipping regeneration");
                }
            }

            // 2. Set grid obstructions
            SetupObstructions(encounter);
            Debug.Log($"CombatInitiator: Set {encounter.ObstructedTilePositions.Count} obstructions");

            // 3. Spawn enemy units
            SpawnEnemies(encounter);
            Debug.Log($"CombatInitiator: Spawned {spawnedEnemies.Count} enemies");

            // 4. Position player party
            PositionPlayerParty(encounter);
            Debug.Log($"CombatInitiator: Positioned player party");

            // 5. Create starting hazards
            CreateStartingHazards(encounter);
            Debug.Log($"CombatInitiator: Created {encounter.StartingHazards.Count} starting hazards");

            // 6. Initialize TurnManager with all units
            InitializeCombat(encounter);
            Debug.Log($"CombatInitiator: Combat initialized with {allCombatUnits.Count} total units");

            Debug.Log("CombatInitiator: Battle setup complete!");
        }

        /// <summary>
        /// Clear previous battle state (spawned enemies, etc.)
        /// </summary>
        private void ClearPreviousBattle()
        {
            // Destroy spawned enemies (use Destroy, not DestroyImmediate, to avoid memory corruption)
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            spawnedEnemies.Clear();
            allCombatUnits.Clear();
        }

        /// <summary>
        /// Setup grid obstructions from EncounterData.
        /// </summary>
        private void SetupObstructions(EncounterData encounter)
        {
            foreach (Vector2Int obstructedPos in encounter.ObstructedTilePositions)
            {
                if (gridManager.IsValidGridPosition(obstructedPos.x, obstructedPos.y))
                {
                    gridManager.SetObstruction(obstructedPos.x, obstructedPos.y, true);
                }
                else
                {
                    Debug.LogWarning($"CombatInitiator: Invalid obstruction position ({obstructedPos.x}, {obstructedPos.y}) - skipping");
                }
            }
        }

        /// <summary>
        /// Spawn enemy units at positions specified in EncounterData.
        /// </summary>
        private void SpawnEnemies(EncounterData encounter)
        {
            foreach (EncounterData.EnemySpawnDefinition spawnDef in encounter.EnemySpawns)
            {
                if (spawnDef.enemyPrefab == null)
                {
                    Debug.LogWarning("CombatInitiator: Enemy spawn definition has null prefab - skipping");
                    continue;
                }

                // Validate grid position
                if (!gridManager.IsValidGridPosition(spawnDef.gridPosition.x, spawnDef.gridPosition.y))
                {
                    Debug.LogWarning($"CombatInitiator: Invalid enemy spawn position ({spawnDef.gridPosition.x}, {spawnDef.gridPosition.y}) - skipping");
                    continue;
                }

                // Get target cell
                GridCell cell = gridManager.GetCell(spawnDef.gridPosition.x, spawnDef.gridPosition.y);
                if (cell == null)
                {
                    Debug.LogWarning($"CombatInitiator: Could not get cell at ({spawnDef.gridPosition.x}, {spawnDef.gridPosition.y}) - skipping");
                    continue;
                }

                // Check if cell is walkable and not occupied
                if (!cell.IsWalkable)
                {
                    Debug.LogWarning($"CombatInitiator: Enemy spawn position ({spawnDef.gridPosition.x}, {spawnDef.gridPosition.y}) is obstructed - skipping");
                    continue;
                }

                if (cell.OccupyingUnit != null)
                {
                    Debug.LogWarning($"CombatInitiator: Enemy spawn position ({spawnDef.gridPosition.x}, {spawnDef.gridPosition.y}) is occupied - skipping");
                    continue;
                }

                // Instantiate enemy
                Vector3 worldPos = cell.WorldPosition;
                worldPos.y = 0.5f; // Keep unit elevated
                GameObject enemyObj = Instantiate(spawnDef.enemyPrefab, worldPos, Quaternion.identity);
                enemyObj.name = $"{spawnDef.enemyPrefab.name}_{spawnDef.gridPosition.x}_{spawnDef.gridPosition.y}";

                // Get Unit component
                Unit unit = enemyObj.GetComponent<Unit>();
                if (unit == null)
                {
                    Debug.LogError($"CombatInitiator: Enemy prefab '{spawnDef.enemyPrefab.name}' does not have Unit component!");
                    Destroy(enemyObj);
                    continue;
                }

                // Set grid position
                unit.SetGridPosition(spawnDef.gridPosition.x, spawnDef.gridPosition.y, worldPos);

                // Ensure enemy has AIController (required for enemy turns)
                AIController aiController = enemyObj.GetComponent<AIController>();
                if (aiController == null)
                {
                    aiController = enemyObj.AddComponent<AIController>();
                    Debug.Log($"CombatInitiator: Added AIController to enemy '{enemyObj.name}'");
                }

                // Override faction if specified
                if (spawnDef.factionData != null)
                {
                    // Note: This assumes Unit has a way to set faction from FactionData
                    // If not, this will need to be implemented in Unit class
                    Debug.Log($"CombatInitiator: Faction override specified for enemy, but Unit.SetFaction() not yet implemented");
                }

                // Level scaling (future enhancement - placeholder for now)
                if (spawnDef.level > 1)
                {
                    Debug.Log($"CombatInitiator: Level scaling not yet implemented - enemy will use default level");
                }

                // Track spawned enemy
                spawnedEnemies.Add(enemyObj);
                allCombatUnits.Add(unit);

                Debug.Log($"CombatInitiator: Spawned enemy '{enemyObj.name}' at ({spawnDef.gridPosition.x}, {spawnDef.gridPosition.y})");
            }
        }

        /// <summary>
        /// Position player party members at spawn positions from EncounterData.
        /// </summary>
        private void PositionPlayerParty(EncounterData encounter)
        {
            List<Unit> partyMembers = partyManager.GetPartyMembersAsUnits();

            if (partyMembers == null || partyMembers.Count == 0)
            {
                Debug.LogWarning("CombatInitiator: No player party members found!");
                return;
            }

            List<Vector2Int> spawnPositions = encounter.PlayerSpawnPositions;

            for (int i = 0; i < partyMembers.Count; i++)
            {
                Unit unit = partyMembers[i];
                if (unit == null)
                {
                    continue;
                }

                Vector2Int spawnPos;

                // Get spawn position (use last position if not enough defined)
                if (i < spawnPositions.Count)
                {
                    spawnPos = spawnPositions[i];
                }
                else if (spawnPositions.Count > 0)
                {
                    // Use last defined position
                    spawnPos = spawnPositions[spawnPositions.Count - 1];
                    Debug.LogWarning($"CombatInitiator: Not enough spawn positions defined, using last position for unit {i}");
                }
                else
                {
                    // Auto-position: default to (0, i) if no positions defined
                    spawnPos = new Vector2Int(0, i);
                    Debug.LogWarning($"CombatInitiator: No spawn positions defined, auto-positioning unit {i} at ({spawnPos.x}, {spawnPos.y})");
                }

                // Validate position
                if (!gridManager.IsValidGridPosition(spawnPos.x, spawnPos.y))
                {
                    Debug.LogWarning($"CombatInitiator: Invalid player spawn position ({spawnPos.x}, {spawnPos.y}) for unit {i} - skipping");
                    continue;
                }

                // Get target cell
                GridCell cell = gridManager.GetCell(spawnPos.x, spawnPos.y);
                if (cell == null)
                {
                    Debug.LogWarning($"CombatInitiator: Could not get cell at ({spawnPos.x}, {spawnPos.y}) for unit {i} - skipping");
                    continue;
                }

                // Check if cell is walkable and not occupied
                if (!cell.IsWalkable)
                {
                    Debug.LogWarning($"CombatInitiator: Player spawn position ({spawnPos.x}, {spawnPos.y}) is obstructed - skipping");
                    continue;
                }

                if (cell.OccupyingUnit != null && cell.OccupyingUnit != unit)
                {
                    Debug.LogWarning($"CombatInitiator: Player spawn position ({spawnPos.x}, {spawnPos.y}) is occupied - skipping");
                    continue;
                }

                // Move unit to spawn position
                Vector3 worldPos = cell.WorldPosition;
                worldPos.y = 0.5f; // Keep unit elevated
                unit.SetGridPosition(spawnPos.x, spawnPos.y, worldPos);

                // Add to combat units list
                if (!allCombatUnits.Contains(unit))
                {
                    allCombatUnits.Add(unit);
                }

                Debug.Log($"CombatInitiator: Positioned player unit '{unit.UnitName}' at ({spawnPos.x}, {spawnPos.y})");
            }
        }

        /// <summary>
        /// Create starting hazards from EncounterData.
        /// </summary>
        private void CreateStartingHazards(EncounterData encounter)
        {
            if (hazardManager == null)
            {
                return;
            }

            foreach (EncounterData.StartingHazard startingHazard in encounter.StartingHazards)
            {
                if (startingHazard.hazardData == null)
                {
                    Debug.LogWarning("CombatInitiator: Starting hazard has null HazardData - skipping");
                    continue;
                }

                // Validate position
                if (!gridManager.IsValidGridPosition(startingHazard.position.x, startingHazard.position.y))
                {
                    Debug.LogWarning($"CombatInitiator: Invalid hazard position ({startingHazard.position.x}, {startingHazard.position.y}) - skipping");
                    continue;
                }

                // Create hazard with optional overrides
                int damageOverride = startingHazard.directDamageOverride > 0 ? startingHazard.directDamageOverride : 0;
                int durationOverride = startingHazard.durationOverride != 0 ? startingHazard.durationOverride : 0;

                hazardManager.CreateHazard(
                    startingHazard.hazardData,
                    startingHazard.position.x,
                    startingHazard.position.y,
                    damageOverride,
                    durationOverride
                );

                Debug.Log($"CombatInitiator: Created {startingHazard.hazardData.HazardName} hazard at ({startingHazard.position.x}, {startingHazard.position.y})");
            }
        }

        /// <summary>
        /// Initialize TurnManager with all combat units.
        /// </summary>
        private void InitializeCombat(EncounterData encounter)
        {
            if (allCombatUnits.Count == 0)
            {
                Debug.LogError("CombatInitiator: No units to initialize combat with!");
                return;
            }

            // Final check for TurnManager - retry if still null
            if (turnManager == null)
            {
                turnManager = ManagerRegistry.Get<TurnManager>();
                if (turnManager == null)
                {
                    turnManager = TurnManager.Instance;
                }
                if (turnManager == null)
                {
                    turnManager = FindFirstObjectByType<TurnManager>();
                }
            }

            if (turnManager == null)
            {
                Debug.LogError("CombatInitiator: TurnManager not available! Cannot initialize combat. Make sure TurnManager GameObject exists in the scene.");
                Debug.LogError("CombatInitiator: Enemies and player units have been spawned, but combat will not start without TurnManager.");
                return;
            }

            // Initialize BattleStatisticsTracker
            BattleStatisticsTracker tracker = BattleStatisticsTracker.Instance;
            if (tracker != null)
            {
                List<Unit> partyMembers = partyManager != null ? partyManager.GetPartyMembersAsUnits() : new List<Unit>();
                tracker.InitializeBattle(partyMembers);
            }

            // Show stakes notification before starting combat
            // Use FindObjectsByType with IncludeInactive to find inactive GameObjects
            Riftbourne.UI.BattleStakesNotificationUI[] allStakesUI = FindObjectsByType<Riftbourne.UI.BattleStakesNotificationUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Riftbourne.UI.BattleStakesNotificationUI stakesUI = allStakesUI != null && allStakesUI.Length > 0 ? allStakesUI[0] : null;
            
            Debug.Log($"CombatInitiator: Searching for BattleStakesNotificationUI... Found {allStakesUI?.Length ?? 0} instance(s)");
            
            if (stakesUI != null && encounter != null)
            {
                Debug.Log($"CombatInitiator: Found BattleStakesNotificationUI on GameObject '{stakesUI.gameObject.name}', showing stakes notification");
                stakesUI.ShowStakes(encounter, () =>
                {
                    // Initialize TurnManager after notification is acknowledged
                    turnManager.InitializeCombat(allCombatUnits, encounter);
                    Debug.Log("CombatInitiator: Battle started after stakes notification acknowledged.");
                });
            }
            else
            {
                if (stakesUI == null)
                {
                    Debug.LogWarning("CombatInitiator: BattleStakesNotificationUI not found in scene! Proceeding without stakes notification.");
                    Debug.LogWarning("CombatInitiator: Make sure BattleStakesNotificationUI component exists in the scene (can be on inactive GameObject).");
                }
                if (encounter == null)
                {
                    Debug.LogWarning("CombatInitiator: EncounterData is null! Proceeding without stakes notification.");
                }
                // No stakes UI found, proceed directly
                turnManager.InitializeCombat(allCombatUnits, encounter);
            }
        }
    }
}
