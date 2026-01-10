using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using Riftbourne.Characters;
using Riftbourne.Combat;
using Riftbourne.Skills;
using Riftbourne.Grid;
using Riftbourne.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.UI
{
    /// <summary>
    /// Hotbar-based action panel with Move/Attack/Skills/End Turn buttons.
    /// Uses Hotbar_1 through Hotbar_0 for actions and skills.
    /// </summary>
    public class ActionPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnManager turnManager;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text currentUnitText;
        [SerializeField] private TMP_Text movePointsDisplay;
        [SerializeField] private Button endTurnButton;

        [Header("Hotbar Buttons")]
        [Tooltip("Hotbar_1 through Hotbar_0 (10 buttons total). Assign in order: 1, 2, 3, 4, 5, 6, 7, 8, 9, 0")]
        [SerializeField] private Button[] hotbarButtons = new Button[10];

        [Header("Grid Reference")]
        [SerializeField] private GridManager gridManager;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSound;

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMP_Text tooltipTitleText;
        [SerializeField] private TMP_Text tooltipDescriptionText;
        [Tooltip("Fixed screen position for tooltip (0-1 range, e.g., (0.5, 0.5) = center, (0, 1) = top-left)")]
        [SerializeField] private Vector2 tooltipScreenPosition = new Vector2(0.5f, 0.1f); // Center-bottom by default

        // Hotbar slot assignments
        private enum HotbarActionType
        {
            None,
            Move,
            MeleeAttack,
            RangedAttack,
            Skill
        }

        private class HotbarSlot
        {
            public HotbarActionType actionType;
            public Skill assignedSkill;
            public Button button;
            public TMP_Text buttonText;
            public Text legacyText; // Support for legacy Text components
            public Image iconImage; // Cached icon image to avoid searching every frame

            public HotbarSlot(Button btn)
            {
                button = btn;
                actionType = HotbarActionType.None;
                assignedSkill = null;
                iconImage = null; // Will be cached after construction
                
                // Try to find text component in button or its children
                if (btn != null)
                {
                    buttonText = btn.GetComponentInChildren<TMP_Text>();
                    // Fallback to legacy Text component if TMP_Text not found
                    if (buttonText == null)
                    {
                        legacyText = btn.GetComponentInChildren<Text>();
                    }
                }
            }
        }

        private HotbarSlot[] hotbarSlots = new HotbarSlot[10];
        private Unit currentUnit;
        private CharacterMovementController currentController;
        private SkillTargetingController currentSkillController;
        private List<Skill> currentSkills = new List<Skill>();
        
        // Track currently active/selected button for visual feedback
        private int activeButtonIndex = -1;
        
        // Store original colors for restoration
        private Dictionary<Image, Color> originalIconColors = new Dictionary<Image, Color>();
        
        // Track which units have already had their hotbar populated (prevents infinite loops)
        private HashSet<Unit> populatedUnits = new HashSet<Unit>();

        private void Awake()
        {
            if (turnManager == null)
            {
                turnManager = ManagerRegistry.Get<TurnManager>();
            }

            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
                // Fallback to Instance if ManagerRegistry isn't ready yet
                if (gridManager == null)
                {
                    gridManager = GridManager.Instance;
                }
            }

            // Initialize audio source if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }

            // Initialize hotbar slots - ensure all slots are created
            for (int i = 0; i < 10; i++)
            {
                Button button = (i < hotbarButtons.Length) ? hotbarButtons[i] : null;
                hotbarSlots[i] = new HotbarSlot(button);
                
                // Cache icon images after slot creation (can't do this in constructor)
                if (hotbarSlots[i] != null && hotbarSlots[i].button != null)
                {
                    hotbarSlots[i].iconImage = GetIconImageCached(hotbarSlots[i].button);
                }
            }
        }

        private void Start()
        {
            // Setup hotbar button listeners
            SetupHotbarButtons();

            // Setup end turn button
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            }
        }

        private void SetupHotbarButtons()
        {
            // Initialize tooltip
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }

            // Hotbar_1 (index 0) - Move
            if (hotbarSlots[0] != null && hotbarSlots[0].button != null)
            {
                int index = 0; // Capture index for closure
                hotbarSlots[0].button.onClick.AddListener(() => HandleHotbarAction(index));
                hotbarSlots[0].actionType = HotbarActionType.Move;
                UpdateHotbarButtonText(0, "Move");
                SetupTooltipForButton(hotbarSlots[0].button, "Move", "Move your character to an adjacent cell. Uses movement points.");
            }

            // Hotbar_2 (index 1) - Melee Attack
            if (hotbarSlots[1] != null && hotbarSlots[1].button != null)
            {
                int index = 1; // Capture index for closure
                hotbarSlots[1].button.onClick.AddListener(() => HandleHotbarAction(index));
                hotbarSlots[1].actionType = HotbarActionType.MeleeAttack;
                UpdateHotbarButtonText(1, "Melee");
                SetupTooltipForButton(hotbarSlots[1].button, "Melee Attack", "Attack an adjacent enemy with your melee weapon.");
            }

            // Hotbar_3 (index 2) - Ranged Attack (placeholder)
            if (hotbarSlots[2] != null && hotbarSlots[2].button != null)
            {
                int index = 2; // Capture index for closure
                hotbarSlots[2].button.onClick.AddListener(() => HandleHotbarAction(index));
                hotbarSlots[2].actionType = HotbarActionType.RangedAttack;
                UpdateHotbarButtonText(2, "Ranged");
                SetupTooltipForButton(hotbarSlots[2].button, "Ranged Attack", "Attack an enemy at range with your ranged weapon.");
            }

            // Hotbar_4 through Hotbar_0 (indices 3-9) - Skills (will be assigned dynamically)
            for (int i = 3; i < 10; i++)
            {
                if (hotbarSlots[i] != null && hotbarSlots[i].button != null)
                {
                    int index = i; // Capture index for closure
                    hotbarSlots[i].button.onClick.AddListener(() => HandleHotbarAction(index));
                    hotbarSlots[i].actionType = HotbarActionType.None;
                    UpdateHotbarButtonText(i, "");
                    // Tooltip will be set dynamically when skills are assigned
                }
            }
        }

        private void Update()
        {
            // Ensure hotbar slots are initialized before updating
            if (hotbarSlots == null || hotbarSlots.Length != 10)
            {
                return; // Not initialized yet
            }

            UpdateCurrentUnit();
            // UpdateHotbarSkills() is now called only when unit changes (in UpdateCurrentUnit)
            UpdateButtonStates();
            UpdateActiveButtonVisual();
            
            // Update stats display every frame to show real-time HP/action changes
            if (currentUnit != null && currentUnitText != null)
            {
                UpdateUnitStatsDisplay();
            }

            // Update movement points display
            UpdateMovePointsDisplay();
        }

        /// <summary>
        /// Update active button visual feedback based on current game state.
        /// Clears selection if ranges are cleared or if skill targeting is cancelled.
        /// </summary>
        private void UpdateActiveButtonVisual()
        {
            if (activeButtonIndex < 0 || activeButtonIndex >= 10)
                return;

            // Check if we should clear the active button
            bool shouldClear = false;

            // Clear if skill targeting is no longer active (but we had a skill button selected)
            if (activeButtonIndex >= 3 && activeButtonIndex < 10)
            {
                // This is a skill button
                if (currentSkillController == null || !currentSkillController.AwaitingSkillTarget)
                {
                    // Skill targeting was cancelled or completed
                    shouldClear = true;
                }
            }
            // For move/attack buttons (0-2), keep them highlighted while ranges are shown
            // They'll be cleared when another button is clicked or when turn ends

            if (shouldClear)
            {
                ClearActiveButton();
            }
        }

        private void UpdateCurrentUnit()
        {
            // Track the SELECTED unit from PartyManager, not just the current turn unit
            Unit newUnit = null;
            
            if (PartyManager.Instance != null)
            {
                newUnit = PartyManager.Instance.SelectedUnit;
            }
            
            // Fallback to TurnManager's current unit if no selection
            if (newUnit == null && turnManager != null)
            {
                newUnit = turnManager.CurrentUnit;
            }

            if (newUnit != currentUnit)
            {
                // Unsubscribe from previous unit's equipment changes
                if (currentUnit != null && currentUnit.UnitEquipment != null)
                {
                    currentUnit.UnitEquipment.OnEquipmentChanged -= OnEquipmentChanged;
                }

                currentUnit = newUnit;
                currentController = currentUnit?.GetComponent<CharacterMovementController>();
                currentSkillController = currentUnit?.GetComponent<SkillTargetingController>();

                // Subscribe to new unit's equipment changes
                if (currentUnit != null && currentUnit.UnitEquipment != null)
                {
                    currentUnit.UnitEquipment.OnEquipmentChanged += OnEquipmentChanged;
                }

                // Clear active button when unit changes
                ClearActiveButton();

                // Update unit stats display
                if (currentUnitText != null && currentUnit != null)
                {
                    UpdateUnitStatsDisplay();
                }

                // Update skills when unit changes (only runs once per unit, tracked by populatedUnits)
                UpdateHotbarSkills();
                UpdateEquipmentIcons();
            }
        }

        /// <summary>
        /// Called when equipment changes on the current unit.
        /// Updates hotbar skills and equipment icons when equipment is equipped/unequipped.
        /// </summary>
        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentItem item)
        {
            // Only update if this is the current unit
            if (currentUnit != null)
            {
                UpdateHotbarSkills();
                UpdateEquipmentIcons();
            }
        }

        private void UpdateHotbarSkills()
        {
            if (currentUnit == null)
            {
                // Clear all skill slots
                for (int i = 3; i < 10; i++)
                {
                    if (hotbarSlots[i] != null)
                    {
                        hotbarSlots[i].actionType = HotbarActionType.None;
                        hotbarSlots[i].assignedSkill = null;
                        UpdateHotbarButtonText(i, "");
                    }
                }
                currentSkills.Clear();
                return;
            }

            // Only populate hotbar once per unit (at battle start or when unit first becomes current)
            // Allow re-population only if equipment changes (handled by OnEquipmentChanged)
            // This prevents infinite loops from repeated calls
            if (populatedUnits.Contains(currentUnit))
            {
                // Unit already populated - skip unless we're being called from equipment change
                // We'll check if skills actually changed below
            }
            else
            {
                // First time seeing this unit - mark as populated
                populatedUnits.Add(currentUnit);
            }

            // Get available skills
            List<Skill> availableSkills = currentUnit.GetAvailableSkills();

            // Check if skills have changed (compare by reference and count)
            bool skillsChanged = currentSkills.Count != availableSkills.Count;
            if (!skillsChanged)
            {
                for (int i = 0; i < availableSkills.Count; i++)
                {
                    if (i >= currentSkills.Count || currentSkills[i] != availableSkills[i])
                    {
                        skillsChanged = true;
                        break;
                    }
                }
            }
            
            // Only update if skills actually changed (prevents unnecessary updates)
            // OR if this is the first time we're populating for this unit
            if (!skillsChanged && populatedUnits.Contains(currentUnit))
            {
                return; // No changes and already populated, skip update
            }

            // Update the hotbar with current skills
            currentSkills = new List<Skill>(availableSkills);

            // Assign skills to hotbar slots 4-0 (indices 3-9)
            int skillIndex = 0;
            for (int i = 3; i < 10 && skillIndex < availableSkills.Count; i++)
            {
                if (hotbarSlots[i] != null)
                {
                    Skill skill = availableSkills[skillIndex];
                    hotbarSlots[i].actionType = HotbarActionType.Skill;
                    hotbarSlots[i].assignedSkill = skill;
                    UpdateHotbarButtonText(i, skill.SkillName);
                    
                    // Set skill icon if available
                    if (skill.Icon != null)
                    {
                        SetHotbarButtonIcon(i, skill.Icon);
                    }
                    else
                    {
                        SetHotbarButtonIcon(i, null);
                    }
                    
                    // Setup tooltip for skill
                    SetupTooltipForSkill(hotbarSlots[i].button, skill);
                    
                    skillIndex++;
                }
            }
            
            // Clear tooltips for unused skill slots
            for (int i = 3 + availableSkills.Count; i < 10; i++)
            {
                if (hotbarSlots[i] != null && hotbarSlots[i].button != null)
                {
                    ClearTooltipForButton(hotbarSlots[i].button);
                }
            }

            // Clear remaining slots
            for (int i = 3 + availableSkills.Count; i < 10; i++)
            {
                if (hotbarSlots[i] != null)
                {
                    hotbarSlots[i].actionType = HotbarActionType.None;
                    hotbarSlots[i].assignedSkill = null;
                    UpdateHotbarButtonText(i, "");
                    SetHotbarButtonIcon(i, null);
                }
            }
        }

        /// <summary>
        /// Updates equipment icons on melee and ranged attack hotbar buttons.
        /// Melee attack button shows equipped melee weapon icon, or unarmed icon if no weapon.
        /// Ranged attack button shows equipped ranged weapon icon, or remains in default state if no weapon.
        /// </summary>
        private void UpdateEquipmentIcons()
        {
            if (currentUnit == null)
            {
                // Clear equipment icons
                SetHotbarButtonIcon(1, null); // Melee attack
                SetHotbarButtonIcon(2, null); // Ranged attack
                return;
            }

            // Update melee attack button (index 1)
            // Priority: 1) Equipped melee weapon icon, 2) Unarmed icon fallback
            EquipmentItem meleeWeapon = currentUnit.GetEquippedItem(EquipmentSlot.MeleeWeapon);
            if (meleeWeapon != null && meleeWeapon.Icon != null)
            {
                // Show equipped weapon icon (highest priority)
                SetHotbarButtonIcon(1, meleeWeapon.Icon);
            }
            else
            {
                // Fallback: No melee weapon equipped, show unarmed icon
                SetHotbarButtonIcon(1, IconGenerator.GetUnarmedIcon());
            }

            // Update ranged attack button (index 2)
            // Priority: 1) Equipped ranged weapon icon, 2) Blank/default button
            EquipmentItem rangedWeapon = currentUnit.GetEquippedItem(EquipmentSlot.RangedWeapon);
            if (rangedWeapon != null && rangedWeapon.Icon != null)
            {
                // Show equipped weapon icon (highest priority)
                SetHotbarButtonIcon(2, rangedWeapon.Icon);
            }
            else
            {
                // Fallback: No ranged weapon equipped, clear icon (shows default button)
                SetHotbarButtonIcon(2, null);
            }
        }

        /// <summary>
        /// Sets the icon sprite for a hotbar button.
        /// </summary>
        private void SetHotbarButtonIcon(int slotIndex, Sprite iconSprite)
        {
            if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
                return;

            if (hotbarSlots[slotIndex] == null || hotbarSlots[slotIndex].button == null)
                return;

            Image iconImage = GetIconImage(hotbarSlots[slotIndex].button);
            if (iconImage != null)
            {
                iconImage.sprite = iconSprite;
                // Store original color if not already stored
                if (iconSprite != null && !originalIconColors.ContainsKey(iconImage))
                {
                    originalIconColors[iconImage] = iconImage.color;
                }
            }
        }

        private void UpdateButtonStates()
        {
            // Check if current unit is valid and in the active turn window
            bool canAct = false;
            
            if (currentUnit != null && currentUnit.IsPlayerControlled)
            {
                if (turnManager != null && turnManager.IsUnitInCurrentWindow(currentUnit))
                {
                    canAct = true;
                }
            }

            if (!canAct)
            {
                // Disable all hotbar buttons if not player's turn or not in window
                for (int i = 0; i < 10; i++)
                {
                    if (hotbarSlots[i] != null)
                    {
                        SetButtonInteractable(hotbarSlots[i].button, false);
                    }
                }
                SetButtonInteractable(endTurnButton, false);
                return;
            }

            // Check if unit is stunned - if so, disable all action buttons
            bool isStunned = currentUnit.IsStunned();
            
            // Update hotbar button states based on action economy
            // Hotbar_1 (Move) - Enable if has movement points and not stunned
            if (hotbarSlots[0] != null)
            {
                SetButtonInteractable(hotbarSlots[0].button, !isStunned && currentUnit.MovementPointsRemaining > 0);
            }

            // Hotbar_2 (Melee Attack) - Enable if hasn't acted and not stunned
            if (hotbarSlots[1] != null)
            {
                SetButtonInteractable(hotbarSlots[1].button, !isStunned && !currentUnit.HasActedThisTurn);
            }

            // Hotbar_3 (Ranged Attack) - Enable if hasn't acted, has ranged weapon, and not stunned
            if (hotbarSlots[2] != null)
            {
                bool hasRangedWeapon = currentUnit.AttackRange > 1;
                SetButtonInteractable(hotbarSlots[2].button, !isStunned && !currentUnit.HasActedThisTurn && hasRangedWeapon);
            }

            // Hotbar_4-0 (Skills) - Enable if hasn't acted, skill is assigned, and not stunned
            for (int i = 3; i < 10; i++)
            {
                if (hotbarSlots[i] != null)
                {
                    bool enabled = !isStunned && 
                                  !currentUnit.HasActedThisTurn && 
                                  hotbarSlots[i].actionType == HotbarActionType.Skill && 
                                  hotbarSlots[i].assignedSkill != null;
                    SetButtonInteractable(hotbarSlots[i].button, enabled);
                    
                    // Ensure empty slots are clearly disabled
                    if (hotbarSlots[i].actionType == HotbarActionType.None)
                    {
                        SetButtonInteractable(hotbarSlots[i].button, false);
                    }
                }
            }

            // EndTurn - Always enabled when it's player's turn
            SetButtonInteractable(endTurnButton, true);
        }

        /// <summary>
        /// Set button interactable state and apply visual greying when disabled.
        /// When disabled, buttons are visually greyed out to indicate they cannot be used.
        /// </summary>
        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button == null) return;

            button.interactable = interactable;

            // Apply explicit visual greying when disabled
            if (!interactable)
            {
                // Method 1: Use Unity's ColorBlock disabledColor (automatic greying)
                var colors = button.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grey with reduced alpha
                button.colors = colors;

                // Method 2: Also reduce alpha on button's target graphic for more visible greying
                Image targetImage = button.targetGraphic as Image;
                if (targetImage != null)
                {
                    Color greyedColor = targetImage.color;
                    greyedColor.a = 0.5f; // Reduce alpha to 50%
                    targetImage.color = greyedColor;
                }

                // Method 3: Grey out icon images as well (use cached icon from slot)
                Image iconImage = GetIconImage(button);
                if (iconImage != null)
                {
                    Color iconColor = iconImage.color;
                    iconColor.a = 0.5f; // Reduce alpha to 50%
                    iconImage.color = iconColor;
                }

                // Method 4: Grey out text
                if (hotbarSlots != null)
                {
                    for (int i = 0; i < hotbarSlots.Length; i++)
                    {
                        if (hotbarSlots[i] != null && hotbarSlots[i].button == button)
                        {
                            if (hotbarSlots[i].buttonText != null)
                            {
                                Color textColor = hotbarSlots[i].buttonText.color;
                                textColor.a = 0.5f; // Reduce alpha to 50%
                                hotbarSlots[i].buttonText.color = textColor;
                            }
                            else if (hotbarSlots[i].legacyText != null)
                            {
                                Color textColor = hotbarSlots[i].legacyText.color;
                                textColor.a = 0.5f; // Reduce alpha to 50%
                                hotbarSlots[i].legacyText.color = textColor;
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                // Restore normal appearance when enabled
                Image targetImage = button.targetGraphic as Image;
                if (targetImage != null)
                {
                    Color normalColor = targetImage.color;
                    normalColor.a = 1.0f; // Full alpha
                    targetImage.color = normalColor;
                }

                Image iconImage = GetIconImage(button);
                if (iconImage != null)
                {
                    // Restore original color if stored, otherwise use full alpha white
                    if (originalIconColors.ContainsKey(iconImage))
                    {
                        iconImage.color = originalIconColors[iconImage];
                    }
                    else
                    {
                        Color iconColor = iconImage.color;
                        iconColor.a = 1.0f; // Full alpha
                        iconImage.color = iconColor;
                    }
                }

                // Restore text color
                if (hotbarSlots != null)
                {
                    for (int i = 0; i < hotbarSlots.Length; i++)
                    {
                        if (hotbarSlots[i] != null && hotbarSlots[i].button == button)
                        {
                            if (hotbarSlots[i].buttonText != null)
                            {
                                Color textColor = hotbarSlots[i].buttonText.color;
                                textColor.a = 1.0f; // Full alpha
                                hotbarSlots[i].buttonText.color = textColor;
                            }
                            else if (hotbarSlots[i].legacyText != null)
                            {
                                Color textColor = hotbarSlots[i].legacyText.color;
                                textColor.a = 1.0f; // Full alpha
                                hotbarSlots[i].legacyText.color = textColor;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateHotbarButtonText(int index, string text)
        {
            if (index >= 0 && index < hotbarSlots.Length)
            {
                // Try TMP_Text first, then fallback to legacy Text
                if (hotbarSlots[index].buttonText != null)
                {
                    hotbarSlots[index].buttonText.text = text;
                }
                else if (hotbarSlots[index].legacyText != null)
                {
                    hotbarSlots[index].legacyText.text = text;
                }
            }
        }


        private void HandleHotbarAction(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 10 || currentUnit == null)
                return;

            if (hotbarSlots[slotIndex] == null)
            {
                Debug.LogWarning($"ActionPanelUI: Hotbar slot {slotIndex} is null");
                return;
            }

            HotbarSlot slot = hotbarSlots[slotIndex];

            // Cancel any active skill selection when switching actions
            if (currentSkillController != null && currentSkillController.AwaitingSkillTarget)
            {
                currentSkillController.CancelSkillSelection();
            }

            // Clear any range highlights from previous actions
            if (gridManager != null)
            {
                gridManager.ClearRangeHighlights();
            }

            // Play button click sound
            PlayButtonClickSound();

            // Update visual feedback - highlight the clicked button
            SetActiveButton(slotIndex);

            switch (slot.actionType)
            {
                case HotbarActionType.Move:
                    OnMoveButtonClicked();
                    break;

                case HotbarActionType.MeleeAttack:
                    OnMeleeAttackButtonClicked();
                    break;

                case HotbarActionType.RangedAttack:
                    OnRangedAttackButtonClicked();
                    break;

                case HotbarActionType.Skill:
                    if (slot.assignedSkill != null)
                    {
                        OnSkillButtonClicked(slot.assignedSkill);
                    }
                    break;

                case HotbarActionType.None:
                default:
                    // Empty slot - do nothing
                    ClearActiveButton();
                    break;
            }
        }

        /// <summary>
        /// Get the Image component that displays the icon (usually on a child GameObject).
        /// This version is used for caching during initialization - no debug logging.
        /// </summary>
        private Image GetIconImageCached(Button button)
        {
            if (button == null) return null;

            // First, try to find an Image component on a child GameObject named "Image"
            Transform imageChild = button.transform.Find("Image");
            if (imageChild != null)
            {
                Image childImage = imageChild.GetComponent<Image>();
                if (childImage != null)
                {
                    return childImage;
                }
            }

            // Try all children for Image components
            for (int i = 0; i < button.transform.childCount; i++)
            {
                Transform child = button.transform.GetChild(i);
                Image childImage = child.GetComponent<Image>();
                if (childImage != null)
                {
                    return childImage;
                }
            }

            // Fallback: Get the first Image component in children (excluding the button's own Image)
            Image[] allImages = button.GetComponentsInChildren<Image>(true);
            foreach (Image img in allImages)
            {
                // Skip the button's own Image (targetGraphic)
                if (img.gameObject != button.gameObject)
                {
                    return img;
                }
            }

            // Last resort: return the button's target graphic
            return button.targetGraphic as Image;
        }

        /// <summary>
        /// Get the Image component that displays the icon (usually on a child GameObject).
        /// Uses cached version from HotbarSlot if available.
        /// </summary>
        private Image GetIconImage(Button button)
        {
            if (button == null) return null;

            // Try to get cached icon image from hotbar slot
            if (hotbarSlots != null)
            {
                for (int i = 0; i < hotbarSlots.Length; i++)
                {
                    if (hotbarSlots[i] != null && hotbarSlots[i].button == button)
                    {
                        return hotbarSlots[i].iconImage;
                    }
                }
            }

            // Fallback to searching if not found in cache
            return GetIconImageCached(button);
        }

        /// <summary>
        /// Set the active button for visual feedback (highlighting).
        /// Uses scale transform and color change for visual feedback.
        /// </summary>
        private void SetActiveButton(int buttonIndex)
        {
            // Clear previous active button
            if (activeButtonIndex >= 0 && activeButtonIndex < 10 && hotbarSlots[activeButtonIndex] != null)
            {
                if (hotbarSlots[activeButtonIndex].button != null)
                {
                    ResetButtonVisual(hotbarSlots[activeButtonIndex].button);
                }
            }

            // Set new active button with highlight
            activeButtonIndex = buttonIndex;
            if (activeButtonIndex >= 0 && activeButtonIndex < 10 && hotbarSlots[activeButtonIndex] != null)
            {
                if (hotbarSlots[activeButtonIndex].button != null)
                {
                    HighlightButton(hotbarSlots[activeButtonIndex].button);
                    Debug.Log($"Set active button: Hotbar slot {buttonIndex}");
                }
            }
        }

        /// <summary>
        /// Highlight a button with visual feedback (color and scale).
        /// </summary>
        private void HighlightButton(Button button)
        {
            if (button == null)
            {
                Debug.LogWarning("HighlightButton: button is null");
                return;
            }

            // Method 1: Change the button's selected color in its color block
            var colors = button.colors;
            colors.selectedColor = new Color(1f, 0.9f, 0.3f, 1f); // Bright yellow/gold
            button.colors = colors;
            button.Select(); // Set button to selected state
            Debug.Log($"[VISUAL] Set button '{button.name}' to selected state with highlight color");

            // Method 2: Also try to change icon color directly (in case transitions are disabled)
            Image iconImage = GetIconImage(button);
            if (iconImage != null)
            {
                // Store original color if not already stored
                if (!originalIconColors.ContainsKey(iconImage))
                {
                    originalIconColors[iconImage] = iconImage.color;
                }

                // Set highlight color directly - use a more vibrant color
                Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f); // Bright yellow/gold
                iconImage.color = highlightColor;
            }

            // Method 3: Add scale effect for more visible feedback
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = new Vector3(1.15f, 1.15f, 1f); // Slightly larger for more visibility
                Debug.Log($"[VISUAL] Scaled button '{button.name}' to 1.15x");
            }
        }

        /// <summary>
        /// Reset button visual to normal state.
        /// </summary>
        private void ResetButtonVisual(Button button)
        {
            if (button == null) return;

            // Deselect the button
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            
            // Reset button colors
            var colors = button.colors;
            colors.selectedColor = colors.normalColor; // Reset to normal
            button.colors = colors;

            // Reset icon color
            Image iconImage = GetIconImage(button);
            if (iconImage != null)
            {
                if (originalIconColors.ContainsKey(iconImage))
                {
                    iconImage.color = originalIconColors[iconImage];
                }
                else
                {
                    iconImage.color = Color.white;
                }
            }

            // Reset scale
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one; // Normal size
            }
        }

        /// <summary>
        /// Clear the active button visual feedback.
        /// </summary>
        private void ClearActiveButton()
        {
            if (activeButtonIndex >= 0 && activeButtonIndex < 10 && hotbarSlots[activeButtonIndex] != null)
            {
                if (hotbarSlots[activeButtonIndex].button != null)
                {
                    ResetButtonVisual(hotbarSlots[activeButtonIndex].button);
                }
            }
            activeButtonIndex = -1;
        }

        // Action handlers
        private void OnMoveButtonClicked()
        {
            Debug.Log("Move button clicked - Click a valid cell to move");

            if (currentUnit == null)
            {
                Debug.LogWarning("ActionPanelUI: Cannot show movement range - currentUnit is null");
                return;
            }

            if (gridManager == null)
            {
                Debug.LogWarning("ActionPanelUI: Cannot show movement range - gridManager is null");
                return;
            }

            // Use pathfinding-aware ShowMovementRange
            gridManager.ShowMovementRange(
                currentUnit,  // Pass the unit for pathfinding
                currentUnit.MovementPointsRemaining  // Show remaining, not max
            );
            Debug.Log($"ActionPanelUI: Showing movement range for {currentUnit.UnitName} with {currentUnit.MovementPointsRemaining} movement points");
        }

        private void OnMeleeAttackButtonClicked()
        {
            Debug.Log("Melee attack button clicked - Click an adjacent enemy to attack");

            if (currentUnit == null)
            {
                Debug.LogWarning("ActionPanelUI: Cannot show attack range - currentUnit is null");
                return;
            }

            if (gridManager == null)
            {
                Debug.LogWarning("ActionPanelUI: Cannot show attack range - gridManager is null");
                return;
            }

            gridManager.ShowAttackRange(
                currentUnit.GridX,
                currentUnit.GridY,
                1 // Melee range = 1
            );
            Debug.Log($"ActionPanelUI: Showing melee attack range at ({currentUnit.GridX}, {currentUnit.GridY})");
        }

        private void OnRangedAttackButtonClicked()
        {
            Debug.Log("Ranged attack button clicked - Click an enemy in range to attack");

            if (currentUnit == null)
            {
                Debug.LogWarning("ActionPanelUI: Cannot show ranged attack range - currentUnit is null");
                return;
            }

            if (gridManager == null)
            {
                Debug.LogWarning("ActionPanelUI: Cannot show ranged attack range - gridManager is null");
                return;
            }

            // Check if unit has a ranged weapon
            if (currentUnit.AttackRange <= 1)
            {
                Debug.LogWarning($"ActionPanelUI: {currentUnit.UnitName} does not have a ranged weapon equipped!");
                return;
            }

            gridManager.ShowAttackRange(
                currentUnit.GridX,
                currentUnit.GridY,
                currentUnit.AttackRange
            );
            Debug.Log($"ActionPanelUI: Showing ranged attack range at ({currentUnit.GridX}, {currentUnit.GridY}) with range {currentUnit.AttackRange}");
        }

        private void OnSkillButtonClicked(Skill skill)
        {
            if (currentSkillController == null)
            {
                Debug.LogWarning($"ActionPanelUI: Cannot use skill {skill?.SkillName} - currentSkillController is null");
                return;
            }

            if (skill == null)
            {
                Debug.LogWarning("ActionPanelUI: Cannot use skill - skill is null");
                return;
            }

            Debug.Log($"Skill button clicked: {skill.SkillName}");
            // Call skill targeting controller's skill selection (this will show the skill range)
            currentSkillController.SelectSkillFromUI(skill);
        }

        private void OnEndTurnButtonClicked()
        {
            if (turnManager != null)
            {
                Debug.Log("End Turn button clicked");

                // Play button click sound
                PlayButtonClickSound();

                // Clear range highlights when ending turn
                if (gridManager != null)
                {
                    gridManager.ClearRangeHighlights();
                }

                // Clear active button visual feedback
                ClearActiveButton();

                turnManager.EndTurn();
            }
        }

        /// <summary>
        /// Update the unit stats display text with HP and action economy.
        /// </summary>
        private void UpdateUnitStatsDisplay()
        {
            if (currentUnit == null || currentUnitText == null) return;

            // Build status text
            string statusText = $"<b>{currentUnit.UnitName}</b> - Lv.{currentUnit.Level}\n";
            statusText += $"HP: {currentUnit.CurrentHP}/{currentUnit.MaxHP}";
            
            // Show XP progress
            int xpNeeded = currentUnit.GetXPRequiredForNextLevel();
            statusText += $" | XP: {currentUnit.CurrentXP}/{xpNeeded}";
            
            // Show SP if any
            if (currentUnit.SkillPoints > 0)
            {
                statusText += $" | SP: {currentUnit.SkillPoints}";
            }
            
            // Show movement points and action status
            statusText += $"\nMove: {currentUnit.MovementPointsRemaining}";
            
            string actStatus = currentUnit.HasActedThisTurn ? "<color=red>Used</color>" : "<color=green>Ready</color>";
            statusText += $"  |  Action: {actStatus}";

            currentUnitText.text = statusText;
        }

        /// <summary>
        /// Update the movement points display with format "remaining / max".
        /// </summary>
        private void UpdateMovePointsDisplay()
        {
            if (movePointsDisplay == null) return;

            if (currentUnit == null)
            {
                movePointsDisplay.text = "- / -";
                return;
            }

            int remaining = currentUnit.MovementPointsRemaining;
            int max = currentUnit.MaxMovementPoints;
            movePointsDisplay.text = $"{remaining} / {max}";
        }

        /// <summary>
        /// Play button click sound effect.
        /// </summary>
        private void PlayButtonClickSound()
        {
            if (audioSource != null && buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }

        #region Tooltip System

        /// <summary>
        /// Setup tooltip for a button with static action information.
        /// </summary>
        private void SetupTooltipForButton(Button button, string title, string description)
        {
            if (button == null) return;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }
            else
            {
                // Remove existing entries to avoid duplicates
                trigger.triggers.Clear();
            }

            // Pointer Enter
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { ShowTooltip(title, description, button); });
            trigger.triggers.Add(pointerEnter);

            // Pointer Exit
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { HideTooltip(); });
            trigger.triggers.Add(pointerExit);
        }

        /// <summary>
        /// Setup tooltip for a skill button.
        /// </summary>
        private void SetupTooltipForSkill(Button button, Skill skill)
        {
            if (button == null || skill == null) return;

            // Build skill description
            string description = skill.Description;
            
            // Add skill details
            if (!string.IsNullOrEmpty(description))
            {
                description += "\n\n";
            }
            
            description += $"Range: {skill.Range}";
            
            if (skill.AOEType != AOEType.None && skill.AOEPattern != AOEPatternType.None)
            {
                description += $" | AOE: {skill.AOEPattern} ({skill.AOESize})";
            }
            
            if (skill.BaseDamage > 0)
            {
                description += $"\nDamage: {skill.BaseDamage}";
            }
            
            if (skill.AppliesStatusEffect && skill.StatusEffectData != null)
            {
                description += $"\nEffect: {skill.StatusEffectData.EffectName}";
            }
            
            if (skill.StunChance > 0)
            {
                description += $"\nStun: {skill.StunChance}% chance";
            }

            SetupTooltipForButton(button, skill.SkillName, description);
        }

        /// <summary>
        /// Clear tooltip for a button.
        /// </summary>
        private void ClearTooltipForButton(Button button)
        {
            if (button == null) return;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
            }
        }

        /// <summary>
        /// Show the tooltip with title and description.
        /// </summary>
        private void ShowTooltip(string title, string description, Button sourceButton)
        {
            if (tooltipPanel == null) return;

            if (tooltipTitleText != null)
            {
                tooltipTitleText.text = title;
            }

            if (tooltipDescriptionText != null)
            {
                tooltipDescriptionText.text = description;
            }

            tooltipPanel.SetActive(true);

            // Position tooltip at fixed screen position
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect == null) return;

            // Find or create a root canvas for tooltips (one that covers the entire screen)
            Canvas rootCanvas = FindOrCreateTooltipCanvas();
            
            // Ensure tooltip is a child of the root canvas
            if (rootCanvas != null && tooltipRect.parent != rootCanvas.transform)
            {
                tooltipRect.SetParent(rootCanvas.transform, false);
            }

            // Position tooltip at fixed screen position (0-1 range)
            RectTransform canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
            if (canvasRect != null)
            {
                // Convert screen position (0-1) to local position
                // tooltipScreenPosition: (0,0) = bottom-left, (1,1) = top-right
                Vector2 anchorPosition = new Vector2(
                    tooltipScreenPosition.x * canvasRect.rect.width - canvasRect.rect.width * 0.5f,
                    tooltipScreenPosition.y * canvasRect.rect.height - canvasRect.rect.height * 0.5f
                );
                
                tooltipRect.localPosition = anchorPosition;
                
                // Set anchors to center for easier positioning
                tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
                tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
                tooltipRect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>
        /// Find the root canvas or create a tooltip canvas that covers the entire screen.
        /// </summary>
        private Canvas FindOrCreateTooltipCanvas()
        {
            // First, try to find the root canvas
            Canvas existingCanvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (existingCanvas != null)
            {
                // Traverse up to find the root canvas
                Canvas rootCanvas = existingCanvas;
                int maxDepth = 10; // Safety limit to prevent infinite loops
                int depth = 0;
                
                while (rootCanvas.transform.parent != null && depth < maxDepth)
                {
                    depth++;
                    Canvas parentCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
                    if (parentCanvas != null && parentCanvas != rootCanvas)
                    {
                        rootCanvas = parentCanvas;
                    }
                    else
                    {
                        break;
                    }
                }
                
                if (depth >= maxDepth)
                {
                    Debug.LogWarning("FindOrCreateTooltipCanvas: Reached max depth while traversing canvas hierarchy!");
                }
                
                // If root canvas covers the screen, use it
                if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay || 
                    rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    return rootCanvas;
                }
            }

            // If no suitable canvas found, look for a canvas named "TooltipCanvas" or create one
            GameObject tooltipCanvasObj = GameObject.Find("TooltipCanvas");
            if (tooltipCanvasObj != null)
            {
                Canvas tooltipCanvas = tooltipCanvasObj.GetComponent<Canvas>();
                if (tooltipCanvas != null)
                {
                    return tooltipCanvas;
                }
            }

            // Create a new tooltip canvas that covers the entire screen
            GameObject newCanvasObj = new GameObject("TooltipCanvas");
            Canvas newCanvas = newCanvasObj.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 1000; // High sorting order to appear on top
            
            CanvasScaler scaler = newCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            newCanvasObj.AddComponent<GraphicRaycaster>();
            
            // Make it cover the entire screen
            RectTransform canvasRect = newCanvasObj.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;
            canvasRect.anchoredPosition = Vector2.zero;

            return newCanvas;
        }


        /// <summary>
        /// Hide the tooltip.
        /// </summary>
        private void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        #endregion
    }
}
