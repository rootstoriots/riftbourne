namespace Riftbourne.Characters
{
    /// <summary>
    /// Unit type classification. Influences available actions and skill preferences.
    /// Separate from AI behavior - a Beast can be Support type, etc.
    /// </summary>
    public enum UnitType
    {
        Beast,      // Usually melee attacks and melee skill attacks
        Soldier,    // Can be melee or ranged depending on equipped items
        Magi        // Prioritizes skill attacks, ground skills
    }
}

