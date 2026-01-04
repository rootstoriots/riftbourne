namespace Riftbourne.Characters
{
    /// <summary>
    /// Defines the six equipment slots available to units.
    /// Any equipment may teach skills, but Codex is purely for learning.
    /// </summary>
    public enum EquipmentSlot
    {
        MeleeWeapon,    // Swords, axes, etc. - can teach melee combat skills
        RangedWeapon,   // Bows, crossbows, etc. - can teach ranged skills
        Armor,          // Chest piece - can teach defensive skills
        Accessory1,     // Ring, amulet, etc. - can teach utility skills
        Accessory2,     // Second accessory slot for variety
        Codex           // Books, grimoires, manuals - purely for skill learning
    }
}