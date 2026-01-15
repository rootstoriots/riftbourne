# Mixamo Character Model Setup Guide

This guide will walk you through replacing the player capsule with a Mixamo FBX character model and setting up animations for both exploration and battle modes.

## Table of Contents

1. [Downloading from Mixamo](#downloading-from-mixamo)
2. [Importing to Unity](#importing-to-unity)
3. [Setting Up the Model](#setting-up-the-model)
4. [Creating Animations](#creating-animations)
5. [Setting Up Exploration Mode](#setting-up-exploration-mode)
6. [Setting Up Battle Mode](#setting-up-battle-mode)
7. [Troubleshooting](#troubleshooting)

---

## Downloading from Mixamo

### Step 1: Choose a Character Model

1. Go to [Mixamo.com](https://www.mixamo.com/) (free Adobe account required)
2. Navigate to **Characters** in the top menu
3. Browse and select a character model (recommended: "Y Bot" or "Remy" for testing)
4. Click on the character to view details
5. Click **Download** button

### Step 2: Configure Download Settings

**Important Settings:**
- **Format**: FBX for Unity
- **Skin**: With Skin (required for animations)
- **Pose**: T-Pose (standard)
- **Format Options**:
  - **FPS**: 30 (standard for Unity)
  - **Keyframe Reduction**: None (for best quality)
  - **Format**: Binary FBX

Click **Download** to save the FBX file.

### Step 3: Download Animations

1. Navigate to **Animations** in the top menu
2. Search for and download these essential animations:
   - **Idle**: "Idle" or "Idle Breathing"
   - **Walk**: "Walking" or "Walking Forward"
   - **Run**: "Running" or "Running Forward"
   - **Sprint**: "Running" (can reuse run animation)
   - **Combat Idle**: "Combat Idle" (optional, for battle mode)
   - **Attack**: "Punching" or "Sword Slash" (optional, for battle mode)
   - **Hit Reaction**: "Hit Reaction" (optional, for battle mode)
   - **Death**: "Death" (optional, for battle mode)

**For each animation:**
- **Format**: FBX for Unity
- **Skin**: Without Skin (animations only, no mesh)
- **FPS**: 30
- **Keyframe Reduction**: None

---

## Importing to Unity

### Step 1: Create Folder Structure

1. In Unity, navigate to `Assets/`
2. Create the following folder structure:
   ```
   Assets/
   ├── Models/
   │   ├── Characters/
   │   │   ├── Player/
   │   │   │   ├── Model/
   │   │   │   └── Animations/
   ```

### Step 2: Import Character Model

1. Drag the character FBX file into `Assets/Models/Characters/Player/Model/`
2. Select the imported model in the Project window
3. In the Inspector, configure **Model** tab:
   - **Scale Factor**: 1 (or adjust if character is too large/small)
   - **Mesh Compression**: Off (for best quality)
   - **Read/Write Enabled**: ✓ (required for runtime modifications)
   - **Generate Colliders**: ✗ (we'll use CharacterController)
   - **Generate Lightmap UVs**: ✗ (not needed for player)
   - **Normals**: Import
   - **Tangents**: Calculate
   - **Swap UVs**: ✗
   - **Generate Colliders**: ✗

4. Configure **Rig** tab:
   - **Animation Type**: Humanoid (if available) or Generic
   - **Avatar Definition**: Create From This Model
   - **Optimize Game Objects**: ✓ (removes unnecessary bones)
   - **Root Node**: Auto (Unity will detect)

5. Configure **Materials** tab:
   - **Material Creation Mode**: Standard (Legacy) or Use External Materials
   - **Location**: Use Material Naming Convention

6. Click **Apply**

### Step 3: Import Animations

1. Drag all animation FBX files into `Assets/Models/Characters/Player/Animations/`
2. For each animation file:
   - Select the animation file
   - In Inspector, go to **Rig** tab:
     - **Animation Type**: Humanoid (or Generic, matching your model)
     - **Avatar Definition**: Copy From Other Avatar
     - **Source**: Select your character model's avatar
   - Go to **Animation** tab:
     - **Import Animation**: ✓
     - **Bake Animations**: ✓
     - **Root Transform Rotation** → **Bake Into Pose**: ✓ CHECKED
     - **Root Transform Position (Y)** → **Bake Into Pose**: ✓ CHECKED (prevents vertical movement)
     - **Root Transform Position (XZ)** → **Bake Into Pose**: ✓ CHECKED (prevents sliding)
     - **Loop Time**: ✓ (for idle, walk, run)
     - **Loop Pose**: ✓ (for seamless looping)
     - **Cycle Offset**: 0
     - **Additive Reference Pose**: None
   - Click **Apply**

---

## Setting Up the Model

### Step 1: Create Player Prefab

1. Drag the character model from Project window into the Scene (temporarily)
2. Position it at (0, 0, 0)
3. In Hierarchy, select the character GameObject
4. In Inspector, verify:
   - **Transform**: Position (0, 0, 0), Rotation (0, 0, 0), Scale (1, 1, 1)
   - **Animator** component should be present (added automatically)
   - **Skinned Mesh Renderer** component should be present

5. **Add Required Components:**
   - **Character Controller** (for exploration mode)
     - Center: (0, 1, 0)
     - Radius: 0.5
     - Height: 2
     - Skin Width: 0.08
     - Min Move Distance: 0.001
     - Slope Limit: 45
     - Step Offset: 0.3

6. **Remove or Disable Capsule Mesh:**
   - If the model has a visible capsule mesh child, disable or delete it
   - The CharacterController provides collision, not visual representation

7. **Create Prefab:**
   - Drag the GameObject from Hierarchy to `Assets/Prefabs/`
   - Name it `PlayerCharacter_Animated.prefab`
   - Delete the instance from the scene (we'll use the prefab)

### Step 2: Configure Animator Component

1. Select the `PlayerCharacter_Animated` prefab
2. In Inspector, find the **Animator** component
3. We'll create an Animator Controller in the next section, so leave **Controller** empty for now
4. Configure:
   - **Avatar**: Should reference the character's avatar
   - **Apply Root Motion**: ✗ (we handle movement via CharacterController)
   - **Update Mode**: Normal
   - **Culling Mode**: Always Animate

---

## Creating Animations

### Step 1: Create Animator Controller

1. In Project window, navigate to `Assets/Models/Characters/Player/Animations/`
2. Right-click → **Create → Animator Controller**
3. Name it `PlayerAnimatorController`
4. Double-click to open in Animator window

### Step 2: Add Animation States

1. In the Animator window, you'll see an empty state machine
2. **Create States:**
   - Right-click in empty space → **Create State → Empty**
   - Name it "Idle"
   - Right-click → **Create State → Empty**
   - Name it "Walk"
   - Right-click → **Create State → Empty**
   - Name it "Run"
   - Right-click → **Create State → Empty**
   - Name it "Sprint"

3. **Assign Animations to States:**
   - Select "Idle" state
   - In Inspector, set **Motion** to your idle animation clip
   - Select "Walk" state
   - In Inspector, set **Motion** to your walk animation clip
   - Select "Run" state
   - In Inspector, set **Motion** to your run animation clip
   - Select "Sprint" state
   - In Inspector, set **Motion** to your sprint animation clip (or reuse run)

### Step 3: Create Parameters

1. In Animator window, click **Parameters** tab (top left)
2. Click **+** button and add:
   - **Speed** (Float) - for movement speed
   - **IsSprinting** (Bool) - for sprint state
   - **IsGrounded** (Bool) - for ground check (optional)

### Step 4: Create Transitions

1. **Idle → Walk:**
   - Right-click "Idle" → **Make Transition** → Click "Walk"
   - Select the transition arrow
   - In Inspector:
     - **Conditions**: Speed > 0.1
     - **Has Exit Time**: ✗
     - **Transition Duration**: 0.1
     - **Interruption Source**: None

2. **Walk → Idle:**
   - Right-click "Walk" → **Make Transition** → Click "Idle"
   - Conditions: Speed < 0.1
   - Has Exit Time: ✗
   - Transition Duration: 0.1

3. **Walk → Run:**
   - Right-click "Walk" → **Make Transition** → Click "Run"
   - Conditions: Speed > 3.0 AND IsSprinting = false
   - Has Exit Time: ✗
   - Transition Duration: 0.15

4. **Run → Walk:**
   - Right-click "Run" → **Make Transition** → Click "Walk"
   - Conditions: Speed < 3.0 OR IsSprinting = false
   - Has Exit Time: ✗
   - Transition Duration: 0.15

5. **Run → Sprint:**
   - Right-click "Run" → **Make Transition** → Click "Sprint"
   - Conditions: IsSprinting = true
   - Has Exit Time: ✗
   - Transition Duration: 0.1

6. **Sprint → Run:**
   - Right-click "Sprint" → **Make Transition** → Click "Run"
   - Conditions: IsSprinting = false
   - Has Exit Time: ✗
   - Transition Duration: 0.1

7. **Set Default State:**
   - Right-click "Idle" → **Set as Layer Default State**

### Step 5: Assign Controller to Prefab

1. Select `PlayerCharacter_Animated` prefab
2. In Inspector, find **Animator** component
3. Set **Controller** to `PlayerAnimatorController`

---

## Setting Up Exploration Mode

### Step 1: Update ExplorationController Script

The `ExplorationController` already has animation event hooks. We need to connect them to the Animator.

1. Open `Assets/Scripts/Exploration/ExplorationController.cs`
2. Add these fields at the top of the class (after existing fields):

```csharp
[Header("Animation")]
[SerializeField] private Animator animator;

// In Awake() or Start(), add:
if (animator == null)
{
    animator = GetComponent<Animator>();
}
```

3. In the `Update()` method, after calculating movement, add:

```csharp
// Update animator parameters
if (animator != null)
{
    animator.SetFloat("Speed", currentSpeed);
    animator.SetBool("IsSprinting", isSprinting);
    animator.SetBool("IsGrounded", IsGrounded);
}
```

### Step 2: Replace Player in Exploration Scene

1. Open `ExplorationScene.unity`
2. Find the **Player** GameObject in Hierarchy
3. Select it
4. In Inspector:
   - **Remove** the Capsule (Mesh Renderer) component if present
   - **Disable** or delete any child objects that are visual capsules
5. **Add the Character Model:**
   - Drag `PlayerCharacter_Animated` prefab into the scene as a child of Player
   - OR: Replace the Player GameObject entirely:
     - Delete the current Player GameObject
     - Drag `PlayerCharacter_Animated` prefab into scene
     - Rename it to "Player"
     - Add `ExplorationController` component if not present
     - Add `CharacterController` component if not present
     - Configure both components as described in `EXPLORATION_SCENE_SETUP.md`

6. **Verify Setup:**
   - Player GameObject has `ExplorationController`
   - Player GameObject has `CharacterController`
   - Character model child has `Animator` with `PlayerAnimatorController` assigned
   - Animator component has the correct Avatar assigned

### Step 3: Test in Play Mode

1. Enter Play Mode
2. Use WASD to move
3. Hold Shift to sprint
4. Verify animations transition smoothly:
   - Idle when stationary
   - Walk when moving slowly
   - Run when moving faster
   - Sprint when holding Shift

---

## Setting Up Battle Mode

### Step 1: Create Battle Animator Controller (Optional)

If you want different animations for battle mode:

1. Create a new Animator Controller: `PlayerBattleAnimatorController`
2. Add states:
   - **Combat Idle** (if you have combat idle animation)
   - **Attack** (if you have attack animation)
   - **Hit Reaction** (if you have hit reaction)
   - **Death** (if you have death animation)
3. Create transitions between states
4. Add parameters: `IsAttacking`, `IsHit`, `IsDead`

### Step 2: Update Unit Prefab

1. Open `Assets/Prefabs/PlayerCharacter.prefab` (or your unit prefab)
2. If it's still a capsule, replace it:
   - Delete any capsule mesh components
   - Add the character model as a child
   - Add `Animator` component if not present
   - Assign `PlayerAnimatorController` or `PlayerBattleAnimatorController`

### Step 3: Update BattleSceneInitializer

The `BattleSceneInitializer` uses a unit prefab. Make sure your unit prefab has:
- Character model with Animator
- Unit component
- Proper setup for battle animations

### Step 4: Connect Battle Animations (Optional)

If you want to trigger animations during combat:

1. Open `Assets/Scripts/Characters/Unit.cs`
2. Add Animator reference and animation triggers:

```csharp
[Header("Animation")]
[SerializeField] private Animator animator;

private void Awake()
{
    if (animator == null)
    {
        animator = GetComponentInChildren<Animator>();
    }
}

// In attack methods, add:
if (animator != null)
{
    animator.SetTrigger("Attack");
}

// In take damage methods, add:
if (animator != null)
{
    animator.SetTrigger("Hit");
}

// In death methods, add:
if (animator != null)
{
    animator.SetBool("IsDead", true);
}
```

---

## Troubleshooting

### Quick Fixes for Common Issues

**If your character is sinking, not rotating, or moving continuously:**

1. **Select your character GameObject** (the one with ExplorationController)
2. **Find the Animator component** in the Inspector
3. **"Apply Root Motion" will show as "Handled by script"** - This is CORRECT and expected
   - The ExplorationController automatically disables root motion in code
   - You don't need to (and can't) change this setting manually
4. **Check child objects** - If your character model is a child GameObject:
   - Select the child GameObject with the Animator
   - If it also shows "Handled by script", that's fine
   - If it doesn't, make sure "Apply Root Motion" is UNCHECKED
5. **Re-import animations** if needed:
   - Select each animation file in Project window
   - In Inspector → Animation tab
   - **CHECK "Bake Into Pose"** for:
     - **Root Transform Position (XZ)**
     - **Root Transform Position (Y)**
     - **Root Transform Rotation**
   - Click Apply
6. **Check CharacterController settings**:
   - Center: (0, 1, 0) for a 2-unit tall character
   - Height: 2
   - Radius: 0.5

**If your IDLE animation is causing sinking/popping (and it happens with MULTIPLE animations):**

**This indicates the problem is NOT the animation, but the Animator/model setup.**

**DIAGNOSTIC CHECKLIST:**

1. **Check GameObject Hierarchy:**
   - Where is the Animator component? (Parent or child?)
   - Where is the CharacterController? (Should be on same GameObject as ExplorationController)
   - Where is the character model mesh? (Usually a child GameObject)
   
2. **Check for Multiple Animators:**
   - Select your Player GameObject
   - In Hierarchy, expand all children
   - Check if there are MULTIPLE Animator components
   - The ExplorationController should disable root motion on ALL of them (check Console for logs)

3. **Check Avatar Configuration:**
   - Select your character model FBX (not the animation)
   - In Inspector → **Rig** tab
   - **Animation Type** should be "Humanoid" or "Generic"
   - **Avatar Definition** should be "Create From This Model"
   - Click **Apply** if you made changes

4. **Check Model Import Settings:**
   - Select your character model FBX
   - In Inspector → **Model** tab
   - **Scale Factor**: Should be 1 (or appropriate for your model)
   - **Mesh Compression**: Off (for best quality)
   - **Read/Write Enabled**: ✓ CHECKED (required)

5. **Verify Animation Import (for ALL animations):**
   - Select each animation file (Idle, Walk, Run, etc.)
   - In Inspector → **Animation** tab
   - **CHECK "Bake Into Pose"** for:
     - Root Transform Position (Y)
     - Root Transform Position (XZ)  
     - Root Transform Rotation
   - Click **Apply** for each

6. **Check Console for Debug Messages:**
   - When you enter Play Mode, look for messages like:
     - `[ROOT MOTION] ExplorationController: Disabled root motion on Animator '...'`
   - This confirms the code is finding and disabling root motion

**COMMON ISSUES:**

- **Animator on child, CharacterController on parent**: This can cause conflicts. Move Animator to parent or ensure both are on same GameObject.
- **Avatar not configured**: Re-import the model with correct Avatar settings.
- **Model scale wrong**: Character might be wrong size, causing CharacterController to not align properly.

**CRITICAL FIX - If Animator is on Child GameObject:**

If your logs show the Animator is on a child GameObject (like `PlayerCharacter_Animated`) but your `ExplorationController` and `CharacterController` are on the parent (like `Player`), this can cause root motion conflicts.

**Solution:**
1. **Option A (Recommended)**: Move Animator to the parent GameObject
   - Select the parent GameObject (the one with ExplorationController)
   - Add Animator component to it
   - Assign the Animator Controller and Avatar
   - Remove Animator from child GameObject
   - Assign the character model mesh as a child (without Animator)

2. **Option B**: Move CharacterController to child GameObject
   - Select the child GameObject (the one with Animator)
   - Add CharacterController component to it
   - Add ExplorationController component to it
   - Remove these from parent GameObject
   - This makes the child GameObject the "Player"

**The key is: Animator, CharacterController, and ExplorationController should ALL be on the SAME GameObject.**

---

### Character Model Issues

**Problem: Character is too large/small**
- **Solution**: Adjust **Scale Factor** in model import settings, or scale the GameObject transform

**Problem: Character is floating or sinking**
- **Solution**: Adjust CharacterController **Center** and **Height** values, or adjust model's root position

**Problem: Character appears T-posed**
- **Solution**: Ensure Animator Controller is assigned and has a default state with an animation

**Problem: Character mesh is invisible**
- **Solution**: Check that Skinned Mesh Renderer is enabled and has materials assigned

### Animation Issues

**Problem: Animations don't play**
- **Solution**: 
  - Verify Animator Controller is assigned
  - Check that animation clips are assigned to states
  - Ensure default state is set
  - Verify parameters are being set correctly in code

**Problem: Animations are choppy or slow**
- **Solution**:
  - Check animation import settings (FPS should be 30)
  - Verify "Loop Time" is enabled for looping animations
  - Check that "Bake Animations" is enabled in import settings

**Problem: Character slides while animating**
- **Solution**: 
  - In animation import settings, **CHECK "Bake Into Pose"** for:
    - **Root Transform Position (XZ)** - This prevents horizontal sliding
    - **Root Transform Position (Y)** - This prevents vertical movement
    - **Root Transform Rotation** - This prevents unwanted rotation
  - Ensure "Apply Root Motion" is **UNCHECKED** in Animator component

**Problem: Character is sinking into ground then popping back up**
- **Solution**: 
  - **CRITICAL**: Ensure "Apply Root Motion" is **DISABLED** in the Animator component
  - Check CharacterController **Center** and **Height** values match your character model:
    - Center Y should be approximately half the character's height
    - Height should match your character's height (typically 1.8-2.0 for humanoid)
  - Verify the character model's pivot point is at the feet (not center)
  - If model pivot is wrong, adjust CharacterController **Center** Y value to compensate
  - Check that animation import has **Root Transform Position (XZ)** disabled

**Problem: Idle animation causes character to sink and pop repeatedly**
- **Solution**: 
  - **This is caused by root motion in the idle animation (breathing animations often have this)**
  - **CRITICAL**: The fix MUST be in animation import settings, NOT code workarounds
  - **Step 1**: Select your idle animation file in Project window
  - **Step 2**: In Inspector → **Animation** tab, find **Root Transform Rotation** and **Root Transform Position (Y)** sections
  - **Step 3**: **CHECK "Bake Into Pose"** for all:
    - **Root Transform Rotation** → **Bake Into Pose**: ✓ CHECKED
    - **Root Transform Position (Y)** → **Bake Into Pose**: ✓ CHECKED
    - **Root Transform Position (XZ)** → **Bake Into Pose**: ✓ CHECKED
  - **Step 4**: Click **Apply**
  - **Step 5**: Select your character GameObject → Animator component
    - **Note**: "Apply Root Motion" will show as "Handled by script" - this is CORRECT
    - The ExplorationController automatically disables root motion in code
  - **Step 6**: If character model is a child GameObject, check that child's Animator too
    - If it shows "Handled by script", that's fine
    - If not, ensure "Apply Root Motion" is UNCHECKED
  - **Note**: "Bake Into Pose" means the root motion is baked into the animation and won't affect the GameObject's transform

**Problem: Character is unable to turn/rotate**
- **Solution**: 
  - The ExplorationController now automatically rotates the character to face movement direction
  - If rotation is too slow, increase **Rotation Speed** in ExplorationController (default: 10)
  - If rotation is too fast, decrease **Rotation Speed**
  - Ensure the character model's forward direction is correct (usually +Z in Unity)
  - Check that the Animator component is not overriding rotation (Apply Root Motion should be disabled)

**Problem: Character continuously moves forward without input**
- **Solution**: 
  - **CRITICAL**: Disable "Apply Root Motion" in the Animator component
  - In animation import settings, disable **Root Transform Position (XZ)** for all movement animations
  - Check that the Speed parameter in Animator Controller transitions properly:
    - Idle → Walk: Speed > 0.1
    - Walk → Idle: Speed < 0.1
  - Verify that ExplorationController is properly resetting velocity when no input is detected
  - If using root motion animations, you may need to re-import them with root motion disabled

**Problem: Transitions are too slow/fast**
- **Solution**: Adjust **Transition Duration** in transition settings (lower = faster)

**Problem: Character rotates incorrectly**
- **Solution**: 
  - Check model import rotation settings
  - Verify CharacterController rotation is not being overridden
  - In animation import, **CHECK "Bake Into Pose"** for **Root Transform Rotation**
  - Ensure "Apply Root Motion" is **UNCHECKED** in Animator component

### Movement Issues

**Problem: Character doesn't move with animation**
- **Solution**: 
  - Ensure CharacterController is handling movement (not root motion)
  - Verify ExplorationController is updating Animator parameters
  - Check that Speed parameter is being set correctly

**Problem: Sprint animation doesn't trigger**
- **Solution**:
  - Verify IsSprinting parameter is being set in ExplorationController
  - Check transition conditions in Animator Controller
  - Ensure sprint animation is assigned to Sprint state

### Battle Mode Issues

**Problem: Units don't animate in battle**
- **Solution**:
  - Verify unit prefab has Animator component
  - Check that Animator Controller is assigned
  - Ensure Unit script is updating Animator parameters (if implemented)

**Problem: Battle animations conflict with exploration animations**
- **Solution**: Use separate Animator Controllers for exploration and battle, or use animation layers

---

## Quick Reference Checklist

### Exploration Mode Setup
- [ ] Character model imported and configured
- [ ] Animations imported and configured
- [ ] Animator Controller created with states and transitions
- [ ] Player prefab created with model and Animator
- [ ] ExplorationController updated to set Animator parameters
- [ ] Player GameObject in scene uses animated prefab
- [ ] CharacterController configured correctly
- [ ] Animations test successfully in Play Mode

### Battle Mode Setup
- [ ] Unit prefab updated with character model
- [ ] Battle Animator Controller created (optional)
- [ ] Unit script updated to trigger animations (optional)
- [ ] BattleSceneInitializer uses updated unit prefab
- [ ] Battle animations test successfully

### Common Settings
- [ ] Model Scale Factor: 1 (or adjusted)
- [ ] Animation FPS: 30
- [ ] Animation Type: Humanoid (or Generic)
- [ ] Apply Root Motion: **UNCHECKED** (CRITICAL - prevents sinking and unwanted movement)
- [ ] Loop Time: Enabled (for idle/walk/run)
- [ ] Root Transform Position (XZ) → **Bake Into Pose**: ✓ CHECKED (prevents sliding)
- [ ] Root Transform Position (Y) → **Bake Into Pose**: ✓ CHECKED (prevents sinking/popping)
- [ ] Root Transform Rotation → **Bake Into Pose**: ✓ CHECKED (prevents unwanted rotation)
- [ ] CharacterController Center and Height configured correctly
- [ ] Rotation Speed set in ExplorationController (default: 10)

---

## Additional Resources

- **Mixamo Website**: https://www.mixamo.com/
- **Unity Animation Documentation**: https://docs.unity3d.com/Manual/AnimationSection.html
- **Unity Animator Controller Guide**: https://docs.unity3d.com/Manual/class-AnimatorController.html
- **Character Controller Documentation**: https://docs.unity3d.com/Manual/class-CharacterController.html

---

## Notes

- **Performance**: Using Humanoid animation type allows for animation retargeting (using same animations on different models) but may have slightly higher overhead than Generic
- **Root Motion**: We disable root motion because we handle movement via CharacterController for precise control
- **Animation Layers**: For more complex setups (e.g., upper body attack animations while lower body walks), use Animation Layers in the Animator Controller
- **Animation Events**: You can add Animation Events to trigger sounds, effects, or gameplay events at specific frames

---

**Last Updated**: 2024
**Unity Version**: 6.0.3.2f1 LTS
