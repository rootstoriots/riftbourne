# NPC System Unity Implementation Guide

This comprehensive guide covers everything you need to know to implement and extend the NPC system in Unity.

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture](#architecture)
3. [Initial Setup](#initial-setup)
4. [Creating NPCs](#creating-npcs)
5. [Setting Up Waypoints](#setting-up-waypoints)
6. [Assigning Behaviors](#assigning-behaviors)
7. [Creating Custom Behaviors](#creating-custom-behaviors)
8. [Advanced Usage](#advanced-usage)
9. [Troubleshooting](#troubleshooting)
10. [Best Practices](#best-practices)

---

## System Overview

The NPC system uses a **Strategy Pattern** architecture, making it easy to add new behaviors without modifying existing code. Each NPC has:

- **NPCController**: Core movement and animation controller
- **NPCBehavior Component**: Defines how the NPC acts (Idle, WaypointPatrol, or custom)
- **CharacterController**: Unity's built-in character movement component
- **Animator**: Handles character animations

### Key Features

- ✅ **Extensible**: Easy to add new behaviors by creating new classes
- ✅ **Modular**: Behaviors are separate components, easy to swap
- ✅ **Configurable**: Each behavior has its own settings
- ✅ **Reusable**: Same behavior can be used by multiple NPCs
- ✅ **Runtime Swappable**: Change behaviors at runtime via code

---

## Architecture

### Component Hierarchy

```
NPC GameObject
├── CharacterController (Unity component)
├── Animator (Unity component)
├── NPCController (our script)
└── NPCBehavior Component (IdleBehavior, WaypointPatrolBehavior, or custom)
```

### Behavior System Flow

```
NPCController (Update loop)
    ↓
Calls currentBehavior.UpdateBehavior()
    ↓
Behavior updates velocity and speed
    ↓
NPCController applies movement and animations
```

### Available Behaviors

1. **IdleBehavior**: NPC stays in place, plays idle animation
2. **WaypointPatrolBehavior**: NPC patrols between waypoints with configurable speeds and wait times
3. **Custom Behaviors**: Create your own by inheriting from `NPCBehavior`

---

## Initial Setup

### Step 1: Verify Scripts Are Imported

1. Open Unity Editor
2. Check that these scripts exist in `Assets/Scripts/Exploration/`:
   - `NPCController.cs`
   - `NPCWaypoint.cs`
3. Check that these scripts exist in `Assets/Scripts/Exploration/Behaviors/`:
   - `NPCBehavior.cs` (base class)
   - `IdleBehavior.cs`
   - `WaypointPatrolBehavior.cs`

If any are missing, ensure they're imported into your project.

### Step 2: Create NPC Prefab

1. In Project window, navigate to `Assets/Prefabs/`
2. Find `PlayerCharacter_Animated.prefab`
3. **Right-click** → **Duplicate**
4. Rename duplicate to `NPCCharacter.prefab`
5. **Double-click** `NPCCharacter.prefab` to open in Prefab Mode

#### Configure NPC Prefab Components

1. **Select root GameObject** in Prefab Mode
2. **Remove** `ExplorationController` component (if present)
3. **Add Component** → Search "NPCController" → Add
4. **Verify CharacterController** is present (should already be there)
5. **Verify Animator** is present and has:
   - Controller: `PlayerAnimatorController`
   - Apply Root Motion: **Unchecked**
6. **Exit Prefab Mode** and **Save**

---

## Creating NPCs

### Method 1: Using the Prefab (Recommended)

1. Open your exploration scene
2. In Project window, find `NPCCharacter.prefab`
3. **Drag** prefab into Hierarchy
4. **Position** NPC at desired location
5. **Rename** instance (e.g., "Guard_NPC", "Merchant_NPC")

### Method 2: Manual Setup

1. In Hierarchy, **right-click** → **Create Empty**
2. Name it (e.g., "NPC_Guard")
3. **Add Component** → **Character Controller**
   - Configure: Center (0, 1, 0), Radius 0.5, Height 2
4. **Add Component** → **NPCController**
5. **Add Component** → **Animator**
   - Assign `PlayerAnimatorController`
6. Add character model as child (if using separate model)

---

## Setting Up Waypoints

### Creating Waypoint GameObjects

1. In Hierarchy, **right-click** → **Create Empty**
2. Name it descriptively (e.g., "Waypoint_Guard_Post_1")
3. **Position** waypoint where NPC should patrol
4. **Add Component** → Search "NPCWaypoint" → Add

### Configuring Waypoint Settings

Select the waypoint GameObject and configure in Inspector:

- **Wait Time**: Seconds to wait at this waypoint (default: 1)
- **Speed Override**: Custom speed for segment TO this waypoint
  - `0` = Use NPC's primary speed
  - `> 0` = Use this speed for the segment leading TO this waypoint
- **Show Gizmo**: Enable to see waypoint in Scene view
- **Gizmo Color**: Color for waypoint visualization

### Waypoint Organization Tips

- **Name clearly**: "Waypoint_Guard_Post_1", "Waypoint_Guard_Post_2"
- **Group waypoints**: Create empty parent GameObject to organize
- **Use gizmos**: Enable "Show Gizmo" to visualize patrol routes
- **Share waypoints**: Multiple NPCs can use the same waypoint GameObjects

### Example: Creating a Patrol Route

1. Create 4 waypoints in a square pattern:
   - Waypoint_North
   - Waypoint_East
   - Waypoint_South
   - Waypoint_West
2. Position them to form a patrol route
3. Configure wait times (e.g., 2 seconds at each)
4. Optionally set speed overrides for different segments

---

## Assigning Behaviors

### Assigning Idle Behavior

1. Select NPC GameObject in Hierarchy
2. In Inspector, find **NPCController** component
3. **Add Component** → Search "IdleBehavior" → Add
4. In **NPCController**, drag **IdleBehavior** component to **Current Behavior** field

**Result**: NPC will stay in place and play idle animation.

### Assigning Waypoint Patrol Behavior

1. Select NPC GameObject
2. **Add Component** → Search "WaypointPatrolBehavior" → Add
3. Configure **WaypointPatrolBehavior**:
   - **Waypoints**: Set Size to number of waypoints
   - **Drag waypoint GameObjects** from Hierarchy into list slots
   - **Primary Move Speed**: Default speed (e.g., 3)
   - **Waypoint Reach Distance**: Distance to consider waypoint reached (0.5)
   - **Loop Waypoints**: ✓ Checked to loop back to first waypoint
   - **Acceleration**: Rate of speed change (10)
4. In **NPCController**, drag **WaypointPatrolBehavior** component to **Current Behavior** field

**Result**: NPC will patrol between waypoints, wait at each, then continue.

### Behavior Assignment Tips

- **Only one behavior active**: NPCController uses the behavior in "Current Behavior" field
- **Order matters**: For WaypointPatrolBehavior, waypoints are visited in list order
- **Runtime switching**: You can change behaviors via code (see Advanced Usage)

---

## Creating Custom Behaviors

### Step 1: Create New Behavior Script

1. In Project window, navigate to `Assets/Scripts/Exploration/Behaviors/`
2. **Right-click** → **Create** → **C# Script**
3. Name it (e.g., "FollowPlayerBehavior")
4. **Double-click** to open in code editor

### Step 2: Implement Behavior Class

Replace the default code with:

```csharp
using UnityEngine;
using Riftbourne.Exploration.Behaviors;

namespace Riftbourne.Exploration.Behaviors
{
    /// <summary>
    /// Example custom behavior: NPC follows the player.
    /// </summary>
    public class FollowPlayerBehavior : NPCBehavior
    {
        [Header("Follow Settings")]
        [Tooltip("Speed at which NPC follows player.")]
        [SerializeField] private float followSpeed = 3f;
        
        [Tooltip("Distance to maintain from player.")]
        [SerializeField] private float followDistance = 2f;
        
        [Tooltip("Acceleration rate.")]
        [SerializeField] private float acceleration = 10f;
        
        private Transform playerTransform;
        
        public override void Initialize(NPCController controller, CharacterController charController, Animator anim)
        {
            base.Initialize(controller, charController, anim);
            
            // Find player (you may need to adjust this based on your setup)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning($"[FollowPlayerBehavior] {npcController?.gameObject.name ?? "NPC"}: Player not found!");
            }
        }
        
        public override void UpdateBehavior(ref Vector3 velocity, ref float currentSpeed, float deltaTime)
        {
            if (playerTransform == null)
            {
                // No player to follow, stop
                velocity.x = Mathf.Lerp(velocity.x, 0f, acceleration * deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, 0f, acceleration * deltaTime);
                currentSpeed = 0f;
                return;
            }
            
            Vector3 npcPosition = npcController.transform.position;
            Vector3 playerPosition = playerTransform.position;
            
            // Calculate direction to player (horizontal only)
            Vector3 direction = playerPosition - npcPosition;
            direction.y = 0f;
            
            float distanceToPlayer = direction.magnitude;
            
            // If too close, stop
            if (distanceToPlayer <= followDistance)
            {
                velocity.x = Mathf.Lerp(velocity.x, 0f, acceleration * deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, 0f, acceleration * deltaTime);
                currentSpeed = 0f;
                return;
            }
            
            // Move toward player
            direction.Normalize();
            currentSpeed = Mathf.Lerp(currentSpeed, followSpeed, acceleration * deltaTime);
            velocity.x = direction.x * currentSpeed;
            velocity.z = direction.z * currentSpeed;
        }
        
        public override string GetBehaviorName()
        {
            return "Follow Player";
        }
    }
}
```

### Step 3: Save and Use

1. **Save** the script
2. Return to Unity (it will compile automatically)
3. Select NPC GameObject
4. **Add Component** → Search your new behavior name → Add
5. Configure settings in Inspector
6. In **NPCController**, assign to **Current Behavior** field

### Behavior Implementation Guidelines

#### Required Methods

- **`UpdateBehavior(ref Vector3 velocity, ref float currentSpeed, float deltaTime)`**
  - Called every frame
  - Modify `velocity.x` and `velocity.z` for horizontal movement
  - Modify `currentSpeed` for speed value
  - Do NOT modify `velocity.y` (handled by NPCController)

- **`GetBehaviorName()`**
  - Return display name for UI/debugging

#### Optional Methods

- **`Initialize(...)`**: Called when behavior is assigned
- **`OnBehaviorActivated()`**: Called when behavior becomes active
- **`OnBehaviorDeactivated()`**: Called when behavior is switched away from

#### Important Notes

- **Don't modify velocity.y**: NPCController handles gravity
- **Use deltaTime**: Always use the provided `deltaTime` parameter
- **Access NPCController**: Use `npcController` field for NPC position, etc.
- **Coroutines**: Use `npcController.StartCoroutine()` for coroutines

### Example: Random Wander Behavior

```csharp
using UnityEngine;
using Riftbourne.Exploration.Behaviors;

namespace Riftbourne.Exploration.Behaviors
{
    public class RandomWanderBehavior : NPCBehavior
    {
        [Header("Wander Settings")]
        [SerializeField] private float wanderSpeed = 2f;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float minWaitTime = 1f;
        [SerializeField] private float maxWaitTime = 3f;
        [SerializeField] private float reachDistance = 0.5f;
        
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private bool isWaiting = false;
        private float waitEndTime;
        
        public override void Initialize(NPCController controller, CharacterController charController, Animator anim)
        {
            base.Initialize(controller, charController, anim);
            startPosition = npcController.transform.position;
            PickNewTarget();
        }
        
        public override void UpdateBehavior(ref Vector3 velocity, ref float currentSpeed, float deltaTime)
        {
            // Check if waiting
            if (isWaiting)
            {
                if (Time.time >= waitEndTime)
                {
                    isWaiting = false;
                    PickNewTarget();
                }
                else
                {
                    velocity.x = 0f;
                    velocity.z = 0f;
                    currentSpeed = 0f;
                    return;
                }
            }
            
            // Move toward target
            Vector3 npcPos = npcController.transform.position;
            Vector3 direction = targetPosition - npcPos;
            direction.y = 0f;
            
            float distance = direction.magnitude;
            
            if (distance <= reachDistance)
            {
                // Reached target, start waiting
                isWaiting = true;
                waitEndTime = Time.time + Random.Range(minWaitTime, maxWaitTime);
                velocity.x = 0f;
                velocity.z = 0f;
                currentSpeed = 0f;
                return;
            }
            
            direction.Normalize();
            currentSpeed = wanderSpeed;
            velocity.x = direction.x * currentSpeed;
            velocity.z = direction.z * currentSpeed;
        }
        
        private void PickNewTarget()
        {
            // Pick random position within wander radius
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            targetPosition = startPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
        
        public override string GetBehaviorName()
        {
            return "Random Wander";
        }
    }
}
```

---

## Advanced Usage

### Runtime Behavior Switching

```csharp
// Get NPCController reference
NPCController npcController = npcGameObject.GetComponent<NPCController>();

// Get existing behavior component
WaypointPatrolBehavior patrolBehavior = npcGameObject.GetComponent<WaypointPatrolBehavior>();

// Switch to patrol behavior
npcController.SetBehavior(patrolBehavior);

// Or get behavior by type
IdleBehavior idleBehavior = npcGameObject.GetComponent<IdleBehavior>();
npcController.SetBehavior(idleBehavior);
```

### Accessing Behavior-Specific Data

```csharp
// Get WaypointPatrolBehavior
WaypointPatrolBehavior patrol = npcController.GetBehavior<WaypointPatrolBehavior>();

if (patrol != null)
{
    int currentWaypoint = patrol.GetCurrentWaypointIndex();
    int totalWaypoints = patrol.GetWaypointCount();
    bool isWaiting = patrol.IsWaitingAtWaypoint();
    
    // Add waypoint at runtime
    NPCWaypoint newWaypoint = // ... get waypoint reference
    patrol.AddWaypoint(newWaypoint);
}
```

### Creating Behaviors Programmatically

```csharp
// Add behavior component at runtime
FollowPlayerBehavior followBehavior = npcGameObject.AddComponent<FollowPlayerBehavior>();
followBehavior.followSpeed = 4f;
followBehavior.followDistance = 3f;

// Assign to NPCController
npcController.SetBehavior(followBehavior);
```

### Multiple Behavior Components

You can have multiple behavior components on the same NPC and switch between them:

```csharp
// NPC has both IdleBehavior and WaypointPatrolBehavior components
IdleBehavior idle = npcGameObject.GetComponent<IdleBehavior>();
WaypointPatrolBehavior patrol = npcGameObject.GetComponent<WaypointPatrolBehavior>();

// Switch based on game state
if (playerIsNearby)
{
    npcController.SetBehavior(patrol); // Start patrolling
}
else
{
    npcController.SetBehavior(idle); // Go idle
}
```

### Behavior State Management

Behaviors can maintain their own state:

```csharp
public class GuardBehavior : NPCBehavior
{
    private bool isAlerted = false;
    private float alertTime = 0f;
    
    public override void OnBehaviorActivated()
    {
        // Reset state when activated
        isAlerted = false;
        alertTime = 0f;
    }
    
    public void SetAlerted(bool alerted)
    {
        isAlerted = alerted;
        if (alerted)
        {
            alertTime = Time.time;
        }
    }
}
```

---

## Troubleshooting

### NPC Doesn't Move

**Checklist:**
- ✅ Behavior component is assigned to NPCController's "Current Behavior" field
- ✅ CharacterController component is present
- ✅ Behavior component is enabled (checkbox in Inspector)
- ✅ No errors in Console

**Solution:**
- Ensure behavior is assigned in NPCController
- Check that behavior's `UpdateBehavior` method is modifying velocity

### NPC Falls Through Ground

**Checklist:**
- ✅ Ground has a Collider component
- ✅ CharacterController is configured correctly
- ✅ Gravity is set to -9.81 in NPCController

**Solution:**
- Add Collider to ground
- Verify CharacterController settings match player character

### Animations Not Working

**Checklist:**
- ✅ Animator component is present
- ✅ Animator Controller is assigned (`PlayerAnimatorController`)
- ✅ Avatar is assigned in Animator
- ✅ Root Motion is disabled

**Solution:**
- Assign Animator Controller
- Ensure Avatar matches character model
- Verify root motion is unchecked

### Waypoint Patrol Not Working

**Checklist:**
- ✅ WaypointPatrolBehavior component is present
- ✅ Waypoints list is not empty
- ✅ Waypoints are assigned (not null)
- ✅ Waypoint GameObjects have NPCWaypoint component
- ✅ Behavior is assigned to NPCController

**Solution:**
- Add waypoints to WaypointPatrolBehavior's waypoints list
- Ensure waypoints have NPCWaypoint component
- Check waypoint positions are reachable

### Behavior Not Found in Add Component Menu

**Solution:**
- Ensure script is in `Assets/Scripts/Exploration/Behaviors/` folder
- Check script compiles without errors
- Verify script inherits from `NPCBehavior`
- Refresh Unity (Assets → Refresh)

---

## Best Practices

### 1. Behavior Organization

- **Keep behaviors focused**: Each behavior should do one thing well
- **Reuse behaviors**: Don't duplicate, reuse existing behaviors
- **Name clearly**: Use descriptive names (e.g., `MerchantIdleBehavior`)

### 2. Waypoint Management

- **Name waypoints**: Use clear, descriptive names
- **Group waypoints**: Use parent GameObjects to organize
- **Share waypoints**: Multiple NPCs can use same waypoint GameObjects
- **Test routes**: Verify patrol routes in Scene view using gizmos

### 3. Performance

- **Limit active NPCs**: Too many NPCs can impact performance
- **Use object pooling**: For NPCs that spawn/despawn frequently
- **Optimize behaviors**: Keep `UpdateBehavior` methods efficient

### 4. Code Organization

- **Namespace**: Keep behaviors in `Riftbourne.Exploration.Behaviors`
- **Documentation**: Add XML comments to custom behaviors
- **Settings**: Expose important values as SerializeField for Inspector

### 5. Testing

- **Test in isolation**: Test each behavior separately
- **Test combinations**: Verify behavior switching works
- **Test edge cases**: Empty waypoint lists, null references, etc.

---

## Example: Complete NPC Setup

### Scenario: Guard NPC with Patrol Route

1. **Create Waypoints:**
   - Waypoint_Guard_Start (position: 0, 0, 0)
   - Waypoint_Guard_Mid (position: 5, 0, 0)
   - Waypoint_Guard_End (position: 10, 0, 0)

2. **Configure Waypoints:**
   - All waypoints: Wait Time = 2 seconds
   - Waypoint_Guard_Mid: Speed Override = 5 (faster segment)

3. **Create NPC:**
   - Drag `NPCCharacter.prefab` into scene
   - Position at Waypoint_Guard_Start
   - Rename to "Guard_NPC"

4. **Add Behavior:**
   - Add Component → WaypointPatrolBehavior
   - Set Waypoints list Size = 3
   - Drag waypoints into list (in order: Start, Mid, End)
   - Set Primary Move Speed = 3
   - Set Loop Waypoints = ✓

5. **Assign Behavior:**
   - In NPCController, drag WaypointPatrolBehavior to Current Behavior

6. **Test:**
   - Enter Play Mode
   - Verify guard patrols: Start → Mid → End → Start (loop)

---

## Summary

The NPC system is designed to be:

- **Easy to use**: Simple component-based setup
- **Extensible**: Add new behaviors by creating new classes
- **Flexible**: Switch behaviors at runtime
- **Maintainable**: Clear separation of concerns

For questions or issues, refer to the troubleshooting section or check the code comments in the behavior scripts.
