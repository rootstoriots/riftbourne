using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Riftbourne.Items;
using Riftbourne.Characters;

namespace Riftbourne.UI
{
    /// <summary>
    /// UI for selecting which equipment slot to equip an item to.
    /// Used when an item can be equipped in multiple slots (e.g., accessories).
    /// </summary>
    public class EquipmentSlotSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Transform slotButtonContainer;
        [SerializeField] private GameObject slotButtonPrefab;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private Button cancelButton;

        private System.Action<EquipmentSlot> onSlotSelected;
        private List<Button> slotButtons = new List<Button>();

        private void Awake()
        {
            if (selectionPanel == null)
                selectionPanel = gameObject;

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HideSelection);
            }

            HideSelection();
        }

        /// <summary>
        /// Show slot selection UI for the equipment item.
        /// </summary>
        public void ShowSlotSelection(EquipmentItem equipment, CharacterState character, System.Action<EquipmentSlot> onSelected)
        {
            if (equipment == null || equipment.CompatibleSlots == null || equipment.CompatibleSlots.Count == 0)
            {
                Debug.LogWarning("EquipmentSlotSelectionUI: Equipment has no compatible slots!");
                return;
            }

            onSlotSelected = onSelected;

            // Clear existing buttons
            ClearSlotButtons();

            // Set prompt
            if (promptText != null)
            {
                promptText.text = $"Select slot to equip {equipment.ItemName}:";
            }

            // Create buttons for each compatible slot
            foreach (var slot in equipment.CompatibleSlots)
            {
                CreateSlotButton(slot, character);
            }

            selectionPanel.SetActive(true);
        }

        /// <summary>
        /// Create a button for an equipment slot.
        /// </summary>
        private void CreateSlotButton(EquipmentSlot slot, CharacterState character)
        {
            if (slotButtonPrefab == null || slotButtonContainer == null) return;

            GameObject buttonObj = Instantiate(slotButtonPrefab, slotButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
                button = buttonObj.AddComponent<Button>();

            // Set label
            TextMeshProUGUI labelText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                string slotName = GetSlotDisplayName(slot);
                EquipmentItem currentlyEquipped = character?.GetEquippedItem(slot);
                if (currentlyEquipped != null)
                {
                    labelText.text = $"{slotName} (Currently: {currentlyEquipped.ItemName})";
                }
                else
                {
                    labelText.text = $"{slotName} (Empty)";
                }
            }

            // Set click handler
            button.onClick.AddListener(() =>
            {
                onSlotSelected?.Invoke(slot);
                HideSelection();
            });

            slotButtons.Add(button);
        }

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

        /// <summary>
        /// Clear all slot buttons.
        /// </summary>
        private void ClearSlotButtons()
        {
            foreach (var button in slotButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            slotButtons.Clear();
        }

        /// <summary>
        /// Hide the selection UI.
        /// </summary>
        public void HideSelection()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
            onSlotSelected = null;
        }
    }
}
