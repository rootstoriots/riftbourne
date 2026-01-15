namespace Riftbourne.Items
{
    /// <summary>
    /// Enumeration of target types for consumable items.
    /// Defines who or what can be targeted when using a consumable.
    /// </summary>
    public enum ConsumableTargetType
    {
        Self,         // Can only target the user
        SingleAlly,   // Targets one friendly unit
        SingleEnemy,  // Targets one enemy unit
        AllAllies,    // Affects all friendly units
        AllEnemies,   // Affects all enemy units
        GroundAOE     // Targets a ground location with area of effect
    }
}
