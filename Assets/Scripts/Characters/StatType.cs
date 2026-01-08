namespace Riftbourne.Characters
{
    /// <summary>
    /// Enumeration of all stat types in the game.
    /// Used for type-safe stat bonus lookups instead of string-based switches.
    /// </summary>
    public enum StatType
    {
        Attack,      // Physical attack power
        Defense,     // Physical defense
        Strength,    // Physical power, melee damage
        Finesse,     // Precision, ranged damage, evasion
        Focus,       // Mental power, magic damage, resistance
        Speed,       // Turn order (higher = goes first)
        Luck         // Critical chance, item drops
    }
}

