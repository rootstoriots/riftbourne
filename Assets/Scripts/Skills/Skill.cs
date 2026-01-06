using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Skills
{
    /// <summary>
    /// Which stat scales a skill's damage.
    /// </summary>
    public enum StatScaling
    {
        None,       // No stat scaling
        Strength,   // Physical power
        Finesse,    // Precision/ranged
        Focus       // Mental/magic power
    }
    
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
        [SerializeField] private int masteryCost = 3;  // SP required to master this skill

        [Header("Effects")]
        [SerializeField] private int baseDamage = 0;
        [Tooltip("Which attribute scales this skill's damage")]
        [SerializeField] private StatScaling damageScaling = StatScaling.None;
        [Tooltip("Multiplier for stat scaling (e.g., 1.5 = damage + Strength * 1.5)")]
        [SerializeField] private float scalingMultiplier = 1.0f;
        [SerializeField] private bool appliesBurn = false;
        [SerializeField] private int burnDuration = 3;
        [SerializeField] private int burnDamagePerTurn = 5;

        [Header("Ground Hazard")]
        [SerializeField] private bool createsGroundHazard = false;
        [SerializeField] private int hazardDuration = 3;
        [SerializeField] private int hazardDamagePerTurn = 5;

        [Header("Targeting")]
        [SerializeField] private int range = 1; // Manhattan distance
        [SerializeField] private bool requiresLineOfSight = true;

        // Public properties
        public string SkillName => skillName;
        public int Tier => tier;
        public string Description => description;
        public MantleType RequiredMantle => requiredMantle;
        public int MinimumLevel => minimumLevel;
        public int MasteryCost => masteryCost;
        public int BaseDamage => baseDamage;
        public StatScaling DamageScaling => damageScaling;
        public float ScalingMultiplier => scalingMultiplier;
        public bool AppliesBurn => appliesBurn;
        public int BurnDuration => burnDuration;
        public int BurnDamagePerTurn => burnDamagePerTurn;
        public int Range => range;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public bool CreatesGroundHazard => createsGroundHazard;
        public int HazardDuration => hazardDuration;
        public int HazardDamagePerTurn => hazardDamagePerTurn;

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
        
        /// <summary>
        /// Calculate total damage for this skill including stat scaling.
        /// </summary>
        public int CalculateDamage(Unit user)
        {
            float totalDamage = baseDamage;
            
            // Apply stat scaling
            if (damageScaling != StatScaling.None)
            {
                int stat = 0;
                switch (damageScaling)
                {
                    case StatScaling.Strength:
                        stat = user.Strength;
                        break;
                    case StatScaling.Finesse:
                        stat = user.Finesse;
                        break;
                    case StatScaling.Focus:
                        stat = user.Focus;
                        break;
                }
                
                totalDamage += stat * scalingMultiplier;
            }
            
            return Mathf.RoundToInt(totalDamage);
        }
    }
}