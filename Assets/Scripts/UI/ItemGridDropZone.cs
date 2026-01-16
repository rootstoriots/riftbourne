using UnityEngine;
using UnityEngine.EventSystems;

namespace Riftbourne.UI
{
    /// <summary>
    /// Drop zone component for the inventory grid container.
    /// Allows items to be dropped on empty grid space.
    /// Attach this to the Content GameObject (with GridLayoutGroup) or the grid container.
    /// </summary>
    public class ItemGridDropZone : MonoBehaviour, IDropHandler
    {
        private ItemGridUI parentGrid;

        private void Awake()
        {
            parentGrid = GetComponentInParent<ItemGridUI>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handle drops from equipment slots
            EquipmentSlotUI draggedEquipmentSlot = eventData.pointerDrag?.GetComponent<EquipmentSlotUI>();
            if (draggedEquipmentSlot != null)
            {
                // The EquipmentSlotUI will handle the unequip logic
                // This just confirms the drop was accepted
                return;
            }

            // Handle drops from item slots
            ItemSlotUI draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
            if (draggedSlot != null)
            {
                // ItemSlotUI handles its own drop logic (ensures item is in inventory)
                // This just confirms the drop zone accepts drops
                // The ItemSlotUI.HandleDrop will be called and will ensure the item is in inventory
                return;
            }

            // Handle drops from other sources (treasury, loot, etc.)
            // This could be extended in the future for external item sources
        }
    }
}
