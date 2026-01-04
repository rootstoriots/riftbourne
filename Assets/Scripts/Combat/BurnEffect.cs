using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Represents a Burn status effect on a unit.
    /// Deals damage over time for a fixed duration.
    /// </summary>
    public class BurnEffect
    {
        private Unit target;
        private int damagePerTurn;
        private int remainingDuration;

        public Unit Target => target;
        public int DamagePerTurn => damagePerTurn;
        public int RemainingDuration => remainingDuration;
        public bool IsExpired => remainingDuration <= 0;

        public BurnEffect(Unit target, int damagePerTurn, int duration)
        {
            this.target = target;
            this.damagePerTurn = damagePerTurn;
            this.remainingDuration = duration;

            Debug.Log($"{target.name} is now BURNING! {damagePerTurn} damage per turn for {duration} turns.");
        }

        /// <summary>
        /// Apply burn damage and reduce duration.
        /// Called at the start of the burning unit's turn.
        /// </summary>
        public void ApplyBurnDamage()
        {
            if (IsExpired || !target.IsAlive)
            {
                return;
            }

            int actualDamage = target.TakeDamage(damagePerTurn);
            remainingDuration--;

            Debug.Log($"{target.name} takes {actualDamage} BURN damage! ({remainingDuration} turns remaining)");

            if (IsExpired)
            {
                Debug.Log($"{target.name} is no longer burning.");
            }
        }

        /// <summary>
        /// Refresh the burn effect duration (does not stack damage).
        /// </summary>
        public void Refresh(int newDuration)
        {
            remainingDuration = Mathf.Max(remainingDuration, newDuration);
            Debug.Log($"{target.name}'s Burn refreshed to {remainingDuration} turns.");
        }
    }
}