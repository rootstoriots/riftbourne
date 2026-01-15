using Riftbourne.Items;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Defines weapon families for proficiency tracking.
    /// Each weapon type (melee or ranged) maps to its own unique family.
    /// Used to track practical familiarity and combat reliability with weapon families.
    /// </summary>
    public enum WeaponFamily
    {
        None,               // No weapon family
        ShortBlade,         // Short blades, daggers, etc.
        Sword,              // Standard swords
        HeavyBlade,         // Large blades, greatswords, etc.
        OneHandedBlunt,     // One-handed blunt weapons, maces, clubs, etc.
        TwoHandedBlunt,     // Two-handed blunt weapons, hammers, etc.
        Spear,              // Spears and lances
        Staff,              // Staves, magical or melee
        Polearm,            // Polearms, halberds, etc.
        Gloves,             // Unarmed combat, gauntlets, etc.
        Bows,               // Bows and longbows
        Crossbows,          // Crossbows
        Handguns,           // Handguns, pistols, etc.
        Rifles              // Rifles and long-range firearms
    }

    /// <summary>
    /// Helper methods for weapon family operations.
    /// </summary>
    public static class WeaponFamilyHelper
    {
        /// <summary>
        /// Get the weapon family from an equipment item.
        /// Returns WeaponFamily.None if the item is not a weapon or has no weapon type.
        /// </summary>
        public static WeaponFamily GetWeaponFamily(EquipmentItem item)
        {
            if (item == null) return WeaponFamily.None;

            // Check melee weapon type first
            if (item.MeleeWeaponType != MeleeWeaponType.None)
            {
                return MeleeWeaponTypeToFamily(item.MeleeWeaponType);
            }

            // Check ranged weapon type
            if (item.RangedWeaponType != RangedWeaponType.None)
            {
                return RangedWeaponTypeToFamily(item.RangedWeaponType);
            }

            return WeaponFamily.None;
        }

        /// <summary>
        /// Convert MeleeWeaponType to WeaponFamily.
        /// </summary>
        public static WeaponFamily MeleeWeaponTypeToFamily(MeleeWeaponType meleeType)
        {
            switch (meleeType)
            {
                case MeleeWeaponType.ShortBlade: return WeaponFamily.ShortBlade;
                case MeleeWeaponType.Sword: return WeaponFamily.Sword;
                case MeleeWeaponType.HeavyBlade: return WeaponFamily.HeavyBlade;
                case MeleeWeaponType.OneHandedBlunt: return WeaponFamily.OneHandedBlunt;
                case MeleeWeaponType.TwoHandedBlunt: return WeaponFamily.TwoHandedBlunt;
                case MeleeWeaponType.Spear: return WeaponFamily.Spear;
                case MeleeWeaponType.Staff: return WeaponFamily.Staff;
                case MeleeWeaponType.Polearm: return WeaponFamily.Polearm;
                case MeleeWeaponType.Gloves: return WeaponFamily.Gloves;
                default: return WeaponFamily.None;
            }
        }

        /// <summary>
        /// Convert RangedWeaponType to WeaponFamily.
        /// </summary>
        public static WeaponFamily RangedWeaponTypeToFamily(RangedWeaponType rangedType)
        {
            switch (rangedType)
            {
                case RangedWeaponType.Bows: return WeaponFamily.Bows;
                case RangedWeaponType.Crossbows: return WeaponFamily.Crossbows;
                case RangedWeaponType.Handguns: return WeaponFamily.Handguns;
                case RangedWeaponType.Rifles: return WeaponFamily.Rifles;
                default: return WeaponFamily.None;
            }
        }
    }
}
