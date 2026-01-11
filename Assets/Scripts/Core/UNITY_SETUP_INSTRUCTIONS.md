# Unity Setup Instructions - Party System

## Quick Setup Guide

Follow these steps to set up the party system in Unity Editor.

## Step 1: Create ScriptableObject Assets

### 1.1 Create Character Definitions

1. **Create folder structure:**
   - `Assets/Resources/Characters/`

2. **Create CharacterDefinition assets:**
   - Right-click in Project window → `Create → Riftbourne → Characters → Character Definition`
   - For each character:
     - Set `characterID` (e.g., "char_alex_001")
     - Set `characterName` (e.g., "Alex")
     - Assign `portrait` sprite (if you have one)
     - Set base stats (Strength, Finesse, Focus, Speed, Luck) - start with 5 each
     - Set base narrative skills (Perception, Interpretive, Empathic) - start with 5, 3, 4
     - Set `mantle` type (or None)
     - Add `availableSkills` (drag Skill assets if you have them)
     - Add `startingEquipment` (drag EquipmentItem assets if you have them)
     - **Important:** Set `isPOVCharacter = true` for characters who can be protagonists
     - Add `availableChapters` (list of chapter ID strings, e.g., "chapter_001")
     - Add `narrativeTracks` (list of track ID strings, e.g., "track_001")

3. **Save assets** in `Assets/Resources/Characters/`

### 1.2 Create Chapter Definitions

1. **Create folder:**
   - `Assets/Resources/Chapters/`

2. **Create ChapterDefinition assets:**
   - Right-click → `Create → Riftbourne → Story → Chapter Definition`
   - For each chapter:
     - Set `chapterID` (e.g., "chapter_001")
     - Set `chapterName` (e.g., "The Beginning")
     - **Assign `narrativeTrack`** (drag a NarrativeTrack asset)
     - **Assign `povCharacter`** (drag a CharacterDefinition that has `isPOVCharacter = true`)
     - Add `requiredCharacters` (drag CharacterDefinitions that MUST be in party)
     - Add `availableCharacters` (drag CharacterDefinitions that CAN be in party)
     - Set `startingLocation` (scene name, e.g., "ExplorationScene")
     - Add `progressionRequirements` (string IDs, e.g., "defeat_boss", "complete_quest")
     - Optionally assign `nextChapter` (or let track handle ordering)

3. **Save assets** in `Assets/Resources/Chapters/`

### 1.3 Create Narrative Track

1. **Create folder:**
   - `Assets/Resources/Tracks/`

2. **Create NarrativeTrack asset:**
   - Right-click → `Create → Riftbourne → Story → Narrative Track`
   - Set `trackID` (e.g., "track_001")
   - Set `trackName` (e.g., "Main Story")
   - **Add `chapters` in order** (drag ChapterDefinitions in story order - this is important!)
   - Add `trackSpecificCharacters` (drag CharacterDefinitions unique to this track)

3. **Save asset** in `Assets/Resources/Tracks/`

## Step 2: Set Up Core Managers (Persistent)

### 2.1 Create Persistent Manager GameObject

1. **In your first scene (or a dedicated "Managers" scene):**
   - Create empty GameObject: `GameObject → Create Empty`
   - Name it: **"GameManagers"**
   - **Important:** This GameObject should persist across scenes

2. **Add PartyManager component:**
   - Select "GameManagers"
   - `Add Component → Party Manager`
   - Set `partySizeLimit` (default 6 is fine)
   - Assign `selectionRingPrefab` if you have one (optional)
   - Assign `selectionRingPool` if using object pooling (optional)
   - Assign `unitSelectionSound` AudioClip if desired (optional)

3. **Add ChapterManager component:**
   - Still on "GameManagers"
   - `Add Component → Chapter Manager`
   - No Inspector fields needed (managed via code)

4. **Make persistent (if not using DontDestroyOnLoad scene):**
   - The code handles `DontDestroyOnLoad` automatically, but ensure this GameObject exists before other scenes load

## Step 3: Set Up Game Initialization

### 3.1 Set Up GameInitializer (First Scene)

**Option A: Menu/Start Scene**
1. In your menu or first scene:
   - Create empty GameObject: **"GameInitializer"**
   - `Add Component → Game Initializer`
   - **Assign `startingTrack`** (drag your NarrativeTrack asset)
   - Set `autoLoadFirstChapter = true`
   - Set `forceInitialize = false` (unless testing)

**Option B: Exploration Scene (if starting directly)**
1. In your Exploration scene:
   - Create empty GameObject: **"GameInitializer"**
   - `Add Component → Game Initializer`
   - **Assign `startingTrack`** (drag your NarrativeTrack asset)
   - Set `autoLoadFirstChapter = true`

### 3.2 Set Up ExplorationSceneInitializer (Exploration Scene)

1. **In your Exploration scene:**
   - Create empty GameObject: **"ExplorationInitializer"** (or add to existing manager)
   - `Add Component → Exploration Scene Initializer`
   - Set `autoLoadOnStart = true`
   - **Assign `defaultTrack`** (drag your NarrativeTrack asset - used if no track is set)
   - Set `onlyLoadIfPartyEmpty = true` (prevents reloading if party exists)

2. **This ensures:**
   - When exploration scene loads, if no party exists, it automatically loads the first chapter
   - Party is set up from chapter requirements
   - POV character is set

## Step 4: Set Up Scene Transitions

### 4.1 Battle Scene Setup

1. **In your Battle scene:**
   - Create empty GameObject: **"BattleInitializer"**
   - `Add Component → Battle Scene Initializer`
   - **Assign `unitPrefab`** (your Unit prefab)
   - Set `autoPosition = true` (or false for manual positioning)
   - If auto-positioning: set `autoStartX = 1`, `autoStartY = 1` (grid coordinates)
   - If manual: add positions to `partyStartPositions` list

2. **Add BattleEndHandler:**
   - Create empty GameObject: **"BattleEndHandler"** (or add to same GameObject)
   - `Add Component → Battle End Handler`
   - Set `explorationSceneName = "ExplorationScene"` (or your exploration scene name)
   - Set `transitionDelay = 2.0` (seconds before returning)

### 4.2 Exploration Scene - Battle Trigger

1. **In your Exploration scene:**
   - Create empty GameObject: **"ExplorationManager"** (or use existing)
   - `Add Component → Battle Scene Loader`
   - Set `battleSceneName = "BattleScene"` (or your battle scene name)
   - **Assign `unitPrefab`** (your Unit prefab)

2. **Wire up battle trigger:**
   - Call `BattleSceneLoader.LoadBattleScene()` when you want to start a battle
   - Can be from button, enemy encounter, etc.

## Step 5: Testing Setup - Status Menu

### 5.1 Verify StatusMenuUI

1. **In your Exploration scene:**
   - Ensure StatusMenuUI component exists (should already be set up)
   - No Inspector changes needed - code automatically uses POV character

### 5.2 Test Party Display

1. **Create test setup:**
   - Create at least one CharacterDefinition with `isPOVCharacter = true`
   - Create one ChapterDefinition that requires that character
   - Create one NarrativeTrack with that chapter
   - Assign track to GameInitializer or ExplorationSceneInitializer

2. **Test in Play Mode:**
   - Enter Play Mode
   - Check Console for initialization messages
   - Press **TAB** to open Status Menu
   - **Status Tab should show:**
     - Character name (from POV character)
     - Level, XP, SP
     - Stats (Strength, Finesse, Focus, Speed, Luck)
   - **Narrative Skills Tab should show:**
     - POV character's narrative skill levels

3. **Verify party members:**
   - Check Console logs for "PartyManager: Added [characterID] to party"
   - Status Menu should display POV character data
   - If multiple party members, Status Menu shows POV (protagonist)

## Step 6: Build Settings

1. **Add scenes to Build Settings:**
   - File → Build Settings
   - Add Exploration scene
   - Add Battle scene
   - Add any other scenes
   - Set first scene (menu or exploration) as Scene 0

## Troubleshooting

### "Party is empty in Status Menu"
- **Check:** Is ChapterManager loading a chapter?
- **Check:** Does the chapter have required characters?
- **Check:** Are CharacterDefinitions in `Resources/Characters/`?
- **Check:** Console for "ChapterManager: Set up party" message

### "POV character not showing"
- **Check:** Does CharacterDefinition have `isPOVCharacter = true`?
- **Check:** Is that character in the chapter's `requiredCharacters`?
- **Check:** Console for "PartyManager: Set [characterID] as POV character"

### "Chapter not loading"
- **Check:** Is GameInitializer or ExplorationSceneInitializer in scene?
- **Check:** Is `startingTrack` or `defaultTrack` assigned?
- **Check:** Does track have chapters?
- **Check:** Console for initialization errors

### "Status Menu shows 'No Character Selected'"
- **Check:** Is PartyManager.Instance not null?
- **Check:** Does party have members? (Check Console logs)
- **Check:** Is POV character set? (Check Console logs)

## Quick Test Checklist

- [ ] Created at least 1 CharacterDefinition with `isPOVCharacter = true`
- [ ] Created at least 1 ChapterDefinition with that character as POV and required
- [ ] Created 1 NarrativeTrack with that chapter
- [ ] Set up GameManagers GameObject with PartyManager and ChapterManager
- [ ] Set up GameInitializer or ExplorationSceneInitializer with track assigned
- [ ] Entered Play Mode
- [ ] Checked Console for "ChapterManager: Loading chapter" message
- [ ] Checked Console for "PartyManager: Added [character] to party" message
- [ ] Pressed TAB to open Status Menu
- [ ] Verified character name, stats, and narrative skills display correctly

## Next Steps After Testing

Once Status Menu works:
1. Test chapter progression (complete requirements, advance to next chapter)
2. Test battle transitions (exploration → battle → exploration)
3. Test multiple party members
4. Test POV character switching between chapters
5. Test narrative skill checks in exploration
