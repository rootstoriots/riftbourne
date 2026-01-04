using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Skills;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Handles skill execution - the HOW of skill mechanics.
    /// This is the rules engine that processes skill effects.
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>
        /// Execute a skill from user to target.
        /// Returns true if skill was successfully executed.
        /// </summary>
        public static bool ExecuteSkill(Skill skill, Unit user, Unit target)
        {
            // Validate skill can be used
            if (!skill.CanUseSkill(user))
            {
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

            // Apply damage
            if (skill.BaseDamage > 0)
            {
                int actualDamage = target.TakeDamage(skill.BaseDamage);
                Debug.Log($"{skill.SkillName} deals {actualDamage} damage!");
            }

            // Apply burn effect
            if (skill.AppliesBurn)
            {
                target.ApplyBurn(skill.BurnDamagePerTurn, skill.BurnDuration);
            }

            // Record skill usage for mastery progression
            user.RecordSkillUsage(skill);

            return true;
        }
    }
}