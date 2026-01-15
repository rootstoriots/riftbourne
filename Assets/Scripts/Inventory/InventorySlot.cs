using UnityEngine;
using Riftbourne.Items;

namespace Riftbourne.Inventory
{
    /// <summary>
    /// Represents a stack of items in an inventory slot.
    /// Serializable class for use in inventory management systems.
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [SerializeField] private Item item;
        [SerializeField] private int quantity;

        // Public properties
        public Item Item => item;
        public int Quantity => quantity;

        /// <summary>
        /// Creates a new inventory slot with the specified item and quantity.
        /// Quantity will be clamped to the item's maxStackSize.
        /// </summary>
        public InventorySlot(Item item, int quantity)
        {
            this.item = item;
            if (item != null)
            {
                this.quantity = UnityEngine.Mathf.Clamp(quantity, 0, item.MaxStackSize);
            }
            else
            {
                this.quantity = 0;
            }
        }

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }

        /// <summary>
        /// Gets the total weight of this stack (item weight * quantity).
        /// </summary>
        public float GetTotalWeight()
        {
            if (item == null)
            {
                return 0f;
            }
            return item.Weight * quantity;
        }

        /// <summary>
        /// Checks if the specified item can be stacked with the current item.
        /// </summary>
        public bool CanStack(Item otherItem)
        {
            if (item == null || otherItem == null)
            {
                return false;
            }
            return item == otherItem && quantity < item.MaxStackSize;
        }

        /// <summary>
        /// Adds the specified amount to this stack.
        /// Returns the amount that didn't fit (overflow).
        /// </summary>
        public int AddToStack(int amount)
        {
            if (item == null)
            {
                return amount; // Can't add to empty slot
            }

            int spaceAvailable = item.MaxStackSize - quantity;
            int amountToAdd = UnityEngine.Mathf.Min(amount, spaceAvailable);
            quantity += amountToAdd;
            
            return amount - amountToAdd; // Return overflow
        }

        /// <summary>
        /// Removes the specified amount from this stack.
        /// Returns the actual amount removed (may be less than requested if insufficient quantity).
        /// </summary>
        public int RemoveFromStack(int amount)
        {
            int amountToRemove = UnityEngine.Mathf.Min(amount, quantity);
            quantity -= amountToRemove;
            
            // Clear item reference if stack is empty
            if (quantity <= 0)
            {
                item = null;
                quantity = 0;
            }
            
            return amountToRemove;
        }

        /// <summary>
        /// Checks if this slot is empty (no item or quantity <= 0).
        /// </summary>
        public bool IsEmpty()
        {
            return item == null || quantity <= 0;
        }
    }
}
