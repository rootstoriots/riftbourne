namespace Riftbourne.Characters
{
    /// <summary>
    /// Defines the types of melee weapons available in the game.
    /// Used to categorize melee equipment for gameplay mechanics and skill requirements.
    /// </summary>
    public enum MeleeWeaponType
    {
        None,               // No specific melee weapon type
        ShortBlade,         // Short blades, daggers, etc.
        Sword,              // Standard swords
        HeavyBlade,         // Large blades, greatswords, etc.
        OneHandedBlunt,     // One-handed blunt weapons, maces, clubs, etc.
        TwoHandedBlunt,     // Two-handed blunt weapons, hammers, etc.
        Spear,              // Spears and lances
        Staff,              // Staves, magical or melee
        Polearm,            // Polearms, halberds, etc.
        Gloves              // Unarmed combat, gauntlets, etc.
    }
}
