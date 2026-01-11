# Battle Scene Setup Instructions

## Overview

This guide will help you set up the battle scene to properly receive character data from the exploration scene and ensure all information flows correctly between scenes.

## Data Flow Summary

1. **Exploration → Battle:**
   - `BattleSceneLoader` exports party `CharacterState` data to `SceneTransitionData`
   - Battle scene loads
   - `BattleSceneInitializer` reads from `SceneTransitionData`
   - `UnitFactory` creates `Unit` GameObjects from `CharacterState` data
   - Units are positioned on the grid and registered with `PartyManager`

2. **Battle → Exploration:**
   - Battle ends (victory/defeat)
   - `BattleEndHandler` collects updated `Unit` data
   - `ExplorationSceneLoader` updates `CharacterState` from `Unit` data
   - Exploration scene loads with updated party

## Step 0: Add Scenes to Build Settings (IMPORTANT!)

**⚠️ CRITICAL:** Before you can load scenes via code, they must be added to Unity's Build Settings.

### 0.1 Add Battle Scene to Build Settings

1. **Open Build Settings:**
   - Menu: `File → Build Settings...` (or press `Ctrl+Shift+B` / `Cmd+Shift+B` on Mac)

2. **Add Battle Scene:**
   - In the Project window, navigate to your Battle Scene
   - **Drag the Battle Scene** from Project window into the "Scenes In Build" list in Build Settings
   - OR click "Add Open Scenes" if the Battle Scene is currently open

3. **Add Exploration Scene (if not already added):**
   - Drag your Exploration Scene into the "Scenes In Build" list
   - Make sure it's at index 0 if it's your starting scene

4. **Verify Scene Names:**
   - Check the scene names match what you've set in `BattleSceneLoader` and `BattleEndHandler`
   - Default: "BattleScene" and "ExplorationScene"
   - If your scenes have different names, update the component settings

5. **Save Build Settings:**
   - The scenes are automatically saved

### 0.2 Troubleshooting

**Error: "Scene 'BattleScene' couldn't be loaded because it has not been added to the active build profile"**

**Solution:**
- Follow Step 0.1 above to add the scene to Build Settings
- Make sure the scene name in Build Settings matches exactly (case-sensitive)
- Check that the scene file exists in your Assets folder

**Note:** In the Editor, you can test scenes without adding them to Build Settings, but when loading via `SceneManager.LoadScene()`, they must be in Build Settings.

## Step 1: Clean Up Battle Scene

### 1.1 Remove Testing Prefabs

**Answer to your question:** **YES, remove testing prefabs for party members.**

**Why:**
- Party members are now **dynamically created** from `CharacterState` data
- Testing prefabs will conflict with the dynamic creation system
- Only **enemy units** should remain in the scene (they're not created dynamically)

**What to do:**
1. **Open your Battle Scene**
2. **Find any testing/prefab party member Units** in the Hierarchy
3. **Delete them** (or disable them if you want to keep for reference)
4. **Keep enemy units** - these should remain as they are manually placed

**Example:**
- ❌ Delete: "TestPlayerUnit1", "TestPlayerUnit2", "PlayerPrefab_Instance"
- ✅ Keep: "EnemyGoblin1", "EnemyOrc1", etc.

### 1.2 Verify Enemy Units

1. **Check that enemy units have:**
   - `Unit` component attached
   - `Faction` set to `Enemy` (not `Player`)
   - `isPlayerControlled = false`
   - Proper grid positions assigned

2. **Enemy units should NOT be in PartyManager** - they're separate from the party system

## Step 2: Set Up BattleSceneInitializer

### 2.1 Create or Find BattleSceneInitializer GameObject

1. **In Battle Scene Hierarchy:**
   - Look for existing GameObject named **"BattleInitializer"** (or similar)
   - If it doesn't exist, create it:
     - Right-click in Hierarchy → `Create Empty`
     - Name it: **"BattleInitializer"**

2. **Add Component:**
   - Select "BattleInitializer"
   - `Add Component → Scripts → Core → Battle Scene Initializer`

### 2.2 Configure BattleSceneInitializer

In the Inspector, set the following:

1. **Unit Prefab:**
   - **Drag your Unit prefab** into the `Unit Prefab` field
   - This is the prefab that will be instantiated for each party member
   - **Important:** Make sure this prefab has:
     - `Unit` component attached
     - All necessary components (HPDisplay, etc.)
     - Proper setup for battle

2. **Auto-Position Settings:**
   - `Auto Position = true` (recommended for automatic placement)
   - `Auto Start X = 1` (starting X grid coordinate)
   - `Auto Start Y = 1` (starting Y grid coordinate)
   - Party members will be placed in a vertical line starting at (1, 1)

   **OR** for manual positioning:
   - `Auto Position = false`
   - Add positions to `Party Start Positions` list:
     - Click `+` to add entries
     - Set X and Y grid coordinates for each party member
     - Example: (1, 1), (1, 2), (1, 3) for 3 party members

3. **Party Start Positions:**
   - Only used if `Auto Position = false`
   - Leave empty if using auto-positioning

## Step 3: Set Up BattleEndHandler

### 3.1 Create BattleEndHandler GameObject

1. **In Battle Scene Hierarchy:**
   - Create empty GameObject: **"BattleEndHandler"**
   - (Or add component to existing GameObject like "BattleManager")

2. **Add Component:**
   - `Add Component → Scripts → Core → Battle End Handler`

### 3.2 Configure BattleEndHandler

In the Inspector:

1. **Exploration Scene Name:**
   - Set `Exploration Scene Name = "ExplorationScene"` (or your exploration scene name)

2. **Transition Delay:**
   - Set `Transition Delay = 2.0` (seconds to wait before returning to exploration)
   - Adjust as needed for victory/defeat animations

## Step 4: Verify GridManager

### 4.1 Check GridManager Exists

1. **In Battle Scene:**
   - Ensure `GridManager` exists in the scene
   - It should be a singleton or accessible via `ManagerRegistry`

2. **Verify Grid Setup:**
   - Grid should be properly initialized
   - Grid cells should exist at the positions you're using for party members
   - Check that positions (1, 1), (1, 2), etc. are valid grid cells

## Step 5: Set Up Exploration Scene Battle Trigger

### 5.1 Add BattleSceneLoader to Exploration Scene

1. **In Exploration Scene:**
   - Find or create a GameObject for battle management (e.g., "ExplorationManager")
   - `Add Component → Scripts → Core → Battle Scene Loader`

2. **Configure BattleSceneLoader:**
   - `Battle Scene Name = "BattleScene"` (or your battle scene name)
   - **Assign `Unit Prefab`** - same prefab as in BattleSceneInitializer

### 5.2 Wire Up Battle Trigger

**F2 Key (Already Implemented):**
- The `ExplorationController` script automatically handles F2 key press
- **No additional setup needed** - just press F2 in the exploration scene to trigger battle
- The script will automatically find `BattleSceneLoader` in the scene
- Make sure `BattleSceneLoader` component exists in the exploration scene (see Step 5.1)

**Other Options (for future use):**

1. **Button/UI Trigger:**
   ```csharp
   // In a button's OnClick event or script
   BattleSceneLoader loader = FindFirstObjectByType<BattleSceneLoader>();
   if (loader != null)
   {
       loader.LoadBattleScene();
   }
   ```

2. **Enemy Encounter:**
   - When player encounters an enemy in exploration
   - Call `LoadBattleScene()` to transition

3. **Custom Key Binding:**
   - Modify `ExplorationController.HandleBattleTrigger()` to use a different key
   - Or add additional key detection in `ReadInput()` method

## Step 6: Verify Unit Prefab Setup

### 6.1 Check Unit Prefab Components

Your Unit prefab should have:

1. **Unit Component:**
   - All stats properly configured (will be overridden by CharacterState)
   - `Faction` can be set to Player (will be set by UnitFactory)
   - `isPlayerControlled = true` (will be set by UnitFactory)

2. **Required Components:**
   - `HPDisplay` component (for HP bar display)
   - Any other battle-specific components

3. **Visual Representation:**
   - Model/sprite for the unit
   - Animator (if using animations)

### 6.2 Test Unit Prefab

1. **Drag Unit prefab into scene** (temporarily)
2. **Verify it has all components**
3. **Delete from scene** (we'll create them dynamically)

## Step 7: Testing Checklist

### 7.1 Pre-Testing Verification

Before testing, verify:

- [ ] Battle scene has `BattleSceneInitializer` with Unit prefab assigned
- [ ] Battle scene has `BattleEndHandler` with exploration scene name set
- [ ] Exploration scene has `BattleSceneLoader` with Unit prefab assigned
- [ ] Testing prefabs removed from battle scene (only enemies remain)
- [ ] GridManager exists and is properly set up
- [ ] Party is initialized in exploration scene (check Console logs)

### 7.2 Test Flow

1. **Start in Exploration Scene:**
   - Verify party is initialized
   - Check Console for "PartyManager: Added [character] to party" messages
   - Open Status Menu (TAB) and verify party members are displayed

2. **Trigger Battle:**
   - Use your battle trigger (button, encounter, etc.)
   - Check Console for:
     - "BattleSceneLoader: Exporting X party members to battle scene"
     - Scene should transition to battle scene

3. **In Battle Scene:**
   - Check Console for:
     - "BattleSceneInitializer: Initializing battle scene with X party members"
     - "BattleSceneInitializer: Created Unit [name] at grid (X, Y)"
   - **Verify:**
     - Party member Units appear on the grid
     - Units are at correct positions
     - Units have correct stats (check HP, stats match CharacterState)
     - Units are player-controlled (can select and move them)
     - Enemy units are present and separate

4. **During Battle:**
   - Test combat mechanics
   - Verify HP changes are reflected
   - Check that skills/equipment work correctly

5. **End Battle:**
   - Win or lose the battle
   - Check Console for:
     - "BattleEndHandler: Combat ended - Player Victory: true/false"
     - "ExplorationSceneLoader: Loading exploration scene"
   - Scene should transition back to exploration

6. **Back in Exploration:**
   - Verify party data persisted
   - Check that HP changes from battle are reflected
   - Open Status Menu and verify stats match battle results

## Step 8: Troubleshooting

### "No party members in battle scene"

**Symptoms:** Battle scene loads but no party member Units appear

**Solutions:**
1. Check Console for errors from `BattleSceneInitializer`
2. Verify `SceneTransitionData` has party data:
   - Check Console for "No party data found in SceneTransitionData"
   - Ensure `BattleSceneLoader` is called from exploration scene
3. Verify Unit prefab is assigned in `BattleSceneInitializer`
4. Check that GridManager exists and grid positions are valid
5. Verify party exists in exploration scene before triggering battle

### "Units have wrong stats"

**Symptoms:** Units in battle don't match CharacterState stats

**Solutions:**
1. Check Console for "UnitFactory: Created Unit..." messages
2. Verify `UpdateFromCharacterState` is being called (check Unit.cs)
3. Check that CharacterDefinition assets have correct base stats
4. Verify equipment is properly equipped in CharacterState
5. Check that level/XP are correct in CharacterState

### "Units are in wrong positions"

**Symptoms:** Party members spawn at incorrect grid positions

**Solutions:**
1. Check `autoPosition` and `autoStartX/Y` settings in `BattleSceneInitializer`
2. Verify grid positions are valid (check GridManager)
3. If using manual positioning, check `partyStartPositions` list
4. Verify GridManager is initialized before `BattleSceneInitializer.Start()`

### "Battle doesn't transition back to exploration"

**Symptoms:** Battle ends but doesn't return to exploration scene

**Solutions:**
1. Check that `BattleEndHandler` exists in battle scene
2. Verify `GameEvents.OnCombatEnded` is being raised (check combat system)
3. Check Console for "BattleEndHandler: Combat ended" message
4. Verify `explorationSceneName` is set correctly in `BattleEndHandler`
5. Check that `ExplorationSceneLoader` component exists (will be created automatically)

### "Party data lost after battle"

**Symptoms:** Party members disappear or reset after returning to exploration

**Solutions:**
1. Check Console for "ExplorationSceneLoader: UpdatePartyFromBattle" messages
2. Verify `Unit.ExportToCharacterState()` is working correctly
3. Check that `PartyManager` is persisting data (should be singleton with DontDestroyOnLoad)
4. Verify `SceneTransitionData` is not being destroyed incorrectly

## Step 9: Advanced Configuration

### 9.1 Custom Positioning

If you want more control over party positioning:

1. Set `autoPosition = false` in `BattleSceneInitializer`
2. Add custom positions to `partyStartPositions`:
   - For each party member, add a `Vector2Int` with grid coordinates
   - Example: (2, 1), (2, 3), (2, 5) for spaced-out positions

### 9.2 Multiple Battle Scenes

If you have multiple battle scenes:

1. Set `battleSceneName` in `BattleSceneLoader` to the specific scene
2. Or call `LoadBattleScene("SpecificBattleScene")` with scene name parameter
3. Ensure each battle scene has its own `BattleSceneInitializer` configured

### 9.3 Battle-Specific Setup

You can customize battle initialization by:

1. Modifying `BattleSceneInitializer.InitializeBattleScene()` for scene-specific logic
2. Adding additional setup after units are created
3. Using events/callbacks for custom initialization

## Summary

**Key Points:**
- ✅ Remove testing prefabs - party members are created dynamically
- ✅ Keep enemy units - they're manually placed
- ✅ Assign Unit prefab to both `BattleSceneInitializer` and `BattleSceneLoader`
- ✅ Set up `BattleEndHandler` for automatic return to exploration
- ✅ Verify GridManager exists and grid positions are valid
- ✅ Test the full flow: Exploration → Battle → Exploration

**Data Flow:**
- Exploration: `CharacterState` → `SceneTransitionData` → Battle
- Battle: `Unit` (created from `CharacterState`) → Battle gameplay
- Battle End: `Unit` → `CharacterState` (updated) → Exploration

The system automatically handles all data conversion and persistence. Just ensure the components are properly set up!
