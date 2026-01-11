# Chapter and Narrative Track System - User Guide

## Overview

The chapter and narrative track system allows you to create branching storylines with different protagonists, manage party composition per chapter, and control story progression through requirements.

## Core Concepts

### Narrative Tracks
A **Narrative Track** is a complete storyline path. Think of it as a "route" through the game. You can have multiple tracks that tell different stories or focus on different characters.

**Example:**
- Track 1: "Alex's Journey" - Follows Alex as the main character
- Track 2: "Sarah's Path" - Follows Sarah as the main character
- Track 3: "The Rift" - A shared storyline both can experience

### Chapters
A **Chapter** is a segment within a narrative track. Each chapter has:
- A specific protagonist (POV character)
- A set of characters available for the party
- Required characters that must be in the party
- Progression requirements that must be met to advance

**Example:**
- Chapter 1: "The Awakening" - Alex wakes up, party: Alex only
- Chapter 2: "Meeting Sarah" - Alex meets Sarah, party: Alex + Sarah
- Chapter 3: "The Choice" - Player chooses path, party: Alex + Sarah + optional third character

### Character Movement Between Chapters/Tracks

Characters can move between chapters and tracks in several ways:

#### 1. **Chapter Progression (Same Track)**
When a chapter completes, the next chapter in the track loads automatically. Characters carry over their state (HP, level, equipment, etc.).

**Example Flow:**
```
Chapter 1 (Alex only) 
  → Complete requirements 
  → Chapter 2 loads (Alex + Sarah join)
  → Chapter 3 loads (Alex + Sarah + optional character)
```

#### 2. **Track Switching**
Players can switch to a different narrative track if it's unlocked. This changes the entire storyline.

**Example:**
```
Track 1: "Alex's Journey" - Chapter 3
  → Player makes choice
  → Switch to Track 2: "Sarah's Path" - Chapter 1
  → Different story, different party composition
```

#### 3. **Character Joining/Leaving**
Characters can join or leave the party based on:
- Chapter requirements (required characters)
- Chapter availability (available characters)
- Story events (joinChapter/leaveChapter metadata)

**Example:**
- Chapter 1: Alex starts alone
- Chapter 2: Sarah joins (required character)
- Chapter 3: Optional character can join (available character)
- Chapter 4: Sarah leaves (leaveChapter metadata)

## How It Works

### Initialization Flow

1. **Game Starts**
   - GameInitializer sets the starting narrative track
   - ChapterManager loads the first chapter of that track

2. **Chapter Loads**
   - ChapterManager reads the ChapterDefinition
   - PartyManager clears existing party
   - Required characters are created as CharacterStates and added to party
   - POV character is set from chapter definition
   - Available characters can be added later (optional)

3. **During Gameplay**
   - Party members persist across exploration and battle
   - Character data (HP, XP, equipment) is maintained
   - POV character's narrative skills are used for exploration checks

4. **Chapter Progression**
   - Player completes requirements (defeat boss, complete quest, etc.)
   - `ChapterManager.CompleteRequirement("requirement_id")` is called
   - When all requirements are met, `CanProgressToNextChapter()` returns true
   - `ProgressToNextChapter()` loads the next chapter
   - Party composition updates based on new chapter's requirements

### Character State Persistence

**Important:** Character states persist across chapters and tracks. When a character appears in multiple chapters:
- Their level, XP, SP, equipment, and mastered skills are preserved
- Only party composition changes (who's in the active party)
- HP and status effects are maintained

**Example:**
```
Chapter 1: Alex (Level 5, 100 HP, Iron Sword)
  → Battle happens, Alex takes damage (now 50 HP)
  → Chapter 2 loads
  → Alex is still Level 5, still has 50 HP, still has Iron Sword
```

### Progression Requirements

Each chapter has a list of `progressionRequirements` (string IDs). These are checked when determining if the chapter can progress.

**Common Requirement Types:**
- `"defeat_boss_chapter1"` - Boss defeated
- `"complete_quest_001"` - Quest completed
- `"reach_location_town"` - Location reached
- `"talk_to_npc_elder"` - NPC interaction

**How to Use:**
```csharp
// When boss is defeated
ChapterManager.Instance.CompleteRequirement("defeat_boss_chapter1");

// When quest completes
ChapterManager.Instance.CompleteRequirement("complete_quest_001");

// Check if can progress
if (ChapterManager.Instance.CanProgressToNextChapter())
{
    ChapterManager.Instance.ProgressToNextChapter();
}
```

## Setting Up Chapters and Tracks

### Step 1: Create Character Definitions
1. Create CharacterDefinition assets for all characters
2. Set `isPOVCharacter = true` for characters who can be protagonists
3. Set `availableChapters` and `narrativeTracks` to indicate where they appear

### Step 2: Create Chapter Definitions
1. Create ChapterDefinition for each chapter
2. Assign the `narrativeTrack` it belongs to
3. Set `povCharacter` (who is the protagonist)
4. Add `requiredCharacters` (must be in party)
5. Add `availableCharacters` (can be in party)
6. Set `progressionRequirements` (what unlocks next chapter)
7. Optionally set `nextChapter` (or let track handle ordering)

### Step 3: Create Narrative Track
1. Create NarrativeTrack asset
2. Add chapters in order (this determines chapter sequence)
3. Add `trackSpecificCharacters` (characters unique to this track)

### Step 4: Initialize in Game
1. Set starting track in GameInitializer
2. ChapterManager automatically loads first chapter
3. Party is set up from chapter requirements

## Example: Multi-Track Story

**Track 1: "The Warrior's Path"**
- Chapter 1: Alex starts journey (Alex only)
- Chapter 2: Alex meets mentor (Alex + Mentor)
- Chapter 3: Alex faces trial (Alex only, mentor leaves)
- Chapter 4: Alex becomes warrior (Alex + new companion)

**Track 2: "The Scholar's Path"**
- Chapter 1: Sarah starts journey (Sarah only)
- Chapter 2: Sarah finds ancient text (Sarah + Scholar)
- Chapter 3: Sarah deciphers mystery (Sarah + Scholar)
- Chapter 4: Sarah discovers truth (Sarah only, scholar leaves)

**Track 3: "The United Path"** (Unlocked after completing either Track 1 or 2)
- Chapter 1: Alex and Sarah meet (Alex + Sarah)
- Chapter 2: They combine forces (Alex + Sarah + others)
- Chapter 3: Final confrontation (Full party)

**Character Movement:**
- Alex appears in Track 1 and Track 3
- Sarah appears in Track 2 and Track 3
- When switching tracks, characters maintain their progression
- Party composition changes based on current chapter

## Best Practices

1. **Character IDs**: Use consistent naming (e.g., "char_alex_001")
2. **Chapter IDs**: Use consistent naming (e.g., "chapter_001")
3. **Requirement IDs**: Use descriptive names (e.g., "defeat_boss_chapter1")
4. **Track Ordering**: Keep chapters in narrative track in story order
5. **POV Characters**: Only one POV per chapter, but can change between chapters
6. **Party Size**: Respect `partySizeLimit` in PartyManager (default 6)

## Common Patterns

### Pattern 1: Linear Story
- Single track
- Chapters in order
- Characters join as story progresses
- Simple progression requirements

### Pattern 2: Branching Story
- Multiple tracks
- Player choice determines track
- Some characters appear in multiple tracks
- Tracks can converge later

### Pattern 3: Character-Specific Routes
- Each track focuses on one character
- That character is always POV in their track
- Other characters appear as supporting cast
- Tracks can be played in any order (if unlocked)

### Pattern 4: Dynamic Party
- Characters join/leave based on story events
- Use `joinChapter` and `leaveChapter` metadata
- Party composition changes frequently
- Maintains character state across changes
