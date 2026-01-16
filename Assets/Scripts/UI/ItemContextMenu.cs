using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Riftbourne.Items;
using Riftbourne.Inventory;
using Riftbourne.Characters;
using Riftbourne.Core;
using Riftbourne.Combat;

namespace Riftbourne.UI
{
    /// <summary>
    /// Context menu that appears on right-clicking an item.
    /// Provides actions: Examine, Send to Character, Discard, Equip, Send to Treasury.
    /// </summary>
    public class ItemContextMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform menuItemContainer;
        [SerializeField] private GameObject menuItemPrefab;

        [Header("References")]
        [SerializeField] private ItemExamineUI examineUI;
        [SerializeField] private EquipmentSlotSelectionUI equipmentSlotSelectionUI;

        private InventorySlot currentSlot;
        private CharacterState currentCharacter;
        private List<Button> menuButtons = new List<Button>();

        private void Awake()
        {
            if (menuPanel == null)
                menuPanel = gameObject;

            HideMenu();
        }

        private void Update()
        {
            // Close menu on outside click
            if (menuPanel.activeSelf && Input.GetMouseButtonDown(0))
            {
                // Check if click is outside menu
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                    menuPanel.GetComponent<RectTransform>(),
                    Input.mousePosition,
                    null))
                {
                    HideMenu();
                }
            }
        }

        /// <summary>
        /// Show the context menu for the specified item slot.
        /// </summary>
        public void ShowContextMenu(InventorySlot slot, Vector3 position)
        {
            if (slot == null || slot.Item == null) return;

            currentSlot = slot;

            // Get current character from PartyManager
            if (PartyManager.Instance != null)
            {
                currentCharacter = PartyManager.Instance.POVCharacter;
                if (currentCharacter == null)
                {
                    var partyMembers = PartyManager.Instance.GetPartyMembers();
                    if (partyMembers != null && partyMembers.Count > 0)
                    {
                        currentCharacter = partyMembers[0];
                    }
                }
            }

            // Position menu
            PositionMenu(position);

            // Clear existing menu items
            ClearMenuItems();

            // Build menu items based on item type and context
            BuildMenuItems();

            // Show menu
            menuPanel.SetActive(true);
        }

        /// <summary>
        /// Position the menu near the specified position.
        /// </summary>
        private void PositionMenu(Vector3 worldPosition)
        {
            RectTransform rectTransform = menuPanel.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;

            // Convert world position to canvas position
            Vector2 canvasPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, worldPosition),
                rootCanvas.worldCamera,
                out canvasPosition);

            rectTransform.localPosition = canvasPosition;
        }

        /// <summary>
        /// Build menu items based on item properties.
        /// </summary>
        private void BuildMenuItems()
        {
            if (currentSlot == null || currentSlot.Item == null || menuItemPrefab == null) return;

            Item item = currentSlot.Item;

            // Always show Examine
            CreateMenuItem("Examine", () => OnExamineClicked());

            // Use (for non-battle consumables)
            if (item.ItemType == ItemType.ConsumableNonBattle && item is ConsumableItem consumable)
            {
                CreateMenuItem("Use", () => OnUseClicked());
            }

            // Send to Character (if multiple party members)
            if (PartyManager.Instance != null)
            {
                var partyMembers = PartyManager.Instance.GetPartyMembers();
                if (partyMembers != null && partyMembers.Count > 1)
                {
                    // Create submenu for each party member
                    foreach (var member in partyMembers)
                    {
                        if (member != currentCharacter && member.Definition != null)
                        {
                            string memberName = member.Definition.CharacterName;
                            CreateMenuItem($"Send to {memberName}", () => OnSendToCharacterClicked(member));
                        }
                    }
                }
            }

            // Discard (if item can be discarded)
            if (item is KeyItem keyItem)
            {
                if (!keyItem.CanDrop())
                {
                    // Key items that can't be dropped shouldn't show discard
                }
                else
                {
                    CreateMenuItem("Discard", () => OnDiscardClicked());
                }
            }
            else
            {
                CreateMenuItem("Discard", () => OnDiscardClicked());
            }

            // Equip (if equipment)
            if (item is EquipmentItem equipment)
            {
                if (equipment.CompatibleSlots != null && equipment.CompatibleSlots.Count > 0)
                {
                    if (equipment.CompatibleSlots.Count == 1)
                    {
                        // Single slot - direct equip
                        EquipmentSlot slot = equipment.CompatibleSlots[0];
                        CreateMenuItem($"Equip ({slot})", () => OnEquipClicked(slot));
                    }
                    else
                    {
                        // Multiple slots - show selection
                        CreateMenuItem("Equip...", () => OnEquipWithSelectionClicked());
                    }
                }
            }

            // Send to Treasury
            CreateMenuItem("Send to Treasury", () => OnSendToTreasuryClicked());
        }

        /// <summary>
        /// Create a menu item button.
        /// </summary>
        private void CreateMenuItem(string label, System.Action onClick)
        {
            if (menuItemPrefab == null || menuItemContainer == null) return;

            GameObject menuItemObj = Instantiate(menuItemPrefab, menuItemContainer);
            Button button = menuItemObj.GetComponent<Button>();
            if (button == null)
                button = menuItemObj.AddComponent<Button>();

            // Set label
            TextMeshProUGUI labelText = menuItemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.text = label;
            }

            // Set click handler
            button.onClick.AddListener(() =>
            {
                onClick?.Invoke();
                HideMenu();
            });

            menuButtons.Add(button);
        }

        /// <summary>
        /// Clear all menu items.
        /// </summary>
        private void ClearMenuItems()
        {
            foreach (var button in menuButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            menuButtons.Clear();
        }

        /// <summary>
        /// Hide the context menu.
        /// </summary>
        public void HideMenu()
        {
            menuPanel.SetActive(false);
            currentSlot = null;
        }

        #region Menu Actions

        private void OnExamineClicked()
        {
            if (currentSlot == null || currentSlot.Item == null) return;

            if (examineUI != null)
            {
                examineUI.ShowExamine(currentSlot.Item);
            }
            else
            {
                // Fallback: show tooltip text
                Debug.Log($"Examining: {currentSlot.Item.ItemName}\n{currentSlot.Item.Description}");
            }
        }

        private void OnSendToCharacterClicked(CharacterState targetCharacter)
        {
            if (currentSlot == null || currentSlot.Item == null || currentCharacter == null || targetCharacter == null) return;

            // Remove from current character
            Item item = currentSlot.Item;
            int quantity = currentSlot.Quantity;

            if (currentCharacter.RemoveItem(item, quantity))
            {
                // Add to target character
                targetCharacter.AddItem(item, quantity);
                Debug.Log($"Sent {quantity}x {item.ItemName} to {targetCharacter.Definition.CharacterName}");
            }
        }

        private void OnDiscardClicked()
        {
            if (currentSlot == null || currentSlot.Item == null || currentCharacter == null) return;

            // Show confirmation dialog (simplified for now)
            Item item = currentSlot.Item;
            int quantity = currentSlot.Quantity;

            // Check if item can be dropped
            if (item is KeyItem keyItem && !keyItem.CanDrop())
            {
                Debug.LogWarning($"Cannot discard {item.ItemName} - it's a key item!");
                return;
            }

            // Remove from inventory
            if (currentCharacter.RemoveItem(item, quantity))
            {
                Debug.Log($"Discarded {quantity}x {item.ItemName}");
            }
        }

        private void OnEquipClicked(EquipmentSlot slot)
        {
            if (currentSlot == null || currentSlot.Item == null || currentCharacter == null) return;

            EquipmentItem equipment = currentSlot.Item as EquipmentItem;
            if (equipment == null) return;

            // Remove from inventory
            if (currentCharacter.RemoveItem(equipment, 1))
            {
                // Equip item
                EquipmentItem currentlyEquipped = currentCharacter.GetEquippedItem(slot);
                if (currentlyEquipped != null)
                {
                    // Unequip current item and add back to inventory
                    currentCharacter.UnequipItem(slot);
                    currentCharacter.AddItem(currentlyEquipped, 1);
                }

                currentCharacter.EquipItem(equipment, slot);
                Debug.Log($"Equipped {equipment.ItemName} to {slot}");
            }
        }

        private void OnEquipWithSelectionClicked()
        {
            if (currentSlot == null || currentSlot.Item == null) return;

            EquipmentItem equipment = currentSlot.Item as EquipmentItem;
            if (equipment == null || equipment.CompatibleSlots == null) return;

            if (equipmentSlotSelectionUI != null)
            {
                equipmentSlotSelectionUI.ShowSlotSelection(equipment, currentCharacter, (selectedSlot) =>
                {
                    OnEquipClicked(selectedSlot);
                });
            }
            else
            {
                // Fallback: equip to first compatible slot
                if (equipment.CompatibleSlots.Count > 0)
                {
                    OnEquipClicked(equipment.CompatibleSlots[0]);
                }
            }
        }

        private void OnSendToTreasuryClicked()
        {
            if (currentSlot == null || currentSlot.Item == null || currentCharacter == null) return;

            Item item = currentSlot.Item;
            int quantity = currentSlot.Quantity;

            if (TreasuryManager.Instance != null)
            {
                if (currentCharacter.RemoveItem(item, quantity))
                {
                    TreasuryManager.Instance.AddItem(item, quantity);
                    Debug.Log($"Sent {quantity}x {item.ItemName} to Treasury");
                }
            }
            else
            {
                Debug.LogWarning("TreasuryManager not found! Cannot send item to treasury.");
            }
        }

        private void OnUseClicked()
        {
            if (currentSlot == null || currentSlot.Item == null || currentCharacter == null) return;

            ConsumableItem consumable = currentSlot.Item as ConsumableItem;
            if (consumable == null) return;

            // Verify it's a non-battle consumable
            if (consumable.ItemType != ItemType.ConsumableNonBattle)
            {
                Debug.LogWarning($"Cannot use {consumable.ItemName} - not a non-battle consumable!");
                return;
            }

            // Verify it can be used outside combat
            if (!consumable.UsableOutOfCombat)
            {
                Debug.LogWarning($"Cannot use {consumable.ItemName} outside of combat!");
                return;
            }

            // Apply effects to the character
            bool success = ApplyConsumableEffects(consumable, currentCharacter);

            if (success)
            {
                // Remove one from inventory
                currentCharacter.RemoveItem(consumable, 1);
                Debug.Log($"{currentCharacter.Definition.CharacterName} used {consumable.ItemName}");
            }
        }

        /// <summary>
        /// Apply consumable effects to a character (outside combat).
        /// </summary>
        private bool ApplyConsumableEffects(ConsumableItem consumable, CharacterState character)
        {
            if (consumable == null || character == null || consumable.Effects == null) return false;

            bool anyEffectApplied = false;

            foreach (var effect in consumable.Effects)
            {
                switch (effect.effectType)
                {
                    case ConsumableEffectType.Heal:
                        // Heal HP using CharacterState's Heal method
                        int healAmount = effect.magnitude;
                        int actualHealing = character.Heal(healAmount);
                        anyEffectApplied = true;
                        Debug.Log($"{character.Definition.CharacterName} healed {actualHealing} HP (now {character.CurrentHP}/{character.MaxHP})");
                        break;

                    case ConsumableEffectType.BuffStat:
                        // Note: CharacterState doesn't have temporary stat buffs yet
                        // This would need to be implemented if you want stat buffs outside combat
                        Debug.LogWarning($"Stat buffs outside combat not yet implemented for {effect.affectedStat}");
                        // Could be implemented later with a buff system
                        break;

                    case ConsumableEffectType.RemoveStatusEffect:
                        // Note: Status effect removal would need to be implemented
                        Debug.LogWarning("Status effect removal outside combat not yet implemented");
                        break;

                    // Damage and debuff effects don't make sense outside combat
                    case ConsumableEffectType.Damage:
                    case ConsumableEffectType.DebuffStat:
                    case ConsumableEffectType.ApplyStatusEffect:
                        Debug.LogWarning($"Effect type {effect.effectType} not applicable outside combat");
                        break;

                    default:
                        Debug.LogWarning($"Unknown effect type: {effect.effectType}");
                        break;
                }
            }

            return anyEffectApplied;
        }

        #endregion
    }
}
