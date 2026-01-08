using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Skills
{
    /// <summary>
    /// When a passive skill becomes active (grants bonuses).
    /// </summary>
    public enum PassiveSkillActivationMode
    {
        OnEquip,    // Active immediately when granted by equipment
        OnMastery   // Only active after spending SP to master it
    }

    /// <summary>
    /// Passive skills provide permanent bonuses when mastered.
    /// Unlike combat skills, these don't have active effects - they're always-on bonuses.
    /// </summary>
    [CreateAssetMenu(fileName = "New Passive Skill", menuName = "Riftbourne/Skills/Passive Skill")]
    public class PassiveSkill : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillName;
        [SerializeField][TextArea(2, 4)] private string description;
        [SerializeField] private int tier = 1;

        [Header("SP Cost")]
        [Tooltip("How much SP required to master this passive skill")]
        [SerializeField] private int spCost = 3;

        [Header("Activation")]
        [Tooltip("When does this passive skill become active?\nOnEquip: Active immediately when granted by equipment\nOnMastery: Only active after spending SP to master it")]
        [SerializeField] private PassiveSkillActivationMode activationMode = PassiveSkillActivationMode.OnMastery;

        [Header("Stat Bonuses (Flat)")]
        [SerializeField] private int strengthBonus = 0;
        [SerializeField] private int finesseBonus = 0;
        [SerializeField] private int focusBonus = 0;
        [SerializeField] private int speedBonus = 0;
        [SerializeField] private int luckBonus = 0;
        [SerializeField] private int attackBonus = 0;
        [SerializeField] private int defenseBonus = 0;

        [Header("HP Bonuses")]
        [Tooltip("Flat HP bonus (added directly to max HP)")]
        [SerializeField] private int maxHPBonusFlat = 0;
        [Tooltip("Percentage-based HP bonus (e.g., 10 = +10%)")]
        [SerializeField] private float maxHPBonusPercent = 0f;

        [Header("Movement Range Bonus")]
        [Tooltip("Flat movement range bonus (e.g., 1 = +1 movement range)")]
        [SerializeField] private int movementRangeBonus = 0;

        // Properties
        public string SkillName => skillName;
        public string Description => description;
        public int Tier => tier;
        public int SPCost => spCost;
        public PassiveSkillActivationMode ActivationMode => activationMode;

        // Stat bonuses
        public int StrengthBonus => strengthBonus;
        public int FinesseBonus => finesseBonus;
        public int FocusBonus => focusBonus;
        public int SpeedBonus => speedBonus;
        public int LuckBonus => luckBonus;
        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;

        // HP bonuses
        public int MaxHPBonusFlat => maxHPBonusFlat;
        public float MaxHPBonusPercent => maxHPBonusPercent;
        
        // Movement range bonus
        public int MovementRangeBonus => movementRangeBonus;

        /// <summary>
        /// Gets the stat bonus for a given StatType.
        /// Provides type-safe access to stat bonuses using the StatType enum.
        /// </summary>
        public int GetStatBonus(StatType statType)
        {
            switch (statType)
            {
                case StatType.Attack: return attackBonus;
                case StatType.Defense: return defenseBonus;
                case StatType.Strength: return strengthBonus;
                case StatType.Finesse: return finesseBonus;
                case StatType.Focus: return focusBonus;
                case StatType.Speed: return speedBonus;
                case StatType.Luck: return luckBonus;
                default: return 0;
            }
        }
    }
}