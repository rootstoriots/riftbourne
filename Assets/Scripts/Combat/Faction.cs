using UnityEngine;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Represents a faction in the game. Factions determine unit allegiances.
    /// Special "Player" faction is reserved for player-controlled units.
    /// </summary>
    public enum Faction
    {
        Player,      // Player-controlled units (special handling)
        Faction1,    // First enemy faction
        Faction2,    // Second enemy faction
        Faction3,    // Third enemy faction
        Neutral      // Neutral units (neither hostile nor allied by default)
    }

    /// <summary>
    /// Relationship type between two factions.
    /// </summary>
    public enum FactionRelationshipType
    {
        Hostile,     // Enemies - will attack each other
        Neutral,     // No special relationship
        Ally         // Allies - will help each other
    }
}

