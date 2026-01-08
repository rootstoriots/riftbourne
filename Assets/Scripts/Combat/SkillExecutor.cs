using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Skills;
using Riftbourne.Grid;
using Riftbourne.Core;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Handles skill execution - the HOW of skill mechanics.
    /// This is the rules engine that processes skill effects.
    /// Registered with ManagerRegistry for dependency injection.
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        public static SkillExecutor Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                ManagerRegistry.Register(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ManagerRegistry.Unregister(this);
                Instance = null;
            }
        }

        /// <summary>
        /// Execute a skill from user to target.
        /// Returns true if skill was successfully executed.
        /// </summary>
        public bool ExecuteSkill(Skill skill, Unit user, Unit target)
        {
            // Null checks
            if (skill == null)
            {
                Debug.LogError("SkillExecutor: Skill is null!");
                return false;
            }

            if (user == null)
            {
                Debug.LogError("SkillExecutor: User unit is null!");
                return false;
            }

            if (target == null)
            {
                Debug.LogError("SkillExecutor: Target unit is null!");
                return false;
            }

            // Validate skill can be used (Mantle requirements)
            if (!skill.CanUseSkill(user))
            {
                return false;
            }

            // Validate user has access to this skill (from equipment, mastery, or known skills)
            if (!user.CanUseSkill(skill))
            {
                Debug.Log($"{user.UnitName} does not have access to {skill.SkillName}!");
                return false;
            }

            // Validate target is in range
            if (!skill.IsInRange(user, target))
            {
                Debug.Log($"{target.UnitName} is out of range for {skill.SkillName}!");
                return false;
            }

            // Validate target is alive
            if (!target.IsAlive)
            {
                return false;
            }

            Debug.Log($"{user.UnitName} casts {skill.SkillName} on {target.UnitName}!");

            // Calculate and apply damage (includes stat scaling)
            int damage = skill.CalculateDamage(user);
            if (damage > 0)
            {
                int actualDamage = target.TakeDamage(damage);
                Debug.Log($"{skill.SkillName} deals {actualDamage} damage!");
            }

            // Apply status effect using StatusEffectData
            if (skill.AppliesStatusEffect && skill.StatusEffectData != null)
            {
                int duration = skill.StatusEffectDurationOverride > 0 ? skill.StatusEffectDurationOverride : 3; // Default fallback
                target.ApplyStatusEffect(skill.StatusEffectData, duration);
            }

            // Record action and award XP
            user.RecordAction();  // Award SP based on action count
            int baseXP = GameConstants.Instance != null ? GameConstants.Instance.BaseActionXP : 5;
            user.AwardXP(baseXP);
            
            // Award bonus XP if target died
            if (!target.IsAlive)
            {
                int killBonus = GameConstants.Instance != null ? GameConstants.Instance.KillBonusXP : 25;
                user.AwardXP(killBonus);
            }

            // Mark that user has acted this turn
            user.MarkAsActed();

            return true;
        }

        /// <summary>
        /// Execute a ground-targeted skill (creates hazards, AOE effects, etc.)
        /// Returns true if skill was successfully executed.
        /// </summary>
        public bool ExecuteGroundSkill(Skill skill, Unit user, int targetX, int targetY)
        {
            // Null checks
            if (skill == null)
            {
                Debug.LogError("SkillExecutor: Skill is null!");
                return false;
            }

            if (user == null)
            {
                Debug.LogError("SkillExecutor: User unit is null!");
                return false;
            }

            // Validate skill can be used (Mantle requirements)
            if (!skill.CanUseSkill(user))
            {
                return false;
            }

            // Validate user has access to this skill (from equipment, mastery, or known skills)
            if (!user.CanUseSkill(skill))
            {
                Debug.Log($"{user.UnitName} does not have access to {skill.SkillName}!");
                return false;
            }

            // Validate range (Manhattan distance)
            int distance = Mathf.Abs(targetX - user.GridX) + Mathf.Abs(targetY - user.GridY);
            if (distance > skill.Range)
            {
                Debug.Log($"Target ({targetX}, {targetY}) is out of range for {skill.SkillName}!");
                return false;
            }

            // Don't allow targeting self position (distance must be > 0)
            if (distance == 0)
            {
                Debug.Log($"Cannot cast {skill.SkillName} on your own position!");
                return false;
            }

            // Validate target position is valid before proceeding
            GridManager gridManager = ManagerRegistry.Get<GridManager>();
            if (gridManager != null && !gridManager.IsValidGridPosition(targetX, targetY))
            {
                Debug.LogWarning($"Cannot cast {skill.SkillName} at invalid position ({targetX}, {targetY})!");
                return false;
            }

            Debug.Log($"{user.UnitName} casts {skill.SkillName} at ({targetX}, {targetY})!");

            // Create ground hazard if skill has this effect
            if (skill.CreatesGroundHazard)
            {
                HazardManager hazardManager = ManagerRegistry.Get<HazardManager>();
                if (hazardManager == null)
                {
                    Debug.LogError("HazardManager not found in scene!");
                    return false;
                }

                // Get hazard data from skill
                Grid.HazardData hazardData = skill.HazardData;
                if (hazardData == null)
                {
                    Debug.LogWarning($"Skill {skill.SkillName} creates ground hazard but no HazardData is assigned!");
                    return false;
                }

                // Use damage/duration from skill if set, otherwise use defaults from HazardData
                int directDamage = skill.HazardDamagePerTurn > 0 ? skill.HazardDamagePerTurn : hazardData.DirectDamagePerTurn;
                int duration = skill.HazardDuration > 0 ? skill.HazardDuration : 0; // 0 means use HazardData default

                // Use the new modular CreateHazard method with the skill's HazardData
                hazardManager.CreateHazard(hazardData, targetX, targetY, directDamage, duration);
            }

            // Record action and award XP
            user.RecordAction();  // Award SP based on action count
            int baseXP = GameConstants.Instance != null ? GameConstants.Instance.BaseActionXP : 5;
            user.AwardXP(baseXP);  // Award XP for successful skill use

            // Mark that user has acted this turn
            user.MarkAsActed();

            return true;
        }
    }
}