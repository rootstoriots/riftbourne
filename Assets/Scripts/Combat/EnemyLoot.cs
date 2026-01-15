using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Items;
using Riftbourne.Inventory;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Component for enemies that generates loot when they die.
    /// Uses LootTable ScriptableObjects to define what items drop.
    /// </summary>
    public class EnemyLoot : MonoBehaviour
    {
        [Header("Loot Tables")]
        [Tooltip("Loot tables to use for generating drops. All tables are processed.")]
        [SerializeField] private LootTable[] lootTables = new LootTable[0];

        [Header("Currency Settings")]
        [Tooltip("Whether this enemy drops currency (Aurum Shards) when killed")]
        [SerializeField] private bool dropCurrency = true;

        /// <summary>
        /// Generates loot from all loot tables and currency from the unit.
        /// Returns LootData containing all dropped items and currency.
        /// </summary>
        public LootData GenerateLoot()
        {
            List<InventorySlot> allItems = new List<InventorySlot>();
            int currencyAmount = 0;

            // Generate items from all loot tables
            foreach (var table in lootTables)
            {
                if (table != null)
                {
                    List<InventorySlot> tableLoot = table.GenerateLoot();
                    if (tableLoot != null)
                    {
                        allItems.AddRange(tableLoot);
                    }
                }
            }

            // Add currency if enabled and unit has currency
            if (dropCurrency)
            {
                Unit unit = GetComponent<Unit>();
                if (unit != null)
                {
                    currencyAmount = unit.AurumShards;
                }
            }

            return new LootData(allItems, currencyAmount);
        }
    }
}
