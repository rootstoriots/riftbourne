using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Skills;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Combat.AI
{
    /// <summary>
    /// Protector AI behavior - tanks, protects allies, draws aggro.
    /// Moves to protect allies and engages enemies in melee.
    /// </summary>
    public class ProtectorBehavior : AIBehavior
    {
        private AIBehaviorData behaviorData;

        public ProtectorBehavior(AIBehaviorData data)
        {
            behaviorData = data;
        }

        public override Unit ChooseTarget(List<Unit> allUnits)
        {
            // Protector prioritizes protecting allies, then attacking threats
            List<Unit> allies = GetAllyTargets(allUnits);
            List<Unit> enemies = GetEnemyTargets(allUnits);

            // First, check if any allies are in danger (low HP and near enemies)
            if (allies.Count > 0)
            {
                Unit allyInDanger = null;
                float highestThreat = 0f;

                foreach (Unit ally in allies)
                {
                    float hpPercent = (float)ally.CurrentHP / ally.MaxHP;
                    
                    // Check if ally is threatened (low HP or near enemies)
                    int nearbyEnemies = 0;
                    foreach (Unit enemy in enemies)
                    {
                        int distance = GetDistance(ally, enemy);
                        if (distance <= 2)
                        {
                            nearbyEnemies++;
                        }
                    }

                    float threat = (1f - hpPercent) * 100f + nearbyEnemies * 50f;
                    if (threat > highestThreat && (hpPercent < 0.7f || nearbyEnemies > 0))
                    {
                        highestThreat = threat;
                        allyInDanger = ally;
                    }
                }

                if (allyInDanger != null)
                {
                    // Find the enemy threatening this ally
                    Unit threateningEnemy = null;
                    int closestDistance = int.MaxValue;

                    foreach (Unit enemy in enemies)
                    {
                        int distance = GetDistance(allyInDanger, enemy);
                        if (distance <= 2 && distance < closestDistance)
                        {
                            closestDistance = distance;
                            threateningEnemy = enemy;
                        }
                    }

                    if (threateningEnemy != null)
                    {
                        return threateningEnemy; // Attack the threat
                    }
                }
            }

            // No allies in immediate danger - attack closest enemy
            if (enemies.Count == 0)
            {
                return null;
            }

            // Protector prefers engaging closest enemies (tank role)
            Unit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Unit enemy in enemies)
            {
                float score = ScoreTarget(enemy);
                
                // Protector prefers close targets (engage in melee)
                int distance = GetDistance(controlledUnit, enemy);
                score -= distance * 15f; // Strong preference for proximity

                // Also consider enemy's threat to allies
                int nearbyAllies = 0;
                foreach (Unit ally in allies)
                {
                    int allyDistance = GetDistance(enemy, ally);
                    if (allyDistance <= 2)
                    {
                        nearbyAllies++;
                    }
                }
                score += nearbyAllies * 30f; // Bonus for protecting allies

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
                // Adjacent - protector prefers melee combat
                if (availableSkills != null && availableSkills.Count > 0)
                {
                    // Consider defensive/taunt skills
                    var defensiveSkills = availableSkills.Where(s => s.Range <= 1 && IsSkillSuitableForUnitType(s, controlledUnit.UnitType)).ToList();
                    if (defensiveSkills.Count > 0 && Random.value < 0.3f)
                    {
                        return AIActionType.Skill;
                    }
                }
                return AIActionType.MeleeAttack;
            }
            else
            {
                // Not adjacent - move to engage
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

                // Protector has moderate hazard avoidance (will take risks for allies)
                if (cell.Hazard != null)
                {
                    float avoidance = behaviorData?.HazardAvoidance ?? 0.6f;
                    score -= 1000f * avoidance;
                }

                // Protector wants to get adjacent (tank role)
                int dx = Mathf.Abs(cell.X - target.GridX);
                int dy = Mathf.Abs(cell.Y - target.GridY);
                int chebyshevDistance = Mathf.Max(dx, dy);

                if (chebyshevDistance == 1)
                {
                    score += 150f; // Big bonus for engaging in melee
                }
                else
                {
                // Prefer positions that block enemy from reaching allies
                // This is simplified - in full implementation, would check all units
                score -= chebyshevDistance * 10f; // Penalty for distance
                }

                // Check if this position protects allies better
                // (Simplified - full implementation would check ally positions)

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

