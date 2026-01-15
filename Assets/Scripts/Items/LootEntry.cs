using UnityEngine;
using Riftbourne.Inventory;

namespace Riftbourne.Items
{
    /// <summary>
    /// Represents a single loot entry in a loot table.
    /// Defines an item that can drop with a probability and quantity range.
    /// </summary>
    [System.Serializable]
    public class LootEntry
    {
        [Header("Item")]
        [Tooltip("The item that can drop")]
        [SerializeField] private Item item;

        [Header("Drop Settings")]
        [Tooltip("Probability of this item dropping (0.0 to 1.0, where 1.0 = 100%)")]
        [Range(0f, 1f)]
        [SerializeField] private float dropChance = 1.0f;

        [Tooltip("Minimum quantity that can drop")]
        [SerializeField] private int minQuantity = 1;

        [Tooltip("Maximum quantity that can drop")]
        [SerializeField] private int maxQuantity = 1;

        // Public properties
        public Item Item => item;
        public float DropChance => dropChance;
        public int MinQuantity => minQuantity;
        public int MaxQuantity => maxQuantity;

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public LootEntry()
        {
            item = null;
            dropChance = 1.0f;
            minQuantity = 1;
            maxQuantity = 1;
        }

        /// <summary>
        /// Creates a new loot entry.
        /// </summary>
        public LootEntry(Item item, float dropChance, int minQuantity, int maxQuantity)
        {
            this.item = item;
            this.dropChance = Mathf.Clamp01(dropChance);
            this.minQuantity = Mathf.Max(1, minQuantity);
            this.maxQuantity = Mathf.Max(minQuantity, maxQuantity);
        }

        /// <summary>
        /// Rolls for this loot entry and returns an InventorySlot if successful.
        /// Returns null if the roll fails or item is null.
        /// </summary>
        public InventorySlot RollForLoot()
        {
            if (item == null)
                return null;

            // Roll for drop
            if (Random.value > dropChance)
                return null;

            // Determine quantity
            int quantity = Random.Range(minQuantity, maxQuantity + 1);
            if (quantity <= 0)
                return null;

            // Clamp quantity to item's max stack size
            quantity = Mathf.Min(quantity, item.MaxStackSize);

            return new InventorySlot(item, quantity);
        }
    }
}
