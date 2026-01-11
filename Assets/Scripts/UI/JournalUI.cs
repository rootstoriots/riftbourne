using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Riftbourne.Exploration;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.UI
{
    /// <summary>
    /// UI controller for the journal system as a tab within Status Menu.
    /// Displays journal entries with confidence level filtering.
    /// No longer handles input - that's managed by StatusMenuUI.
    /// </summary>
    public class JournalUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private TMP_Text entryPrefab;
        [SerializeField] private TMP_Text emptyStateText;
        
        [Header("Filter Buttons")]
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterCertainButton;
        [SerializeField] private Button filterUncertainButton;
        [SerializeField] private Button filterSpeculativeButton;
        
        [Header("Colors")]
        [SerializeField] private Color certainColor = new Color(0.2f, 0.1f, 0.05f); // Dark brown
        [SerializeField] private Color uncertainColor = new Color(0.4f, 0.3f, 0.2f); // Medium brown
        [SerializeField] private Color speculativeColor = new Color(0.6f, 0.5f, 0.4f); // Light brown
        
        private ConfidenceLevel? currentFilter = null; // null = show all
        private List<TMP_Text> entryTextObjects = new List<TMP_Text>();
        
        private void OnEnable()
        {
            // Check for blocking elements and button setup
            CheckButtonBlocking();
            
            // Subscribe to filter buttons with debug logging
            if (filterAllButton != null)
            {
                Debug.Log($"[JournalUI] filterAllButton found - interactable: {filterAllButton.interactable}, enabled: {filterAllButton.enabled}, raycastTarget: {GetButtonRaycastTarget(filterAllButton)}");
                filterAllButton.onClick.AddListener(() => {
                    Debug.Log("[JournalUI] Filter All button pressed");
                    SetFilter(null);
                });
            }
            else
            {
                Debug.LogWarning("[JournalUI] filterAllButton is null!");
            }
            
            if (filterCertainButton != null)
            {
                Debug.Log($"[JournalUI] filterCertainButton found - interactable: {filterCertainButton.interactable}, enabled: {filterCertainButton.enabled}, raycastTarget: {GetButtonRaycastTarget(filterCertainButton)}");
                filterCertainButton.onClick.AddListener(() => {
                    Debug.Log("[JournalUI] Filter Certain button pressed");
                    SetFilter(ConfidenceLevel.Certain);
                });
            }
            else
            {
                Debug.LogWarning("[JournalUI] filterCertainButton is null!");
            }
            
            if (filterUncertainButton != null)
            {
                Debug.Log($"[JournalUI] filterUncertainButton found - interactable: {filterUncertainButton.interactable}, enabled: {filterUncertainButton.enabled}, raycastTarget: {GetButtonRaycastTarget(filterUncertainButton)}");
                filterUncertainButton.onClick.AddListener(() => {
                    Debug.Log("[JournalUI] Filter Uncertain button pressed");
                    SetFilter(ConfidenceLevel.Uncertain);
                });
            }
            else
            {
                Debug.LogWarning("[JournalUI] filterUncertainButton is null!");
            }
            
            if (filterSpeculativeButton != null)
            {
                Debug.Log($"[JournalUI] filterSpeculativeButton found - interactable: {filterSpeculativeButton.interactable}, enabled: {filterSpeculativeButton.enabled}, raycastTarget: {GetButtonRaycastTarget(filterSpeculativeButton)}");
                filterSpeculativeButton.onClick.AddListener(() => {
                    Debug.Log("[JournalUI] Filter Speculative button pressed");
                    SetFilter(ConfidenceLevel.Speculative);
                });
            }
            else
            {
                Debug.LogWarning("[JournalUI] filterSpeculativeButton is null!");
            }
        }
        
        /// <summary>
        /// Check if buttons are being blocked by parent UI elements.
        /// </summary>
        private void CheckButtonBlocking()
        {
            Button[] buttons = { filterAllButton, filterCertainButton, filterUncertainButton, filterSpeculativeButton };
            
            foreach (Button button in buttons)
            {
                if (button == null) continue;
                
                // Check parent hierarchy for blocking elements
                Transform parent = button.transform.parent;
                int depth = 0;
                bool foundBlocker = false;
                
                while (parent != null && depth < 10)
                {
                    // Check for Image/Graphic components with raycast target enabled
                    UnityEngine.UI.Image image = parent.GetComponent<UnityEngine.UI.Image>();
                    if (image != null && image.raycastTarget)
                    {
                        Debug.LogWarning($"[JournalUI] BLOCKER FOUND: {parent.name} has Image with raycastTarget=true (blocks button: {button.name}, depth: {depth})");
                        Debug.LogWarning($"[JournalUI] SOLUTION: Disable 'Raycast Target' on the Image component of {parent.name}");
                        foundBlocker = true;
                    }
                    
                    // Check for CanvasGroup that might block
                    CanvasGroup canvasGroup = parent.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        if (!canvasGroup.interactable)
                        {
                            Debug.LogWarning($"[JournalUI] BLOCKER FOUND: {parent.name} has CanvasGroup with interactable=false (blocks button: {button.name})");
                            Debug.LogWarning($"[JournalUI] SOLUTION: Enable 'Interactable' on CanvasGroup of {parent.name}");
                            foundBlocker = true;
                        }
                        if (!canvasGroup.blocksRaycasts)
                        {
                            Debug.LogWarning($"[JournalUI] BLOCKER FOUND: {parent.name} has CanvasGroup with blocksRaycasts=false (blocks button: {button.name})");
                            Debug.LogWarning($"[JournalUI] SOLUTION: Enable 'Blocks Raycasts' on CanvasGroup of {parent.name}");
                            foundBlocker = true;
                        }
                    }
                    
                    parent = parent.parent;
                    depth++;
                }
                
                if (!foundBlocker)
                {
                    Debug.Log($"[JournalUI] No blockers found for button: {button.name}");
                }
            }
        }
        
        /// <summary>
        /// Get raycast target status of button and its image component.
        /// </summary>
        private string GetButtonRaycastTarget(Button button)
        {
            if (button == null) return "N/A";
            
            UnityEngine.UI.Image image = button.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                return $"Image.raycastTarget={image.raycastTarget}";
            }
            
            return "No Image component";
        }
        
        private void OnDisable()
        {
            // Unsubscribe from filter buttons
            if (filterAllButton != null)
                filterAllButton.onClick.RemoveAllListeners();
            if (filterCertainButton != null)
                filterCertainButton.onClick.RemoveAllListeners();
            if (filterUncertainButton != null)
                filterUncertainButton.onClick.RemoveAllListeners();
            if (filterSpeculativeButton != null)
                filterSpeculativeButton.onClick.RemoveAllListeners();
        }
        
        /// <summary>
        /// Set the confidence level filter.
        /// Public method for StatusMenuUI to call.
        /// </summary>
        public void SetFilter(ConfidenceLevel? filter)
        {
            Debug.Log($"[JournalUI] SetFilter called with: {(filter.HasValue ? filter.Value.ToString() : "null (All)")}");
            currentFilter = filter;
            RefreshEntries();
        }
        
        /// <summary>
        /// Refresh the journal entries display based on current filter.
        /// Public method for StatusMenuUI to call when tab is opened.
        /// </summary>
        public void RefreshEntries()
        {
            Debug.Log($"[JournalUI] RefreshEntries called. Current filter: {(currentFilter.HasValue ? currentFilter.Value.ToString() : "null (All)")}");
            
            if (entryContainer == null)
            {
                Debug.LogError("[JournalUI] entryContainer is null! Cannot refresh entries.");
                return;
            }
            
            // Clear existing entries
            foreach (var entryObj in entryTextObjects)
            {
                if (entryObj != null)
                {
                    Destroy(entryObj.gameObject);
                }
            }
            entryTextObjects.Clear();
            
            // Get entries from JournalSystem
            JournalSystem journalSystem = JournalSystem.Instance;
            if (journalSystem == null)
            {
                ShowEmptyState("Journal system not available.");
                return;
            }
            
            List<JournalEntry> entries;
            if (currentFilter.HasValue)
            {
                entries = journalSystem.GetEntriesByConfidence(currentFilter.Value);
            }
            else
            {
                entries = journalSystem.GetAllEntries();
            }
            
            if (entries == null || entries.Count == 0)
            {
                ShowEmptyState("No journal entries yet.");
                return;
            }
            
            // Hide empty state
            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(false);
            }
            
            // Create entry display objects
            foreach (var entry in entries)
            {
                CreateEntryDisplay(entry);
            }
            
            // Scroll to top
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        /// <summary>
        /// Create a UI element for a journal entry.
        /// </summary>
        private void CreateEntryDisplay(JournalEntry entry)
        {
            if (entryContainer == null) return;
            
            TMP_Text entryText;
            if (entryPrefab != null)
            {
                // Use prefab if available
                entryText = Instantiate(entryPrefab, entryContainer);
            }
            else
            {
                // Create new text object
                GameObject entryObj = new GameObject($"Entry_{entryTextObjects.Count}");
                entryObj.transform.SetParent(entryContainer, false);
                entryText = entryObj.AddComponent<TextMeshProUGUI>();
                
                // Set up RectTransform
                RectTransform rect = entryObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(800, 100);
            }
            
            // Format entry text with timestamp and confidence
            string timestamp = entry.Timestamp.ToString("MM/dd HH:mm");
            string confidencePrefix = GetConfidencePrefix(entry.ConfidenceLevel);
            string entryDisplay = $"[{timestamp}] {confidencePrefix} {entry.EntryText}";
            
            if (entry.IsUnresolved)
            {
                entryDisplay += " [Unresolved]";
            }
            if (entry.IsKnownIncorrect)
            {
                entryDisplay += " [Known Incorrect]";
            }
            
            entryText.text = entryDisplay;
            entryText.color = GetConfidenceColor(entry.ConfidenceLevel);
            
            // Set font style for speculative entries
            if (entry.ConfidenceLevel == ConfidenceLevel.Speculative)
            {
                entryText.fontStyle = FontStyles.Italic;
            }
            
            entryTextObjects.Add(entryText);
        }
        
        /// <summary>
        /// Get a prefix string for confidence level.
        /// </summary>
        private string GetConfidencePrefix(ConfidenceLevel level)
        {
            switch (level)
            {
                case ConfidenceLevel.Certain:
                    return "•";
                case ConfidenceLevel.Uncertain:
                    return "?";
                case ConfidenceLevel.Speculative:
                    return "~";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// Get color for confidence level.
        /// </summary>
        private Color GetConfidenceColor(ConfidenceLevel level)
        {
            switch (level)
            {
                case ConfidenceLevel.Certain:
                    return certainColor;
                case ConfidenceLevel.Uncertain:
                    return uncertainColor;
                case ConfidenceLevel.Speculative:
                    return speculativeColor;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Show empty state message.
        /// </summary>
        private void ShowEmptyState(string message)
        {
            if (emptyStateText != null)
            {
                emptyStateText.text = message;
                emptyStateText.gameObject.SetActive(true);
            }
        }
    }
}
