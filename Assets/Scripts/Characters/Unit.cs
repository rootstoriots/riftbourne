using UnityEngine;
using Riftbourne.Grid;
using Riftbourne.Skills;
using Riftbourne.Combat;
using Riftbourne.UI;
using Riftbourne.Core;
using Riftbourne.Items;
using Riftbourne.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Riftbourne.Characters
{
    public class Unit : Character
    {
        [Header("Combat Stats")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int currentHP;
        [SerializeField] private int attackPower = 15; // Legacy - will be replaced by Strength scaling
        [SerializeField] private int defensePower = 5;  // Legacy - will be replaced by stat scaling
        
        [Header("Core Attributes")]
        [SerializeField] private int strength = 5;    // Physical power, melee damage
        [SerializeField] private int finesse = 5;     // Precision, ranged damage, evasion
        [SerializeField] private int focus = 5;       // Mental power, magic damage, resistance
        [SerializeField] private int speed = 5;       // Turn order (higher = goes first)
        [SerializeField] private int luck = 5;        // Critical chance, item drops (future)

        [Header("UI")]
        [SerializeField] private HPDisplay hpDisplay;

        [Header("Magical Capacity")]
        [SerializeField] private MantleType mantle = MantleType.None;

        // Public property for Mantle
        public MantleType Mantle => mantle;

        [Header("Skills")]
        [SerializeField] private List<Skill> knownSkills = new List<Skill>();

        [Header("Equipment")]
        [Tooltip("Starting equipment for each slot (leave empty for no equipment)")]
        [SerializeField] private EquipmentItem meleeWeapon;
        [SerializeField] private EquipmentItem rangedWeapon;
        [SerializeField] private EquipmentItem armor;
        [SerializeField] private EquipmentItem accessory1;
        [SerializeField] private EquipmentItem accessory2;
        [SerializeField] private EquipmentItem codex;

        [Header("Inventory System")]
        [SerializeField] private List<InventorySlot> inventory = new List<InventorySlot>();
        [SerializeField] private List<InventorySlot> containerInventory = new List<InventorySlot>();
        [SerializeField] private ContainerItem[] containerSlots = new ContainerItem[2];
        [SerializeField] private int maxContainerSlots = 2;
        [SerializeField] private int aurumShards = 0;

        [Header("Unit Identity")]
        [SerializeField] private string unitName = "Unit";
        [SerializeField] private bool isPlayerControlled = true;
        [Tooltip("Portrait sprite for this unit. If not set, will use default portrait generator.")]
        [SerializeField] private Sprite portrait;
        
        [Header("Faction Assignment")]
        [Tooltip("Option 1: Use ScriptableObject faction (recommended for custom factions)")]
        [SerializeField] private FactionData factionData;
        
        [Tooltip("Option 2: Use enum faction (for backward compatibility or simple cases)")]
        [SerializeField] private Faction faction = Faction.Player;
        
        [Header("Unit Type")]
        [SerializeField] private UnitType unitType = UnitType.Soldier;
        
        [Header("Progression")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int skillPoints = 0;  // SP for mastering skills
        [SerializeField] private int totalActions = 0;  // Track total successful actions
        
        // SP progression tracking
        private int lastSPAwardedAtAction = 0;  // Track when we last awarded SP

        // Narrative skills for exploration mode
        private Dictionary<Skills.NarrativeSkillCategory, int> narrativeSkills = new Dictionary<Skills.NarrativeSkillCategory, int>();

        // Equipment system
        private Dictionary<EquipmentSlot, EquipmentItem> equippedItems = new Dictionary<EquipmentSlot, EquipmentItem>();

        // SP-based mastery tracking (replaces old usage-based system)
        private HashSet<Skill> masteredSkills = new HashSet<Skill>();
        private HashSet<PassiveSkill> masteredPassiveSkills = new HashSet<PassiveSkill>();

        // Component classes for separation of concerns
        private UnitStats unitStats;
        private UnitCombat unitCombat;
        private UnitProgression unitProgression;
        private UnitEquipment unitEquipment;
        private UnitStatusEffects unitStatusEffects;
        private WeaponProficiencyManager weaponProficiencyManager;
        
        // Public access to component classes
        public UnitEquipment UnitEquipment => unitEquipment;
        public WeaponProficiencyManager WeaponProficiencyManager => weaponProficiencyManager;

        // Events
        /// <summary>
        /// Event raised when unit's HP changes (damage or healing).
        /// Parameters: (currentHP, maxHP)
        /// </summary>
        public event Action<int, int> OnHPChanged;

        // Public properties
        public List<Skill> KnownSkills => knownSkills;
        public bool IsBurning => unitStatusEffects != null && unitStatusEffects.IsBurning;
        public StatusEffect BurnEffect => unitStatusEffects?.BurnEffect;
        public List<InventorySlot> Inventory => inventory;
        public List<InventorySlot> ContainerInventory => containerInventory;
        public ContainerItem[] ContainerSlots => containerSlots;
        public int MaxHP => CalculateMaxHP();
        public int CurrentHP => unitCombat != null ? unitCombat.CurrentHP : currentHP;
        public int AttackPower => unitCombat != null ? unitCombat.GetAttackPower(unitEquipment.GetTotalEquipmentBonus(StatType.Attack) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Attack)) : (attackPower + unitEquipment.GetTotalEquipmentBonus(StatType.Attack) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Attack));
        public int DefensePower => unitCombat != null ? unitCombat.GetDefensePower(unitEquipment.GetTotalEquipmentBonus(StatType.Defense) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Defense)) : (defensePower + unitEquipment.GetTotalEquipmentBonus(StatType.Defense) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Defense));
        
        // Core Attributes (include equipment bonuses + passive skill bonuses)
        public int Strength => (unitStats != null ? unitStats.BaseStrength : strength) + unitEquipment.GetTotalEquipmentBonus(StatType.Strength) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Strength);
        public int Finesse => (unitStats != null ? unitStats.BaseFinesse : finesse) + unitEquipment.GetTotalEquipmentBonus(StatType.Finesse) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Finesse);
        public int Focus => (unitStats != null ? unitStats.BaseFocus : focus) + unitEquipment.GetTotalEquipmentBonus(StatType.Focus) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Focus);
        public int Speed => (unitStats != null ? unitStats.BaseSpeed : speed) + unitEquipment.GetTotalEquipmentBonus(StatType.Speed) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Speed);
        public int Luck => (unitStats != null ? unitStats.BaseLuck : luck) + unitEquipment.GetTotalEquipmentBonus(StatType.Luck) + unitEquipment.GetTotalPassiveSkillBonus(StatType.Luck);
        
        // Movement Range (includes passive skill bonuses)
        public new int MovementRange => base.MovementRange + (unitEquipment?.GetTotalMovementRangeBonus() ?? 0);
        
        /// <summary>
        /// Gets the attack range for this unit.
        /// - If ranged weapon is equipped: minimum 3 + equipment range bonus
        /// - If no ranged weapon: 1 (melee range)
        /// Skills use their own range and are not affected by this property.
        /// </summary>
        public int AttackRange
        {
            get
            {
                if (unitEquipment != null && unitEquipment.HasRangedWeapon())
                {
                    // Minimum 3 range for ranged weapons, plus equipment bonus
                    return Mathf.Max(3, 3 + unitEquipment.GetRangedWeaponRangeBonus());
                }
                // Melee range
                return 1;
            }
        }
        
        public string UnitName => unitName;
        public bool IsPlayerControlled => isPlayerControlled;
        public Sprite Portrait => portrait;
        
        // Encumbrance-Affected Stats (Effective Stats)
        public int EffectiveMovementRange
        {
            get
            {
                int baseRange = MovementRange;
                
                // Apply encumbrance penalties
                float encumbrance = EncumbrancePercent;
                
                if (encumbrance > 2.0f)
                    baseRange -= 4;
                else if (encumbrance > 1.5f)
                    baseRange -= 3;
                else if (encumbrance > 1.25f)
                    baseRange -= 2;
                else if (encumbrance > 1.0f)
                    baseRange -= 1;
                
                return Mathf.Max(1, baseRange);
            }
        }

        public int EffectiveSpeed
        {
            get
            {
                int baseSpeed = Speed;
                
                float encumbrance = EncumbrancePercent;
                
                if (encumbrance > 2.0f)
                    baseSpeed -= 5;
                else if (encumbrance > 1.5f)
                    baseSpeed -= 3;
                else if (encumbrance > 1.25f)
                    baseSpeed -= 2;
                else if (encumbrance > 1.0f)
                    baseSpeed -= 1;
                
                return Mathf.Max(1, baseSpeed);
            }
        }

        public int EffectiveAttackPower
        {
            get
            {
                float baseAttack = AttackPower;
                float multiplier = 1.0f;
                
                float encumbrance = EncumbrancePercent;
                
                if (encumbrance > 2.0f)
                    multiplier = 0.7f;
                else if (encumbrance > 1.5f)
                    multiplier = 0.8f;
                else if (encumbrance > 1.25f)
                    multiplier = 0.9f;
                
                return Mathf.RoundToInt(baseAttack * multiplier);
            }
        }

        public int EffectiveDefensePower
        {
            get
            {
                float baseDefense = DefensePower;
                float multiplier = 1.0f;
                
                float encumbrance = EncumbrancePercent;
                
                if (encumbrance > 2.0f)
                    multiplier = 0.7f;
                else if (encumbrance > 1.5f)
                    multiplier = 0.8f;
                else if (encumbrance > 1.25f)
                    multiplier = 0.9f;
                
                return Mathf.RoundToInt(baseDefense * multiplier);
            }
        }

        public int EffectiveFinesse
        {
            get
            {
                int baseFinesse = Finesse;
                
                float encumbrance = EncumbrancePercent;
                
                if (encumbrance > 2.0f)
                    baseFinesse -= 5;
                else if (encumbrance > 1.5f)
                    baseFinesse -= 2;
                
                return Mathf.Max(0, baseFinesse);
            }
        }

        // Inventory Weight Properties
        public float BaseCarryCapacity => Strength * 5.0f;
        
        public float CurrentWeight
        {
            get
            {
                float total = 0f;
                
                // Main inventory weight
                foreach (var slot in inventory)
                {
                    if (slot != null && slot.Item != null)
                        total += slot.GetTotalWeight();
                }
                
                // Container inventory (with reduction applied)
                foreach (var slot in containerInventory)
                {
                    if (slot != null && slot.Item != null)
                    {
                        float reduction = GetContainerReduction();
                        total += slot.GetTotalWeight() * (1f - reduction);
                    }
                }
                
                // Equipped battle items (50% weight)
                if (unitEquipment != null)
                {
                    foreach (var kvp in unitEquipment.EquippedItems)
                    {
                        if (kvp.Value != null && kvp.Value is EquipmentItem equipItem)
                        {
                            total += equipItem.Weight * 0.5f;
                        }
                    }
                }
                
                return total;
            }
        }
        
        public float EffectiveCarryCapacity
        {
            get
            {
                float capacity = BaseCarryCapacity;
                
                // Containers don't add capacity, they reduce weight of contents
                // Capacity is purely strength-based
                
                return capacity;
            }
        }
        
        public float EncumbrancePercent => EffectiveCarryCapacity > 0 ? CurrentWeight / EffectiveCarryCapacity : 0f;
        
        public bool IsOverencumbered => EncumbrancePercent > 1.0f;
        
        public int AurumShards => aurumShards;
        
        /// <summary>
        /// Get the faction enum. If using ScriptableObject faction, returns the mapped enum.
        /// Falls back to enum field if ScriptableObject not assigned or not mapped.
        /// </summary>
        public Faction Faction 
        { 
            get 
            {
                // If using ScriptableObject faction, try to map it to enum
                if (factionData != null)
                {
                    FactionRegistry registry = Resources.Load<FactionRegistry>("FactionRegistry");
                    if (registry != null)
                    {
                        registry.BuildLookup(); // Ensure lookup is built
                        Faction? mappedEnum = registry.GetEnumForFactionData(factionData);
                        if (mappedEnum.HasValue)
                        {
                            return mappedEnum.Value;
                        }
                    }
                    
                    // If registry not found or not mapped, check IsPlayerFaction flag as fallback
                    if (factionData.IsPlayerFaction)
                    {
                        return Faction.Player;
                    }
                    
                    // If not mapped and not player, return the enum field (should be set correctly)
                    Debug.LogWarning($"Unit {unitName}: FactionData '{factionData.FactionName}' is not mapped to an enum in FactionRegistry. Using fallback enum value.");
                }
                return faction;
            }
        }
        
        /// <summary>
        /// Get the ScriptableObject faction data (if assigned).
        /// </summary>
        public FactionData FactionData => factionData;
        
        public UnitType UnitType => unitType;
        public bool IsAlive => unitCombat != null ? unitCombat.IsAlive : (currentHP > 0);
        
        // Progression
        public int Level => unitProgression != null ? unitProgression.Level : level;
        public int CurrentXP => unitProgression != null ? unitProgression.CurrentXP : currentXP;
        public int SkillPoints => unitProgression != null ? unitProgression.SkillPoints : skillPoints;
        public int TotalActions => unitProgression != null ? unitProgression.TotalActions : totalActions;

        // Action tracking for turn-based gameplay
        public int MovementPointsRemaining { get; private set; }
        public int MaxMovementPoints => MovementRange; // Movement points = movement range
        public bool HasActedThisTurn { get; private set; }

        // Hazard damage tracking - only take damage once per hazard type per turn
        private HashSet<HazardTile.HazardType> hazardsDamagedByThisTurn = new HashSet<HazardTile.HazardType>();

        private void Awake()
        {
            currentHP = maxHP;
            
            // Sync faction with isPlayerControlled for backward compatibility
            // Priority: ScriptableObject faction > enum faction > isPlayerControlled
            if (factionData != null)
            {
                // Using ScriptableObject faction - sync isPlayerControlled if needed
                if (factionData.IsPlayerFaction && !isPlayerControlled)
                {
                    isPlayerControlled = true;
                }
                else if (!factionData.IsPlayerFaction && isPlayerControlled)
                {
                    isPlayerControlled = false;
                }
            }
            else
            {
                // Using enum faction - sync with isPlayerControlled
                if (isPlayerControlled && faction != Faction.Player)
                {
                    faction = Faction.Player;
                }
                else if (!isPlayerControlled && faction == Faction.Player)
                {
                    // If not player controlled but faction is Player, set to default enemy faction
                    faction = Faction.Faction1;
                }
            }
            
            // Initialize manager references if not already set by base class
            // (Base class Character.Awake() should have already set these, but ensure they're set)
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
                // Fallback to Instance if ManagerRegistry isn't ready yet
                if (gridManager == null)
                {
                    gridManager = GridManager.Instance;
                }
            }
            if (hazardManager == null)
            {
                hazardManager = ManagerRegistry.Get<HazardManager>();
                if (hazardManager == null)
                {
                    hazardManager = FindFirstObjectByType<HazardManager>();
                }
            }
            
            // Initialize grid position EARLY - before other systems try to use it
            // This must happen in Awake() so it's ready before Start() methods run
            InitializeGridPosition();
            
            // Initialize movement points
            MovementPointsRemaining = MovementRange;

            // Initialize component classes
            unitStats = new UnitStats(strength, finesse, focus, speed, luck);
            unitCombat = new UnitCombat(this, maxHP, attackPower, defensePower);
            unitProgression = new UnitProgression(this, level, currentXP, skillPoints, masteredSkills, masteredPassiveSkills);
            unitEquipment = new UnitEquipment(this, masteredSkills, masteredPassiveSkills, knownSkills);
            unitStatusEffects = new UnitStatusEffects(this);
            weaponProficiencyManager = new WeaponProficiencyManager(this);
            
            // Initialize all weapon families so they show up in UI from the start
            weaponProficiencyManager.InitializeAllFamilies();
            
            // Initialize narrative skills with default values
            narrativeSkills[Skills.NarrativeSkillCategory.Perception] = 5;
            narrativeSkills[Skills.NarrativeSkillCategory.Interpretive] = 3;
            narrativeSkills[Skills.NarrativeSkillCategory.Empathic] = 4;
        }
        
        /// <summary>
        /// Initialize grid position from world position.
        /// Called in Awake() to ensure gridX/gridY are set before other systems query them.
        /// </summary>
        private void InitializeGridPosition()
        {
            if (gridManager == null)
            {
                Debug.LogWarning($"{unitName} - GridManager not available in Awake(), will retry in Start()");
                return;
            }
            
            // Calculate grid coordinates from world position
            // Grid cells are centered at (0.5, 1.5, 2.5, etc.) for cellSize = 1
            float cellSize = gridManager.CellSize;
            
            // Calculate which cell this unit is in
            // If cellSize=1, cell centers are at 0.5, 1.5, 2.5...
            // So a unit at world pos (1.5, 0, 1.5) should be in cell (1, 1)
            // Formula: gridX = floor(worldX / cellSize)
            int startX = Mathf.FloorToInt(transform.position.x / cellSize);
            int startY = Mathf.FloorToInt(transform.position.z / cellSize);

            if (gridManager.IsValidGridPosition(startX, startY))
            {
                // Get the proper centered world position from the grid cell
                GridCell cell = gridManager.GetCell(startX, startY);
                if (cell != null)
                {
                    Vector3 centeredPosition = cell.WorldPosition;
                    centeredPosition.y = 0.5f; // Keep unit elevated
                    
                    // Set grid position immediately - this updates gridX and gridY
                    SetGridPosition(startX, startY, centeredPosition);
                    transform.position = centeredPosition; // Snap to cell center
                    
                    Debug.Log($"{unitName} initialized at grid position ({startX}, {startY}) - world pos: {centeredPosition}");
                }
                else
                {
                    Debug.LogError($"{unitName} - GridManager returned null cell for ({startX}, {startY}). Grid position NOT initialized!");
                }
            }
            else
            {
                Debug.LogError($"{unitName} spawned at invalid grid position! World pos: {transform.position}, Calculated grid: ({startX}, {startY}), cellSize: {cellSize}. Grid position NOT initialized!");
            }
        }


        private void Start()
        {
            // Grid position should already be initialized in Awake(), but double-check
            // This is a fallback in case GridManager wasn't available in Awake()
            // First, retry getting GridManager if it's still null
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
                if (gridManager == null)
                {
                    // Try Instance property
                    gridManager = GridManager.Instance;
                }
                if (gridManager == null)
                {
                    // Last resort: try FindObjectOfType (slower but works if registry hasn't initialized)
                    gridManager = FindFirstObjectByType<GridManager>();
                    if (gridManager != null)
                    {
                        Debug.LogWarning($"{unitName} - GridManager found via FindObjectOfType in Start() (ManagerRegistry not ready)");
                    }
                    else
                    {
                        Debug.LogError($"{unitName} - GridManager still not available in Start()! Grid position cannot be initialized.");
                        return;
                    }
                }
                else
                {
                    Debug.Log($"{unitName} - GridManager now available in Start(), initializing grid position");
                }
            }
            
            // Now check if grid position needs initialization
            // Calculate what the grid position SHOULD be from current world position
            float cellSize = gridManager.CellSize;
            int expectedX = Mathf.FloorToInt(transform.position.x / cellSize);
            int expectedY = Mathf.FloorToInt(transform.position.z / cellSize);
            
            // Check if grid position is valid and matches world position
            bool needsInitialization = false;
            
            // First check: Is the grid position valid?
            if (!gridManager.IsValidGridPosition(GridX, GridY))
            {
                needsInitialization = true;
                Debug.LogWarning($"{unitName} - Grid position ({GridX}, {GridY}) is invalid, reinitializing in Start()");
            }
            // Second check: Does grid position match where we actually are in the world?
            else if (GridX != expectedX || GridY != expectedY)
            {
                needsInitialization = true;
                Debug.LogWarning($"{unitName} - Grid position ({GridX}, {GridY}) doesn't match world position (expected {expectedX}, {expectedY}), reinitializing in Start()");
            }
            // Third check: Does the grid cell exist and is the unit positioned at the cell center?
            else
            {
                GridCell cell = gridManager.GetCell(GridX, GridY);
                if (cell == null)
                {
                    needsInitialization = true;
                    Debug.LogWarning($"{unitName} - Grid cell at ({GridX}, {GridY}) is null, reinitializing in Start()");
                }
                else
                {
                    // Check if unit is actually at the cell center (within tolerance)
                    Vector3 expectedWorldPos = cell.WorldPosition;
                    float distanceFromCenter = Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z),
                        new Vector3(expectedWorldPos.x, 0, expectedWorldPos.z)
                    );
                    if (distanceFromCenter > 0.1f) // Not at cell center
                    {
                        needsInitialization = true;
                        Debug.LogWarning($"{unitName} - Unit at world {transform.position} is not at cell center {expectedWorldPos}, reinitializing in Start()");
                    }
                }
            }
            
            // Always initialize if we haven't done so yet, or if validation failed
            if (needsInitialization)
            {
                InitializeGridPosition();
            }

            // HP Display updates itself automatically in its Update() method

            // Equip starting equipment to appropriate slots
            if (meleeWeapon != null) EquipItem(meleeWeapon, EquipmentSlot.MeleeWeapon);
            if (rangedWeapon != null) EquipItem(rangedWeapon, EquipmentSlot.RangedWeapon);
            if (armor != null) EquipItem(armor, EquipmentSlot.Armor);
            if (accessory1 != null) EquipItem(accessory1, EquipmentSlot.Accessory1);
            if (accessory2 != null) EquipItem(accessory2, EquipmentSlot.Accessory2);
            if (codex != null) EquipItem(codex, EquipmentSlot.Codex);

            // Register with managers
            RegisterWithManagers();
        }

        private void OnEnable()
        {
            RegisterWithManagers();
        }

        private void OnDisable()
        {
            UnregisterFromManagers();
        }

        private void RegisterWithManagers()
        {
            // Register with TurnManager
            TurnManager turnManager = ManagerRegistry.Get<TurnManager>();
            if (turnManager != null)
            {
                turnManager.RegisterUnit(this);
            }

            // Register with PartyManager if player-controlled
            if (isPlayerControlled)
            {
                PartyManager partyManager = PartyManager.Instance;
                if (partyManager != null)
                {
                    partyManager.RegisterUnit(this);
                }
            }
        }

        private bool isUnregistering = false; // Guard to prevent infinite loops

        private void UnregisterFromManagers()
        {
            // Guard against infinite loops
            if (isUnregistering)
            {
                return;
            }

            isUnregistering = true;

            try
            {
                // Unregister from TurnManager
                TurnManager turnManager = ManagerRegistry.Get<TurnManager>();
                if (turnManager != null)
                {
                    turnManager.UnregisterUnit(this);
                }

                // Unregister from PartyManager if player-controlled
                if (isPlayerControlled)
                {
                    PartyManager partyManager = PartyManager.Instance;
                    if (partyManager != null)
                    {
                        partyManager.UnregisterUnit(this);
                    }
                }
            }
            finally
            {
                isUnregistering = false;
            }
        }

        /// <summary>
        /// Called at the start of this unit's turn to apply hazard damage and status effects.
        /// NOTE: This is called for ALL units (both player and enemy/AI units) when their turn starts.
        /// </summary>
        public void OnTurnStart()
        {
            // Reset action flags at start of turn
            MovementPointsRemaining = MovementRange; // Reset to full movement
            HasActedThisTurn = false;
            hazardsDamagedByThisTurn.Clear(); // Reset hazard damage tracking
            Debug.Log($"{unitName} turn started - Move: {MovementPointsRemaining}/{MaxMovementPoints}, actions reset");

            // Apply hazard damage if standing on hazard at turn start
            // Use cached manager references
            if (gridManager == null)
            {
                Debug.LogWarning($"[TURN START] {unitName} - GridManager is null, cannot check for hazards");
            }
            else if (hazardManager == null)
            {
                Debug.LogWarning($"[TURN START] {unitName} - HazardManager is null, cannot check for hazards");
            }
            else
            {
                Debug.Log($"[TURN START] {unitName} checking for hazards at ({GridX}, {GridY})");
                GridCell currentCell = gridManager.GetCell(GridX, GridY);
                if (currentCell == null)
                {
                    Debug.LogWarning($"[TURN START] {unitName} - Cell at ({GridX}, {GridY}) is null!");
                }
                else if (currentCell.Hazard == null)
                {
                    Debug.Log($"[TURN START] {unitName} at ({GridX}, {GridY}) - no hazard on cell");
                }
                else
                {
                    Debug.Log($"[TURN START] {unitName} standing on {currentCell.Hazard.Type} hazard at ({GridX}, {GridY})");
                    hazardManager.ApplyHazardDamageToUnit(this, currentCell);
                }
            }

            // Apply status effect damage (burn, poison, etc.)
            unitStatusEffects?.ApplyStatusEffectDamage();

            // Check if unit is stunned - if so, automatically end their turn
            if (IsStunned())
            {
                Debug.Log($"{unitName} is stunned and skips their turn!");
                TurnManager turnManager = ManagerRegistry.Get<TurnManager>();
                if (turnManager != null)
                {
                    // End turn immediately - unit cannot act
                    turnManager.EndTurn(this);
                }
                else
                {
                    Debug.LogWarning($"{unitName} is stunned but TurnManager is not available!");
                }
            }
        }

        /// <summary>
        /// Take damage from an attack. Returns actual damage dealt.
        /// </summary>
        /// <param name="incomingAttack">The attack power/damage value</param>
        /// <param name="damageSource">Optional source of the damage (e.g., "Burn", "Poison", "Attack") for logging</param>
        public int TakeDamage(int incomingAttack, string damageSource = null)
        {
            int damage = unitCombat.TakeDamage(incomingAttack, DefensePower);
            currentHP = unitCombat.CurrentHP;

            if (!string.IsNullOrEmpty(damageSource))
            {
                Debug.Log($"{unitName} took {damage} {damageSource} damage! HP: {currentHP}/{MaxHP}");
            }
            else
            {
                Debug.Log($"{unitName} took {damage} damage! HP: {currentHP}/{MaxHP}");
            }

            // Apply durability loss to armor
            if (unitEquipment != null && unitEquipment.EquippedItems.TryGetValue(EquipmentSlot.Armor, out var armor))
            {
                if (armor is EquipmentItem equipItem)
                {
                    equipItem.LoseDurability(damage * 0.1f);
                    
                    if (equipItem.IsBroken)
                    {
                        Debug.LogWarning($"{unitName}'s {equipItem.ItemName} is broken!");
                        // Recalculate stats since armor bonuses are now 0
                    }
                }
            }

            // Raise HP changed events (both local and global)
            OnHPChanged?.Invoke(currentHP, MaxHP);
            GameEvents.RaiseUnitHPChanged(this, currentHP, MaxHP);

            // Raise damage event for damage indicators
            if (damage > 0)
            {
                GameEvents.RaiseUnitDamaged(this, damage);
            }

            if (!IsAlive)
            {
                OnDeath();
            }

            return damage;
        }

        /// <summary>
        /// Take pre-calculated damage (defense already applied by CombatCalculator).
        /// Returns actual damage dealt.
        /// </summary>
        public int TakeDamageDirect(int finalDamage)
        {
            int damage = unitCombat.TakeDamageDirect(finalDamage);
            currentHP = unitCombat.CurrentHP;

            Debug.Log($"{unitName} took {damage} damage! HP: {currentHP}/{MaxHP}");

            // Apply durability loss to armor
            if (unitEquipment != null && unitEquipment.EquippedItems.TryGetValue(EquipmentSlot.Armor, out var armor))
            {
                if (armor is EquipmentItem equipItem)
                {
                    equipItem.LoseDurability(damage * 0.1f);
                    
                    if (equipItem.IsBroken)
                    {
                        Debug.LogWarning($"{unitName}'s {equipItem.ItemName} is broken!");
                        // Recalculate stats since armor bonuses are now 0
                    }
                }
            }

            // Raise HP changed events (both local and global)
            OnHPChanged?.Invoke(currentHP, MaxHP);
            GameEvents.RaiseUnitHPChanged(this, currentHP, MaxHP);

            // Raise damage event for damage indicators
            if (damage > 0)
            {
                GameEvents.RaiseUnitDamaged(this, damage);
            }

            if (!IsAlive)
            {
                OnDeath();
            }

            return damage;
        }

        /// <summary>
        /// Take damage that bypasses defense (for environmental hazards, poison, etc.)
        /// Returns actual damage dealt.
        /// </summary>
        public int TakeDamageBypassDefense(int incomingDamage)
        {
            int minDamage = GameConstants.Instance != null ? GameConstants.Instance.MinimumDamage : 1;
            int damage = Mathf.Max(minDamage, incomingDamage);
            
            unitCombat.SetHP(unitCombat.CurrentHP - damage, MaxHP);
            currentHP = unitCombat.CurrentHP;

            Debug.Log($"{unitName} took {damage} damage (bypassing defense)! HP: {currentHP}/{MaxHP}");

            // Raise HP changed events (both local and global)
            OnHPChanged?.Invoke(currentHP, MaxHP);
            GameEvents.RaiseUnitHPChanged(this, currentHP, MaxHP);

            // Raise damage event for damage indicators
            if (damage > 0)
            {
                GameEvents.RaiseUnitDamaged(this, damage);
            }

            if (!IsAlive)
            {
                OnDeath();
            }

            return damage;
        }

        /// <summary>
        /// Heal this unit by a specified amount.
        /// Returns the actual amount healed (may be less than requested if at max HP).
        /// </summary>
        public int Heal(int amount)
        {
            int actualHealing = unitCombat.Heal(amount, MaxHP);
            currentHP = unitCombat.CurrentHP;

            Debug.Log($"{unitName} healed for {actualHealing}! HP: {currentHP}/{MaxHP}");

            // Raise HP changed events (both local and global)
            OnHPChanged?.Invoke(currentHP, MaxHP);
            GameEvents.RaiseUnitHPChanged(this, currentHP, MaxHP);

            // Raise healing event for damage indicators
            if (actualHealing > 0)
            {
                GameEvents.RaiseUnitHealed(this, actualHealing);
            }

            return actualHealing;
        }

        /// <summary>
        /// Called when unit's HP reaches 0.
        /// </summary>
        private void OnDeath()
        {
            Debug.Log($"{unitName} has been defeated!");
            GameEvents.RaiseUnitDied(this);
            // Future: Play death animation, drop loot, etc.
        }

        /// <summary>
        /// Check if this unit can move to the target grid position.
        /// Uses pathfinding to determine if cell is actually reachable.
        /// </summary>
        public bool CanMoveTo(int targetX, int targetY)
        {
            // Cannot move if dead
            if (!IsAlive) return false;

            // Cannot move if already moving
            if (IsMoving) return false;

            // Check if within movement range (Manhattan distance)
            int distance = Mathf.Abs(targetX - GridX) + Mathf.Abs(targetY - GridY);
            if (distance > MovementRange) return false;
            
            // Check if destination is occupied
            if (gridManager != null)
            {
                GridCell targetCell = gridManager.GetCell(targetX, targetY);
                if (targetCell != null && targetCell.OccupyingUnit != null && targetCell.OccupyingUnit != this)
                {
                    return false; // Destination occupied
                }
            }
            
            // Check if cell is actually reachable via pathfinding
            if (gridManager != null)
            {
                HashSet<GridCell> reachable = gridManager.GetReachableCells(this, MovementRange);
                GridCell targetCell = gridManager.GetCell(targetX, targetY);
                
                if (targetCell != null && !reachable.Contains(targetCell))
                {
                    Debug.Log($"{unitName} cannot reach ({targetX}, {targetY}) - path blocked!");
                    return false; // Not reachable
                }
            }

            return true;
        }
        
        /// <summary>
        /// Check if path to target is blocked by enemies.
        /// Checks all cells along the Manhattan path.
        /// Allies don't block paths, but enemies do.
        /// </summary>
        public bool IsPathBlocked(int targetX, int targetY)
        {
            if (gridManager == null) return false;
            
            // Check all cells along the Manhattan path
            // Move horizontally first, then vertically (or vice versa - both need checking)
            
            int startX = GridX;
            int startY = GridY;
            
            // Path 1: Move X first, then Y
            if (CheckPathBlocked(startX, startY, targetX, startY)) return true; // Horizontal portion
            if (CheckPathBlocked(targetX, startY, targetX, targetY)) return true; // Vertical portion
            
            // Path 2: Move Y first, then X (alternate route)
            if (CheckPathBlocked(startX, startY, startX, targetY)) return true; // Vertical portion
            if (CheckPathBlocked(startX, targetY, targetX, targetY)) return true; // Horizontal portion
            
            return false;
        }
        
        /// <summary>
        /// Check if a straight line path (horizontal or vertical) is blocked by enemies.
        /// </summary>
        private bool CheckPathBlocked(int fromX, int fromY, int toX, int toY)
        {
            // Horizontal movement
            if (fromY == toY)
            {
                int step = fromX < toX ? 1 : -1;
                for (int x = fromX; x != toX; x += step)
                {
                    if (IsCellBlockedByEnemy(x, fromY))
                    {
                        Debug.Log($"{unitName} path blocked by enemy at ({x}, {fromY})");
                        return true;
                    }
                }
            }
            // Vertical movement
            else if (fromX == toX)
            {
                int step = fromY < toY ? 1 : -1;
                for (int y = fromY; y != toY; y += step)
                {
                    if (IsCellBlockedByEnemy(fromX, y))
                    {
                        Debug.Log($"{unitName} path blocked by enemy at ({fromX}, {y})");
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a specific cell contains an enemy unit.
        /// </summary>
        private bool IsCellBlockedByEnemy(int x, int y)
        {
            if (gridManager == null) return false;
            
            GridCell cell = gridManager.GetCell(x, y);
            if (cell == null) return false;
            
            Unit occupant = cell.OccupyingUnit;
            if (occupant == null || occupant == this) return false;
            
            // Enemy blocks path (hostile faction)
            FactionRelationship factionRel = FactionRelationship.Instance ?? FindFirstObjectByType<FactionRelationship>();
            if (factionRel != null)
            {
                return factionRel.AreHostile(this.Faction, occupant.Faction);
            }
            
            // Fallback: different faction = enemy
            return occupant.Faction != this.Faction;
        }

        /// <summary>
        /// Apply a status effect to this unit using StatusEffectData.
        /// </summary>
        public void ApplyStatusEffect(Combat.StatusEffectData effectData, int duration)
        {
            unitStatusEffects?.ApplyStatusEffect(effectData, duration);
        }

        /// <summary>
        /// Apply a burn effect to this unit.
        /// Legacy method for backward compatibility.
        /// </summary>
        public void ApplyBurn(int damagePerTurn, int duration)
        {
            unitStatusEffects?.ApplyBurn(damagePerTurn, duration);
        }

        /// <summary>
        /// Get total hit chance modifier from all active status effects.
        /// </summary>
        public float GetTotalHitChanceModifier()
        {
            return unitStatusEffects?.GetTotalHitChanceModifier() ?? 0f;
        }

        /// <summary>
        /// Get total critical hit chance modifier from all active status effects.
        /// </summary>
        public float GetTotalCritChanceModifier()
        {
            return unitStatusEffects?.GetTotalCritChanceModifier() ?? 0f;
        }

        /// <summary>
        /// Get total parry chance modifier from all active status effects.
        /// </summary>
        public float GetTotalParryChanceModifier()
        {
            return unitStatusEffects?.GetTotalParryChanceModifier() ?? 0f;
        }

        /// <summary>
        /// Get total critical defense modifier from all active status effects.
        /// </summary>
        public float GetTotalCritDefenseModifier()
        {
            return unitStatusEffects?.GetTotalCritDefenseModifier() ?? 0f;
        }

        /// <summary>
        /// Check if this unit is stunned (has any status effect that prevents actions).
        /// </summary>
        public bool IsStunned()
        {
            return unitStatusEffects?.IsStunned() ?? false;
        }

        /// <summary>
        /// Check if this unit has a specific skill.
        /// </summary>
        public bool HasSkill(Skill skill)
        {
            return knownSkills.Contains(skill);
        }

        /// <summary>
        /// Add a skill to this unit's known skills.
        /// </summary>
        public void LearnSkill(Skill skill)
        {
            if (!knownSkills.Contains(skill))
            {
                knownSkills.Add(skill);
                Debug.Log($"{name} learned {skill.SkillName}!");
            }
        }

        #region Equipment System

        /// <summary>
        /// Equip an item to a specific slot.
        /// Item must be compatible with the chosen slot.
        /// </summary>
        public bool EquipItem(EquipmentItem item, EquipmentSlot targetSlot)
        {
            return unitEquipment?.EquipItem(item, targetSlot) ?? false;
        }
        
        /// <summary>
        /// Auto-equip an item to first available compatible slot.
        /// Useful for starting equipment.
        /// </summary>
        public bool EquipItem(EquipmentItem item)
        {
            return unitEquipment?.EquipItem(item) ?? false;
        }

        /// <summary>
        /// Unequip item from a slot.
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            return unitEquipment?.UnequipItem(slot) ?? false;
        }

        /// <summary>
        /// Get the item equipped in a specific slot.
        /// </summary>
        public EquipmentItem GetEquippedItem(EquipmentSlot slot)
        {
            return unitEquipment?.GetEquippedItem(slot);
        }

        /// <summary>
        /// Can this unit use a specific skill?
        /// Checks mastered skills AND equipped items.
        /// </summary>
        public bool CanUseSkill(Skill skill)
        {
            return unitEquipment?.CanUseSkill(skill) ?? false;
        }

        /// <summary>
        /// Get all skills this unit can currently use (mastered + equipped).
        /// </summary>
        public List<Skill> GetAvailableSkills()
        {
            return unitEquipment?.GetAvailableSkills() ?? new List<Skill>();
        }
        
        /// <summary>
        /// Calculate MaxHP including base value, equipment bonuses, and passive skill bonuses.
        /// Flat bonuses are added first, then percentage bonuses scale the total.
        /// </summary>
        private int CalculateMaxHP()
        {
            if (unitEquipment != null && unitCombat != null)
            {
                int flatBonus = unitEquipment.GetTotalMaxHPBonusFlat();
                float percentBonus = unitEquipment.GetTotalMaxHPBonusPercent();
                unitCombat.RecalculateMaxHP(maxHP, flatBonus, percentBonus);
                return unitCombat.MaxHP;
            }
            
            // Fallback calculation
            int baseHP = maxHP;
            int fallbackFlatBonus = 0;
            float fallbackPercentBonus = 0f;
            
            if (unitEquipment != null)
            {
                fallbackFlatBonus = unitEquipment.GetTotalMaxHPBonusFlat();
                fallbackPercentBonus = unitEquipment.GetTotalMaxHPBonusPercent();
            }
            
            float totalHP = (baseHP + fallbackFlatBonus) * (1f + (fallbackPercentBonus / 100f));
            return Mathf.RoundToInt(totalHP);
        }

        #endregion

        #region Inventory System

        /// <summary>
        /// Helper method to get the maximum container reduction from all equipped containers.
        /// </summary>
        private float GetContainerReduction()
        {
            float maxReduction = 0f;
            
            foreach (var container in containerSlots)
            {
                if (container != null)
                {
                    maxReduction = Mathf.Max(maxReduction, container.EncumbranceReduction);
                }
            }
            
            return maxReduction;
        }

        /// <summary>
        /// Helper method to get total container capacity from all equipped containers.
        /// </summary>
        private int GetTotalContainerCapacity()
        {
            int total = 0;
            
            foreach (var container in containerSlots)
            {
                if (container != null)
                {
                    total += container.SlotCapacity;
                }
            }
            
            return total;
        }

        /// <summary>
        /// Add an item to the inventory.
        /// Returns true if item was added successfully.
        /// </summary>
        public bool AddItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;
            
            // Validate no container nesting
            if (item is ContainerItem containerToAdd)
            {
                foreach (var slot in containerInventory)
                {
                    if (slot != null && slot.Item is ContainerItem)
                    {
                        Debug.LogWarning("Cannot nest containers inside containers!");
                        return false;
                    }
                }
            }
            
            int remaining = quantity;
            
            // Try to stack with existing slots first
            foreach (var slot in inventory)
            {
                if (slot != null && slot.CanStack(item))
                {
                    remaining = slot.AddToStack(remaining);
                    if (remaining <= 0)
                        return true;
                }
            }
            
            // Create new slots for remaining quantity
            while (remaining > 0)
            {
                int stackSize = Mathf.Min(remaining, item.MaxStackSize);
                inventory.Add(new InventorySlot(item, stackSize));
                remaining -= stackSize;
            }
            
            return true;
        }

        /// <summary>
        /// Remove an item from the inventory.
        /// Returns true if at least some quantity was removed.
        /// </summary>
        public bool RemoveItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;
            
            int remaining = quantity;
            
            // Remove from slots
            for (int i = inventory.Count - 1; i >= 0; i--)
            {
                var slot = inventory[i];
                if (slot != null && slot.Item == item)
                {
                    int removed = slot.RemoveFromStack(remaining);
                    remaining -= removed;
                    
                    if (slot.IsEmpty())
                        inventory.RemoveAt(i);
                    
                    if (remaining <= 0)
                        return true;
                }
            }
            
            return remaining < quantity; // Returns true if at least some was removed
        }

        /// <summary>
        /// Get the total count of a specific item in inventory.
        /// </summary>
        public int GetItemCount(Item item)
        {
            int count = 0;
            
            foreach (var slot in inventory)
            {
                if (slot != null && slot.Item == item)
                    count += slot.Quantity;
            }
            
            return count;
        }

        /// <summary>
        /// Check if unit has a specific item in the required quantity.
        /// </summary>
        public bool HasItem(Item item, int quantity = 1)
        {
            return GetItemCount(item) >= quantity;
        }

        /// <summary>
        /// Equip a container to a container slot.
        /// Returns true if successful.
        /// </summary>
        public bool EquipContainer(ContainerItem container, int slotIndex)
        {
            if (container == null)
                return false;
            
            if (slotIndex < 0 || slotIndex >= maxContainerSlots)
            {
                Debug.LogWarning($"Invalid container slot index: {slotIndex}");
                return false;
            }
            
            // Check if item is in inventory
            if (!HasItem(container, 1))
            {
                Debug.LogWarning("Container not in inventory!");
                return false;
            }
            
            // Unequip existing container in this slot
            if (containerSlots[slotIndex] != null)
            {
                UnequipContainer(slotIndex);
            }
            
            // Equip new container
            containerSlots[slotIndex] = container;
            RemoveItem(container, 1);
            
            Debug.Log($"Equipped {container.ItemName} to container slot {slotIndex}");
            return true;
        }

        /// <summary>
        /// Unequip a container from a container slot.
        /// Items in the container are moved to main inventory.
        /// </summary>
        public void UnequipContainer(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxContainerSlots)
                return;
            
            var container = containerSlots[slotIndex];
            if (container == null)
                return;
            
            // Move all items from container inventory to main inventory
            foreach (var slot in containerInventory)
            {
                if (slot != null && slot.Item != null)
                {
                    AddItem(slot.Item, slot.Quantity);
                }
            }
            
            containerInventory.Clear();
            
            // Return container to inventory
            AddItem(container, 1);
            containerSlots[slotIndex] = null;
            
            Debug.Log($"Unequipped {container.ItemName} from slot {slotIndex}");
        }

        /// <summary>
        /// Move an item from main inventory to container inventory.
        /// Returns true if successful.
        /// </summary>
        public bool MoveToContainer(Item item, int quantity = 1)
        {
            if (GetTotalContainerCapacity() <= 0)
            {
                Debug.LogWarning("No containers equipped!");
                return false;
            }
            
            if (containerInventory.Count >= GetTotalContainerCapacity())
            {
                Debug.LogWarning("Container is full!");
                return false;
            }
            
            if (!HasItem(item, quantity))
                return false;
            
            // Prevent container nesting
            if (item is ContainerItem)
            {
                Debug.LogWarning("Cannot put containers inside containers!");
                return false;
            }
            
            RemoveItem(item, quantity);
            
            // Add to container inventory
            int remaining = quantity;
            
            foreach (var slot in containerInventory)
            {
                if (slot != null && slot.CanStack(item))
                {
                    remaining = slot.AddToStack(remaining);
                    if (remaining <= 0)
                        return true;
                }
            }
            
            while (remaining > 0 && containerInventory.Count < GetTotalContainerCapacity())
            {
                int stackSize = Mathf.Min(remaining, item.MaxStackSize);
                containerInventory.Add(new InventorySlot(item, stackSize));
                remaining -= stackSize;
            }
            
            return true;
        }

        /// <summary>
        /// Upgrade the maximum number of container slots.
        /// </summary>
        public void UpgradeContainerSlots(int newMax)
        {
            if (newMax > maxContainerSlots)
            {
                Array.Resize(ref containerSlots, newMax);
                maxContainerSlots = newMax;
                Debug.Log($"Container slots upgraded to {newMax}");
            }
        }

        /// <summary>
        /// Spend Aurum Shards.
        /// Returns true if successful, false if insufficient balance.
        /// </summary>
        public bool SpendAurumShards(int amount)
        {
            if (amount <= 0 || aurumShards < amount)
                return false;
            
            aurumShards -= amount;
            Debug.Log($"{unitName} spent {amount} Aurum Shards. Remaining: {aurumShards}");
            return true;
        }

        /// <summary>
        /// Gain Aurum Shards.
        /// </summary>
        public void GainAurumShards(int amount)
        {
            if (amount <= 0)
                return;
            
            aurumShards += amount;
            Debug.Log($"{unitName} gained {amount} Aurum Shards. Total: {aurumShards}");
        }

        /// <summary>
        /// Distribute an item among party members.
        /// </summary>
        public void DistributeItem(Item item, int totalQuantity, List<Unit> partyMembers)
        {
            if (partyMembers == null || partyMembers.Count == 0)
                return;
            
            if (!HasItem(item, totalQuantity))
            {
                Debug.LogWarning($"Insufficient {item.ItemName} to distribute!");
                return;
            }
            
            RemoveItem(item, totalQuantity);
            
            int perMember = totalQuantity / partyMembers.Count;
            int remainder = totalQuantity % partyMembers.Count;
            
            for (int i = 0; i < partyMembers.Count; i++)
            {
                int amount = perMember + (i < remainder ? 1 : 0);
                partyMembers[i].AddItem(item, amount);
            }
            
            Debug.Log($"Distributed {totalQuantity} {item.ItemName} among {partyMembers.Count} party members");
        }

        #endregion

        /// <summary>
        /// Spend movement points (called after moving N cells).
        /// Returns true if movement was allowed, false if not enough points.
        /// </summary>
        public bool SpendMovementPoints(int points)
        {
            // Cannot move after acting (unless haven't moved at all yet)
            if (HasActedThisTurn && MovementPointsRemaining < MaxMovementPoints)
            {
                Debug.Log($"{unitName} cannot move - already acted after moving!");
                return false;
            }

            if (points > MovementPointsRemaining)
            {
                Debug.Log($"{unitName} cannot move {points} cells - only {MovementPointsRemaining} movement remaining!");
                return false;
            }

            MovementPointsRemaining -= points;
            // Ensure it doesn't go below 0
            MovementPointsRemaining = Mathf.Max(0, MovementPointsRemaining);
            Debug.Log($"{unitName} moved {points} cells - {MovementPointsRemaining}/{MaxMovementPoints} movement remaining");
            return true;
        }

        /// <summary>
        /// Force-spend movement points (used when movement completes to prevent infinite movement bug).
        /// This bypasses normal checks and always deducts points, even if the unit has acted.
        /// </summary>
        public void ForceSpendMovementPoints(int points)
        {
            if (points <= 0)
            {
                Debug.LogWarning($"{unitName} ForceSpendMovementPoints called with invalid points: {points}");
                return;
            }

            // Always spend points, even if it would normally be blocked
            // This prevents the infinite movement bug when movement completes after hazard damage
            MovementPointsRemaining = Mathf.Max(0, MovementPointsRemaining - points);
            Debug.Log($"{unitName} force-spent {points} movement points - {MovementPointsRemaining}/{MaxMovementPoints} remaining");
        }

        /// <summary>
        /// Mark that this unit has taken an action (attack, skill, etc).
        /// If unit has moved, this locks out further movement.
        /// </summary>
        public void MarkAsActed()
        {
            HasActedThisTurn = true;
            
            // If unit has moved (not at max movement), lock movement
            if (MovementPointsRemaining < MaxMovementPoints)
            {
                MovementPointsRemaining = 0; // Can't move anymore after acting
                Debug.Log($"{unitName} acted after moving - movement locked");
            }
            else
            {
                // Unit hasn't moved yet, so can still move after acting
                Debug.Log($"{unitName} acted (can still move after)");
            }
        }

        /// <summary>
        /// Check if this unit has already been damaged by this hazard type this turn.
        /// </summary>
        public bool HasBeenDamagedByHazard(HazardTile.HazardType hazardType)
        {
            return hazardsDamagedByThisTurn.Contains(hazardType);
        }

        /// <summary>
        /// Mark that this unit has been damaged by a specific hazard type this turn.
        /// </summary>
        public void MarkDamagedByHazard(HazardTile.HazardType hazardType)
        {
            hazardsDamagedByThisTurn.Add(hazardType);
            Debug.Log($"{unitName} marked as damaged by {hazardType} this turn");
        }
        
        #region Progression System
        
        /// <summary>
        /// Calculate XP required for a given level.
        /// Formula: 100 × (1.5 ^ (level - 1))
        /// Level 1→2: 100 XP, Level 2→3: 150 XP, Level 3→4: 225 XP, etc.
        /// </summary>
        public int GetXPRequiredForLevel(int targetLevel)
        {
            return unitProgression?.GetXPRequiredForLevel(targetLevel) ?? Mathf.RoundToInt(100 * Mathf.Pow(1.5f, targetLevel - 2));
        }
        
        /// <summary>
        /// Get XP required to reach next level.
        /// </summary>
        public int GetXPRequiredForNextLevel()
        {
            return unitProgression?.GetXPRequiredForNextLevel() ?? GetXPRequiredForLevel(level + 1);
        }
        
        /// <summary>
        /// Award XP to this unit and handle leveling up.
        /// </summary>
        /// <summary>
        /// Deprecated: XP system has been replaced with weapon proficiency system.
        /// This method is kept for backward compatibility but does nothing.
        /// </summary>
        [System.Obsolete("XP system has been replaced with weapon proficiency system. Use proficiency tracking instead.")]
        public void AwardXP(int amount)
        {
            // No-op: XP system removed in favor of weapon proficiency system
        }
        
        /// <summary>
        /// Record a successful action and award SP based on action count.
        /// Call this for attacks, skill uses, etc.
        /// </summary>
        public void RecordAction()
        {
            unitProgression?.RecordAction();
            totalActions = unitProgression?.TotalActions ?? totalActions;
            skillPoints = unitProgression?.SkillPoints ?? skillPoints;
        }
        
        /// <summary>
        /// Spend SP to master a combat skill permanently.
        /// Returns true if successful, false if not enough SP or already mastered.
        /// </summary>
        public bool MasterSkill(Skill skill)
        {
            bool result = unitProgression?.MasterSkill(skill) ?? false;
            skillPoints = unitProgression?.SkillPoints ?? skillPoints;
            return result;
        }
        
        /// <summary>
        /// Check if a skill is mastered.
        /// </summary>
        public bool IsSkillMastered(Skill skill)
        {
            return unitProgression?.IsSkillMastered(skill) ?? false;
        }
        
        /// <summary>
        /// Spend SP to master a passive skill permanently.
        /// Returns true if successful, false if not enough SP or already mastered.
        /// </summary>
        public bool MasterPassiveSkill(PassiveSkill passiveSkill)
        {
            bool result = unitProgression?.MasterPassiveSkill(passiveSkill, CalculateMaxHP, (newHP) => { currentHP = newHP; unitCombat?.SetHP(newHP, MaxHP); }) ?? false;
            skillPoints = unitProgression?.SkillPoints ?? skillPoints;
            return result;
        }
        
        /// <summary>
        /// Check if a passive skill is mastered.
        /// </summary>
        public bool IsPassiveSkillMastered(PassiveSkill passiveSkill)
        {
            return unitProgression?.IsPassiveSkillMastered(passiveSkill) ?? false;
        }
        
        /// <summary>
        /// Get all passive skills currently available from equipment.
        /// </summary>
        public List<PassiveSkill> GetAvailablePassiveSkills()
        {
            return unitEquipment?.GetAvailablePassiveSkills() ?? new List<PassiveSkill>();
        }
        
        #endregion
        
        #region Narrative Skills
        
        /// <summary>
        /// Get the narrative skill level for a specific category.
        /// </summary>
        public int GetNarrativeSkillLevel(Skills.NarrativeSkillCategory category)
        {
            if (narrativeSkills.ContainsKey(category))
            {
                return narrativeSkills[category];
            }
            return 0;
        }
        
        /// <summary>
        /// Get all narrative skills as a dictionary.
        /// </summary>
        public Dictionary<Skills.NarrativeSkillCategory, int> NarrativeSkills => new Dictionary<Skills.NarrativeSkillCategory, int>(narrativeSkills);
        
        #endregion

        #region CharacterState Integration

        [Header("Character State (Runtime)")]
        [Tooltip("Reference to CharacterState this Unit represents (for battle mode)")]
        [SerializeField] private CharacterState characterState;

        /// <summary>
        /// Get the CharacterState this Unit represents.
        /// </summary>
        public CharacterState CharacterState => characterState;

        /// <summary>
        /// Update Unit from CharacterState (for battle initialization).
        /// Syncs all stats, equipment, skills, and progression.
        /// </summary>
        public void UpdateFromCharacterState(CharacterState state)
        {
            if (state == null)
            {
                Debug.LogWarning($"Unit {unitName}: Cannot update from null CharacterState!");
                return;
            }

            characterState = state;

            // Update basic info
            if (state.Definition != null)
            {
                unitName = state.Definition.CharacterName;
                portrait = state.Definition.Portrait;
                mantle = state.Definition.Mantle;
            }

            // Update stats (set base stats, equipment will add bonuses)
            strength = state.CurrentStrength;
            finesse = state.CurrentFinesse;
            focus = state.CurrentFocus;
            speed = state.CurrentSpeed;
            luck = state.CurrentLuck;

            // Update UnitStats component if it exists
            if (unitStats != null)
            {
                unitStats.SetStrength(strength);
                unitStats.SetFinesse(finesse);
                unitStats.SetFocus(focus);
                unitStats.SetSpeed(speed);
                unitStats.SetLuck(luck);
            }

            // Update HP
            maxHP = state.MaxHP;
            currentHP = state.CurrentHP;
            if (unitCombat != null)
            {
                unitCombat.SetHP(currentHP, maxHP);
            }

            // Update progression
            level = state.Level;
            currentXP = state.CurrentXP;
            skillPoints = state.SkillPoints;
            totalActions = state.TotalActions;

            // Update weapon proficiencies
            if (weaponProficiencyManager != null && state.WeaponProficiencies != null)
            {
                Dictionary<WeaponFamily, WeaponProficiency> proficiencyData = new Dictionary<WeaponFamily, WeaponProficiency>();
                foreach (var kvp in state.WeaponProficiencies)
                {
                    proficiencyData[kvp.Key] = kvp.Value;
                }
                weaponProficiencyManager.InitializeFromData(proficiencyData);
            }

            // Update UnitProgression component if it exists
            if (unitProgression != null)
            {
                // Note: UnitProgression doesn't have setters, so we'll need to sync through reflection or recreate
                // For now, the serialized fields are updated above
            }

            // Update equipment
            if (unitEquipment != null)
            {
                // Clear existing equipment
                foreach (var slot in System.Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>())
                {
                    unitEquipment.UnequipItem(slot);
                }

                // Equip items from CharacterState
                foreach (var kvp in state.EquippedItems)
                {
                    unitEquipment.EquipItem(kvp.Value, kvp.Key);
                }
            }
            else
            {
                // Set serialized equipment fields directly
                meleeWeapon = state.GetEquippedItem(EquipmentSlot.MeleeWeapon);
                rangedWeapon = state.GetEquippedItem(EquipmentSlot.RangedWeapon);
                armor = state.GetEquippedItem(EquipmentSlot.Armor);
                accessory1 = state.GetEquippedItem(EquipmentSlot.Accessory1);
                accessory2 = state.GetEquippedItem(EquipmentSlot.Accessory2);
                codex = state.GetEquippedItem(EquipmentSlot.Codex);
            }

            // Update known skills
            knownSkills.Clear();
            knownSkills.AddRange(state.GetAvailableSkills());

            // Update mastered skills (sync with UnitProgression)
            if (unitProgression != null)
            {
                // Clear existing mastered skills
                var masteredSkillsSet = unitProgression.MasteredSkills;
                masteredSkillsSet.Clear();

                // Add mastered skills from CharacterState
                foreach (var skill in state.MasteredSkills)
                {
                    masteredSkillsSet.Add(skill);
                }

                // Update mastered passive skills
                var masteredPassivesSet = unitProgression.MasteredPassiveSkills;
                masteredPassivesSet.Clear();
                foreach (var passive in state.MasteredPassiveSkills)
                {
                    masteredPassivesSet.Add(passive);
                }
            }

            // Update narrative skills
            narrativeSkills[Skills.NarrativeSkillCategory.Perception] = state.CurrentPerception;
            narrativeSkills[Skills.NarrativeSkillCategory.Interpretive] = state.CurrentInterpretive;
            narrativeSkills[Skills.NarrativeSkillCategory.Empathic] = state.CurrentEmpathic;

            // Update status effects (apply from CharacterState)
            if (unitStatusEffects != null)
            {
                // Clear existing status effects
                // Note: UnitStatusEffects doesn't have a clear method, so we'll need to handle this
                // For now, status effects will be applied as they occur in battle
            }

            // Sync inventory from CharacterState
            inventory.Clear();
            if (state.Inventory != null)
            {
                foreach (var slot in state.Inventory)
                {
                    if (slot != null && slot.Item != null && !slot.IsEmpty())
                    {
                        inventory.Add(new InventorySlot(slot.Item, slot.Quantity));
                    }
                }
            }

            containerInventory.Clear();
            if (state.ContainerInventory != null)
            {
                foreach (var slot in state.ContainerInventory)
                {
                    if (slot != null && slot.Item != null && !slot.IsEmpty())
                    {
                        containerInventory.Add(new InventorySlot(slot.Item, slot.Quantity));
                    }
                }
            }

            // Copy container slots
            if (state.ContainerSlots != null)
            {
                if (containerSlots == null || containerSlots.Length != state.ContainerSlots.Length)
                {
                    containerSlots = new ContainerItem[state.ContainerSlots.Length];
                }
                for (int i = 0; i < containerSlots.Length && i < state.ContainerSlots.Length; i++)
                {
                    containerSlots[i] = state.ContainerSlots[i];
                }
            }

            // Copy currency
            aurumShards = state.AurumShards;

            Debug.Log($"Unit {unitName}: Updated from CharacterState {state.CharacterID}");
        }

        /// <summary>
        /// Export Unit state back to CharacterState (for battle → exploration transition).
        /// Updates CharacterState with current Unit values.
        /// </summary>
        public void ExportToCharacterState(CharacterState state)
        {
            if (state == null)
            {
                Debug.LogWarning($"Unit {unitName}: Cannot export to null CharacterState!");
                return;
            }

            // Update CharacterState from Unit
            state.UpdateFromUnit(this);

            Debug.Log($"Unit {unitName}: Exported state to CharacterState {state.CharacterID}");
        }

        /// <summary>
        /// Sync inventory from Unit back to CharacterState.
        /// Called after battle ends or when returning to exploration to persist inventory changes.
        /// </summary>
        public void SyncInventoryToCharacterState()
        {
            if (characterState == null)
            {
                Debug.LogWarning($"Unit {unitName}: Cannot sync inventory - CharacterState is null!");
                return;
            }

            // Clear and copy inventory back to CharacterState
            characterState.ClearInventory();
            foreach (var slot in inventory)
            {
                if (slot != null && slot.Item != null && !slot.IsEmpty())
                {
                    characterState.AddItem(slot.Item, slot.Quantity);
                }
            }

            // Clear and copy container inventory
            characterState.ClearContainerInventory();
            foreach (var slot in containerInventory)
            {
                if (slot != null && slot.Item != null && !slot.IsEmpty())
                {
                    characterState.AddToContainerInventory(slot.Item, slot.Quantity);
                }
            }

            // Copy container slots
            characterState.SetContainerSlots(containerSlots);

            // Copy currency
            characterState.SetAurumShards(aurumShards);

            Debug.Log($"Unit {unitName}: Synced inventory to CharacterState {characterState.CharacterID}");
        }

        #endregion

        #region Debug Helpers

        [ContextMenu("Debug: Show Inventory")]
        public void DebugShowInventory()
        {
            Debug.Log($"=== {unitName} Inventory ===");
            Debug.Log($"Aurum Shards: {aurumShards}");
            Debug.Log($"Weight: {CurrentWeight:F2} / {EffectiveCarryCapacity:F2} kg ({EncumbrancePercent:P0})");
            Debug.Log($"Encumbered: {IsOverencumbered}");
            
            Debug.Log("\nMain Inventory:");
            foreach (var slot in inventory)
            {
                if (slot != null && slot.Item != null)
                    Debug.Log($"  {slot.Item.ItemName} x{slot.Quantity} ({slot.GetTotalWeight():F2} kg)");
            }
            
            Debug.Log("\nContainer Slots:");
            for (int i = 0; i < containerSlots.Length; i++)
            {
                var container = containerSlots[i];
                Debug.Log($"  Slot {i}: {(container != null ? container.ItemName : "Empty")}");
            }
            
            if (containerInventory.Count > 0)
            {
                Debug.Log("\nContainer Inventory:");
                foreach (var slot in containerInventory)
                {
                    if (slot != null && slot.Item != null)
                    {
                        float reducedWeight = slot.GetTotalWeight() * (1f - GetContainerReduction());
                        Debug.Log($"  {slot.Item.ItemName} x{slot.Quantity} ({reducedWeight:F2} kg after reduction)");
                    }
                }
            }
        }

        [ContextMenu("Debug: Add 100 Aurum Shards")]
        public void DebugAddCurrency()
        {
            GainAurumShards(100);
        }

        [ContextMenu("Debug: Overencumber")]
        public void DebugOverencumber()
        {
            // Add heavy items to test encumbrance
            // Use reflection to set protected fields for debug purposes
            var dummyItem = ScriptableObject.CreateInstance<LootItem>();
            var itemNameField = typeof(Item).GetField("itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var weightField = typeof(Item).GetField("weight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (itemNameField != null && weightField != null)
            {
                itemNameField.SetValue(dummyItem, "Heavy Rock");
                weightField.SetValue(dummyItem, 50f);
                AddItem(dummyItem, 5);
            }
            else
            {
                Debug.LogWarning("DebugOverencumber: Could not set item fields via reflection. Create a test item manually.");
            }
        }

        #endregion
    }
}