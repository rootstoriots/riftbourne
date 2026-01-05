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
        [SerializeField] private int attackPower = 15;
        [SerializeField] private int defensePower = 5;

        [Header("UI")]
        [SerializeField] private HPDisplay hpDisplay;

        [Header("Magical Capacity")]
        [SerializeField] private MantleType mantle = MantleType.None;

        // Public property for Mantle
        public MantleType Mantle => mantle;

        [Header("Skills")]
        [SerializeField] private List<Skill> knownSkills = new List<Skill>();

        [Header("Equipment & Learning")]
        [SerializeField] private List<EquipmentItem> startingEquipment = new List<EquipmentItem>();

        [Header("Unit Identity")]
        [SerializeField] private string unitName = "Unit";
        [SerializeField] private bool isPlayerControlled = true;

        // Burn status effect tracking
        private BurnEffect burnEffect = null;

        // Equipment system
        private Dictionary<EquipmentSlot, EquipmentItem> equippedItems = new Dictionary<EquipmentSlot, EquipmentItem>();

        // Mastery tracking
        private Dictionary<Skill, SkillMastery> skillMasteryProgress = new Dictionary<Skill, SkillMastery>();

        // Public properties
        public List<Skill> KnownSkills => knownSkills;
        public bool IsBurning => burnEffect != null && !burnEffect.IsExpired;
        public BurnEffect BurnEffect => burnEffect;
        public int MaxHP => maxHP;
        public int CurrentHP => currentHP;
        public int AttackPower => attackPower;
        public int DefensePower => defensePower;
        public string UnitName => unitName;
        public bool IsPlayerControlled => isPlayerControlled;
        public bool IsAlive => currentHP > 0;

        // Action tracking for turn-based gameplay
        public bool HasMovedThisTurn { get; private set; }
        public bool HasActedThisTurn { get; private set; }

        // Hazard damage tracking - only take damage once per hazard type per turn
        private HashSet<HazardTile.HazardType> hazardsDamagedByThisTurn = new HashSet<HazardTile.HazardType>();

        private void Awake()
        {
            currentHP = maxHP;
            gridManager = FindFirstObjectByType<GridManager>();
        }


        private void Start()
        {
            // Initialize grid position based on world position
            int startX = Mathf.FloorToInt(transform.position.x);
            int startY = Mathf.FloorToInt(transform.position.z);

            GridManager gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null && gridManager.IsValidGridPosition(startX, startY))
            {
                SetGridPosition(startX, startY, new Vector3(startX, 0, startY));
                Debug.Log($"{unitName} initialized at grid position ({startX}, {startY})");
            }
            else
            {
                Debug.LogWarning($"{unitName} spawned at invalid grid position!");
            }

            // HP Display updates itself automatically in its Update() method

            // Equip starting equipment
            foreach (var item in startingEquipment)
            {
                EquipItem(item);
            }
        }

        /// <summary>
        /// Called at the start of this unit's turn to apply burn damage.
        /// </summary>
        public void OnTurnStart()
        {
            // Reset action flags at start of turn
            HasMovedThisTurn = false;
            HasActedThisTurn = false;
            hazardsDamagedByThisTurn.Clear(); // Reset hazard damage tracking
            Debug.Log($"{unitName} turn started - actions reset");

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
        /// Inherits movement range check from Character class.
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

            // Check if target cell is occupied by another unit
            if (gridManager != null)
            {
                GridCell targetCell = gridManager.GetCell(targetX, targetY);
                if (targetCell != null && targetCell.OccupyingUnit != null && targetCell.OccupyingUnit != this)
                {
                    return false; // Cell occupied by another unit
                }
            }


            return true;
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
        /// Equip an item to the appropriate slot.
        /// </summary>
        public bool EquipItem(EquipmentItem item)
        {
            if (item == null) return false;

            // Unequip whatever is in that slot currently
            if (equippedItems.ContainsKey(item.SlotType))
            {
                UnequipItem(item.SlotType);
            }

            // Equip the new item
            equippedItems[item.SlotType] = item;

            // If it teaches skills, start tracking mastery for each
            if (item.TeachesSkills)
            {
                foreach (Skill skill in item.GrantedSkills)
                {
                    if (!skillMasteryProgress.ContainsKey(skill))
                    {
                        skillMasteryProgress[skill] = new SkillMastery(skill, item.MasteryThreshold);
                    }
                }
            }

            Debug.Log($"{unitName} equipped {item.ItemName} in {item.SlotType} slot.");
            return true;
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
        /// Checks both mastered skills AND equipped items.
        /// </summary>
        public bool CanUseSkill(Skill skill)
        {
            if (skill == null) return false;

            // Check if skill is in known skills (backwards compatibility)
            if (knownSkills.Contains(skill)) return true;

            // Check if skill is mastered
            if (skillMasteryProgress.ContainsKey(skill) && skillMasteryProgress[skill].isMastered)
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
        /// Record that a skill was used and update mastery progress.
        /// </summary>
        public void RecordSkillUsage(Skill skill)
        {
            if (skill == null) return;

            // If we're tracking mastery for this skill, increment usage
            if (skillMasteryProgress.ContainsKey(skill))
            {
                bool justMastered = skillMasteryProgress[skill].IncrementUsage();

                if (justMastered)
                {
                    Debug.Log($"🎉 {unitName} has MASTERED {skill.SkillName}!");
                }
            }
        }

        /// <summary>
        /// Get mastery progress for a skill (returns null if not being learned).
        /// </summary>
        public SkillMastery GetMasteryProgress(Skill skill)
        {
            return skillMasteryProgress.ContainsKey(skill) ? skillMasteryProgress[skill] : null;
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
            foreach (var mastery in skillMasteryProgress.Values)
            {
                if (mastery.isMastered && !available.Contains(mastery.skill))
                    available.Add(mastery.skill);
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

        #endregion

        /// <summary>
        /// Mark that this unit has moved this turn.
        /// </summary>
        public void MarkAsMoved()
        {
            HasMovedThisTurn = true;
            Debug.Log($"{unitName} has now moved this turn");
        }

        /// <summary>
        /// Mark that this unit has taken an action (attack, skill, etc).
        /// </summary>
        public void MarkAsActed()
        {
            HasActedThisTurn = true;
            Debug.Log($"{unitName} has now acted this turn");
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
    }
}