using UnityEngine;
using Riftbourne.Skills;
using Riftbourne.Characters;

namespace Riftbourne.Items
{
    /// <summary>
    /// Base class for all equipment items.
    /// Can grant skills for learning and provide stat modifiers.
    /// Now includes durability system and extends the base Item class.
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Riftbourne/Items/Equipment Item")]
    public class EquipmentItem : Item
    {
        [Tooltip("Which slots can this item be equipped in? (Can select multiple)")]
        [SerializeField] private System.Collections.Generic.List<EquipmentSlot> compatibleSlots = new System.Collections.Generic.List<EquipmentSlot>();

        [Header("Equipment Type")]
        [Tooltip("The type of melee weapon (only relevant if compatible with MeleeWeapon slot)")]
        [SerializeField] private MeleeWeaponType meleeWeaponType = MeleeWeaponType.None;
        
        [Tooltip("The type of ranged weapon (only relevant if compatible with RangedWeapon slot)")]
        [SerializeField] private RangedWeaponType rangedWeaponType = RangedWeaponType.None;
        
        [Tooltip("The type of armor (only relevant if compatible with Armor slot)")]
        [SerializeField] private ArmorType armorType = ArmorType.None;

        [Header("Ranged Weapon Properties")]
        [Tooltip("Range bonus for ranged weapons (only relevant if compatible with RangedWeapon slot). Base range is 3, this adds to it.")]
        [SerializeField] private int rangeBonus = 0;

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

        [Header("Durability")]
        [Tooltip("Current durability of this equipment item")]
        [SerializeField] private float currentDurability = 100f;
        
        [Tooltip("Maximum durability of this equipment item")]
        [SerializeField] private float maxDurability = 100f;

        // Properties
        public System.Collections.Generic.List<EquipmentSlot> CompatibleSlots => compatibleSlots;
        public MeleeWeaponType MeleeWeaponType => meleeWeaponType;
        public RangedWeaponType RangedWeaponType => rangedWeaponType;
        public ArmorType ArmorType => armorType;
        public int RangeBonus => rangeBonus;
        public System.Collections.Generic.List<Skill> GrantedSkills => grantedSkills;
        public System.Collections.Generic.List<PassiveSkill> GrantedPassiveSkills => grantedPassiveSkills;
        public float CurrentDurability => currentDurability;
        public float MaxDurability => maxDurability;
        
        // Durability properties
        public float RepairCost => Mathf.RoundToInt(baseValue * 0.5f);
        
        /// <summary>
        /// Returns true if this item is broken.
        /// Items with maxDurability <= 0 are considered indestructible (never broken).
        /// </summary>
        public bool IsBroken => maxDurability > 0 && currentDurability <= 0;
        
        /// <summary>
        /// Returns true if this item uses the durability system.
        /// Items with maxDurability <= 0 are indestructible.
        /// </summary>
        public bool HasDurability => maxDurability > 0;

        // Stat bonus properties - return 0 if broken
        public int AttackBonus => IsBroken ? 0 : attackBonus;
        public int DefenseBonus => IsBroken ? 0 : defenseBonus;
        public int StrengthBonus => IsBroken ? 0 : strengthBonus;
        public int FinesseBonus => IsBroken ? 0 : finesseBonus;
        public int FocusBonus => IsBroken ? 0 : focusBonus;
        public int SpeedBonus => IsBroken ? 0 : speedBonus;
        public int LuckBonus => IsBroken ? 0 : luckBonus;
        public float MaxHPBonusPercent => IsBroken ? 0f : maxHPBonusPercent;

        private void OnEnable()
        {
            itemType = ItemType.Equipment;
            maxStackSize = 1; // Equipment never stacks
            
            // Initialize durability if this is a new item (currentDurability is 0)
            if (currentDurability <= 0 && maxDurability > 0)
            {
                ResetDurability();
            }
        }

        /// <summary>
        /// Does this equipment teach any skills?
        /// </summary>
        public bool TeachesSkills => grantedSkills != null && grantedSkills.Count > 0;
        
        /// <summary>
        /// Gets the primary slot for this equipment.
        /// Returns first compatible slot.
        /// </summary>
        public EquipmentSlot PrimarySlot
        {
            get
            {
                if (compatibleSlots != null && compatibleSlots.Count > 0)
                {
                    return compatibleSlots[0];
                }
                // Return MeleeWeapon as default if no slots defined (shouldn't happen in practice)
                return EquipmentSlot.MeleeWeapon;
            }
        }
        
        /// <summary>
        /// Can this item be equipped in the specified slot?
        /// </summary>
        public bool CanEquipInSlot(EquipmentSlot slot)
        {
            return compatibleSlots != null && compatibleSlots.Contains(slot);
        }

        /// <summary>
        /// Reduces the durability of this equipment by the specified amount.
        /// If durability reaches 0 or below, the item is marked as broken.
        /// Does nothing if maxDurability <= 0 (item is indestructible).
        /// </summary>
        public void LoseDurability(float amount)
        {
            // Items with maxDurability <= 0 are indestructible
            if (maxDurability <= 0)
            {
                return;
            }
            
            currentDurability -= amount;
            if (currentDurability <= 0)
            {
                currentDurability = 0;
            }
        }

        /// <summary>
        /// Repairs this equipment item using Aurum Shards.
        /// Returns true if repair was successful, false if insufficient currency.
        /// Items with maxDurability <= 0 cannot be repaired (they're indestructible).
        /// </summary>
        public bool Repair(int aurumShards)
        {
            // Items with maxDurability <= 0 are indestructible and don't need repair
            if (maxDurability <= 0)
            {
                return false;
            }
            
            int cost = Mathf.RoundToInt(RepairCost);
            if (aurumShards >= cost)
            {
                currentDurability = maxDurability;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets durability to maximum.
        /// Called when item is first created or picked up.
        /// </summary>
        public void ResetDurability()
        {
            currentDurability = maxDurability;
        }

        /// <summary>
        /// Override tooltip to include durability information.
        /// </summary>
        public override string GetTooltipText()
        {
            string tooltip = base.GetTooltipText();
            
            // Only show durability if the item uses the durability system
            if (HasDurability)
            {
                tooltip += $"\n\n<b>Durability: {currentDurability:F0}/{maxDurability:F0}</b>";
                
                if (IsBroken)
                {
                    tooltip += " <color=#FF0000>[BROKEN]</color>";
                }
            }
            else
            {
                tooltip += "\n\n<b>Durability: Indestructible</b>";
            }
            
            return tooltip;
        }
    }
}
