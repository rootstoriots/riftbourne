using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Combat;
using Riftbourne.Characters;
using Riftbourne.Core;
using Riftbourne.Items;
using Riftbourne.Inventory;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Riftbourne.UI
{
    /// <summary>
    /// Enhanced loot selection interface allowing individual item selection,
    /// "Take All", and "Leave" options.
    /// </summary>
    public class LootSelectionUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject lootPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button takeAllButton;
        [SerializeField] private Button leaveAllButton;
        [SerializeField] private Button confirmButton;
        
        [Header("Loot Display")]
        [SerializeField] private Transform lootItemsContainer;
        [SerializeField] private GameObject lootItemPrefab; // Prefab for individual loot item with checkbox
        
        [Header("Currency Display")]
        [SerializeField] private GameObject currencySection;
        [SerializeField] private TMP_Text currencyText;
        
        [Header("Weight/Capacity Display")]
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private TMP_Text capacityText;
        
        private List<InventorySlot> availableLoot = new List<InventorySlot>();
        private int availableCurrency = 0;
        private HashSet<int> selectedIndices = new HashSet<int>();
        private List<LootItemUI> lootItemUIs = new List<LootItemUI>();
        private Action onComplete;
        
        private void Awake()
        {
            Debug.Log($"LootSelectionUI: Awake called on GameObject '{gameObject.name}'");
            
            // Hide panel initially
            if (lootPanel != null)
            {
                lootPanel.SetActive(false);
                Debug.Log($"LootSelectionUI: Panel '{lootPanel.name}' set to inactive");
            }
            else
            {
                Debug.LogError("LootSelectionUI: lootPanel is null in Awake! Make sure it's assigned in Inspector.");
            }
            
            // Setup buttons
            if (takeAllButton != null)
            {
                takeAllButton.onClick.AddListener(OnTakeAllClicked);
                Debug.Log("LootSelectionUI: TakeAllButton listener added");
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: takeAllButton is null!");
            }
            
            if (leaveAllButton != null)
            {
                leaveAllButton.onClick.AddListener(OnLeaveAllClicked);
                Debug.Log("LootSelectionUI: LeaveAllButton listener added");
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: leaveAllButton is null!");
            }
            
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
                Debug.Log("LootSelectionUI: ConfirmButton listener added");
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: confirmButton is null!");
            }
        }
        
        private void Start()
        {
            Debug.Log($"LootSelectionUI: Start called on GameObject '{gameObject.name}', active: {gameObject.activeSelf}");
        }
        
        /// <summary>
        /// Show the loot selection screen.
        /// </summary>
        public void ShowLoot(List<InventorySlot> loot, int currency, Action onCompleteCallback)
        {
            Debug.Log("LootSelectionUI: ShowLoot called");
            Debug.Log($"LootSelectionUI: GameObject name: {gameObject.name}, active: {gameObject.activeSelf}, enabled: {enabled}");
            
            if (loot == null)
            {
                loot = new List<InventorySlot>();
            }
            
            if (lootPanel == null)
            {
                Debug.LogError("LootSelectionUI: lootPanel is null! Cannot show loot selection.");
                Debug.LogError($"LootSelectionUI: Component is on GameObject '{gameObject.name}'");
                onCompleteCallback?.Invoke();
                return;
            }
            
            Debug.Log($"LootSelectionUI: lootPanel GameObject: '{lootPanel.name}', currently active: {lootPanel.activeSelf}");
            
            availableLoot = new List<InventorySlot>(loot);
            availableCurrency = currency;
            onComplete = onCompleteCallback;
            selectedIndices.Clear();
            
            Debug.Log($"LootSelectionUI: Showing {loot.Count} items and {currency} currency");
            
            // Ensure this GameObject is active (component must be on active GameObject)
            if (!gameObject.activeSelf)
            {
                Debug.LogWarning("LootSelectionUI: GameObject is inactive! Activating it now.");
                gameObject.SetActive(true);
            }
            
            // Update UI
            UpdateLootDisplay();
            UpdateCurrencyDisplay();
            UpdateWeightDisplay();
            
            // Show panel
            lootPanel.SetActive(true);
            Debug.Log($"LootSelectionUI: Panel activated. Panel active: {lootPanel.activeSelf}, activeInHierarchy: {lootPanel.activeInHierarchy}");
            
            // Verify panel is visible
            Canvas canvas = lootPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"LootSelectionUI: Panel is under Canvas '{canvas.name}', Canvas active: {canvas.gameObject.activeSelf}");
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: Panel is not under a Canvas! UI may not render.");
            }
            
            // Ensure buttons are interactable
            if (takeAllButton != null)
            {
                takeAllButton.interactable = true;
            }
            if (leaveAllButton != null)
            {
                leaveAllButton.interactable = true;
            }
            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
            
            // Pause game time
            Time.timeScale = 0f;
            Debug.Log("LootSelectionUI: Time.timeScale set to 0");
            
            // Use coroutine to ensure button is properly set up after UI is fully rendered
            StartCoroutine(EnsureButtonInteractable());
        }
        
        /// <summary>
        /// Coroutine to ensure buttons are interactable after UI is fully rendered.
        /// Fixes issue where first click on "Take All" button doesn't register.
        /// Uses WaitForSecondsRealtime to work even when timeScale = 0.
        /// </summary>
        private IEnumerator EnsureButtonInteractable()
        {
            // Wait a tiny amount of real time (not affected by timeScale) to ensure UI is fully rendered
            yield return new WaitForSecondsRealtime(0.01f);
            
            // Force button to be interactable and selectable
            if (takeAllButton != null)
            {
                takeAllButton.interactable = true;
                // Force button to be selectable (helps with first click recognition)
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null); // Clear selection first
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(takeAllButton.gameObject);
            }
        }
        
        /// <summary>
        /// Update loot items display.
        /// </summary>
        private void UpdateLootDisplay()
        {
            Debug.Log($"LootSelectionUI: UpdateLootDisplay called. availableLoot.Count: {availableLoot?.Count ?? 0}");
            
            if (lootItemsContainer == null)
            {
                Debug.LogError("LootSelectionUI: lootItemsContainer is null! Cannot update loot display.");
                return;
            }
            
            Debug.Log($"LootSelectionUI: lootItemsContainer: '{lootItemsContainer.name}', active: {lootItemsContainer.gameObject.activeSelf}");
            
            // Clear existing entries
            int childCount = lootItemsContainer.childCount;
            Debug.Log($"LootSelectionUI: Clearing {childCount} existing entries");
            foreach (Transform child in lootItemsContainer)
            {
                Destroy(child.gameObject);
            }
            lootItemUIs.Clear();
            
            // Create entries for each loot item
            int itemsCreated = 0;
            for (int i = 0; i < availableLoot.Count; i++)
            {
                InventorySlot slot = availableLoot[i];
                if (slot == null || slot.IsEmpty())
                {
                    Debug.Log($"LootSelectionUI: Skipping slot {i} (null or empty)");
                    continue;
                }
                
                GameObject entryObj = null;
                if (lootItemPrefab != null)
                {
                    entryObj = Instantiate(lootItemPrefab, lootItemsContainer);
                }
                else
                {
                    // Fallback: create simple entry with checkbox
                    entryObj = new GameObject($"LootItem_{i}");
                    entryObj.transform.SetParent(lootItemsContainer, false);
                    RectTransform rect = entryObj.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400, 50);
                    
                    // Add horizontal layout
                    HorizontalLayoutGroup layout = entryObj.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 10;
                    layout.childControlWidth = false;
                    layout.childControlHeight = true;
                    
                    // Add checkbox
                    GameObject checkboxObj = new GameObject("Checkbox");
                    checkboxObj.transform.SetParent(entryObj.transform, false);
                    Toggle toggle = checkboxObj.AddComponent<Toggle>();
                    RectTransform toggleRect = checkboxObj.GetComponent<RectTransform>();
                    toggleRect.sizeDelta = new Vector2(30, 30);
                    
                    // Add text
                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(entryObj.transform, false);
                    TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
                    text.text = $"{slot.Item.ItemName} x{slot.Quantity}";
                    RectTransform textRect = textObj.GetComponent<RectTransform>();
                    textRect.sizeDelta = new Vector2(300, 30);
                }
                
                // Get or create LootItemUI component
                LootItemUI itemUI = entryObj.GetComponent<LootItemUI>();
                if (itemUI == null)
                {
                    itemUI = entryObj.AddComponent<LootItemUI>();
                }
                
                itemUI.Initialize(i, slot, this);
                lootItemUIs.Add(itemUI);
                itemsCreated++;
            }
            
            Debug.Log($"LootSelectionUI: Created {itemsCreated} loot item entries");
        }
        
        /// <summary>
        /// Update currency display.
        /// </summary>
        private void UpdateCurrencyDisplay()
        {
            Debug.Log($"LootSelectionUI: UpdateCurrencyDisplay called. Currency: {availableCurrency}");
            
            if (currencySection != null)
            {
                currencySection.SetActive(availableCurrency > 0);
                Debug.Log($"LootSelectionUI: Currency section active: {currencySection.activeSelf}");
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: currencySection is null!");
            }
            
            if (currencyText != null)
            {
                currencyText.text = $"Aurum Shards: {availableCurrency}";
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: currencyText is null!");
            }
        }
        
        /// <summary>
        /// Update weight/capacity display.
        /// </summary>
        private void UpdateWeightDisplay()
        {
            // Calculate total weight of selected items
            float totalWeight = 0f;
            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < availableLoot.Count)
                {
                    totalWeight += availableLoot[index].GetTotalWeight();
                }
            }
            
            if (weightText != null)
            {
                weightText.text = $"Selected Weight: {totalWeight:F2} kg";
            }
            
            // Get party leader's capacity (if available)
            if (capacityText != null && PartyManager.Instance != null)
            {
                List<Unit> party = PartyManager.Instance.GetPartyMembersAsUnits();
                if (party != null && party.Count > 0)
                {
                    Unit leader = party[0];
                    if (leader != null)
                    {
                        float capacity = leader.EffectiveCarryCapacity;
                        capacityText.text = $"Capacity: {capacity:F2} kg";
                    }
                }
            }
        }
        
        /// <summary>
        /// Toggle selection of a loot item.
        /// </summary>
        public void ToggleItemSelection(int index)
        {
            if (selectedIndices.Contains(index))
            {
                selectedIndices.Remove(index);
            }
            else
            {
                selectedIndices.Add(index);
            }
            
            UpdateWeightDisplay();
        }
        
        /// <summary>
        /// Handle "Take All" button click.
        /// </summary>
        private void OnTakeAllClicked()
        {
            selectedIndices.Clear();
            for (int i = 0; i < availableLoot.Count; i++)
            {
                selectedIndices.Add(i);
            }
            
            // Update UI - set all toggles to selected
            foreach (LootItemUI itemUI in lootItemUIs)
            {
                if (itemUI != null)
                {
                    itemUI.SetSelected(true);
                }
            }
            
            // Force UI to update immediately
            Canvas.ForceUpdateCanvases();
            
            UpdateWeightDisplay();
        }
        
        /// <summary>
        /// Handle "Leave All" button click.
        /// </summary>
        private void OnLeaveAllClicked()
        {
            selectedIndices.Clear();
            
            // Update UI - set all toggles to unselected
            foreach (LootItemUI itemUI in lootItemUIs)
            {
                if (itemUI != null)
                {
                    itemUI.SetSelected(false);
                }
            }
            
            // Force UI to update immediately
            Canvas.ForceUpdateCanvases();
            
            UpdateWeightDisplay();
        }
        
        /// <summary>
        /// Handle confirm button click - distribute selected loot.
        /// </summary>
        private void OnConfirmClicked()
        {
            Debug.Log("LootSelectionUI: OnConfirmClicked called");
            
            // Get party leader (or first available party member)
            List<Unit> party = PartyManager.Instance?.GetPartyMembersAsUnits();
            if (party == null || party.Count == 0)
            {
                Debug.LogWarning("LootSelectionUI: No party members found!");
                Close();
                return;
            }
            
            Unit recipient = party[0]; // Use party leader
            
            // Distribute selected items
            int itemsDistributed = 0;
            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < availableLoot.Count)
                {
                    InventorySlot slot = availableLoot[index];
                    if (slot != null && !slot.IsEmpty())
                    {
                        recipient.AddItem(slot.Item, slot.Quantity);
                        itemsDistributed++;
                    }
                }
            }
            Debug.Log($"LootSelectionUI: Distributed {itemsDistributed} items to {recipient.UnitName}");
            
            // Distribute currency
            if (availableCurrency > 0)
            {
                recipient.GainAurumShards(availableCurrency);
                Debug.Log($"LootSelectionUI: Distributed {availableCurrency} currency to {recipient.UnitName}");
            }
            
            // Clear loot from LootManager
            if (LootManager.Instance != null)
            {
                LootManager.Instance.ClearBattleLoot();
            }
            
            Close();
        }
        
        /// <summary>
        /// Close the loot selection screen.
        /// </summary>
        private void Close()
        {
            Debug.Log("LootSelectionUI: Close called");
            
            // Hide panel
            if (lootPanel != null)
            {
                lootPanel.SetActive(false);
                Debug.Log("LootSelectionUI: Panel deactivated");
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: lootPanel is null in Close()!");
            }
            
            // Resume game time
            Time.timeScale = 1f;
            
            // Invoke callback
            if (onComplete != null)
            {
                Debug.Log("LootSelectionUI: Invoking onComplete callback");
                onComplete.Invoke();
                onComplete = null;
            }
            else
            {
                Debug.LogWarning("LootSelectionUI: onComplete callback is null!");
            }
        }
        
        /// <summary>
        /// Check if loot selection screen is currently showing.
        /// </summary>
        public bool IsShowing()
        {
            return lootPanel != null && lootPanel.activeSelf;
        }
    }
    
    /// <summary>
    /// UI component for individual loot item entry.
    /// </summary>
    public class LootItemUI : MonoBehaviour
    {
        private int index;
        private InventorySlot slot;
        private LootSelectionUI parent;
        private Toggle toggle;
        private bool isProgrammaticallyUpdating = false; // Flag to prevent callback during programmatic updates
        
        public void Initialize(int itemIndex, InventorySlot itemSlot, LootSelectionUI parentUI)
        {
            index = itemIndex;
            slot = itemSlot;
            parent = parentUI;
            
            // Find toggle component
            toggle = GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnToggleChanged);
            }
            
            // Update text display
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0 && slot != null && !slot.IsEmpty())
            {
                texts[0].text = $"{slot.Item.ItemName} x{slot.Quantity} ({slot.GetTotalWeight():F2} kg)";
            }
        }
        
        public void SetSelected(bool selected)
        {
            if (toggle != null)
            {
                // Set flag to prevent callback from triggering ToggleItemSelection
                // Unity's onValueChanged fires synchronously when setting isOn programmatically
                isProgrammaticallyUpdating = true;
                toggle.isOn = selected;
                // Reset flag immediately after setting (event fires synchronously, so this is safe)
                isProgrammaticallyUpdating = false;
            }
        }
        
        private void OnToggleChanged(bool isOn)
        {
            // Only process toggle changes from user interaction, not programmatic updates
            if (!isProgrammaticallyUpdating && parent != null)
            {
                parent.ToggleItemSelection(index);
            }
        }
    }
}
