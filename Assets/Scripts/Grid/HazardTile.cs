using UnityEngine;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Represents a hazardous tile on the grid (fire, ice, poison, etc.)
    /// Tracks type, damage, duration, and visual representation
    /// </summary>
    public class HazardTile
    {
        public enum HazardType
        {
            Fire,
            // Future: Ice, Poison, etc.
        }

        // Properties
        public HazardType Type { get; private set; }
        public int DamagePerTurn { get; private set; }
        public int RemainingTurns { get; private set; }
        public GameObject VisualObject { get; set; } // Fire quad visual

        // Constructor
        // Track creation to prevent immediate decrement
        private bool justCreated = true;

        public HazardTile(HazardType type, int damagePerTurn, int duration)
        {
            Type = type;
            DamagePerTurn = damagePerTurn;
            RemainingTurns = duration;
        }

        /// <summary>
        /// Decrements remaining turns. Returns true if hazard should be removed.
        /// </summary>
        public bool DecrementDuration()
        {
            // Skip first decrement (hazard just created this round)
            if (justCreated)
            {
                justCreated = false;
                return false; // Don't remove
            }

            RemainingTurns--;
            return RemainingTurns <= 0;
        }

        /// <summary>
        /// Refreshes duration (when reapplying fire to same cell)
        /// </summary>
        public void RefreshDuration(int duration)
        {
            RemainingTurns = Mathf.Max(RemainingTurns, duration);
        }
    }
}