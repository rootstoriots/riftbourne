using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Items;

namespace Riftbourne.UI
{
    /// <summary>
    /// Panel displaying all equipment slots in a paperdoll style.
    /// Shows currently equipped items and accepts drag-and-drop.
    /// </summary>
    public class EquipmentSlotsPanel : MonoBehaviour
    {
        [Header("Slot References")]
        [SerializeField] private EquipmentSlotUI meleeWeaponSlot;
        [SerializeField] private EquipmentSlotUI rangedWeaponSlot;
        [SerializeField] private EquipmentSlotUI armorSlot;
        [SerializeField] private EquipmentSlotUI accessory1Slot;
        [SerializeField] private EquipmentSlotUI accessory2Slot;
        [SerializeField] private EquipmentSlotUI codexSlot;

        [Header("References")]
        [SerializeField] private ItemDetailsPanel detailsPanel;
        [SerializeField] private ItemGridUI itemGrid;
        
        [Header("Character Portrait")]
        [Tooltip("Image component displaying the character's full body portrait behind equipment slots. Should preserve aspect ratio with fixed height.")]
        [SerializeField] private Image characterPortraitImage;

        private CharacterState currentCharacter;
        private Dictionary<EquipmentSlot, EquipmentSlotUI> slotMap = new Dictionary<EquipmentSlot, EquipmentSlotUI>();

        private void Awake()
        {
            BuildSlotMap();
        }

        private void OnRectTransformDimensionsChange()
        {
            // Recalculate portrait width when panel size changes
            if (characterPortraitImage != null && characterPortraitImage.sprite != null)
            {
                AdjustPortraitWidth();
            }
        }

        /// <summary>
        /// Build the slot mapping dictionary.
        /// </summary>
        private void BuildSlotMap()
        {
            slotMap.Clear();

            if (meleeWeaponSlot != null)
                slotMap[EquipmentSlot.MeleeWeapon] = meleeWeaponSlot;
            if (rangedWeaponSlot != null)
                slotMap[EquipmentSlot.RangedWeapon] = rangedWeaponSlot;
            if (armorSlot != null)
                slotMap[EquipmentSlot.Armor] = armorSlot;
            if (accessory1Slot != null)
                slotMap[EquipmentSlot.Accessory1] = accessory1Slot;
            if (accessory2Slot != null)
                slotMap[EquipmentSlot.Accessory2] = accessory2Slot;
            if (codexSlot != null)
                slotMap[EquipmentSlot.Codex] = codexSlot;
        }

        /// <summary>
        /// Initialize the panel with a character.
        /// </summary>
        public void Initialize(CharacterState character)
        {
            currentCharacter = character;

            // Update character portrait
            UpdateCharacterPortrait(character);

            // Initialize all slots (itemGrid may be set later via SetItemGrid)
            foreach (var kvp in slotMap)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Initialize(character, this, detailsPanel, itemGrid);
                }
            }

            RefreshAllSlots();
        }

        /// <summary>
        /// Update the character portrait image.
        /// </summary>
        private void UpdateCharacterPortrait(CharacterState character)
        {
            if (characterPortraitImage == null) return;

            if (character != null && character.Definition != null && character.Definition.InventoryTabPortrait != null)
            {
                characterPortraitImage.sprite = character.Definition.InventoryTabPortrait;
                characterPortraitImage.enabled = true;
                
                // Ensure aspect ratio is preserved
                if (characterPortraitImage.type != Image.Type.Simple)
                {
                    characterPortraitImage.type = Image.Type.Simple;
                }
                characterPortraitImage.preserveAspect = true;
                
                // Adjust width to preserve aspect ratio (height is fixed by RectTransform)
                AdjustPortraitWidth();
            }
            else
            {
                characterPortraitImage.enabled = false;
            }
        }

        /// <summary>
        /// Adjust the portrait width to preserve aspect ratio based on fixed height.
        /// </summary>
        private void AdjustPortraitWidth()
        {
            if (characterPortraitImage == null || characterPortraitImage.sprite == null) return;

            RectTransform rectTransform = characterPortraitImage.rectTransform;
            if (rectTransform == null) return;

            Sprite sprite = characterPortraitImage.sprite;
            
            // Get current height (should be fixed)
            float currentHeight = rectTransform.rect.height;
            
            // Calculate aspect ratio
            float aspectRatio = sprite.rect.width / sprite.rect.height;
            
            // Calculate new width based on aspect ratio
            float newWidth = currentHeight * aspectRatio;
            
            // Set the width (preserving height)
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }

        /// <summary>
        /// Refresh all equipment slots.
        /// </summary>
        public void RefreshAllSlots()
        {
            // Update portrait in case character changed
            if (currentCharacter != null)
            {
                UpdateCharacterPortrait(currentCharacter);
            }

            foreach (var slotUI in slotMap.Values)
            {
                if (slotUI != null)
                {
                    slotUI.RefreshDisplay();
                }
            }
        }

        /// <summary>
        /// Get a slot UI by equipment slot type.
        /// </summary>
        public EquipmentSlotUI GetSlotUI(EquipmentSlot slot)
        {
            return slotMap.ContainsKey(slot) ? slotMap[slot] : null;
        }

        /// <summary>
        /// Set the item grid reference (for drag-and-drop from equipment to inventory).
        /// </summary>
        public void SetItemGrid(ItemGridUI grid)
        {
            itemGrid = grid;
            
            // Update all slots with the grid reference
            foreach (var slotUI in slotMap.Values)
            {
                if (slotUI != null && currentCharacter != null)
                {
                    slotUI.Initialize(currentCharacter, this, detailsPanel, itemGrid);
                }
            }
        }
    }
}
