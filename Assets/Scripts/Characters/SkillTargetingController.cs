using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Grid;
using Riftbourne.Combat;
using Riftbourne.Skills;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles skill selection and targeting for a unit.
    /// Separated from movement controller for better separation of concerns.
    /// </summary>
    public class SkillTargetingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Unit unit;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TurnManager turnManager;

        // Skill selection state
        private Skill selectedSkill = null;
        private bool awaitingSkillTarget = false;

        // Camera service reference
        private CameraService cameraService;
        
        // AOE preview state
        private int lastHoverX = -1;
        private int lastHoverY = -1;

        // Public properties
        public bool IsSkillSelected => selectedSkill != null;
        public bool AwaitingSkillTarget => awaitingSkillTarget;

        private void Awake()
        {
            unit = GetComponent<Unit>();
        }

        private void Start()
        {
            if (gridManager == null)
            {
                gridManager = ManagerRegistry.Get<GridManager>();
            }

            if (turnManager == null)
            {
                turnManager = ManagerRegistry.Get<TurnManager>();
            }

            // Get camera service
            cameraService = CameraService.Instance;
        }

        private void Update()
        {
            // Only process input if this unit is the currently selected party member
            if (PartyManager.Instance == null || PartyManager.Instance.SelectedUnit != unit)
            {
                return;
            }

            // Only process input if this unit is in the current turn window
            if (turnManager == null || !turnManager.IsUnitInCurrentWindow(unit))
            {
                return;
            }

            // Number key selection (1-9) when skill menu is open
            if (awaitingSkillTarget && Keyboard.current != null)
            {
                List<Skill> availableSkills = unit.GetAvailableSkills();

                if (Keyboard.current.digit1Key.wasPressedThisFrame && availableSkills.Count >= 1)
                    SelectSkill(availableSkills[0]);
                if (Keyboard.current.digit2Key.wasPressedThisFrame && availableSkills.Count >= 2)
                    SelectSkill(availableSkills[1]);
                if (Keyboard.current.digit3Key.wasPressedThisFrame && availableSkills.Count >= 3)
                    SelectSkill(availableSkills[2]);
                if (Keyboard.current.digit4Key.wasPressedThisFrame && availableSkills.Count >= 4)
                    SelectSkill(availableSkills[3]);
                if (Keyboard.current.digit5Key.wasPressedThisFrame && availableSkills.Count >= 5)
                    SelectSkill(availableSkills[4]);
            }

            // RIGHT CLICK - Skill selection menu
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleRightClick();
            }

            // ESC to cancel skill selection
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelSkillSelection();
            }

            // Update AOE preview when hovering over potential targets
            if (awaitingSkillTarget && selectedSkill != null)
            {
                UpdateAOEPreview();
            }
        }

        /// <summary>
        /// Handle skill targeting when left clicking during skill selection.
        /// Called by CharacterMovementController when a skill is selected.
        /// </summary>
        public void HandleSkillTargeting(int targetX, int targetY, Unit targetUnit)
        {
            if (!awaitingSkillTarget || selectedSkill == null)
                return;

            UseSelectedSkill(targetX, targetY, targetUnit);
        }

        /// <summary>
        /// Public method for UI to trigger skill selection.
        /// </summary>
        public void SelectSkillFromUI(Skill skill)
        {
            SelectSkill(skill);
        }

        private void SelectSkill(Skill skill)
        {
            selectedSkill = skill;
            awaitingSkillTarget = true;
            lastHoverX = -1;
            lastHoverY = -1;
            Debug.Log($">>> Selected: {skill.SkillName} - Click target to use (ESC to cancel) <<<");
            
            // Show skill range visualization
            if (gridManager != null && unit != null)
            {
                // Validate unit's grid position before showing range
                if (gridManager.IsValidGridPosition(unit.GridX, unit.GridY))
                {
                    // If AOE skill, show initial AOE pattern preview in a default direction
                    if (skill.AOEType != AOEType.None && skill.AOEPattern != AOEPatternType.None)
                    {
                        // Show initial AOE pattern in a default direction (e.g., east)
                        int defaultTargetX = unit.GridX + Mathf.Min(skill.Range, 3);
                        int defaultTargetY = unit.GridY;
                        gridManager.ShowAOEPattern(skill, unit.GridX, unit.GridY, defaultTargetX, defaultTargetY);
                        Debug.Log($"Showing AOE skill pattern: {skill.AOEPattern} (Range: {skill.Range})");
                    }
                    else
                    {
                        gridManager.ShowSkillRange(unit.GridX, unit.GridY, skill.Range);
                        Debug.Log($"Showing skill range: {skill.Range} (Manhattan distance)");
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot show skill range - unit {unit.UnitName} has invalid grid position ({unit.GridX}, {unit.GridY})");
                }
            }
        }

        private void HandleRightClick()
        {
            // Show skill selection menu
            List<Skill> availableSkills = unit.GetAvailableSkills();

            if (availableSkills.Count == 0)
            {
                Debug.Log("No skills available!");
                return;
            }

            // Show menu and wait for number key press
            Debug.Log("=== AVAILABLE SKILLS ===");
            for (int i = 0; i < availableSkills.Count; i++)
            {
                string groundTarget = availableSkills[i].CreatesGroundHazard ? " [GROUND]" : " [UNIT]";
                Debug.Log($"Press [{i + 1}]: {availableSkills[i].SkillName}{groundTarget} (Range: {availableSkills[i].Range})");
            }
            Debug.Log("ESC to cancel.");

            // Set flag to listen for number keys
            awaitingSkillTarget = true;
            selectedSkill = null; // Don't auto-select!
        }

        private void UseSelectedSkill(int targetX, int targetY, Unit targetUnit)
        {
            // Validate selected skill
            if (selectedSkill == null)
            {
                Debug.LogError("UseSelectedSkill: No skill selected!");
                return;
            }

            // Check if already acted this turn
            if (unit.HasActedThisTurn)
            {
                Debug.Log("You have already acted this turn!");
                return;
            }

            bool success = false;
            SkillExecutor skillExecutor = ManagerRegistry.Get<SkillExecutor>();
            
            if (skillExecutor == null)
            {
                Debug.LogError("SkillExecutor not found!");
                return;
            }

            // Check if this is an AOE skill
            bool isAOE = selectedSkill.AOEType != AOEType.None && selectedSkill.AOEPattern != AOEPatternType.None;

            // AOE skills can target ground or units
            if (isAOE)
            {
                if (selectedSkill.AOEType == AOEType.TrueAOE)
                {
                    // True AOE - can target any location (ground or unit position)
                    if (targetUnit != null)
                    {
                        // Target unit - use unit's position for AOE center
                        success = skillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
                    }
                    else
                    {
                        // Target ground - use ground skill execution
                        success = skillExecutor.ExecuteGroundSkill(selectedSkill, unit, targetX, targetY);
                    }
                }
                else // FromSource
                {
                    // FromSource AOE - can target ground or units (uses target position to determine direction)
                    if (targetUnit != null && targetUnit != unit)
                    {
                        // Target unit - use unit's position for direction
                        success = skillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
                    }
                    else
                    {
                        // Target ground - use ground position for direction
                        success = skillExecutor.ExecuteGroundSkill(selectedSkill, unit, targetX, targetY);
                    }
                }
            }
            // Ground-targeted skill (Ignite Ground) - can target empty cells or occupied cells
            else if (selectedSkill.CreatesGroundHazard)
            {
                // Ground hazards can be placed on any valid cell (empty or occupied)
                success = skillExecutor.ExecuteGroundSkill(selectedSkill, unit, targetX, targetY);
            }
            // Unit-targeted skill (Spark) - requires a valid unit target
            else if (targetUnit != null && targetUnit != unit)
            {
                success = skillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
            }
            else
            {
                Debug.Log($"{selectedSkill.SkillName} requires a valid target!");
            }

            if (success)
            {
                // Clear skill range visualization after successful use
                if (gridManager != null)
                {
                    gridManager.ClearRangeHighlights();
                }
                CancelSkillSelection();
                // Don't end turn automatically - let player decide!
            }
        }

        /// <summary>
        /// Public method for clicking enemies to use skills on them.
        /// </summary>
        public void UseSkillOnTarget(Unit targetUnit)
        {
            if (selectedSkill == null || !awaitingSkillTarget)
            {
                Debug.LogWarning("No skill selected!");
                return;
            }

            if (unit.HasActedThisTurn)
            {
                Debug.Log("You have already acted this turn!");
                return;
            }

            if (selectedSkill.CreatesGroundHazard)
            {
                Debug.Log($"{selectedSkill.SkillName} requires ground target, not unit target!");
                return;
            }

            SkillExecutor skillExecutor = ManagerRegistry.Get<SkillExecutor>();
            if (skillExecutor != null)
            {
                bool success = skillExecutor.ExecuteSkill(selectedSkill, unit, targetUnit);
                if (success)
                {
                    // Clear skill range visualization after successful use
                    if (gridManager != null)
                    {
                        gridManager.ClearRangeHighlights();
                    }
                    CancelSkillSelection();
                }
            }
        }

        /// <summary>
        /// Update AOE pattern preview when hovering over potential targets.
        /// Updates dynamically as mouse moves to show AOE pattern in different directions.
        /// </summary>
        private void UpdateAOEPreview()
        {
            if (selectedSkill == null || gridManager == null || unit == null) return;

            // Only show AOE preview for AOE skills
            if (selectedSkill.AOEType == AOEType.None || selectedSkill.AOEPattern == AOEPatternType.None)
            {
                return;
            }

            // Get camera for raycasting
            Camera cam = null;
            if (cameraService != null && cameraService.MainCamera != null)
            {
                cam = cameraService.MainCamera;
            }
            else
            {
                cam = Camera.main;
            }

            if (cam == null || Mouse.current == null) return;

            // Raycast to find what we're hovering over
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            int hoverX = -1;
            int hoverY = -1;
            bool validHover = false;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                Vector3 hitPoint = hit.point;
                hoverX = Mathf.FloorToInt(hitPoint.x);
                hoverY = Mathf.FloorToInt(hitPoint.z);

                // Check if we hit a unit
                Unit hoveredUnit = hit.collider.GetComponent<Unit>();
                if (hoveredUnit != null)
                {
                    hoverX = hoveredUnit.GridX;
                    hoverY = hoveredUnit.GridY;
                }

                // Check if hover position is valid and in range
                if (gridManager.IsValidGridPosition(hoverX, hoverY))
                {
                    int distance = Mathf.Abs(hoverX - unit.GridX) + Mathf.Abs(hoverY - unit.GridY);
                    
                    if (distance <= selectedSkill.Range && distance > 0)
                    {
                        validHover = true;
                    }
                }
            }

            // Always update AOE pattern if hovering over a valid target (even if position hasn't changed)
            // This ensures the pattern stays visible and updates smoothly
            if (validHover)
            {
                // Update AOE pattern at hover position (updates even if same position for smooth display)
                gridManager.ShowAOEPattern(selectedSkill, unit.GridX, unit.GridY, hoverX, hoverY);
                lastHoverX = hoverX;
                lastHoverY = hoverY;
            }
            else
            {
                // Not hovering over valid target - show default AOE pattern in a default direction
                // This keeps the AOE visualization active even when not hovering over a target
                if (lastHoverX == -1 || lastHoverY == -1)
                {
                    // Show default direction AOE pattern
                    int defaultTargetX = unit.GridX + Mathf.Min(selectedSkill.Range, 3);
                    int defaultTargetY = unit.GridY;
                    gridManager.ShowAOEPattern(selectedSkill, unit.GridX, unit.GridY, defaultTargetX, defaultTargetY);
                }
            }
        }

        /// <summary>
        /// Cancel skill selection and clear any targeting state.
        /// Public so UI can cancel skill selection when switching actions.
        /// </summary>
        public void CancelSkillSelection()
        {
            if (awaitingSkillTarget)
            {
                Debug.Log("Skill selection cancelled");
            }
            selectedSkill = null;
            awaitingSkillTarget = false;
            lastHoverX = -1;
            lastHoverY = -1;
            
            // Clear skill range visualization
            if (gridManager != null)
            {
                gridManager.ClearRangeHighlights();
            }
        }
    }
}

