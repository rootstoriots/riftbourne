using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Items;
using Riftbourne.Inventory;

namespace Riftbourne.NPCs
{
    /// <summary>
    /// Merchant NPC that can buy and sell items to/from players.
    /// Supports per-session buyback inventory.
    /// </summary>
    public class Merchant : MonoBehaviour
    {
        [Header("Merchant Settings")]
        [SerializeField] private string merchantName = "Merchant";
        [SerializeField] private List<Item> stock = new List<Item>();
        [SerializeField] [Range(0.1f, 0.9f)] private float sellPriceMultiplier = 0.5f;
        [SerializeField] [Range(0.5f, 2.0f)] private float buyPriceMultiplier = 1.0f;
        
        private List<InventorySlot> buybackInventory = new List<InventorySlot>();
        private Unit currentCustomer;
        
        public string MerchantName => merchantName;
        public List<Item> Stock => stock;
        
        /// <summary>
        /// Get the price the merchant will pay for an item (sell price).
        /// </summary>
        public int GetSellPrice(Item item)
        {
            return Mathf.RoundToInt(item.BaseValue * sellPriceMultiplier);
        }
        
        /// <summary>
        /// Get the price the merchant will charge for an item (buy price).
        /// </summary>
        public int GetBuyPrice(Item item)
        {
            return Mathf.RoundToInt(item.BaseValue * buyPriceMultiplier);
        }
        
        /// <summary>
        /// Sell an item to the merchant.
        /// Returns true if successful.
        /// </summary>
        public bool SellItemToMerchant(Unit seller, Item item, int quantity)
        {
            if (seller == null || item == null || quantity <= 0)
                return false;
            
            // Validate seller has items
            if (!seller.HasItem(item, quantity))
            {
                Debug.LogWarning($"{seller.UnitName} doesn't have {quantity}x {item.ItemName}!");
                return false;
            }
            
            // Can't sell key items
            if (item is KeyItem)
            {
                Debug.LogWarning("Cannot sell key items!");
                return false;
            }
            
            // Calculate payment
            int payment = GetSellPrice(item) * quantity;
            
            // Remove from seller
            seller.RemoveItem(item, quantity);
            
            // Add to buyback
            buybackInventory.Add(new InventorySlot(item, quantity));
            
            // Give currency
            seller.GainAurumShards(payment);
            
            Debug.Log($"{seller.UnitName} sold {quantity}x {item.ItemName} for {payment} Aurum Shards");
            return true;
        }
        
        /// <summary>
        /// Buy an item from the merchant.
        /// Returns true if successful.
        /// </summary>
        public bool BuyItemFromMerchant(Unit buyer, Item item, int quantity)
        {
            if (buyer == null || item == null || quantity <= 0)
                return false;
            
            // Validate item in stock
            if (!stock.Contains(item))
            {
                Debug.LogWarning($"{item.ItemName} not in stock!");
                return false;
            }
            
            // Calculate cost
            int cost = GetBuyPrice(item) * quantity;
            
            // Check buyer has currency
            if (buyer.AurumShards < cost)
            {
                Debug.LogWarning($"Insufficient funds! Need {cost}, have {buyer.AurumShards}");
                return false;
            }
            
            // Spend currency
            if (!buyer.SpendAurumShards(cost))
                return false;
            
            // Give item
            buyer.AddItem(item, quantity);
            
            Debug.Log($"{buyer.UnitName} bought {quantity}x {item.ItemName} for {cost} Aurum Shards");
            return true;
        }
        
        /// <summary>
        /// Buy back an item that was previously sold to this merchant.
        /// Returns true if successful.
        /// </summary>
        public bool BuybackItem(Unit buyer, InventorySlot slot)
        {
            if (buyer == null || slot == null)
                return false;
            
            // Check if in buyback inventory
            if (!buybackInventory.Contains(slot))
            {
                Debug.LogWarning("Item not in buyback inventory!");
                return false;
            }
            
            // Calculate refund (100% of sell price)
            int refund = GetSellPrice(slot.Item) * slot.Quantity;
            
            // Check buyer has currency
            if (buyer.AurumShards < refund)
            {
                Debug.LogWarning($"Insufficient funds for buyback! Need {refund}, have {buyer.AurumShards}");
                return false;
            }
            
            // Spend currency
            if (!buyer.SpendAurumShards(refund))
                return false;
            
            // Give item back
            buyer.AddItem(slot.Item, slot.Quantity);
            
            // Remove from buyback
            buybackInventory.Remove(slot);
            
            Debug.Log($"{buyer.UnitName} bought back {slot.Quantity}x {slot.Item.ItemName} for {refund} Aurum Shards");
            return true;
        }
        
        /// <summary>
        /// Open a trading session with a customer.
        /// </summary>
        public void OpenMerchantSession(Unit customer)
        {
            currentCustomer = customer;
            Debug.Log($"{merchantName} opened trading with {customer.UnitName}");
        }
        
        /// <summary>
        /// End the trading session and clear buyback inventory.
        /// </summary>
        public void EndMerchantSession()
        {
            buybackInventory.Clear();
            currentCustomer = null;
            Debug.Log($"{merchantName} closed trading session");
        }
        
        [ContextMenu("Debug: Show Stock")]
        public void DebugShowStock()
        {
            Debug.Log($"=== {merchantName} Stock ===");
            foreach (var item in stock)
            {
                Debug.Log($"{item.ItemName} - Buy: {GetBuyPrice(item)} AS");
            }
        }
    }
}
