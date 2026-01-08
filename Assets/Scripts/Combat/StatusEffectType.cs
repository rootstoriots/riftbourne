namespace Riftbourne.Combat
{
    /// <summary>
    /// Enumeration of all status effect types in the game.
    /// Used for type-safe status effect handling and tracking.
    /// </summary>
    public enum StatusEffectType
    {
        Burn,        // Damage over time (fire-based)
        Poison,      // Damage over time (poison-based)
        Freeze,      // Prevents movement/actions
        Stun,        // Prevents actions but not movement
        Slow,        // Reduces movement speed
        Haste,       // Increases movement speed
        Regeneration, // Heals over time
        Shield,      // Damage reduction
        // Add more types as needed
    }
}

