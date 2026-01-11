# Battle Setup Quick Checklist

## ⚠️ CRITICAL FIRST STEP: Build Settings

- [ ] **Battle Scene added to Build Settings** (`File → Build Settings`)
- [ ] **Exploration Scene added to Build Settings** (if not already)
- [ ] Scene names match component settings (case-sensitive)

## Pre-Setup: Clean Battle Scene

- [ ] **Remove testing/prefab party member Units** from battle scene
- [ ] **Keep enemy units** (they're manually placed, not dynamic)
- [ ] Verify enemy units have `Faction = Enemy` and `isPlayerControlled = false`

## Battle Scene Setup

### BattleSceneInitializer
- [ ] GameObject "BattleInitializer" exists in battle scene
- [ ] `Battle Scene Initializer` component attached
- [ ] **Unit Prefab assigned** (your Unit prefab)
- [ ] `Auto Position = true` (or configure manual positions)
- [ ] `Auto Start X = 1`, `Auto Start Y = 1` (or custom positions)

### BattleEndHandler
- [ ] GameObject "BattleEndHandler" exists in battle scene
- [ ] `Battle End Handler` component attached
- [ ] `Exploration Scene Name = "ExplorationScene"` (or your scene name)
- [ ] `Transition Delay = 2.0` (or your preferred delay)

### GridManager
- [ ] GridManager exists in battle scene
- [ ] Grid is properly initialized
- [ ] Grid positions (1,1), (1,2), etc. are valid

## Exploration Scene Setup

### BattleSceneLoader
- [ ] GameObject with `Battle Scene Loader` component exists
- [ ] `Battle Scene Name = "BattleScene"` (or your scene name)
- [ ] **Unit Prefab assigned** (same as BattleSceneInitializer)
- [ ] **F2 key trigger is ready** (automatically handled by ExplorationController)

## Unit Prefab Setup

- [ ] Unit prefab has `Unit` component
- [ ] Unit prefab has `HPDisplay` component (or other required components)
- [ ] Unit prefab is properly configured for battle

## Testing

- [ ] Start in exploration scene - party initializes correctly
- [ ] **Press F2** - battle scene transitions
- [ ] Battle scene loads - party member Units appear at correct positions
- [ ] Units have correct stats matching CharacterState
- [ ] Battle gameplay works (movement, combat, etc.)
- [ ] End battle - scene transitions back to exploration
- [ ] Party data persists - stats match battle results

## Common Issues

- **No party members in battle?** → Check Unit prefab assignment, SceneTransitionData, Console errors
- **Wrong stats?** → Check CharacterState data, UpdateFromCharacterState method
- **Wrong positions?** → Check autoPosition settings, grid positions, GridManager
- **No return to exploration?** → Check BattleEndHandler, GameEvents.OnCombatEnded, Console logs
