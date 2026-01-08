using UnityEngine;
using System.Linq;

namespace Riftbourne.Combat
{
    /// <summary>
    /// ScriptableObject data container for faction relationship configurations.
    /// Allows designers to create reusable relationship presets that can be loaded into FactionRelationship component.
    /// Create via: Assets > Create > Riftbourne > Faction Relationship Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Faction Relationship Data", menuName = "Riftbourne/Faction Relationship Data")]
    public class FactionRelationshipData : ScriptableObject
    {
        [System.Serializable]
        public class RelationshipEntry
        {
            [Tooltip("Option 1: Use ScriptableObject faction")]
            public FactionData factionData1;
            
            [Tooltip("Option 2: Use enum faction (if ScriptableObject not assigned)")]
            public Faction faction1 = Faction.Player;
            
            [Tooltip("Option 1: Use ScriptableObject faction")]
            public FactionData factionData2;
            
            [Tooltip("Option 2: Use enum faction (if ScriptableObject not assigned)")]
            public Faction faction2 = Faction.Faction1;
            
            [Tooltip("Relationship type between these two factions")]
            public FactionRelationshipType relationship;
            
            /// <summary>
            /// Get the effective faction enum for faction1.
            /// </summary>
            public Faction GetFaction1()
            {
                if (factionData1 != null)
                {
                    FactionRegistry registry = Resources.Load<FactionRegistry>("FactionRegistry");
                    if (registry != null)
                    {
                        registry.BuildLookup();
                        Faction? mappedEnum = registry.GetEnumForFactionData(factionData1);
                        if (mappedEnum.HasValue)
                        {
                            return mappedEnum.Value;
                        }
                    }
                    // Fallback: check if it's player faction
                    if (factionData1.IsPlayerFaction)
                    {
                        return Faction.Player;
                    }
                }
                return faction1;
            }
            
            /// <summary>
            /// Get the effective faction enum for faction2.
            /// </summary>
            public Faction GetFaction2()
            {
                if (factionData2 != null)
                {
                    FactionRegistry registry = Resources.Load<FactionRegistry>("FactionRegistry");
                    if (registry != null)
                    {
                        registry.BuildLookup();
                        Faction? mappedEnum = registry.GetEnumForFactionData(factionData2);
                        if (mappedEnum.HasValue)
                        {
                            return mappedEnum.Value;
                        }
                    }
                    // Fallback: check if it's player faction
                    if (factionData2.IsPlayerFaction)
                    {
                        return Faction.Player;
                    }
                }
                return faction2;
            }
        }

        [Header("Faction Relationships")]
        [Tooltip("Define relationships between factions. Relationships are bidirectional - if Faction1 is Hostile to Faction2, Faction2 is also Hostile to Faction1.")]
        [SerializeField] private RelationshipEntry[] relationships = new RelationshipEntry[0];

        /// <summary>
        /// Get all relationship entries defined in this data asset.
        /// </summary>
        public RelationshipEntry[] Relationships => relationships;

        /// <summary>
        /// Apply these relationships to a FactionRelationship component.
        /// </summary>
        public void ApplyTo(FactionRelationship factionRelationship)
        {
            if (factionRelationship == null)
            {
                Debug.LogError("FactionRelationshipData: Cannot apply to null FactionRelationship component!");
                return;
            }

            foreach (var entry in relationships)
            {
                if (entry != null)
                {
                    // Get effective faction enums (handles both ScriptableObject and enum)
                    Faction f1 = entry.GetFaction1();
                    Faction f2 = entry.GetFaction2();
                    factionRelationship.SetRelationship(f1, f2, entry.relationship);
                }
            }

            Debug.Log($"FactionRelationshipData '{name}': Applied {relationships.Length} relationships to FactionRelationship component.");
        }
    }
}

