using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Grid;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Core
{
    /// <summary>
    /// Initializes battle scene with party members from CharacterStates.
    /// Attach this to a GameObject in the battle scene.
    /// </summary>
    public class BattleSceneInitializer : MonoBehaviour
    {
        [Header("Unit Prefab")]
        [Tooltip("Prefab to use when creating Units from CharacterStates")]
        [SerializeField] private GameObject unitPrefab;

        [Header("Starting Positions")]
        [Tooltip("Grid positions for party members (if empty, will auto-position)")]
        [SerializeField] private List<Vector2Int> partyStartPositions = new List<Vector2Int>();

        [Header("Auto-Position Settings")]
        [Tooltip("If true, automatically positions party members in a line")]
        [SerializeField] private bool autoPosition = true;
        [Tooltip("Starting X position for auto-positioning")]
        [SerializeField] private int autoStartX = 1;
        [Tooltip("Starting Y position for auto-positioning")]
        [SerializeField] private int autoStartY = 1;

        private void Awake()
        {
            Debug.Log("BattleSceneInitializer: Awake() called");
        }

        private void Start()
        {
            Debug.Log("BattleSceneInitializer: Start() called");
            InitializeBattleScene();
        }

        /// <summary>
        /// Initialize battle scene with party data.
        /// </summary>
        public void InitializeBattleScene()
        {
            Debug.Log("BattleSceneInitializer: InitializeBattleScene() called");
            
            // Check if unit prefab is assigned
            if (unitPrefab == null)
            {
                Debug.LogError("BattleSceneInitializer: Unit Prefab is not assigned in Inspector! Cannot create party units.");
                return;
            }
            
            // Get party data from SceneTransitionData
            SceneTransitionData transitionData = SceneTransitionData.Instance;
            if (transitionData == null)
            {
                // Try to find it in the scene (it might have been created in a previous scene)
                transitionData = FindFirstObjectByType<SceneTransitionData>();
                if (transitionData == null)
                {
                    Debug.LogError("BattleSceneInitializer: SceneTransitionData.Instance is null and not found in scene! Party data cannot be loaded.");
                    Debug.LogError("BattleSceneInitializer: Make sure BattleSceneLoader.LoadBattleScene() was called from the exploration scene before loading battle scene.");
                    return;
                }
            }
            
            Debug.Log($"BattleSceneInitializer: SceneTransitionData found. PartyData is null: {transitionData.PartyData == null}, Count: {transitionData.PartyData?.Count ?? 0}");
            
            if (transitionData.PartyData == null || transitionData.PartyData.Count == 0)
            {
                Debug.LogWarning("BattleSceneInitializer: No party data found in SceneTransitionData! Battle scene may not have party members.");
                Debug.LogWarning("BattleSceneInitializer: Make sure BattleSceneLoader.LoadBattleScene() was called from the exploration scene.");
                return;
            }

            List<CharacterState> party = transitionData.PartyData;
            Debug.Log($"BattleSceneInitializer: Initializing battle scene with {party.Count} party members");

            // Get GridManager for positioning
            GridManager gridManager = ManagerRegistry.Get<GridManager>();
            if (gridManager == null)
            {
                gridManager = GridManager.Instance;
            }

            if (gridManager == null)
            {
                Debug.LogError("BattleSceneInitializer: GridManager not available! Cannot position units.");
                return;
            }

            // Create Units from CharacterStates
            List<Unit> createdUnits = new List<Unit>();
            List<Vector3> positions = new List<Vector3>();

            // Determine positions
            if (autoPosition && partyStartPositions.Count < party.Count)
            {
                // Auto-position in a line
                for (int i = 0; i < party.Count; i++)
                {
                    int x = autoStartX;
                    int y = autoStartY + i;
                    partyStartPositions.Add(new Vector2Int(x, y));
                }
            }

            // Create units
            for (int i = 0; i < party.Count; i++)
            {
                CharacterState state = party[i];
                Vector2Int gridPos = i < partyStartPositions.Count ? partyStartPositions[i] : new Vector2Int(autoStartX, autoStartY + i);

                // Get world position from grid
                GridCell cell = gridManager.GetCell(gridPos.x, gridPos.y);
                if (cell == null)
                {
                    Debug.LogWarning($"BattleSceneInitializer: Invalid grid position ({gridPos.x}, {gridPos.y}) for unit {i}");
                    continue;
                }

                Vector3 worldPos = cell.WorldPosition;
                worldPos.y = 0.5f; // Keep unit elevated

                // Create Unit from CharacterState
                if (unitPrefab == null)
                {
                    Debug.LogError($"BattleSceneInitializer: Unit prefab is not assigned! Cannot create unit for {state.Definition?.CharacterName ?? "Unknown"}");
                    continue;
                }
                
                Unit unit = UnitFactory.CreateFromCharacterState(state, unitPrefab, worldPos, gridPos.x, gridPos.y);
                if (unit != null)
                {
                    createdUnits.Add(unit);
                    positions.Add(worldPos);
                    Debug.Log($"BattleSceneInitializer: Created Unit {state.Definition.CharacterName} at grid ({gridPos.x}, {gridPos.y})");
                }
                else
                {
                    Debug.LogError($"BattleSceneInitializer: Failed to create Unit for {state.Definition?.CharacterName ?? "Unknown"}");
                }
            }

            // Register units with PartyManager (for battle selection)
            if (PartyManager.Instance != null)
            {
                foreach (var unit in createdUnits)
                {
                    PartyManager.Instance.RegisterUnit(unit);
                }
            }

            Debug.Log($"BattleSceneInitializer: Initialized {createdUnits.Count} units in battle scene");
        }
    }
}
