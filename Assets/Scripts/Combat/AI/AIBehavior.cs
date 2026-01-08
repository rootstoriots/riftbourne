using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Skills;
using System.Collections.Generic;

namespace Riftbourne.Combat.AI
{
    /// <summary>
    /// Base class for AI behavior strategies.
    /// Each behavior type (Berserker, Support, etc.) implements this interface.
    /// Uses Strategy pattern for flexible, swappable AI behaviors.
    /// </summary>
    public abstract class AIBehavior
    {
        protected Unit controlledUnit;
        protected GridManager gridManager;
        protected HazardManager hazardManager;
        protected FactionRelationship factionRelationship;

        /// <summary>
        /// Initialize the behavior with required references.
        /// </summary>
        public virtual void Initialize(Unit unit, GridManager grid, HazardManager hazards, FactionRelationship factionRel)
        {
            controlledUnit = unit;
            gridManager = grid;
            hazardManager = hazards;
            factionRelationship = factionRel;
        }

        /// <summary>
        /// Choose the best target for this unit based on behavior strategy.
        /// Returns null if no valid targets found.
        /// </summary>
        public abstract Unit ChooseTarget(List<Unit> allUnits);

        /// <summary>
        /// Choose the best action to perform (attack, skill, move, etc.).
        /// Returns the chosen action type.
        /// </summary>
        public abstract AIActionType ChooseAction(Unit target, List<Skill> availableSkills);

        /// <summary>
        /// Evaluate and return the best cell to move to.
        /// Returns null if no good move is found.
        /// </summary>
        public abstract GridCell EvaluateBestMove(Unit target, List<GridCell> validMoves);

        /// <summary>
        /// Score a potential target. Higher = better target.
        /// Base implementation provides common scoring logic.
        /// </summary>
        protected virtual float ScoreTarget(Unit target)
        {
            if (target == null || !target.IsAlive) return float.MinValue;

            // Check if target is an enemy
            if (factionRelationship == null || !factionRelationship.AreHostile(controlledUnit.Faction, target.Faction))
            {
                return float.MinValue; // Not an enemy
            }

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
        /// Score a potential move position. Higher = better position.
        /// Base implementation provides common scoring logic.
        /// </summary>
        protected virtual float ScoreMovePosition(GridCell cell, Unit target)
        {
            if (cell == null || target == null) return float.MinValue;

            float score = 0f;

            // HUGE penalty for hazards (avoid fire!)
            if (cell.Hazard != null)
            {
                score -= 1000f;
            }

            // Prefer positions closer to target (using Chebyshev distance to match attack system)
            int dx = Mathf.Abs(cell.X - target.GridX);
            int dy = Mathf.Abs(cell.Y - target.GridY);
            int chebyshevDistance = Mathf.Max(dx, dy);
            int manhattanDistance = dx + dy;

            // Penalty for distance (use Manhattan for movement cost estimation)
            score -= manhattanDistance * 10f;

            // HUGE bonus for being adjacent to target (can attack after move)
            if (chebyshevDistance == 1)
            {
                score += 100f; // Prioritize adjacency
            }

            return score;
        }

        /// <summary>
        /// Get Manhattan distance between two units.
        /// </summary>
        protected int GetDistance(Unit unit1, Unit unit2)
        {
            return Mathf.Abs(unit1.GridX - unit2.GridX) + Mathf.Abs(unit1.GridY - unit2.GridY);
        }

        /// <summary>
        /// Get all valid enemy targets.
        /// </summary>
        protected List<Unit> GetEnemyTargets(List<Unit> allUnits)
        {
            List<Unit> enemies = new List<Unit>();

            foreach (Unit unit in allUnits)
            {
                if (unit != null && unit.IsAlive && unit != controlledUnit)
                {
                    if (factionRelationship != null && factionRelationship.AreHostile(controlledUnit.Faction, unit.Faction))
                    {
                        enemies.Add(unit);
                    }
                }
            }

            return enemies;
        }

        /// <summary>
        /// Get all valid ally targets (for support behaviors).
        /// </summary>
        protected List<Unit> GetAllyTargets(List<Unit> allUnits)
        {
            List<Unit> allies = new List<Unit>();

            foreach (Unit unit in allUnits)
            {
                if (unit != null && unit.IsAlive && unit != controlledUnit)
                {
                    if (factionRelationship != null && factionRelationship.AreAllied(controlledUnit.Faction, unit.Faction))
                    {
                        allies.Add(unit);
                    }
                }
            }

            return allies;
        }

        /// <summary>
        /// Check if a skill is suitable for this unit type.
        /// </summary>
        protected bool IsSkillSuitableForUnitType(Skill skill, UnitType unitType)
        {
            // Beast prefers melee skills
            if (unitType == UnitType.Beast)
            {
                return skill.Range <= 1; // Melee range
            }

            // Magi prefers skills with range or ground effects
            if (unitType == UnitType.Magi)
            {
                return skill.Range > 1 || skill.CreatesGroundHazard;
            }

            // Soldier can use any skill
            return true;
        }
    }

    /// <summary>
    /// Types of actions the AI can choose.
    /// </summary>
    public enum AIActionType
    {
        Move,           // Just move (no attack)
        MeleeAttack,   // Move and melee attack
        RangedAttack,  // Ranged attack (if available)
        Skill,          // Use a skill
        Support,        // Support action (heal, buff)
        Wait            // Do nothing
    }
}

