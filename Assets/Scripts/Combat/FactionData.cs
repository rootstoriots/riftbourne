using UnityEngine;
using System.Collections.Generic;

namespace Riftbourne.Combat
{
    /// <summary>
    /// ScriptableObject representation of a faction.
    /// Allows designers to create and configure factions without code changes.
    /// Each faction defines its own relationships to other factions.
    /// Create via: Assets > Create > Riftbourne > Faction Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Faction", menuName = "Riftbourne/Faction Data")]
    public class FactionData : ScriptableObject
    {
        [System.Serializable]
        public class FactionRelationshipEntry
        {
            [Tooltip("The other faction this relationship applies to")]
            public FactionData targetFaction;
            
            [Tooltip("Relationship type to this faction")]
            public FactionRelationshipType relationship;
        }

        [Header("Faction Identity")]
        [Tooltip("Display name of this faction")]
        [SerializeField] private string factionName = "New Faction";
        
        [TextArea(3, 5)]
        [Tooltip("Description of this faction")]
        [SerializeField] private string description = "Faction description";

        [Header("Visual Identity")]
        [Tooltip("Color associated with this faction (for UI, minimap, etc.)")]
        [SerializeField] private Color factionColor = Color.white;

        [Header("Faction Settings")]
        [Tooltip("Is this faction the player faction?")]
        [SerializeField] private bool isPlayerFaction = false;
        
        [Tooltip("Is this faction neutral by default?")]
        [SerializeField] private bool isNeutralByDefault = false;

        [Header("Faction Relationships")]
        [Tooltip("Define this faction's relationships to other factions. Relationships are bidirectional - if Faction A is Hostile to Faction B, Faction B is also Hostile to Faction A.")]
        [SerializeField] private List<FactionRelationshipEntry> relationships = new List<FactionRelationshipEntry>();

        // Public properties
        public string FactionName => factionName;
        public string Description => description;
        public Color FactionColor => factionColor;
        public bool IsPlayerFaction => isPlayerFaction;
        public bool IsNeutralByDefault => isNeutralByDefault;
        public List<FactionRelationshipEntry> Relationships => relationships;

        /// <summary>
        /// Unique identifier for this faction (based on asset name or GUID).
        /// Used for comparison and lookups.
        /// </summary>
        public string FactionID => name; // Use asset name as ID

        /// <summary>
        /// Get the relationship type to another faction.
        /// Returns the relationship if defined, or default based on settings.
        /// </summary>
        public FactionRelationshipType GetRelationshipTo(FactionData otherFaction)
        {
            if (otherFaction == null || otherFaction == this)
            {
                return FactionRelationshipType.Ally; // Same faction = always allies
            }

            // Check explicit relationships
            foreach (var entry in relationships)
            {
                if (entry != null && entry.targetFaction == otherFaction)
                {
                    return entry.relationship;
                }
            }

            // Default behavior
            if (isNeutralByDefault || otherFaction.isNeutralByDefault)
            {
                return FactionRelationshipType.Neutral;
            }

            return FactionRelationshipType.Hostile; // Default: hostile
        }

        private void OnValidate()
        {
            // Ensure name is set
            if (string.IsNullOrEmpty(factionName))
            {
                factionName = name;
            }
        }
    }
}

