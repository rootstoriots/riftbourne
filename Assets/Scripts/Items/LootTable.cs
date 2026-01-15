using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Inventory;

namespace Riftbourne.Items
{
    /// <summary>
    /// ScriptableObject that defines a loot table for enemies.
    /// Contains guaranteed drops and random drops with probability-based rolls.
    /// </summary>
    [CreateAssetMenu(fileName = "New Loot Table", menuName = "Riftbourne/Items/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Header("Guaranteed Drops")]
        [Tooltip("Items that always drop when this loot table is used")]
        [SerializeField] private List<LootEntry> guaranteedDrops = new List<LootEntry>();

        [Header("Random Drops")]
        [Tooltip("Items that have a chance to drop based on their drop probability")]
        [SerializeField] private List<LootEntry> randomDrops = new List<LootEntry>();

        // Public properties
        public List<LootEntry> GuaranteedDrops => guaranteedDrops;
        public List<LootEntry> RandomDrops => randomDrops;

        /// <summary>
        /// Generates loot from this loot table.
        /// Returns a list of InventorySlots containing all dropped items.
        /// </summary>
        public List<InventorySlot> GenerateLoot()
        {
            List<InventorySlot> loot = new List<InventorySlot>();

            // Process guaranteed drops
            foreach (var entry in guaranteedDrops)
            {
                if (entry != null && entry.Item != null)
                {
                    InventorySlot slot = entry.RollForLoot();
                    if (slot != null && !slot.IsEmpty())
                    {
                        loot.Add(slot);
                    }
                }
            }

            // Process random drops (roll for each)
            foreach (var entry in randomDrops)
            {
                if (entry != null && entry.Item != null)
                {
                    InventorySlot slot = entry.RollForLoot();
                    if (slot != null && !slot.IsEmpty())
                    {
                        loot.Add(slot);
                    }
                }
            }

            return loot;
        }
    }
}
