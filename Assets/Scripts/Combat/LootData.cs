using System.Collections.Generic;
using Riftbourne.Inventory;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Data structure for passing loot from enemies to LootManager.
    /// Contains items and currency that an enemy drops upon death.
    /// </summary>
    public struct LootData
    {
        public List<InventorySlot> items;
        public int currency;

        /// <summary>
        /// Creates a new LootData instance.
        /// </summary>
        public LootData(List<InventorySlot> items, int currency)
        {
            this.items = items ?? new List<InventorySlot>();
            this.currency = currency;
        }

        /// <summary>
        /// Creates an empty LootData instance.
        /// </summary>
        public static LootData Empty => new LootData(new List<InventorySlot>(), 0);

        /// <summary>
        /// Checks if this loot data is empty (no items and no currency).
        /// </summary>
        public bool IsEmpty()
        {
            return (items == null || items.Count == 0) && currency <= 0;
        }
    }
}
