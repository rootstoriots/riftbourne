# ExplorationTest Scene Setup Instructions

This guide provides step-by-step instructions for manually setting up the ExplorationTest scene for 3D exploration movement testing.

## Prerequisites

- Unity Editor open
- PlayerInputActions.inputactions has been updated with Move, Look, and Sprint actions
- ExplorationController.cs and ExplorationCamera.cs scripts are in the project

## Step 1: Create New Scene

1. In Unity Editor, go to **File > New Scene**
2. Select **Basic (Built-in)** template
3. Click **Create**
4. Save the scene: **File > Save As**
5. Navigate to `Assets/Scenes/`
6. Name it `ExplorationTest.unity`
7. Click **Save**

## Step 2: Set Up Ground Plane

1. In Hierarchy, right-click and select **3D Object > Plane**
2. Rename it to "Ground"
3. In Inspector, set Transform:
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (10, 1, 10) - This creates a 100x100 unit ground plane
4. Optional: Add a Material to make it visible (create a new Material and assign a color)

## Step 3: Add Walls and Obstacles

### Create Walls

1. Right-click in Hierarchy, select **3D Object > Cube**
2. Rename to "Wall_1"
3. Set Transform:
   - Position: (0, 1, -20)
   - Scale: (20, 2, 1)
4. Duplicate this wall (Ctrl+D) and place additional walls:
   - Wall_2: Position (20, 1, 0), Scale (1, 2, 20)
   - Wall_3: Position (0, 1, 20), Scale (20, 2, 1)
   - Wall_4: Position (-20, 1, 0), Scale (1, 2, 20)

### Create Obstacles (Optional)

1. Create several Cube GameObjects for testing collision:
   - Obstacle_1: Position (5, 1, 5), Scale (2, 2, 2)
   - Obstacle_2: Position (-5, 1, -5), Scale (2, 2, 2)
   - Obstacle_3: Position (10, 1, -10), Scale (3, 3, 3)

**Note:** All walls and obstacles should have Collider components (added automatically with primitive shapes).

## Step 4: Create Player GameObject

1. Right-click in Hierarchy, select **3D Object > Capsule**
2. Rename to "Player"
3. Set Transform:
   - Position: (0, 1, 0) - Slightly above ground
   - Rotation: (0, 0, 0)
   - Scale: (1, 1, 1)

### Add CharacterController Component

1. Select Player GameObject
2. In Inspector, click **Add Component**
3. Search for "Character Controller"
4. Add the component
5. Configure CharacterController:
   - **Center**: (0, 1, 0)
   - **Radius**: 0.5
   - **Height**: 2
   - **Skin Width**: 0.08
   - **Min Move Distance**: 0.001
   - **Slope Limit**: 45
   - **Step Offset**: 0.3

### Add ExplorationController Script

1. With Player selected, click **Add Component**
2. Search for "ExplorationController"
3. Add the component
4. In Inspector, configure:
   - **Move Speed**: 5
   - **Sprint Multiplier**: 1.8
   - **Acceleration**: 10
   - **Enable Click To Move**: ✓ (checked)
   - **Click To Move Stop Distance**: 0.5
   - **Clickable Layers**: Everything (default)
   - **Gravity**: -9.81
   - **Ground Check Distance**: 0.1
   - **Camera Transform**: Leave empty (will be set automatically or assigned in next step)
   - **Show Debug Info**: ✓ (checked)

## Step 5: Set Up Camera

1. In Hierarchy, find the **Main Camera** (created by default)
2. Rename it to "ExplorationCamera"
3. Set Transform:
   - Position: (0, 5.5, -7) - Behind and above player (camera will auto-adjust)
   - Rotation: (25, 0, 0) - Looking down at player at a comfortable angle

### Add ExplorationCamera Script

1. Select ExplorationCamera
2. Click **Add Component**
3. Search for "ExplorationCamera"
4. Add the component
5. In Inspector, configure:
   - **Target**: Drag Player GameObject from Hierarchy
   - **Follow Distance**: 8 (default, can be adjusted)
   - **Min Follow Distance**: 3
   - **Max Follow Distance**: 15
   - **Height Offset**: 2
   - **Follow Smoothing**: 0.1
   - **Zoom Speed**: 2
   - **Rotation Sensitivity**: 0.5 (reduced from default for smoother control)
   - **Min Vertical Angle**: -30
   - **Max Vertical Angle**: 60
   - **Default Vertical Angle**: 25
   - **Require Right Mouse**: ✓ (checked)
   - **Use Collision Avoidance**: ✓ (checked)
   - **Collision Radius**: 0.3
   - **Collision Layers**: Everything (default)
   - **Show Debug Info**: ✓ (checked)

### Link Camera to Player Controller

1. Select Player GameObject
2. In ExplorationController component, find **Camera Transform** field
3. Drag ExplorationCamera from Hierarchy to this field

## Step 6: Set Up Lighting

1. In Hierarchy, find **Directional Light** (created by default)
2. Set Transform:
   - Rotation: (50, -30, 0) - Angled lighting
3. In Inspector, configure:
   - **Intensity**: 1
   - **Color**: White (or slightly warm)

## Step 7: Regenerate Input Actions C# Class

**Important:** After modifying PlayerInputActions.inputactions, Unity needs to regenerate the C# class.

1. Select `Assets/PlayerInputActions.inputactions` in Project window
2. In Inspector, you should see the Input Actions asset
3. Unity should automatically regenerate `PlayerInputActions.cs`
4. If it doesn't regenerate automatically:
   - Right-click the .inputactions file
   - Select **Reimport**
   - Or close and reopen Unity

## Step 8: Test the Scene

1. Press **Play** button
2. Test the following:
   - **WASD** keys move the player
   - **Left Shift** increases movement speed (sprint)
   - **Left Click** on ground/objects to move toward that location
   - **Right Mouse Button + Mouse Drag** rotates the camera
   - **Mouse Scroll Wheel** zooms in/out (like battle scene)
   - Player should stop when hitting walls (no clipping)
   - Player should automatically walk to clicked locations
   - WASD input cancels click-to-move
   - Camera should smoothly follow the player
   - Camera should avoid clipping through walls
   - Debug info should display in top-left corner

## Troubleshooting

### Player doesn't move
- Check that PlayerInputActions.inputactions has been regenerated
- Verify ExplorationController has CharacterController component
- Check Console for errors

### Camera doesn't rotate
- Verify right mouse button is being held while dragging
- Check that Look action is bound in PlayerInputActions
- Ensure ExplorationCamera has Target assigned

### Player falls through ground
- Verify Ground Plane has a Collider
- Check CharacterController settings
- Ensure Ground plane Y position is 0

### Camera clips through walls
- Enable "Use Collision Avoidance" in ExplorationCamera
- Adjust Collision Radius if needed
- Verify Collision Layers includes walls

## Optional Enhancements

- Add a skybox for better visual context
- Add more complex obstacles (stairs, ramps)
- Create a simple player model instead of capsule
- Add particle effects or visual feedback
- Create multiple test areas with different terrain

## Next Steps

Once the scene is set up and working:
- Test all movement directions
- Verify collision detection
- Test camera rotation limits
- Adjust movement speed and camera settings to taste
- Add animations (when ready) using the movement events
