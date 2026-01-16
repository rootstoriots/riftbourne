using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Riftbourne.Items;
using Riftbourne.Inventory;

namespace Riftbourne.UI
{
    /// <summary>
    /// Individual item slot in the inventory grid.
    /// Displays item icon with button overlay, handles hover, right-click, and drag-and-drop.
    /// </summary>
    public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image buttonOverlayImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Drag Settings")]
        [SerializeField] private float dragAlpha = 0.6f;
        [SerializeField] private GameObject dragGhostPrefab;

        private InventorySlot inventorySlot;
        private ItemGridUI parentGrid;
        private ItemDetailsPanel detailsPanel;
        private ItemContextMenu contextMenu;
        private bool isDragging = false;
        private GameObject dragGhost;
        private Canvas rootCanvas;

        // Events
        public System.Action<ItemSlotUI> OnSlotClicked;
        public System.Action<ItemSlotUI> OnSlotHovered;
        public System.Action<ItemSlotUI> OnSlotUnhovered;

        public InventorySlot Slot => inventorySlot;
        public Item Item => inventorySlot?.Item;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            rootCanvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// Initialize the slot with inventory data.
        /// </summary>
        public void Initialize(InventorySlot slot, ItemGridUI grid, ItemDetailsPanel detailsPanelRef, ItemContextMenu contextMenuRef)
        {
            inventorySlot = slot;
            parentGrid = grid;
            detailsPanel = detailsPanelRef;
            contextMenu = contextMenuRef;

            RefreshDisplay();
        }

        /// <summary>
        /// Refresh the visual display of this slot.
        /// </summary>
        public void RefreshDisplay()
        {
            if (inventorySlot == null || inventorySlot.Item == null)
            {
                // Empty slot - hide everything
                if (iconImage != null) iconImage.enabled = false;
                if (buttonOverlayImage != null) buttonOverlayImage.enabled = false;
                if (quantityText != null) quantityText.enabled = false;
                return;
            }

            Item item = inventorySlot.Item;

            // Display icon
            if (iconImage != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.enabled = item.Icon != null;
            }

            // Display quantity if stackable
            if (quantityText != null)
            {
                if (inventorySlot.Quantity > 1)
                {
                    quantityText.text = inventorySlot.Quantity.ToString();
                    quantityText.enabled = true;
                }
                else
                {
                    quantityText.enabled = false;
                }
            }

            // Apply button style and rarity tinting
            ApplyButtonStyle(item);
        }

        /// <summary>
        /// Apply button style based on item type and rarity.
        /// </summary>
        private void ApplyButtonStyle(Item item)
        {
            if (buttonOverlayImage == null) return;

            // Get style from manager
            ItemButtonStyleManager styleManager = ItemButtonStyleManager.Instance;
            if (styleManager != null)
            {
                ItemButtonStyle style = styleManager.GetStyleForItemType(item.ItemType);
                if (style != null)
                {
                    buttonOverlayImage.sprite = style.ButtonSprite;
                    buttonOverlayImage.enabled = style.ButtonSprite != null;

                    // Apply rarity-based color tinting
                    Color baseColor = style.BaseColor;
                    Color rarityTint = GetRarityColor(item.Rarity);
                    buttonOverlayImage.color = baseColor * rarityTint;
                }
                else
                {
                    // Default style
                    buttonOverlayImage.enabled = false;
                }
            }
            else
            {
                // Fallback: simple rarity color
                buttonOverlayImage.color = GetRarityColor(item.Rarity);
                buttonOverlayImage.enabled = true;
            }
        }

        /// <summary>
        /// Get color for item rarity (with faint hue).
        /// </summary>
        private Color GetRarityColor(ItemRarity rarity)
        {
            Color baseColor = Color.white;
            float tintStrength = 0.3f; // Faint hue

            switch (rarity)
            {
                case ItemRarity.Common:
                    baseColor = Color.white;
                    break;
                case ItemRarity.Uncommon:
                    baseColor = Color.Lerp(Color.white, Color.green, tintStrength);
                    break;
                case ItemRarity.Rare:
                    baseColor = Color.Lerp(Color.white, Color.blue, tintStrength);
                    break;
                case ItemRarity.Epic:
                    baseColor = Color.Lerp(Color.white, new Color(0.64f, 0.21f, 0.93f), tintStrength); // Purple
                    break;
                case ItemRarity.Legendary:
                    baseColor = Color.Lerp(Color.white, new Color(1f, 0.5f, 0f), tintStrength); // Orange
                    break;
            }

            return baseColor;
        }

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isDragging) return;

            OnSlotHovered?.Invoke(this);
            
            // Update details panel on hover (panel is static, just updates content)
            if (inventorySlot != null && inventorySlot.Item != null && detailsPanel != null)
            {
                detailsPanel.ShowItemDetails(inventorySlot.Item);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDragging) return;

            OnSlotUnhovered?.Invoke(this);
            
            // Optionally clear details panel on exit, or keep last hovered item
            // For now, we'll keep the last hovered item visible
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;

            // Right-click for context menu
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (inventorySlot != null && inventorySlot.Item != null && contextMenu != null)
                {
                    contextMenu.ShowContextMenu(inventorySlot, transform.position);
                }
            }
            // Left-click
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnSlotClicked?.Invoke(this);
                
                // Update details panel with clicked item (in case hover was missed)
                if (inventorySlot != null && inventorySlot.Item != null && detailsPanel != null)
                {
                    detailsPanel.ShowItemDetails(inventorySlot.Item);
                }
            }
        }

        #endregion

        #region Drag and Drop

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (inventorySlot == null || inventorySlot.Item == null) return;

            isDragging = true;

            // Create drag ghost
            if (dragGhostPrefab != null && rootCanvas != null)
            {
                dragGhost = Instantiate(dragGhostPrefab, rootCanvas.transform);
                Image ghostImage = dragGhost.GetComponent<Image>();
                if (ghostImage != null && inventorySlot.Item.Icon != null)
                {
                    ghostImage.sprite = inventorySlot.Item.Icon;
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

            // Check for valid drop - use RaycastAll to find what's under the pointer
            // (pointerCurrentRaycast may be null when blocksRaycasts is false)
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
            
            GameObject dropTarget = null;
            
            // Find the first valid drop target (skip the dragged item itself)
            // Priority: ItemSlotUI (for swapping) > EquipmentSlotUI > ItemGridDropZone (for adding to inventory)
            foreach (var result in results)
            {
                if (result.gameObject != null && result.gameObject != gameObject)
                {
                    // Priority 1: Check for ItemSlotUI (for reordering/swapping)
                    ItemSlotUI targetSlot = result.gameObject.GetComponent<ItemSlotUI>();
                    if (targetSlot == null)
                        targetSlot = result.gameObject.GetComponentInParent<ItemSlotUI>();
                    if (targetSlot != null && targetSlot != this)
                    {
                        dropTarget = result.gameObject;
                        break;
                    }

                    // Priority 2: Check for EquipmentSlotUI (for equipping)
                    EquipmentSlotUI equipmentSlot = result.gameObject.GetComponent<EquipmentSlotUI>();
                    if (equipmentSlot == null)
                        equipmentSlot = result.gameObject.GetComponentInParent<EquipmentSlotUI>();
                    if (equipmentSlot != null)
                    {
                        dropTarget = result.gameObject;
                        break;
                    }

                    // Priority 3: Check for ItemGridDropZone (for adding to inventory)
                    ItemGridDropZone gridDropZone = result.gameObject.GetComponent<ItemGridDropZone>();
                    if (gridDropZone == null)
                        gridDropZone = result.gameObject.GetComponentInParent<ItemGridDropZone>();
                    if (gridDropZone != null)
                    {
                        dropTarget = result.gameObject;
                        break;
                    }
                }
            }

            if (dropTarget != null)
            {
                HandleDrop(dropTarget);
            }
            else
            {
                Debug.Log("ItemSlotUI: No valid drop target found");
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handle being dropped on
            ItemSlotUI draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
            if (draggedSlot != null && draggedSlot != this)
            {
                HandleItemSwap(draggedSlot);
            }
        }

        /// <summary>
        /// Handle dropping item on a target.
        /// </summary>
        private void HandleDrop(GameObject dropTarget)
        {
            if (dropTarget == null) return;

            // Check for equipment slot (check self and parents)
            EquipmentSlotUI equipmentSlot = dropTarget.GetComponent<EquipmentSlotUI>();
            if (equipmentSlot == null)
            {
                equipmentSlot = dropTarget.GetComponentInParent<EquipmentSlotUI>();
            }
            if (equipmentSlot != null)
            {
                equipmentSlot.HandleItemDrop(this);
                // Refresh grid after equip
                if (parentGrid != null)
                {
                    parentGrid.RefreshGrid();
                }
                return;
            }

            // Check for another item slot (reordering)
            ItemSlotUI targetSlot = dropTarget.GetComponent<ItemSlotUI>();
            if (targetSlot == null)
            {
                targetSlot = dropTarget.GetComponentInParent<ItemSlotUI>();
            }
            if (targetSlot != null && targetSlot != this)
            {
                HandleItemSwap(targetSlot);
                return;
            }

            // Check if dropped on inventory grid container/content (empty space)
            ItemGridDropZone gridDropZone = dropTarget.GetComponent<ItemGridDropZone>();
            if (gridDropZone == null)
            {
                gridDropZone = dropTarget.GetComponentInParent<ItemGridDropZone>();
            }
            if (gridDropZone != null && inventorySlot != null && inventorySlot.Item != null)
            {
                // Dropped on grid area - ensure item is in inventory and refresh grid
                if (parentGrid != null)
                {
                    parentGrid.EnsureItemInInventory(inventorySlot.Item, inventorySlot.Quantity);
                    parentGrid.RefreshGrid();
                }
                return;
            }

            // Check for character portrait (send to character)
            // This would be handled by a character portrait component
        }

        /// <summary>
        /// Handle swapping items between slots.
        /// </summary>
        private void HandleItemSwap(ItemSlotUI otherSlot)
        {
            if (parentGrid != null)
            {
                parentGrid.SwapSlots(this, otherSlot);
            }
        }

        #endregion
    }
}
