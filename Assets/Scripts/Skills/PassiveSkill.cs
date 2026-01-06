using UnityEngine;

namespace Riftbourne.Skills
{
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

        [Header("Stat Bonuses (Flat)")]
        [SerializeField] private int strengthBonus = 0;
        [SerializeField] private int finesseBonus = 0;
        [SerializeField] private int focusBonus = 0;
        [SerializeField] private int speedBonus = 0;
        [SerializeField] private int luckBonus = 0;
        [SerializeField] private int attackBonus = 0;
        [SerializeField] private int defenseBonus = 0;

        [Header("Percentage Bonuses")]
        [Tooltip("Percentage-based bonuses (e.g., 10 = +10%)")]
        [SerializeField] private float maxHPBonusPercent = 0f;
        [SerializeField] private float movementRangeBonus = 0f;  // Future: +1 movement range

        // Properties
        public string SkillName => skillName;
        public string Description => description;
        public int Tier => tier;
        public int SPCost => spCost;

        // Stat bonuses
        public int StrengthBonus => strengthBonus;
        public int FinesseBonus => finesseBonus;
        public int FocusBonus => focusBonus;
        public int SpeedBonus => speedBonus;
        public int LuckBonus => luckBonus;
        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;

        // Percentage bonuses
        public float MaxHPBonusPercent => maxHPBonusPercent;
        public float MovementRangeBonus => movementRangeBonus;
    }
}