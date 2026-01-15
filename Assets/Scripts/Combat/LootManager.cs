using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Items;
using Riftbourne.Inventory;
using Riftbourne.Core;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Manages loot collection and distribution after combat.
    /// Accumulates loot from defeated enemies and displays it after battle.
    /// </summary>
    public class LootManager : MonoBehaviour
    {
        private static LootManager instance;
        public static LootManager Instance => instance;
        
        private List<InventorySlot> battleLoot = new List<InventorySlot>();
        private int battleCurrency = 0;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Add loot data from an enemy to the battle loot pool.
        /// Called by TurnManager when an enemy with EnemyLoot component dies.
        /// </summary>
        public void AddLoot(LootData lootData)
        {
            if (lootData.IsEmpty())
                return;

            Debug.Log($"Adding loot to battle loot pool...");

            // Add items
            if (lootData.items != null)
            {
                foreach (var slot in lootData.items)
                {
                    if (slot != null && slot.Item != null && !slot.IsEmpty())
                    {
                        AddToBattleLoot(slot.Item, slot.Quantity);
                    }
                }
            }

            // Add currency
            if (lootData.currency > 0)
            {
                battleCurrency += lootData.currency;
                Debug.Log($"Added {lootData.currency} Aurum Shards to battle loot (Total: {battleCurrency})");
            }
        }
        
        /// <summary>
        /// Add an item to the battle loot pool.
        /// </summary>
        public void AddToBattleLoot(Item item, int quantity)
        {
            if (item == null || quantity <= 0)
                return;
            
            // Try to stack
            foreach (var slot in battleLoot)
            {
                if (slot.CanStack(item))
                {
                    slot.AddToStack(quantity);
                    return;
                }
            }
            
            // Create new slot
            battleLoot.Add(new InventorySlot(item, quantity));
            Debug.Log($"Added {quantity}x {item.ItemName} to battle loot");
        }
        
        /// <summary>
        /// Display post-battle loot and distribute to party.
        /// Now handled by LootSelectionUI in BattleEndHandler.
        /// This method is kept for backward compatibility but does nothing.
        /// </summary>
        public void ShowPostBattleLoot()
        {
            // Loot selection is now handled by LootSelectionUI in BattleEndHandler
            // This method is kept for backward compatibility
            Debug.Log("LootManager.ShowPostBattleLoot: Loot selection is now handled by LootSelectionUI in BattleEndHandler.");
        }
        
        /// <summary>
        /// Get current battle loot (for LootSelectionUI).
        /// </summary>
        public List<InventorySlot> GetBattleLoot()
        {
            return new List<InventorySlot>(battleLoot);
        }
        
        /// <summary>
        /// Get current battle currency (for LootSelectionUI).
        /// </summary>
        public int GetBattleCurrency()
        {
            return battleCurrency;
        }
        
        /// <summary>
        /// Clear the battle loot pool.
        /// </summary>
        public void ClearBattleLoot()
        {
            battleLoot.Clear();
            battleCurrency = 0;
        }
        
        [ContextMenu("Debug: Add Random Loot")]
        public void DebugAddRandomLoot()
        {
            // TODO: Create test items
            Debug.Log("Add test items to test this feature");
        }
    }
}
