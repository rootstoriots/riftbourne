using System;
using System.Collections.Generic;
using UnityEngine;
using Riftbourne.Combat;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles weapon proficiency tracking and advancement for a unit.
    /// Component class to reduce Unit.cs complexity.
    /// Manages all weapon family proficiencies and determines meaningful combat encounters.
    /// </summary>
    public class WeaponProficiencyManager
    {
        private Unit unit;
        private Dictionary<WeaponFamily, WeaponProficiency> proficiencies;

        public WeaponProficiencyManager(Unit unit)
        {
            this.unit = unit;
            this.proficiencies = new Dictionary<WeaponFamily, WeaponProficiency>();
        }

        /// <summary>
        /// Get proficiency for a specific weapon family.
        /// Creates a new proficiency if one doesn't exist.
        /// </summary>
        public WeaponProficiency GetProficiency(WeaponFamily family)
        {
            if (family == WeaponFamily.None) return null;

            if (!proficiencies.ContainsKey(family))
            {
                proficiencies[family] = new WeaponProficiency(family);
            }

            return proficiencies[family];
        }

        /// <summary>
        /// Initialize all weapon families with Untrained proficiency.
        /// Useful for ensuring all families are tracked from the start.
        /// </summary>
        public void InitializeAllFamilies()
        {
            foreach (WeaponFamily family in System.Enum.GetValues(typeof(WeaponFamily)))
            {
                if (family != WeaponFamily.None && !proficiencies.ContainsKey(family))
                {
                    proficiencies[family] = new WeaponProficiency(family);
                }
            }
        }

        /// <summary>
        /// Get proficiency for the currently equipped weapon.
        /// Returns null if no weapon is equipped or weapon has no family.
        /// </summary>
        public WeaponProficiency GetCurrentWeaponProficiency()
        {
            if (unit?.UnitEquipment == null) return null;

            EquipmentItem meleeWeapon = unit.UnitEquipment.GetEquippedItem(EquipmentSlot.MeleeWeapon);
            EquipmentItem rangedWeapon = unit.UnitEquipment.GetEquippedItem(EquipmentSlot.RangedWeapon);

            WeaponFamily family = WeaponFamily.None;

            if (meleeWeapon != null)
            {
                family = WeaponFamilyHelper.GetWeaponFamily(meleeWeapon);
            }
            else if (rangedWeapon != null)
            {
                family = WeaponFamilyHelper.GetWeaponFamily(rangedWeapon);
            }

            if (family == WeaponFamily.None) return null;

            return GetProficiency(family);
        }

        /// <summary>
        /// Get the effective stat multiplier for a weapon family based on proficiency tier.
        /// Used in combat calculations to apply stat efficiency.
        /// </summary>
        public float GetEffectiveStatMultiplier(WeaponFamily family)
        {
            WeaponProficiency proficiency = GetProficiency(family);
            if (proficiency == null) return 1.0f;

            return ProficiencyEffects.GetStatEfficiencyMultiplier(proficiency.CurrentTier);
        }

        /// <summary>
        /// Record a combat action and advance proficiency if applicable.
        /// Evaluates encounter context to determine if it's meaningful.
        /// </summary>
        public void RecordCombatAction(WeaponFamily weaponFamily, bool wasHit, bool wasKill, bool wasCrit, Unit target)
        {
            if (weaponFamily == WeaponFamily.None) return;

            WeaponProficiency proficiency = GetProficiency(weaponFamily);
            if (proficiency == null) return;

            // Determine if this is a meaningful encounter
            bool isMeaningful = IsMeaningfulEncounter(target);

            proficiency.RecordCombatOutcome(wasHit, wasKill, wasCrit, isMeaningful);
        }

        /// <summary>
        /// Determine if an encounter is meaningful for proficiency advancement.
        /// Trivial or low-risk encounters give little or no progress.
        /// Uses configurable thresholds from ProficiencySettings.
        /// </summary>
        private bool IsMeaningfulEncounter(Unit target)
        {
            if (target == null || unit == null) return false;

            // Check if target is alive and poses a threat
            if (!target.IsAlive)
            {
                return false; // Already dead, not meaningful
            }

            ProficiencySettings settings = ProficiencySettings.Instance;

            // Check if target is significantly weaker (trivial encounter)
            // Compare HP, stats, or other indicators of difficulty
            int targetMaxHP = target.MaxHP;
            int unitMaxHP = unit.MaxHP;

            // If target has less than configured percentage of unit's HP, it's likely trivial
            if (targetMaxHP < unitMaxHP * settings.MeaningfulEncounterMinHPRatio)
            {
                return false; // Trivial encounter
            }

            // Check relative strength (attack power comparison)
            int targetAttack = target.AttackPower;
            int unitAttack = unit.AttackPower;

            // If target is significantly weaker in attack power AND HP, it's trivial
            if (targetAttack < unitAttack * settings.TrivialEncounterAttackRatio && 
                targetMaxHP < unitMaxHP * settings.TrivialEncounterHPRatio)
            {
                return false; // Trivial encounter
            }

            // Otherwise, it's a meaningful encounter
            return true;
        }

        /// <summary>
        /// Get all proficiencies for display purposes.
        /// </summary>
        public Dictionary<WeaponFamily, WeaponProficiency> GetAllProficiencies()
        {
            return new Dictionary<WeaponFamily, WeaponProficiency>(proficiencies);
        }

        /// <summary>
        /// Initialize proficiencies from serialized data (for save/load).
        /// </summary>
        public void InitializeFromData(Dictionary<WeaponFamily, WeaponProficiency> data)
        {
            if (data != null)
            {
                proficiencies = new Dictionary<WeaponFamily, WeaponProficiency>(data);
            }
        }

        /// <summary>
        /// Get serializable data for save/load.
        /// </summary>
        public Dictionary<WeaponFamily, WeaponProficiency> GetSerializableData()
        {
            return new Dictionary<WeaponFamily, WeaponProficiency>(proficiencies);
        }
    }
}
