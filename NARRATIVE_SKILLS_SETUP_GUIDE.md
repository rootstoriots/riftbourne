# Narrative Skills System Setup Guide

This guide walks you through manually setting up the UI and testing the narrative skills system in Unity.

## Prerequisites

- All scripts have been created (they should be in your project)
- Unity Editor is open
- You have an ExplorationTest scene (or any exploration scene)

## Part 1: Set Up Journal UI

### Step 1.1: Create Journal Canvas

1. In your ExplorationTest scene (or exploration scene), right-click in Hierarchy
2. Select **UI > Canvas**
3. Rename it to "JournalCanvas"
4. In Inspector, set Canvas settings:
   - **Render Mode**: Screen Space - Overlay
   - **Sort Order**: 10 (so it appears above other UI)

### Step 1.2: Create Journal Panel

1. Right-click on JournalCanvas, select **UI > Panel**
2. Rename to "JournalPanel"
3. In Inspector:
   - Set **Anchor Presets** to stretch-stretch (hold Alt while clicking)
   - Set all margins to 0 (or leave some padding if you want)
   - Set **Color** to a parchment-like color (e.g., R: 0.95, G: 0.90, B: 0.80, A: 0.95)

### Step 1.3: Create Journal Header

1. Right-click on JournalPanel, select **UI > Text - TextMeshPro**
2. Rename to "JournalHeader"
3. Set text to "Journal"
4. Position at top of panel
5. Set font size to 36, alignment to center

### Step 1.4: Create Filter Buttons

1. Right-click on JournalPanel, select **UI > Button - TextMeshPro**
2. Rename to "FilterAllButton"
3. Set button text to "All"
4. Duplicate this button 3 times:
   - FilterCertainButton (text: "Certain")
   - FilterUncertainButton (text: "Uncertain")
   - FilterSpeculativeButton (text: "Speculative")
5. Arrange buttons horizontally at the top (below header)

### Step 1.5: Create Scroll View

1. Right-click on JournalPanel, select **UI > Scroll View**
2. Rename to "JournalScrollView"
3. In Inspector:
   - Set **Anchor Presets** to stretch-stretch (hold Alt)
   - Set margins: Left: 20, Right: 20, Top: 80, Bottom: 20 (adjust based on your header/buttons)
4. Expand the ScrollView in Hierarchy:
   - Find **Viewport** > **Content** (this is where entries will go)
   - Rename Content to "EntryContainer"

### Step 1.6: Create Entry Prefab (Optional but Recommended)

1. Right-click on EntryContainer, select **UI > Text - TextMeshPro**
2. Rename to "JournalEntryPrefab"
3. In Inspector:
   - Set **Width**: 800 (or match your scroll view width)
   - Set **Height**: 100
   - Set **Font Size**: 16
   - Set **Alignment**: Top Left
   - Enable **Word Wrapping**
4. Drag this prefab from Hierarchy to your **Project** window (Assets/Prefabs/ or create a Prefabs folder)
5. Delete the prefab instance from EntryContainer (we'll instantiate it via code)

### Step 1.7: Create Empty State Text

1. Right-click on EntryContainer, select **UI > Text - TextMeshPro**
2. Rename to "EmptyStateText"
3. Set text to "No journal entries yet."
4. Center it in the container
5. Set font size to 24, color to gray

### Step 1.8: Add JournalUI Script

1. Select JournalCanvas in Hierarchy
2. In Inspector, click **Add Component**
3. Search for "JournalUI" and add it
4. In JournalUI component, assign references:
   - **Journal Panel**: Drag JournalPanel from Hierarchy
   - **Scroll Rect**: Drag JournalScrollView from Hierarchy
   - **Entry Container**: Drag EntryContainer from Hierarchy
   - **Entry Prefab**: Drag JournalEntryPrefab from Project (if you created it)
   - **Empty State Text**: Drag EmptyStateText from Hierarchy
   - **Filter All Button**: Drag FilterAllButton
   - **Filter Certain Button**: Drag FilterCertainButton
   - **Filter Uncertain Button**: Drag FilterUncertainButton
   - **Filter Speculative Button**: Drag FilterSpeculativeButton

### Step 1.9: Test Journal UI

1. Press Play
2. Press **J** key - Journal should open/close
3. Test filter buttons (they won't show entries yet since journal is empty)

---

## Part 2: Set Up Status Menu UI

### Step 2.1: Create Status Menu Canvas

1. Right-click in Hierarchy, select **UI > Canvas**
2. Rename to "StatusMenuCanvas"
3. In Inspector:
   - **Render Mode**: Screen Space - Overlay
   - **Sort Order**: 20 (above Journal UI)

### Step 2.2: Create Status Menu Panel

1. Right-click on StatusMenuCanvas, select **UI > Panel**
2. Rename to "StatusMenuPanel"
3. In Inspector:
   - Set **Anchor Presets** to stretch-stretch (hold Alt)
   - Set margins: Left: 100, Right: 100, Top: 50, Bottom: 50 (centered panel)
   - Set **Color**: Semi-transparent dark (e.g., R: 0.1, G: 0.1, B: 0.1, A: 0.9)

### Step 2.3: Create Tab Buttons

1. Right-click on StatusMenuPanel, select **UI > Button - TextMeshPro**
2. Rename to "StatusTabButton"
3. Set button text to "Status"
4. Duplicate this button 4 times:
   - EquipmentTabButton (text: "Equipment")
   - SkillsTabButton (text: "Skills")
   - InventoryTabButton (text: "Inventory")
   - NarrativeSkillsTabButton (text: "Narrative Skills")
5. Arrange buttons horizontally at the top of the panel

### Step 2.4: Create Tab Panels Container

1. Right-click on StatusMenuPanel, select **UI > Panel**
2. Rename to "TabPanelsContainer"
3. Set **Anchor Presets** to stretch-stretch (hold Alt)
4. Set margins: Left: 20, Right: 20, Top: 60, Bottom: 20 (space for tab buttons)

### Step 2.5: Create Status Tab Panel

1. Right-click on TabPanelsContainer, select **UI > Panel**
2. Rename to "StatusTabPanel"
3. Set **Anchor Presets** to stretch-stretch (hold Alt), margins to 0
4. Add TextMeshPro text elements for:
   - CharacterNameText (large, top center)
   - LevelText
   - XpText
   - SpText
   - StrengthText
   - FinesseText
   - FocusText
   - SpeedText
   - LuckText
5. Arrange them vertically on the left side

### Step 2.6: Create Narrative Skills Tab Panel

1. Right-click on TabPanelsContainer, select **UI > Panel**
2. Rename to "NarrativeSkillsTabPanel"
3. Set **Anchor Presets** to stretch-stretch (hold Alt), margins to 0
4. Add TextMeshPro text elements for each skill:
   - **Perception Section**:
     - PerceptionLevelText (e.g., "Perception: 5")
     - PerceptionThresholdText (e.g., "Threshold: Minimal Hint (3-5)")
     - PerceptionDescriptionText (description)
   - **Interpretive Section**:
     - InterpretiveLevelText
     - InterpretiveThresholdText
     - InterpretiveDescriptionText
   - **Empathic Section**:
     - EmpathicLevelText
     - EmpathicThresholdText
     - EmpathicDescriptionText
5. Arrange them vertically with spacing

### Step 2.7: Create Placeholder Tab Panels

1. Right-click on TabPanelsContainer, select **UI > Panel**
2. Rename to "EquipmentTabPanel"
3. Add a TextMeshPro text saying "Equipment Tab - Coming Soon"
4. Repeat for:
   - SkillsTabPanel
   - InventoryTabPanel

### Step 2.8: Add StatusMenuUI Script

1. Select StatusMenuCanvas in Hierarchy
2. In Inspector, click **Add Component**
3. Search for "StatusMenuUI" and add it
4. Assign ALL references in the StatusMenuUI component:
   - **Status Menu Panel**: StatusMenuPanel
   - **All Tab Buttons**: Drag from Hierarchy
   - **All Tab Panels**: Drag from Hierarchy
   - **All Status Tab UI Elements**: Drag text elements
   - **All Narrative Skills Tab UI Elements**: Drag text elements

### Step 2.9: Test Status Menu

1. Press Play
2. Press **TAB** key - Status Menu should open and pause the game
3. Click tab buttons to switch between tabs
4. Verify Status tab shows unit stats
5. Verify Narrative Skills tab shows skill levels (5/3/4)

---

## Part 3: Set Up Interaction Zones (Testing)

### Step 3.1: Create Test Object

1. In your ExplorationTest scene, right-click in Hierarchy
2. Select **3D Object > Cube**
3. Rename to "TestInteractionObject"
4. Position it somewhere the player can walk to

### Step 3.2: Add Interaction Zone

1. Select TestInteractionObject
2. In Inspector, click **Add Component**
3. Search for "InteractionZone" and add it
4. The component will automatically:
   - Require a Collider (adds BoxCollider if missing)
   - Set collider as Trigger
5. Adjust the BoxCollider size if needed (make it larger than the cube for easier testing)

### Step 3.3: Test Interaction Zone

1. Press Play
2. Walk the player near the TestInteractionObject
3. Check Console - you should see:
   - "[InteractionZone] Player entered zone: TestInteractionObject"
   - "[InteractionZone] Player exited zone: TestInteractionObject"

---

## Part 4: Test Journal System

### Step 4.1: Add Test Entry via Code

1. Create a test script or add to an existing script:
```csharp
using Riftbourne.Exploration;
using System.Collections.Generic;

// In Start() or Update() method:
if (Input.GetKeyDown(KeyCode.T)) // Press T to add test entry
{
    JournalSystem.Instance.AddEntry(
        "This is a test entry with certain confidence.",
        ConfidenceLevel.Certain,
        new List<string> { "test", "certainty" }
    );
    
    JournalSystem.Instance.AddEntry(
        "This might be something interesting?",
        ConfidenceLevel.Uncertain,
        new List<string> { "test", "uncertainty" }
    );
    
    JournalSystem.Instance.AddEntry(
        "Could this be a ritual site?",
        ConfidenceLevel.Speculative,
        new List<string> { "test", "speculation" }
    );
}
```

2. Or use the Unity Console to test:
   - Open Console
   - The JournalSystem will log when entries are added

### Step 4.2: Test Journal Filtering

1. Press Play
2. Add test entries (via code above or manually)
3. Press **J** to open Journal
4. Click filter buttons - entries should filter by confidence level

---

## Part 5: Verify Everything Works

### Testing Checklist:

1. ✅ Press **TAB** - Status Menu opens, game pauses
2. ✅ Status tab shows: Name, Level, XP, SP, Strength, Finesse, Focus, Speed, Luck
3. ✅ Narrative Skills tab shows: Perception (5), Interpretive (3), Empathic (4) with threshold bands
4. ✅ Press **J** - Journal opens (game does NOT pause)
5. ✅ Journal filter buttons work
6. ✅ Walk near test object - Console logs proximity detection
7. ✅ All tabs in Status Menu can be clicked (even if empty)

---

## Troubleshooting

### Journal doesn't open with J key
- Check that JournalUI script is on JournalCanvas
- Verify PlayerInputActions.inputactions was saved and Unity regenerated the C# class
- Check Console for errors

### Status Menu doesn't open with TAB
- Check that StatusMenuUI script is on StatusMenuCanvas
- Verify input actions were updated
- Check Console for errors

### No unit stats showing
- Make sure you have a player unit in the scene
- Check that the unit has Unit.cs component
- Verify PartyManager.Instance exists (or unit is player-controlled)

### Interaction Zone not detecting player
- Check that collider is set as Trigger
- Verify player has ExplorationController component
- Check Console for warnings about player detection
- Try assigning playerObject manually in Inspector

### UI elements not visible
- Check Canvas Sort Order (higher = on top)
- Verify UI elements are children of the Canvas
- Check that panels are active (not disabled)
- Verify RectTransform anchors are set correctly

---

## Next Steps

Once everything is working:

1. **Style the UI**: Add better fonts, colors, backgrounds
2. **Add more journal entries**: Integrate with exploration system
3. **Expand Status Menu**: Implement Equipment, Skills, Inventory tabs
4. **Connect Interaction Zones**: Use proximity detection to trigger journal entries
5. **Add narrative skill checks**: Use skill levels to determine what journal entries appear

---

## Quick Reference: Key Bindings

- **J**: Toggle Journal (doesn't pause)
- **TAB**: Toggle Status Menu (pauses game)

---

## File Locations Reference

All scripts should be in:
- `Assets/Scripts/Skills/` - NarrativeSkill.cs, NarrativeSkillCategory.cs
- `Assets/Scripts/Exploration/` - JournalEntry.cs, JournalSystem.cs, ConfidenceLevel.cs, InteractionZone.cs
- `Assets/Scripts/UI/` - JournalUI.cs, StatusMenuUI.cs
- `Assets/Scripts/Characters/Unit.cs` - Updated with narrative skills

Input actions:
- `Assets/PlayerInputActions.inputactions` - Updated with Journal and StatusMenu actions
