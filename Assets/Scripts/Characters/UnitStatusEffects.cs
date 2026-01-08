using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Riftbourne.Combat;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles status effects for a unit (burn, poison, etc.)
    /// Component class to reduce Unit.cs complexity.
    /// Uses StatusEffectData ScriptableObjects directly - no enum needed!
    /// </summary>
    public class UnitStatusEffects
    {
        private Unit unit;
        private Dictionary<StatusEffectData, StatusEffect> activeEffects = new Dictionary<StatusEffectData, StatusEffect>();

        // Legacy properties for backward compatibility (check by name)
        public bool IsBurning => HasStatusEffectByName("Burn");
        public StatusEffect BurnEffect => GetStatusEffectByName("Burn");

        public UnitStatusEffects(Unit unit)
        {
            this.unit = unit;
        }

        /// <summary>
        /// Check if unit has a specific status effect active (by StatusEffectData).
        /// </summary>
        public bool HasStatusEffect(StatusEffectData effectData)
        {
            if (effectData == null) return false;
            if (activeEffects.TryGetValue(effectData, out StatusEffect effect))
            {
                return !effect.IsExpired;
            }
            return false;
        }

        /// <summary>
        /// Check if unit has a status effect by name (for legacy support).
        /// </summary>
        public bool HasStatusEffectByName(string effectName)
        {
            return activeEffects.Values.Any(e => e.Data != null && e.Data.EffectName == effectName && !e.IsExpired);
        }

        /// <summary>
        /// Get a status effect by StatusEffectData (if active).
        /// </summary>
        public StatusEffect GetStatusEffect(StatusEffectData effectData)
        {
            if (effectData == null) return null;
            if (activeEffects.TryGetValue(effectData, out StatusEffect effect))
            {
                return effect;
            }
            return null;
        }

        /// <summary>
        /// Get a status effect by name (for legacy support).
        /// </summary>
        public StatusEffect GetStatusEffectByName(string effectName)
        {
            return activeEffects.Values.FirstOrDefault(e => e.Data != null && e.Data.EffectName == effectName && !e.IsExpired);
        }

        /// <summary>
        /// Apply a status effect to this unit using StatusEffectData.
        /// </summary>
        public void ApplyStatusEffect(StatusEffectData effectData, int duration)
        {
            if (effectData == null)
            {
                Debug.LogWarning("UnitStatusEffects.ApplyStatusEffect: StatusEffectData is null!");
                return;
            }

            // If same effect data already exists, refresh it
            if (activeEffects.TryGetValue(effectData, out StatusEffect existingEffect))
            {
                if (!existingEffect.IsExpired)
                {
                    existingEffect.Refresh(duration);
                    Debug.Log($"{unit.UnitName}'s {effectData.EffectName} effect refreshed to {duration} turns.");
                    return;
                }
            }

            // Create new effect using factory
            StatusEffect newEffect = StatusEffectFactory.Create(effectData, unit, duration);
            if (newEffect != null)
            {
                activeEffects[effectData] = newEffect;
            }
        }

        /// <summary>
        /// Apply a burn effect to this unit.
        /// Legacy method for backward compatibility - tries to find a "Burn" StatusEffectData.
        /// </summary>
        public void ApplyBurn(int damagePerTurn, int duration)
        {
            // Try to find a "Burn" status effect data (this is a workaround for legacy code)
            // In practice, you should use ApplyStatusEffect(StatusEffectData, duration) directly
            Debug.LogWarning("ApplyBurn is deprecated. Use ApplyStatusEffect(StatusEffectData, duration) instead.");
            // For now, we can't apply burn without the StatusEffectData reference
        }

        /// <summary>
        /// Remove a specific status effect from this unit.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectData effectData)
        {
            if (effectData == null) return;
            if (activeEffects.TryGetValue(effectData, out StatusEffect effect))
            {
                effect.OnRemoved();
                activeEffects.Remove(effectData);
            }
        }

        /// <summary>
        /// Apply all status effect turn-based effects at turn start.
        /// Automatically handles all registered status effects.
        /// </summary>
        public void ApplyStatusEffectDamage()
        {
            // Create a list of effects to remove (can't modify dictionary while iterating)
            List<StatusEffectData> effectsToRemove = new List<StatusEffectData>();

            // Apply turn-based effects for all active status effects
            foreach (var kvp in activeEffects)
            {
                StatusEffect effect = kvp.Value;
                
                if (effect.IsExpired || !unit.IsAlive)
                {
                    effectsToRemove.Add(kvp.Key);
                    continue;
                }

                // Call OnTurnStart which handles the effect's turn-based logic
                effect.OnTurnStart();

                // Mark for removal if expired after turn processing
                if (effect.IsExpired)
                {
                    effectsToRemove.Add(kvp.Key);
                }
            }

            // Remove expired effects
            foreach (var effectData in effectsToRemove)
            {
                if (activeEffects.TryGetValue(effectData, out StatusEffect effect))
                {
                    effect.OnRemoved();
                }
                activeEffects.Remove(effectData);
            }
        }

        /// <summary>
        /// Get all active status effects for UI display.
        /// </summary>
        public Dictionary<StatusEffectData, StatusEffect> GetAllActiveEffects()
        {
            return new Dictionary<StatusEffectData, StatusEffect>(activeEffects);
        }
    }
}

