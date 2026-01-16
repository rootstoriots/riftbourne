using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Items;
using Riftbourne.Core;
using Riftbourne.Inventory;

namespace Riftbourne.UI
{
    /// <summary>
    /// Inventory UI for displaying unit inventory, weight, and currency.
    /// Now uses grid-based visual display with drag-and-drop.
    /// Integrated into StatusMenuUI's inventory tab.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private ItemGridUI itemGrid;
        [SerializeField] private EquipmentSlotsPanel equipmentPanel;
        [SerializeField] private ItemDetailsPanel detailsPanel;
        
        private Unit currentUnit;
        private CharacterState currentCharacterState;
        
        /// <summary>
        /// Refresh the inventory display for the current character.
        /// Called by StatusMenuUI when inventory tab is shown.
        /// </summary>
        public void RefreshDisplay(Unit unit = null, CharacterState characterState = null)
        {
            // Update current references
            if (unit != null)
            {
                currentUnit = unit;
            }
            if (characterState != null)
            {
                currentCharacterState = characterState;
            }
            
            // Prefer CharacterState over Unit
            if (currentCharacterState != null)
            {
                RefreshFromCharacterState(currentCharacterState);
            }
            else if (currentUnit != null)
            {
                RefreshFromUnit(currentUnit);
            }
            else
            {
                // Try to get current character from StatusMenuUI or PartyManager
                GetCurrentCharacter();
                if (currentCharacterState != null)
                {
                    RefreshFromCharacterState(currentCharacterState);
                }
                else if (currentUnit != null)
                {
                    RefreshFromUnit(currentUnit);
                }
                else
                {
                    ShowEmptyDisplay();
                }
            }
        }
        
        /// <summary>
        /// Get current character from PartyManager.
        /// Uses the same logic as StatusMenuUI.GetCurrentUnit().
        /// </summary>
        private void GetCurrentCharacter()
        {
            // Try to get POV character from PartyManager (same as StatusMenuUI)
            if (PartyManager.Instance != null)
            {
                CharacterState povCharacter = PartyManager.Instance.POVCharacter;
                if (povCharacter != null)
                {
                    currentCharacterState = povCharacter;
                    // Try to find corresponding Unit
                    currentUnit = FindUnitForCharacterState(povCharacter);
                    return;
                }

                // Fall back to first party member if no POV
                var partyMembers = PartyManager.Instance.GetPartyMembers();
                if (partyMembers != null && partyMembers.Count > 0)
                {
                    currentCharacterState = partyMembers[0];
                    currentUnit = FindUnitForCharacterState(currentCharacterState);
                    return;
                }
            }

            // Legacy: Try PartyManager SelectedUnit (battle mode)
            if (PartyManager.Instance != null && PartyManager.Instance.SelectedUnit != null)
            {
                currentUnit = PartyManager.Instance.SelectedUnit;
                // Try to find CharacterState for this Unit
                currentCharacterState = FindCharacterStateForUnit(currentUnit);
                return;
            }
            
            // Legacy: Try to find first player unit
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            foreach (Unit unit in allUnits)
            {
                if (unit.IsPlayerControlled)
                {
                    currentUnit = unit;
                    currentCharacterState = FindCharacterStateForUnit(unit);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Find Unit GameObject for a CharacterState.
        /// </summary>
        private Unit FindUnitForCharacterState(CharacterState state)
        {
            if (state == null || state.Definition == null) return null;

            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            foreach (Unit unit in allUnits)
            {
                if (unit != null && unit.UnitName == state.Definition.CharacterName && unit.IsPlayerControlled)
                {
                    return unit;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Find CharacterState for a Unit.
        /// </summary>
        private CharacterState FindCharacterStateForUnit(Unit unit)
        {
            if (unit == null || PartyManager.Instance == null) return null;

            var partyMembers = PartyManager.Instance.GetPartyMembers();
            foreach (var state in partyMembers)
            {
                if (state != null && state.Definition != null && state.Definition.CharacterName == unit.UnitName)
                {
                    return state;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Refresh display from CharacterState (preferred method).
        /// CharacterState has inventory, so we can display it directly.
        /// </summary>
        private void RefreshFromCharacterState(CharacterState state)
        {
            
            if (state == null || state.Definition == null)
            {
                ShowEmptyDisplay();
                return;
            }
            
            // CharacterState HAS inventory! Display it directly.
            // Try to find Unit for weight calculations (optional - Unit has weight/encumbrance properties)
            Unit matchingUnit = FindUnitForCharacterState(state);
            
            // Stats display
            if (statsText != null)
            {
                string characterName = state.Definition.CharacterName;
                string weightInfo = "";
                
                // If we found a Unit, use its weight calculations
                if (matchingUnit != null)
                {
                    weightInfo = $"Weight: {matchingUnit.CurrentWeight:F2} / {matchingUnit.EffectiveCarryCapacity:F2} kg " +
                                $"({matchingUnit.EncumbrancePercent:P0})\n" +
                                (matchingUnit.IsOverencumbered ? "<color=red>OVERENCUMBERED!</color>" : "");
                }
                else
                {
                    // Calculate weight manually from CharacterState inventory
                    float totalWeight = CalculateWeightFromInventory(state.Inventory, state.ContainerInventory);
                    // Estimate capacity (CharacterState doesn't have strength, so use default or estimate)
                    float estimatedCapacity = 20.0f; // Default estimate
                    float encumbrancePercent = estimatedCapacity > 0 ? totalWeight / estimatedCapacity : 0f;
                    weightInfo = $"Weight: {totalWeight:F2} / {estimatedCapacity:F2} kg " +
                                $"({encumbrancePercent:P0})\n" +
                                (encumbrancePercent > 1.0f ? "<color=red>OVERENCUMBERED!</color>" : "");
                }
                
                statsText.text = $"<b>{characterName}</b>\n" +
                               $"Aurum Shards: {state.AurumShards} 💰\n" +
                               weightInfo;
            }
            
            // Inventory display - use grid system
            if (itemGrid != null)
            {
                itemGrid.PopulateGrid(state, matchingUnit);
            }

            // Equipment display
            if (equipmentPanel != null)
            {
                equipmentPanel.Initialize(state);
                // Pass itemGrid reference for drag-and-drop
                if (itemGrid != null)
                {
                    equipmentPanel.SetItemGrid(itemGrid);
                }
            }
        }
        
        /// <summary>
        /// Refresh display from Unit.
        /// </summary>
        private void RefreshFromUnit(Unit unit)
        {
            if (unit == null)
            {
                ShowEmptyDisplay();
                return;
            }
        
            // Stats display
            if (statsText != null)
            {
                statsText.text = $"<b>{unit.UnitName}</b>\n" +
                               $"Aurum Shards: {unit.AurumShards} 💰\n" +
                               $"Weight: {unit.CurrentWeight:F2} / {unit.EffectiveCarryCapacity:F2} kg " +
                               $"({unit.EncumbrancePercent:P0})\n" +
                               (unit.IsOverencumbered ? "<color=red>OVERENCUMBERED!</color>" : "");
            }
            
            // Inventory display - use grid system
            // Convert Unit to CharacterState for grid display
            CharacterState unitState = FindCharacterStateForUnit(unit);
            if (itemGrid != null)
            {
                if (unitState != null)
                {
                    itemGrid.PopulateGrid(unitState, unit);
                }
                else
                {
                    // Fallback: create temporary CharacterState from Unit
                    // This is not ideal but maintains backward compatibility
                    Debug.LogWarning("InventoryUI: Could not find CharacterState for Unit. Grid display may not work correctly.");
                }
            }

            // Equipment display
            if (equipmentPanel != null && unitState != null)
            {
                equipmentPanel.Initialize(unitState);
                // Pass itemGrid reference for drag-and-drop
                if (itemGrid != null)
                {
                    equipmentPanel.SetItemGrid(itemGrid);
                }
            }
        }
        
        /// <summary>
        /// Calculate total weight from inventory slots.
        /// </summary>
        private float CalculateWeightFromInventory(List<InventorySlot> mainInventory, List<InventorySlot> containerInventory)
        {
            float total = 0f;
            
            // Main inventory weight
            if (mainInventory != null)
            {
                foreach (var slot in mainInventory)
                {
                    if (slot != null && slot.Item != null)
                        total += slot.GetTotalWeight();
                }
            }
            
            // Container inventory (with reduction - simplified, assumes 50% reduction)
            if (containerInventory != null)
            {
                foreach (var slot in containerInventory)
                {
                    if (slot != null && slot.Item != null)
                    {
                        total += slot.GetTotalWeight() * 0.5f; // 50% weight reduction in containers
                    }
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// Show empty display when no character is selected.
        /// </summary>
        private void ShowEmptyDisplay()
        {
            if (statsText != null)
            {
                statsText.text = "<color=grey>No character selected</color>";
            }
            
            if (itemGrid != null)
            {
                itemGrid.PopulateGrid(null, null);
            }

            if (equipmentPanel != null)
            {
                equipmentPanel.Initialize(null);
            }

            // Show empty state in details panel
            if (detailsPanel != null)
            {
                detailsPanel.ShowEmptyState();
            }
        }
    }
}
