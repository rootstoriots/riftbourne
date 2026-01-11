using UnityEngine;
using Riftbourne.Grid;
using Riftbourne.Core;
using Riftbourne.Combat;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Factory for creating Unit GameObjects from CharacterState data.
    /// Used when transitioning from exploration to battle.
    /// </summary>
    public static class UnitFactory
    {
        /// <summary>
        /// Creates a Unit GameObject from a CharacterState.
        /// Applies all stats, equipment, skills, and status effects.
        /// </summary>
        /// <param name="state">CharacterState to create Unit from</param>
        /// <param name="prefab">Unit prefab to instantiate (if null, creates new GameObject)</param>
        /// <param name="position">World position to place the unit</param>
        /// <param name="gridX">Grid X coordinate (optional, calculated from position if not provided)</param>
        /// <param name="gridY">Grid Y coordinate (optional, calculated from position if not provided)</param>
        /// <returns>Created Unit component, or null if creation failed</returns>
        public static Unit CreateFromCharacterState(CharacterState state, GameObject prefab, Vector3 position, int? gridX = null, int? gridY = null)
        {
            if (state == null)
            {
                Debug.LogError("UnitFactory: Cannot create Unit from null CharacterState!");
                return null;
            }

            if (state.Definition == null)
            {
                Debug.LogError($"UnitFactory: CharacterState {state.CharacterID} has null definition!");
                return null;
            }

            // Create or instantiate GameObject
            GameObject unitObj;
            if (prefab != null)
            {
                unitObj = Object.Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                unitObj = new GameObject(state.Definition.CharacterName);
                unitObj.transform.position = position;
            }

            // Get or add Unit component
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit == null)
            {
                unit = unitObj.AddComponent<Unit>();
            }

            // Initialize Unit from CharacterState using UpdateFromCharacterState method
            // This is the preferred method as it properly syncs all data
            unit.UpdateFromCharacterState(state);
            
            // Also set equipment and skills using reflection (for fields that UpdateFromCharacterState might not cover)
            InitializeUnitEquipmentAndSkills(unit, state);

            // Set grid position if provided
            if (gridX.HasValue && gridY.HasValue)
            {
                GridManager gridManager = ManagerRegistry.Get<GridManager>();
                if (gridManager != null)
                {
                    GridCell cell = gridManager.GetCell(gridX.Value, gridY.Value);
                    if (cell != null)
                    {
                        Vector3 cellPosition = cell.WorldPosition;
                        cellPosition.y = 0.5f; // Keep unit elevated
                        unitObj.transform.position = cellPosition;
                        // Unit will set grid position in its Awake/Start
                    }
                }
            }

            Debug.Log($"UnitFactory: Created Unit {state.Definition.CharacterName} from CharacterState {state.CharacterID}");
            return unit;
        }

        /// <summary>
        /// Initialize Unit equipment and skills from CharacterState.
        /// UpdateFromCharacterState handles most fields, but we ensure faction and player control are set.
        /// </summary>
        private static void InitializeUnitEquipmentAndSkills(Unit unit, CharacterState state)
        {
            if (unit == null || state == null) return;

            // Set faction and player control through reflection (UpdateFromCharacterState doesn't handle these)
            var unitType = typeof(Unit);
            
            // Set isPlayerControlled
            var isPlayerControlledField = unitType.GetField("isPlayerControlled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isPlayerControlledField != null)
            {
                isPlayerControlledField.SetValue(unit, true); // Party members are always player-controlled
            }

            // Set faction
            var factionField = unitType.GetField("faction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (factionField != null)
            {
                factionField.SetValue(unit, Faction.Player);
            }

            Debug.Log($"UnitFactory: Set faction and player control for Unit {state.Definition?.CharacterName ?? "Unknown"}");
        }

        /// <summary>
        /// Create multiple Units from a list of CharacterStates.
        /// </summary>
        public static List<Unit> CreateUnitsFromParty(List<CharacterState> party, GameObject prefab, List<Vector3> positions)
        {
            List<Unit> units = new List<Unit>();

            if (party == null || party.Count == 0)
            {
                Debug.LogWarning("UnitFactory: Cannot create units from empty party!");
                return units;
            }

            if (positions == null || positions.Count < party.Count)
            {
                Debug.LogWarning("UnitFactory: Not enough positions provided! Using default positions.");
                // Generate default positions
                positions = new List<Vector3>();
                for (int i = 0; i < party.Count; i++)
                {
                    positions.Add(new Vector3(i * 2f, 0.5f, 0f));
                }
            }

            for (int i = 0; i < party.Count; i++)
            {
                Unit unit = CreateFromCharacterState(party[i], prefab, positions[i]);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }
    }
}
