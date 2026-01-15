using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Skills;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Tracks comprehensive battle statistics by subscribing to GameEvents.
    /// Singleton pattern for easy access throughout the battle.
    /// </summary>
    public class BattleStatisticsTracker : MonoBehaviour
    {
        private static BattleStatisticsTracker instance;
        public static BattleStatisticsTracker Instance => instance;
        
        private BattleStatistics statistics;
        
        // Track SP earned per character (need to track before/after RecordAction calls)
        private Dictionary<string, int> spBeforeBattle = new Dictionary<string, int>();
        private Dictionary<string, int> lastRecordedSP = new Dictionary<string, int>();
        
        // Track last attacker for kill attribution
        private Dictionary<Unit, Unit> lastAttackerMap = new Dictionary<Unit, Unit>();
        
        public BattleStatistics Statistics => statistics;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                statistics = new BattleStatistics();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to all relevant GameEvents
            GameEvents.OnUnitDamaged += HandleUnitDamaged;
            GameEvents.OnUnitDied += HandleUnitDied;
            GameEvents.OnSkillMastered += HandleSkillMastered;
            GameEvents.OnUnitTurnEnded += HandleUnitTurnEnded;
            GameEvents.OnCriticalHit += HandleCriticalHit;
            GameEvents.OnSkillUsed += HandleSkillUsed;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            GameEvents.OnUnitDamaged -= HandleUnitDamaged;
            GameEvents.OnUnitDied -= HandleUnitDied;
            GameEvents.OnSkillMastered -= HandleSkillMastered;
            GameEvents.OnUnitTurnEnded -= HandleUnitTurnEnded;
            GameEvents.OnCriticalHit -= HandleCriticalHit;
            GameEvents.OnSkillUsed -= HandleSkillUsed;
        }
        
        /// <summary>
        /// Initialize tracking for a battle. Call this when battle starts.
        /// </summary>
        public void InitializeBattle(List<Unit> partyMembers)
        {
            statistics.Clear();
            spBeforeBattle.Clear();
            lastRecordedSP.Clear();
            lastAttackerMap.Clear();
            
            // Register all party members and record initial SP
            if (partyMembers != null)
            {
                foreach (Unit unit in partyMembers)
                {
                    if (unit != null)
                    {
                        statistics.RegisterCharacter(unit);
                        int currentSP = unit.SkillPoints;
                        string id = unit.UnitName;
                        spBeforeBattle[id] = currentSP;
                        lastRecordedSP[id] = currentSP;
                    }
                }
            }
        }
        
        /// <summary>
        /// Record SP earned by checking difference from initial SP.
        /// Call this periodically or when SP changes are detected.
        /// </summary>
        public void UpdateSPEarned(Unit unit)
        {
            if (unit == null) return;
            
            string id = unit.UnitName;
            if (!spBeforeBattle.ContainsKey(id)) return;
            
            int currentSP = unit.SkillPoints;
            int lastSP = lastRecordedSP.ContainsKey(id) ? lastRecordedSP[id] : spBeforeBattle[id];
            
            if (currentSP > lastSP)
            {
                int earned = currentSP - lastSP;
                statistics.RecordSPEarned(unit, earned);
                lastRecordedSP[id] = currentSP;
            }
        }
        
        /// <summary>
        /// Record that a unit used a skill.
        /// </summary>
        public void RecordSkillUsed(Unit unit)
        {
            if (unit == null) return;
            statistics.RecordSkillUsed(unit);
        }
        
        /// <summary>
        /// Record that a unit dealt damage (and track attacker for kill attribution).
        /// </summary>
        public void RecordDamage(Unit attacker, Unit target, int damage, bool wasCritical)
        {
            if (attacker == null || target == null) return;
            
            // Only track player unit actions
            if (attacker.Faction == Faction.Player || (attacker.FactionData != null && attacker.FactionData.IsPlayerFaction))
            {
                statistics.RecordDamageDealt(attacker, damage);
                
                if (wasCritical)
                {
                    statistics.RecordCriticalHit(attacker);
                }
                
                // Track last attacker for kill attribution
                lastAttackerMap[target] = attacker;
            }
        }
        
        /// <summary>
        /// Handle unit damaged event.
        /// </summary>
        private void HandleUnitDamaged(Unit unit, int damageAmount)
        {
            // Note: GameEvents.OnUnitDamaged doesn't provide attacker info
            // We'll need to track this separately via CombatCalculator or SkillExecutor
            // For now, this is a placeholder - actual tracking happens in RecordDamage()
        }
        
        /// <summary>
        /// Handle unit died event.
        /// </summary>
        private void HandleUnitDied(Unit unit)
        {
            if (unit == null) return;
            
            // Check if this was killed by a player unit
            if (lastAttackerMap.ContainsKey(unit))
            {
                Unit killer = lastAttackerMap[unit];
                if (killer != null && (killer.Faction == Faction.Player || (killer.FactionData != null && killer.FactionData.IsPlayerFaction)))
                {
                    statistics.RecordKill(killer, unit);
                }
            }
            
            // Remove from tracking
            lastAttackerMap.Remove(unit);
        }
        
        /// <summary>
        /// Handle skill mastered event.
        /// </summary>
        private void HandleSkillMastered(Unit unit, Skill skill)
        {
            if (unit == null || skill == null) return;
            statistics.RecordSkillMastered(unit, skill);
        }
        
        /// <summary>
        /// Handle unit turn ended - update SP earned.
        /// </summary>
        private void HandleUnitTurnEnded(Unit unit)
        {
            if (unit == null) return;
            UpdateSPEarned(unit);
        }
        
        /// <summary>
        /// Get final SP earned for all characters and merge to lifetime totals (call at battle end).
        /// </summary>
        public void FinalizeSPTracking(List<Unit> partyMembers)
        {
            if (partyMembers != null)
            {
                foreach (Unit unit in partyMembers)
                {
                    if (unit != null)
                    {
                        UpdateSPEarned(unit);
                    }
                }
            }
            
            // Merge current battle stats to lifetime totals
            if (statistics != null)
            {
                statistics.MergeToLifetimeTotals();
            }
        }
        
        /// <summary>
        /// Get lifetime statistics (totals across all battles).
        /// </summary>
        public BattleStatistics GetLifetimeStatistics()
        {
            // Return a copy of lifetime stats
            // Note: This returns the same BattleStatistics object but with lifetime data
            // The lifetime totals are stored in the same object
            return statistics;
        }
        
        /// <summary>
        /// Handle critical hit event.
        /// </summary>
        private void HandleCriticalHit(Unit attacker, Unit target, int damageAmount)
        {
            if (attacker == null) return;
            RecordDamage(attacker, target, damageAmount, true);
        }
        
        /// <summary>
        /// Handle skill used event.
        /// </summary>
        private void HandleSkillUsed(Unit user, Skill skill, Unit target)
        {
            if (user == null) return;
            RecordSkillUsed(user);
        }
    }
}
