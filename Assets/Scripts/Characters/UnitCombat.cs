using UnityEngine;
using Riftbourne.Core;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles HP, damage, and defense calculations for a unit.
    /// Component class to reduce Unit.cs complexity.
    /// </summary>
    public class UnitCombat
    {
        private Unit unit;
        private int maxHP;
        private int currentHP;
        private int attackPower; // Legacy
        private int defensePower; // Legacy

        public int MaxHP { get; private set; }
        public int CurrentHP => currentHP;
        public bool IsAlive => currentHP > 0;

        public UnitCombat(Unit unit, int maxHP, int attackPower, int defensePower)
        {
            this.unit = unit;
            this.maxHP = maxHP;
            this.currentHP = maxHP;
            this.attackPower = attackPower;
            this.defensePower = defensePower;
            RecalculateMaxHP();
        }

        public void RecalculateMaxHP(int baseMaxHP, int flatBonus, float percentBonus)
        {
            this.maxHP = baseMaxHP;
            float totalHP = (baseMaxHP + flatBonus) * (1f + (percentBonus / 100f));
            MaxHP = Mathf.RoundToInt(totalHP);
            
            // Adjust current HP if max changed
            if (MaxHP < currentHP)
            {
                currentHP = MaxHP;
            }
        }

        private void RecalculateMaxHP()
        {
            MaxHP = maxHP; // Will be updated by Unit
        }

        public int TakeDamage(int incomingAttack, int defensePower)
        {
            int minDamage = GameConstants.Instance != null ? GameConstants.Instance.MinimumDamage : 1;
            int damage = Mathf.Max(minDamage, incomingAttack - defensePower);

            currentHP -= damage;
            currentHP = Mathf.Max(0, currentHP);

            return damage;
        }

        /// <summary>
        /// Heal this unit by a specified amount.
        /// Returns the actual amount healed (may be less than requested if at max HP).
        /// </summary>
        public int Heal(int amount, int maxHP)
        {
            int hpBefore = currentHP;
            currentHP += amount;
            currentHP = Mathf.Min(currentHP, maxHP);
            return currentHP - hpBefore; // Return actual amount healed
        }

        public void SetHP(int value, int maxHP)
        {
            currentHP = Mathf.Clamp(value, 0, maxHP);
        }

        public int GetAttackPower(int equipmentBonus)
        {
            return attackPower + equipmentBonus;
        }

        public int GetDefensePower(int equipmentBonus)
        {
            return defensePower + equipmentBonus;
        }
    }
}

