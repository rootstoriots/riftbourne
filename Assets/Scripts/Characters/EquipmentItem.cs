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
        [SerializeField] private EquipmentSlot slotType;

        [Header("Skill Learning")]
        [Tooltip("The skills this equipment grants access to (can teach multiple skills)")]
        [SerializeField] private System.Collections.Generic.List<Skill> grantedSkills = new System.Collections.Generic.List<Skill>();

        [Tooltip("How many times each skill must be used to master it (0 = cannot be mastered)")]
        [SerializeField] private int masteryThreshold = 5;

        [Header("Stat Modifiers (Future)")]
        [Tooltip("Future: Attack/Defense bonuses, etc.")]
        [SerializeField] private int attackBonus = 0;
        [SerializeField] private int defenseBonus = 0;

        // Properties
        public string ItemName => itemName;
        public string Description => description;
        public EquipmentSlot SlotType => slotType;
        public System.Collections.Generic.List<Skill> GrantedSkills => grantedSkills;
        public int MasteryThreshold => masteryThreshold;
        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;

        /// <summary>
        /// Does this equipment teach any skills?
        /// </summary>
        public bool TeachesSkills => grantedSkills != null && grantedSkills.Count > 0;
    }
}