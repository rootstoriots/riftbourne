using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Skills
{
    /// <summary>
    /// Defines a skill/ability that characters can use.
    /// This is the DATA layer - it defines WHAT the skill is.
    /// Combat resolution (HOW it executes) happens in Combat/ folder.
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "Riftbourne/Skill")]
    public class Skill : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillName = "New Skill";
        [SerializeField] private int tier = 1;
        [TextArea(3, 5)]
        [SerializeField] private string description = "Skill description";

        [Header("Requirements")]
        [SerializeField] private MantleType requiredMantle = MantleType.None;
        [SerializeField] private int minimumLevel = 1;

        [Header("Effects")]
        [SerializeField] private int baseDamage = 0;
        [SerializeField] private bool appliesBurn = false;
        [SerializeField] private int burnDuration = 3;
        [SerializeField] private int burnDamagePerTurn = 5;

        [Header("Targeting")]
        [SerializeField] private int range = 1; // Manhattan distance
        [SerializeField] private bool requiresLineOfSight = true;

        // Public properties
        public string SkillName => skillName;
        public int Tier => tier;
        public string Description => description;
        public MantleType RequiredMantle => requiredMantle;
        public int MinimumLevel => minimumLevel;
        public int BaseDamage => baseDamage;
        public bool AppliesBurn => appliesBurn;
        public int BurnDuration => burnDuration;
        public int BurnDamagePerTurn => burnDamagePerTurn;
        public int Range => range;
        public bool RequiresLineOfSight => requiresLineOfSight;

        /// <summary>
        /// Checks if a unit can use this skill.
        /// </summary>
        public bool CanUseSkill(Unit user)
        {
            // Check Mantle requirement
            if (requiredMantle != MantleType.None && user.Mantle != requiredMantle)
            {
                Debug.Log($"{user.name} cannot use {skillName} - requires {requiredMantle} Mantle but has {user.Mantle}");
                return false;
            }

            // Future: Check level requirement
            // Future: Check prerequisites

            return true;
        }

        /// <summary>
        /// Checks if a target is in range.
        /// </summary>
        public bool IsInRange(Unit user, Unit target)
        {
            int distance = Mathf.Abs(target.GridX - user.GridX) + Mathf.Abs(target.GridY - user.GridY);
            return distance <= range;
        }
    }
}