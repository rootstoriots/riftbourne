# Configurable Battle System Setup Guide

## Overview

This guide explains how to set up and use the new configurable battle system that allows battles to be defined via `EncounterData` ScriptableObjects. The system supports variable arena sizes, positions, enemy configurations, hazards, and obstructions.

## System Components

### 1. EncounterData ScriptableObject
Defines a complete battle encounter with:
- Arena configuration (grid size, position, environment type)
- Enemy spawn definitions (prefab, position, level, faction)
- Player spawn positions
- Starting hazards
- Grid obstructions
- Battle parameters (victory conditions, turn limits)
- Victory rewards (optional)

### 2. CombatInitiator Component
Orchestrates battle setup from EncounterData:
- Generates grid at specified position/size
- Spawns enemies at defined positions
- Positions player party
- Creates starting hazards
- Sets up grid obstructions
- Initializes TurnManager

### 3. GridManager Updates
- Dynamic grid generation with configurable size and position
- Grid clearing and regeneration support
- Obstruction system with visual indicators

### 4. TurnManager Updates
- Dynamic initialization with `InitializeCombat(List<Unit>)` method
- Optional auto-initialization flag for backward compatibility

## Setup Instructions

### Step 1: Create Test EncounterData Assets

1. **Create Encounters Folder** (optional, for organization):
   - In Project window, navigate to `Assets/Resources/`
   - Right-click → `Create → Folder`
   - Name it: `Encounters`

2. **Create Small Skirmish Encounter**:
   - Right-click in `Assets/Resources/Encounters/` (or `Assets/Resources/`)
   - Select `Create → Riftbourne → Encounter Data`
   - Name it: `TestEncounter_SmallSkirmish`
   - Configure:
     - **Arena Configuration**:
       - Grid Width: `5`
       - Grid Height: `5`
       - Grid Origin Position: `(0, 0, 0)`
       - Environment Type: `Field`
     - **Enemy Configuration**:
       - Add 2 enemy spawns:
         - Enemy 1: Prefab = `EnemyUnit`, Position = `(4, 2)`, Level = `1`
         - Enemy 2: Prefab = `EnemyUnit`, Position = `(4, 3)`, Level = `1`
     - **Player Spawn Positions**:
       - Add 2-3 positions: `(0, 2)`, `(0, 3)`, `(0, 4)`
     - **Obstructed Tiles**: Leave empty for now
     - **Starting Hazards**: Leave empty for now

3. **Create Medium Battle Encounter**:
   - Create another Encounter Data asset
   - Name it: `TestEncounter_MediumBattle`
   - Configure:
     - **Arena Configuration**:
       - Grid Width: `8`
       - Grid Height: `8`
       - Grid Origin Position: `(0, 0, 0)`
       - Environment Type: `Forest`
     - **Enemy Configuration**:
       - Add 4 enemy spawns:
         - Enemy 1: `(7, 2)`
         - Enemy 2: `(7, 3)`
         - Enemy 3: `(7, 4)`
         - Enemy 4: `(7, 5)`
     - **Player Spawn Positions**:
       - Add 3-4 positions: `(0, 2)`, `(0, 3)`, `(0, 4)`, `(0, 5)`
     - **Obstructed Tiles**:
       - Add a few obstacles: `(4, 2)`, `(4, 3)`, `(4, 4)`
     - **Starting Hazards**:
       - Add 1-2 fire hazards (if you have HazardData assets):
         - Position: `(3, 3)`, Duration: `3`
         - Position: `(3, 4)`, Duration: `3`

4. **Create Hazard-Heavy Fight Encounter**:
   - Create another Encounter Data asset
   - Name it: `TestEncounter_HazardHeavy`
   - Configure:
     - **Arena Configuration**:
       - Grid Width: `10`
       - Grid Height: `10`
       - Grid Origin Position: `(0, 0, 0)`
       - Environment Type: `Cave`
     - **Enemy Configuration**:
       - Add 3 enemy spawns at various positions
     - **Player Spawn Positions**:
       - Add 3-4 positions near one edge
     - **Obstructed Tiles**:
       - Add multiple obstacles creating a maze-like pattern
     - **Starting Hazards**:
       - Add multiple hazards scattered across the battlefield

### Step 2: Set Up BattleScene

1. **Open BattleScene**:
   - Navigate to `Assets/Scenes/BattleScene.unity`
   - Open the scene

2. **Remove Manual Unit Placements** (if any):
   - In Hierarchy, find any manually placed player/enemy units
   - Delete or disable them (they will be spawned by CombatInitiator)

3. **Add CombatInitiator Component**:
   - In Hierarchy, find or create an empty GameObject
   - Name it: `CombatInitiator`
   - Select it
   - In Inspector, click `Add Component`
   - Search for `CombatInitiator`
   - Add the component

4. **Configure CombatInitiator**:
   - In Inspector, find the `CombatInitiator` component
   - **Test Encounter**: Drag one of your test EncounterData assets here
     - For testing, use `TestEncounter_SmallSkirmish`
   - **Default Enemy Prefab** (optional): Drag `EnemyUnit` prefab here if you want a fallback

5. **Verify Required Managers Exist**:
   - Ensure these GameObjects exist in the scene:
     - `GridManager` (with GridManager component)
     - `TurnManager` (with TurnManager component)
     - `PartyManager` (with PartyManager component)
     - `HazardManager` (with HazardManager component)
   - If any are missing, create empty GameObjects and add the components

6. **Configure TurnManager** (Important):
   - Select `TurnManager` GameObject
   - In Inspector, find `TurnManager` component
   - **Auto Initialize On Start**: Set to `false` (CombatInitiator will initialize combat)
   - This prevents TurnManager from auto-finding units before CombatInitiator spawns them

7. **Configure GridManager** (Important):
   - Select `GridManager` GameObject
   - In Inspector, find `GridManager` component
   - **Auto Generate On Start**: Set to `false` (CombatInitiator will generate grid)
   - This prevents GridManager from generating a default grid before CombatInitiator sets up the encounter grid

8. **Handle BattleSceneInitializer** (If present):
   - If `BattleSceneInitializer` exists in the scene, it will create player units from `SceneTransitionData`
   - `CombatInitiator` will generate the grid FIRST (so BattleSceneInitializer can use it), then reposition units at spawn positions from `EncounterData`
   - **Execution Order**: `CombatInitiator` generates grid early in a coroutine, then waits for `BattleSceneInitializer` to create units before completing setup
   - **Note**: For exploration->battle transitions, make sure `EncounterData` is passed through `SceneTransitionData` (see "Integration with Exploration Mode" below)

### Step 3: Set Up Player Party (For Testing)

**Important Note**: If `BattleSceneInitializer` exists in the scene, it will create player units from `SceneTransitionData` in its `Start()` method. `CombatInitiator` will then reposition those units at the spawn positions defined in `EncounterData`. This is the recommended setup for normal gameplay.

If you need a player party for testing:

1. **Option A: Use BattleSceneInitializer** (Recommended):
   - The existing `BattleSceneInitializer` will create party from `SceneTransitionData`
   - `CombatInitiator` will reposition them at spawn positions from `EncounterData`
   - **Note**: Make sure `BattleSceneInitializer` runs before `CombatInitiator` (check execution order in Project Settings → Script Execution Order if needed)

2. **Option B: Create Test Party Members** (For standalone testing):
   - If testing without `SceneTransitionData`, create test Unit GameObjects in the scene
   - Set their `Faction` to `Player`
   - Set `Is Player Controlled` to `true`
   - Register them with PartyManager (or they'll be found automatically)
   - `CombatInitiator` will position them at spawn positions from `EncounterData`

### Step 4: Test the System

1. **Enter Play Mode**:
   - Press Play in Unity Editor
   - CombatInitiator should:
     - Clear any existing grid
     - Generate new grid with encounter dimensions
     - Spawn enemies at specified positions
     - Position player party at spawn positions
     - Create starting hazards
     - Set up obstructions
     - Initialize TurnManager

2. **Verify Setup**:
   - Check Console for log messages from CombatInitiator
   - Verify grid is correct size and position
   - Verify enemies spawned at correct positions
   - Verify player units positioned correctly
   - Verify hazards appear (if any)
   - Verify obstructions are visible (dark grey cubes)
   - Verify combat starts (TurnManager initializes)

3. **Test Different Encounters**:
   - Stop Play Mode
   - Change `Test Encounter` field in CombatInitiator to a different EncounterData
   - Enter Play Mode again
   - Verify new encounter loads correctly

## Troubleshooting

### Issue: Grid doesn't generate
- **Check**: GridManager is in scene
- **Check**: CombatInitiator has valid EncounterData assigned
- **Check**: Console for error messages

### Issue: Enemies don't spawn
- **Check**: Enemy prefab has `Unit` component
- **Check**: Spawn positions are within grid bounds
- **Check**: Spawn positions aren't obstructed or occupied
- **Check**: Console for warnings about invalid positions

### Issue: Player units don't position
- **Check**: PartyManager has party members registered
- **Check**: EncounterData has player spawn positions defined
- **Check**: Spawn positions are within grid bounds
- **Check**: Console for warnings

### Issue: Hazards don't appear
- **Check**: HazardManager is in scene
- **Check**: HazardData assets are assigned in EncounterData
- **Check**: Hazard positions are within grid bounds

### Issue: Obstructions don't appear
- **Check**: EncounterData has obstructed tile positions defined
- **Check**: Positions are within grid bounds
- **Check**: GridManager.SetObstruction() is being called

### Issue: TurnManager doesn't initialize
- **Check**: TurnManager.Auto Initialize On Start is `false`
- **Check**: CombatInitiator is calling InitializeCombat()
- **Check**: At least one unit exists (player or enemy)
- **Check**: Console for error messages

### Issue: Combat doesn't start
- **Check**: TurnManager initialized successfully
- **Check**: Units are properly registered
- **Check**: At least one player unit and one enemy unit exist
- **Check**: Console for initialization messages

## Integration with Exploration Mode

### Triggering Battles from Exploration

To trigger battles from exploration mode (e.g., F2 key):

1. **Set up BattleSceneLoader in Exploration Scene**:
   - Add `BattleSceneLoader` component to a GameObject in the exploration scene
   - Configure `battleSceneName = "BattleScene"`

2. **Pass EncounterData when loading battle**:
   ```csharp
   // In your exploration trigger script
   BattleSceneLoader loader = FindFirstObjectByType<BattleSceneLoader>();
   EncounterData encounter = Resources.Load<EncounterData>("Encounters/MyEncounter");
   loader.LoadBattleScene("BattleScene", encounter);
   ```

3. **Configure CombatInitiator in BattleScene**:
   - Set `Load From Scene Transition Data = true` (default)
   - Set `Auto Initiate On Start = false` (will auto-initiate if encounter found in SceneTransitionData)
   - Leave `Encounter Data` field empty (will load from SceneTransitionData)

4. **Execution Flow**:
   - `BattleSceneLoader.LoadBattleScene()` stores `EncounterData` in `SceneTransitionData`
   - Battle scene loads
   - `CombatInitiator.Start()` loads `EncounterData` from `SceneTransitionData`
   - `CombatInitiator` generates grid FIRST (so `BattleSceneInitializer` can use it)
   - `BattleSceneInitializer.Start()` creates player units from `SceneTransitionData.PartyData`
   - `CombatInitiator` completes setup: positions units, spawns enemies, creates hazards, initializes combat

### Unity Execution Order

**Important**: Unity's `Start()` methods are called in the order GameObjects appear in the Hierarchy (top to bottom), but only after ALL `Awake()` methods complete.

**Current Setup**:
- `CombatInitiator` uses a coroutine to generate the grid EARLY (before other Start() methods complete)
- This ensures the grid exists when `BattleSceneInitializer` tries to create units
- Then `CombatInitiator` waits for units to be created before completing setup

**If you need to change execution order**:
- Go to `Edit → Project Settings → Script Execution Order`
- Add scripts and set their order (lower numbers execute first)
- Recommended: `CombatInitiator` should run before `BattleSceneInitializer`

## Advanced Usage

### Runtime Encounter Loading

To load encounters at runtime (e.g., from exploration scene):

```csharp
// In your battle trigger script
CombatInitiator initiator = FindFirstObjectByType<CombatInitiator>();
EncounterData encounter = Resources.Load<EncounterData>("Encounters/MyEncounter");
initiator.InitiateCombat(encounter);
```

### Multiple Encounters in Same Scene

You can test multiple encounters without restarting:

```csharp
// Clear and reload with different encounter
CombatInitiator initiator = FindFirstObjectByType<CombatInitiator>();
EncounterData newEncounter = Resources.Load<EncounterData>("Encounters/AnotherEncounter");
initiator.InitiateCombat(newEncounter);
```

### Custom Enemy Prefabs

1. Create enemy prefab with `Unit` component
2. Set default faction (non-player)
3. Configure stats, skills, equipment
4. Assign to `EnemySpawnDefinition.enemyPrefab` in EncounterData

### Custom Obstruction Visuals

The obstruction system uses simple dark grey cubes. To customize:

1. Modify `GridManager.CreateObstructionVisual()` method
2. Use custom prefabs instead of primitives
3. Store prefab reference in GridManager

## File Structure

```
Assets/
├── Scripts/
│   ├── Combat/
│   │   ├── EncounterData.cs (NEW)
│   │   ├── CombatInitiator.cs (NEW)
│   │   └── TurnManager.cs (MODIFIED)
│   └── Grid/
│       └── GridManager.cs (MODIFIED)
└── Resources/
    └── Encounters/ (CREATE THIS)
        ├── TestEncounter_SmallSkirmish.asset
        ├── TestEncounter_MediumBattle.asset
        └── TestEncounter_HazardHeavy.asset
```

## Next Steps

1. Create test EncounterData assets as described above
2. Set up BattleScene with CombatInitiator
3. Test each encounter type
4. Create more encounters for your game
5. Integrate with exploration scene triggers (future enhancement)

## Notes

- Grid origin position allows battles at different world locations
- Environment type is for future visual themes (not yet implemented)
- Level scaling for enemies is placeholder (future enhancement)
- Victory conditions are defined but not yet enforced (future enhancement)
- Turn limits are defined but not yet enforced (future enhancement)
- Victory rewards are defined but not yet distributed (future enhancement)
