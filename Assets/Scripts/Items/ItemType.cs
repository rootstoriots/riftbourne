namespace Riftbourne.Items
{
    /// <summary>
    /// Enumeration of all item types in the game.
    /// Used to categorize items for inventory management and usage rules.
    /// </summary>
    public enum ItemType
    {
        Loot,                  // Vendor trash and crafting materials
        ConsumableBattle,      // Usable items that work in combat
        ConsumableNonBattle,   // Usable items that work outside combat
        Equipment,             // Weapons, armor, accessories
        Container,             // Bags, backpacks that reduce encumbrance
        KeyItem                // Quest items and story items
    }
}
