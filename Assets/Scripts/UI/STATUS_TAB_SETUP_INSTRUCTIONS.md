# Status Tab UI Setup Instructions

## Overview

The Status Tab has been redesigned to show:
- **6 party member portraits** along the top (clickable to switch displayed character)
- **Character name** under each portrait
- **Large character portrait** for the selected character
- **Full Name and Title** display
- **Narrative Skills** integrated into the status tab (removed separate tab)

## Step 1: Update CharacterDefinition Assets

1. **Open each CharacterDefinition asset** in your project
2. **Set the new fields:**
   - `fullName` - Character's full name (e.g., "Alexander the Brave")
   - `title` - Character's title/epithet (e.g., "The Riftwalker", "Master Scholar")
   - If `fullName` is empty, it will use `characterName` as fallback
   - `title` can be left empty if character has no title

## Step 2: Set Up Status Tab Panel UI

### 2.1 Create Party Portraits Container

1. **In your Status Tab Panel:**
   - Create empty GameObject: **"PartyPortraitsContainer"**
   - Add `RectTransform` component (should be automatic)
   - Set Anchor to **Top Center** (or Top Stretch)
   - Set position: **Y = -50** (or appropriate offset from top)
   - Set width to fill panel width
   - Set height: **100-150 pixels**

2. **Add Horizontal Layout Group:**
   - Select "PartyPortraitsContainer"
   - `Add Component → Layout → Horizontal Layout Group`
   - Set `Spacing = 10` (adjust as needed)
   - Set `Child Alignment = Middle Center`
   - **IMPORTANT:** Disable `Child Force Expand` for **Width** (uncheck it) - this prevents stretching
   - **IMPORTANT:** Disable `Child Force Expand` for **Height** (uncheck it) - this prevents stretching
   - Set `Child Control Size` - Width = **false**, Height = **false** (let children control their own size)

### 2.2 Create Party Portrait Prefab (Optional but Recommended)

1. **Create new GameObject:**
   - Right-click in Hierarchy → `UI → Image`
   - Name it: **"PartyPortraitPrefab"**

2. **Set up Portrait Image:**
   - Set `RectTransform`:
     - Width: **100**
     - Height: **100**
     - Anchor: **Middle Center** (both min and max at 0.5, 0.5)
     - Pivot: **Middle Center** (0.5, 0.5)
   - Set `Image` component:
     - `Image Type = Simple` (this is the default - fine to leave as-is)
     - `Preserve Aspect = true` (keeps portrait from distorting)
     - Assign a default sprite or leave empty (will be set by code)

3. **Add Button Component:**
   - `Add Component → Button`
   - Set `Transition = Color Tint` (or Sprite Swap if you have highlight sprites)
   - Set `Normal Color = White`
   - Set `Highlighted Color = Yellow` (or your highlight color)
   - Set `Selected Color = White`
   - Set `Pressed Color = Light Gray`

4. **Add Character Name Text:**
   - Right-click "PartyPortraitPrefab" → `UI → Text - TextMeshPro`
   - Name it: **"NameText"**
   - Set `RectTransform`:
     - Anchor: Bottom Stretch
     - Position Y: **-5** (just below portrait)
     - Height: **20**
   - Set `TextMeshProUGUI`:
     - `Alignment = Center`
     - `Font Size = 12`
     - `Text = "Character Name"` (placeholder)

5. **Optional: Add Selection Border:**
   - Right-click "PartyPortraitPrefab" → `UI → Image`
   - Name it: **"SelectionBorder"**
   - Set as child of portrait Image
   - Set `RectTransform` to fill parent
   - Set `Image`:
     - `Color = Transparent` (or your border color)
     - `Image Type = Simple`
   - Disable by default (will be enabled when selected)

6. **Save as Prefab:**
   - Drag "PartyPortraitPrefab" from Hierarchy to `Assets/Prefabs/UI/` folder
   - Delete from scene (we'll assign the prefab)

### 2.3 Set Up Large Portrait Display

1. **In Status Tab Panel:**
   - Create `UI → Image` GameObject
   - Name it: **"LargePortrait"**
   - Set `RectTransform`:
     - Width: **200-300 pixels**
     - Height: **200-300 pixels**
     - Position: **Left side** of panel (or center, your choice)
     - Anchor: **Middle Left** (or Middle Center)

2. **Set Image component:**
   - `Image Type = Simple` (this is the default - fine to leave as-is)
   - `Preserve Aspect = true` (optional, keeps portrait from distorting)
   - Leave sprite empty (set by code)

### 2.4 Set Up Character Name and Title Display

1. **Create Full Name Text:**
   - Create `UI → Text - TextMeshPro`
   - Name it: **"CharacterFullName"**
   - Position: **Below or next to large portrait**
   - Set `Font Size = 18-24` (larger than regular text)
   - Set `Alignment = Left` (or Center)

2. **Create Title Text:**
   - Create `UI → Text - TextMeshPro`
   - Name it: **"CharacterTitle"**
   - Position: **Below full name**
   - Set `Font Size = 14-16`
   - Set `Font Style = Italic` (optional, for title styling)
   - Set `Alignment = Left` (or Center)

### 2.5 Set Up Narrative Skills Section

1. **Create Container:**
   - Create empty GameObject: **"NarrativeSkillsSection"**
   - Add `RectTransform`
   - Position: **Right side** of panel (or below stats)

2. **Add Vertical Layout Group:**
   - `Add Component → Layout → Vertical Layout Group`
   - Set `Spacing = 10`
   - Set `Child Alignment = Upper Left`

3. **Create Narrative Skill Displays:**
   For each skill (Perception, Interpretive, Empathic):
   - Create `UI → Text - TextMeshPro` for level (e.g., "PerceptionLevel")
   - Create `UI → Text - TextMeshPro` for threshold (e.g., "PerceptionThreshold")
   - Create `UI → Text - TextMeshPro` for description (e.g., "PerceptionDescription")
   - Or create a prefab for skill display and instantiate 3 times

4. **Example Structure:**
   ```
   NarrativeSkillsSection
   ├── PerceptionGroup
   │   ├── PerceptionLevel (TextMeshPro)
   │   ├── PerceptionThreshold (TextMeshPro)
   │   └── PerceptionDescription (TextMeshPro)
   ├── InterpretiveGroup
   │   ├── InterpretiveLevel (TextMeshPro)
   │   ├── InterpretiveThreshold (TextMeshPro)
   │   └── InterpretiveDescription (TextMeshPro)
   └── EmpathicGroup
       ├── EmpathicLevel (TextMeshPro)
       ├── EmpathicThreshold (TextMeshPro)
       └── EmpathicDescription (TextMeshPro)
   ```

## Step 3: Wire Up StatusMenuUI Component

1. **Select GameObject with StatusMenuUI component** (usually on StatusMenuCanvas or similar)

2. **In Inspector, find StatusMenuUI component:**

3. **Assign Party Portraits Fields:**
   - `partyPortraitsContainer` → Drag "PartyPortraitsContainer" GameObject
   - `partyPortraitPrefab` → Drag "PartyPortraitPrefab" prefab (if you created one)
   - `maxPartyPortraits` → Set to **6**

4. **Assign Character Display Fields:**
   - `largePortraitImage` → Drag "LargePortrait" Image component
   - `characterFullNameText` → Drag "CharacterFullName" TextMeshPro component
   - `characterTitleText` → Drag "CharacterTitle" TextMeshPro component
   - `characterNameText` → Keep existing assignment (for backward compatibility)
   - `levelText`, `xpText`, `spText` → Keep existing assignments
   - `strengthText`, `finesseText`, `focusText`, `speedText`, `luckText` → Keep existing assignments

5. **Assign Narrative Skills Fields:**
   - `narrativeSkillsSection` → Drag "NarrativeSkillsSection" GameObject
   - `perceptionLevelText` → Drag PerceptionLevel TextMeshPro
   - `perceptionThresholdText` → Drag PerceptionThreshold TextMeshPro
   - `perceptionDescriptionText` → Drag PerceptionDescription TextMeshPro
   - `interpretiveLevelText` → Drag InterpretiveLevel TextMeshPro
   - `interpretiveThresholdText` → Drag InterpretiveThreshold TextMeshPro
   - `interpretiveDescriptionText` → Drag InterpretiveDescription TextMeshPro
   - `empathicLevelText` → Drag EmpathicLevel TextMeshPro
   - `empathicThresholdText` → Drag EmpathicThreshold TextMeshPro
   - `empathicDescriptionText` → Drag EmpathicDescription TextMeshPro

## Step 4: Hide Narrative Skills Tab (Optional)

1. **Find Narrative Skills Tab Button** in your UI
2. **Disable the GameObject** (or hide it)
3. The code will automatically hide it, but you can manually disable it in Inspector

## Step 5: Layout Suggestions

### Recommended Layout:

```
Status Tab Panel
├── PartyPortraitsContainer (Top, horizontal)
│   ├── Portrait 1
│   ├── Portrait 2
│   ├── Portrait 3
│   ├── Portrait 4
│   ├── Portrait 5
│   └── Portrait 6
├── Main Content Area (Below portraits)
│   ├── Left Side
│   │   ├── LargePortrait (Image)
│   │   ├── CharacterFullName (Text)
│   │   └── CharacterTitle (Text)
│   ├── Center
│   │   ├── Level (Text)
│   │   ├── XP (Text)
│   │   ├── SP (Text)
│   │   ├── Strength (Text)
│   │   ├── Finesse (Text)
│   │   ├── Focus (Text)
│   │   ├── Speed (Text)
│   │   └── Luck (Text)
│   └── Right Side
│       └── NarrativeSkillsSection
│           ├── Perception
│           ├── Interpretive
│           └── Empathic
```

## Troubleshooting

### Portraits Are Stretching

If party portraits are stretching horizontally:

1. **Check Horizontal Layout Group on PartyPortraitsContainer:**
   - Select "PartyPortraitsContainer"
   - In `Horizontal Layout Group` component:
     - **Uncheck** `Child Force Expand` → **Width**
     - **Uncheck** `Child Force Expand` → **Height**
     - Set `Child Control Size` → **Width = false**
     - Set `Child Control Size` → **Height = false**

2. **Check Portrait RectTransform:**
   - Select each portrait GameObject
   - In `RectTransform`:
     - Set `Width = 100` (fixed size)
     - Set `Height = 100` (fixed size)
     - Set `Anchor Min = (0.5, 0.5)`
     - Set `Anchor Max = (0.5, 0.5)`
     - Set `Pivot = (0.5, 0.5)`

3. **Check Portrait Image Component:**
   - Select portrait Image
   - Ensure `Image Type = Simple`
   - Enable `Preserve Aspect = true`

The code will automatically set these values for dynamically created portraits, but if you're using a prefab, make sure the prefab has these settings.

### Image Type = Simple

**Question:** "There is no setting for Image Type = Simple with the party portrait prefab setup. Is it fine to leave this alone?"

**Answer:** Yes! `Image Type = Simple` is the **default** setting for Unity UI Images. If you don't see it explicitly set, it's already Simple. You only need to change it if you want Filled, Sliced, or Tiled. For portraits, Simple is perfect.

## Step 6: Testing

1. **Enter Play Mode**
2. **Ensure party is set up** (via ExplorationSceneInitializer or GameInitializer)
3. **Press TAB** to open Status Menu
4. **Verify:**
   - ✅ Party portraits appear at top (up to 6)
   - ✅ Character names appear under each portrait
   - ✅ Clicking a portrait switches displayed character
   - ✅ Large portrait updates when character is selected
   - ✅ Full name and title display correctly
   - ✅ Narrative skills appear in status tab
   - ✅ Stats display correctly for selected character

## Troubleshooting

### "Portraits not appearing"
- Check `partyPortraitsContainer` is assigned
- Check party has members (check Console logs)
- Check `partyPortraitPrefab` is assigned (or code will create basic UI)

### "Portrait clicks not working"
- Ensure Button component exists on portrait prefab
- Check Console for "Selected character" message when clicking
- Verify `OnPortraitClicked` is being called

### "Large portrait not showing"
- Check `largePortraitImage` is assigned
- Check CharacterDefinition has `portrait` sprite assigned
- Verify Image component is enabled

### "Full name/Title not showing"
- Check `characterFullNameText` and `characterTitleText` are assigned
- Check CharacterDefinition has `fullName` and `title` set
- If `fullName` is empty, it uses `characterName`

### "Narrative skills not appearing"
- Check `narrativeSkillsSection` is assigned
- Check all narrative skill TextMeshPro components are assigned
- Verify section GameObject is active

## Customization Tips

1. **Portrait Size:** Adjust `RectTransform` width/height of portrait prefab
2. **Portrait Spacing:** Adjust `Spacing` in Horizontal Layout Group
3. **Selection Visual:** Customize `UpdatePortraitSelection` method to add borders, outlines, or color changes
4. **Layout:** Use Unity's Layout Groups to automatically arrange elements
5. **Styling:** Create custom UI themes with colors, fonts, and sprites
