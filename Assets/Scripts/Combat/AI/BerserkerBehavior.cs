using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Skills;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Combat.AI
{
    /// <summary>
    /// Aggressive AI behavior - prioritizes attacking closest/low HP enemies.
    /// Moves aggressively toward targets and prefers melee combat.
    /// </summary>
    public class BerserkerBehavior : AIBehavior
    {
        private AIBehaviorData behaviorData;

        public BerserkerBehavior(AIBehaviorData data)
        {
            behaviorData = data;
        }

        public override Unit ChooseTarget(List<Unit> allUnits)
        {
            List<Unit> enemies = GetEnemyTargets(allUnits);

            if (enemies.Count == 0)
            {
                return null;
            }

            // Score each enemy - berserker prefers low HP and close targets
            Unit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Unit enemy in enemies)
            {
                float score = ScoreTarget(enemy);
                
                // Additional berserker-specific scoring
                float hpPercent = (float)enemy.CurrentHP / enemy.MaxHP;
                score += (1f - hpPercent) * 100f * (behaviorData?.LowHPWeight ?? 0.5f);

                int distance = GetDistance(controlledUnit, enemy);
                score -= distance * 10f * (1f - (behaviorData?.ProximityWeight ?? 0.3f));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        public override AIActionType ChooseAction(Unit target, List<Skill> availableSkills)
        {
            if (target == null || !target.IsAlive)
            {
                return AIActionType.Wait;
            }

            // Check if adjacent
            int dx = Mathf.Abs(controlledUnit.GridX - target.GridX);
            int dy = Mathf.Abs(controlledUnit.GridY - target.GridY);
            int chebyshevDistance = Mathf.Max(dx, dy);

            if (chebyshevDistance == 1)
            {
                // Adjacent - prefer melee attack, but consider skills if available
                if (availableSkills != null && availableSkills.Count > 0)
                {
                    // Berserker prefers melee skills over basic attack
                    float skillChance = behaviorData?.SkillPreference ?? 0.3f;
                    if (Random.value < skillChance)
                    {
                        // Check for suitable melee skills
                        var meleeSkills = availableSkills.Where(s => s.Range <= 1 && IsSkillSuitableForUnitType(s, controlledUnit.UnitType)).ToList();
                        if (meleeSkills.Count > 0)
                        {
                            return AIActionType.Skill;
                        }
                    }
                }
                return AIActionType.MeleeAttack;
            }
            else
            {
                // Not adjacent - need to move
                return AIActionType.Move;
            }
        }

        public override GridCell EvaluateBestMove(Unit target, List<GridCell> validMoves)
        {
            if (target == null || validMoves == null || validMoves.Count == 0)
            {
                return null;
            }

            GridCell bestCell = null;
            float bestScore = float.MinValue;

            foreach (GridCell cell in validMoves)
            {
                float score = ScoreMovePosition(cell, target);

                // Berserker-specific: high aggression, less hazard avoidance
                if (cell.Hazard != null)
                {
                    float avoidance = behaviorData?.HazardAvoidance ?? 0.5f;
                    score -= 1000f * (1f - avoidance); // Less penalty if low avoidance
                }

                // Aggressive movement bonus
                float aggression = behaviorData?.AggressionLevel ?? 0.7f;
                int dx = Mathf.Abs(cell.X - target.GridX);
                int dy = Mathf.Abs(cell.Y - target.GridY);
                int chebyshevDistance = Mathf.Max(dx, dy);
                if (chebyshevDistance == 1)
                {
                    score += 100f * aggression; // Bonus for getting adjacent
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            return bestCell;
        }
    }
}

