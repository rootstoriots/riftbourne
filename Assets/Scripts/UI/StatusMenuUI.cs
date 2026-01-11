using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Riftbourne.Characters;
using Riftbourne.Skills;
using Riftbourne.Core;
using Riftbourne.Exploration;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.UI
{
    /// <summary>
    /// Main status menu UI with tab system.
    /// Toggle with TAB key - pauses exploration when open.
    /// </summary>
    public class StatusMenuUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject statusMenuPanel;
        
        [Header("Tab Buttons")]
        [SerializeField] private Button statusTabButton;
        [SerializeField] private Button equipmentTabButton;
        [SerializeField] private Button skillsTabButton;
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button journalTabButton;
        [SerializeField] private Button mapTabButton;
        [SerializeField] private Button narrativeSkillsTabButton; // DEV ONLY - Will be removed in production
        
        [Header("Tab Panels")]
        [SerializeField] private GameObject statusTabPanel;
        [SerializeField] private GameObject equipmentTabPanel;
        [SerializeField] private GameObject skillsTabPanel;
        [SerializeField] private GameObject inventoryTabPanel;
        [SerializeField] private GameObject journalTabPanel;
        [SerializeField] private GameObject mapTabPanel;
        [SerializeField] private GameObject narrativeSkillsTabPanel; // DEV ONLY - Will be removed in production
        
        [Header("Journal Tab")]
        [SerializeField] private JournalUI journalUI;
        
        [Header("Map Tab")]
        [SerializeField] private TMP_Text mapPlaceholderText;
        
        [Header("Status Tab UI - Party Portraits")]
        [Tooltip("Container for party member portraits (should have 6 slots)")]
        [SerializeField] private Transform partyPortraitsContainer;
        [Tooltip("Six static portrait slot GameObjects (in order: slot 0-5). Each should have Image, Button, and Name Text components.")]
        [SerializeField] private GameObject[] partyPortraitSlots = new GameObject[6];
        [Tooltip("Maximum number of party portraits to display")]
        [SerializeField] private int maxPartyPortraits = 6;
        
        [Header("Status Tab UI - Character Display")]
        [SerializeField] private Image largePortraitImage;
        [SerializeField] private TMP_Text characterFullNameText;
        [SerializeField] private TMP_Text characterTitleText;
        [SerializeField] private TMP_Text characterNameText; // Keep for backward compatibility
        [SerializeField] private TMP_Text characterBioText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text spText;
        [SerializeField] private TMP_Text strengthText;
        [SerializeField] private TMP_Text finesseText;
        [SerializeField] private TMP_Text focusText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text luckText;
        
        [Header("Status Tab UI - Weapon Proficiencies")]
        [Tooltip("Weapon proficiencies section (integrated into status tab)")]
        [SerializeField] private GameObject proficiencySection;
        [Tooltip("Container for proficiency list entries")]
        [SerializeField] private Transform proficiencyListContainer;
        [Tooltip("Prefab for individual proficiency entry display")]
        [SerializeField] private GameObject proficiencyEntryPrefab;
        
        [Header("Status Tab UI - Narrative Skills")]
        [Tooltip("Narrative skills section (integrated into status tab)")]
        [SerializeField] private GameObject narrativeSkillsSection;
        [SerializeField] private TMP_Text perceptionLevelText;
        [SerializeField] private TMP_Text perceptionThresholdText;
        [SerializeField] private TMP_Text interpretiveLevelText;
        [SerializeField] private TMP_Text interpretiveThresholdText;
        [SerializeField] private TMP_Text empathicLevelText;
        [SerializeField] private TMP_Text empathicThresholdText;
        [SerializeField] private TMP_Text perceptionDescriptionText;
        [SerializeField] private TMP_Text interpretiveDescriptionText;
        [SerializeField] private TMP_Text empathicDescriptionText;
        
        private PlayerInputActions inputActions;
        private Unit currentUnit; // Legacy - for backward compatibility
        private CharacterState currentCharacterState; // Currently displayed character
        private float previousTimeScale = 1f;
        
        // Party portrait UI elements
        private Dictionary<GameObject, CharacterState> portraitToCharacterMap = new Dictionary<GameObject, CharacterState>();
        
        private enum TabType
        {
            Status,
            Equipment,
            Skills,
            Inventory,
            Journal,
            Map,
            NarrativeSkills // DEV ONLY - Will be removed in production
        }
        
        private TabType currentTab = TabType.Status;
        
        private void Awake()
        {
            Debug.Log("[StatusMenuUI] Awake() called - script is active!");
            Debug.Log($"[StatusMenuUI] GameObject name: {gameObject.name}, active: {gameObject.activeSelf}, enabled: {enabled}");
            
            inputActions = new PlayerInputActions();
            
            // Ensure menu starts hidden
            // IMPORTANT: Don't disable the panel if this script is ON the panel!
            // The script should be on a parent GameObject (like StatusMenuCanvas)
            if (statusMenuPanel != null)
            {
                // Only disable if it's not the same GameObject as this script
                if (statusMenuPanel != gameObject)
                {
                    statusMenuPanel.SetActive(false);
                    Debug.Log("[StatusMenuUI] Status menu panel found and hidden");
                }
                else
                {
                    Debug.LogError("[StatusMenuUI] ERROR: StatusMenuUI script is attached to the same GameObject as statusMenuPanel! Move the script to a parent GameObject (like StatusMenuCanvas) instead!");
                    // Don't disable ourselves - that would disable the script!
                }
            }
            else
            {
                Debug.LogWarning("[StatusMenuUI] statusMenuPanel is NULL in Awake()! Make sure it's assigned in Inspector.");
            }
        }
        
        private void OnEnable()
        {
            Debug.Log("[StatusMenuUI] OnEnable() called!");
            Debug.Log($"[StatusMenuUI] GameObject active: {gameObject.activeSelf}, component enabled: {enabled}");
        }
        
        private void Start()
        {
            Debug.Log("[StatusMenuUI] Start() called");
            Debug.Log($"[StatusMenuUI] GameObject active in Start: {gameObject.activeSelf}, component enabled: {enabled}");
            
            // Double-check panel is disabled (in case something enabled it)
            if (statusMenuPanel != null && statusMenuPanel != gameObject)
            {
                if (statusMenuPanel.activeSelf)
                {
                    Debug.LogWarning("[StatusMenuUI] Panel was active in Start() - disabling it now");
                    statusMenuPanel.SetActive(false);
                }
            }
            
            // Setup input in Start (after everything is initialized)
            SetupInput();
            
            // Test Input System initialization
            if (Keyboard.current != null)
            {
                Debug.Log("[StatusMenuUI] Keyboard.current is available - Input System is working");
            }
            else
            {
                Debug.LogError("[StatusMenuUI] Keyboard.current is NULL - Input System not initialized!");
            }
        }
        
        private void OnDestroy()
        {
            Debug.Log("[StatusMenuUI] OnDestroy() called");
            InputSystem.onEvent -= OnInputEvent;
        }
        
        private void OnInputEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr eventPtr, UnityEngine.InputSystem.InputDevice device)
        {
            // Check if this is a keyboard event
            if (device is Keyboard keyboard)
            {
                // Check if TAB key was pressed
                // Only use this if Input Actions aren't working (to avoid double-toggling)
                if (keyboard.tabKey.wasPressedThisFrame && (inputActions == null || !inputActions.Gameplay.enabled))
                {
                    Debug.Log("[StatusMenuUI] TAB detected via InputSystem.onEvent (fallback method)!");
                    ToggleStatusMenu();
                }
                
                // Check if Escape key was pressed
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    HandleEscapeKey();
                }
            }
        }
        
        /// <summary>
        /// Handle Escape key press.
        /// Priority: Status Menu > System Menu
        /// </summary>
        private void HandleEscapeKey()
        {
            // First check if status menu is open
            if (statusMenuPanel != null && statusMenuPanel.activeSelf)
            {
                // Status menu is open - close it
                Debug.Log("[StatusMenuUI] Escape key detected - closing status menu");
                CloseStatusMenu();
                return;
            }

            // Status menu is closed - check system menu
            var systemMenu = FindFirstObjectByType<SystemMenuUI>();
            if (systemMenu != null)
            {
                if (systemMenu.IsOpen)
                {
                    // System menu is open - let SystemMenuUI handle closing it
                    // Don't do anything here to avoid conflicts
                    Debug.Log("[StatusMenuUI] Escape key detected - system menu is open, letting it handle closing");
                    return;
                }
                else
                {
                    // Both menus are closed - open system menu
                    // Use coroutine to delay opening slightly to avoid same-frame closing
                    Debug.Log("[StatusMenuUI] Escape key detected - opening system menu");
                    StartCoroutine(OpenSystemMenuDelayed(systemMenu));
                }
            }
            else
            {
                Debug.LogWarning("[StatusMenuUI] Escape key detected but SystemMenuUI not found!");
            }
        }

        /// <summary>
        /// Open system menu with a small delay to prevent immediate closing.
        /// </summary>
        private System.Collections.IEnumerator OpenSystemMenuDelayed(SystemMenuUI systemMenu)
        {
            // Wait until end of frame to ensure Escape key press is fully processed
            yield return new WaitForEndOfFrame();
            yield return null; // Wait one more frame
            
            if (systemMenu != null && !systemMenu.IsOpen)
            {
                systemMenu.OpenMenu();
            }
        }

        /// <summary>
        /// Close the status menu (called by Escape key).
        /// </summary>
        private void CloseStatusMenu()
        {
            if (statusMenuPanel != null && statusMenuPanel.activeSelf)
            {
                statusMenuPanel.SetActive(false);
                Time.timeScale = previousTimeScale;
                Debug.Log("[StatusMenuUI] Status menu closed via Escape key");
            }
        }
        
        private void SetupInput()
        {
            Debug.Log("[StatusMenuUI] SetupInput() called - setting up input");
            
            if (inputActions == null)
            {
                Debug.LogError("[StatusMenuUI] inputActions is null! This should not happen.");
                return;
            }
            
            inputActions?.Gameplay.Enable();
            
            // Check if StatusMenu action exists
            try
            {
                inputActions.Gameplay.StatusMenu.performed += OnStatusMenuToggle;
                Debug.Log("[StatusMenuUI] StatusMenu input action subscribed successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StatusMenuUI] Failed to subscribe to StatusMenu action: {e.Message}. Make sure PlayerInputActions.inputactions was saved and Unity regenerated the C# class.");
            }
            
            // Subscribe to Journal action (J key)
            try
            {
                inputActions.Gameplay.Journal.performed += OnJournalKeyPressed;
                Debug.Log("[StatusMenuUI] Journal input action subscribed successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StatusMenuUI] Failed to subscribe to Journal action: {e.Message}. Make sure PlayerInputActions.inputactions was saved and Unity regenerated the C# class.");
            }
            
            // Subscribe to InputSystem.onEvent as alternative method
            InputSystem.onEvent += OnInputEvent;
            Debug.Log("[StatusMenuUI] Subscribed to InputSystem.onEvent");
            
            // Subscribe to tab buttons
            if (statusTabButton != null)
                statusTabButton.onClick.AddListener(() => SwitchTab(TabType.Status));
            if (equipmentTabButton != null)
                equipmentTabButton.onClick.AddListener(() => SwitchTab(TabType.Equipment));
            if (skillsTabButton != null)
                skillsTabButton.onClick.AddListener(() => SwitchTab(TabType.Skills));
            if (inventoryTabButton != null)
                inventoryTabButton.onClick.AddListener(() => SwitchTab(TabType.Inventory));
            if (journalTabButton != null)
                journalTabButton.onClick.AddListener(() => SwitchTab(TabType.Journal));
            if (mapTabButton != null)
                mapTabButton.onClick.AddListener(() => SwitchTab(TabType.Map));
            // Hide narrative skills tab button (narrative skills now in status tab)
            if (narrativeSkillsTabButton != null)
            {
                narrativeSkillsTabButton.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Handle J key press - opens Status Menu and switches to Journal tab.
        /// </summary>
        private void OnJournalKeyPressed(InputAction.CallbackContext context)
        {
            Debug.Log("[StatusMenuUI] J key pressed - opening Status Menu to Journal tab");
            
            // If menu is closed, open it and switch to Journal tab
            if (statusMenuPanel == null || !statusMenuPanel.activeSelf)
            {
                // Open menu
                statusMenuPanel.SetActive(true);
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                GetCurrentUnit();
                
                // Switch to Journal tab
                SwitchTab(TabType.Journal);
            }
            else
            {
                // Menu is open - if we're on Journal tab, close menu. Otherwise switch to Journal tab.
                if (currentTab == TabType.Journal)
                {
                    // Close menu
                    statusMenuPanel.SetActive(false);
                    Time.timeScale = previousTimeScale;
                }
                else
                {
                    // Switch to Journal tab
                    SwitchTab(TabType.Journal);
                }
            }
        }
        
        private void OnDisable()
        {
            Debug.Log("[StatusMenuUI] OnDisable() called!");
            
            InputSystem.onEvent -= OnInputEvent;
            
            inputActions?.Gameplay.Disable();
            if (inputActions != null)
            {
                inputActions.Gameplay.StatusMenu.performed -= OnStatusMenuToggle;
            }
            
            // Unsubscribe from tab buttons
            if (statusTabButton != null)
                statusTabButton.onClick.RemoveAllListeners();
            if (equipmentTabButton != null)
                equipmentTabButton.onClick.RemoveAllListeners();
            if (skillsTabButton != null)
                skillsTabButton.onClick.RemoveAllListeners();
            if (inventoryTabButton != null)
                inventoryTabButton.onClick.RemoveAllListeners();
            if (narrativeSkillsTabButton != null)
                narrativeSkillsTabButton.onClick.RemoveAllListeners();
        }
        
        private void OnStatusMenuToggle(InputAction.CallbackContext context)
        {
            Debug.Log("[StatusMenuUI] TAB key pressed - OnStatusMenuToggle called");
            ToggleStatusMenu();
        }
        
        /// <summary>
        /// Toggle the status menu on/off.
        /// </summary>
        private void ToggleStatusMenu()
        {
            if (statusMenuPanel == null)
            {
                Debug.LogError("[StatusMenuUI] statusMenuPanel is null! Make sure it's assigned in Inspector.");
                return;
            }
            
            bool isActive = statusMenuPanel.activeSelf;
            Debug.Log($"[StatusMenuUI] Panel is currently {(isActive ? "active" : "inactive")}, toggling...");
            
            // Toggle the panel
            statusMenuPanel.SetActive(!isActive);
            
            // Check the NEW state (after toggle)
            bool nowActive = statusMenuPanel.activeSelf;
            
            if (nowActive)
            {
                // Opening menu - pause exploration and refresh
                Debug.Log("[StatusMenuUI] Opening status menu - pausing game");
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                GetCurrentUnit();
                RefreshPartyPortraits(); // Update party portraits when menu opens
                RefreshCurrentTab();
            }
            else
            {
                // Closing menu - resume exploration
                Debug.Log("[StatusMenuUI] Closing status menu - resuming game");
                Time.timeScale = previousTimeScale;
            }
        }
        
        private void Update()
        {
            // Removed TAB key check from Update() to prevent double-toggling
            // TAB is now handled by Input Actions and InputSystem.onEvent only
        }
        
        /// <summary>
        /// Get the current player unit for display.
        /// </summary>
        private void GetCurrentUnit()
        {
            // Try to get POV character from PartyManager (new system)
            if (PartyManager.Instance != null)
            {
                CharacterState povCharacter = PartyManager.Instance.POVCharacter;
                if (povCharacter != null)
                {
                    currentCharacterState = povCharacter;
                    // Try to find corresponding Unit for backward compatibility
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
            
            currentUnit = null;
            currentCharacterState = null;
        }

        /// <summary>
        /// Find Unit GameObject for a CharacterState (for backward compatibility).
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
        /// Switch to a different tab.
        /// </summary>
        private void SwitchTab(TabType tab)
        {
            currentTab = tab;
            RefreshCurrentTab();
        }
        
        /// <summary>
        /// Refresh the currently active tab.
        /// </summary>
        private void RefreshCurrentTab()
        {
            // Hide all tab panels
            if (statusTabPanel != null) statusTabPanel.SetActive(false);
            if (equipmentTabPanel != null) equipmentTabPanel.SetActive(false);
            if (skillsTabPanel != null) skillsTabPanel.SetActive(false);
            if (inventoryTabPanel != null) inventoryTabPanel.SetActive(false);
            if (journalTabPanel != null) journalTabPanel.SetActive(false);
            if (mapTabPanel != null) mapTabPanel.SetActive(false);
            if (narrativeSkillsTabPanel != null) narrativeSkillsTabPanel.SetActive(false);
            
            // Show current tab panel and refresh
            switch (currentTab)
            {
                case TabType.Status:
                    if (statusTabPanel != null) statusTabPanel.SetActive(true);
                    RefreshStatusTab();
                    break;
                case TabType.Equipment:
                    if (equipmentTabPanel != null) equipmentTabPanel.SetActive(true);
                    // TODO: Implement equipment tab
                    break;
                case TabType.Skills:
                    if (skillsTabPanel != null) skillsTabPanel.SetActive(true);
                    // TODO: Implement skills tab
                    break;
                case TabType.Inventory:
                    if (inventoryTabPanel != null) inventoryTabPanel.SetActive(true);
                    // TODO: Implement inventory tab
                    break;
                case TabType.Journal:
                    if (journalTabPanel != null) journalTabPanel.SetActive(true);
                    RefreshJournalTab();
                    break;
                case TabType.Map:
                    if (mapTabPanel != null) mapTabPanel.SetActive(true);
                    RefreshMapTab();
                    break;
                case TabType.NarrativeSkills:
                    // Narrative skills are now integrated into Status tab
                    // This tab is deprecated and should not be accessible
                    break;
            }
        }
        
        /// <summary>
        /// Refresh the Journal tab display.
        /// </summary>
        private void RefreshJournalTab()
        {
            if (journalUI != null)
            {
                journalUI.RefreshEntries();
            }
        }
        
        /// <summary>
        /// Refresh the Map tab display (placeholder).
        /// </summary>
        private void RefreshMapTab()
        {
            if (mapPlaceholderText != null)
            {
                mapPlaceholderText.text = "Map - Coming Soon";
            }
        }
        
        /// <summary>
        /// Refresh the Status tab display.
        /// Uses CharacterState if available, falls back to Unit for backward compatibility.
        /// Now includes narrative skills integrated into the status tab.
        /// </summary>
        private void RefreshStatusTab()
        {
            // Prefer CharacterState over Unit
            if (currentCharacterState != null && currentCharacterState.Definition != null)
            {
                var def = currentCharacterState.Definition;
                
                // Large portrait
                if (largePortraitImage != null)
                {
                    largePortraitImage.sprite = def.Portrait;
                    largePortraitImage.enabled = def.Portrait != null;
                }
                
                // Full name and title
                if (characterFullNameText != null)
                    characterFullNameText.text = def.FullName;
                
                if (characterTitleText != null)
                {
                    characterTitleText.text = string.IsNullOrEmpty(def.Title) ? "" : def.Title;
                    if (characterTitleText.gameObject != null)
                        characterTitleText.gameObject.SetActive(!string.IsNullOrEmpty(def.Title));
                }
                
                // Character name (backward compatibility)
                if (characterNameText != null)
                    characterNameText.text = def.CharacterName;
                
                // Character bio
                if (characterBioText != null)
                {
                    characterBioText.text = string.IsNullOrEmpty(def.Bio) ? "No bio available." : def.Bio;
                }
                
                // Progression
                if (levelText != null)
                    levelText.text = $"Level: {currentCharacterState.Level}";
                
                if (xpText != null)
                {
                    xpText.text = ""; // XP system removed
                }
                
                if (spText != null)
                    spText.text = $"SP: {currentCharacterState.SkillPoints}";
                
                // Stats
                if (strengthText != null)
                    strengthText.text = $"Strength: {currentCharacterState.CurrentStrength}";
                
                if (finesseText != null)
                    finesseText.text = $"Finesse: {currentCharacterState.CurrentFinesse}";
                
                if (focusText != null)
                    focusText.text = $"Focus: {currentCharacterState.CurrentFocus}";
                
                if (speedText != null)
                    speedText.text = $"Speed: {currentCharacterState.CurrentSpeed}";
                
                if (luckText != null)
                    luckText.text = $"Luck: {currentCharacterState.CurrentLuck}";
                
                // Narrative Skills (integrated into status tab)
                RefreshNarrativeSkillsDisplay(currentCharacterState);
                
                // Weapon Proficiencies
                RefreshProficiencyDisplay(currentCharacterState);
                
                return;
            }

            // Fallback to Unit (backward compatibility)
            if (currentUnit == null)
            {
                if (characterNameText != null) characterNameText.text = "No Character Selected";
                if (characterFullNameText != null) characterFullNameText.text = "No Character Selected";
                if (characterBioText != null) characterBioText.text = "";
                if (largePortraitImage != null) largePortraitImage.enabled = false;
                return;
            }
            
            if (characterNameText != null)
                characterNameText.text = currentUnit.UnitName;
            
            if (characterFullNameText != null)
                characterFullNameText.text = currentUnit.UnitName;
            
            if (largePortraitImage != null)
            {
                largePortraitImage.sprite = currentUnit.Portrait;
                largePortraitImage.enabled = currentUnit.Portrait != null;
            }
            
            // Character bio (try to get from CharacterState if available)
            if (characterBioText != null)
            {
                // Try to get bio from CharacterState first
                CharacterState unitState = null;
                if (PartyManager.Instance != null)
                {
                    var partyMembers = PartyManager.Instance.GetPartyMembers();
                    unitState = partyMembers?.FirstOrDefault(c => c.CharacterID == currentUnit.name || c.CharacterID == currentUnit.UnitName);
                }
                
                if (unitState != null && unitState.Definition != null)
                {
                    characterBioText.text = string.IsNullOrEmpty(unitState.Definition.Bio) ? "No bio available." : unitState.Definition.Bio;
                }
                else
                {
                    characterBioText.text = "No bio available.";
                }
            }
            
            if (levelText != null)
                levelText.text = $"Level: {currentUnit.Level}";
            
            if (xpText != null)
            {
                xpText.text = ""; // XP system removed
            }
            
            if (spText != null)
                spText.text = $"SP: {currentUnit.SkillPoints}";
            
            if (strengthText != null)
                strengthText.text = $"Strength: {currentUnit.Strength}";
            
            if (finesseText != null)
                finesseText.text = $"Finesse: {currentUnit.Finesse}";
            
            if (focusText != null)
                focusText.text = $"Focus: {currentUnit.Focus}";
            
            if (speedText != null)
                speedText.text = $"Speed: {currentUnit.Speed}";
            
            if (luckText != null)
                luckText.text = $"Luck: {currentUnit.Luck}";
            
            // Narrative skills fallback
            RefreshNarrativeSkillsDisplay(null);
            
            // Weapon Proficiencies fallback
            RefreshProficiencyDisplay(null);
        }
        
        /// <summary>
        /// Refresh narrative skills display (integrated into status tab).
        /// </summary>
        private void RefreshNarrativeSkillsDisplay(CharacterState character)
        {
            // Show/hide narrative skills section
            if (narrativeSkillsSection != null)
            {
                narrativeSkillsSection.SetActive(character != null || currentUnit != null);
            }
            
            if (character != null)
            {
                // Perception
                int perceptionLevel = character.GetNarrativeSkillLevel(NarrativeSkillCategory.Perception);
                if (perceptionLevelText != null)
                    perceptionLevelText.text = $"Perception: {perceptionLevel}";
                if (perceptionThresholdText != null)
                    perceptionThresholdText.text = GetThresholdBandText(perceptionLevel);
                if (perceptionDescriptionText != null)
                    perceptionDescriptionText.text = "Awareness, observation, pattern recognition. Adds metadata to locations, objects, NPC behaviors.";
                
                // Interpretive
                int interpretiveLevel = character.GetNarrativeSkillLevel(NarrativeSkillCategory.Interpretive);
                if (interpretiveLevelText != null)
                    interpretiveLevelText.text = $"Interpretive: {interpretiveLevel}";
                if (interpretiveThresholdText != null)
                    interpretiveThresholdText.text = GetThresholdBandText(interpretiveLevel);
                if (interpretiveDescriptionText != null)
                    interpretiveDescriptionText.text = "Lore literacy, symbol decoding, cultural memory. Converts raw observations into meaning.";
                
                // Empathic
                int empathicLevel = character.GetNarrativeSkillLevel(NarrativeSkillCategory.Empathic);
                if (empathicLevelText != null)
                    empathicLevelText.text = $"Empathic: {empathicLevel}";
                if (empathicThresholdText != null)
                    empathicThresholdText.text = GetThresholdBandText(empathicLevel);
                if (empathicDescriptionText != null)
                    empathicDescriptionText.text = "Social intuition, intent sense, emotional residue reading. Infers past motivations, tensions, or secrets.";
            }
            else if (currentUnit != null)
            {
                // Fallback to Unit
                int perceptionLevel = currentUnit.GetNarrativeSkillLevel(NarrativeSkillCategory.Perception);
                if (perceptionLevelText != null)
                    perceptionLevelText.text = $"Perception: {perceptionLevel}";
                if (perceptionThresholdText != null)
                    perceptionThresholdText.text = GetThresholdBandText(perceptionLevel);
                
                int interpretiveLevel = currentUnit.GetNarrativeSkillLevel(NarrativeSkillCategory.Interpretive);
                if (interpretiveLevelText != null)
                    interpretiveLevelText.text = $"Interpretive: {interpretiveLevel}";
                if (interpretiveThresholdText != null)
                    interpretiveThresholdText.text = GetThresholdBandText(interpretiveLevel);
                
                int empathicLevel = currentUnit.GetNarrativeSkillLevel(NarrativeSkillCategory.Empathic);
                if (empathicLevelText != null)
                    empathicLevelText.text = $"Empathic: {empathicLevel}";
                if (empathicThresholdText != null)
                    empathicThresholdText.text = GetThresholdBandText(empathicLevel);
            }
            else
            {
                // No character selected
                if (perceptionLevelText != null) perceptionLevelText.text = "Perception: --";
                if (interpretiveLevelText != null) interpretiveLevelText.text = "Interpretive: --";
                if (empathicLevelText != null) empathicLevelText.text = "Empathic: --";
            }
        }
        
        /// <summary>
        /// Refresh weapon proficiency display.
        /// </summary>
        private void RefreshProficiencyDisplay(CharacterState character)
        {
            if (proficiencySection == null || proficiencyListContainer == null)
            {
                return; // UI elements not set up yet
            }
            
            // Show/hide section based on whether character exists
            if (proficiencySection != null)
            {
                proficiencySection.SetActive(character != null || currentUnit != null);
            }
            
            if (character == null && currentUnit == null)
            {
                return;
            }
            
            // Get proficiencies from CharacterState or Unit
            SerializableDictionary<WeaponFamily, WeaponProficiency> proficiencies = null;
            if (character != null && character.WeaponProficiencies != null)
            {
                proficiencies = character.WeaponProficiencies;
            }
            else if (currentUnit != null && currentUnit.WeaponProficiencyManager != null)
            {
                var unitProfs = currentUnit.WeaponProficiencyManager.GetAllProficiencies();
                proficiencies = new SerializableDictionary<WeaponFamily, WeaponProficiency>();
                foreach (var kvp in unitProfs)
                {
                    proficiencies[kvp.Key] = kvp.Value;
                }
            }
            
            if (proficiencies == null || proficiencyListContainer == null)
            {
                return;
            }
            
            // Clear existing entries
            foreach (Transform child in proficiencyListContainer)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Display ALL weapon families, initializing Untrained if not present
            foreach (WeaponFamily family in System.Enum.GetValues(typeof(WeaponFamily)))
            {
                if (family == WeaponFamily.None) continue;
                
                // Get or create proficiency for this family
                WeaponProficiency proficiency = null;
                if (proficiencies != null && proficiencies.ContainsKey(family))
                {
                    proficiency = proficiencies[family];
                }
                
                // If no proficiency exists, create a default Untrained one for display
                if (proficiency == null)
                {
                    proficiency = new WeaponProficiency(family);
                }
                
                // Display this weapon family
                if (proficiencyEntryPrefab != null)
                {
                    GameObject entry = Instantiate(proficiencyEntryPrefab, proficiencyListContainer);
                    
                    // Find text components (flexible naming)
                    TMP_Text familyText = null;
                    TMP_Text tierText = null;
                    
                    // Try common naming patterns
                    Transform familyTransform = entry.transform.Find("WeaponFamilyText");
                    if (familyTransform == null) familyTransform = entry.transform.Find("FamilyText");
                    if (familyTransform == null) familyTransform = entry.transform.Find("WeaponText");
                    if (familyTransform != null) familyText = familyTransform.GetComponent<TMP_Text>();
                    
                    Transform tierTransform = entry.transform.Find("TierText");
                    if (tierTransform == null) tierTransform = entry.transform.Find("ProficiencyText");
                    if (tierTransform != null) tierText = tierTransform.GetComponent<TMP_Text>();
                    
                    // Fallback: get first two TMP_Text components
                    if (familyText == null || tierText == null)
                    {
                        TMP_Text[] texts = entry.GetComponentsInChildren<TMP_Text>();
                        if (texts.Length >= 1 && familyText == null) familyText = texts[0];
                        if (texts.Length >= 2 && tierText == null) tierText = texts[1];
                    }
                    
                    if (familyText != null)
                    {
                        familyText.text = GetWeaponFamilyDisplayName(family);
                    }
                    
                    if (tierText != null)
                    {
                        tierText.text = GetTierDisplayName(proficiency.CurrentTier);
                    }
                }
            }
            
            // Always show section if character exists
            if (proficiencySection != null)
            {
                proficiencySection.SetActive(character != null || currentUnit != null);
            }
        }
        
        /// <summary>
        /// Get display name for weapon family.
        /// </summary>
        private string GetWeaponFamilyDisplayName(WeaponFamily family)
        {
            switch (family)
            {
                case WeaponFamily.ShortBlade: return "Short Blades";
                case WeaponFamily.Sword: return "Swords";
                case WeaponFamily.HeavyBlade: return "Heavy Blades";
                case WeaponFamily.OneHandedBlunt: return "One-Handed Blunt";
                case WeaponFamily.TwoHandedBlunt: return "Two-Handed Blunt";
                case WeaponFamily.Spear: return "Spears";
                case WeaponFamily.Staff: return "Staves";
                case WeaponFamily.Polearm: return "Polearms";
                case WeaponFamily.Gloves: return "Unarmed";
                case WeaponFamily.Bows: return "Bows";
                case WeaponFamily.Crossbows: return "Crossbows";
                case WeaponFamily.Handguns: return "Handguns";
                case WeaponFamily.Rifles: return "Rifles";
                default: return family.ToString();
            }
        }
        
        /// <summary>
        /// Get display name for proficiency tier.
        /// </summary>
        private string GetTierDisplayName(WeaponProficiencyTier tier)
        {
            switch (tier)
            {
                case WeaponProficiencyTier.Untrained: return "Untrained";
                case WeaponProficiencyTier.Familiar: return "Familiar";
                case WeaponProficiencyTier.Trained: return "Trained";
                case WeaponProficiencyTier.Competent: return "Competent";
                case WeaponProficiencyTier.Proficient: return "Proficient";
                case WeaponProficiencyTier.Advanced: return "Advanced";
                case WeaponProficiencyTier.Expert: return "Expert";
                case WeaponProficiencyTier.Master: return "Master";
                case WeaponProficiencyTier.Grandmaster: return "Grandmaster";
                case WeaponProficiencyTier.Legendary: return "Legendary";
                default: return tier.ToString();
            }
        }
        
        /// <summary>
        /// Refresh party portraits at the top of the status tab.
        /// Uses 6 static slots - enables/populates active slots, disables empty ones.
        /// </summary>
        private void RefreshPartyPortraits()
        {
            if (partyPortraitSlots == null || partyPortraitSlots.Length != 6)
            {
                Debug.LogWarning("[StatusMenuUI] partyPortraitSlots array must have exactly 6 elements! Cannot display party portraits.");
                return;
            }
            
            // Clear existing mappings
            portraitToCharacterMap.Clear();
            
            // Get party members
            List<CharacterState> partyMembers = new List<CharacterState>();
            if (PartyManager.Instance != null)
            {
                partyMembers = PartyManager.Instance.GetPartyMembers();
            }
            
            int partyCount = partyMembers != null ? partyMembers.Count : 0;
            int portraitsToShow = Mathf.Min(partyCount, maxPartyPortraits);
            
            // Process each of the 6 static slots
            for (int i = 0; i < 6; i++)
            {
                GameObject slot = partyPortraitSlots[i];
                
                if (slot == null)
                {
                    Debug.LogWarning($"[StatusMenuUI] Party portrait slot {i} is null! Assign all 6 slots in Inspector.");
                    continue;
                }
                
                if (i < portraitsToShow && partyMembers[i] != null && partyMembers[i].Definition != null)
                {
                    // Slot has a party member - enable and populate
                    CharacterState character = partyMembers[i];
                    slot.SetActive(true);
                    SetupPortraitUI(slot, character, i == 0); // First character is selected by default
                    portraitToCharacterMap[slot] = character;
                }
                else
                {
                    // Slot is empty - disable it
                    slot.SetActive(false);
                }
            }
        }
        
        
        /// <summary>
        /// Setup portrait UI with character data and click handler.
        /// </summary>
        private void SetupPortraitUI(GameObject portraitObj, CharacterState character, bool isSelected)
        {
            if (portraitObj == null || character == null || character.Definition == null) return;
            
            // Ensure portrait has fixed size (prevents stretching from layout group)
            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
            if (portraitRect != null)
            {
                // Only set size if it's not already set (allows prefab to override)
                if (portraitRect.sizeDelta.x < 1 || portraitRect.sizeDelta.y < 1)
                {
                    portraitRect.sizeDelta = new Vector2(100, 100);
                }
                // Ensure anchors are centered to prevent stretching
                if (portraitRect.anchorMin != portraitRect.anchorMax)
                {
                    Vector2 centerAnchor = new Vector2(0.5f, 0.5f);
                    portraitRect.anchorMin = centerAnchor;
                    portraitRect.anchorMax = centerAnchor;
                    portraitRect.pivot = centerAnchor;
                }
            }
            
            // Find Image component for portrait
            Image portraitImage = portraitObj.GetComponent<Image>();
            if (portraitImage == null)
            {
                // Try to find in children
                portraitImage = portraitObj.GetComponentInChildren<Image>();
            }
            
            if (portraitImage != null)
            {
                // Ensure Image type is Simple and preserve aspect
                portraitImage.type = Image.Type.Simple;
                portraitImage.preserveAspect = true;
                
                if (character.Definition.Portrait != null)
                {
                    portraitImage.sprite = character.Definition.Portrait;
                }
            }
            
            // Find name text
            TMP_Text nameText = portraitObj.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = character.Definition.CharacterName;
            }
            
            // Add click handler
            Button button = portraitObj.GetComponent<Button>();
            if (button == null)
            {
                button = portraitObj.AddComponent<Button>();
            }
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnPortraitClicked(character));
            
            // Visual feedback for selected character
            UpdatePortraitSelection(portraitObj, isSelected);
        }
        
        /// <summary>
        /// Handle portrait click - switch displayed character.
        /// </summary>
        private void OnPortraitClicked(CharacterState character)
        {
            if (character == null) return;
            
            currentCharacterState = character;
            currentUnit = FindUnitForCharacterState(character);
            
            Debug.Log($"[StatusMenuUI] Selected character: {character.Definition.CharacterName}");
            
            // Update portrait selections
            if (partyPortraitSlots != null)
            {
                foreach (GameObject slot in partyPortraitSlots)
                {
                    if (slot != null && slot.activeSelf && portraitToCharacterMap.ContainsKey(slot))
                    {
                        bool isSelected = portraitToCharacterMap[slot] == character;
                        UpdatePortraitSelection(slot, isSelected);
                    }
                }
            }
            
            // Refresh status tab to show new character
            if (currentTab == TabType.Status)
            {
                RefreshStatusTab();
            }
        }
        
        /// <summary>
        /// Update visual state of portrait to show selection.
        /// </summary>
        private void UpdatePortraitSelection(GameObject portraitObj, bool isSelected)
        {
            if (portraitObj == null) return;
            
            Image portraitImage = portraitObj.GetComponent<Image>();
            if (portraitImage == null)
            {
                portraitImage = portraitObj.GetComponentInChildren<Image>();
            }
            
            if (portraitImage != null)
            {
                // Add selection border or highlight
                // You can customize this with a ColorBlock or add a border Image
                ColorBlock colors = new ColorBlock();
                colors.normalColor = isSelected ? Color.white : Color.gray;
                colors.highlightedColor = Color.yellow;
                colors.pressedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.disabledColor = Color.gray;
                colors.colorMultiplier = 1f;
                
                Button button = portraitObj.GetComponent<Button>();
                if (button != null)
                {
                    button.colors = colors;
                }
                
                // Optional: Add border or outline for selected
                if (isSelected)
                {
                    // You can add a border Image component here if desired
                }
            }
        }
        
        /// <summary>
        /// Refresh the Narrative Skills tab display.
        /// Uses POV character's narrative skills.
        /// DEV ONLY - Will be removed in production
        /// </summary>
        private void RefreshNarrativeSkillsTab()
        {
            // Prefer CharacterState (POV character) over Unit
            CharacterState displayCharacter = currentCharacterState;
            if (displayCharacter == null && PartyManager.Instance != null)
            {
                displayCharacter = PartyManager.Instance.POVCharacter;
            }

            if (displayCharacter != null)
            {
                // Perception
                int perceptionLevel = displayCharacter.GetNarrativeSkillLevel(NarrativeSkillCategory.Perception);
                if (perceptionLevelText != null)
                    perceptionLevelText.text = $"Perception: {perceptionLevel}";
                if (perceptionThresholdText != null)
                    perceptionThresholdText.text = GetThresholdBandText(perceptionLevel);
                if (perceptionDescriptionText != null)
                    perceptionDescriptionText.text = "Awareness, observation, pattern recognition. Adds metadata to locations, objects, NPC behaviors.";
                
                // Interpretive
                int interpretiveLevel = displayCharacter.GetNarrativeSkillLevel(NarrativeSkillCategory.Interpretive);
                if (interpretiveLevelText != null)
                    interpretiveLevelText.text = $"Interpretive: {interpretiveLevel}";
                if (interpretiveThresholdText != null)
                    interpretiveThresholdText.text = GetThresholdBandText(interpretiveLevel);
                if (interpretiveDescriptionText != null)
                    interpretiveDescriptionText.text = "Lore literacy, symbol decoding, cultural memory. Converts raw observations into meaning.";
                
                // Empathic
                int empathicLevel = displayCharacter.GetNarrativeSkillLevel(NarrativeSkillCategory.Empathic);
                if (empathicLevelText != null)
                    empathicLevelText.text = $"Empathic: {empathicLevel}";
                if (empathicThresholdText != null)
                    empathicThresholdText.text = GetThresholdBandText(empathicLevel);
                if (empathicDescriptionText != null)
                    empathicDescriptionText.text = "Social intuition, intent sense, emotional residue reading. Infers past motivations, tensions, or secrets.";
                
                return;
            }

            // Fallback to Unit (backward compatibility)
            if (currentUnit == null)
            {
                if (perceptionLevelText != null) perceptionLevelText.text = "No Character Selected";
                return;
            }
            
            // Perception
            int perceptionLevelFallback = currentUnit.GetNarrativeSkillLevel(NarrativeSkillCategory.Perception);
            if (perceptionLevelText != null)
                perceptionLevelText.text = $"Perception: {perceptionLevelFallback}";
            if (perceptionThresholdText != null)
                perceptionThresholdText.text = GetThresholdBandText(perceptionLevelFallback);
            if (perceptionDescriptionText != null)
                perceptionDescriptionText.text = "Awareness, observation, pattern recognition. Adds metadata to locations, objects, NPC behaviors.";
            
            // Interpretive
            int interpretiveLevelFallback = currentUnit.GetNarrativeSkillLevel(NarrativeSkillCategory.Interpretive);
            if (interpretiveLevelText != null)
                interpretiveLevelText.text = $"Interpretive: {interpretiveLevelFallback}";
            if (interpretiveThresholdText != null)
                interpretiveThresholdText.text = GetThresholdBandText(interpretiveLevelFallback);
            if (interpretiveDescriptionText != null)
                interpretiveDescriptionText.text = "Lore literacy, symbol decoding, cultural memory. Converts raw observations into meaning.";
            
            // Empathic
            int empathicLevelFallback = currentUnit.GetNarrativeSkillLevel(NarrativeSkillCategory.Empathic);
            if (empathicLevelText != null)
                empathicLevelText.text = $"Empathic: {empathicLevelFallback}";
            if (empathicThresholdText != null)
                empathicThresholdText.text = GetThresholdBandText(empathicLevelFallback);
            if (empathicDescriptionText != null)
                empathicDescriptionText.text = "Social intuition, intent sense, emotional residue reading. Infers past motivations, tensions, or secrets.";
        }
        
        /// <summary>
        /// Get threshold band text for a skill level.
        /// </summary>
        private string GetThresholdBandText(int level)
        {
            if (level >= 9)
                return "Threshold: Multiple Theories (9+)";
            if (level >= 6)
                return "Threshold: Structured Clue (6-8)";
            if (level >= 3)
                return "Threshold: Minimal Hint (3-5)";
            return "Threshold: None (0-2)";
        }
    }
}
