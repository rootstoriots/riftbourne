using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Grid;
using Riftbourne.Combat;

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
        
        [Header("Status Effects")]
        [Tooltip("Does this skill apply a status effect to the target?")]
        [SerializeField] private bool appliesStatusEffect = false;
        [Tooltip("Status effect data to apply (only used if appliesStatusEffect is true). Assign a StatusEffectData ScriptableObject here.")]
        [SerializeField] private Combat.StatusEffectData statusEffectData;
        [Tooltip("Duration override for status effect (uses StatusEffectData default if 0)")]
        [SerializeField] private int statusEffectDurationOverride = 0;
        
        [Header("Legacy Burn Settings (Deprecated - use Status Effects above)")]
        [SerializeField] private bool appliesBurn = false;
        [SerializeField] private int burnDuration = 3;
        [SerializeField] private int burnDamagePerTurn = 5;

        [Header("Ground Hazard")]
        [SerializeField] private bool createsGroundHazard = false;
        [Tooltip("Hazard data to use when creating ground hazard. Assign a HazardData ScriptableObject here.")]
        [SerializeField] private Grid.HazardData hazardData;
        [Tooltip("Duration for the hazard (overrides HazardData if set)")]
        [SerializeField] private int hazardDuration = 3;
        [Tooltip("Damage per turn for the hazard (overrides HazardData if set, or uses default from HazardData)")]
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
        
        // New status effect system
        public bool AppliesStatusEffect => appliesStatusEffect || appliesBurn; // Support legacy burn
        public Combat.StatusEffectData StatusEffectData => statusEffectData;
        public int StatusEffectDurationOverride => statusEffectDurationOverride;
        
        // Legacy burn properties (for backward compatibility)
        public bool AppliesBurn => appliesBurn || (appliesStatusEffect && statusEffectData != null && statusEffectData.EffectName == "Burn");
        public int BurnDuration => burnDuration;
        public int BurnDamagePerTurn => burnDamagePerTurn;
        public int Range => range;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public bool CreatesGroundHazard => createsGroundHazard;
        public Grid.HazardData HazardData => hazardData;
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