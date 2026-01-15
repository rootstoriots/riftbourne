using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Items;
using Riftbourne.Grid;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Static utility class for executing consumable items in combat.
    /// Handles validation, effect application, and item consumption.
    /// </summary>
    public static class ConsumableExecutor
    {
        /// <summary>
        /// Execute a consumable item on a target unit.
        /// Returns true if successful.
        /// </summary>
        public static bool ExecuteConsumable(ConsumableItem consumable, Unit user, Unit target)
        {
            if (consumable == null || user == null || target == null)
                return false;
            
            // Validate context
            bool inCombat = TurnManager.Instance != null && TurnManager.Instance.IsInCombat;
            if (!consumable.CanUseInCurrentContext(inCombat))
            {
                Debug.LogWarning($"Cannot use {consumable.ItemName} in current context!");
                return false;
            }
            
            // Validate user has item
            if (!user.HasItem(consumable, 1))
            {
                Debug.LogWarning($"{user.UnitName} doesn't have {consumable.ItemName}!");
                return false;
            }
            
            // Validate range (if in combat)
            if (inCombat)
            {
                int distance = Mathf.Abs(user.GridX - target.GridX) + Mathf.Abs(user.GridY - target.GridY);
                if (distance > consumable.Range)
                {
                    Debug.LogWarning($"Target out of range! ({distance} > {consumable.Range})");
                    return false;
                }
            }
            
            // Apply effects
            foreach (var effect in consumable.Effects)
            {
                ApplyEffect(effect, target);
            }
            
            // Consume item
            user.RemoveItem(consumable, 1);
            
            // Record action for XP/SP (if in combat)
            if (inCombat)
            {
                user.MarkAsActed();
                user.RecordAction(); // For SP progression
            }
            
            Debug.Log($"{user.UnitName} used {consumable.ItemName} on {target.UnitName}");
            return true;
        }
        
        /// <summary>
        /// Execute a consumable item on a ground position (for AOE effects).
        /// Returns true if successful.
        /// </summary>
        public static bool ExecuteConsumableGround(ConsumableItem consumable, Unit user, int targetX, int targetY)
        {
            if (consumable == null || user == null)
                return false;
            
            // Validate context
            bool inCombat = TurnManager.Instance != null && TurnManager.Instance.IsInCombat;
            if (!consumable.CanUseInCurrentContext(inCombat))
            {
                Debug.LogWarning($"Cannot use {consumable.ItemName} in current context!");
                return false;
            }
            
            // Validate user has item
            if (!user.HasItem(consumable, 1))
            {
                Debug.LogWarning($"{user.UnitName} doesn't have {consumable.ItemName}!");
                return false;
            }
            
            // Validate range (if in combat)
            if (inCombat)
            {
                int distance = Mathf.Abs(user.GridX - targetX) + Mathf.Abs(user.GridY - targetY);
                if (distance > consumable.Range)
                {
                    Debug.LogWarning($"Target out of range! ({distance} > {consumable.Range})");
                    return false;
                }
            }
            
            // Apply ground effects
            foreach (var effect in consumable.Effects)
            {
                if (effect.effectType == ConsumableEffectType.Damage && effect.aoeRadius > 0)
                {
                    ApplyAOEDamage(targetX, targetY, effect.aoeRadius, effect.magnitude);
                }
            }
            
            // Consume item
            user.RemoveItem(consumable, 1);
            
            // Record action for XP/SP (if in combat)
            if (inCombat)
            {
                user.MarkAsActed();
                user.RecordAction();
            }
            
            Debug.Log($"{user.UnitName} used {consumable.ItemName} at ({targetX}, {targetY})");
            return true;
        }
        
        /// <summary>
        /// Apply a single consumable effect to a target unit.
        /// </summary>
        private static void ApplyEffect(ConsumableEffect effect, Unit target)
        {
            switch (effect.effectType)
            {
                case ConsumableEffectType.Heal:
                    target.Heal(effect.magnitude);
                    break;
                    
                case ConsumableEffectType.Damage:
                    target.TakeDamage(effect.magnitude);
                    break;
                    
                case ConsumableEffectType.BuffStat:
                    // TODO: Implement buff system (future)
                    Debug.Log($"Applied {effect.affectedStat} buff (+{effect.magnitude}) to {target.UnitName} for {effect.duration} turns");
                    break;
                    
                case ConsumableEffectType.DebuffStat:
                    // TODO: Implement debuff system (future)
                    Debug.Log($"Applied {effect.affectedStat} debuff (-{effect.magnitude}) to {target.UnitName} for {effect.duration} turns");
                    break;
                    
                default:
                    Debug.LogWarning($"Unimplemented effect type: {effect.effectType}");
                    break;
            }
        }
        
        /// <summary>
        /// Apply AOE damage to all units within radius.
        /// </summary>
        private static void ApplyAOEDamage(int centerX, int centerY, int radius, int damage)
        {
            var gridManager = GridManager.Instance;
            if (gridManager == null)
                return;
            
            // Find all units within radius
            var allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            
            foreach (var unit in allUnits)
            {
                int distance = Mathf.Abs(unit.GridX - centerX) + Mathf.Abs(unit.GridY - centerY);
                
                if (distance <= radius)
                {
                    unit.TakeDamage(damage);
                    Debug.Log($"{unit.UnitName} took {damage} AOE damage");
                }
            }
        }
    }
}
