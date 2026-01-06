using UnityEngine;
using Riftbourne.Grid;
using Riftbourne.Skills;
using Riftbourne.Combat;
using Riftbourne.UI;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Characters
{
    public class Unit : Character
    {
        private GridManager gridManager;

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

        [Header("Unit Identity")]
        [SerializeField] private string unitName = "Unit";
        [SerializeField] private bool isPlayerControlled = true;
        
        [Header("Progression")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int skillPoints = 0;  // SP for mastering skills
        [SerializeField] private int totalActions = 0;  // Track total successful actions
        
        // SP progression tracking
        private int actionsPerSP = 5;  // Award 1 SP per 5 actions (configurable)
        private int lastSPAwardedAtAction = 0;  // Track when we last awarded SP

        // Burn status effect tracking
        private BurnEffect burnEffect = null;

        // Equipment system
        private Dictionary<EquipmentSlot, EquipmentItem> equippedItems = new Dictionary<EquipmentSlot, EquipmentItem>();

        // SP-based mastery tracking (replaces old usage-based system)
        private HashSet<Skill> masteredSkills = new HashSet<Skill>();
        private HashSet<PassiveSkill> masteredPassiveSkills = new HashSet<PassiveSkill>();

        // Public properties
        public List<Skill> KnownSkills => knownSkills;
        public bool IsBurning => burnEffect != null && !burnEffect.IsExpired;
        public BurnEffect BurnEffect => burnEffect;
        public int MaxHP => CalculateMaxHP();
        public int CurrentHP => currentHP;
        public int AttackPower => attackPower + GetTotalEquipmentBonus("Attack"); // Legacy + equipment
        public int DefensePower => defensePower + GetTotalEquipmentBonus("Defense"); // Legacy + equipment
        
        // Core Attributes (include equipment bonuses + passive skill bonuses)
        public int Strength => strength + GetTotalEquipmentBonus("Strength") + GetTotalPassiveSkillBonus("Strength");
        public int Finesse => finesse + GetTotalEquipmentBonus("Finesse") + GetTotalPassiveSkillBonus("Finesse");
        public int Focus => focus + GetTotalEquipmentBonus("Focus") + GetTotalPassiveSkillBonus("Focus");
        public int Speed => speed + GetTotalEquipmentBonus("Speed") + GetTotalPassiveSkillBonus("Speed");  // Replaces Initiative for turn order
        public int Luck => luck + GetTotalEquipmentBonus("Luck") + GetTotalPassiveSkillBonus("Luck");
        
        public string UnitName => unitName;
        public bool IsPlayerControlled => isPlayerControlled;
        public bool IsAlive => currentHP > 0;
        
        // Progression
        public int Level => level;
        public int CurrentXP => currentXP;
        public int SkillPoints => skillPoints;
        public int TotalActions => totalActions;

        // Action tracking for turn-based gameplay
        public int MovementPointsRemaining { get; private set; }
        public int MaxMovementPoints => MovementRange; // Movement points = movement range
        public bool HasActedThisTurn { get; private set; }

        // Hazard damage tracking - only take damage once per hazard type per turn
        private HashSet<HazardTile.HazardType> hazardsDamagedByThisTurn = new HashSet<HazardTile.HazardType>();

        private void Awake()
        {
            currentHP = maxHP;
            gridManager = FindFirstObjectByType<GridManager>();
            
            // Initialize movement points
            MovementPointsRemaining = MovementRange;
        }


        private void Start()
        {
            GridManager gridManager = FindFirstObjectByType<GridManager>();
            
            // Initialize grid position based on world position
            int startX = Mathf.FloorToInt(transform.position.x);
            int startY = Mathf.FloorToInt(transform.position.z);

            if (gridManager != null && gridManager.IsValidGridPosition(startX, startY))
            {
                // Get the proper centered world position from the grid cell
                GridCell cell = gridManager.GetCell(startX, startY);
                if (cell != null)
                {
                    Vector3 centeredPosition = cell.WorldPosition;
                    centeredPosition.y = 0.5f; // Keep unit elevated
                    
                    SetGridPosition(startX, startY, centeredPosition);
                    transform.position = centeredPosition; // Snap to cell center
                    
                    Debug.Log($"{unitName} initialized at grid position ({startX}, {startY}) - world pos: {centeredPosition}");
                }
            }
            else
            {
                Debug.LogWarning($"{unitName} spawned at invalid grid position!");
            }

            // HP Display updates itself automatically in its Update() method

            // Equip starting equipment to appropriate slots
            if (meleeWeapon != null) EquipItem(meleeWeapon, EquipmentSlot.MeleeWeapon);
            if (rangedWeapon != null) EquipItem(rangedWeapon, EquipmentSlot.RangedWeapon);
            if (armor != null) EquipItem(armor, EquipmentSlot.Armor);
            if (accessory1 != null) EquipItem(accessory1, EquipmentSlot.Accessory1);
            if (accessory2 != null) EquipItem(accessory2, EquipmentSlot.Accessory2);
            if (codex != null) EquipItem(codex, EquipmentSlot.Codex);
        }

        /// <summary>
        /// Called at the start of this unit's turn to apply burn damage.
        /// </summary>
        public void OnTurnStart()
        {
            // Reset action flags at start of turn
            MovementPointsRemaining = MovementRange; // Reset to full movement
            HasActedThisTurn = false;
            hazardsDamagedByThisTurn.Clear(); // Reset hazard damage tracking
            Debug.Log($"{unitName} turn started - Move: {MovementPointsRemaining}/{MaxMovementPoints}, actions reset");

            // Apply hazard damage if standing on hazard at turn start
            GridManager gridManager = FindFirstObjectByType<GridManager>();
            HazardManager hazardManager = FindFirstObjectByType<HazardManager>();

            if (gridManager != null && hazardManager != null)
            {
                GridCell currentCell = gridManager.GetCell(GridX, GridY);
                if (currentCell != null && currentCell.Hazard != null)
                {
                    Debug.Log($"[TURN START] {unitName} standing on {currentCell.Hazard.Type} hazard");
                    hazardManager.ApplyHazardDamageToUnit(this, currentCell);
                }
            }

            // Apply burn damage if burning
            if (IsBurning)
            {
                burnEffect.ApplyBurnDamage();

                // HP Display updates itself automatically

                // Clean up expired burn
                if (burnEffect.IsExpired)
                {
                    burnEffect = null;
                }
            }
        }

        /// <summary>
        /// Take damage from an attack. Returns actual damage dealt.
        /// </summary>
        public int TakeDamage(int incomingAttack)
        {
            // Calculate damage: (Attack - Defense), minimum 1
            int damage = Mathf.Max(1, incomingAttack - defensePower);

            currentHP -= damage;
            currentHP = Mathf.Max(0, currentHP); // Don't go below 0

            Debug.Log($"{unitName} took {damage} damage! HP: {currentHP}/{maxHP}");

            if (!IsAlive)
            {
                OnDeath();
            }

            return damage;
        }

        /// <summary>
        /// Heal this unit by a specified amount.
        /// </summary>
        public void Heal(int amount)
        {
            currentHP += amount;
            currentHP = Mathf.Min(currentHP, maxHP); // Don't exceed max

            Debug.Log($"{unitName} healed for {amount}! HP: {currentHP}/{maxHP}");
        }

        /// <summary>
        /// Called when unit's HP reaches 0.
        /// </summary>
        private void OnDeath()
        {
            Debug.Log($"{unitName} has been defeated!");
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
            
            // Enemy blocks path (different team)
            return occupant.IsPlayerControlled != this.IsPlayerControlled;
        }

        /// <summary>
        /// Apply a burn effect to this unit.
        /// </summary>
        public void ApplyBurn(int damagePerTurn, int duration)
        {
            if (burnEffect != null && !burnEffect.IsExpired)
            {
                // Refresh existing burn
                burnEffect.Refresh(duration);
            }
            else
            {
                // Create new burn effect
                burnEffect = new BurnEffect(this, damagePerTurn, duration);
            }
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
            if (item == null) return false;
            
            // Check if item can be equipped in this slot
            if (!item.CanEquipInSlot(targetSlot))
            {
                Debug.LogWarning($"{item.ItemName} cannot be equipped in {targetSlot} slot!");
                return false;
            }

            // Unequip whatever is in that slot currently
            if (equippedItems.ContainsKey(targetSlot))
            {
                UnequipItem(targetSlot);
            }

            // Equip the new item
            equippedItems[targetSlot] = item;

            Debug.Log($"{unitName} equipped {item.ItemName} in {targetSlot} slot.");
            return true;
        }
        
        /// <summary>
        /// Auto-equip an item to first available compatible slot.
        /// Useful for starting equipment.
        /// </summary>
        public bool EquipItem(EquipmentItem item)
        {
            if (item == null || item.CompatibleSlots == null || item.CompatibleSlots.Count == 0)
            {
                Debug.LogWarning($"Cannot equip {item?.ItemName} - no compatible slots defined!");
                return false;
            }
            
            // Try to equip in first compatible slot
            EquipmentSlot firstSlot = item.CompatibleSlots[0];
            return EquipItem(item, firstSlot);
        }

        /// <summary>
        /// Unequip item from a slot.
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.ContainsKey(slot)) return false;

            EquipmentItem item = equippedItems[slot];
            equippedItems.Remove(slot);

            Debug.Log($"{unitName} unequipped {item.ItemName} from {slot} slot.");
            return true;
        }

        /// <summary>
        /// Get the item equipped in a specific slot.
        /// </summary>
        public EquipmentItem GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
        }

        /// <summary>
        /// Can this unit use a specific skill?
        /// Checks mastered skills AND equipped items.
        /// </summary>
        public bool CanUseSkill(Skill skill)
        {
            if (skill == null) return false;

            // Check if skill is in known skills (backwards compatibility)
            if (knownSkills.Contains(skill)) return true;

            // Check if skill is mastered (SP-based system)
            if (masteredSkills.Contains(skill))
                return true;

            // Check if any equipped item grants this skill
            foreach (var equipped in equippedItems.Values)
            {
                if (equipped.GrantedSkills != null && equipped.GrantedSkills.Contains(skill))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get all skills this unit can currently use (mastered + equipped).
        /// </summary>
        public List<Skill> GetAvailableSkills()
        {
            List<Skill> available = new List<Skill>();

            // Add known skills (backwards compatibility)
            available.AddRange(knownSkills);

            // Add mastered skills
            foreach (Skill skill in masteredSkills)
            {
                if (!available.Contains(skill))
                    available.Add(skill);
            }

            // Add skills from equipped items
            foreach (var item in equippedItems.Values)
            {
                if (item.TeachesSkills)
                {
                    foreach (Skill skill in item.GrantedSkills)
                    {
                        if (!available.Contains(skill))
                            available.Add(skill);
                    }
                }
            }

            return available;
        }
        
        /// <summary>
        /// Calculate MaxHP including base value, equipment bonuses, and passive skill bonuses.
        /// Percentage bonuses scale with base maxHP.
        /// </summary>
        private int CalculateMaxHP()
        {
            int baseHP = maxHP;
            float percentBonus = 0f;
            
            // Sum percentage bonuses from all equipment
            foreach (var item in equippedItems.Values)
            {
                percentBonus += item.MaxHPBonusPercent;
            }
            
            // Sum percentage bonuses from mastered passive skills
            foreach (var passive in masteredPassiveSkills)
            {
                percentBonus += passive.MaxHPBonusPercent;
            }
            
            // Apply percentage bonus (e.g., 10% = 0.10)
            float totalHP = baseHP * (1f + (percentBonus / 100f));
            return Mathf.RoundToInt(totalHP);
        }
        
        /// <summary>
        /// Get total flat bonus for a specific stat from all equipped items.
        /// </summary>
        private int GetTotalEquipmentBonus(string statName)
        {
            int totalBonus = 0;
            
            foreach (var item in equippedItems.Values)
            {
                switch (statName)
                {
                    case "Attack": totalBonus += item.AttackBonus; break;
                    case "Defense": totalBonus += item.DefenseBonus; break;
                    case "Strength": totalBonus += item.StrengthBonus; break;
                    case "Finesse": totalBonus += item.FinesseBonus; break;
                    case "Focus": totalBonus += item.FocusBonus; break;
                    case "Speed": totalBonus += item.SpeedBonus; break;
                    case "Luck": totalBonus += item.LuckBonus; break;
                }
            }
            
            return totalBonus;
        }
        
        /// <summary>
        /// Get total flat bonus for a specific stat from all mastered passive skills.
        /// </summary>
        private int GetTotalPassiveSkillBonus(string statName)
        {
            int totalBonus = 0;
            
            foreach (var passive in masteredPassiveSkills)
            {
                switch (statName)
                {
                    case "Attack": totalBonus += passive.AttackBonus; break;
                    case "Defense": totalBonus += passive.DefenseBonus; break;
                    case "Strength": totalBonus += passive.StrengthBonus; break;
                    case "Finesse": totalBonus += passive.FinesseBonus; break;
                    case "Focus": totalBonus += passive.FocusBonus; break;
                    case "Speed": totalBonus += passive.SpeedBonus; break;
                    case "Luck": totalBonus += passive.LuckBonus; break;
                }
            }
            
            return totalBonus;
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
            Debug.Log($"{unitName} moved {points} cells - {MovementPointsRemaining}/{MaxMovementPoints} movement remaining");
            return true;
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
            return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, targetLevel - 2));
        }
        
        /// <summary>
        /// Get XP required to reach next level.
        /// </summary>
        public int GetXPRequiredForNextLevel()
        {
            return GetXPRequiredForLevel(level + 1);
        }
        
        /// <summary>
        /// Award XP to this unit and handle leveling up.
        /// </summary>
        public void AwardXP(int amount)
        {
            if (amount <= 0) return;
            
            currentXP += amount;
            Debug.Log($"{unitName} gained {amount} XP! ({currentXP}/{GetXPRequiredForNextLevel()})");
            
            // Check for level up
            while (currentXP >= GetXPRequiredForNextLevel())
            {
                LevelUp();
            }
        }
        
        /// <summary>
        /// Level up this unit - grants stat increases.
        /// SP is now awarded via actions, not leveling.
        /// </summary>
        private void LevelUp()
        {
            int xpNeeded = GetXPRequiredForNextLevel();
            currentXP -= xpNeeded;
            level++;
            
            // Random stat increases (PLACEHOLDER - should be player-chosen eventually)
            // TODO: Replace with player stat allocation system
            int statsToAllocate = 2;
            for (int i = 0; i < statsToAllocate; i++)
            {
                int randomStat = UnityEngine.Random.Range(0, 5);
                switch (randomStat)
                {
                    case 0: strength++; Debug.Log($"{unitName} Strength +1"); break;
                    case 1: finesse++; Debug.Log($"{unitName} Finesse +1"); break;
                    case 2: focus++; Debug.Log($"{unitName} Focus +1"); break;
                    case 3: speed++; Debug.Log($"{unitName} Speed +1"); break;
                    case 4: luck++; Debug.Log($"{unitName} Luck +1"); break;
                }
            }
            
            Debug.Log($"🎉 {unitName} reached Level {level}! +2 random stats");
        }
        
        /// <summary>
        /// Record a successful action and award SP based on action count.
        /// Call this for attacks, skill uses, etc.
        /// </summary>
        public void RecordAction()
        {
            totalActions++;
            
            // Check if we should award SP (every 5 actions)
            int spToAward = (totalActions - lastSPAwardedAtAction) / actionsPerSP;
            
            if (spToAward > 0)
            {
                skillPoints += spToAward;
                lastSPAwardedAtAction += spToAward * actionsPerSP;
                Debug.Log($"⭐ {unitName} earned {spToAward} SP! ({totalActions} total actions, {skillPoints} SP total)");
            }
        }
        
        /// <summary>
        /// Spend SP to master a combat skill permanently.
        /// Returns true if successful, false if not enough SP or already mastered.
        /// </summary>
        public bool MasterSkill(Skill skill)
        {
            if (skill == null) return false;
            
            if (masteredSkills.Contains(skill))
            {
                Debug.Log($"{unitName} already mastered {skill.SkillName}!");
                return false;
            }
            
            if (skillPoints < skill.MasteryCost)
            {
                Debug.Log($"{unitName} needs {skill.MasteryCost} SP to master {skill.SkillName}, but only has {skillPoints} SP");
                return false;
            }
            
            skillPoints -= skill.MasteryCost;
            masteredSkills.Add(skill);
            Debug.Log($"✨ {unitName} mastered {skill.SkillName} for {skill.MasteryCost} SP! ({skillPoints} SP remaining)");
            return true;
        }
        
        /// <summary>
        /// Check if a skill is mastered.
        /// </summary>
        public bool IsSkillMastered(Skill skill)
        {
            return masteredSkills.Contains(skill);
        }
        
        /// <summary>
        /// Spend SP to master a passive skill permanently.
        /// Returns true if successful, false if not enough SP or already mastered.
        /// </summary>
        public bool MasterPassiveSkill(PassiveSkill passiveSkill)
        {
            if (passiveSkill == null) return false;
            
            if (masteredPassiveSkills.Contains(passiveSkill))
            {
                Debug.Log($"{unitName} already mastered {passiveSkill.SkillName}!");
                return false;
            }
            
            if (skillPoints < passiveSkill.SPCost)
            {
                Debug.Log($"{unitName} needs {passiveSkill.SPCost} SP to master {passiveSkill.SkillName}, but only has {skillPoints} SP");
                return false;
            }
            
            skillPoints -= passiveSkill.SPCost;
            masteredPassiveSkills.Add(passiveSkill);
            Debug.Log($"✨ {unitName} mastered passive skill {passiveSkill.SkillName} for {passiveSkill.SPCost} SP! ({skillPoints} SP remaining)");
            
            // Recalculate max HP in case it changed
            int newMaxHP = CalculateMaxHP();
            if (newMaxHP > currentHP)
            {
                currentHP = newMaxHP; // Heal to new max if HP increased
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if a passive skill is mastered.
        /// </summary>
        public bool IsPassiveSkillMastered(PassiveSkill passiveSkill)
        {
            return masteredPassiveSkills.Contains(passiveSkill);
        }
        
        /// <summary>
        /// Get all passive skills currently available from equipment.
        /// </summary>
        public List<PassiveSkill> GetAvailablePassiveSkills()
        {
            List<PassiveSkill> available = new List<PassiveSkill>();
            
            // Add passive skills from equipped items
            foreach (var item in equippedItems.Values)
            {
                if (item.GrantedPassiveSkills != null)
                {
                    foreach (PassiveSkill passiveSkill in item.GrantedPassiveSkills)
                    {
                        if (!available.Contains(passiveSkill))
                            available.Add(passiveSkill);
                    }
                }
            }
            
            return available;
        }
        
        #endregion
    }
}