using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Skills;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Combat.AI
{
    /// <summary>
    /// Support AI behavior - prioritizes healing/buffing allies, attacks when safe.
    /// Prefers staying back and supporting from range.
    /// </summary>
    public class SupportBehavior : AIBehavior
    {
        private AIBehaviorData behaviorData;

        public SupportBehavior(AIBehaviorData data)
        {
            behaviorData = data;
        }

        public override Unit ChooseTarget(List<Unit> allUnits)
        {
            // Support behavior: prioritize helping allies, then attack enemies
            List<Unit> allies = GetAllyTargets(allUnits);
            List<Unit> enemies = GetEnemyTargets(allUnits);

            // First, check if any allies need healing
            float supportPreference = behaviorData?.SupportPreference ?? 0.5f;
            if (allies.Count > 0 && Random.value < supportPreference)
            {
                // Find ally with lowest HP
                Unit woundedAlly = null;
                float lowestHP = float.MaxValue;

                foreach (Unit ally in allies)
                {
                    float hpPercent = (float)ally.CurrentHP / ally.MaxHP;
                    if (hpPercent < 0.8f && hpPercent < lowestHP) // Only consider allies below 80% HP
                    {
                        lowestHP = hpPercent;
                        woundedAlly = ally;
                    }
                }

                if (woundedAlly != null)
                {
                    return woundedAlly; // Return as "target" for support action
                }
            }

            // No allies need help, or support preference is low - attack enemies
            if (enemies.Count == 0)
            {
                return null;
            }

            // Support prefers safer targets (weaker enemies)
            Unit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Unit enemy in enemies)
            {
                float score = ScoreTarget(enemy);
                
                // Support prefers low HP targets (finish them off safely)
                float hpPercent = (float)enemy.CurrentHP / enemy.MaxHP;
                score += (1f - hpPercent) * 150f; // Higher weight than berserker

                // Prefer targets that are further away (safer)
                int distance = GetDistance(controlledUnit, enemy);
                score += distance * 5f; // Bonus for distance (opposite of berserker)

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

            // Check if target is an ally (support action)
            bool isAlly = factionRelationship != null && factionRelationship.AreAllied(controlledUnit.Faction, target.Faction);
            
            if (isAlly)
            {
                // Support action - prefer skills (healing/buffing)
                if (availableSkills != null && availableSkills.Count > 0)
                {
                    // Look for support skills (healing, buffing)
                    var supportSkills = availableSkills.Where(s => 
                        s.BaseDamage <= 0 || // Non-damaging skills
                        IsSkillSuitableForUnitType(s, controlledUnit.UnitType)
                    ).ToList();
                    
                    if (supportSkills.Count > 0)
                    {
                        return AIActionType.Support;
                    }
                }
                // No support skills - can't help, wait
                return AIActionType.Wait;
            }

            // Target is an enemy - attack if safe
            int dx = Mathf.Abs(controlledUnit.GridX - target.GridX);
            int dy = Mathf.Abs(controlledUnit.GridY - target.GridY);
            int chebyshevDistance = Mathf.Max(dx, dy);

            if (chebyshevDistance == 1)
            {
                // Adjacent - can melee attack
                return AIActionType.MeleeAttack;
            }
            else if (chebyshevDistance <= 3 && availableSkills != null && availableSkills.Count > 0)
            {
                // Check for ranged skills
                var rangedSkills = availableSkills.Where(s => s.Range >= chebyshevDistance && IsSkillSuitableForUnitType(s, controlledUnit.UnitType)).ToList();
                if (rangedSkills.Count > 0)
                {
                    return AIActionType.Skill;
                }
            }

            // Not in range - move (but support prefers staying back)
            return AIActionType.Move;
        }

        public override GridCell EvaluateBestMove(Unit target, List<GridCell> validMoves)
        {
            if (target == null || validMoves == null || validMoves.Count == 0)
            {
                return null;
            }

            bool isAlly = factionRelationship != null && factionRelationship.AreAllied(controlledUnit.Faction, target.Faction);
            
            GridCell bestCell = null;
            float bestScore = float.MinValue;

            foreach (GridCell cell in validMoves)
            {
                float score = ScoreMovePosition(cell, target);

                // Support behavior: high hazard avoidance
                if (cell.Hazard != null)
                {
                    float avoidance = behaviorData?.HazardAvoidance ?? 0.8f;
                    score -= 1000f * avoidance;
                }

                int dx = Mathf.Abs(cell.X - target.GridX);
                int dy = Mathf.Abs(cell.Y - target.GridY);
                int chebyshevDistance = Mathf.Max(dx, dy);

                if (isAlly)
                {
                    // For allies, prefer being close (for healing range)
                    if (chebyshevDistance <= 2)
                    {
                        score += 50f; // Bonus for being in support range
                    }
                }
                else
                {
                    // For enemies, prefer staying at range (2-3 cells away)
                    if (chebyshevDistance >= 2 && chebyshevDistance <= 3)
                    {
                        score += 30f; // Bonus for safe range
                    }
                    else if (chebyshevDistance == 1)
                    {
                        score -= 20f; // Penalty for being too close (support doesn't like melee)
                    }
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

