using UnityEngine;
using Riftbourne.Skills;
using Riftbourne.Combat;
using Riftbourne.Core;
using Riftbourne.Items;
using Riftbourne.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Runtime character state that changes during gameplay.
    /// Serializable for save system.
    /// Managed by PartyManager.
    /// </summary>
    [System.Serializable]
    public class CharacterState
    {
        // Reference to static definition
        [SerializeField] private string characterID;
        [NonSerialized] private CharacterDefinition definition;

        // Current stats (calculated from base + equipment + level + passives)
        [SerializeField] private int currentStrength;
        [SerializeField] private int currentFinesse;
        [SerializeField] private int currentFocus;
        [SerializeField] private int currentSpeed;
        [SerializeField] private int currentLuck;

        // Current narrative skills (may change)
        [SerializeField] private int currentPerception;
        [SerializeField] private int currentInterpretive;
        [SerializeField] private int currentEmpathic;

        // Progression
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int skillPoints = 0;
        [SerializeField] private int totalActions = 0;
        [SerializeField] private int lastSPAwardedAtAction = 0;

        // Combat state
        [SerializeField] private int currentHP;
        [SerializeField] private int maxHP;
        [SerializeField] private List<string> activeStatusEffectIDs = new List<string>(); // Store StatusEffectData names for serialization
        [NonSerialized] private List<StatusEffectData> activeStatusEffects = new List<StatusEffectData>(); // Runtime only

        // Equipment (current)
        [SerializeField] private SerializableDictionary<EquipmentSlot, string> equippedItemIDs = new SerializableDictionary<EquipmentSlot, string>(); // Store item names for serialization
        [NonSerialized] private Dictionary<EquipmentSlot, EquipmentItem> equippedItems = new Dictionary<EquipmentSlot, EquipmentItem>(); // Runtime only

        // Skills
        [SerializeField] private List<string> knownSkillNames = new List<string>(); // Store skill names for serialization
        [SerializeField] private List<string> masteredSkillNames = new List<string>(); // Store skill names for serialization
        [SerializeField] private List<string> masteredPassiveSkillNames = new List<string>(); // Store skill names for serialization
        [NonSerialized] private List<Skill> knownSkills = new List<Skill>(); // Runtime only
        [NonSerialized] private HashSet<Skill> masteredSkills = new HashSet<Skill>(); // Runtime only
        [NonSerialized] private HashSet<PassiveSkill> masteredPassiveSkills = new HashSet<PassiveSkill>(); // Runtime only

        // Weapon Proficiencies
        [SerializeField] private SerializableDictionary<WeaponFamily, WeaponProficiency> weaponProficiencies = new SerializableDictionary<WeaponFamily, WeaponProficiency>();

        // Inventory System
        [SerializeField] private List<InventorySlot> inventory = new List<InventorySlot>();
        [SerializeField] private List<InventorySlot> containerInventory = new List<InventorySlot>();
        [SerializeField] private ContainerItem[] containerSlots = new ContainerItem[2];
        [SerializeField] private int aurumShards = 0;

        // Cache for stat calculations
        private bool statsDirty = true;

        // Events
        public event Action OnStatsChanged;
        public event Action OnHPChanged;
        public event Action OnLevelUp;
        public event Action<EquipmentSlot, EquipmentItem> OnEquipmentChanged;

        // Properties
        public string CharacterID => characterID;
        public CharacterDefinition Definition
        {
            get
            {
                if (definition == null && !string.IsNullOrEmpty(characterID))
                {
                    // Try to load from Resources
                    definition = Resources.Load<CharacterDefinition>($"Characters/{characterID}");
                }
                return definition;
            }
            set => definition = value;
        }

        // Current stats
        public int CurrentStrength { get { EnsureStatsCalculated(); return currentStrength; } }
        public int CurrentFinesse { get { EnsureStatsCalculated(); return currentFinesse; } }
        public int CurrentFocus { get { EnsureStatsCalculated(); return currentFocus; } }
        public int CurrentSpeed { get { EnsureStatsCalculated(); return currentSpeed; } }
        public int CurrentLuck { get { EnsureStatsCalculated(); return currentLuck; } }

        // Narrative skills
        public int CurrentPerception => currentPerception;
        public int CurrentInterpretive => currentInterpretive;
        public int CurrentEmpathic => currentEmpathic;

        // Progression
        public int Level => level;
        public int CurrentXP => currentXP;
        public int SkillPoints => skillPoints;
        public int TotalActions => totalActions;

        // Combat state
        public int CurrentHP => currentHP;
        public int MaxHP { get { EnsureStatsCalculated(); return maxHP; } }
        public List<StatusEffectData> ActiveStatusEffects => new List<StatusEffectData>(activeStatusEffects);

        // Equipment
        public Dictionary<EquipmentSlot, EquipmentItem> EquippedItems => new Dictionary<EquipmentSlot, EquipmentItem>(equippedItems);

        // Skills
        public List<Skill> KnownSkills => new List<Skill>(knownSkills);
        public HashSet<Skill> MasteredSkills => new HashSet<Skill>(masteredSkills);
        public HashSet<PassiveSkill> MasteredPassiveSkills => new HashSet<PassiveSkill>(masteredPassiveSkills);

        // Weapon Proficiencies
        public SerializableDictionary<WeaponFamily, WeaponProficiency> WeaponProficiencies => weaponProficiencies;

        // Inventory
        public List<InventorySlot> Inventory => inventory;
        public List<InventorySlot> ContainerInventory => containerInventory;
        public ContainerItem[] ContainerSlots => containerSlots;
        public int AurumShards => aurumShards;

        // Constructor
        public CharacterState(CharacterDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("CharacterState: Cannot create state from null definition!");
                return;
            }

            this.characterID = definition.CharacterID;
            this.definition = definition;

            // Initialize with base values
            currentStrength = definition.BaseStrength;
            currentFinesse = definition.BaseFinesse;
            currentFocus = definition.BaseFocus;
            currentSpeed = definition.BaseSpeed;
            currentLuck = definition.BaseLuck;

            currentPerception = definition.BasePerception;
            currentInterpretive = definition.BaseInterpretive;
            currentEmpathic = definition.BaseEmpathic;

            // Initialize HP (base max HP calculation - will be recalculated with equipment)
            maxHP = 100; // Default base HP
            currentHP = maxHP;

            // Initialize all weapon families with Untrained proficiency
            foreach (WeaponFamily family in System.Enum.GetValues(typeof(WeaponFamily)))
            {
                if (family != WeaponFamily.None && !weaponProficiencies.ContainsKey(family))
                {
                    weaponProficiencies[family] = new WeaponProficiency(family);
                }
            }

            // Initialize known skills from definition
            foreach (var skill in definition.AvailableSkills)
            {
                if (skill != null)
                {
                    knownSkills.Add(skill);
                    knownSkillNames.Add(skill.SkillName);
                }
            }

            // Equip starting equipment
            foreach (var equipment in definition.StartingEquipment)
            {
                if (equipment != null)
                {
                    EquipItem(equipment, equipment.PrimarySlot);
                }
            }

            // Initialize inventory from definition
            if (definition.StartingInventory != null)
            {
                foreach (var slot in definition.StartingInventory)
                {
                    if (slot != null && slot.Item != null && !slot.IsEmpty())
                    {
                        inventory.Add(new InventorySlot(slot.Item, slot.Quantity));
                    }
                }
            }

            // Initialize currency
            aurumShards = definition.StartingAurumShards;

            // Initialize container slots array
            containerSlots = new ContainerItem[2];

            CalculateStats();
        }

        /// <summary>
        /// Ensure stats are calculated before accessing them.
        /// </summary>
        private void EnsureStatsCalculated()
        {
            if (statsDirty)
            {
                CalculateStats();
            }
        }

        /// <summary>
        /// Recalculate stats from base + equipment + level + passives.
        /// </summary>
        public void CalculateStats()
        {
            if (definition == null)
            {
                Debug.LogWarning($"CharacterState: Cannot calculate stats - definition is null for {characterID}");
                return;
            }

            // Start with base stats
            int baseStrength = definition.BaseStrength;
            int baseFinesse = definition.BaseFinesse;
            int baseFocus = definition.BaseFocus;
            int baseSpeed = definition.BaseSpeed;
            int baseLuck = definition.BaseLuck;

            // Add equipment bonuses
            int equipmentStrength = 0;
            int equipmentFinesse = 0;
            int equipmentFocus = 0;
            int equipmentSpeed = 0;
            int equipmentLuck = 0;
            int equipmentMaxHPFlat = 0;
            float equipmentMaxHPPercent = 0f;

            foreach (var item in equippedItems.Values)
            {
                if (item != null)
                {
                    equipmentStrength += item.StrengthBonus;
                    equipmentFinesse += item.FinesseBonus;
                    equipmentFocus += item.FocusBonus;
                    equipmentSpeed += item.SpeedBonus;
                    equipmentLuck += item.LuckBonus;
                    equipmentMaxHPPercent += item.MaxHPBonusPercent;
                }
            }

            // Add passive skill bonuses
            int passiveStrength = 0;
            int passiveFinesse = 0;
            int passiveFocus = 0;
            int passiveSpeed = 0;
            int passiveLuck = 0;
            int passiveMaxHPFlat = 0;
            float passiveMaxHPPercent = 0f;

            foreach (var passive in masteredPassiveSkills)
            {
                if (passive != null)
                {
                    passiveStrength += passive.StrengthBonus;
                    passiveFinesse += passive.FinesseBonus;
                    passiveFocus += passive.FocusBonus;
                    passiveSpeed += passive.SpeedBonus;
                    passiveLuck += passive.LuckBonus;
                    passiveMaxHPFlat += passive.MaxHPBonusFlat;
                    passiveMaxHPPercent += passive.MaxHPBonusPercent;
                }
            }

            // Calculate final stats
            currentStrength = baseStrength + equipmentStrength + passiveStrength;
            currentFinesse = baseFinesse + equipmentFinesse + passiveFinesse;
            currentFocus = baseFocus + equipmentFocus + passiveFocus;
            currentSpeed = baseSpeed + equipmentSpeed + passiveSpeed;
            currentLuck = baseLuck + equipmentLuck + passiveLuck;

            // Calculate max HP
            int baseMaxHP = 100; // Default base HP
            float totalHP = (baseMaxHP + equipmentMaxHPFlat + passiveMaxHPFlat) * (1f + ((equipmentMaxHPPercent + passiveMaxHPPercent) / 100f));
            int newMaxHP = Mathf.RoundToInt(totalHP);

            // Adjust current HP if max changed
            if (newMaxHP != maxHP)
            {
                float hpRatio = maxHP > 0 ? (float)currentHP / maxHP : 1f;
                maxHP = newMaxHP;
                currentHP = Mathf.RoundToInt(maxHP * hpRatio);
                currentHP = Mathf.Clamp(currentHP, 0, maxHP);
            }
            else
            {
                maxHP = newMaxHP;
            }

            statsDirty = false;
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Handle level up, stat increases.
        /// </summary>
        public void ApplyLevelUp()
        {
            level++;
            Debug.Log($"Character {characterID} leveled up to {level}!");
            
            // Award random stat increases
            int statsToAllocate = GameConstants.Instance != null ? GameConstants.Instance.StatsPerLevel : 2;
            for (int i = 0; i < statsToAllocate; i++)
            {
                int randomStat = UnityEngine.Random.Range(0, 5);
                switch (randomStat)
                {
                    case 0: currentStrength++; break;
                    case 1: currentFinesse++; break;
                    case 2: currentFocus++; break;
                    case 3: currentSpeed++; break;
                    case 4: currentLuck++; break;
                }
            }

            statsDirty = true;
            CalculateStats();
            OnLevelUp?.Invoke();
        }

        /// <summary>
        /// Award XP and handle leveling up.
        /// </summary>
        public void AwardXP(int amount)
        {
            if (amount <= 0) return;

            currentXP += amount;
            Debug.Log($"Character {characterID} gained {amount} XP! ({currentXP}/{GetXPRequiredForNextLevel()})");

            while (currentXP >= GetXPRequiredForNextLevel())
            {
                int xpNeeded = GetXPRequiredForNextLevel();
                currentXP -= xpNeeded;
                ApplyLevelUp();
            }
        }

        /// <summary>
        /// Calculate XP required for a given level.
        /// </summary>
        public int GetXPRequiredForLevel(int targetLevel)
        {
            return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, targetLevel - 2));
        }

        /// <summary>
        /// Get XP required to reach next level.
        /// </summary>
        public int GetXPRequiredForNextLevel()
        {
            return GetXPRequiredForLevel(level + 1);
        }

        /// <summary>
        /// Update HP.
        /// </summary>
        public int TakeDamage(int amount)
        {
            int minDamage = GameConstants.Instance != null ? GameConstants.Instance.MinimumDamage : 1;
            int damage = Mathf.Max(minDamage, amount);
            
            currentHP -= damage;
            currentHP = Mathf.Max(0, currentHP);
            
            OnHPChanged?.Invoke();
            return damage;
        }

        /// <summary>
        /// Heal this character.
        /// </summary>
        public int Heal(int amount)
        {
            int hpBefore = currentHP;
            currentHP += amount;
            currentHP = Mathf.Min(currentHP, MaxHP);
            
            int actualHealing = currentHP - hpBefore;
            OnHPChanged?.Invoke();
            return actualHealing;
        }

        /// <summary>
        /// Equip item and recalculate stats.
        /// </summary>
        public bool EquipItem(EquipmentItem item, EquipmentSlot slot)
        {
            if (item == null) return false;

            if (!item.CanEquipInSlot(slot))
            {
                Debug.LogWarning($"CharacterState: {item.ItemName} cannot be equipped in {slot} slot!");
                return false;
            }

            // Unequip existing item in slot
            if (equippedItems.ContainsKey(slot))
            {
                UnequipItem(slot);
            }

            equippedItems[slot] = item;
            equippedItemIDs[slot] = item.ItemName; // For serialization

            Debug.Log($"CharacterState: Equipped {item.ItemName} in {slot} slot.");
            
            statsDirty = true;
            CalculateStats();
            OnEquipmentChanged?.Invoke(slot, item);
            return true;
        }

        /// <summary>
        /// Unequip item from slot.
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.ContainsKey(slot)) return false;

            EquipmentItem item = equippedItems[slot];
            equippedItems.Remove(slot);
            equippedItemIDs.Remove(slot);

            Debug.Log($"CharacterState: Unequipped {item.ItemName} from {slot} slot.");
            
            statsDirty = true;
            CalculateStats();
            OnEquipmentChanged?.Invoke(slot, null);
            return true;
        }

        /// <summary>
        /// Get equipped item in slot.
        /// </summary>
        public EquipmentItem GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
        }

        /// <summary>
        /// Add skill to known skills.
        /// </summary>
        public void LearnSkill(Skill skill)
        {
            if (skill == null) return;

            if (!knownSkills.Contains(skill))
            {
                knownSkills.Add(skill);
                knownSkillNames.Add(skill.SkillName);
                Debug.Log($"CharacterState: Learned {skill.SkillName}!");
            }
        }

        /// <summary>
        /// Mark skill as mastered (spend SP).
        /// </summary>
        public bool MasterSkill(Skill skill)
        {
            if (skill == null) return false;

            if (masteredSkills.Contains(skill))
            {
                Debug.Log($"CharacterState: Already mastered {skill.SkillName}!");
                return false;
            }

            if (skillPoints < skill.MasteryCost)
            {
                Debug.Log($"CharacterState: Needs {skill.MasteryCost} SP to master {skill.SkillName}, but only has {skillPoints} SP");
                return false;
            }

            skillPoints -= skill.MasteryCost;
            masteredSkills.Add(skill);
            masteredSkillNames.Add(skill.SkillName);
            Debug.Log($"CharacterState: Mastered {skill.SkillName} for {skill.MasteryCost} SP! ({skillPoints} SP remaining)");
            return true;
        }

        /// <summary>
        /// Check if skill is mastered.
        /// </summary>
        public bool IsSkillMastered(Skill skill)
        {
            return masteredSkills.Contains(skill);
        }

        /// <summary>
        /// Mark passive skill as mastered (spend SP).
        /// </summary>
        public bool MasterPassiveSkill(PassiveSkill passiveSkill)
        {
            if (passiveSkill == null) return false;

            if (masteredPassiveSkills.Contains(passiveSkill))
            {
                Debug.Log($"CharacterState: Already mastered {passiveSkill.SkillName}!");
                return false;
            }

            if (skillPoints < passiveSkill.SPCost)
            {
                Debug.Log($"CharacterState: Needs {passiveSkill.SPCost} SP to master {passiveSkill.SkillName}, but only has {skillPoints} SP");
                return false;
            }

            skillPoints -= passiveSkill.SPCost;
            masteredPassiveSkills.Add(passiveSkill);
            masteredPassiveSkillNames.Add(passiveSkill.SkillName);
            
            // Recalculate stats to apply passive bonuses
            statsDirty = true;
            CalculateStats();
            
            Debug.Log($"CharacterState: Mastered passive skill {passiveSkill.SkillName} for {passiveSkill.SPCost} SP! ({skillPoints} SP remaining)");
            return true;
        }

        /// <summary>
        /// Check if passive skill is mastered.
        /// </summary>
        public bool IsPassiveSkillMastered(PassiveSkill passiveSkill)
        {
            return masteredPassiveSkills.Contains(passiveSkill);
        }

        /// <summary>
        /// Record a successful action and award SP.
        /// </summary>
        public void RecordAction()
        {
            totalActions++;

            int actionsPerSP = GameConstants.Instance != null ? GameConstants.Instance.ActionsPerSP : 5;
            int spToAward = (totalActions - lastSPAwardedAtAction) / actionsPerSP;

            if (spToAward > 0)
            {
                skillPoints += spToAward;
                lastSPAwardedAtAction += spToAward * actionsPerSP;
                Debug.Log($"CharacterState: Earned {spToAward} SP! ({totalActions} total actions, {skillPoints} SP total)");
            }
        }

        /// <summary>
        /// Get narrative skill level for a category.
        /// </summary>
        public int GetNarrativeSkillLevel(NarrativeSkillCategory category)
        {
            switch (category)
            {
                case NarrativeSkillCategory.Perception: return currentPerception;
                case NarrativeSkillCategory.Interpretive: return currentInterpretive;
                case NarrativeSkillCategory.Empathic: return currentEmpathic;
                default: return 0;
            }
        }

        /// <summary>
        /// Apply status effect.
        /// </summary>
        public void ApplyStatusEffect(StatusEffectData effectData, int duration)
        {
            if (effectData == null) return;

            if (!activeStatusEffects.Contains(effectData))
            {
                activeStatusEffects.Add(effectData);
                activeStatusEffectIDs.Add(effectData.EffectName);
                Debug.Log($"CharacterState: Applied status effect {effectData.EffectName}!");
            }
        }

        /// <summary>
        /// Remove status effect.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectData effectData)
        {
            if (effectData == null) return;

            if (activeStatusEffects.Remove(effectData))
            {
                activeStatusEffectIDs.Remove(effectData.EffectName);
                Debug.Log($"CharacterState: Removed status effect {effectData.EffectName}!");
            }
        }

        /// <summary>
        /// Update state from battle Unit (for battle → exploration transition).
        /// </summary>
        public void UpdateFromUnit(Unit unit)
        {
            if (unit == null) return;

            // Update HP
            currentHP = unit.CurrentHP;
            maxHP = unit.MaxHP;

            // Update progression
            level = unit.Level;
            currentXP = unit.CurrentXP;
            skillPoints = unit.SkillPoints;
            totalActions = unit.TotalActions;

            // Update weapon proficiencies
            if (unit.WeaponProficiencyManager != null)
            {
                var unitProficiencies = unit.WeaponProficiencyManager.GetSerializableData();
                weaponProficiencies.Clear();
                foreach (var kvp in unitProficiencies)
                {
                    weaponProficiencies[kvp.Key] = kvp.Value;
                }
            }

            // Update equipment (sync from Unit's equipment)
            equippedItems.Clear();
            equippedItemIDs.Clear();
            foreach (var slot in System.Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>())
            {
                var item = unit.GetEquippedItem(slot);
                if (item != null)
                {
                    equippedItems[slot] = item;
                    equippedItemIDs[slot] = item.ItemName;
                }
            }

            // Update mastered skills
            masteredSkills.Clear();
            masteredSkillNames.Clear();
            foreach (var skill in unit.KnownSkills)
            {
                if (unit.IsSkillMastered(skill))
                {
                    masteredSkills.Add(skill);
                    masteredSkillNames.Add(skill.SkillName);
                }
            }

            // Sync inventory from Unit
            ClearInventory();
            if (unit.Inventory != null)
            {
                foreach (var slot in unit.Inventory)
                {
                    if (slot != null && slot.Item != null && !slot.IsEmpty())
                    {
                        AddItem(slot.Item, slot.Quantity);
                    }
                }
            }

            // Sync container inventory
            ClearContainerInventory();
            if (unit.ContainerInventory != null)
            {
                foreach (var slot in unit.ContainerInventory)
                {
                    if (slot != null && slot.Item != null && !slot.IsEmpty())
                    {
                        AddToContainerInventory(slot.Item, slot.Quantity);
                    }
                }
            }

            // Sync container slots
            if (unit.ContainerSlots != null)
            {
                SetContainerSlots(unit.ContainerSlots);
            }

            // Sync currency
            SetAurumShards(unit.AurumShards);

            // Recalculate stats
            statsDirty = true;
            CalculateStats();

            Debug.Log($"CharacterState: Updated from Unit {unit.UnitName} (including inventory)");
        }

        /// <summary>
        /// Get all available skills (known + mastered + from equipment).
        /// </summary>
        public List<Skill> GetAvailableSkills()
        {
            List<Skill> available = new List<Skill>();
            available.AddRange(knownSkills);
            available.AddRange(masteredSkills);

            foreach (var item in equippedItems.Values)
            {
                if (item != null && item.TeachesSkills && item.GrantedSkills != null)
                {
                    foreach (var skill in item.GrantedSkills)
                    {
                        if (skill != null && !available.Contains(skill))
                        {
                            available.Add(skill);
                        }
                    }
                }
            }

            return available;
        }

        #region Inventory Management

        /// <summary>
        /// Add an item to the inventory.
        /// Returns true if item was added successfully.
        /// </summary>
        public bool AddItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            int remaining = quantity;

            // Try to stack with existing slots first
            foreach (var slot in inventory)
            {
                if (slot != null && slot.CanStack(item))
                {
                    remaining = slot.AddToStack(remaining);
                    if (remaining <= 0)
                        return true;
                }
            }

            // Create new slots for remaining quantity
            while (remaining > 0)
            {
                int stackSize = Mathf.Min(remaining, item.MaxStackSize);
                inventory.Add(new InventorySlot(item, stackSize));
                remaining -= stackSize;
            }

            return true;
        }

        /// <summary>
        /// Remove an item from the inventory.
        /// Returns true if at least some quantity was removed.
        /// </summary>
        public bool RemoveItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            int remaining = quantity;

            // Remove from slots
            for (int i = inventory.Count - 1; i >= 0; i--)
            {
                var slot = inventory[i];
                if (slot != null && slot.Item == item)
                {
                    int removed = slot.RemoveFromStack(remaining);
                    remaining -= removed;

                    if (slot.IsEmpty())
                        inventory.RemoveAt(i);

                    if (remaining <= 0)
                        return true;
                }
            }

            return remaining < quantity; // Returns true if at least some was removed
        }

        /// <summary>
        /// Get the total count of a specific item in inventory.
        /// </summary>
        public int GetItemCount(Item item)
        {
            int count = 0;

            foreach (var slot in inventory)
            {
                if (slot != null && slot.Item == item)
                    count += slot.Quantity;
            }

            return count;
        }

        /// <summary>
        /// Check if character has a specific item in the required quantity.
        /// </summary>
        public bool HasItem(Item item, int quantity = 1)
        {
            return GetItemCount(item) >= quantity;
        }

        /// <summary>
        /// Gain Aurum Shards.
        /// </summary>
        public void GainAurumShards(int amount)
        {
            if (amount <= 0)
                return;

            aurumShards += amount;
            Debug.Log($"CharacterState: Gained {amount} Aurum Shards. Total: {aurumShards}");
        }

        /// <summary>
        /// Spend Aurum Shards.
        /// Returns true if successful, false if insufficient balance.
        /// </summary>
        public bool SpendAurumShards(int amount)
        {
            if (amount <= 0 || aurumShards < amount)
                return false;

            aurumShards -= amount;
            Debug.Log($"CharacterState: Spent {amount} Aurum Shards. Remaining: {aurumShards}");
            return true;
        }

        /// <summary>
        /// Clear all inventory (for syncing from Unit).
        /// </summary>
        public void ClearInventory()
        {
            inventory.Clear();
        }

        /// <summary>
        /// Clear container inventory (for syncing from Unit).
        /// </summary>
        public void ClearContainerInventory()
        {
            containerInventory.Clear();
        }

        /// <summary>
        /// Set container slots (for syncing from Unit).
        /// </summary>
        public void SetContainerSlots(ContainerItem[] slots)
        {
            if (slots == null)
            {
                containerSlots = new ContainerItem[2];
            }
            else
            {
                containerSlots = new ContainerItem[slots.Length];
                System.Array.Copy(slots, containerSlots, slots.Length);
            }
        }

        /// <summary>
        /// Set Aurum Shards directly (for syncing from Unit).
        /// </summary>
        public void SetAurumShards(int amount)
        {
            aurumShards = Mathf.Max(0, amount);
        }

        /// <summary>
        /// Add item to container inventory (for syncing from Unit).
        /// </summary>
        public void AddToContainerInventory(Item item, int quantity)
        {
            if (item == null || quantity <= 0)
                return;

            int remaining = quantity;

            // Try to stack with existing slots first
            foreach (var slot in containerInventory)
            {
                if (slot != null && slot.CanStack(item))
                {
                    remaining = slot.AddToStack(remaining);
                    if (remaining <= 0)
                        return;
                }
            }

            // Create new slots for remaining quantity
            while (remaining > 0)
            {
                int stackSize = Mathf.Min(remaining, item.MaxStackSize);
                containerInventory.Add(new InventorySlot(item, stackSize));
                remaining -= stackSize;
            }
        }

        #endregion

        /// <summary>
        /// Restore runtime references after deserialization.
        /// </summary>
        public void RestoreRuntimeReferences()
        {
            // Restore definition
            if (!string.IsNullOrEmpty(characterID))
            {
                definition = Resources.Load<CharacterDefinition>($"Characters/{characterID}");
            }

            // Restore equipment
            equippedItems.Clear();
            foreach (var kvp in equippedItemIDs)
            {
                // Try to find equipment by name (simplified - in production, use GUID or proper lookup)
                EquipmentItem item = Resources.FindObjectsOfTypeAll<EquipmentItem>()
                    .FirstOrDefault(e => e.ItemName == kvp.Value);
                if (item != null)
                {
                    equippedItems[kvp.Key] = item;
                }
            }

            // Restore skills
            knownSkills.Clear();
            masteredSkills.Clear();
            masteredPassiveSkills.Clear();

            // Restore known skills
            foreach (var skillName in knownSkillNames)
            {
                Skill skill = Resources.FindObjectsOfTypeAll<Skill>()
                    .FirstOrDefault(s => s.SkillName == skillName);
                if (skill != null)
                {
                    knownSkills.Add(skill);
                }
            }

            // Restore mastered skills
            foreach (var skillName in masteredSkillNames)
            {
                Skill skill = Resources.FindObjectsOfTypeAll<Skill>()
                    .FirstOrDefault(s => s.SkillName == skillName);
                if (skill != null)
                {
                    masteredSkills.Add(skill);
                }
            }

            // Restore mastered passive skills
            foreach (var skillName in masteredPassiveSkillNames)
            {
                PassiveSkill passive = Resources.FindObjectsOfTypeAll<PassiveSkill>()
                    .FirstOrDefault(p => p.SkillName == skillName);
                if (passive != null)
                {
                    masteredPassiveSkills.Add(passive);
                }
            }

            // Restore status effects
            activeStatusEffects.Clear();
            foreach (var effectName in activeStatusEffectIDs)
            {
                StatusEffectData effect = Resources.FindObjectsOfTypeAll<StatusEffectData>()
                    .FirstOrDefault(e => e.EffectName == effectName);
                if (effect != null)
                {
                    activeStatusEffects.Add(effect);
                }
            }

            statsDirty = true;
        }
    }

    /// <summary>
    /// Serializable dictionary for Unity serialization.
    /// </summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                this[keys[i]] = values[i];
            }
        }
    }
}
