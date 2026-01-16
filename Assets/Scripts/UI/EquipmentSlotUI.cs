using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
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
        [SerializeField] private Image dropIndicatorImage;

        [Header("Slot Configuration")]
        [SerializeField] private EquipmentSlot equipmentSlot;

        [Header("Drag Settings")]
        [SerializeField] private float dragAlpha = 0.6f;
        [SerializeField] private GameObject dragGhostPrefab;

        [Header("Drop Indicator Pulse Settings")]
        [Tooltip("Pulses per second (higher = faster)")]
        [SerializeField] private float pulseSpeed = 2f;
        [Tooltip("Minimum scale during pulse (0.9 = 90% size)")]
        [SerializeField] private float minScale = 0.9f;
        [Tooltip("Maximum scale during pulse (1.1 = 110% size)")]
        [SerializeField] private float maxScale = 1.1f;
        [Tooltip("Minimum alpha during pulse (0.5 = 50% transparent)")]
        [SerializeField] private float minAlpha = 0.5f;
        [Tooltip("Maximum alpha during pulse (1.0 = fully opaque)")]
        [SerializeField] private float maxAlpha = 1f;
        [Tooltip("Minimum brightness multiplier (0.7 = 70% brightness)")]
        [SerializeField] private float minBrightness = 0.7f;
        [Tooltip("Maximum brightness multiplier (1.2 = 120% brightness, creates glow)")]
        [SerializeField] private float maxBrightness = 1.2f;

        private CharacterState currentCharacter;
        private EquipmentSlotsPanel parentPanel;
        private ItemDetailsPanel detailsPanel;
        private ItemGridUI itemGrid;
        private InventoryUI inventoryUI;
        private bool isDragging = false;
        private GameObject dragGhost;
        private Canvas rootCanvas;
        
        // Pulsing animation
        private Coroutine pulseCoroutine;
        private bool isPulsing = false;

        // Events
        public System.Action<EquipmentSlotUI, EquipmentItem> OnItemEquipped;

        public EquipmentSlot Slot => equipmentSlot;

        /// <summary>
        /// Show pulsing drop indicator for this slot.
        /// </summary>
        public void ShowDropIndicator()
        {
            if (dropIndicatorImage == null)
            {
                Debug.LogWarning($"EquipmentSlotUI: Cannot show drop indicator - DropIndicatorImage is null! Slot: {equipmentSlot}");
                return;
            }
            
            if (!isPulsing)
            {
                dropIndicatorImage.enabled = true;
                isPulsing = true;
                
                // Ensure we have a valid MonoBehaviour to run coroutine
                if (this != null && gameObject.activeInHierarchy)
                {
                    pulseCoroutine = StartCoroutine(PulseAnimation());
                    Debug.Log($"EquipmentSlotUI: Started pulse animation for {equipmentSlot}");
                }
                else
                {
                    Debug.LogWarning($"EquipmentSlotUI: Cannot start coroutine - GameObject not active! Slot: {equipmentSlot}");
                    isPulsing = false;
                    dropIndicatorImage.enabled = false;
                }
            }
        }

        /// <summary>
        /// Hide pulsing drop indicator for this slot.
        /// </summary>
        public void HideDropIndicator()
        {
            if (dropIndicatorImage == null) return;
            
            if (isPulsing)
            {
                isPulsing = false;
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }
                dropIndicatorImage.enabled = false;
                
                // Reset scale and alpha
                if (dropIndicatorImage != null)
                {
                    dropIndicatorImage.transform.localScale = Vector3.one;
                    Color color = dropIndicatorImage.color;
                    color.a = 1f;
                    dropIndicatorImage.color = color;
                }
            }
        }

        /// <summary>
        /// Pulsing animation coroutine.
        /// Creates a smooth pulsing/glowing effect using scale and alpha animations.
        /// All parameters are configurable in the Inspector.
        /// </summary>
        private IEnumerator PulseAnimation()
        {
            if (dropIndicatorImage == null)
            {
                Debug.LogWarning($"EquipmentSlotUI: DropIndicatorImage is null! Cannot pulse. Slot: {equipmentSlot}");
                yield break;
            }

            // Store base color RGB values (preserve original color, ignore alpha)
            Color originalColor = dropIndicatorImage.color;
            float baseR = originalColor.r;
            float baseG = originalColor.g;
            float baseB = originalColor.b;

            // Initialize starting values
            float startTime = Time.unscaledTime; // Use unscaled time to work even if game is paused
            float initialScale = Mathf.Lerp(minScale, maxScale, 0.5f); // Start at midpoint
            dropIndicatorImage.transform.localScale = Vector3.one * initialScale;
            
            Debug.Log($"EquipmentSlotUI: Pulse animation started for {equipmentSlot}. Base color: R={baseR:F2}, G={baseG:F2}, B={baseB:F2}");

            int frameCount = 0;
            while (isPulsing && dropIndicatorImage != null && dropIndicatorImage.enabled)
            {
                // Calculate time elapsed since animation started
                float elapsedTime = (Time.unscaledTime - startTime) * pulseSpeed;
                
                // Create smooth sine wave from 0 to 1
                float pulse = (Mathf.Sin(elapsedTime * Mathf.PI * 2f) + 1f) * 0.5f;

                // Scale animation (pulsing size)
                float scale = Mathf.Lerp(minScale, maxScale, pulse);
                dropIndicatorImage.transform.localScale = Vector3.one * scale;

                // Alpha animation (fading in/out)
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);

                // Brightness/glow animation (makes it "glow")
                float brightness = Mathf.Lerp(minBrightness, maxBrightness, pulse);

                // Apply all changes to color
                Color color = new Color(
                    Mathf.Clamp01(baseR * brightness),
                    Mathf.Clamp01(baseG * brightness),
                    Mathf.Clamp01(baseB * brightness),
                    alpha
                );

                dropIndicatorImage.color = color;

                // Debug output every 60 frames (~1 second at 60fps)
                frameCount++;
                if (frameCount % 60 == 0)
                {
                    Debug.Log($"EquipmentSlotUI: Pulse animating - elapsed={elapsedTime:F2}s, pulse={pulse:F2}, scale={scale:F2}, alpha={alpha:F2}, brightness={brightness:F2}");
                }

                yield return null;
            }
            
            Debug.Log($"EquipmentSlotUI: Pulse animation ended for {equipmentSlot}");
        }

        private void Awake()
        {
            if (slotLabelText != null)
            {
                slotLabelText.text = GetSlotDisplayName(equipmentSlot);
                // Hide label by default - will show on hover if slot is empty
                slotLabelText.enabled = false;
            }

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            rootCanvas = GetComponentInParent<Canvas>();
            
            // Initialize drop indicator
            if (dropIndicatorImage != null)
            {
                dropIndicatorImage.enabled = false;
            }
        }

        /// <summary>
        /// Initialize the slot with character and panel references.
        /// </summary>
        public void Initialize(CharacterState character, EquipmentSlotsPanel panel, ItemDetailsPanel detailsPanelRef, ItemGridUI grid = null, InventoryUI inventoryUIRef = null)
        {
            currentCharacter = character;
            parentPanel = panel;
            detailsPanel = detailsPanelRef;
            itemGrid = grid;
            inventoryUI = inventoryUIRef;
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
                // Hide label (will show on hover)
                if (slotLabelText != null) slotLabelText.enabled = false;
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
                // Hide label when item is equipped
                if (slotLabelText != null) slotLabelText.enabled = false;
            }
            else
            {
                // Empty slot
                if (itemIconImage != null) itemIconImage.enabled = false;
                if (emptyIndicator != null) emptyIndicator.SetActive(true);
                // Hide label by default (will show on hover)
                if (slotLabelText != null) slotLabelText.enabled = false;
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

                // Refresh weight text in inventory UI
                if (inventoryUI != null)
                {
                    inventoryUI.RefreshWeightText();
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
            if (currentCharacter == null) return;

            // Update details panel on hover (panel is static, just updates content)
            EquipmentItem equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);
            if (equippedItem != null && detailsPanel != null)
            {
                detailsPanel.ShowItemDetails(equippedItem);
            }

            // Show slot label if slot is empty
            if (equippedItem == null && slotLabelText != null)
            {
                slotLabelText.enabled = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Hide slot label when cursor leaves
            if (slotLabelText != null)
            {
                slotLabelText.enabled = false;
            }
            
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

                    // Refresh weight text in inventory UI
                    if (inventoryUI != null)
                    {
                        inventoryUI.RefreshWeightText();
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

                    // Refresh weight text in inventory UI
                    if (inventoryUI != null)
                    {
                        inventoryUI.RefreshWeightText();
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

                    // Refresh weight text in inventory UI
                    if (inventoryUI != null)
                    {
                        inventoryUI.RefreshWeightText();
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
