using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Registry that manages all faction ScriptableObjects.
    /// Provides lookup and management for custom factions.
    /// Create via: Assets > Create > Riftbourne > Faction Registry
    /// </summary>
    [CreateAssetMenu(fileName = "FactionRegistry", menuName = "Riftbourne/Faction Registry")]
    public class FactionRegistry : ScriptableObject
    {
        [Header("Faction Definitions")]
        [Tooltip("All faction ScriptableObjects used in the game. Each faction should be registered here. Factions are also auto-registered from units at battle start.")]
        [SerializeField] private List<FactionData> registeredFactions = new List<FactionData>();

        [Header("Player Faction")]
        [Tooltip("Reference to the player faction (optional - can also be identified by IsPlayerFaction flag in FactionData). Used for convenience lookups.")]
        [SerializeField] private FactionData playerFaction;

        // Runtime lookup cache
        private Dictionary<string, FactionData> factionLookup;
        internal Dictionary<Faction, FactionData> enumToDataLookup; // Internal for access from other classes

        /// <summary>
        /// Get all registered factions.
        /// </summary>
        public List<FactionData> RegisteredFactions => registeredFactions;

        /// <summary>
        /// Build lookup dictionaries for fast access.
        /// Call this after loading or when data changes.
        /// </summary>
        public void BuildLookup()
        {
            factionLookup = new Dictionary<string, FactionData>();
            enumToDataLookup = new Dictionary<Faction, FactionData>();

            // Build ID-based lookup
            foreach (var faction in registeredFactions)
            {
                if (faction != null && !string.IsNullOrEmpty(faction.FactionID))
                {
                    factionLookup[faction.FactionID] = faction;
                }
            }

            // Map player faction to Player enum (if specified or found via IsPlayerFaction flag)
            FactionData player = playerFaction;
            if (player == null)
            {
                // Find player faction by IsPlayerFaction flag
                player = registeredFactions.FirstOrDefault(f => f != null && f.IsPlayerFaction);
            }
            if (player != null)
            {
                enumToDataLookup[Faction.Player] = player;
            }

            // Auto-assign other factions to available enum slots (Faction1, Faction2, Faction3, Neutral)
            // This ensures all registered factions get enum mappings
            int enumSlotIndex = 0;
            Faction[] availableEnums = { Faction.Faction1, Faction.Faction2, Faction.Faction3, Faction.Neutral };
            
            foreach (var faction in registeredFactions)
            {
                if (faction == null || faction.IsPlayerFaction) continue;
                if (enumToDataLookup.ContainsValue(faction)) continue; // Already mapped
                
                if (enumSlotIndex < availableEnums.Length)
                {
                    enumToDataLookup[availableEnums[enumSlotIndex]] = faction;
                    enumSlotIndex++;
                }
            }
        }

        /// <summary>
        /// Apply all faction relationships to a FactionRelationship component.
        /// Reads relationships from each FactionData and applies them.
        /// </summary>
        public void ApplyRelationshipsTo(FactionRelationship factionRelationship)
        {
            if (factionRelationship == null)
            {
                Debug.LogError("FactionRegistry: Cannot apply relationships to null FactionRelationship component!");
                return;
            }

            BuildLookup(); // Ensure lookup is built (this auto-assigns enum mappings)

            int relationshipCount = 0;

            // Process relationships from each registered faction
            foreach (var faction in registeredFactions)
            {
                if (faction == null || faction.Relationships == null) continue;

                // Get this faction's enum value (should always be mapped now after BuildLookup)
                Faction? thisFactionEnum = GetEnumForFactionData(faction);
                if (!thisFactionEnum.HasValue)
                {
                    Debug.LogWarning($"FactionRegistry: Faction '{faction.FactionName}' is not mapped to an enum and will be skipped for relationships.");
                    continue;
                }

                foreach (var relationshipEntry in faction.Relationships)
                {
                    if (relationshipEntry == null || relationshipEntry.targetFaction == null) continue;

                    // Get target faction's enum value
                    Faction? targetFactionEnum = GetEnumForFactionData(relationshipEntry.targetFaction);
                    if (!targetFactionEnum.HasValue)
                    {
                        Debug.LogWarning($"FactionRegistry: Target faction '{relationshipEntry.targetFaction.FactionName}' is not mapped to an enum. Skipping relationship.");
                        continue;
                    }

                    // Apply relationship (bidirectional)
                    factionRelationship.SetRelationship(thisFactionEnum.Value, targetFactionEnum.Value, relationshipEntry.relationship);
                    relationshipCount++;
                }
            }

            Debug.Log($"FactionRegistry: Applied {relationshipCount} relationships from {registeredFactions.Count} factions to FactionRelationship component.");
        }

        /// <summary>
        /// Get the enum value for a FactionData, or null if not mapped.
        /// </summary>
        public Faction? GetEnumForFactionData(FactionData factionData)
        {
            if (factionData == null) return null;

            if (enumToDataLookup == null)
            {
                BuildLookup();
            }

            // Check if this faction is mapped to an enum
            foreach (var kvp in enumToDataLookup)
            {
                if (kvp.Value == factionData)
                {
                    return kvp.Key;
                }
            }

            return null; // Not mapped - should not happen if BuildLookup was called
        }

        /// <summary>
        /// Get a faction by its ID.
        /// </summary>
        public FactionData GetFactionByID(string factionID)
        {
            if (factionLookup == null)
            {
                BuildLookup();
            }

            if (factionLookup.TryGetValue(factionID, out FactionData faction))
            {
                return faction;
            }

            Debug.LogWarning($"FactionData with ID '{factionID}' not found in registry!");
            return null;
        }

        /// <summary>
        /// Get a faction by enum value (for backward compatibility).
        /// </summary>
        public FactionData GetFactionByEnum(Faction factionEnum)
        {
            if (enumToDataLookup == null)
            {
                BuildLookup();
            }

            if (enumToDataLookup.TryGetValue(factionEnum, out FactionData faction))
            {
                return faction;
            }

            return null;
        }

        /// <summary>
        /// Check if a faction is registered.
        /// </summary>
        public bool IsRegistered(string factionID)
        {
            if (factionLookup == null)
            {
                BuildLookup();
            }

            return factionLookup.ContainsKey(factionID);
        }

        /// <summary>
        /// Register a faction at runtime.
        /// </summary>
        public void RegisterFaction(FactionData faction)
        {
            if (faction == null) return;

            if (!registeredFactions.Contains(faction))
            {
                registeredFactions.Add(faction);
                BuildLookup(); // Rebuild cache
            }
        }

        /// <summary>
        /// Unregister a faction at runtime.
        /// </summary>
        public void UnregisterFaction(FactionData faction)
        {
            if (faction == null) return;

            if (registeredFactions.Remove(faction))
            {
                BuildLookup(); // Rebuild cache
            }
        }
    }
}

