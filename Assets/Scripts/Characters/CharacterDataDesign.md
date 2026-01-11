# Character Data Structure Design

## Overview

Character data is split into three layers: static definitions (ScriptableObjects), runtime state (in-memory objects), and metadata (story/availability information).

## Data Layers

### Layer 1: CharacterDefinition (Static)

**Type**: ScriptableObject  
**Purpose**: Never-changing character data  
**Location**: Assets folder as .asset files

**Fields:**
```csharp
- string characterID (unique identifier, e.g., "char_alex_001")
- string characterName (display name)
- Sprite portrait
- string bio (character description)

// Base Stats (starting values)
- int baseStrength
- int baseFinesse
- int baseFocus
- int baseSpeed
- int baseLuck

// Base Narrative Skills
- int basePerception
- int baseInterpretive
- int baseEmpathic

// Skills and Equipment
- List<Skill> availableSkills (skills this character can learn)
- List<EquipmentItem> startingEquipment (optional starting gear)

// Character Type
- CharacterClass characterClass (e.g., Warrior, Mage, Rogue)
- MantleType mantle (magical affinity)
```

**Usage**: Created by designers, never modified at runtime. Referenced by CharacterState for base values.

### Layer 2: CharacterState (Runtime)

**Type**: Serializable Class  
**Purpose**: Current character state that changes during gameplay  
**Location**: Managed by PartyManager, stored in memory

**Fields:**
```csharp
- string characterID (reference to CharacterDefinition)
- CharacterDefinition definition (reference to static data)

// Current Stats (base + equipment + level bonuses)
- int currentStrength
- int currentFinesse
- int currentFocus
- int currentSpeed
- int currentLuck

// Current Narrative Skills (may change)
- int currentPerception
- int currentInterpretive
- int currentEmpathic

// Progression
- int level
- int currentXP
- int skillPoints (SP)
- int totalActions

// Combat State
- int currentHP
- int maxHP
- List<StatusEffect> activeStatusEffects

// Equipment (current)
- Dictionary<EquipmentSlot, EquipmentItem> equippedItems

// Skills
- List<Skill> knownSkills
- HashSet<Skill> masteredSkills
- HashSet<PassiveSkill> masteredPassiveSkills
```

**Methods:**
```csharp
- CalculateStats() // Recalculate stats from base + equipment + level
- ApplyLevelUp() // Handle level up, stat increases
- TakeDamage(amount) // Update HP
- EquipItem(item, slot) // Equip item, update stats
- LearnSkill(skill) // Add skill to known skills
- MasterSkill(skill) // Mark skill as mastered
```

**Serialization**: Must be serializable for save system.

### Layer 3: CharacterMetadata (Story)

**Type**: ScriptableObject or Class  
**Purpose**: Story and availability information  
**Location**: Can be part of CharacterDefinition or separate

**Fields:**
```csharp
- bool isPOVCharacter (can be protagonist)
- List<string> availableChapters (chapter IDs where character can appear)
- List<string> requiredChapters (must appear in these chapters)
- List<string> narrativeTracks (which storylines include this character)
- int joinChapter (when character joins party, if applicable)
- int leaveChapter (when character leaves party, if applicable)
- CharacterRelationship[] relationships (relationships with other characters)
```

## Data Flow

### Character Creation Flow

1. **Design Time**: Create CharacterDefinition ScriptableObject
2. **Chapter Start**: 
   - Load CharacterDefinition
   - Create CharacterState from definition
   - Initialize with base values
   - Apply starting equipment
3. **Runtime**: CharacterState is updated as player progresses
4. **Save Time**: Serialize CharacterState to save file
5. **Load Time**: Deserialize CharacterState, link to CharacterDefinition

### Stat Calculation Flow

```
Base Stats (CharacterDefinition)
    +
Level Bonuses (from progression)
    +
Equipment Bonuses (from equipped items)
    +
Passive Skill Bonuses (from mastered passives)
    =
Current Stats (CharacterState)
```

### Equipment Flow

1. Player equips item in Status Menu
2. CharacterState.EquipItem() called
3. Item removed from inventory
4. Item added to equippedItems dictionary
5. Stats recalculated
6. UI updated

### Skill Learning Flow

1. Character uses skill from equipment (temporary)
2. Player spends SP to master skill
3. CharacterState.MasterSkill() called
4. Skill added to masteredSkills
5. Skill now permanently available
6. SP deducted

## Integration with Existing Systems

### Unit.cs Integration

**Current**: Unit.cs has stats, skills, equipment directly  
**Future**: Unit.cs should reference CharacterState

**Migration Strategy:**
1. Add CharacterState reference to Unit
2. Initialize Unit from CharacterState in battle
3. Update CharacterState from Unit after battle
4. Gradually move logic from Unit to CharacterState

### PartyManager Integration

**Current**: PartyManager tracks Units  
**Future**: PartyManager tracks CharacterStates

**Changes Needed:**
1. Replace Unit references with CharacterState
2. Create Units from CharacterStates when needed
3. Update CharacterStates from Units after battles

### Exploration Integration

**Current**: Exploration uses first player unit  
**Future**: Exploration uses POV character's narrative skills

**Changes Needed:**
1. Get POV character from PartyManager
2. Use POV character's narrative skills for exploration checks
3. Display POV character in Status Menu

## State Management Patterns

### Singleton Pattern
- PartyManager: Manages all CharacterStates
- ChapterManager: Manages current chapter/track

### Observer Pattern
- CharacterState changes notify UI
- Equipment changes notify stat displays
- Level up events notify progression UI

### Factory Pattern
- CharacterStateFactory: Creates CharacterState from CharacterDefinition
- UnitFactory: Creates Unit from CharacterState (for battle)

## Serialization Strategy

### Save Format (Future)
```json
{
  "characterStates": [
    {
      "characterID": "char_alex_001",
      "level": 5,
      "currentXP": 250,
      "skillPoints": 12,
      "currentHP": 85,
      "maxHP": 100,
      "equippedItems": {
        "MeleeWeapon": "sword_iron_001",
        "Armor": "armor_leather_001"
      },
      "masteredSkills": ["skill_slash_001", "skill_block_001"],
      "activeStatusEffects": []
    }
  ],
  "partyMembers": ["char_alex_001", "char_sarah_002"],
  "povCharacter": "char_alex_001"
}
```

### Validation on Load
- Check CharacterDefinition exists for each CharacterState
- Validate equipment items exist
- Validate skills exist
- Check for invalid stat values
- Recalculate stats if needed

## Performance Considerations

### Memory
- CharacterState objects are lightweight (mostly references)
- CharacterDefinitions loaded once at startup
- CharacterStates only exist for active party members

### Updates
- Stat recalculation only when needed (equipment change, level up)
- UI updates only when state changes (observer pattern)
- Batch updates when possible (multiple equipment changes)

### Caching
- Cache calculated stats in CharacterState
- Invalidate cache when equipment/skills change
- Recalculate on demand

## Example Usage

### Creating a Character
```csharp
// 1. Load definition
CharacterDefinition def = Resources.Load<CharacterDefinition>("Characters/Alex");

// 2. Create state
CharacterState state = CharacterStateFactory.Create(def);

// 3. Add to party
PartyManager.Instance.AddPartyMember(state);
```

### Updating Character
```csharp
// Level up
state.AwardXP(100);
if (state.LeveledUp)
{
    state.ApplyLevelUp();
}

// Equip item
state.EquipItem(sword, EquipmentSlot.MeleeWeapon);

// Master skill
state.MasterSkill(fireballSkill);
```

### Using in Battle
```csharp
// Create Unit from CharacterState
Unit battleUnit = UnitFactory.CreateFromCharacterState(characterState);

// After battle, update state
characterState.UpdateFromUnit(battleUnit);
```

## Migration Notes

### From Current System
- Unit.cs currently has all character data
- Need to extract to CharacterState
- Keep Unit.cs for battle-specific logic
- CharacterState becomes source of truth

### Backward Compatibility
- Support loading old save files
- Migrate Unit data to CharacterState
- Handle missing fields gracefully
