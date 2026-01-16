using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Riftbourne.Items;
using Riftbourne.Characters;

namespace Riftbourne.UI
{
    /// <summary>
    /// UI representation of an equipment slot (paperdoll style).
    /// Displays currently equipped item and accepts drag-and-drop.
    /// Can also be dragged from to unequip items.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image slotBackgroundImage;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TextMeshProUGUI slotLabelText;
        [SerializeField] private GameObject emptyIndicator;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Slot Configuration")]
        [SerializeField] private EquipmentSlot equipmentSlot;

        [Header("Drag Settings")]
        [SerializeField] private float dragAlpha = 0.6f;
        [SerializeField] private GameObject dragGhostPrefab;

        private CharacterState currentCharacter;
        private EquipmentSlotsPanel parentPanel;
        private ItemDetailsPanel detailsPanel;
        private ItemGridUI itemGrid;
        private bool isDragging = false;
        private GameObject dragGhost;
        private Canvas rootCanvas;

        // Events
        public System.Action<EquipmentSlotUI, EquipmentItem> OnItemEquipped;

        public EquipmentSlot Slot => equipmentSlot;

        private void Awake()
        {
            if (slotLabelText != null)
            {
                slotLabelText.text = GetSlotDisplayName(equipmentSlot);
            }

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            rootCanvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// Initialize the slot with character and panel references.
        /// </summary>
        public void Initialize(CharacterState character, EquipmentSlotsPanel panel, ItemDetailsPanel detailsPanelRef, ItemGridUI grid = null)
        {
            currentCharacter = character;
            parentPanel = panel;
            detailsPanel = detailsPanelRef;
            itemGrid = grid;
            RefreshDisplay();
        }

        /// <summary>
        /// Refresh the display of this equipment slot.
        /// </summary>
        public void RefreshDisplay()
        {
            if (currentCharacter == null)
            {
                // Empty slot
                if (itemIconImage != null) itemIconImage.enabled = false;
                if (emptyIndicator != null) emptyIndicator.SetActive(true);
                return;
            }

            EquipmentItem equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);

            if (equippedItem != null)
            {
                // Show equipped item
                if (itemIconImage != null)
                {
                    itemIconImage.sprite = equippedItem.Icon;
                    itemIconImage.enabled = equippedItem.Icon != null;
                }
                if (emptyIndicator != null)
                {
                    emptyIndicator.SetActive(false);
                }
            }
            else
            {
                // Empty slot
                if (itemIconImage != null) itemIconImage.enabled = false;
                if (emptyIndicator != null) emptyIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// Handle item being dropped on this slot.
        /// </summary>
        public void HandleItemDrop(ItemSlotUI itemSlot)
        {
            if (itemSlot == null || itemSlot.Item == null || currentCharacter == null) return;

            EquipmentItem equipment = itemSlot.Item as EquipmentItem;
            if (equipment == null)
            {
                Debug.LogWarning($"Cannot equip {itemSlot.Item.ItemName} - not an equipment item!");
                return;
            }

            // Check if item can be equipped in this slot
            if (!equipment.CanEquipInSlot(equipmentSlot))
            {
                Debug.LogWarning($"{equipment.ItemName} cannot be equipped in {equipmentSlot} slot!");
                return;
            }

            // Remove from inventory
            if (currentCharacter.RemoveItem(equipment, 1))
            {
                // Unequip current item if any
                EquipmentItem currentlyEquipped = currentCharacter.GetEquippedItem(equipmentSlot);
                if (currentlyEquipped != null)
                {
                    currentCharacter.UnequipItem(equipmentSlot);
                    currentCharacter.AddItem(currentlyEquipped, 1);
                }

                // Equip new item
                currentCharacter.EquipItem(equipment, equipmentSlot);
                OnItemEquipped?.Invoke(this, equipment);

                Debug.Log($"Equipped {equipment.ItemName} to {equipmentSlot}");

                // Refresh parent panel
                if (parentPanel != null)
                {
                    parentPanel.RefreshAllSlots();
                }
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handle drops from inventory slots
            ItemSlotUI draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
            if (draggedSlot != null)
            {
                HandleItemDrop(draggedSlot);
                return;
            }

            // Also check parent in case the drag object is a child
            if (draggedSlot == null && eventData.pointerDrag != null)
            {
                draggedSlot = eventData.pointerDrag.GetComponentInParent<ItemSlotUI>();
                if (draggedSlot != null)
                {
                    HandleItemDrop(draggedSlot);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentCharacter == null || detailsPanel == null) return;

            // Update details panel on hover (panel is static, just updates content)
            EquipmentItem equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);
            if (equippedItem != null)
            {
                detailsPanel.ShowItemDetails(equippedItem);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Optionally clear details panel on exit, or keep last hovered item
            // For now, we'll keep the last hovered item visible
        }

        #region Drag and Drop (from equipment slot)

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (currentCharacter == null) return;

            EquipmentItem equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);
            if (equippedItem == null) return; // Can't drag empty slot

            isDragging = true;

            // Create drag ghost
            if (dragGhostPrefab != null && rootCanvas != null)
            {
                dragGhost = Instantiate(dragGhostPrefab, rootCanvas.transform);
                Image ghostImage = dragGhost.GetComponent<Image>();
                if (ghostImage != null && equippedItem.Icon != null)
                {
                    ghostImage.sprite = equippedItem.Icon;
                }
            }

            // Make original semi-transparent
            if (canvasGroup != null)
            {
                canvasGroup.alpha = dragAlpha;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Update drag ghost position
            if (dragGhost != null && rootCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    eventData.position,
                    rootCanvas.worldCamera,
                    out Vector2 localPoint);
                dragGhost.transform.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;

            // Restore original appearance
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // Destroy drag ghost
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }

            // Check for valid drop target - use RaycastAll to find what's under the pointer
            // (pointerCurrentRaycast may be null when blocksRaycasts is false)
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
            
            GameObject dropTarget = null;
            
            // Find the first valid drop target (skip the dragged equipment slot itself)
            foreach (var result in results)
            {
                if (result.gameObject != null && result.gameObject != gameObject)
                {
                    // Check if this is a valid drop target (inventory slot or grid area)
                    if (result.gameObject.GetComponent<ItemSlotUI>() != null ||
                        result.gameObject.GetComponent<ItemGridDropZone>() != null ||
                        result.gameObject.GetComponentInParent<ItemSlotUI>() != null ||
                        result.gameObject.GetComponentInParent<ItemGridDropZone>() != null ||
                        result.gameObject.GetComponentInParent<ItemGridUI>() != null)
                    {
                        dropTarget = result.gameObject;
                        break;
                    }
                }
            }

            if (dropTarget != null)
            {
                HandleDragToInventory(dropTarget);
            }
            else
            {
                Debug.Log("EquipmentSlotUI: No valid drop target found");
            }
        }

        /// <summary>
        /// Handle dragging equipped item to inventory slot.
        /// </summary>
        private void HandleDragToInventory(GameObject dropTarget)
        {
            if (currentCharacter == null) return;

            EquipmentItem equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);
            if (equippedItem == null) return;

            // Check if dropped on an inventory slot (check self and parents)
            ItemSlotUI targetSlot = dropTarget.GetComponent<ItemSlotUI>();
            if (targetSlot == null)
            {
                targetSlot = dropTarget.GetComponentInParent<ItemSlotUI>();
            }
            if (targetSlot != null)
            {
                // Unequip and add to inventory
                if (currentCharacter.UnequipItem(equipmentSlot))
                {
                    currentCharacter.AddItem(equippedItem, 1);
                    Debug.Log($"Unequipped {equippedItem.ItemName} from {equipmentSlot}");

                    // Refresh displays
                    if (parentPanel != null)
                    {
                        parentPanel.RefreshAllSlots();
                    }
                    if (itemGrid != null)
                    {
                        itemGrid.RefreshGrid();
                    }
                }
                return;
            }

            // Check if dropped on the inventory grid area (empty space or grid container)
            ItemGridDropZone gridDropZone = dropTarget.GetComponent<ItemGridDropZone>();
            if (gridDropZone == null)
            {
                gridDropZone = dropTarget.GetComponentInParent<ItemGridDropZone>();
            }
            if (gridDropZone != null)
            {
                // Unequip and add to inventory
                if (currentCharacter.UnequipItem(equipmentSlot))
                {
                    currentCharacter.AddItem(equippedItem, 1);
                    Debug.Log($"Unequipped {equippedItem.ItemName} from {equipmentSlot}");

                    // Refresh displays
                    if (parentPanel != null)
                    {
                        parentPanel.RefreshAllSlots();
                    }
                    if (itemGrid != null)
                    {
                        itemGrid.RefreshGrid();
                    }
                }
                return;
            }

            // Also check for ItemGridUI component as fallback
            ItemGridUI grid = dropTarget.GetComponentInParent<ItemGridUI>();
            if (grid != null)
            {
                // Unequip and add to inventory
                if (currentCharacter.UnequipItem(equipmentSlot))
                {
                    currentCharacter.AddItem(equippedItem, 1);
                    Debug.Log($"Unequipped {equippedItem.ItemName} from {equipmentSlot}");

                    // Refresh displays
                    if (parentPanel != null)
                    {
                        parentPanel.RefreshAllSlots();
                    }
                    if (itemGrid != null)
                    {
                        itemGrid.RefreshGrid();
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Get display name for equipment slot.
        /// </summary>
        private string GetSlotDisplayName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.MeleeWeapon => "Melee Weapon",
                EquipmentSlot.RangedWeapon => "Ranged Weapon",
                EquipmentSlot.Armor => "Armor",
                EquipmentSlot.Accessory1 => "Accessory 1",
                EquipmentSlot.Accessory2 => "Accessory 2",
                EquipmentSlot.Codex => "Codex",
                _ => slot.ToString()
            };
        }
    }
}
