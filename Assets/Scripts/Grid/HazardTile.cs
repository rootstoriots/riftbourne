using UnityEngine;

namespace Riftbourne.Grid
{
    /// <summary>
    /// Represents a hazardous tile on the grid (fire, ice, poison, rivers, etc.)
    /// Tracks hazard data, damage, duration, and visual representation
    /// </summary>
    public class HazardTile
    {
        // Legacy enum for backward compatibility (deprecated - use HazardData instead)
        public enum HazardType
        {
            Fire,      // Burns units, deals damage over time
            Ice,       // Slows movement, may freeze
            Poison,    // Deals damage over time, may stack
            Acid,      // Deals damage and reduces defense
            Lightning, // Deals instant damage when stepped on
            // Add more types as needed
        }

        // Properties
        public HazardData Data { get; private set; }
        public int DirectDamagePerTurn { get; private set; }
        public int RemainingTurns { get; private set; } // -1 for permanent/natural hazards
        public GameObject VisualObject { get; set; }
        public bool IsPermanent => RemainingTurns < 0;

        // Legacy property for backward compatibility
        public HazardType Type => Data != null ? InferTypeFromData() : HazardType.Fire;

        // Constructor
        private bool justCreated = true;

        public HazardTile(HazardData data, int directDamagePerTurn, int duration)
        {
            Data = data;
            DirectDamagePerTurn = directDamagePerTurn;
            // Natural hazards are permanent (-1), others use specified duration
            RemainingTurns = (data != null && data.IsNatural) ? -1 : duration;
        }

        /// <summary>
        /// Legacy constructor for backward compatibility.
        /// </summary>
        public HazardTile(HazardType type, int damagePerTurn, int duration)
        {
            // Create a temporary data-less hazard (will need to be updated)
            Data = null;
            DirectDamagePerTurn = damagePerTurn;
            RemainingTurns = duration;
        }

        /// <summary>
        /// Infer hazard type from status effect data (for backward compatibility).
        /// </summary>
        private HazardType InferTypeFromData()
        {
            if (Data == null || Data.StatusEffectData == null)
            {
                return HazardType.Fire; // Default fallback
            }

            // Map status effect name to hazard type (legacy support)
            string effectName = Data.StatusEffectData.EffectName;
            if (effectName == "Burn")
                return HazardType.Fire;
            else if (effectName == "Poison")
                return HazardType.Poison;
            // Add more mappings as needed
            else
                return HazardType.Fire;
        }

        /// <summary>
        /// Decrements remaining turns. Returns true if hazard should be removed.
        /// Permanent hazards are never removed.
        /// </summary>
        public bool DecrementDuration()
        {
            // Permanent hazards never expire
            if (IsPermanent)
            {
                return false;
            }

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