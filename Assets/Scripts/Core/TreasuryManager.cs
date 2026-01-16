using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Items;
using Riftbourne.Inventory;

namespace Riftbourne.Core
{
    /// <summary>
    /// Singleton manager for the treasury storage system.
    /// Stores items that can be retrieved at save points.
    /// </summary>
    public class TreasuryManager : MonoBehaviour
    {
        public static TreasuryManager Instance { get; private set; }

        [Header("Treasury Storage")]
        [SerializeField] private List<InventorySlot> treasuryItems = new List<InventorySlot>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("TreasuryManager: Multiple instances detected! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Add an item to the treasury.
        /// </summary>
        public void AddItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return;

            int remaining = quantity;

            // Try to stack with existing slots
            foreach (var slot in treasuryItems)
            {
                if (slot != null && slot.CanStack(item))
                {
                    remaining = slot.AddToStack(remaining);
                    if (remaining <= 0)
                    {
                        Debug.Log($"Treasury: Added {quantity}x {item.ItemName} (stacked)");
                        return;
                    }
                }
            }

            // Create new slots for remaining quantity
            while (remaining > 0)
            {
                int stackSize = Mathf.Min(remaining, item.MaxStackSize);
                treasuryItems.Add(new InventorySlot(item, stackSize));
                remaining -= stackSize;
            }

            Debug.Log($"Treasury: Added {quantity}x {item.ItemName}");
        }

        /// <summary>
        /// Remove an item from the treasury.
        /// </summary>
        public bool RemoveItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            int remaining = quantity;

            // Remove from slots
            for (int i = treasuryItems.Count - 1; i >= 0; i--)
            {
                var slot = treasuryItems[i];
                if (slot != null && slot.Item == item)
                {
                    int removed = slot.RemoveFromStack(remaining);
                    remaining -= removed;

                    if (slot.IsEmpty())
                        treasuryItems.RemoveAt(i);

                    if (remaining <= 0)
                    {
                        Debug.Log($"Treasury: Removed {quantity}x {item.ItemName}");
                        return true;
                    }
                }
            }

            return remaining < quantity; // Returns true if at least some was removed
        }

        /// <summary>
        /// Get all items in the treasury.
        /// </summary>
        public List<InventorySlot> GetAllItems()
        {
            return new List<InventorySlot>(treasuryItems);
        }

        /// <summary>
        /// Get the count of a specific item in treasury.
        /// </summary>
        public int GetItemCount(Item item)
        {
            int count = 0;

            foreach (var slot in treasuryItems)
            {
                if (slot != null && slot.Item == item)
                    count += slot.Quantity;
            }

            return count;
        }

        /// <summary>
        /// Check if treasury has a specific item in the required quantity.
        /// </summary>
        public bool HasItem(Item item, int quantity = 1)
        {
            return GetItemCount(item) >= quantity;
        }

        /// <summary>
        /// Clear all items from treasury.
        /// </summary>
        public void ClearTreasury()
        {
            treasuryItems.Clear();
            Debug.Log("Treasury: Cleared all items");
        }
    }
}
