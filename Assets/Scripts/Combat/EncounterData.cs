using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Grid;
using Riftbourne.Inventory;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Environment types for battle arenas (for future visual themes).
    /// </summary>
    public enum EnvironmentType
    {
        Forest,
        Cave,
        Field,
        Urban,
        Dungeon,
        Desert,
        Swamp,
        Custom
    }

    /// <summary>
    /// Victory conditions for battles.
    /// </summary>
    public enum VictoryCondition
    {
        KillAll,           // Eliminate all enemies
        SurviveXRounds,    // Survive for specified number of rounds
        ProtectTarget,     // Keep a specific unit alive
        ReachLocation,     // Reach a specific grid position
        Custom             // Custom condition (future)
    }

    /// <summary>
    /// ScriptableObject that defines a complete battle encounter configuration.
    /// Includes arena setup, enemy spawns, player positions, hazards, and battle parameters.
    /// Create via: Assets > Create > Riftbourne > Encounter Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Encounter", menuName = "Riftbourne/Encounter Data")]
    public class EncounterData : ScriptableObject
    {
        [System.Serializable]
        public class EnemySpawnDefinition
        {
            [Tooltip("Enemy prefab to spawn. Must have Unit component.")]
            public GameObject enemyPrefab;

            [Tooltip("Grid position where this enemy will spawn (X, Y)")]
            public Vector2Int gridPosition;

            [Tooltip("Enemy level (for stat scaling - future enhancement)")]
            public int level = 1;

            [Tooltip("Optional: Override faction for this enemy (uses prefab default if null)")]
            public FactionData factionData;
        }

        [System.Serializable]
        public class StartingHazard
        {
            [Tooltip("Hazard data defining the hazard type")]
            public HazardData hazardData;

            [Tooltip("Grid position where this hazard will be placed (X, Y)")]
            public Vector2Int position;

            [Tooltip("Optional: Override direct damage per turn (0 = use HazardData default)")]
            public int directDamageOverride = 0;

            [Tooltip("Optional: Override duration in turns (0 = use HazardData default, -1 = permanent)")]
            public int durationOverride = 0;
        }

        [Header("Arena Configuration")]
        [Tooltip("Width of the battle grid in cells")]
        [SerializeField] private int gridWidth = 10;

        [Tooltip("Height of the battle grid in cells")]
        [SerializeField] private int gridHeight = 10;

        [Tooltip("World position where the grid origin (0,0) will be placed")]
        [SerializeField] private Vector3 gridOriginPosition = Vector3.zero;

        [Tooltip("Environment type for this arena (for future visual themes)")]
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.Field;

        [Tooltip("List of grid positions that are obstructed (unwalkable tiles)")]
        [SerializeField] private List<Vector2Int> obstructedTilePositions = new List<Vector2Int>();

        [Header("Enemy Configuration")]
        [Tooltip("List of enemy spawn definitions")]
        [SerializeField] private List<EnemySpawnDefinition> enemySpawns = new List<EnemySpawnDefinition>();

        [Header("Player Spawn Positions")]
        [Tooltip("Grid positions where player party members will spawn (up to 6 positions)")]
        [SerializeField] private List<Vector2Int> playerSpawnPositions = new List<Vector2Int>();

        [Header("Battle Parameters")]
        [Tooltip("Victory condition for this encounter")]
        [SerializeField] private VictoryCondition victoryCondition = VictoryCondition.KillAll;

        [Tooltip("Turn limit (0 = no limit)")]
        [SerializeField] private int turnLimit = 0;

        [Header("Starting Hazards")]
        [Tooltip("Hazards that are present at the start of battle")]
        [SerializeField] private List<StartingHazard> startingHazards = new List<StartingHazard>();

        [Header("Loot/Rewards (Optional)")]
        [Tooltip("Rewards granted on victory (optional)")]
        [SerializeField] private List<LootData> victoryRewards = new List<LootData>();

        // Public properties
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public Vector3 GridOriginPosition => gridOriginPosition;
        public EnvironmentType EnvironmentType => environmentType;
        public List<Vector2Int> ObstructedTilePositions => new List<Vector2Int>(obstructedTilePositions);
        public List<EnemySpawnDefinition> EnemySpawns => new List<EnemySpawnDefinition>(enemySpawns);
        public List<Vector2Int> PlayerSpawnPositions => new List<Vector2Int>(playerSpawnPositions);
        public VictoryCondition VictoryCondition => victoryCondition;
        public int TurnLimit => turnLimit;
        public List<StartingHazard> StartingHazards => new List<StartingHazard>(startingHazards);
        public List<LootData> VictoryRewards => new List<LootData>(victoryRewards);
    }
}
