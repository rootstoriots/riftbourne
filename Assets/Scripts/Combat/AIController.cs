using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Riftbourne.Characters;
using Riftbourne.Grid;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Controls AI decision-making for enemy units.
    /// Makes tactical choices about movement, targeting, and actions.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float thinkingDelay = 0.5f; // Visual delay so player sees AI "thinking"

        private Unit controlledUnit;
        private GridManager gridManager;
        private HazardManager hazardManager;
        private TurnManager turnManager;

        private void Awake()
        {
            controlledUnit = GetComponent<Unit>();
        }

        private void Start()
        {
            gridManager = FindFirstObjectByType<GridManager>();
            hazardManager = FindFirstObjectByType<HazardManager>();
            turnManager = FindFirstObjectByType<TurnManager>();
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

            Debug.Log($"[AI] {controlledUnit.UnitName} is thinking...");

            // Add visual delay so player sees AI is taking a turn
            Invoke(nameof(ExecuteAITurn), thinkingDelay);
        }

        /// <summary>
        /// Execute the actual AI logic after thinking delay.
        /// </summary>
        private void ExecuteAITurn()
        {
            // STEP 1: Find best target
            Unit targetEnemy = ChooseTarget();

            if (targetEnemy == null)
            {
                Debug.Log("[AI] No valid targets found, skipping turn");
                return;
            }

            Debug.Log($"[AI] Target selected: {targetEnemy.UnitName}");

            // STEP 2: Find best position to move to
            GridCell bestMoveCell = EvaluateBestMove(targetEnemy);

            if (bestMoveCell != null &&
                (bestMoveCell.X != controlledUnit.GridX || bestMoveCell.Y != controlledUnit.GridY))
            {
                Debug.Log($"[AI] Moving to ({bestMoveCell.X}, {bestMoveCell.Y})");

                // Move to cell, then attack when movement completes
                MoveToCell(bestMoveCell, () =>
                {
                    // Movement complete - try to attack
                    AttackAction.ExecuteMeleeAttack(controlledUnit, targetEnemy);
                    // Turn ends when window advances (handled by TurnManager)
                });
            }
            else
            {
                // Can't move or already in best position - just attack
                Debug.Log("[AI] Staying in place, attacking");
                AttackAction.ExecuteMeleeAttack(controlledUnit, targetEnemy);
                // Turn ends when window advances (handled by TurnManager)
            }
        }

        /// <summary>
        /// Choose the best enemy target based on tactical priorities.
        /// Priority: Low HP > Closest > Random
        /// </summary>
        private Unit ChooseTarget()
        {
            // Find all living enemy units (player-controlled units)
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            List<Unit> enemies = new List<Unit>();

            foreach (Unit unit in allUnits)
            {
                if (unit.IsAlive && unit.IsPlayerControlled)
                {
                    enemies.Add(unit);
                }
            }

            if (enemies.Count == 0)
            {
                return null;
            }

            // Score each enemy
            Unit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Unit enemy in enemies)
            {
                float score = ScoreTarget(enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Score a potential target. Higher = better target.
        /// </summary>
        private float ScoreTarget(Unit target)
        {
            float score = 0f;

            // Prefer low HP targets (finish them off!)
            float hpPercent = (float)target.CurrentHP / target.MaxHP;
            score += (1f - hpPercent) * 100f; // 0-100 points based on missing HP

            // Prefer closer targets
            int distance = GetDistance(controlledUnit, target);
            score -= distance * 10f; // Penalty for distance

            return score;
        }

        /// <summary>
        /// Evaluate all possible moves and return the best cell to move to.
        /// </summary>
        private GridCell EvaluateBestMove(Unit targetEnemy)
        {
            List<GridCell> validMoves = GetValidMoves();

            if (validMoves.Count == 0)
            {
                return null;
            }

            GridCell bestCell = null;
            float bestScore = float.MinValue;

            foreach (GridCell cell in validMoves)
            {
                float score = ScoreMovePosition(cell, targetEnemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            return bestCell;
        }

        /// <summary>
        /// Score a potential move position. Higher = better position.
        /// </summary>
        private float ScoreMovePosition(GridCell cell, Unit targetEnemy)
        {
            float score = 0f;

            // HUGE penalty for hazards (avoid fire!)
            if (cell.Hazard != null)
            {
                score -= 1000f;
                Debug.Log($"[AI] Cell ({cell.X}, {cell.Y}) has hazard, score penalty: -1000");
            }

            // Prefer positions closer to target
            int distanceToTarget = Mathf.Abs(cell.X - targetEnemy.GridX) + Mathf.Abs(cell.Y - targetEnemy.GridY);
            score -= distanceToTarget * 10f;

            // Bonus for being adjacent to target (can attack after move)
            if (distanceToTarget == 1)
            {
                score += 50f;
                Debug.Log($"[AI] Cell ({cell.X}, {cell.Y}) is adjacent to target, bonus: +50");
            }

            return score;
        }

        /// <summary>
        /// Get all cells this unit can legally move to.
        /// </summary>
        private List<GridCell> GetValidMoves()
        {
            List<GridCell> validCells = new List<GridCell>();

            // Check all cells within movement range
            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    GridCell cell = gridManager.GetCell(x, y);
                    if (cell != null && controlledUnit.CanMoveTo(cell.X, cell.Y))
                    {
                        validCells.Add(cell);
                    }

                }
            }

            return validCells;
        }

        /// <summary>
        /// Move the unit to the specified cell (smoothly, not instant).
        /// </summary>
        private void MoveToCell(GridCell targetCell, System.Action onComplete = null)
        {
            Vector3 targetPosition = new Vector3(targetCell.X, 0.5f, targetCell.Y);

            // Use the Character's built-in smooth movement system with callback
            controlledUnit.MoveTo(targetCell.X, targetCell.Y, targetPosition, onComplete);

            Debug.Log($"[AI] Initiated smooth move to ({targetCell.X}, {targetCell.Y})");
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
    }
}