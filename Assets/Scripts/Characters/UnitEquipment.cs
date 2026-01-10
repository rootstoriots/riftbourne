using Riftbourne.Skills;
using System;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles equipment management and stat bonuses for a unit.
    /// Component class to reduce Unit.cs complexity.
    /// </summary>
    public class UnitEquipment
    {
        private Unit unit;
        private Dictionary<EquipmentSlot, EquipmentItem> equippedItems = new Dictionary<EquipmentSlot, EquipmentItem>();
        private HashSet<Skill> masteredSkills;
        private HashSet<PassiveSkill> masteredPassiveSkills;
        private List<Skill> knownSkills;

        public UnitEquipment(Unit unit, HashSet<Skill> masteredSkills, HashSet<PassiveSkill> masteredPassiveSkills, List<Skill> knownSkills)
        {
            this.unit = unit;
            this.masteredSkills = masteredSkills;
            this.masteredPassiveSkills = masteredPassiveSkills;
            this.knownSkills = knownSkills;
        }

        public Dictionary<EquipmentSlot, EquipmentItem> EquippedItems => equippedItems;

        /// <summary>
        /// Event raised when equipment is equipped or unequipped.
        /// Allows UI systems to refresh skill lists and other equipment-dependent displays.
        /// </summary>
        public event Action<EquipmentSlot, EquipmentItem> OnEquipmentChanged;

        public bool EquipItem(EquipmentItem item, EquipmentSlot targetSlot)
        {
            if (item == null) return false;
            
            if (!item.CanEquipInSlot(targetSlot))
            {
                UnityEngine.Debug.LogWarning($"{item.ItemName} cannot be equipped in {targetSlot} slot!");
                return false;
            }

            if (equippedItems.ContainsKey(targetSlot))
            {
                UnequipItem(targetSlot);
            }

            equippedItems[targetSlot] = item;
            UnityEngine.Debug.Log($"{unit.UnitName} equipped {item.ItemName} in {targetSlot} slot.");
            
            // Log skills granted by this equipment
            if (item.TeachesSkills && item.GrantedSkills != null && item.GrantedSkills.Count > 0)
            {
                UnityEngine.Debug.Log($"{unit.UnitName} gained access to skills from {item.ItemName}:");
                foreach (Skill skill in item.GrantedSkills)
                {
                    if (skill != null)
                    {
                        UnityEngine.Debug.Log($"  - {skill.SkillName}");
                    }
                }
            }
            
            // Notify listeners that equipment changed
            OnEquipmentChanged?.Invoke(targetSlot, item);
            
            return true;
        }

        public bool EquipItem(EquipmentItem item)
        {
            if (item == null)
            {
                UnityEngine.Debug.LogWarning($"Cannot equip null item!");
                return false;
            }
            
            // Use PrimarySlot which handles both compatibleSlots and legacy slotType
            EquipmentSlot primarySlot = item.PrimarySlot;
            return EquipItem(item, primarySlot);
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.ContainsKey(slot)) return false;

            EquipmentItem item = equippedItems[slot];
            equippedItems.Remove(slot);

            UnityEngine.Debug.Log($"{unit.UnitName} unequipped {item.ItemName} from {slot} slot.");
            
            // Log skills revoked by unequipping this equipment
            if (item.TeachesSkills && item.GrantedSkills != null && item.GrantedSkills.Count > 0)
            {
                UnityEngine.Debug.Log($"{unit.UnitName} lost access to skills from {item.ItemName}:");
                foreach (Skill skill in item.GrantedSkills)
                {
                    if (skill != null)
                    {
                        UnityEngine.Debug.Log($"  - {skill.SkillName}");
                    }
                }
            }
            
            // Notify listeners that equipment changed (item is null when unequipped)
            OnEquipmentChanged?.Invoke(slot, null);
            
            return true;
        }

        public EquipmentItem GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
        }

        public bool CanUseSkill(Skill skill)
        {
            if (skill == null) return false;

            if (knownSkills != null && knownSkills.Contains(skill)) return true;
            if (masteredSkills != null && masteredSkills.Contains(skill)) return true;

            foreach (var equipped in equippedItems.Values)
            {
                if (equipped != null && equipped.TeachesSkills && equipped.GrantedSkills != null && equipped.GrantedSkills.Contains(skill))
                    return true;
            }

            return false;
        }

        public List<Skill> GetAvailableSkills()
        {
            List<Skill> available = new List<Skill>();
            available.AddRange(knownSkills);

            foreach (Skill skill in masteredSkills)
            {
                if (!available.Contains(skill))
                    available.Add(skill);
            }

            foreach (var item in equippedItems.Values)
            {
                if (item != null && item.TeachesSkills)
                {
                if (item.GrantedSkills == null)
                {
                    // Only log warning once, not every frame
                    // UnityEngine.Debug.LogWarning($"{unit.UnitName}: Equipment item {item.ItemName} has TeachesSkills=true but GrantedSkills is null!");
                    continue;
                }
                
                if (item.GrantedSkills.Count == 0)
                {
                    // Only log warning once, not every frame
                    // UnityEngine.Debug.LogWarning($"{unit.UnitName}: Equipment item {item.ItemName} has TeachesSkills=true but GrantedSkills list is empty!");
                    continue;
                }
                    
                    foreach (Skill skill in item.GrantedSkills)
                    {
                        if (skill == null)
                        {
                            // Only log warning once, not every frame
                            // UnityEngine.Debug.LogWarning($"{unit.UnitName}: Equipment item {item.ItemName} has a null skill in GrantedSkills!");
                            continue;
                        }
                        
                        if (!available.Contains(skill))
                        {
                            available.Add(skill);
                            // Debug logging removed - was causing spam when called every frame
                            // UnityEngine.Debug.Log($"{unit.UnitName}: Added skill '{skill.SkillName}' from equipment {item.ItemName}");
                        }
                    }
                }
            }

            // Only log when skills actually change (not every frame)
            // Debug logging moved to only log when skills are first added/removed
            // UnityEngine.Debug.Log($"{unit.UnitName}: Total available skills: {available.Count}");

            return available;
        }

        public int GetTotalEquipmentBonus(StatType statType)
        {
            int totalBonus = 0;
            
            foreach (var item in equippedItems.Values)
            {
                switch (statType)
                {
                    case StatType.Attack: totalBonus += item.AttackBonus; break;
                    case StatType.Defense: totalBonus += item.DefenseBonus; break;
                    case StatType.Strength: totalBonus += item.StrengthBonus; break;
                    case StatType.Finesse: totalBonus += item.FinesseBonus; break;
                    case StatType.Focus: totalBonus += item.FocusBonus; break;
                    case StatType.Speed: totalBonus += item.SpeedBonus; break;
                    case StatType.Luck: totalBonus += item.LuckBonus; break;
                }
            }
            
            return totalBonus;
        }

        /// <summary>
        /// Gets all passive skills that are currently active.
        /// A passive skill is active if:
        /// - It's OnEquip mode and granted by currently equipped items, OR
        /// - It's OnMastery mode and has been mastered
        /// </summary>
        private HashSet<PassiveSkill> GetActivePassiveSkills()
        {
            HashSet<PassiveSkill> active = new HashSet<PassiveSkill>();
            
            // Add mastered passive skills (always active if mastered)
            foreach (var passive in masteredPassiveSkills)
            {
                active.Add(passive);
            }
            
            // Add OnEquip passive skills from currently equipped items
            foreach (var item in equippedItems.Values)
            {
                if (item.GrantedPassiveSkills != null)
                {
                    foreach (var passive in item.GrantedPassiveSkills)
                    {
                        if (passive != null && passive.ActivationMode == PassiveSkillActivationMode.OnEquip)
                        {
                            active.Add(passive);
                        }
                    }
                }
            }
            
            return active;
        }

        public int GetTotalPassiveSkillBonus(StatType statType)
        {
            int totalBonus = 0;
            
            foreach (var passive in GetActivePassiveSkills())
            {
                totalBonus += passive.GetStatBonus(statType);
            }
            
            return totalBonus;
        }

        public int GetTotalMaxHPBonusFlat()
        {
            int flatBonus = 0;
            
            foreach (var passive in GetActivePassiveSkills())
            {
                flatBonus += passive.MaxHPBonusFlat;
            }
            
            return flatBonus;
        }

        public float GetTotalMaxHPBonusPercent()
        {
            float percentBonus = 0f;
            
            foreach (var item in equippedItems.Values)
            {
                percentBonus += item.MaxHPBonusPercent;
            }
            
            foreach (var passive in GetActivePassiveSkills())
            {
                percentBonus += passive.MaxHPBonusPercent;
            }
            
            return percentBonus;
        }

        public int GetTotalMovementRangeBonus()
        {
            int totalBonus = 0;
            
            foreach (var passive in GetActivePassiveSkills())
            {
                totalBonus += passive.MovementRangeBonus;
            }
            
            return totalBonus;
        }

        /// <summary>
        /// Gets the total range bonus from equipped ranged weapons.
        /// Only counts bonuses from items equipped in the RangedWeapon slot.
        /// </summary>
        public int GetRangedWeaponRangeBonus()
        {
            if (equippedItems.TryGetValue(EquipmentSlot.RangedWeapon, out EquipmentItem rangedWeapon) && rangedWeapon != null)
            {
                return rangedWeapon.RangeBonus;
            }
            return 0;
        }

        /// <summary>
        /// Checks if the unit has a ranged weapon equipped.
        /// </summary>
        public bool HasRangedWeapon()
        {
            return equippedItems.ContainsKey(EquipmentSlot.RangedWeapon) && equippedItems[EquipmentSlot.RangedWeapon] != null;
        }

        public List<PassiveSkill> GetAvailablePassiveSkills()
        {
            List<PassiveSkill> available = new List<PassiveSkill>();
            
            foreach (var item in equippedItems.Values)
            {
                if (item.GrantedPassiveSkills != null)
                {
                    foreach (PassiveSkill passiveSkill in item.GrantedPassiveSkills)
                    {
                        if (!available.Contains(passiveSkill))
                            available.Add(passiveSkill);
                    }
                }
            }
            
            return available;
        }
    }
}

