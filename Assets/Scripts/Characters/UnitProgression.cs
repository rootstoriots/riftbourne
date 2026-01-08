using UnityEngine;
using Riftbourne.Skills;
using Riftbourne.Core;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles XP, leveling, SP, and skill mastery for a unit.
    /// Component class to reduce Unit.cs complexity.
    /// </summary>
    public class UnitProgression
    {
        private Unit unit;
        private int level;
        private int currentXP;
        private int skillPoints;
        private int totalActions;
        private int lastSPAwardedAtAction;
        private HashSet<Skill> masteredSkills;
        private HashSet<PassiveSkill> masteredPassiveSkills;

        public int Level => level;
        public int CurrentXP => currentXP;
        public int SkillPoints => skillPoints;
        public int TotalActions => totalActions;
        public HashSet<Skill> MasteredSkills => masteredSkills;
        public HashSet<PassiveSkill> MasteredPassiveSkills => masteredPassiveSkills;

        public UnitProgression(Unit unit, int level, int currentXP, int skillPoints, HashSet<Skill> masteredSkills, HashSet<PassiveSkill> masteredPassiveSkills)
        {
            this.unit = unit;
            this.level = level;
            this.currentXP = currentXP;
            this.skillPoints = skillPoints;
            this.masteredSkills = masteredSkills;
            this.masteredPassiveSkills = masteredPassiveSkills;
        }

        public int GetXPRequiredForLevel(int targetLevel)
        {
            return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, targetLevel - 2));
        }

        public int GetXPRequiredForNextLevel()
        {
            return GetXPRequiredForLevel(level + 1);
        }

        public void AwardXP(int amount, System.Action<int> onLevelUp)
        {
            if (amount <= 0) return;
            
            currentXP += amount;
            UnityEngine.Debug.Log($"{unit.UnitName} gained {amount} XP! ({currentXP}/{GetXPRequiredForNextLevel()})");
            
            while (currentXP >= GetXPRequiredForNextLevel())
            {
                LevelUp(onLevelUp);
            }
        }

        private void LevelUp(System.Action<int> onLevelUp)
        {
            int xpNeeded = GetXPRequiredForNextLevel();
            currentXP -= xpNeeded;
            level++;
            
            int statsToAllocate = GameConstants.Instance != null ? GameConstants.Instance.StatsPerLevel : 2;
            UnityEngine.Debug.Log($"🎉 {unit.UnitName} reached Level {level}! +{statsToAllocate} random stats");
            
            onLevelUp?.Invoke(level);
            GameEvents.RaiseUnitLeveledUp(unit, level);
        }

        public void RecordAction()
        {
            totalActions++;
            
            int actionsPerSP = GameConstants.Instance != null ? GameConstants.Instance.ActionsPerSP : 5;
            int spToAward = (totalActions - lastSPAwardedAtAction) / actionsPerSP;
            
            if (spToAward > 0)
            {
                skillPoints += spToAward;
                lastSPAwardedAtAction += spToAward * actionsPerSP;
                UnityEngine.Debug.Log($"⭐ {unit.UnitName} earned {spToAward} SP! ({totalActions} total actions, {skillPoints} SP total)");
            }
        }

        public bool MasterSkill(Skill skill)
        {
            if (skill == null) return false;
            
            if (masteredSkills.Contains(skill))
            {
                UnityEngine.Debug.Log($"{unit.UnitName} already mastered {skill.SkillName}!");
                return false;
            }
            
            if (skillPoints < skill.MasteryCost)
            {
                UnityEngine.Debug.Log($"{unit.UnitName} needs {skill.MasteryCost} SP to master {skill.SkillName}, but only has {skillPoints} SP");
                return false;
            }
            
            skillPoints -= skill.MasteryCost;
            masteredSkills.Add(skill);
            UnityEngine.Debug.Log($"✨ {unit.UnitName} mastered {skill.SkillName} for {skill.MasteryCost} SP! ({skillPoints} SP remaining)");
            GameEvents.RaiseSkillMastered(unit, skill);
            return true;
        }

        public bool IsSkillMastered(Skill skill)
        {
            return masteredSkills.Contains(skill);
        }

        public bool MasterPassiveSkill(PassiveSkill passiveSkill, System.Func<int> getMaxHP, System.Action<int> setHP)
        {
            if (passiveSkill == null) return false;
            
            if (masteredPassiveSkills.Contains(passiveSkill))
            {
                UnityEngine.Debug.Log($"{unit.UnitName} already mastered {passiveSkill.SkillName}!");
                return false;
            }
            
            if (skillPoints < passiveSkill.SPCost)
            {
                UnityEngine.Debug.Log($"{unit.UnitName} needs {passiveSkill.SPCost} SP to master {passiveSkill.SkillName}, but only has {skillPoints} SP");
                return false;
            }
            
            skillPoints -= passiveSkill.SPCost;
            masteredPassiveSkills.Add(passiveSkill);
            UnityEngine.Debug.Log($"✨ {unit.UnitName} mastered passive skill {passiveSkill.SkillName} for {passiveSkill.SPCost} SP! ({skillPoints} SP remaining)");
            
            int newMaxHP = getMaxHP();
            int currentHP = unit.CurrentHP;
            if (newMaxHP > currentHP)
            {
                setHP(newMaxHP);
            }
            
            return true;
        }

        public bool IsPassiveSkillMastered(PassiveSkill passiveSkill)
        {
            return masteredPassiveSkills.Contains(passiveSkill);
        }
    }
}

