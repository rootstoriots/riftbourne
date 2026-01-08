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

            // Execute attack
            Debug.Log($"{attacker.UnitName} attacks {target.UnitName}!");
            int damageDealt = target.TakeDamage(attacker.AttackPower);
            Debug.Log($"Attack dealt {damageDealt} damage!");
            
            // Record action and award XP
            attacker.RecordAction();  // Award SP based on action count
            int baseXP = GameConstants.Instance != null ? GameConstants.Instance.BaseActionXP : 5;
            attacker.AwardXP(baseXP);
            
            // Award bonus XP if target died
            if (!target.IsAlive)
            {
                int killBonus = GameConstants.Instance != null ? GameConstants.Instance.KillBonusXP : 25;
                attacker.AwardXP(killBonus);
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