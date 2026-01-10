using System;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Skills;

namespace Riftbourne.Core
{
    /// <summary>
    /// Centralized event definitions for game-wide events.
    /// Uses C# events for code-to-code communication to reduce coupling.
    /// </summary>
    public static class GameEvents
    {
        // Unit Events
        /// <summary>
        /// Event raised when a unit's HP changes (damage or healing).
        /// Parameters: (unit, currentHP, maxHP)
        /// </summary>
        public static event Action<Unit, int, int> OnUnitHPChanged;

        /// <summary>
        /// Event raised when a unit dies.
        /// Parameters: (unit)
        /// </summary>
        public static event Action<Unit> OnUnitDied;

        /// <summary>
        /// Event raised when a unit levels up.
        /// Parameters: (unit, newLevel)
        /// </summary>
        public static event Action<Unit, int> OnUnitLeveledUp;

        /// <summary>
        /// Event raised when a unit masters a skill.
        /// Parameters: (unit, skill)
        /// </summary>
        public static event Action<Unit, Skill> OnSkillMastered;

        /// <summary>
        /// Event raised when a unit takes damage.
        /// Parameters: (unit, damageAmount)
        /// </summary>
        public static event Action<Unit, int> OnUnitDamaged;

        /// <summary>
        /// Event raised when a unit receives healing.
        /// Parameters: (unit, healAmount)
        /// </summary>
        public static event Action<Unit, int> OnUnitHealed;

        /// <summary>
        /// Event raised when an attack misses a unit.
        /// Parameters: (targetUnit)
        /// </summary>
        public static event Action<Unit> OnAttackMissed;

        // Combat Events
        /// <summary>
        /// Event raised when the turn window changes (units that can act).
        /// Parameters: (units in window)
        /// </summary>
        public static event Action<List<Unit>> OnTurnWindowChanged;

        /// <summary>
        /// Event raised when the current unit changes.
        /// Parameters: (currentUnit)
        /// </summary>
        public static event Action<Unit> OnCurrentUnitChanged;

        /// <summary>
        /// Event raised when a unit ends their turn.
        /// Parameters: (unit)
        /// </summary>
        public static event Action<Unit> OnUnitTurnEnded;

        /// <summary>
        /// Event raised when combat ends.
        /// Parameters: (playerVictory)
        /// </summary>
        public static event Action<bool> OnCombatEnded;

        // Event Invocation Methods (for raising events)
        public static void RaiseUnitHPChanged(Unit unit, int currentHP, int maxHP)
        {
            OnUnitHPChanged?.Invoke(unit, currentHP, maxHP);
        }

        public static void RaiseUnitDied(Unit unit)
        {
            OnUnitDied?.Invoke(unit);
        }

        public static void RaiseUnitLeveledUp(Unit unit, int newLevel)
        {
            OnUnitLeveledUp?.Invoke(unit, newLevel);
        }

        public static void RaiseSkillMastered(Unit unit, Skill skill)
        {
            OnSkillMastered?.Invoke(unit, skill);
        }

        public static void RaiseTurnWindowChanged(List<Unit> units)
        {
            OnTurnWindowChanged?.Invoke(units);
        }

        public static void RaiseCurrentUnitChanged(Unit unit)
        {
            OnCurrentUnitChanged?.Invoke(unit);
        }

        public static void RaiseUnitTurnEnded(Unit unit)
        {
            OnUnitTurnEnded?.Invoke(unit);
        }

        public static void RaiseCombatEnded(bool playerVictory)
        {
            OnCombatEnded?.Invoke(playerVictory);
        }

        public static void RaiseUnitDamaged(Unit unit, int damageAmount)
        {
            // Ensure DamageIndicatorManager is initialized
            if (OnUnitDamaged == null || OnUnitDamaged.GetInvocationList().Length == 0)
            {
                _ = Riftbourne.UI.DamageIndicatorManager.Instance;
            }
            OnUnitDamaged?.Invoke(unit, damageAmount);
        }

        public static void RaiseUnitHealed(Unit unit, int healAmount)
        {
            OnUnitHealed?.Invoke(unit, healAmount);
        }

        public static void RaiseAttackMissed(Unit targetUnit)
        {
            OnAttackMissed?.Invoke(targetUnit);
        }
    }
}

