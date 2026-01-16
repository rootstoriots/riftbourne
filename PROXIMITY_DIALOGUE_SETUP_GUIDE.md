# Proximity Dialogue System Setup Guide

This guide explains how to set up and use the NPC proximity dialogue system in your exploration scenes.

## System Overview

The proximity dialogue system allows NPCs to automatically speak when the player gets within range. Features include:

- **Proximity Detection**: Dialogue triggers when player enters a configurable distance
- **Weighted Random Selection**: Multiple dialogue lines with different weights for variety
- **Queue System**: Prevents overlapping voices when multiple NPCs are nearby
- **Cooldown System**: Prevents dialogue spam when player re-enters range
- **Optional Audio**: Support for voice lines with AudioClip
- **Fade Animations**: Smooth fade in/out for dialogue text
- **World-Space UI**: Dialogue appears above NPCs' heads

## Prerequisites

- NPC prefab with `NPCController` component (see `NPC_SETUP_GUIDE.md`)
- Exploration scene with player character
- `ProximityDialogueManager` in the scene (auto-created if missing)

## Step 1: Create Dialogue Data Asset

1. In Unity Editor, right-click in Project window
2. Navigate to **Create** → **Riftbourne** → **Proximity Dialogue Data**
3. Name it (e.g., "GuardDialogue", "MerchantDialogue")
4. Select the asset in Inspector

### Configure Dialogue Entries

1. Set **Size** of `Dialogue Entries` array to the number of dialogue lines you want
2. For each entry:
   - **Dialogue Text**: The text to display above NPC (supports multi-line)
   - **Audio Clip**: (Optional) Voice line audio file
   - **Weight**: Probability weight (higher = more likely to be selected)
     - Example: Weight 2.0 is twice as likely as weight 1.0
   - **Display Duration**: How long text stays visible (0 = use default)
3. Set **Default Display Duration** (used if entry doesn't specify duration)

### Example Dialogue Setup

```
Dialogue Entry 0:
  Text: "Welcome to the town square!"
  Weight: 2.0
  Duration: 0 (uses default)

Dialogue Entry 1:
  Text: "Be careful out there, traveler."
  Weight: 1.0
  Duration: 4.0

Dialogue Entry 2:
  Text: "Have you seen the merchant? He's looking for you."
  Weight: 0.5
  Duration: 6.0
```

## Step 2: Add Proximity Dialogue to NPC

1. Select your NPC GameObject in the scene (or open NPC prefab)
2. In Inspector, click **Add Component**
3. Search for "ProximityDialogueComponent" and add it

### Configure Proximity Dialogue Component

1. **Dialogue Data**: Drag your dialogue data asset from Project window
2. **Proximity Distance**: Distance at which dialogue triggers (default: 5 units)
   - Use Scene view gizmo (cyan wireframe sphere) to visualize range
3. **Cooldown Period**: Seconds between dialogue plays (default: 30)
   - Prevents spam when player re-enters range
4. **Show Debug Logs**: Enable for testing (shows console messages)
5. **Show Gizmo**: Enable to see proximity range in Scene view

## Step 3: Add Proximity Dialogue Manager to Scene

The manager is auto-created if missing, but you can add it manually:

1. In Hierarchy, create empty GameObject (right-click → **Create Empty**)
2. Name it "ProximityDialogueManager"
3. Add Component → Search "ProximityDialogueManager" → Add
4. Configure settings:
   - **Max Queue Size**: Maximum dialogue requests in queue (default: 10)
   - **Show Debug Logs**: Enable for testing

## Step 4: Test in Play Mode

1. Enter Play Mode
2. Move player character near an NPC with proximity dialogue
3. When within range, dialogue should:
   - Appear above NPC's head
   - Fade in smoothly
   - Play audio (if assigned)
   - Display for configured duration
   - Fade out smoothly

### Testing Multiple NPCs

1. Place multiple NPCs with dialogue near each other
2. Move player into range of all NPCs simultaneously
3. Verify:
   - Only one dialogue plays at a time
   - Other NPCs are queued
   - Queue processes in order
   - No overlapping voices

### Testing Cooldown

1. Trigger dialogue from an NPC
2. Move player out of range
3. Wait less than cooldown period
4. Move player back into range
5. Verify: Dialogue does NOT trigger (cooldown active)
6. Wait for cooldown to expire
7. Move player back into range
8. Verify: Dialogue triggers again

## Troubleshooting

### Dialogue Doesn't Appear

**Checklist:**
- ✅ Dialogue data asset is assigned to ProximityDialogueComponent
- ✅ Dialogue entries array is not empty
- ✅ Dialogue entries have text (not empty strings)
- ✅ Player is within proximity distance
- ✅ Cooldown period has expired (if re-entering range)
- ✅ ProximityDialogueManager exists in scene

**Solution:**
- Enable "Show Debug Logs" on ProximityDialogueComponent
- Check Console for error messages
- Verify player detection (component should find ExplorationController)

### Multiple Dialogues Play Simultaneously

**Checklist:**
- ✅ ProximityDialogueManager exists in scene
- ✅ Only one manager instance (singleton)

**Solution:**
- Ensure ProximityDialogueManager is in scene
- Check Console for manager initialization messages

### Dialogue Text Not Visible

**Checklist:**
- ✅ Camera is facing NPC
- ✅ NPC is in camera view
- ✅ Canvas is created (check Hierarchy for "ProximityDialogueUI")

**Solution:**
- Dialogue UI uses world-space canvas that faces camera
- Ensure NPC is visible to main camera
- Check that CameraService exists (or Camera.main is set)

### Audio Not Playing

**Checklist:**
- ✅ Audio clip is assigned to dialogue entry
- ✅ Audio clip is valid (not null)
- ✅ Audio file format is supported by Unity

**Solution:**
- Verify audio clip assignment in dialogue data asset
- Check AudioSource component is created (in ProximityDialogueUI GameObject)
- Test audio clip in Unity's Audio preview

### Queue Not Working

**Checklist:**
- ✅ ProximityDialogueManager exists
- ✅ Max Queue Size is not 0
- ✅ Multiple NPCs are requesting dialogue

**Solution:**
- Enable "Show Debug Logs" on manager
- Check Console for queue messages
- Verify manager is processing queue correctly

## Advanced Usage

### Dynamic Dialogue Assignment

You can assign dialogue data at runtime:

```csharp
ProximityDialogueComponent dialogueComponent = npc.GetComponent<ProximityDialogueComponent>();
ProximityDialogueData newDialogue = Resources.Load<ProximityDialogueData>("Dialogue/NewDialogue");
// Note: You'll need to make dialogueData public or add a SetDialogueData method
```

### Adjusting Proximity Distance Per NPC

Different NPCs can have different trigger distances:

- **Merchant NPC**: Large distance (8-10 units) to catch player attention
- **Guard NPC**: Medium distance (5 units) for standard interaction
- **Secret NPC**: Small distance (2-3 units) for hidden dialogue

### Weighted Random Tips

- **Common dialogue**: Weight 2.0-5.0 (plays frequently)
- **Rare dialogue**: Weight 0.1-0.5 (plays occasionally)
- **Special dialogue**: Weight 0.01-0.1 (plays rarely, for special events)

### Audio Best Practices

- Keep audio clips short (2-10 seconds recommended)
- Use compressed formats (MP3, OGG) for smaller file sizes
- Match audio length to display duration
- Test audio volume levels (dialogue should be audible but not overpowering)

## Integration with Existing Systems

### NPCController Compatibility

- ProximityDialogueComponent works alongside NPCController
- No changes needed to NPCController
- Both components can be on the same NPC GameObject

### ExplorationController Integration

- Automatically detects player via ExplorationController component
- No manual player reference needed
- Works with any GameObject that has ExplorationController

### Camera System

- Uses CameraService if available (from Core namespace)
- Falls back to Camera.main if CameraService not found
- Dialogue UI automatically faces camera

## File Structure

Created files:
- `Assets/Scripts/Exploration/ProximityDialogueData.cs` - ScriptableObject data container
- `Assets/Scripts/Exploration/ProximityDialogueComponent.cs` - NPC component
- `Assets/Scripts/Exploration/ProximityDialogueManager.cs` - Singleton manager
- `Assets/Scripts/Exploration/ProximityDialogueUI.cs` - World-space UI component

## Next Steps

- Create dialogue data assets for your NPCs
- Add ProximityDialogueComponent to NPC prefabs
- Test in exploration scene
- Adjust proximity distances and cooldowns based on gameplay feel
- Add voice lines for important NPCs
- Experiment with weighted random values for dialogue variety

## Example: Complete NPC Setup

1. **Create Dialogue Data**:
   - Name: "GuardDialogue"
   - 3 dialogue entries with different weights
   - Default duration: 5 seconds

2. **Setup NPC**:
   - Add NPCController (with behavior)
   - Add ProximityDialogueComponent
   - Assign "GuardDialogue" asset
   - Set proximity distance: 6 units
   - Set cooldown: 45 seconds

3. **Test**:
   - Enter Play Mode
   - Move player near guard
   - Verify dialogue appears and plays correctly
