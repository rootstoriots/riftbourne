using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Skills;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Combat.AI
{
    /// <summary>
    /// Coward AI behavior - defensive, retreats when low HP, attacks from safety.
    /// Prefers ranged attacks and avoids melee combat.
    /// </summary>
    public class CowardBehavior : AIBehavior
    {
        private AIBehaviorData behaviorData;

        public CowardBehavior(AIBehaviorData data)
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

            // Coward prefers weak, distant targets
            Unit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Unit enemy in enemies)
            {
                float score = ScoreTarget(enemy);
                
                // Coward prefers low HP targets (finish them off quickly)
                float hpPercent = (float)enemy.CurrentHP / enemy.MaxHP;
                score += (1f - hpPercent) * 120f;

                // Strong preference for distant targets (safety)
                int distance = GetDistance(controlledUnit, enemy);
                score += distance * 15f; // Big bonus for distance

                // Check own HP - if low, prefer even weaker targets
                float ownHPPercent = (float)controlledUnit.CurrentHP / controlledUnit.MaxHP;
                if (ownHPPercent < (behaviorData?.RetreatThreshold ?? 0.3f))
                {
                    // Very low HP - only attack if target is also very weak
                    if (hpPercent > 0.5f)
                    {
                        score -= 200f; // Heavy penalty for healthy enemies when we're low
                    }
                }

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

            // Check own HP - if below threshold, consider retreating
            float ownHPPercent = (float)controlledUnit.CurrentHP / controlledUnit.MaxHP;
            float retreatThreshold = behaviorData?.RetreatThreshold ?? 0.3f;

            if (ownHPPercent < retreatThreshold)
            {
                // Low HP - prefer ranged attacks or skills, avoid melee
                int dx = Mathf.Abs(controlledUnit.GridX - target.GridX);
                int dy = Mathf.Abs(controlledUnit.GridY - target.GridY);
                int chebyshevDistance = Mathf.Max(dx, dy);

                if (chebyshevDistance == 1)
                {
                    // Adjacent - too dangerous! Try to move away
                    return AIActionType.Move; // Will move away in EvaluateBestMove
                }
                else if (chebyshevDistance <= 3 && availableSkills != null && availableSkills.Count > 0)
                {
                    // Safe range - use skills
                    var rangedSkills = availableSkills.Where(s => s.Range >= chebyshevDistance && IsSkillSuitableForUnitType(s, controlledUnit.UnitType)).ToList();
                    if (rangedSkills.Count > 0)
                    {
                        return AIActionType.Skill;
                    }
                }
                // Can't attack safely - wait or move away
                return AIActionType.Wait;
            }

            // Normal HP - can attack, but prefer ranged
            int targetDx = Mathf.Abs(controlledUnit.GridX - target.GridX);
            int targetDy = Mathf.Abs(controlledUnit.GridY - target.GridY);
            int targetDistance = Mathf.Max(targetDx, targetDy);

            if (targetDistance == 1)
            {
                // Adjacent - only attack if we're healthy
                if (ownHPPercent > 0.6f)
                {
                    return AIActionType.MeleeAttack;
                }
                else
                {
                    // Not healthy enough for melee - move away
                    return AIActionType.Move;
                }
            }
            else if (targetDistance <= 3 && availableSkills != null && availableSkills.Count > 0)
            {
                // Prefer ranged skills
                var rangedSkills = availableSkills.Where(s => s.Range >= targetDistance && IsSkillSuitableForUnitType(s, controlledUnit.UnitType)).ToList();
                if (rangedSkills.Count > 0)
                {
                    return AIActionType.Skill;
                }
            }

            // Not in range - move (but coward will prefer moving away from enemies)
            return AIActionType.Move;
        }

        public override GridCell EvaluateBestMove(Unit target, List<GridCell> validMoves)
        {
            if (validMoves == null || validMoves.Count == 0)
            {
                return null;
            }

            float ownHPPercent = (float)controlledUnit.CurrentHP / controlledUnit.MaxHP;
            float retreatThreshold = behaviorData?.RetreatThreshold ?? 0.3f;
            bool shouldRetreat = ownHPPercent < retreatThreshold;

            GridCell bestCell = null;
            float bestScore = float.MinValue;

            foreach (GridCell cell in validMoves)
            {
                float score = 0f;

                // Very high hazard avoidance
                if (cell.Hazard != null)
                {
                    float avoidance = behaviorData?.HazardAvoidance ?? 0.9f;
                    score -= 1000f * avoidance;
                }

                if (target != null && target.IsAlive)
                {
                    int dx = Mathf.Abs(cell.X - target.GridX);
                    int dy = Mathf.Abs(cell.Y - target.GridY);
                    int chebyshevDistance = Mathf.Max(dx, dy);

                    if (shouldRetreat)
                    {
                        // Retreating - move AWAY from target
                        score += chebyshevDistance * 50f; // Big bonus for distance
                        score -= 100f; // Penalty for being adjacent
                    }
                    else
                    {
                        // Normal behavior - prefer safe range (2-3 cells)
                        if (chebyshevDistance >= 2 && chebyshevDistance <= 3)
                        {
                            score += 40f; // Bonus for safe range
                        }
                        else if (chebyshevDistance == 1)
                        {
                            score -= 50f; // Penalty for melee range
                        }
                        else if (chebyshevDistance > 3)
                        {
                            score -= 20f; // Slight penalty for being too far (can't attack)
                        }
                    }
                }
                else
                {
                    // No target - prefer staying in place or moving to safety
                    score += 10f;
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

