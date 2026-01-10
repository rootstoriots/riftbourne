using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Core;
using Riftbourne.Combat.AI;
using Riftbourne.Skills;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Controls AI decision-making for enemy units.
    /// Makes tactical choices about movement, targeting, and actions.
    /// Uses AI behavior strategies for flexible, configurable AI.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float thinkingDelay = 0.5f; // Visual delay so player sees AI "thinking"
        [SerializeField] private AIBehaviorData behaviorData; // Behavior configuration (if null, defaults to Berserker)

        /// <summary>
        /// Event raised when the AI turn is complete.
        /// Fired before EndTurn is called on TurnManager.
        /// </summary>
        public event Action<Unit> OnTurnComplete;

        private Unit controlledUnit;
        private GridManager gridManager;
        private HazardManager hazardManager;
        private TurnManager turnManager;
        private FactionRelationship factionRelationship;
        private AIBehavior currentBehavior; // Current behavior strategy
        private int lastMovementCost = 0; // Track movement cost for the current move
        private Coroutine executeTurnCoroutine; // Track the coroutine so we can cancel it if needed
        private bool wasInWindowAtStart = false; // Cache window state at start of turn execution

        private void Awake()
        {
            controlledUnit = GetComponent<Unit>();
        }

        private void Start()
        {
            // Fetch managers in Start() to ensure they're registered (Awake order is not guaranteed)
            gridManager = ManagerRegistry.Get<GridManager>();
            hazardManager = ManagerRegistry.Get<HazardManager>();
            turnManager = ManagerRegistry.Get<TurnManager>();
            factionRelationship = FactionRelationship.Instance ?? FindFirstObjectByType<FactionRelationship>();
            
            if (turnManager == null)
            {
                Debug.LogError($"[AI] {gameObject.name}: TurnManager not found in ManagerRegistry! Make sure TurnManager exists in the scene and registers itself.");
            }
            if (gridManager == null)
            {
                Debug.LogError($"[AI] {gameObject.name}: GridManager not found in ManagerRegistry! Make sure GridManager exists in the scene.");
            }
            if (factionRelationship == null)
            {
                Debug.LogWarning($"[AI] {gameObject.name}: FactionRelationship not found! Creating default instance.");
                GameObject go = new GameObject("FactionRelationship");
                factionRelationship = go.AddComponent<FactionRelationship>();
            }

            // Initialize AI behavior
            InitializeBehavior();
        }

        /// <summary>
        /// Initialize the AI behavior based on behaviorData or default to Berserker.
        /// </summary>
        private void InitializeBehavior()
        {
            AIBehaviorType behaviorType = AIBehaviorType.Berserker;
            AIBehaviorData data = behaviorData;

            if (data != null)
            {
                behaviorType = data.BehaviorType;
            }
            else
            {
                // Create default behavior data if none assigned
                Debug.LogWarning($"[AI] {controlledUnit?.UnitName ?? "Unit"}: No AIBehaviorData assigned, using default Berserker behavior");
            }

            // Create behavior instance based on type
            switch (behaviorType)
            {
                case AIBehaviorType.Berserker:
                    currentBehavior = new BerserkerBehavior(data);
                    break;
                case AIBehaviorType.Support:
                    currentBehavior = new SupportBehavior(data);
                    break;
                case AIBehaviorType.Coward:
                    currentBehavior = new CowardBehavior(data);
                    break;
                case AIBehaviorType.Protector:
                    currentBehavior = new ProtectorBehavior(data);
                    break;
                default:
                    Debug.LogWarning($"[AI] Unknown behavior type {behaviorType}, defaulting to Berserker");
                    currentBehavior = new BerserkerBehavior(data);
                    break;
            }

            // Initialize behavior with required references
            if (currentBehavior != null && controlledUnit != null)
            {
                currentBehavior.Initialize(controlledUnit, gridManager, hazardManager, factionRelationship);
            }
        }

        /// <summary>
        /// Main AI decision-making entry point.
        /// Called by TurnManager when it's this AI's turn.
        /// </summary>
        public void TakeTurn()
        {
            if (controlledUnit == null || !controlledUnit.IsAlive)
            {
                Debug.LogWarning("AIController: Unit is null or dead, skipping turn");
                return;
            }

            // Cancel any existing turn coroutine to prevent stacking
            CancelTurn();

            Debug.Log($"[AI] {controlledUnit.UnitName} is thinking...");

            // Use coroutine instead of Invoke for better control and cancellation
            executeTurnCoroutine = StartCoroutine(ExecuteAITurnWithDelay());
        }

        /// <summary>
        /// Cancel the current AI turn if one is in progress.
        /// Called by TurnManager or when unit is disabled.
        /// </summary>
        public void CancelTurn()
        {
            if (executeTurnCoroutine != null)
            {
                StopCoroutine(executeTurnCoroutine);
                executeTurnCoroutine = null;
                if (controlledUnit != null)
                {
                    Debug.Log($"[AI] {controlledUnit.UnitName}: Cancelled turn coroutine");
                }
            }
        }

        /// <summary>
        /// Coroutine to execute AI turn after thinking delay.
        /// </summary>
        private System.Collections.IEnumerator ExecuteAITurnWithDelay()
        {
            yield return new WaitForSeconds(thinkingDelay);
            
            // Verify unit is still valid before executing
            if (controlledUnit == null || !controlledUnit.IsAlive || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[AI] Cannot execute turn - unit is null, dead, or GameObject is inactive");
                executeTurnCoroutine = null;
                yield break;
            }
            
            ExecuteAITurn();
            executeTurnCoroutine = null;
        }

        /// <summary>
        /// Execute the actual AI logic after thinking delay.
        /// </summary>
        private void ExecuteAITurn()
        {
            // Verify unit is still valid
            if (controlledUnit == null || !controlledUnit.IsAlive || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[AI] Cannot execute turn - unit is null, dead, or GameObject is inactive");
                // Try to end turn if possible
                if (turnManager != null && controlledUnit != null)
                {
                    turnManager.EndTurn(controlledUnit);
                }
                return;
            }

            // Check if unit is stunned - if so, skip turn
            if (controlledUnit.IsStunned())
            {
                Debug.Log($"[AI] {controlledUnit.UnitName} is stunned and skips their turn!");
                if (turnManager != null)
                {
                    turnManager.EndTurn(controlledUnit);
                }
                return;
            }

            // Managers should already be cached from Start()
            // Only check for null and log errors - don't re-fetch to avoid performance overhead
            if (gridManager == null)
            {
                Debug.LogError($"[AI] {gameObject.name}: GridManager is null! Make sure GridManager exists in the scene.");
                // End turn even if GridManager is null to prevent getting stuck
                if (turnManager != null)
                {
                    turnManager.EndTurn(controlledUnit);
                }
                return;
            }
            
            if (turnManager == null)
            {
                Debug.LogError($"[AI] {gameObject.name}: TurnManager is null! Make sure TurnManager exists in the scene and is registered.");
                return;
            }
            
            // Cache window state at start of execution to prevent race conditions during async operations
            wasInWindowAtStart = turnManager.IsUnitInCurrentWindow(controlledUnit);
            if (!wasInWindowAtStart)
            {
                Debug.LogWarning($"[AI] {controlledUnit.UnitName} tried to execute turn but is not in current window (turn may have already ended)");
                return;
            }

            // Initialize behavior if not already done
            if (currentBehavior == null)
            {
                InitializeBehavior();
            }

            // Get all units for behavior to evaluate
            Unit[] allUnitsArray = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            List<Unit> allUnits = new List<Unit>(allUnitsArray);

            // STEP 1: Find best target using behavior strategy
            Unit target = currentBehavior?.ChooseTarget(allUnits);

            if (target == null)
            {
                Debug.Log("[AI] No valid targets found, ending turn");
                // End turn even if no valid targets
                if (turnManager != null)
                {
                    turnManager.EndTurn(controlledUnit);
                }
                return;
            }

            Debug.Log($"[AI] Target selected: {target.UnitName}");

            // STEP 2: Choose action type
            List<Skill> availableSkills = controlledUnit.GetAvailableSkills();
            AIActionType actionType = currentBehavior?.ChooseAction(target, availableSkills) ?? AIActionType.Move;

            // STEP 3: Find best position to move to (if needed)
            List<GridCell> validMoves = GetValidMoves();
            GridCell bestMoveCell = currentBehavior?.EvaluateBestMove(target, validMoves);

            if (bestMoveCell != null &&
                (bestMoveCell.X != controlledUnit.GridX || bestMoveCell.Y != controlledUnit.GridY))
            {
                Debug.Log($"[AI] Moving to ({bestMoveCell.X}, {bestMoveCell.Y})");

                // Calculate movement cost before moving (while we still know the start position)
                List<GridCell> movePath = gridManager.GetPath(controlledUnit, bestMoveCell.X, bestMoveCell.Y);
                lastMovementCost = movePath != null && movePath.Count > 0 ? movePath.Count - 1 : 1; // Don't count start cell
                
                // Validate and spend movement points BEFORE starting movement
                if (lastMovementCost > controlledUnit.MovementPointsRemaining)
                {
                    Debug.LogWarning($"[AI] {controlledUnit.UnitName} cannot move {lastMovementCost} cells - only {controlledUnit.MovementPointsRemaining} movement remaining!");
                    // Can't move, check if we can attack from current position
                    // (handled in the else block below)
                }
                else if (!controlledUnit.SpendMovementPoints(lastMovementCost))
                {
                    Debug.LogWarning($"[AI] {controlledUnit.UnitName} failed to spend movement points - cannot move");
                    // Can't move, check if we can attack from current position
                    // (handled in the else block below)
                }
                else
                {
                    Debug.Log($"[AI] {controlledUnit.UnitName} spent {lastMovementCost} movement points, {controlledUnit.MovementPointsRemaining} remaining");
                    
                    // Move to cell, then attack when movement completes
                    MoveToCell(bestMoveCell, movePath, () =>
                    {
                    // Safety check: verify unit and managers are still valid (movement may have been canceled)
                    // Managers should already be cached from Start() - only validate, don't re-fetch
                    
                    // Comprehensive validation before proceeding
                    if (controlledUnit == null || !controlledUnit.IsAlive || !gameObject.activeInHierarchy)
                    {
                        Debug.Log($"[AI] Movement callback invoked but unit is invalid - movement may have been canceled. Unit: {controlledUnit?.UnitName ?? "NULL"}");
                        return;
                    }
                    
                    if (turnManager == null)
                    {
                        Debug.LogWarning($"[AI] Movement callback invoked but TurnManager is null - cannot end turn");
                        return;
                    }
                    
                    // Verify unit is still in the current turn window
                    // Use cached state first, then verify with live check for safety
                    if (!wasInWindowAtStart)
                    {
                        Debug.Log($"[AI] {controlledUnit.UnitName} movement completed but was not in window at start (turn may have already ended)");
                        return;
                    }
                    // Final safety check with live state
                    if (!turnManager.IsUnitInCurrentWindow(controlledUnit))
                    {
                        Debug.Log($"[AI] {controlledUnit.UnitName} movement completed but is not in current window (turn may have already ended)");
                        return;
                    }
                    
                    // Movement points were already spent before movement started
                    // Verify grid position is updated
                    Debug.Log($"[AI] Movement complete. {controlledUnit.UnitName} at ({controlledUnit.GridX}, {controlledUnit.GridY})");
                    
                    // Validate target is still valid before acting
                    if (target == null || !target.IsAlive)
                    {
                        Debug.Log($"[AI] Target is no longer valid (null: {target == null}, alive: {target?.IsAlive ?? false}) - cannot act");
                        EndAITurn();
                        return;
                    }

                    // Check faction relationship
                    if (factionRelationship != null && !factionRelationship.AreHostile(controlledUnit.Faction, target.Faction) && 
                        !factionRelationship.AreAllied(controlledUnit.Faction, target.Faction))
                    {
                        Debug.Log($"[AI] Target {target.UnitName} is not hostile or allied - cannot act");
                        EndAITurn();
                        return;
                    }
                    
                    Debug.Log($"[AI] Target {target.UnitName} at ({target.GridX}, {target.GridY})");
                    
                    // Execute action based on action type
                    ExecuteAction(target, actionType, availableSkills);
                    
                        // ALWAYS end turn after move+attack (AI cannot move again after attacking)
                        // Call directly - movement completion callback ensures we're ready
                        EndAITurn();
                    });
                    
                    // Return early since we're moving - attack will happen in callback
                    return;
                }
                
                // If we couldn't move, check if we can attack from current position
                // (fall through to attack check in else block below)
            }
            else
            {
                // Can't move or already in best position - check if we can act
                Debug.Log($"[AI] Staying in place. {controlledUnit.UnitName} at ({controlledUnit.GridX}, {controlledUnit.GridY})");
                
                // Validate target is still valid before acting
                if (target == null || !target.IsAlive)
                {
                    Debug.Log($"[AI] Target is no longer valid (null: {target == null}, alive: {target?.IsAlive ?? false}) - cannot act");
                    EndAITurn();
                    return;
                }

                // Check faction relationship
                if (factionRelationship != null && !factionRelationship.AreHostile(controlledUnit.Faction, target.Faction) && 
                    !factionRelationship.AreAllied(controlledUnit.Faction, target.Faction))
                {
                    Debug.Log($"[AI] Target {target.UnitName} is not hostile or allied - cannot act");
                    EndAITurn();
                    return;
                }
                
                Debug.Log($"[AI] Target {target.UnitName} at ({target.GridX}, {target.GridY})");
                
                // Execute action based on action type
                ExecuteAction(target, actionType, availableSkills);
                
                // ALWAYS end turn after action
                // Call directly - no delay needed
                EndAITurn();
            }
        }

        /// <summary>
        /// Execute the chosen action (attack, skill, support, etc.).
        /// </summary>
        private void ExecuteAction(Unit target, AIActionType actionType, List<Skill> availableSkills)
        {
            switch (actionType)
            {
                case AIActionType.MeleeAttack:
                    ExecuteMeleeAttack(target);
                    break;
                case AIActionType.Skill:
                    ExecuteSkillAttack(target, availableSkills);
                    break;
                case AIActionType.Support:
                    ExecuteSupportAction(target, availableSkills);
                    break;
                case AIActionType.Wait:
                    Debug.Log("[AI] Waiting (no action)");
                    break;
                case AIActionType.Move:
                    // Movement already handled above
                    break;
                default:
                    Debug.LogWarning($"[AI] Unknown action type: {actionType}");
                    break;
            }
        }

        /// <summary>
        /// Execute a melee attack on the target.
        /// </summary>
        private void ExecuteMeleeAttack(Unit target)
        {
            // Check if adjacent
            int dx = Mathf.Abs(controlledUnit.GridX - target.GridX);
            int dy = Mathf.Abs(controlledUnit.GridY - target.GridY);
            int chebyshevDistance = Mathf.Max(dx, dy);
            bool isAdjacent = chebyshevDistance == 1;

            if (!isAdjacent)
            {
                Debug.LogWarning($"[AI] Cannot melee attack - not adjacent to {target.UnitName}");
                return;
            }

            AttackAction attackAction = ManagerRegistry.Get<AttackAction>();
            if (attackAction == null)
            {
                attackAction = AttackAction.Instance;
            }

            if (attackAction != null)
            {
                Debug.Log($"[AI] Attempting melee attack on {target.UnitName}");
                if (attackAction.ExecuteMeleeAttack(controlledUnit, target))
                {
                    Debug.Log($"[AI] Attack successful!");
                }
                else
                {
                    Debug.LogWarning($"[AI] Attack failed");
                }
            }
        }

        /// <summary>
        /// Execute a skill attack on the target.
        /// </summary>
        private void ExecuteSkillAttack(Unit target, List<Skill> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0)
            {
                Debug.LogWarning("[AI] No skills available for attack");
                return;
            }

            // Find suitable skill for this unit type and target
            Skill bestSkill = null;
            int targetDistance = Mathf.Abs(controlledUnit.GridX - target.GridX) + Mathf.Abs(controlledUnit.GridY - target.GridY);

            foreach (Skill skill in availableSkills)
            {
                if (skill.Range >= targetDistance && skill.BaseDamage > 0) // Prefer damaging skills
                {
                    bestSkill = skill;
                    break;
                }
            }

            if (bestSkill == null)
            {
                // Fallback to any skill in range
                foreach (Skill skill in availableSkills)
                {
                    if (skill.Range >= targetDistance)
                    {
                        bestSkill = skill;
                        break;
                    }
                }
            }

            if (bestSkill != null)
            {
                SkillExecutor skillExecutor = ManagerRegistry.Get<SkillExecutor>();
                if (skillExecutor != null)
                {
                    Debug.Log($"[AI] Using skill {bestSkill.SkillName} on {target.UnitName}");
                    if (skillExecutor.ExecuteSkill(bestSkill, controlledUnit, target))
                    {
                        controlledUnit.MarkAsActed();
                        controlledUnit.RecordAction();
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AI] No suitable skill found for attack");
            }
        }

        /// <summary>
        /// Execute a support action (heal, buff) on the target.
        /// </summary>
        private void ExecuteSupportAction(Unit target, List<Skill> availableSkills)
        {
            if (availableSkills == null || availableSkills.Count == 0)
            {
                Debug.LogWarning("[AI] No skills available for support");
                return;
            }

            // Find support skill (healing, buffing)
            Skill supportSkill = null;
            int targetDistance = Mathf.Abs(controlledUnit.GridX - target.GridX) + Mathf.Abs(controlledUnit.GridY - target.GridY);

            foreach (Skill skill in availableSkills)
            {
                if (skill.Range >= targetDistance && (skill.BaseDamage <= 0 || skill.AppliesStatusEffect))
                {
                    supportSkill = skill;
                    break;
                }
            }

            if (supportSkill != null)
            {
                SkillExecutor skillExecutor = ManagerRegistry.Get<SkillExecutor>();
                if (skillExecutor != null)
                {
                    Debug.Log($"[AI] Using support skill {supportSkill.SkillName} on {target.UnitName}");
                    if (skillExecutor.ExecuteSkill(supportSkill, controlledUnit, target))
                    {
                        controlledUnit.MarkAsActed();
                        controlledUnit.RecordAction();
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AI] No suitable support skill found");
            }
        }

        /// <summary>
        /// Get all cells this unit can legally move to.
        /// Uses pathfinding to respect obstacles.
        /// </summary>
        private List<GridCell> GetValidMoves()
        {
            // Use pathfinding to get all actually reachable cells
            if (gridManager == null)
            {
                Debug.LogWarning("AIController: GridManager is null!");
                return new List<GridCell>();
            }
            HashSet<GridCell> reachable = gridManager.GetReachableCells(controlledUnit, controlledUnit.MovementRange);
            return new List<GridCell>(reachable);
        }

        /// <summary>
        /// Move the unit to the specified cell (smoothly, not instant).
        /// </summary>
        private void MoveToCell(GridCell targetCell, List<GridCell> path = null, System.Action onComplete = null)
        {
            if (gridManager == null)
            {
                Debug.LogWarning("AIController: GridManager is null, cannot move!");
                return;
            }

            // Get the actual path to follow if not provided
            if (path == null)
            {
                path = gridManager.GetPath(controlledUnit, targetCell.X, targetCell.Y);
            }
            
            // Use the GridCell's centered WorldPosition
            Vector3 targetPosition = targetCell.WorldPosition;

            // Use the Character's built-in smooth movement system with callback AND path
            controlledUnit.MoveTo(targetCell.X, targetCell.Y, targetPosition, onComplete, path);

            Debug.Log($"[AI] Initiated path-following move to ({targetCell.X}, {targetCell.Y})");
        }

        /// <summary>
        /// Check if two units are adjacent (can attack each other).
        /// </summary>
        private bool IsAdjacent(Unit unit1, Unit unit2)
        {
            int distance = Mathf.Abs(unit1.GridX - unit2.GridX) + Mathf.Abs(unit1.GridY - unit2.GridY);
            return distance == 1;
        }

        /// <summary>
        /// Get Manhattan distance between two units.
        /// </summary>
        private int GetDistance(Unit unit1, Unit unit2)
        {
            return Mathf.Abs(unit1.GridX - unit2.GridX) + Mathf.Abs(unit1.GridY - unit2.GridY);
        }

        /// <summary>
        /// End the AI's turn after action completes.
        /// </summary>
        private void EndAITurn()
        {
            // Comprehensive validation
            if (controlledUnit == null || !controlledUnit.IsAlive || !gameObject.activeInHierarchy)
            {
                Debug.Log($"[AI] Cannot end turn - unit is null, dead, or GameObject is inactive (this is normal if movement was canceled)");
                return;
            }
            
            // TurnManager should already be cached from Start()
            if (turnManager == null)
            {
                Debug.LogError($"[AI] Cannot end turn - TurnManager is null! This should not happen if Start() ran correctly.");
                return;
            }
            
            // Safety check: verify unit is still in the current turn window before ending
            // Use cached state first, then verify with live check for safety
            if (!wasInWindowAtStart)
            {
                Debug.Log($"[AI] {controlledUnit.UnitName} tried to end turn but was not in window at start (turn may have already ended)");
                return;
            }
            // Final safety check with live state
            if (!turnManager.IsUnitInCurrentWindow(controlledUnit))
            {
                Debug.Log($"[AI] {controlledUnit.UnitName} tried to end turn but is not in current window (turn may have already ended)");
                return;
            }
            
            Debug.Log($"[AI] Ending turn for {controlledUnit.UnitName}");
            
            // Raise event to notify TurnManager that turn is complete
            OnTurnComplete?.Invoke(controlledUnit);
            
            turnManager.EndTurn(controlledUnit);
        }
        
        /// <summary>
        /// Clean up when the component is disabled or destroyed.
        /// </summary>
        private void OnDisable()
        {
            CancelTurn();
        }

        /// <summary>
        /// Clean up when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            CancelTurn();
            // Unsubscribe from any events to prevent memory leaks
            OnTurnComplete = null;
        }
    }
}