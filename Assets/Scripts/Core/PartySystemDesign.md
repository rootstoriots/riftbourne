# Party System Architecture Design

## Overview

The party system must support multiple narrative tracks with different POV characters, chapter-based party composition changes, and seamless data flow between exploration and battle modes.

## Core Requirements

1. **Multiple Narrative Tracks**: Different storylines that can be played
2. **POV Character Switching**: Different protagonists per chapter
3. **Chapter-Based Party Composition**: Party members change based on current chapter
4. **Data Persistence**: Character data flows between exploration and battle
5. **Mode Integration**: Party works in both exploration and battle modes

## Architecture Components

### 1. Character Data Structure

#### CharacterDefinition (ScriptableObject)
Static character data that never changes:
- Character ID (unique identifier)
- Character Name
- Base Stats (Strength, Finesse, Focus, Speed, Luck)
- Base Narrative Skills (Perception, Interpretive, Empathic)
- Character Portrait
- Character Bio/Description
- Available Skills (list of Skill ScriptableObjects)
- Starting Equipment (optional)

#### CharacterState (Runtime Data)
Dynamic character state that changes during gameplay:
- Current HP
- Current Level
- Current XP
- Current SP (Skill Points)
- Current Equipment (what's equipped now)
- Active Status Effects
- Current Narrative Skill Levels (may change)
- Mastered Skills
- Total Actions (for progression)

#### CharacterMetadata
Character availability and story information:
- Is POV Character (can this character be the protagonist?)
- Available Chapters (which chapters can this character appear in)
- Narrative Tracks (which storylines include this character)
- Required Chapters (must appear in these chapters)
- Optional Chapters (can appear in these chapters)

### 2. Party System

#### PartyManager (Singleton)
Manages the active party composition:
- Current Party Members (list of CharacterState)
- Current POV Character (who is the protagonist)
- Party Size Limit
- Methods:
  - `AddPartyMember(character)`
  - `RemovePartyMember(character)`
  - `SetPOVCharacter(character)`
  - `GetPartyMembers()`
  - `IsInParty(character)`

#### PartyData (Runtime Data)
Current party state:
- Active Members (list of CharacterState references)
- POV Character (CharacterState reference)
- Party Formation (order/positions for battle)
- Party Level (average or highest level)

### 3. Chapter/POV System

#### ChapterDefinition (ScriptableObject)
Defines a chapter in the game:
- Chapter ID
- Chapter Name
- Narrative Track (which storyline this belongs to)
- POV Character (who is the protagonist for this chapter)
- Available Characters (which characters can be in party)
- Required Characters (must be in party)
- Starting Location (where exploration starts)
- Chapter Progression Requirements (what unlocks next chapter)

#### NarrativeTrack (ScriptableObject)
Defines a complete storyline:
- Track ID
- Track Name
- Track Description
- Chapters (ordered list of ChapterDefinitions)
- Track-Specific Characters (characters unique to this track)

#### ChapterManager (Singleton)
Manages current chapter and track:
- Current Chapter (ChapterDefinition)
- Current Narrative Track (NarrativeTrack)
- Chapter Progression State
- Methods:
  - `LoadChapter(chapterID)`
  - `GetCurrentChapter()`
  - `GetCurrentTrack()`
  - `CanProgressToNextChapter()`
  - `ProgressToNextChapter()`

### 4. Exploration-Battle Bridge

#### Data Flow Architecture

**Exploration → Battle:**
1. PartyManager maintains party state during exploration
2. When battle starts:
   - PartyManager exports party data (CharacterState for each member)
   - Battle scene receives party data
   - Battle scene creates Unit GameObjects from CharacterState
   - Equipment, stats, skills are applied to Units

**Battle → Exploration:**
1. During battle, Units update their state (HP, status effects, etc.)
2. When battle ends:
   - Unit states are exported back to CharacterState
   - PartyManager updates party member states
   - Exploration resumes with updated character data

#### BattleSceneLoader
Handles transition from exploration to battle:
- Receives party data from PartyManager
- Creates battle scene with party members
- Applies character data to battle Units
- Handles battle completion and data return

#### ExplorationSceneLoader
Handles transition from battle to exploration:
- Receives updated character data from battle
- Updates PartyManager with new character states
- Resumes exploration at correct location
- Applies any story progression from battle

## System Relationships

```
CharacterDefinition (SO)
    ↓ (instantiated at chapter start)
CharacterState (Runtime)
    ↓ (managed by)
PartyManager
    ↓ (used by)
ExplorationController / BattleScene
    ↓ (updates)
CharacterState
    ↓ (persisted)
Save System (future)
```

## State Management

### Where Data Lives

1. **Character Definitions**: ScriptableObjects in Assets (never change)
2. **Character States**: Runtime objects managed by PartyManager
3. **Party Composition**: PartyManager singleton
4. **Chapter/Track State**: ChapterManager singleton
5. **Current Location**: ExplorationController or SceneManager

### Data Persistence Strategy

**Current Session:**
- CharacterState objects persist in PartyManager
- Updated after each battle
- Maintained across scene transitions

**Future Save System:**
- Save CharacterState data to file
- Save current chapter/track
- Save party composition
- Save exploration location
- Load all data on game start

## Integration Points

### Exploration Mode Integration

1. **Party Display**: Status Menu shows current party members
2. **POV Character**: Exploration uses POV character's narrative skills
3. **Party Formation**: Party order affects exploration (who leads, who follows)
4. **Character Switching**: Can switch which character is "active" in exploration

### Battle Mode Integration

1. **Party to Units**: Party members become battle Units
2. **Equipment Transfer**: Equipped items affect battle stats
3. **Skill Transfer**: Known/mastered skills available in battle
4. **State Sync**: Battle updates reflect back to party members

### Chapter Transitions

1. **Chapter Start**: Load chapter definition, set up party, set POV
2. **Chapter End**: Save progress, unlock next chapter
3. **Track Switching**: Can switch between narrative tracks (if unlocked)

## Future Considerations

### Save System
- Save/load character states
- Save/load party composition
- Save/load chapter progress
- Multiple save slots
- Save game versioning

### Multiple Playthroughs
- Track which chapters/tracks completed
- New Game+ support
- Character progression across playthroughs

### Character Relationships
- Relationship system between characters
- Party member interactions
- Character-specific dialogue

### Dynamic Party Events
- Characters join/leave party during exploration
- Temporary party members
- Character-specific story events

## Implementation Phases

### Phase 1: Basic Structure (Current)
- Design architecture (this document)
- Define data structures
- Plan integration points

### Phase 2: Character System
- Create CharacterDefinition ScriptableObject
- Create CharacterState class
- Create CharacterMetadata structure

### Phase 3: Party Management
- Update PartyManager to use CharacterState
- Implement party composition management
- Add POV character support

### Phase 4: Chapter System
- Create ChapterDefinition ScriptableObject
- Create NarrativeTrack ScriptableObject
- Implement ChapterManager

### Phase 5: Exploration-Battle Bridge
- Implement BattleSceneLoader
- Implement ExplorationSceneLoader
- Test data flow between modes

### Phase 6: Save System (Future)
- Implement save/load functionality
- Add save slot management
- Add versioning support

## Notes

- CharacterState should be serializable for save system
- PartyManager should handle party size limits
- Chapter transitions should be smooth (fade, loading screen, etc.)
- Character data should validate on load (check for missing equipment, invalid skills, etc.)
- Consider performance for large parties (4-6 members typical)
