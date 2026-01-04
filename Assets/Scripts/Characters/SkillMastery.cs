using System;
using Riftbourne.Skills;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Tracks usage and mastery progress for a single skill.
    /// </summary>
    [Serializable]
    public class SkillMastery
    {
        public Skill skill;
        public int usageCount;
        public int masteryThreshold;
        public bool isMastered;

        public SkillMastery(Skill skill, int masteryThreshold)
        {
            this.skill = skill;
            this.usageCount = 0;
            this.masteryThreshold = masteryThreshold;
            this.isMastered = false;
        }

        /// <summary>
        /// Increment usage and check if mastery achieved.
        /// </summary>
        /// <returns>True if mastery was just achieved this use</returns>
        public bool IncrementUsage()
        {
            if (isMastered) return false; // Already mastered

            usageCount++;

            if (usageCount >= masteryThreshold)
            {
                isMastered = true;
                UnityEngine.Debug.Log($"🎉 MASTERY ACHIEVED: {skill.SkillName}! You can now use this skill without equipment.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Progress percentage (0-1)
        /// </summary>
        public float Progress => masteryThreshold > 0 ? (float)usageCount / masteryThreshold : 0f;

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public float ProgressPercent => Progress * 100f;
    }
}