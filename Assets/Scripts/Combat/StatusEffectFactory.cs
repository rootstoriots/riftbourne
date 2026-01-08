using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Factory for creating status effects from StatusEffectData ScriptableObjects.
    /// To add a new status effect: just create a StatusEffectData ScriptableObject!
    /// </summary>
    public static class StatusEffectFactory
    {
        /// <summary>
        /// Create a status effect from StatusEffectData.
        /// </summary>
        public static StatusEffect Create(StatusEffectData data, Unit target, int duration)
        {
            if (data == null)
            {
                Debug.LogWarning("StatusEffectFactory.Create: StatusEffectData is null!");
                return null;
            }

            // Create the effect using the data
            StatusEffect effect = new StatusEffect(target, data, duration);
            effect.OnApplied();
            return effect;
        }
    }
}

