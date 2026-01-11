using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Core;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Service for executing attack actions.
    /// Registered with ManagerRegistry for dependency injection.
    /// </summary>
    public class AttackAction : MonoBehaviour
    {
        public static AttackAction Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    // Auto-create if not in scene
                    GameObject go = new GameObject("AttackAction");
                    _instance = go.AddComponent<AttackAction>();
                    Debug.Log("AttackAction: Auto-created instance (not found in scene)");
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        private static AttackAction _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                ManagerRegistry.Register(this);
                Debug.Log("AttackAction: Registered with ManagerRegistry");
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ManagerRegistry.Unregister(this);
                _instance = null;
            }
        }

        /// <summary>
        /// Execute a melee attack from attacker to target.
        /// Returns true if attack was successful.
        /// </summary>
        public bool ExecuteMeleeAttack(Unit attacker, Unit target)
        {
            // Validate units exist and are alive
            if (attacker == null || target == null)
            {
                Debug.LogWarning("AttackAction: Null unit in attack!");
                return false;
            }

            if (!attacker.IsAlive)
            {
                Debug.LogWarning($"AttackAction: {attacker.UnitName} is dead and cannot attack!");
                return false;
            }

            if (!target.IsAlive)
            {
                Debug.LogWarning($"AttackAction: Cannot attack dead target {target.UnitName}!");
                return false;
            }

            // Check if target is adjacent (melee requirement)
            if (!AreUnitsAdjacent(attacker, target))
            {
                Debug.LogWarning($"AttackAction: {target.UnitName} is not adjacent to {attacker.UnitName}!");
                return false;
            }

            // Get weapon family and proficiency for this attack
            WeaponFamily weaponFamily = WeaponFamily.None;
            WeaponProficiencyTier? proficiencyTier = null;
            
            EquipmentItem meleeWeapon = attacker.UnitEquipment?.GetEquippedItem(EquipmentSlot.MeleeWeapon);
            if (meleeWeapon != null)
            {
                weaponFamily = WeaponFamilyHelper.GetWeaponFamily(meleeWeapon);
                if (weaponFamily != WeaponFamily.None && attacker.WeaponProficiencyManager != null)
                {
                    var proficiency = attacker.WeaponProficiencyManager.GetProficiency(weaponFamily);
                    if (proficiency != null)
                    {
                        proficiencyTier = proficiency.CurrentTier;
                    }
                }
            }

            // Calculate combat result (hit/miss, parry, crit) with proficiency effects
            CombatCalculator.CombatResult result = CombatCalculator.CalculateAttack(attacker, target, attacker.AttackPower, proficiencyTier);
            
            // Handle miss
            if (!result.Hit)
            {
                Debug.Log($"{attacker.UnitName}'s attack missed!");
                
                // Raise event for UI to display "Missed!" on HP indicator
                Riftbourne.Core.GameEvents.RaiseAttackMissed(target);
                
                // Still record action and award XP for attempting attack
                attacker.RecordAction();
                int baseXP = GameConstants.Instance != null ? GameConstants.Instance.BaseActionXP : 5;
                attacker.AwardXP(baseXP);
                attacker.MarkAsActed();
                return true; // Attack was attempted, even if it missed
            }
            
            // Handle parry
            if (result.Parried)
            {
                Debug.Log($"{target.UnitName} parried {attacker.UnitName}'s attack!");
                // Still record action for SP system
                attacker.RecordAction();
                attacker.MarkAsActed();
                return true; // Attack was attempted, even if parried
            }
            
            // Apply damage (already calculated with defense by CombatCalculator)
            int damageDealt = target.TakeDamageDirect(result.FinalDamage);
            
            string damageMessage = $"Attack dealt {damageDealt} damage";
            if (result.CriticalHit && !result.CriticalDefense)
            {
                damageMessage += " (CRITICAL HIT!)";
            }
            else if (result.CriticalHit && result.CriticalDefense)
            {
                damageMessage += " (critical defended)";
            }
            Debug.Log($"{attacker.UnitName} attacks {target.UnitName}! {damageMessage}!");
            
            // Record action for SP system
            attacker.RecordAction();

            // Record combat outcome for proficiency advancement
            if (weaponFamily != WeaponFamily.None && attacker.WeaponProficiencyManager != null)
            {
                bool wasKill = !target.IsAlive;
                bool wasCrit = result.CriticalHit && !result.CriticalDefense;
                attacker.WeaponProficiencyManager.RecordCombatAction(weaponFamily, result.Hit, wasKill, wasCrit, target);
            }

            // Mark attacker as acted (caller can still decide when to end turn)
            attacker.MarkAsActed();

            return true;
        }

        /// <summary>
        /// Execute a ranged attack from attacker to target.
        /// Returns true if attack was successful.
        /// </summary>
        public bool ExecuteRangedAttack(Unit attacker, Unit target)
        {
            // Validate units exist and are alive
            if (attacker == null || target == null)
            {
                Debug.LogWarning("AttackAction: Null unit in ranged attack!");
                return false;
            }

            if (!attacker.IsAlive)
            {
                Debug.LogWarning($"AttackAction: {attacker.UnitName} is dead and cannot attack!");
                return false;
            }

            if (!target.IsAlive)
            {
                Debug.LogWarning($"AttackAction: Cannot attack dead target {target.UnitName}!");
                return false;
            }

            // Check if attacker has a ranged weapon
            if (attacker.AttackRange <= 1)
            {
                Debug.LogWarning($"AttackAction: {attacker.UnitName} does not have a ranged weapon equipped!");
                return false;
            }

            // Check if target is in range (Chebyshev distance)
            int dx = Mathf.Abs(attacker.GridX - target.GridX);
            int dy = Mathf.Abs(attacker.GridY - target.GridY);
            int distance = Mathf.Max(dx, dy); // Chebyshev distance

            if (distance > attacker.AttackRange)
            {
                Debug.LogWarning($"AttackAction: {target.UnitName} is out of range! Distance: {distance}, Range: {attacker.AttackRange}");
                return false;
            }

            // Get weapon family and proficiency for this attack
            WeaponFamily weaponFamily = WeaponFamily.None;
            WeaponProficiencyTier? proficiencyTier = null;
            
            EquipmentItem rangedWeapon = attacker.UnitEquipment?.GetEquippedItem(EquipmentSlot.RangedWeapon);
            if (rangedWeapon != null)
            {
                weaponFamily = WeaponFamilyHelper.GetWeaponFamily(rangedWeapon);
                if (weaponFamily != WeaponFamily.None && attacker.WeaponProficiencyManager != null)
                {
                    var proficiency = attacker.WeaponProficiencyManager.GetProficiency(weaponFamily);
                    if (proficiency != null)
                    {
                        proficiencyTier = proficiency.CurrentTier;
                    }
                }
            }

            // Calculate combat result (hit/miss, parry, crit) with proficiency effects
            CombatCalculator.CombatResult result = CombatCalculator.CalculateAttack(attacker, target, attacker.AttackPower, proficiencyTier);
            
            // Handle miss
            if (!result.Hit)
            {
                Debug.Log($"{attacker.UnitName}'s ranged attack missed!");
                
                // Raise event for UI to display "Missed!" on HP indicator
                Riftbourne.Core.GameEvents.RaiseAttackMissed(target);
                
                // Still record action for SP system
                attacker.RecordAction();
                attacker.MarkAsActed();
                return true; // Attack was attempted, even if it missed
            }
            
            // Handle parry
            if (result.Parried)
            {
                Debug.Log($"{target.UnitName} parried {attacker.UnitName}'s ranged attack!");
                // Still record action for SP system
                attacker.RecordAction();
                attacker.MarkAsActed();
                return true; // Attack was attempted, even if parried
            }
            
            // Apply damage (already calculated with defense by CombatCalculator)
            int damageDealt = target.TakeDamageDirect(result.FinalDamage);
            
            string damageMessage = $"Ranged attack dealt {damageDealt} damage";
            if (result.CriticalHit && !result.CriticalDefense)
            {
                damageMessage += " (CRITICAL HIT!)";
            }
            else if (result.CriticalHit && result.CriticalDefense)
            {
                damageMessage += " (critical defended)";
            }
            Debug.Log($"{attacker.UnitName} performs ranged attack on {target.UnitName}! {damageMessage}!");
            
            // Record action for SP system
            attacker.RecordAction();

            // Record combat outcome for proficiency advancement
            if (weaponFamily != WeaponFamily.None && attacker.WeaponProficiencyManager != null)
            {
                bool wasKill = !target.IsAlive;
                bool wasCrit = result.CriticalHit && !result.CriticalDefense;
                attacker.WeaponProficiencyManager.RecordCombatAction(weaponFamily, result.Hit, wasKill, wasCrit, target);
            }

            // Mark attacker as acted (caller can still decide when to end turn)
            attacker.MarkAsActed();

            return true;
        }

        /// <summary>
        /// Check if two units are adjacent on the grid (Chebyshev distance = 1).
        /// Allows attacks in all 8 directions (cardinal + diagonal).
        /// </summary>
        private bool AreUnitsAdjacent(Unit unit1, Unit unit2)
        {
            int dx = Mathf.Abs(unit1.GridX - unit2.GridX);
            int dy = Mathf.Abs(unit1.GridY - unit2.GridY);
            // Chebyshev distance: max of dx and dy (allows diagonal adjacency)
            int distance = Mathf.Max(dx, dy);
            Debug.Log($"Adjacency check: {unit1.UnitName} at ({unit1.GridX},{unit1.GridY}) vs {unit2.UnitName} at ({unit2.GridX},{unit2.GridY}) = Chebyshev distance {distance}");
            return distance == 1;
        }
    }
}