# Testing Guide - Party System

## Test 1: Status Menu Party Display

This test verifies that party members are properly displayed in the Status Menu.

### Prerequisites

1. **Create Test Character:**
   - Create folder: `Assets/Resources/Characters/`
   - Right-click → `Create → Riftbourne → Characters → Character Definition`
   - Name asset: `TestCharacter`
   - Set fields:
     - `characterID` = "char_test_001"
     - `characterName` = "Test Hero"
     - `baseStrength` = 10
     - `baseFinesse` = 8
     - `baseFocus` = 7
     - `baseSpeed` = 9
     - `baseLuck` = 6
     - `basePerception` = 7
     - `baseInterpretive` = 5
     - `baseEmpathic` = 6
     - **`isPOVCharacter` = true** (IMPORTANT!)
   - Save asset

2. **Create Test Chapter:**
   - Create folder: `Assets/Resources/Chapters/`
   - Right-click → `Create → Riftbourne → Story → Chapter Definition`
   - Name asset: `TestChapter`
   - Set fields:
     - `chapterID` = "chapter_test_001"
     - `chapterName` = "Test Chapter"
     - **`narrativeTrack`** = (will assign after creating track)
     - **`povCharacter`** = Drag `TestCharacter` asset here
     - **`requiredCharacters`** = Add `TestCharacter` to list
     - `startingLocation` = "ExplorationScene"
   - Save asset

3. **Create Test Track:**
   - Create folder: `Assets/Resources/Tracks/`
   - Right-click → `Create → Riftbourne → Story → Narrative Track`
   - Name asset: `TestTrack`
   - Set fields:
     - `trackID` = "track_test_001"
     - `trackName` = "Test Story"
     - **`chapters`** = Add `TestChapter` to list
   - Save asset
   - **Go back to TestChapter** and assign `TestTrack` to `narrativeTrack` field

### Unity Setup

1. **Set Up Managers (in your first scene or exploration scene):**
   - Create empty GameObject: **"GameManagers"**
   - Add Component → **Party Manager**
   - Add Component → **Chapter Manager**

2. **Set Up Initialization (in Exploration Scene):**
   - Create empty GameObject: **"ExplorationInitializer"**
   - Add Component → **Exploration Scene Initializer**
   - Set `autoLoadOnStart = true`
   - **Assign `defaultTrack`** = Drag `TestTrack` asset
   - Set `onlyLoadIfPartyEmpty = true`

3. **Verify StatusMenuUI:**
   - Ensure StatusMenuUI component exists in scene (should already be set up)
   - No changes needed - it will automatically use POV character

### Testing Steps

1. **Enter Play Mode**
   - Press Play button
   - Watch Console for messages

2. **Check Console Output:**
   You should see messages like:
   ```
   ExplorationSceneInitializer: Auto-loading first chapter 'Test Chapter' from track 'Test Story'
   ChapterManager: Loading chapter Test Chapter (ID: chapter_test_001)
   CharacterStateFactory: Created CharacterState for Test Hero (ID: char_test_001)
   PartyManager: Added char_test_001 to party (1/6)
   PartyManager: Set char_test_001 as POV character
   ChapterManager: Set up party with 1 members
   ```

3. **Open Status Menu:**
   - Press **TAB** key
   - Status Menu should open

4. **Verify Status Tab:**
   - **Character Name:** Should show "Test Hero"
   - **Level:** Should show "Level: 1"
   - **XP:** Should show "XP: 0 / [some number]"
   - **SP:** Should show "SP: 0"
   - **Strength:** Should show "Strength: 10"
   - **Finesse:** Should show "Finesse: 8"
   - **Focus:** Should show "Focus: 7"
   - **Speed:** Should show "Speed: 9"
   - **Luck:** Should show "Luck: 6"

5. **Verify Narrative Skills Tab:**
   - Switch to Narrative Skills tab (if you have the button)
   - **Perception:** Should show "Perception: 7"
   - **Interpretive:** Should show "Interpretive: 5"
   - **Empathic:** Should show "Empathic: 6"

### Expected Results

✅ **Success Indicators:**
- Console shows party member added
- Console shows POV character set
- Status Menu opens with TAB key
- Character name displays correctly
- All stats display correctly
- Narrative skills display correctly

❌ **Failure Indicators:**
- Status Menu shows "No Character Selected"
- Console shows errors about missing CharacterDefinition
- Console shows "PartyManager: Party already has X members" (if party wasn't cleared)
- Stats show 0 or incorrect values

### Troubleshooting

**If Status Menu shows "No Character Selected":**
1. Check Console for "PartyManager: Added [character] to party" message
2. Check Console for "PartyManager: Set [character] as POV character" message
3. Verify CharacterDefinition has `isPOVCharacter = true`
4. Verify chapter has character in `requiredCharacters` list
5. Check that ExplorationSceneInitializer has `defaultTrack` assigned

**If Stats are 0 or wrong:**
1. Check CharacterDefinition base stats are set correctly
2. Check Console for "CharacterStateFactory: Created CharacterState" message
3. Verify CharacterState is being created properly

**If Chapter doesn't load:**
1. Check ExplorationSceneInitializer has `defaultTrack` assigned
2. Check Console for "ExplorationSceneInitializer: Auto-loading" message
3. Verify NarrativeTrack has chapters in the list
4. Verify ChapterDefinition has `narrativeTrack` assigned

### Next Test: Multiple Party Members

Once single character works:

1. **Create Second Character:**
   - Create another CharacterDefinition: `TestCharacter2`
   - Set `isPOVCharacter = false` (supporting character)
   - Set different stats

2. **Update Chapter:**
   - Add `TestCharacter2` to `requiredCharacters` or `availableCharacters`

3. **Test Again:**
   - Status Menu should still show POV character (first character)
   - Console should show both characters added to party
   - Verify party size is 2

### Advanced Test: Chapter Progression

1. **Create Second Chapter:**
   - Create `TestChapter2`
   - Set different POV character or same
   - Add to track's chapters list (after first chapter)

2. **Complete Requirements:**
   - In code or via button, call:
     ```csharp
     ChapterManager.Instance.CompleteRequirement("test_requirement");
     ChapterManager.Instance.ProgressToNextChapter();
     ```

3. **Verify:**
   - New chapter loads
   - Party updates based on new chapter requirements
   - Status Menu updates to show new POV character (if changed)
