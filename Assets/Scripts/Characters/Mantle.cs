namespace Riftbourne.Characters
{
    /// <summary>
    /// Represents a character's inborn magical affinity.
    /// Characters can have AT MOST one Mantle.
    /// Having a Mantle allows learning Current (magic) skills of that type.
    /// </summary>
    public enum MantleType
    {
        None,         // No magical capacity
        Pyromancy,    // Fire magic - area denial, damage over time
        Hydromancy,   // Water magic - healing, support, ice control
        Terramancy,   // Earth magic - defense, terrain manipulation
        Aeromancy,    // Air magic - mobility, knockback, lightning
        Electromancy, // Lightning magic - shock, chain attacks
        Animancy,     // Spirit/soul magic - buffs, debuffs, manipulation
        Luxomancy,    // Light magic - healing, purification, radiance
        Vitamancy,    // Life/nature magic - regeneration, poison, growth
        Psychomancy,  // Mind magic - control, illusion, fear
        Chronomancy   // Time magic - haste, slow, temporal manipulation
    }
}