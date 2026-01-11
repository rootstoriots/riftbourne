# Party Portrait Slots Setup Guide

## Overview

The Status Menu UI now uses **6 static portrait slots** instead of dynamically creating/destroying portraits. This provides:
- Consistent positioning
- Better performance (no instantiation/destruction)
- Easier layout control
- Standardized locations

## Unity Editor Setup

### Step 1: Create 6 Portrait Slot GameObjects

1. **In your Status Tab Panel**, locate or create the `partyPortraitsContainer` GameObject
2. **Create 6 child GameObjects** named:
   - `PartyPortraitSlot0`
   - `PartyPortraitSlot1`
   - `PartyPortraitSlot2`
   - `PartyPortraitSlot3`
   - `PartyPortraitSlot4`
   - `PartyPortraitSlot5`

### Step 2: Configure Each Slot

Each slot GameObject should have:

1. **RectTransform** (automatically added)
   - Set desired size (e.g., 100x100)
   - Position in standardized location
   - Use anchors/pivots for consistent layout

2. **Image Component** (for portrait sprite)
   - Add `Image` component
   - Set `Image Type` to `Simple`
   - Enable `Preserve Aspect`

3. **Button Component** (for click interaction)
   - Add `Button` component
   - Configure button colors/states as desired

4. **Name Text (Optional)**
   - Create child GameObject named `Name` or `NameText`
   - Add `TextMeshProUGUI` component
   - Position below portrait
   - Set alignment to center

### Step 3: Layout Options

**Option A: Horizontal Layout Group**
- Add `Horizontal Layout Group` to `partyPortraitsContainer`
- Set spacing between portraits
- Enable `Child Force Expand` if desired
- Slots will automatically space evenly

**Option B: Grid Layout Group**
- Add `Grid Layout Group` to `partyPortraitsContainer`
- Set `Cell Size` (e.g., 100x100)
- Set `Spacing` (e.g., 10, 10)
- Set `Constraint` to `Fixed Column Count` = 6 (or 3 for 2 rows)
- Slots will automatically arrange in grid

**Option C: Manual Positioning**
- Position each slot manually using RectTransform
- Use anchors for responsive positioning
- No layout group needed

### Step 4: Assign Slots in Inspector

1. **Select the StatusMenuUI GameObject**
2. **In Inspector**, find "Status Tab UI - Party Portraits" section
3. **Expand "Party Portrait Slots" array**
4. **Set Size to 6**
5. **Assign each slot** in order:
   - Element 0: `PartyPortraitSlot0`
   - Element 1: `PartyPortraitSlot1`
   - Element 2: `PartyPortraitSlot2`
   - Element 3: `PartyPortraitSlot3`
   - Element 4: `PartyPortraitSlot4`
   - Element 5: `PartyPortraitSlot5`

### Step 5: Initial State

- All 6 slots should be **active** in the scene initially
- The system will automatically disable empty slots at runtime
- You can start with all slots disabled if preferred - they'll be enabled when needed

## How It Works

### Runtime Behavior

1. **On Menu Open:**
   - `RefreshPartyPortraits()` is called
   - Gets party members from `PartyManager`
   - For each slot (0-5):
     - If slot has a party member: **Enable** slot and populate with character data
     - If slot is empty: **Disable** slot

2. **Slot Population:**
   - Portrait image is set from `CharacterDefinition.Portrait`
   - Name text is set from `CharacterDefinition.CharacterName`
   - Button click handler is attached
   - Selection visual feedback is applied

3. **Click Handling:**
   - Clicking a portrait selects that character
   - Selected portrait gets visual feedback
   - Character details are displayed in the status tab

### Example Slot Structure

```
PartyPortraitSlot0
├── Image (portrait sprite)
├── Button (click handler)
└── Name (TextMeshProUGUI - optional)
    └── Text: Character Name
```

## Visual Feedback

The `UpdatePortraitSelection()` method handles visual feedback for selected portraits. You can customize this by:

1. **Adding a selection indicator** (border, highlight, etc.) to each slot
2. **Modifying `UpdatePortraitSelection()`** to enable/disable or change colors
3. **Using different sprites** for selected vs unselected states

Example customization:
```csharp
// In UpdatePortraitSelection(), you could:
if (portraitObj.transform.Find("SelectionBorder") != null)
{
    portraitObj.transform.Find("SelectionBorder").gameObject.SetActive(isSelected);
}
```

## Troubleshooting

### Slots Not Appearing

- **Check:** All 6 slots are assigned in Inspector
- **Check:** Slots are children of `partyPortraitsContainer`
- **Check:** `partyPortraitsContainer` is assigned in Inspector

### Slots Not Disabling

- **Check:** `RefreshPartyPortraits()` is being called when menu opens
- **Check:** Party member count is correct
- **Check:** Slots array has exactly 6 elements

### Portraits Not Clickable

- **Check:** Each slot has a `Button` component
- **Check:** Button is not disabled
- **Check:** Button's `Interactable` is true

### Portraits Not Showing Correct Character

- **Check:** Portrait image sprite is being set correctly
- **Check:** `SetupPortraitUI()` is finding the Image component
- **Check:** CharacterDefinition has a Portrait sprite assigned

## Migration from Old System

If you were using the old dynamic system:

1. **Remove** `partyPortraitPrefab` reference (no longer needed)
2. **Create** 6 static slot GameObjects as described above
3. **Assign** slots to the new `partyPortraitSlots` array
4. **Remove** any code that was creating/destroying portraits dynamically

The old `CreateBasicPortraitUI()` method has been removed - all slots should be pre-configured in the scene.
