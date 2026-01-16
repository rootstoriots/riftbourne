using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Items;
using Riftbourne.Inventory;

namespace Riftbourne.UI
{
    /// <summary>
    /// Manages the 8-column grid layout for displaying inventory items.
    /// Handles dynamic population and scrolling.
    /// </summary>
    public class ItemGridUI : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int columnsPerRow = 8;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private ScrollRect scrollRect;

        [Header("References")]
        [SerializeField] private ItemDetailsPanel detailsPanel;
        [SerializeField] private ItemContextMenu contextMenu;
        [SerializeField] private EquipmentSlotsPanel equipmentPanel;

        private List<ItemSlotUI> slotUIs = new List<ItemSlotUI>();
        private CharacterState currentCharacter;
        private Unit currentUnit;

        private void Awake()
        {
            if (gridContainer == null)
                gridContainer = transform;

            // Ensure GridLayoutGroup is present
            GridLayoutGroup layoutGroup = gridContainer.GetComponent<GridLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
                layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layoutGroup.constraintCount = columnsPerRow;
            }
            else
            {
                layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layoutGroup.constraintCount = columnsPerRow;
            }
        }

        /// <summary>
        /// Populate the grid with items from the character's inventory.
        /// </summary>
        public void PopulateGrid(CharacterState character, Unit unit = null)
        {
            currentCharacter = character;
            currentUnit = unit;

            // Clear existing slots
            ClearGrid();

            // Show empty state in details panel if no character
            if (character == null || character.Inventory == null)
            {
                if (detailsPanel != null)
                {
                    detailsPanel.ShowEmptyState();
                }
                return;
            }

            // Create slots only for items that exist
            foreach (var slot in character.Inventory)
            {
                if (slot != null && slot.Item != null)
                {
                    CreateItemSlot(slot);
                }
            }
        }

        /// <summary>
        /// Create a single item slot UI.
        /// </summary>
        private void CreateItemSlot(InventorySlot slot)
        {
            if (itemSlotPrefab == null || gridContainer == null)
            {
                Debug.LogError("ItemGridUI: itemSlotPrefab or gridContainer is null!");
                return;
            }

            GameObject slotObj = Instantiate(itemSlotPrefab, gridContainer);
            ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
            if (slotUI == null)
            {
                slotUI = slotObj.AddComponent<ItemSlotUI>();
            }

            slotUI.Initialize(slot, this, detailsPanel, contextMenu, equipmentPanel);
            slotUIs.Add(slotUI);
        }

        /// <summary>
        /// Clear all slots from the grid.
        /// </summary>
        private void ClearGrid()
        {
            foreach (var slotUI in slotUIs)
            {
                if (slotUI != null)
                {
                    Destroy(slotUI.gameObject);
                }
            }
            slotUIs.Clear();
        }

        /// <summary>
        /// Refresh the grid display (recreate slots).
        /// </summary>
        public void RefreshGrid()
        {
            PopulateGrid(currentCharacter, currentUnit);
        }

        /// <summary>
        /// Swap items between two slots (for reordering).
        /// </summary>
        public void SwapSlots(ItemSlotUI slot1, ItemSlotUI slot2)
        {
            if (slot1 == null || slot2 == null || currentCharacter == null) return;
            if (slot1.Slot == null || slot2.Slot == null) return;

            // Get indices in inventory
            int index1 = currentCharacter.Inventory.IndexOf(slot1.Slot);
            int index2 = currentCharacter.Inventory.IndexOf(slot2.Slot);

            if (index1 < 0 || index2 < 0) return;

            // Swap in inventory list
            InventorySlot temp = currentCharacter.Inventory[index1];
            currentCharacter.Inventory[index1] = currentCharacter.Inventory[index2];
            currentCharacter.Inventory[index2] = temp;

            // Refresh display
            RefreshGrid();
        }

        /// <summary>
        /// Move an item to a specific index (for drag-and-drop reordering).
        /// </summary>
        public void MoveSlotToIndex(ItemSlotUI slot, int targetIndex)
        {
            if (slot == null || currentCharacter == null || slot.Slot == null) return;

            int currentIndex = currentCharacter.Inventory.IndexOf(slot.Slot);
            if (currentIndex < 0) return;

            if (targetIndex < 0 || targetIndex >= currentCharacter.Inventory.Count)
                targetIndex = currentCharacter.Inventory.Count - 1;

            // Remove from current position
            currentCharacter.Inventory.RemoveAt(currentIndex);

            // Adjust target index if we removed before it
            if (currentIndex < targetIndex)
                targetIndex--;

            // Insert at new position
            currentCharacter.Inventory.Insert(targetIndex, slot.Slot);

            // Refresh display
            RefreshGrid();
        }

        /// <summary>
        /// Ensure an item is in the character's inventory.
        /// Adds it if it doesn't exist, or updates quantity if it does.
        /// </summary>
        public void EnsureItemInInventory(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0 || currentCharacter == null) return;

            // Check if item already exists in inventory
            bool itemExists = false;
            foreach (var slot in currentCharacter.Inventory)
            {
                if (slot != null && slot.Item == item)
                {
                    itemExists = true;
                    break;
                }
            }

            // Add item if it doesn't exist
            if (!itemExists)
            {
                currentCharacter.AddItem(item, quantity);
                Debug.Log($"ItemGridUI: Added {quantity}x {item.ItemName} to inventory");
            }
            // If it exists, the item is already in inventory (no action needed)
        }

        /// <summary>
        /// Set the equipment panel reference (for drag feedback).
        /// </summary>
        public void SetEquipmentPanel(EquipmentSlotsPanel panel)
        {
            equipmentPanel = panel;
            
            // Update all existing slots with the equipment panel reference
            foreach (var slotUI in slotUIs)
            {
                if (slotUI != null)
                {
                    // Re-initialize with equipment panel reference
                    slotUI.Initialize(slotUI.Slot, this, detailsPanel, contextMenu, equipmentPanel);
                }
            }
        }
    }
}
