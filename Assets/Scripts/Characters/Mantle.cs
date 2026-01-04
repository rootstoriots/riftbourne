namespace Riftbourne.Characters
{
    /// <summary>
    /// Represents a character's inborn magical affinity.
    /// Characters can have AT MOST one Mantle.
    /// Having a Mantle allows learning Current (magic) skills of that type.
    /// </summary>
    public enum MantleType
    {
        None,        // No magical capacity
        Pyromancy,   // Fire magic
        Hydromancy,  // Water magic (future)
        Terramancy,  // Earth magic (future)
        Aeromancy    // Air magic (future)
    }
}