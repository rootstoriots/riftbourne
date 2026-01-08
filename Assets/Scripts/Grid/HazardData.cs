using UnityEngine;
using Riftbourne.Combat;

namespace Riftbourne.Grid
{
    /// <summary>
    /// ScriptableObject that defines the properties of a hazard type.
    /// Supports both status-effect hazards and natural landscape hazards (rivers, etc.)
    /// Create these in the Project window: Right-click -> Create -> Riftbourne -> Hazard Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Hazard Data", menuName = "Riftbourne/Hazard Data")]
    public class HazardData : ScriptableObject
    {
        [Header("Hazard Identity")]
        [SerializeField] private string hazardName = "Fire";
        [Tooltip("Is this a natural landscape hazard (river, lava, etc.) that's part of the terrain?")]
        [SerializeField] private bool isNatural = false;

        [Header("Visual Settings")]
        [SerializeField] private Material hazardMaterial;
        [SerializeField] private Color fallbackColor = Color.red;
        [SerializeField] private float visualHeight = 0.03f;
        [SerializeField] private Vector3 visualScale = Vector3.one * 0.8f;
        [Tooltip("Enable emission glow for the hazard")]
        [SerializeField] private bool useEmission = true;
        [SerializeField] private Color emissionColor = Color.red;

        [Header("Status Effect")]
        [Tooltip("Status effect to apply when units step on this hazard. Leave empty for natural hazards that don't apply effects.")]
        [SerializeField] private StatusEffectData statusEffectData;
        [Tooltip("Duration override for status effect (uses StatusEffectData default if 0)")]
        [SerializeField] private int statusEffectDurationOverride = 0;

        [Header("Direct Damage")]
        [Tooltip("Does this hazard deal direct damage per turn (separate from status effect damage)?")]
        [SerializeField] private bool dealsDirectDamage = false;
        [Tooltip("Direct damage per turn (only used if dealsDirectDamage is true)")]
        [SerializeField] private int directDamagePerTurn = 0;

        // Public properties
        public string HazardName => hazardName;
        public bool IsNatural => isNatural;
        public Material HazardMaterial => hazardMaterial;
        public Color FallbackColor => fallbackColor;
        public float VisualHeight => visualHeight;
        public Vector3 VisualScale => visualScale;
        public bool UseEmission => useEmission;
        public Color EmissionColor => emissionColor;
        public StatusEffectData StatusEffectData => statusEffectData;
        public bool AppliesStatusEffect => statusEffectData != null;
        public int StatusEffectDurationOverride => statusEffectDurationOverride;
        public bool DealsDirectDamage => dealsDirectDamage;
        public int DirectDamagePerTurn => directDamagePerTurn;

        /// <summary>
        /// Get the effective status effect duration (uses override if set, otherwise from StatusEffectData).
        /// </summary>
        public int GetStatusEffectDuration()
        {
            if (statusEffectDurationOverride > 0)
            {
                return statusEffectDurationOverride;
            }
            // Default duration if no override and no status effect data
            return 3;
        }
    }
}

