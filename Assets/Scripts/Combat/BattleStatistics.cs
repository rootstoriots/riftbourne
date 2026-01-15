using System;
using System.Collections.Generic;
using Riftbourne.Characters;
using Riftbourne.Skills;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Stores battle statistics for a single character.
    /// </summary>
    [Serializable]
    public class CharacterBattleStats
    {
        public string CharacterID;
        public string CharacterName;
        
        // SP and Skills
        public int SPEarned;
        public List<string> SkillsMastered = new List<string>();
        
        // Combat Stats
        public int Kills;
        public int CriticalHits;
        public int DamageDealt;
        public int SkillsUsed;
        
        public CharacterBattleStats(string characterID, string characterName)
        {
            CharacterID = characterID;
            CharacterName = characterName;
            SPEarned = 0;
            Kills = 0;
            CriticalHits = 0;
            DamageDealt = 0;
            SkillsUsed = 0;
            SkillsMastered = new List<string>();
        }
    }
    
    /// <summary>
    /// Comprehensive battle statistics tracking per-character and party-wide.
    /// Serializable for potential save/load functionality.
    /// </summary>
    [Serializable]
    public class BattleStatistics
    {
        // Per-character statistics (current battle only)
        private Dictionary<string, CharacterBattleStats> characterStats = new Dictionary<string, CharacterBattleStats>();
        
        // Party-wide totals (current battle only)
        public int TotalSPEarned { get; private set; }
        public int TotalKills { get; private set; }
        public int TotalCriticalHits { get; private set; }
        public int TotalDamageDealt { get; private set; }
        public int TotalSkillsUsed { get; private set; }
        public int TotalSkillsMastered { get; private set; }
        
        // Lifetime totals (accumulated across all battles)
        private Dictionary<string, CharacterBattleStats> lifetimeCharacterStats = new Dictionary<string, CharacterBattleStats>();
        public int LifetimeTotalSPEarned { get; private set; }
        public int LifetimeTotalKills { get; private set; }
        public int LifetimeTotalCriticalHits { get; private set; }
        public int LifetimeTotalDamageDealt { get; private set; }
        public int LifetimeTotalSkillsUsed { get; private set; }
        public int LifetimeTotalSkillsMastered { get; private set; }
        
        /// <summary>
        /// Get all character statistics.
        /// </summary>
        public Dictionary<string, CharacterBattleStats> GetCharacterStats()
        {
            return new Dictionary<string, CharacterBattleStats>(characterStats);
        }
        
        /// <summary>
        /// Get statistics for a specific character.
        /// </summary>
        public CharacterBattleStats GetCharacterStats(string characterID)
        {
            if (characterStats.ContainsKey(characterID))
            {
                return characterStats[characterID];
            }
            return null;
        }
        
        /// <summary>
        /// Register a character for tracking.
        /// </summary>
        public void RegisterCharacter(Unit unit)
        {
            if (unit == null) return;
            
            string id = unit.UnitName;
            if (!characterStats.ContainsKey(id))
            {
                characterStats[id] = new CharacterBattleStats(id, unit.UnitName);
            }
        }
        
        /// <summary>
        /// Record SP earned for a character.
        /// </summary>
        public void RecordSPEarned(Unit unit, int amount)
        {
            if (unit == null || amount <= 0) return;
            
            string id = unit.UnitName;
            RegisterCharacter(unit);
            
            characterStats[id].SPEarned += amount;
            TotalSPEarned += amount;
        }
        
        /// <summary>
        /// Record a skill mastered during battle.
        /// </summary>
        public void RecordSkillMastered(Unit unit, Skill skill)
        {
            if (unit == null || skill == null) return;
            
            string id = unit.UnitName;
            RegisterCharacter(unit);
            
            string skillName = skill.SkillName;
            if (!characterStats[id].SkillsMastered.Contains(skillName))
            {
                characterStats[id].SkillsMastered.Add(skillName);
                TotalSkillsMastered++;
            }
        }
        
        /// <summary>
        /// Record a kill.
        /// </summary>
        public void RecordKill(Unit killer, Unit victim)
        {
            if (killer == null || victim == null) return;
            
            string id = killer.UnitName;
            RegisterCharacter(killer);
            
            characterStats[id].Kills++;
            TotalKills++;
        }
        
        /// <summary>
        /// Record a critical hit.
        /// </summary>
        public void RecordCriticalHit(Unit attacker)
        {
            if (attacker == null) return;
            
            string id = attacker.UnitName;
            RegisterCharacter(attacker);
            
            characterStats[id].CriticalHits++;
            TotalCriticalHits++;
        }
        
        /// <summary>
        /// Record damage dealt.
        /// </summary>
        public void RecordDamageDealt(Unit attacker, int damage)
        {
            if (attacker == null || damage <= 0) return;
            
            string id = attacker.UnitName;
            RegisterCharacter(attacker);
            
            characterStats[id].DamageDealt += damage;
            TotalDamageDealt += damage;
        }
        
        /// <summary>
        /// Record a skill usage.
        /// </summary>
        public void RecordSkillUsed(Unit user)
        {
            if (user == null) return;
            
            string id = user.UnitName;
            RegisterCharacter(user);
            
            characterStats[id].SkillsUsed++;
            TotalSkillsUsed++;
        }
        
        /// <summary>
        /// Clear current battle statistics (but keep lifetime totals).
        /// </summary>
        public void Clear()
        {
            characterStats.Clear();
            TotalSPEarned = 0;
            TotalKills = 0;
            TotalCriticalHits = 0;
            TotalDamageDealt = 0;
            TotalSkillsUsed = 0;
            TotalSkillsMastered = 0;
        }
        
        /// <summary>
        /// Merge current battle statistics into lifetime totals.
        /// Call this at the end of each battle.
        /// </summary>
        public void MergeToLifetimeTotals()
        {
            // Merge per-character stats
            foreach (var kvp in characterStats)
            {
                string id = kvp.Key;
                CharacterBattleStats current = kvp.Value;
                
                if (!lifetimeCharacterStats.ContainsKey(id))
                {
                    lifetimeCharacterStats[id] = new CharacterBattleStats(id, current.CharacterName);
                }
                
                CharacterBattleStats lifetime = lifetimeCharacterStats[id];
                lifetime.SPEarned += current.SPEarned;
                lifetime.Kills += current.Kills;
                lifetime.CriticalHits += current.CriticalHits;
                lifetime.DamageDealt += current.DamageDealt;
                lifetime.SkillsUsed += current.SkillsUsed;
                
                // Merge skills mastered (avoid duplicates)
                foreach (string skillName in current.SkillsMastered)
                {
                    if (!lifetime.SkillsMastered.Contains(skillName))
                    {
                        lifetime.SkillsMastered.Add(skillName);
                    }
                }
            }
            
            // Merge party-wide totals
            LifetimeTotalSPEarned += TotalSPEarned;
            LifetimeTotalKills += TotalKills;
            LifetimeTotalCriticalHits += TotalCriticalHits;
            LifetimeTotalDamageDealt += TotalDamageDealt;
            LifetimeTotalSkillsUsed += TotalSkillsUsed;
            LifetimeTotalSkillsMastered += TotalSkillsMastered;
        }
        
        /// <summary>
        /// Get lifetime statistics for all characters.
        /// </summary>
        public Dictionary<string, CharacterBattleStats> GetLifetimeCharacterStats()
        {
            return new Dictionary<string, CharacterBattleStats>(lifetimeCharacterStats);
        }
        
        /// <summary>
        /// Get lifetime statistics for a specific character.
        /// </summary>
        public CharacterBattleStats GetLifetimeCharacterStats(string characterID)
        {
            if (lifetimeCharacterStats.ContainsKey(characterID))
            {
                return lifetimeCharacterStats[characterID];
            }
            return null;
        }
    }
}
