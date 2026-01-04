using UnityEngine;
using Riftbourne.Grid;

namespace Riftbourne.Characters
{
    public class Unit : Character
    {
        [Header("Combat Stats")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int currentHP;
        [SerializeField] private int attackPower = 15;
        [SerializeField] private int defensePower = 5;

        [Header("Unit Identity")]
        [SerializeField] private string unitName = "Unit";
        [SerializeField] private bool isPlayerControlled = true;

        // Public properties
        public int MaxHP => maxHP;
        public int CurrentHP => currentHP;
        public int AttackPower => attackPower;
        public int DefensePower => defensePower;
        public string UnitName => unitName;
        public bool IsPlayerControlled => isPlayerControlled;
        public bool IsAlive => currentHP > 0;

        private void Awake()
        {
            currentHP = maxHP;
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
            GridManager gridManager = FindFirstObjectByType<GridManager>();
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
    }
}