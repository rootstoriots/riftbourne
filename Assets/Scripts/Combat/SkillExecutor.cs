using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Skills;
using Riftbourne.Grid;
using Riftbourne.Core;
using Riftbourne.Items;
using System.Collections.Generic;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Handles skill execution - the HOW of skill mechanics.
    /// This is the rules engine that processes skill effects.
    /// Registered with ManagerRegistry for dependency injection.
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        public static SkillExecutor Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                ManagerRegistry.Register(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ManagerRegistry.Unregister(this);
                Instance = null;
            }
        }

        /// <summary>
        /// Execute a skill from user to target.
        /// Returns true if skill was successfully executed.
        /// </summary>
        public bool ExecuteSkill(Skill skill, Unit user, Unit target)
        {
            // Null checks
            if (skill == null)
            {
                Debug.LogError("SkillExecutor: Skill is null!");
                return false;
            }

            if (user == null)
            {
                Debug.LogError("SkillExecutor: User unit is null!");
                return false;
            }

            if (target == null)
            {
                Debug.LogError("SkillExecutor: Target unit is null!");
                return false;
            }

            // Validate skill can be used (Mantle requirements)
            if (!skill.CanUseSkill(user))
            {
                return false;
            }

            // Validate user has access to this skill (from equipment, mastery, or known skills)
            if (!user.CanUseSkill(skill))
            {
                Debug.Log($"{user.UnitName} does not have access to {skill.SkillName}!");
                return false;
            }

            // Validate target is in range
            if (!skill.IsInRange(user, target))
            {
                Debug.Log($"{target.UnitName} is out of range for {skill.SkillName}!");
                return false;
            }

            // Validate target is alive
            if (!target.IsAlive)
            {
                return false;
            }

            Debug.Log($"{user.UnitName} casts {skill.SkillName} on {target.UnitName}!");

            // Get weapon family and proficiency for this skill
            WeaponFamily weaponFamily = WeaponFamily.None;
            WeaponProficiencyTier? proficiencyTier = null;
            
            // Determine weapon family from equipped weapon
            EquipmentItem meleeWeapon = user.UnitEquipment?.GetEquippedItem(EquipmentSlot.MeleeWeapon);
            EquipmentItem rangedWeapon = user.UnitEquipment?.GetEquippedItem(EquipmentSlot.RangedWeapon);
            
            if (meleeWeapon != null)
            {
                weaponFamily = WeaponFamilyHelper.GetWeaponFamily(meleeWeapon);
            }
            else if (rangedWeapon != null)
            {
                weaponFamily = WeaponFamilyHelper.GetWeaponFamily(rangedWeapon);
            }
            
            if (weaponFamily != WeaponFamily.None && user.WeaponProficiencyManager != null)
            {
                var proficiency = user.WeaponProficiencyManager.GetProficiency(weaponFamily);
                if (proficiency != null)
                {
                    proficiencyTier = proficiency.CurrentTier;
                }
            }

            // Check if this is an AOE skill
            if (skill.AOEType != AOEType.None && skill.AOEPattern != AOEPatternType.None)
            {
                // Execute AOE skill - affects multiple targets
                ExecuteAOESkill(skill, user, target.GridX, target.GridY, target, proficiencyTier);
            }
            else
            {
                // Single target skill - apply to target only
                ApplySkillEffectsToTarget(skill, user, target, proficiencyTier);
            }

            // Raise skill used event
            GameEvents.RaiseSkillUsed(user, skill, target);
            
            // Track skill usage for statistics
            if (BattleStatisticsTracker.Instance != null)
            {
                BattleStatisticsTracker.Instance.RecordSkillUsed(user);
            }
            
            // Record action for SP system
            user.RecordAction();

            // Record combat outcome for proficiency advancement (if skill deals damage)
            if (weaponFamily != WeaponFamily.None && user.WeaponProficiencyManager != null && skill.BaseDamage > 0)
            {
                bool wasAlive = target.IsAlive;
                // Note: We can't determine if it was a crit from skill damage, so we'll use false
                // In the future, we could track crits from skills if needed
                bool wasHit = true; // Skills always "hit" if they execute
                bool wasKill = wasAlive && !target.IsAlive;
                bool wasCrit = false; // Skills don't currently have crit tracking
                user.WeaponProficiencyManager.RecordCombatAction(weaponFamily, wasHit, wasKill, wasCrit, target);
            }

            // Mark that user has acted this turn
            user.MarkAsActed();

            return true;
        }

        /// <summary>
        /// Execute a ground-targeted skill (creates hazards, AOE effects, etc.)
        /// Returns true if skill was successfully executed.
        /// </summary>
        public bool ExecuteGroundSkill(Skill skill, Unit user, int targetX, int targetY)
        {
            // Null checks
            if (skill == null)
            {
                Debug.LogError("SkillExecutor: Skill is null!");
                return false;
            }

            if (user == null)
            {
                Debug.LogError("SkillExecutor: User unit is null!");
                return false;
            }

            // Validate skill can be used (Mantle requirements)
            if (!skill.CanUseSkill(user))
            {
                return false;
            }

            // Validate user has access to this skill (from equipment, mastery, or known skills)
            if (!user.CanUseSkill(skill))
            {
                Debug.Log($"{user.UnitName} does not have access to {skill.SkillName}!");
                return false;
            }

            // Validate range (Manhattan distance)
            int distance = Mathf.Abs(targetX - user.GridX) + Mathf.Abs(targetY - user.GridY);
            if (distance > skill.Range)
            {
                Debug.Log($"Target ({targetX}, {targetY}) is out of range for {skill.SkillName}!");
                return false;
            }

            // Don't allow targeting self position (distance must be > 0)
            if (distance == 0)
            {
                Debug.Log($"Cannot cast {skill.SkillName} on your own position!");
                return false;
            }

            // Validate target position is valid before proceeding
            GridManager gridManager = ManagerRegistry.Get<GridManager>();
            if (gridManager != null && !gridManager.IsValidGridPosition(targetX, targetY))
            {
                Debug.LogWarning($"Cannot cast {skill.SkillName} at invalid position ({targetX}, {targetY})!");
                return false;
            }

            Debug.Log($"{user.UnitName} casts {skill.SkillName} at ({targetX}, {targetY})!");

            // Check if this is an AOE skill
            if (skill.AOEType != AOEType.None && skill.AOEPattern != AOEPatternType.None)
            {
                // Execute AOE skill - affects multiple targets
                ExecuteAOESkill(skill, user, targetX, targetY, null);
            }
            else
            {
                // Non-AOE ground skill - just create hazard if applicable
                if (skill.CreatesGroundHazard)
                {
                    CreateGroundHazard(skill, targetX, targetY);
                }
            }

            // Raise skill used event (ground skill, no target)
            GameEvents.RaiseSkillUsed(user, skill, null);
            
            // Track skill usage for statistics
            if (BattleStatisticsTracker.Instance != null)
            {
                BattleStatisticsTracker.Instance.RecordSkillUsed(user);
            }
            
            // Record action for SP system
            user.RecordAction();

            // Mark that user has acted this turn
            user.MarkAsActed();

            return true;
        }

        /// <summary>
        /// Create a ground hazard at the specified location.
        /// </summary>
        private void CreateGroundHazard(Skill skill, int targetX, int targetY)
        {
            HazardManager hazardManager = ManagerRegistry.Get<HazardManager>();
            if (hazardManager == null)
            {
                Debug.LogError("HazardManager not found in scene!");
                return;
            }

            // Get hazard data from skill
            Grid.HazardData hazardData = skill.HazardData;
            if (hazardData == null)
            {
                Debug.LogWarning($"Skill {skill.SkillName} creates ground hazard but no HazardData is assigned!");
                return;
            }

            // Use damage/duration from skill if set, otherwise use defaults from HazardData
            int directDamage = skill.HazardDamagePerTurn > 0 ? skill.HazardDamagePerTurn : hazardData.DirectDamagePerTurn;
            int duration = skill.HazardDuration > 0 ? skill.HazardDuration : 0; // 0 means use HazardData default

            // Use the new modular CreateHazard method with the skill's HazardData
            hazardManager.CreateHazard(hazardData, targetX, targetY, directDamage, duration);
        }

        /// <summary>
        /// Execute an AOE skill that affects multiple targets.
        /// </summary>
        private void ExecuteAOESkill(Skill skill, Unit user, int targetX, int targetY, Unit primaryTarget, WeaponProficiencyTier? proficiencyTier = null)
        {
            GridManager gridManager = ManagerRegistry.Get<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("SkillExecutor: GridManager not found for AOE skill!");
                return;
            }

            // Calculate affected cells based on AOE pattern
            List<GridCell> affectedCells = AOECalculator.GetAffectedCells(
                skill.AOEPattern,
                skill.AOEType,
                user.GridX,
                user.GridY,
                targetX,
                targetY,
                skill.AOESize,
                gridManager
            );

            Debug.Log($"{skill.SkillName} AOE affects {affectedCells.Count} cell(s) at target ({targetX}, {targetY})!");

            // For LineLimited, only affect the first unit encountered in the line
            // For all other patterns, affect all units in all affected cells (even if targeting empty ground)
            HashSet<Unit> affectedUnits = new HashSet<Unit>();
            
            if (skill.AOEPattern == AOEPatternType.LineLimited)
            {
                // LineLimited: Only affect the first unit encountered in the line
                // GetLineLimitedCells already returns only the cell with the closest unit
                // But we need to ensure we're getting units from the actual line, not just the returned cell
                // So we'll iterate through affected cells (which should already be limited) and get first unit
                foreach (GridCell cell in affectedCells)
                {
                    if (cell.OccupyingUnit != null && cell.OccupyingUnit.IsAlive)
                    {
                        affectedUnits.Add(cell.OccupyingUnit);
                        break; // Only first unit for LineLimited
                    }
                }
            }
            else
            {
                // All other AOE patterns: Affect ALL units in ALL affected cells
                // This works even when targeting empty ground - it finds units in the AOE area
                foreach (GridCell cell in affectedCells)
                {
                    if (cell.OccupyingUnit != null && cell.OccupyingUnit.IsAlive)
                    {
                        affectedUnits.Add(cell.OccupyingUnit);
                        Debug.Log($"  - Unit {cell.OccupyingUnit.UnitName} at ({cell.X}, {cell.Y}) will be affected");
                    }
                }
            }
            
            Debug.Log($"{skill.SkillName} will affect {affectedUnits.Count} unit(s) in the AOE area");

            // Track kills for XP
            int killCount = 0;

            // Apply effects to all affected units
            foreach (Unit affectedUnit in affectedUnits)
            {
                bool wasAlive = affectedUnit.IsAlive;
                ApplySkillEffectsToTarget(skill, user, affectedUnit, proficiencyTier);
                if (wasAlive && !affectedUnit.IsAlive)
                {
                    killCount++;
                }
            }

            // Track kills for proficiency (handled elsewhere)
            if (killCount > 0)
            {
                Debug.Log($"{user.UnitName} eliminated {killCount} unit(s) with {skill.SkillName}!");
            }

            // Also create ground hazards if the skill creates them
            if (skill.CreatesGroundHazard)
            {
                HazardManager hazardManager = ManagerRegistry.Get<HazardManager>();
                if (hazardManager != null)
                {
                    Grid.HazardData hazardData = skill.HazardData;
                    if (hazardData != null)
                    {
                        int directDamage = skill.HazardDamagePerTurn > 0 ? skill.HazardDamagePerTurn : hazardData.DirectDamagePerTurn;
                        int duration = skill.HazardDuration > 0 ? skill.HazardDuration : 0;

                        // Create hazard on all affected cells
                        foreach (GridCell cell in affectedCells)
                        {
                            hazardManager.CreateHazard(hazardData, cell.X, cell.Y, directDamage, duration);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply skill effects (damage, status effects, stun) to a single target.
        /// </summary>
        private void ApplySkillEffectsToTarget(Skill skill, Unit user, Unit target, WeaponProficiencyTier? proficiencyTier = null)
        {
            // Calculate and apply damage (includes stat scaling)
            int damage = skill.CalculateDamage(user);
            
            // Apply proficiency stat efficiency multiplier to damage if applicable
            if (proficiencyTier.HasValue && damage > 0)
            {
                float statMultiplier = ProficiencyEffects.GetStatEfficiencyMultiplier(proficiencyTier.Value);
                // Only apply multiplier if damage scales with stats (not pure base damage)
                // For now, we'll apply it to all damage since skills use stat scaling
                damage = Mathf.RoundToInt(damage * statMultiplier);
            }
            
            if (damage > 0)
            {
                int actualDamage = target.TakeDamage(damage);
                Debug.Log($"{skill.SkillName} deals {actualDamage} damage to {target.UnitName}!");
                
                // Track damage for statistics (skills don't have crits currently)
                if (BattleStatisticsTracker.Instance != null)
                {
                    BattleStatisticsTracker.Instance.RecordDamage(user, target, actualDamage, false);
                }
            }

            // Apply status effect using StatusEffectData
            if (skill.AppliesStatusEffect && skill.StatusEffectData != null)
            {
                int duration = skill.StatusEffectDurationOverride > 0 ? skill.StatusEffectDurationOverride : 3; // Default fallback
                target.ApplyStatusEffect(skill.StatusEffectData, duration);
            }

            // Check for stun chance (separate from regular status effects)
            if (skill.StunChance > 0f)
            {
                float stunRoll = Random.Range(0f, 100f);
                if (stunRoll <= skill.StunChance)
                {
                    // Stun triggered - find or use stun StatusEffectData
                    StatusEffectData stunData = skill.StunStatusEffectData;
                    
                    // If not set, try to find "Stun" in registry
                    if (stunData == null)
                    {
                        StatusEffectRegistry registry = Resources.Load<StatusEffectRegistry>("StatusEffectRegistry");
                        if (registry != null)
                        {
                            registry.BuildLookup();
                            stunData = registry.GetDataByName("Stun");
                        }
                    }
                    
                    if (stunData != null)
                    {
                        target.ApplyStatusEffect(stunData, skill.StunDuration);
                        Debug.Log($"{skill.SkillName} stunned {target.UnitName} for {skill.StunDuration} turn(s)! (Roll: {stunRoll:F1} vs Chance: {skill.StunChance:F1}%)");
                    }
                    else
                    {
                        Debug.LogWarning($"{skill.SkillName} has stun chance but no Stun StatusEffectData found! Create one StatusEffectData asset with EffectName='Stun' and preventsActions=true. You only need to create it once - all skills can use it.");
                    }
                }
            }
        }
    }
}