using UnityEngine;
using Riftbourne.Skills;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Base class for all equipment items.
    /// Can grant skills for learning and provide stat modifiers.
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Riftbourne/Equipment/Equipment Item")]
    public class EquipmentItem : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string itemName;
        [SerializeField][TextArea(2, 4)] private string description;
        
        [Tooltip("Which slots can this item be equipped in? (Can select multiple)")]
        [SerializeField] private System.Collections.Generic.List<EquipmentSlot> compatibleSlots = new System.Collections.Generic.List<EquipmentSlot>();

        [Header("Skill Learning")]
        [Tooltip("The combat skills this equipment grants access to (can teach multiple skills)")]
        [SerializeField] private System.Collections.Generic.List<Skill> grantedSkills = new System.Collections.Generic.List<Skill>();
        
        [Tooltip("The passive skills this equipment grants access to (permanent bonuses when mastered)")]
        [SerializeField] private System.Collections.Generic.List<PassiveSkill> grantedPassiveSkills = new System.Collections.Generic.List<PassiveSkill>();

        [Header("Stat Modifiers")]
        [Tooltip("Flat stat bonuses")]
        [SerializeField] private int attackBonus = 0;
        [SerializeField] private int defenseBonus = 0;
        [SerializeField] private int strengthBonus = 0;
        [SerializeField] private int finesseBonus = 0;
        [SerializeField] private int focusBonus = 0;
        [SerializeField] private int speedBonus = 0;
        [SerializeField] private int luckBonus = 0;
        
        [Header("Percentage Bonuses")]
        [Tooltip("Percentage-based bonuses (e.g., 10 = +10%)")]
        [SerializeField] private float maxHPBonusPercent = 0f;  // +10% max HP, etc.

        // Properties
        public string ItemName => itemName;
        public string Description => description;
        public System.Collections.Generic.List<EquipmentSlot> CompatibleSlots => compatibleSlots;
        public System.Collections.Generic.List<Skill> GrantedSkills => grantedSkills;
        public System.Collections.Generic.List<PassiveSkill> GrantedPassiveSkills => grantedPassiveSkills;
        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;
        public int StrengthBonus => strengthBonus;
        public int FinesseBonus => finesseBonus;
        public int FocusBonus => focusBonus;
        public int SpeedBonus => speedBonus;
        public int LuckBonus => luckBonus;
        public float MaxHPBonusPercent => maxHPBonusPercent;

        /// <summary>
        /// Does this equipment teach any skills?
        /// </summary>
        public bool TeachesSkills => grantedSkills != null && grantedSkills.Count > 0;
        
        /// <summary>
        /// Can this item be equipped in the specified slot?
        /// </summary>
        public bool CanEquipInSlot(EquipmentSlot slot)
        {
            return compatibleSlots != null && compatibleSlots.Contains(slot);
        }
    }
}