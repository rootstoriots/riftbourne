using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Runtime instance of a status effect applied to a unit.
    /// Created from StatusEffectData ScriptableObject.
    /// </summary>
    public class StatusEffect : IStatusEffect
    {
        protected Unit target;
        protected int remainingDuration;
        protected StatusEffectData data;

        public Unit Target => target;
        public int RemainingDuration => remainingDuration;
        public bool IsExpired => remainingDuration <= 0;
        public StatusEffectData Data => data;

        public StatusEffect(Unit target, StatusEffectData data, int duration)
        {
            this.target = target;
            this.data = data;
            this.remainingDuration = duration;
        }

        /// <summary>
        /// Called at the start of the affected unit's turn.
        /// Applies effects based on StatusEffectData configuration.
        /// </summary>
        public virtual void OnTurnStart()
        {
            if (data == null || IsExpired || !target.IsAlive)
            {
                return;
            }

            // Apply damage if configured
            if (data.DealsDamage && data.DamagePerTurn > 0)
            {
                // Pass the effect name as damage source so it appears in the TakeDamage log
                int actualDamage = target.TakeDamage(data.DamagePerTurn, data.EffectName);
                Debug.Log($"{target.UnitName} takes {actualDamage} {data.EffectName} damage! ({remainingDuration} turns remaining)");
            }

            // Apply healing if configured
            if (data.HealsOverTime && data.HealingPerTurn > 0)
            {
                int hpBefore = target.CurrentHP;
                target.Heal(data.HealingPerTurn);
                int actualHealing = target.CurrentHP - hpBefore;
                Debug.Log($"{target.UnitName} heals {actualHealing} from {data.EffectName}! ({remainingDuration} turns remaining)");
            }

            // Decrement duration
            remainingDuration--;

            if (IsExpired)
            {
                Debug.Log($"{target.UnitName} is no longer affected by {data.EffectName}.");
            }
        }

        /// <summary>
        /// Called when the effect is first applied.
        /// </summary>
        public virtual void OnApplied()
        {
            if (data != null)
            {
                Debug.Log($"{target.UnitName} is now affected by {data.EffectName}! ({remainingDuration} turns)");
            }
        }

        /// <summary>
        /// Called when the effect expires or is removed.
        /// Override this to implement cleanup effects.
        /// </summary>
        public virtual void OnRemoved()
        {
            // Override in derived classes for cleanup
        }

        /// <summary>
        /// Refresh the effect duration (when reapplied).
        /// </summary>
        public virtual void Refresh(int newDuration)
        {
            remainingDuration = Mathf.Max(remainingDuration, newDuration);
        }

        /// <summary>
        /// Get a description of this status effect for UI display.
        /// </summary>
        public virtual string GetDescription()
        {
            if (data != null)
            {
                string desc = $"{data.EffectName} ({remainingDuration} turns)";
                if (data.DealsDamage)
                {
                    desc += $"\n{data.DamagePerTurn} damage/turn";
                }
                if (data.HealsOverTime)
                {
                    desc += $"\n{data.HealingPerTurn} healing/turn";
                }
                return desc;
            }
            return $"Status Effect ({remainingDuration} turns)";
        }

        /// <summary>
        /// Check if this effect prevents the unit from taking actions.
        /// </summary>
        public bool PreventsActions()
        {
            return data != null && data.PreventsActions;
        }

        /// <summary>
        /// Check if this effect prevents the unit from moving.
        /// </summary>
        public bool PreventsMovement()
        {
            return data != null && data.PreventsMovement;
        }

        /// <summary>
        /// Get the movement speed multiplier for this effect.
        /// </summary>
        public float GetMovementMultiplier()
        {
            return data != null && data.ModifiesMovement ? data.MovementMultiplier : 1.0f;
        }
    }

    /// <summary>
    /// Interface for all status effects to ensure consistent behavior.
    /// </summary>
    public interface IStatusEffect
    {
        bool IsExpired { get; }
    }
}

