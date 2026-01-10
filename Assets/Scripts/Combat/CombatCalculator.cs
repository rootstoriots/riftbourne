using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Core;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Handles combat calculations including hit/miss, parry, and critical hits.
    /// Centralizes all combat math for consistency.
    /// </summary>
    public static class CombatCalculator
    {
        /// <summary>
        /// Result of a combat calculation.
        /// </summary>
        public struct CombatResult
        {
            public bool Hit;
            public bool Parried;
            public bool CriticalHit;
            public bool CriticalDefense;
            public int FinalDamage;
        }

        /// <summary>
        /// Calculate the result of an attack from attacker to target.
        /// Handles hit/miss, parry, critical hits, and critical defense.
        /// </summary>
        public static CombatResult CalculateAttack(Unit attacker, Unit target, int baseDamage)
        {
            CombatResult result = new CombatResult
            {
                Hit = false,
                Parried = false,
                CriticalHit = false,
                CriticalDefense = false,
                FinalDamage = 0
            };

            // Get combat stats for attacker and target
            float attackerHitChance = GetHitChance(attacker);
            float attackerCritChance = GetCritChance(attacker);
            float targetParryChance = GetParryChance(target);
            float targetCritDefense = GetCritDefense(target);

            // Step 1: Check if attack hits
            float hitRoll = Random.Range(0f, 100f);
            if (hitRoll > attackerHitChance)
            {
                // Attack missed
                Debug.Log($"{attacker.UnitName}'s attack missed! (Roll: {hitRoll:F1} vs Hit Chance: {attackerHitChance:F1}%)");
                return result;
            }

            result.Hit = true;

            // Step 2: Check if target parries
            float parryRoll = Random.Range(0f, 100f);
            if (parryRoll <= targetParryChance)
            {
                result.Parried = true;
                Debug.Log($"{target.UnitName} parried {attacker.UnitName}'s attack! (Roll: {parryRoll:F1} vs Parry Chance: {targetParryChance:F1}%)");
                return result; // Parried attacks deal no damage
            }

            // Step 3: Check for critical hit
            float critRoll = Random.Range(0f, 100f);
            bool isCrit = critRoll <= attackerCritChance;

            // Step 4: Check for critical defense (reduces crit damage)
            float critDefenseRoll = Random.Range(0f, 100f);
            bool critDefended = critDefenseRoll <= targetCritDefense;

            result.CriticalHit = isCrit;
            result.CriticalDefense = critDefended;

            // Step 5: Calculate final damage
            int finalDamage = baseDamage;

            // Apply critical hit multiplier (if not defended)
            if (isCrit && !critDefended)
            {
                float critMultiplier = GameConstants.Instance != null ? GameConstants.Instance.CriticalHitMultiplier : 1.5f;
                finalDamage = Mathf.RoundToInt(finalDamage * critMultiplier);
                Debug.Log($"{attacker.UnitName} scored a critical hit! (Roll: {critRoll:F1} vs Crit Chance: {attackerCritChance:F1}%)");
            }
            else if (isCrit && critDefended)
            {
                Debug.Log($"{target.UnitName} defended against the critical hit! (Roll: {critDefenseRoll:F1} vs Crit Defense: {targetCritDefense:F1}%)");
            }

            // Apply defense reduction
            int defense = target.DefensePower;
            int minDamage = GameConstants.Instance != null ? GameConstants.Instance.MinimumDamage : 1;
            finalDamage = Mathf.Max(minDamage, finalDamage - defense);

            result.FinalDamage = finalDamage;

            return result;
        }

        /// <summary>
        /// Get the hit chance for a unit (base + status effect modifiers).
        /// </summary>
        private static float GetHitChance(Unit unit)
        {
            float baseHitChance = GameConstants.Instance != null ? GameConstants.Instance.BaseHitChance : 90f;
            
            // Finesse affects hit chance (higher finesse = better accuracy)
            float finesseBonus = unit.Finesse * (GameConstants.Instance != null ? GameConstants.Instance.FinesseHitChancePerPoint : 1f);
            
            // Get status effect modifiers
            float statusModifier = unit.GetTotalHitChanceModifier();
            
            float totalHitChance = baseHitChance + finesseBonus + statusModifier;
            
            // Clamp between 5% and 95% (always a chance to miss/hit)
            return Mathf.Clamp(totalHitChance, 5f, 95f);
        }

        /// <summary>
        /// Get the critical hit chance for a unit (base + status effect modifiers).
        /// </summary>
        private static float GetCritChance(Unit unit)
        {
            float baseCritChance = GameConstants.Instance != null ? GameConstants.Instance.BaseCritChance : 5f;
            
            // Luck affects crit chance
            float luckBonus = unit.Luck * (GameConstants.Instance != null ? GameConstants.Instance.LuckCritChancePerPoint : 0.5f);
            
            // Get status effect modifiers
            float statusModifier = unit.GetTotalCritChanceModifier();
            
            float totalCritChance = baseCritChance + luckBonus + statusModifier;
            
            // Clamp between 0% and 50% (reasonable crit cap)
            return Mathf.Clamp(totalCritChance, 0f, 50f);
        }

        /// <summary>
        /// Get the parry chance for a unit (base + status effect modifiers).
        /// </summary>
        private static float GetParryChance(Unit unit)
        {
            float baseParryChance = GameConstants.Instance != null ? GameConstants.Instance.BaseParryChance : 5f;
            
            // Finesse affects parry chance (higher finesse = better reflexes)
            float finesseBonus = unit.Finesse * (GameConstants.Instance != null ? GameConstants.Instance.FinesseParryChancePerPoint : 0.5f);
            
            // Get status effect modifiers
            float statusModifier = unit.GetTotalParryChanceModifier();
            
            float totalParryChance = baseParryChance + finesseBonus + statusModifier;
            
            // Clamp between 0% and 30% (reasonable parry cap)
            return Mathf.Clamp(totalParryChance, 0f, 30f);
        }

        /// <summary>
        /// Get the critical defense chance for a unit (base + status effect modifiers).
        /// </summary>
        private static float GetCritDefense(Unit unit)
        {
            float baseCritDefense = GameConstants.Instance != null ? GameConstants.Instance.BaseCritDefense : 10f;
            
            // Focus affects crit defense (mental awareness helps avoid critical hits)
            float focusBonus = unit.Focus * (GameConstants.Instance != null ? GameConstants.Instance.FocusCritDefensePerPoint : 0.5f);
            
            // Get status effect modifiers
            float statusModifier = unit.GetTotalCritDefenseModifier();
            
            float totalCritDefense = baseCritDefense + focusBonus + statusModifier;
            
            // Clamp between 0% and 50% (reasonable crit defense cap)
            return Mathf.Clamp(totalCritDefense, 0f, 50f);
        }
    }
}
