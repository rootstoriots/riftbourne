using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Manages relationships between factions.
    /// Uses a relationship matrix to determine if factions are hostile, neutral, or allied.
    /// </summary>
    public class FactionRelationship : MonoBehaviour
    {
        public static FactionRelationship Instance { get; private set; }

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
        [Tooltip("Option 1: Load from FactionRegistry (recommended - reads relationships from FactionData assets)")]
        [SerializeField] private FactionRegistry factionRegistry;
        
        [Tooltip("Option 2: Load from legacy FactionRelationshipData asset (deprecated - use FactionRegistry instead)")]
        [SerializeField] private FactionRelationshipData relationshipData;
        
        [Tooltip("Option 3: Define relationships manually in Inspector. If not specified, defaults to Hostile for different factions, Ally for same faction.")]
        [SerializeField] private List<RelationshipEntry> relationships = new List<RelationshipEntry>();

        // Cache for quick lookups
        private Dictionary<Faction, Dictionary<Faction, FactionRelationshipType>> relationshipCache;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // Priority 1: Load from FactionRegistry (reads from FactionData assets)
                if (factionRegistry != null)
                {
                    factionRegistry.ApplyRelationshipsTo(this);
                }
                else
                {
                    // Try to load from Resources as fallback
                    FactionRegistry registry = Resources.Load<FactionRegistry>("FactionRegistry");
                    if (registry != null)
                    {
                        registry.ApplyRelationshipsTo(this);
                    }
                }
                
                // Priority 2: Load from legacy FactionRelationshipData (backward compatibility)
                if (relationshipData != null)
                {
                    relationshipData.ApplyTo(this);
                }
                
                BuildRelationshipCache();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Build a cache of relationships for O(1) lookups.
        /// </summary>
        private void BuildRelationshipCache()
        {
            relationshipCache = new Dictionary<Faction, Dictionary<Faction, FactionRelationshipType>>();

            // Initialize all factions
            foreach (Faction faction in System.Enum.GetValues(typeof(Faction)))
            {
                relationshipCache[faction] = new Dictionary<Faction, FactionRelationshipType>();
            }

            // Process relationships from ScriptableObject data (if loaded)
            if (relationshipData != null && relationshipData.Relationships != null)
            {
                foreach (var entry in relationshipData.Relationships)
                {
                    if (entry == null) continue;
                    
                    // Get effective faction enums (handles both ScriptableObject and enum)
                    Faction f1 = entry.GetFaction1();
                    Faction f2 = entry.GetFaction2();
                    
                    if (!relationshipCache.ContainsKey(f1) || !relationshipCache.ContainsKey(f2))
                        continue;

                    // Relationships are bidirectional
                    relationshipCache[f1][f2] = entry.relationship;
                    relationshipCache[f2][f1] = entry.relationship;
                }
            }

            // Process explicit relationships from Inspector (manual configuration)
            foreach (var entry in relationships)
            {
                if (entry == null) continue;
                
                // Get effective faction enums (handles both ScriptableObject and enum)
                Faction f1 = entry.GetFaction1();
                Faction f2 = entry.GetFaction2();
                
                if (!relationshipCache.ContainsKey(f1) || !relationshipCache.ContainsKey(f2))
                    continue;

                // Relationships are bidirectional (manual entries override ScriptableObject entries)
                relationshipCache[f1][f2] = entry.relationship;
                relationshipCache[f2][f1] = entry.relationship;
            }
        }

        /// <summary>
        /// Get the relationship between two factions.
        /// </summary>
        public FactionRelationshipType GetRelationship(Faction faction1, Faction faction2)
        {
            // Same faction = always allies
            if (faction1 == faction2)
            {
                return FactionRelationshipType.Ally;
            }

            // Check cache for explicit relationship
            if (relationshipCache != null && 
                relationshipCache.ContainsKey(faction1) && 
                relationshipCache[faction1].ContainsKey(faction2))
            {
                return relationshipCache[faction1][faction2];
            }

            // Default: different factions are hostile
            return FactionRelationshipType.Hostile;
        }

        /// <summary>
        /// Check if two factions are hostile to each other.
        /// </summary>
        public bool AreHostile(Faction faction1, Faction faction2)
        {
            return GetRelationship(faction1, faction2) == FactionRelationshipType.Hostile;
        }

        /// <summary>
        /// Check if two factions are allied.
        /// </summary>
        public bool AreAllied(Faction faction1, Faction faction2)
        {
            return GetRelationship(faction1, faction2) == FactionRelationshipType.Ally;
        }

        /// <summary>
        /// Check if two factions are neutral to each other.
        /// </summary>
        public bool AreNeutral(Faction faction1, Faction faction2)
        {
            return GetRelationship(faction1, faction2) == FactionRelationshipType.Neutral;
        }

        /// <summary>
        /// Set a relationship between two factions (runtime modification).
        /// </summary>
        public void SetRelationship(Faction faction1, Faction faction2, FactionRelationshipType relationship)
        {
            if (relationshipCache == null)
            {
                BuildRelationshipCache();
            }

            if (!relationshipCache.ContainsKey(faction1))
            {
                relationshipCache[faction1] = new Dictionary<Faction, FactionRelationshipType>();
            }
            if (!relationshipCache.ContainsKey(faction2))
            {
                relationshipCache[faction2] = new Dictionary<Faction, FactionRelationshipType>();
            }

            // Set bidirectional relationship
            relationshipCache[faction1][faction2] = relationship;
            relationshipCache[faction2][faction1] = relationship;
        }
    }
}

