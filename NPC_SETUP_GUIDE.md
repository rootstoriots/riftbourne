# NPC and Waypoint System Setup Guide

This guide explains how to set up NPCs and waypoints in the exploration scene using the extensible behavior system.

## Prerequisites

- `NPCWaypoint.cs` script created
- `NPCController.cs` script created
- Behavior scripts in `Assets/Scripts/Exploration/Behaviors/`:
  - `NPCBehavior.cs` (base class)
  - `IdleBehavior.cs`
  - `WaypointPatrolBehavior.cs`
- `PlayerCharacter_Animated.prefab` exists (as reference)

## System Architecture

The NPC system uses a **Strategy Pattern** with behavior components:
- **NPCController**: Core movement and animation controller
- **NPCBehavior Components**: Define how NPCs act (Idle, WaypointPatrol, or custom)
- Each NPC can have multiple behavior components, but only one active at a time

## Step 1: Create NPC Prefab

1. In Unity Editor, open the Project window
2. Navigate to `Assets/Prefabs/`
3. Find `PlayerCharacter_Animated.prefab`
4. Right-click on it → **Duplicate**
5. Rename the duplicate to `NPCCharacter.prefab`
6. Select `NPCCharacter.prefab` to open it in Prefab Mode

### Configure NPC Prefab Components

1. **Remove Player-Specific Components:**
   - Select the root GameObject in Prefab Mode
   - In Inspector, find and **Remove** the `ExplorationController` component (if present)
   - The NPC should NOT have player input handling

2. **Add NPCController Component:**
   - With root GameObject selected, click **Add Component**
   - Search for "NPCController"
   - Add the component
   - Configure default settings:
     - **Current Behavior**: Leave empty (will be assigned per-instance)
     - **Rotation Speed**: 10
     - **Gravity**: -9.81
     - **Show Debug Info**: ✓ (optional, for testing)

3. **Verify CharacterController:**
   - Ensure `CharacterController` component is present
   - Settings should match player:
     - **Center**: (0, 1, 0)
     - **Radius**: 0.5
     - **Height**: 2
     - **Skin Width**: 0.08
     - **Min Move Distance**: 0.001
     - **Slope Limit**: 45
     - **Step Offset**: 0.3

4. **Verify Animator:**
   - Find the Animator component (may be on root or child GameObject)
   - Ensure **Controller** is set to `PlayerAnimatorController`
   - Ensure **Apply Root Motion** is unchecked (NPCController handles this)
   - Verify Avatar is assigned correctly

5. **Save Prefab:**
   - Click the arrow in the top-left to exit Prefab Mode
   - Click **Save** if prompted

## Step 2: Create Waypoints in Scene

1. Open your exploration scene (`ExplorationScene.unity` or similar)
2. In Hierarchy, create an empty GameObject (right-click → **Create Empty**)
3. Name it "Waypoint_1" (or similar descriptive name)
4. Position it where you want the NPC to patrol
5. With Waypoint_1 selected, click **Add Component**
6. Search for "NPCWaypoint" and add it
7. Configure waypoint settings:
   - **Wait Time**: 1 (seconds to wait at this waypoint)
   - **Speed Override**: 0 (use NPC's primary speed) or set a custom speed for segment TO this waypoint
   - **Show Gizmo**: ✓ (checked, for visualization)
   - **Gizmo Color**: Yellow (or your preference)

8. **Repeat for additional waypoints:**
   - Create more waypoint GameObjects
   - Position them to form a patrol path
   - Add `NPCWaypoint` component to each
   - Configure wait times and speed overrides as needed

### Waypoint Organization Tips

- Name waypoints descriptively: "Waypoint_Guard_Post_1", "Waypoint_Guard_Post_2", etc.
- Consider grouping waypoints under a parent GameObject for organization
- Use the gizmo visualization to see waypoints in Scene view
- Waypoints can be shared between multiple NPCs (same waypoint can be in multiple NPCs' lists)

## Step 3: Place NPCs in Scene

1. In Hierarchy, drag `NPCCharacter.prefab` into the scene
2. Position the NPC at the desired starting location
3. Rename the instance (e.g., "Guard_NPC_1", "Merchant_NPC", etc.)

### Configure NPC Instance

1. Select the NPC GameObject in Hierarchy
2. In Inspector, find the **NPCController** component

   **For Idle NPCs:**
   - Click **Add Component** → Search "IdleBehavior" → Add
   - In **NPCController**, drag **IdleBehavior** component to **Current Behavior** field
   - NPC will stay in place and play idle animation

   **For Patrolling NPCs:**
   - Click **Add Component** → Search "WaypointPatrolBehavior" → Add
   - Configure **WaypointPatrolBehavior**:
     - **Waypoints**: Set Size to number of waypoints
     - Drag waypoint GameObjects from Hierarchy into list slots
     - **Order matters!** NPCs visit waypoints in list order
     - **Primary Move Speed**: Default speed (e.g., 3)
     - **Waypoint Reach Distance**: 0.5
     - **Loop Waypoints**: ✓ (checked) to loop back to first waypoint
     - **Acceleration**: 10
   - In **NPCController**, drag **WaypointPatrolBehavior** component to **Current Behavior** field

### Speed Override Example

If you want an NPC to move slowly between waypoint 1 and 2, but faster between 2 and 3:

1. Waypoint_1: Speed Override = 0 (uses primary speed)
2. Waypoint_2: Speed Override = 2 (slow segment TO waypoint 2)
3. Waypoint_3: Speed Override = 5 (fast segment TO waypoint 3)

The speed override applies to the segment **leading TO** that waypoint (from the previous waypoint).

## Step 4: Test in Play Mode

1. Enter Play Mode
2. Verify NPC behavior:
   - **Idle NPCs**: Should play idle animation, no movement
   - **Patrolling NPCs**: Should move between waypoints, wait at each, then continue
3. Check animations:
   - NPC should transition between Idle and Walk animations based on movement
   - Animations should be smooth
4. Verify waypoint behavior:
   - NPC should stop at each waypoint for the specified wait time
   - NPC should rotate to face movement direction
   - If looping enabled, NPC should return to first waypoint after last

## Troubleshooting

### NPC doesn't move
- Check that **Behavior Mode** is set to "WaypointPatrol"
- Verify waypoints list is not empty
- Check that waypoints are assigned (not null)
- Ensure CharacterController is present and configured

### NPC moves but doesn't stop at waypoints
- Check **Waypoint Reach Distance** (should be around 0.5)
- Verify waypoint positions are reachable (not too high/low)
- Check that waypoints have `NPCWaypoint` component

### NPC animations not working
- Verify Animator component is present
- Check that Animator Controller is assigned (`PlayerAnimatorController`)
- Ensure Animator has correct Avatar assigned
- Check that root motion is disabled (NPCController handles this)

### NPC falls through ground
- Verify ground has a Collider
- Check CharacterController settings
- Ensure gravity is set correctly (-9.81)

### Waypoint gizmos not visible
- In Scene view, ensure **Gizmos** are enabled (top-right of Scene view)
- Check that waypoints have **Show Gizmo** enabled in NPCWaypoint component

## Advanced Usage

### Runtime Behavior Switching

You can change NPC behaviors at runtime:

```csharp
NPCController npcController = npcGameObject.GetComponent<NPCController>();

// Get behavior components
IdleBehavior idleBehavior = npcGameObject.GetComponent<IdleBehavior>();
WaypointPatrolBehavior patrolBehavior = npcGameObject.GetComponent<WaypointPatrolBehavior>();

// Switch behaviors
npcController.SetBehavior(idleBehavior);      // Switch to idle
npcController.SetBehavior(patrolBehavior);    // Switch to patrol
```

### Runtime Waypoint Management

You can modify waypoints at runtime using WaypointPatrolBehavior:

```csharp
WaypointPatrolBehavior patrol = npcController.GetBehavior<WaypointPatrolBehavior>();

if (patrol != null)
{
    // Add a waypoint
    patrol.AddWaypoint(newWaypoint);
    
    // Clear all waypoints
    patrol.ClearWaypoints();
}
```

### Sharing Waypoints

Multiple NPCs can share the same waypoint GameObjects. This is useful for:
- Creating patrol routes that multiple guards follow
- Having NPCs wait at common locations
- Coordinated NPC movement patterns

### Creating Custom Behaviors

See `NPC_SYSTEM_UNITY_IMPLEMENTATION_GUIDE.md` for detailed instructions on creating custom behaviors. The system is designed to be easily extensible - simply inherit from `NPCBehavior` and implement the required methods.

### Dynamic Waypoint Creation

You can create waypoints programmatically:

```csharp
GameObject waypointObj = new GameObject("DynamicWaypoint");
waypointObj.transform.position = new Vector3(10, 0, 10);
NPCWaypoint waypoint = waypointObj.AddComponent<NPCWaypoint>();
// Note: WaitTime and SpeedOverride are private fields, set via Inspector
// Or make them public properties if you need runtime access

WaypointPatrolBehavior patrol = npcController.GetBehavior<WaypointPatrolBehavior>();
if (patrol != null)
{
    patrol.AddWaypoint(waypoint);
}
```

## Next Steps

- Add more NPCs with different patrol routes
- Experiment with different wait times and speeds
- Create custom behaviors for unique NPC actions
- Consider adding interaction zones near NPCs for dialogue or trading
- See `NPC_SYSTEM_UNITY_IMPLEMENTATION_GUIDE.md` for advanced usage and custom behavior creation
