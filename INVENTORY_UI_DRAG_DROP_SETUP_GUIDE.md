# Inventory UI Drag-and-Drop Setup Guide

This guide provides step-by-step instructions for setting up the new grid-based inventory UI system with drag-and-drop functionality in Unity.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [UI Structure Setup](#ui-structure-setup)
4. [Item Slot Prefab](#item-slot-prefab)
5. [Item Grid Setup](#item-grid-setup)
6. [Equipment Panel Setup](#equipment-panel-setup)
7. [Item Details Panel](#item-details-panel)
8. [Context Menu Setup](#context-menu-setup)
9. [Button Style System](#button-style-system)
10. [Treasury Manager](#treasury-manager)
11. [Final Integration](#final-integration)
12. [Testing](#testing)

---

## Overview

The new inventory system replaces the text-based display with a visual grid system featuring:
- 8-column grid layout
- Drag-and-drop item reordering
- Hover tooltips
- Right-click context menus
- Integrated equipment management
- Rarity-based visual styling

---

## Prerequisites

- Unity project with the Riftbourne scripts already in place
- TextMeshPro package installed
- UI Canvas set up in your scene
- StatusMenuUI already configured

---

## UI Structure Setup

### Step 1: Create the Inventory Tab Panel Structure

1. **Locate your Inventory Tab Panel** in the StatusMenuUI hierarchy
2. **Create the following structure** within the Inventory Tab Panel:

```
InventoryTabPanel
├── StatsPanel (existing or new)
│   ├── CharacterNameText (TextMeshProUGUI)
│   ├── AurumShardsText (TextMeshProUGUI)
│   └── WeightText (TextMeshProUGUI)
├── MainContent
│   ├── EquipmentSection
│   │   └── EquipmentSlotsPanel (GameObject - add EquipmentSlotsPanel component)
│   └── InventorySection
│       ├── ItemGridContainer (GameObject)
│       │   ├── ScrollRect (ScrollRect component)
│       │   │   └── Viewport
│       │   │       └── Content (GridLayoutGroup component)
│       │   └── ItemGridUI (GameObject - add ItemGridUI component)
│       └── ItemDetailsPanel (GameObject - add ItemDetailsPanel component)
└── ContextMenu (GameObject - add ItemContextMenu component)
```

---

## Item Slot Prefab

### Step 2: Create ItemSlotUI Prefab

1. **Create a new GameObject** named `ItemSlotPrefab`
2. **Add RectTransform** component (should be automatic)
3. **Set RectTransform size** to square (e.g., 80x80 pixels)
4. **Add the following components:**
   - `ItemSlotUI` script
   - `CanvasGroup` component
   - `Image` component (for background - optional)

5. **Create child GameObjects:**

   **Icon Layer:**
   - Create child: `IconImage`
     - Add `Image` component
     - Set Image Type to `Simple`
     - Anchor to stretch (fill parent)
     - Set margins to 5-10 pixels for padding

   **Button Overlay Layer:**
   - Create child: `ButtonOverlay`
     - Add `Image` component
     - Set Image Type to `Simple`
     - Anchor to stretch (fill parent)
     - Set Color alpha to ~0.5 for semi-transparency

   **Quantity Text:**
   - Create child: `QuantityText`
     - Add `TextMeshProUGUI` component
     - Position in bottom-right corner
     - Set font size to 12-14
     - Set alignment to bottom-right
     - Set color to white with outline/shadow for visibility

6. **Configure ItemSlotUI component:**
   - Drag `IconImage` to `Icon Image` field
   - Drag `ButtonOverlay` to `Button Overlay Image` field
   - Drag `QuantityText` to `Quantity Text` field
   - Drag the parent GameObject to `Canvas Group` field (or it will auto-add)
   - Set `Drag Alpha` to 0.6
   - (Optional) Create a drag ghost prefab and assign it (see "Drag Ghost Prefab Setup" section below)

7. **Save as Prefab:**
   - Drag to `Assets/Prefabs/UI/` folder
   - Name it `ItemSlotPrefab`

---

## Item Grid Setup

### Step 3: Configure ItemGridUI

1. **Select the ItemGridUI GameObject** in your hierarchy

2. **Add ItemGridUI component** if not already added

3. **Configure ItemGridUI:**
   - `Columns Per Row`: 8
   - `Item Slot Prefab`: Drag your `ItemSlotPrefab` here
   - `Grid Container`: Drag the `Content` GameObject (with GridLayoutGroup)
   - `Scroll Rect`: Drag the `ScrollRect` component
   - `Details Panel`: Drag the `ItemDetailsPanel` GameObject
   - `Context Menu`: Drag the `ContextMenu` GameObject

4. **Add Drop Zone to Grid Container:**
   - Select the `Content` GameObject (the one with GridLayoutGroup)
   - Add `ItemGridDropZone` component
   - **Important:** Also add an `Image` component (even if transparent) - this ensures it can receive raycasts for drop detection
   - This allows items to be dropped on empty grid space

5. **Configure GridLayoutGroup** (on Content GameObject):
   - `Cell Size`: Match your ItemSlotPrefab size (e.g., 80x80)
   - `Spacing`: 5-10 pixels (X and Y)
   - `Start Corner`: Upper Left
   - `Start Axis`: Horizontal
   - `Child Alignment`: Upper Left
   - `Constraint`: Fixed Column Count
   - `Constraint Count`: 8

5. **Add Drop Zone to Grid Container:**
   - Select the `Content` GameObject (the one with GridLayoutGroup)
   - Add `ItemGridDropZone` component
   - **Important:** Also add an `Image` component (even if fully transparent) - this ensures it can receive raycasts for drop detection
   - This allows items to be dropped on empty grid space

6. **Configure ScrollRect:**
   - `Content`: Drag the `Content` GameObject
   - `Horizontal`: Unchecked (vertical scrolling only)
   - `Vertical`: Checked
   - `Movement Type`: Elastic
   - `Scroll Sensitivity`: 20

**Critical Setup Note:** The `Content` GameObject must have:
- `GridLayoutGroup` component
- `ItemGridDropZone` component (for accepting drops on empty space)
- An `Image` component (even if fully transparent) - **required for raycast detection**

---

## Character Portrait Setup

### Step 3.75: Setup Character Portrait in CharacterDefinition

Before setting up the UI, you need to assign portrait sprites to your character definitions.

1. **Open each CharacterDefinition asset** in your project (e.g., `Assets/Resources/Characters/`)

2. **Find the "Identity" section** in the Inspector

3. **Assign Inventory Tab Portrait:**
   - Find the `Inventory Tab Portrait` field (newly added)
   - Drag your full-body character sprite/image to this field
   - This should be a full-body image suitable for displaying behind equipment slots
   - **Note:** The image will automatically preserve aspect ratio with a fixed height

4. **Repeat for all characters** that should have portraits in the inventory tab

**Tips:**
- Portrait images should be full-body or at least torso-up
- Images will be scaled to fit the standardized height while preserving aspect ratio
- If a character doesn't have an `InventoryTabPortrait` assigned, no portrait will be displayed (equipment slots will still work)

---

## Equipment Panel Setup

### Step 4: Create Equipment Slots

1. **Select EquipmentSlotsPanel GameObject**

2. **Add EquipmentSlotsPanel component**

3. **Create 6 Equipment Slot GameObjects** as children:
   - `MeleeWeaponSlot`
   - `RangedWeaponSlot`
   - `ArmorSlot`
   - `Accessory1Slot`
   - `Accessory2Slot`
   - `CodexSlot`

4. **For each Equipment Slot:**

   a. **Add EquipmentSlotUI component**
   
   b. **Set Equipment Slot field:**
      - MeleeWeaponSlot → `MeleeWeapon`
      - RangedWeaponSlot → `RangedWeapon`
      - ArmorSlot → `Armor`
      - Accessory1Slot → `Accessory1`
      - Accessory2Slot → `Accessory2`
      - CodexSlot → `Codex`

   c. **Create UI structure:**
      - `SlotBackground` (Image) - background/border
      - `ItemIcon` (Image) - shows equipped item icon
      - `SlotLabel` (TextMeshProUGUI) - slot name (hidden by default, shows on hover if empty)
      - `EmptyIndicator` (GameObject) - shows "Empty" text/image
      
      **Slot Label Behavior:**
      - Slot labels are **hidden by default** for a cleaner look
      - Labels **only appear when hovering** over an **empty** equipment slot
      - Labels disappear when the cursor moves away
      - This helps players identify slot types without cluttering the UI

   d. **Add drag support:**
      - Add `CanvasGroup` component to the slot GameObject (for drag transparency)
      - (Optional) Create a drag ghost prefab for visual feedback (see "Drag Ghost Prefab Setup" section above)

   e. **Create Drop Indicator (for drag feedback):**
      - Create child GameObject: `DropIndicator` (Image component)
      - Position: Same as slot (centered, or adjust as needed)
      - Size: Slightly larger than slot (e.g., 110% of slot size) or match slot size
      - **Image Component:**
        - Set sprite to a highlight/glow effect (e.g., glowing border, highlight ring)
        - **Color:** Green, blue, or gold (indicates valid drop target)
          - **Tip:** Use a bright color (e.g., bright green RGB: 0, 255, 0) for better glow effect
          - The pulsing animation will make it glow brighter/dimmer automatically
        - Image Type: Simple
        - Preserve Aspect: ✓ (if using a specific sprite)
      - **Important:** Set `Raycast Target` to ✗ (unchecked) - should not block interactions
      - **Order in Layer:** Should be above slot background but below item icon (or adjust as needed)
      - Initially disabled (will be enabled during drag operations)
      
      **Pulsing/Glow Effect:**
      - The drop indicator automatically pulses when an equipment item is dragged
      - The pulse effect includes:
        - **Scale animation:** Grows and shrinks (configurable in Inspector)
        - **Alpha animation:** Fades in and out (configurable in Inspector)
        - **Brightness/Glow:** Color brightness pulses (configurable in Inspector)
      - **Customization (NO CODE NEEDED!):** Adjust pulse settings in the Inspector:
        - Select the Equipment Slot GameObject
        - In `EquipmentSlotUI` component, find "Drop Indicator Pulse Settings" section
        - Adjust these values:
          - **Pulse Speed:** How fast it pulses (default: 2 = 2 pulses/second)
          - **Min Scale / Max Scale:** Size range (default: 0.9 to 1.1 = 90% to 110% size)
          - **Min Alpha / Max Alpha:** Transparency range (default: 0.5 to 1.0 = 50% to 100% opacity)
          - **Min Brightness / Max Brightness:** Glow intensity (default: 0.7 to 1.2 = 70% to 120% brightness)
      - **Tips:**
        - Increase `Max Brightness` (e.g., 1.5) for stronger glow
        - Increase `Max Scale` (e.g., 1.2) for more dramatic size change
        - Decrease `Min Alpha` (e.g., 0.3) for more dramatic fade
        - Increase `Pulse Speed` (e.g., 3) for faster pulsing
   
   f. **Configure EquipmentSlotUI:**
      - Drag `SlotBackground` to `Slot Background Image`
      - Drag `ItemIcon` to `Item Icon Image`
      - Drag `SlotLabel` to `Slot Label Text`
      - Drag `EmptyIndicator` to `Empty Indicator`
      - Drag `DropIndicator` Image to `Drop Indicator Image` field
      - Drag `CanvasGroup` to `Canvas Group` field (or it will auto-add)
      - Set `Drag Alpha` to 0.6 (for semi-transparency during drag)
      - (Optional) Assign drag ghost prefab (see "Drag Ghost Prefab Setup" section above)

5. **Configure EquipmentSlotsPanel:**
   - Drag each slot GameObject to its corresponding field
   - Drag `ItemDetailsPanel` to `Details Panel` field
   - Drag `ItemGridUI` GameObject to `Item Grid` field (for drag-and-drop from equipment to inventory)

6. **Create Character Portrait Background:**
   
   a. **Create Portrait Image GameObject:**
      - In the EquipmentSlotsPanel GameObject, create a child: `CharacterPortrait` (or similar name)
      - Add `Image` component (should be automatic if created via UI → Image)
      - **Important:** This should be a child of EquipmentSlotsPanel, positioned BEHIND the equipment slots
   
   b. **Configure RectTransform:**
      - Set Anchor: **Center** (or appropriate position for your layout)
      - Set Pivot: **Center** (0.5, 0.5)
      - **Set Height:** Fixed value (e.g., 400-600 pixels) - this will be the standardized height
      - **Set Width:** Will be calculated automatically to preserve aspect ratio
      - Position: Center of equipment slots area
   
   c. **Configure Image Component:**
      - **Image Type:** Simple
      - **Preserve Aspect:** ✓ (checked) - **CRITICAL for aspect ratio preservation**
      - **Color:** White (or adjust as needed for tinting)
      - **Raycast Target:** ✗ (unchecked) - portrait should not block interactions with equipment slots
   
   d. **Set Image Order:**
      - Ensure the portrait Image is BEHIND equipment slots in the hierarchy
      - Equipment slots should be children AFTER the portrait in the hierarchy
      - Or use Canvas sorting order: Portrait = lower order, Slots = higher order
   
   e. **Assign to EquipmentSlotsPanel:**
      - Select `EquipmentSlotsPanel` GameObject
      - In `EquipmentSlotsPanel` component, drag `CharacterPortrait` Image to `Character Portrait Image` field

7. **Layout the slots** in a paperdoll-style arrangement over the character portrait (visual design is up to you)

---

## Item Details Panel

### Step 5: Setup ItemDetailsPanel

1. **Select ItemDetailsPanel GameObject**

2. **Add ItemDetailsPanel component**

3. **Create UI structure:**
   ```
   ItemDetailsPanel
   ├── Panel (Image - background)
   ├── ItemIconImage (Image - displays item icon)
   ├── ItemNameText (TextMeshProUGUI)
   ├── RarityText (TextMeshProUGUI)
   ├── ItemTypeText (TextMeshProUGUI) - shows human-readable type
   ├── CharacterNameText (TextMeshProUGUI)
   ├── AurumShardsText (TextMeshProUGUI)
   └── WeightText (TextMeshProUGUI)
   ├── DescriptionText (TextMeshProUGUI)
   ├── WeightText (TextMeshProUGUI)
   ├── ValueText (TextMeshProUGUI) - shows just the number (e.g., "150")
   └── AurumShardsIcon (Image - icon for currency, appears next to value)
   ```

4. **Configure ItemDetailsPanel:**
   - Drag `Panel` GameObject to `Panel Object`
   - Drag `ItemIconImage` Image component to `Item Icon Image` field
   - Drag each TextMeshProUGUI to its corresponding field
   - **Add Aurum Shards Icon:**
     - Create child: `AurumShardsIcon` (Image component)
     - Assign your Aurum Shards sprite/icon to this Image
     - Drag this Image to `Aurum Shards Icon` field in ItemDetailsPanel
   - **Note:** This panel is static (always visible) - no positioning needed

5. **Style the panel:**
   - Add background Image with semi-transparent color
   - Add border/outline if desired
   - Set appropriate font sizes and colors
   - **ItemIconImage:** Set size to appropriate dimensions (e.g., 64x64 or 128x128)
   - Position ItemIconImage prominently (typically top-left or center-top of panel)

---

## Context Menu Setup

### Step 6: Setup ItemContextMenu

1. **Select ContextMenu GameObject**

2. **Add ItemContextMenu component**

3. **Create UI structure:**
   ```
   ContextMenu
   ├── MenuPanel (Image - background)
   │   ├── MenuItemContainer (VerticalLayoutGroup)
   │   │   └── (Menu items will be instantiated here)
   │   └── CancelButton (Button - optional)
   └── MenuItemPrefab (prefab for menu items)
   ```

4. **Create MenuItemPrefab:**
   - Create GameObject: `MenuItemPrefab`
   - Add `Button` component
   - Add child: `Text` (TextMeshProUGUI)
   - Style as needed (background, hover effects)
   - Save as prefab: `Assets/Prefabs/UI/MenuItemPrefab`

5. **Configure ItemContextMenu:**
   - Drag `MenuPanel` to `Menu Panel`
   - Drag `MenuItemContainer` to `Menu Item Container`
   - Drag `MenuItemPrefab` to `Menu Item Prefab`
   - (Optional) Create and assign `ItemExamineUI` GameObject
   - (Optional) Create and assign `EquipmentSlotSelectionUI` GameObject

6. **Configure MenuPanel:**
   - Add `Image` component for background
   - Set appropriate size (width: 200-250px, height: auto)
   - Add `VerticalLayoutGroup` on MenuItemContainer
   - Set spacing and padding

---

## Button Style System

### Step 7: Create ItemButtonStyle Assets

1. **Create ItemButtonStyle ScriptableObjects:**

   For each ItemType, create a style asset:
   - Right-click in Project: `Create > Riftbourne > UI > Item Button Style`
   - Name them:
     - `ButtonStyle_Equipment`
     - `ButtonStyle_Consumable`
     - `ButtonStyle_Loot`
     - `ButtonStyle_Container`
     - `ButtonStyle_KeyItem`

2. **Configure each style:**

   **ButtonStyle_Equipment:**
   - `Item Type`: Equipment
   - `Button Sprite`: Create/assign a sprite for equipment button overlay
   - `Base Color`: White or light gray
   - `Common Color`: White (1, 1, 1)
   - `Uncommon Color`: Light green (0.8, 1, 0.8)
   - `Rare Color`: Light blue (0.8, 0.8, 1)
   - `Epic Color`: Light purple (1, 0.8, 1)
   - `Legendary Color`: Light orange (1, 0.9, 0.7)
   - `Overlay Alpha`: 0.5

   Repeat for other item types with appropriate button sprites.

3. **Create ItemButtonStyleManager:**

   a. **Create GameObject** in scene: `ItemButtonStyleManager`
   
   b. **Add ItemButtonStyleManager component**
   
   c. **Add all style assets** to `Button Styles` list
   
   d. **Set Default Style** to one of your styles (e.g., ButtonStyle_Loot)
   
   e. **Make it persistent:**
      - Add to a persistent scene or DontDestroyOnLoad object
      - Or add to a scene that loads early

---

## Treasury Manager

### Step 8: Setup TreasuryManager

1. **Create GameObject**: `TreasuryManager`

2. **Add TreasuryManager component**

3. **Make it persistent:**
   - The script already has `DontDestroyOnLoad` in Awake
   - Ensure it's in a scene that persists or add to a manager scene

4. **No additional configuration needed** - it's ready to use

---

## Final Integration

### Step 9: Wire Up InventoryUI

1. **Select InventoryUI GameObject** (should be in InventoryTabPanel)

2. **Create Stats Text Components:**
   - Create 3 separate TextMeshProUGUI GameObjects:
     - `CharacterNameText` - displays character name
     - `AurumShardsText` - displays currency amount
     - `WeightText` - displays weight information
   - Position each text component where you want it in the UI
   - Style each as needed (font size, color, alignment, etc.)

3. **Configure InventoryUI component:**
   - `Character Name Text`: Drag your `CharacterNameText` TextMeshProUGUI
   - `Aurum Shards Text`: Drag your `AurumShardsText` TextMeshProUGUI
   - `Weight Text`: Drag your `WeightText` TextMeshProUGUI
   - `Item Grid`: Drag the `ItemGridUI` GameObject
   - `Equipment Panel`: Drag the `EquipmentSlotsPanel` GameObject
   - `Details Panel`: Drag the `ItemDetailsPanel` GameObject

### Step 10: Update StatusMenuUI

1. **Select StatusMenuUI GameObject**

2. **Verify Inventory Tab references:**
   - `Inventory Tab Panel`: Should point to your InventoryTabPanel
   - `Inventory UI`: Should point to your InventoryUI component

3. **Equipment Tab behavior:**
   - The equipment tab button now redirects to inventory tab
   - You can hide the equipment tab button if desired, or keep it for user familiarity

---

## Testing

### Step 11: Test the System

1. **Enter Play Mode**

2. **Open Status Menu** (TAB key)

3. **Switch to Inventory Tab**

4. **Test Hover:**
   - Hover over an item slot
   - Verify ItemDetailsPanel updates with item information and icon
   - Verify panel stays in static location (doesn't move)
   - Hover over different items - panel should update content
   - Hover over equipment slots - should show equipped item details

5. **Test Right-Click:**
   - Right-click an item slot
   - Verify context menu appears
   - Test each menu option:
     - **Examine** - shows item secrets
     - **Use** - only appears for non-battle consumables
     - **Send to Character** - transfers item to party member
     - **Discard** - removes item (with confirmation)
     - **Equip** - equips equipment items
     - **Send to Treasury** - stores item in treasury

6. **Test Drag-and-Drop:**
   - Click and drag an item slot
   - Drop on another slot (should swap)
   - Drop on equipment slot (should equip)
   - **Drag from equipment slot to inventory** (should unequip)
   - **Drag from inventory to equipment slot** (should equip)
   - Verify visual feedback during drag

7. **Test Equipment:**
   - Drag equipment item to equipment slot
   - Verify item equips
   - Verify currently equipped item returns to inventory

8. **Test Character Portrait:**
   - Switch between different characters
   - Verify character portrait appears behind equipment slots
   - Verify portrait preserves aspect ratio (width adjusts, height stays fixed)
   - Verify portrait is behind equipment slots (doesn't cover them)
   - Verify portrait disappears if character has no InventoryTabPortrait assigned

9. **Test Drop Indicator (Equipment Drag Feedback):**
   - Drag an equipment item from inventory grid
   - Verify pulsing drop indicators appear on compatible equipment slots
   - Verify indicators pulse smoothly (scale, alpha, and brightness/glow animation)
   - Verify indicators disappear when drag ends
   - Verify indicators only appear for equipment items (not consumables, etc.)
   - Test with items that can equip in multiple slots (e.g., accessories) - should show on all compatible slots
   - **Verify glow effect:** The indicator should appear to "glow" brighter and dimmer as it pulses

10. **Test Slot Label Hover Behavior:**
    - Hover over an **empty** equipment slot
    - Verify slot label appears (e.g., "Melee Weapon", "Armor", etc.)
    - Move cursor away from empty slot
    - Verify slot label disappears
    - Hover over an **equipped** slot
    - Verify slot label does NOT appear (only shows for empty slots)
    - Verify labels don't clutter the UI when not needed

8. **Test Character Switching:**
   - Switch characters using party portraits
   - Verify inventory updates correctly

---

## Troubleshooting

### Items Not Displaying

- **Check ItemGridUI references:** Ensure all references are assigned
- **Check ItemSlotPrefab:** Verify prefab is assigned and has ItemSlotUI component
- **Check CharacterState:** Ensure character has items in inventory
- **Check Console:** Look for error messages

### Drag-and-Drop Not Working

- **Check EventSystem:** Ensure EventSystem exists in scene
- **Check Canvas:** Ensure Canvas has GraphicRaycaster component
- **Check ItemSlotUI:** Verify drag handlers are properly implemented
- **Check CanvasGroup:** Ensure blocksRaycasts is set correctly

### Context Menu Not Appearing

- **Check Input:** Right-click should trigger IPointerClickHandler
- **Check References:** Verify ItemContextMenu references are assigned
- **Check MenuItemPrefab:** Ensure prefab is assigned

### Button Styles Not Applying

- **Check ItemButtonStyleManager:** Ensure it exists and styles are assigned
- **Check ItemType:** Verify item's ItemType matches style's ItemType
- **Check References:** Verify ItemSlotUI can access ItemButtonStyleManager.Instance

### Item Type Display Issues

- **Item types showing as enum names:** The system automatically converts to human-readable names:
  - `ConsumableNonBattle` → "Consumable"
  - `ConsumableBattle` → "Battle Consumable"
  - `KeyItem` → "Key Item"
  - etc.
- If you see enum names, check that ItemDetailsPanel is using `GetHumanReadableItemType()` method

### Aurum Shards Icon Not Showing

- **Check Icon Assignment:** Ensure Aurum Shards icon sprite is assigned to `AurumShardsIcon` Image
- **Check Icon Enabled:** Icon will only show if sprite is assigned
- **Value Display:** Value text shows just the number (e.g., "150"), icon appears next to it if assigned

### Equipment Drag-and-Drop Not Working

- **Check CanvasGroup:** Equipment slots need CanvasGroup component for drag functionality
- **Check ItemGrid Reference:** Ensure EquipmentSlotsPanel has ItemGridUI assigned
- **Check Drop Targets:** Can drop on inventory slots or inventory grid area
- **Empty Slots:** Cannot drag from empty equipment slots
- **Check Grid Container:** The Content GameObject must have:
  - `ItemGridDropZone` component
  - `Image` component (even if transparent) - **required for raycast detection**
- **Check Raycast:** Ensure GraphicRaycaster is on the Canvas
- **Check EventSystem:** Ensure EventSystem exists in the scene
- **Debug:** Check Console for "No valid drop target found" messages - this indicates raycast issues

### Use Command Not Appearing

- **Check Item Type:** "Use" only appears for `ItemType.ConsumableNonBattle`
- **Check UsableOutOfCombat:** Item must have `UsableOutOfCombat = true`
- **Verify ConsumableItem:** Item must be a ConsumableItem type

### Equipment Not Equipping

- **Check EquipmentItem:** Verify item is actually EquipmentItem type
- **Check CompatibleSlots:** Verify item can equip in target slot
- **Check CharacterState:** Ensure character reference is valid
- **Check Inventory:** Verify item is removed from inventory when equipped

### Character Portrait Not Displaying

- **Check CharacterDefinition:** Verify `InventoryTabPortrait` is assigned in the CharacterDefinition asset
- **Check Image Reference:** Ensure `Character Portrait Image` is assigned in EquipmentSlotsPanel Inspector
- **Check Image Enabled:** Verify the Image component is enabled
- **Check Sprite:** Verify the sprite is assigned to the Image component
- **Check Hierarchy:** Ensure portrait Image is a child of EquipmentSlotsPanel
- **Check Aspect Ratio:** Verify `Preserve Aspect` is checked on the Image component
- **Check RectTransform:** Ensure height is set (width will be calculated automatically)

### Portrait Aspect Ratio Issues

- **Portrait too wide/narrow:** The width is calculated automatically based on sprite aspect ratio and fixed height
- **To adjust:** Change the fixed height in RectTransform, or use a different sprite with different aspect ratio
- **Verify Preserve Aspect:** Must be checked on Image component
- **Image Type:** Should be set to "Simple" (not Filled, Sliced, etc.)

### Drop Indicators Not Appearing

- **Check Drop Indicator Image:** Ensure `Drop Indicator Image` is assigned in EquipmentSlotUI Inspector
- **Check Item Type:** Indicators only appear for EquipmentItem types (not consumables, key items, etc.)
- **Check Compatible Slots:** Verify the equipment item has compatible slots assigned in its definition
- **Check Equipment Panel Reference:** Ensure ItemGridUI has EquipmentSlotsPanel reference (set via InventoryUI)
- **Check Image Enabled:** Drop indicator should start disabled, but will be enabled during drag
- **Check Raycast Target:** Drop indicator should have Raycast Target unchecked (won't affect functionality, but good practice)

### Drop Indicator Not Pulsing

**First, check these basics:**
- **Check Drop Indicator Image:** Ensure `Drop Indicator Image` is assigned in EquipmentSlotUI Inspector
- **Check Image Enabled:** The image should be enabled automatically, but verify it's not manually disabled
- **Check GameObject Active:** Ensure the Equipment Slot GameObject is active in hierarchy
- **Check Component Enabled:** Ensure EquipmentSlotUI component is enabled (checkbox in Inspector)
- **Check Console:** Look for debug messages like "Started pulse animation for [Slot]" or warnings about null DropIndicatorImage

**If still not pulsing:**
- **Check Sprite:** If using a custom sprite, ensure it's visible and has appropriate transparency
- **Check Color:** Use a bright color (e.g., bright green RGB: 0, 255, 0) for better visibility
- **Check Pulse Settings:** In Inspector, verify "Drop Indicator Pulse Settings" values are reasonable:
  - Pulse Speed should be > 0 (default: 2)
  - Min/Max Scale should be different (default: 0.9 and 1.1)
  - Min/Max Alpha should be different (default: 0.5 and 1.0)
  - Min/Max Brightness should be different (default: 0.7 and 1.2)

**Make Pulse More Visible:**
- **Increase Max Scale:** Set to 1.2 or 1.3 for more dramatic size change
- **Decrease Min Alpha:** Set to 0.3 or 0.2 for more dramatic fade
- **Increase Max Brightness:** Set to 1.5 or 2.0 for stronger glow effect
- **Increase Pulse Speed:** Set to 3 or 4 for faster, more noticeable pulsing

**Debug Steps:**
1. Enter Play Mode
2. Drag an equipment item from inventory
3. Check Console for "Started pulse animation" messages
4. If you see warnings about null DropIndicatorImage, assign it in Inspector
5. If no messages appear, the drag start event might not be firing - check EquipmentSlotsPanel references

### Slot Labels Not Showing on Hover

- **Check Text Component:** Ensure `Slot Label Text` is assigned in EquipmentSlotUI Inspector
- **Check Empty Slot:** Labels only show when hovering over **empty** slots (not equipped slots)
- **Check Hover:** Make sure you're actually hovering over the slot (not just nearby)
- **Check Text Enabled:** The text should be enabled/disabled automatically - verify it's not manually disabled
- **Check Character:** Ensure `currentCharacter` is set (labels won't show if character is null)

---

## Additional Notes

### Performance Considerations

- **Object Pooling:** Consider implementing object pooling for ItemSlotUI if you have many items
- **Grid Refresh:** The grid recreates all slots on refresh - optimize if needed for large inventories
- **Details Panel:** Hide details panel when not needed to reduce draw calls

### Future Enhancements

- **Filtering/Sorting:** Add buttons to filter by item type or sort by various criteria
- **Search:** Add search functionality for large inventories
- **Categories:** Add tab system for different item categories
- **Quick Actions:** Add keyboard shortcuts for common actions

### UI Polish

- **Animations:** Add smooth transitions for item movements
- **Sound Effects:** Add audio feedback for drag-drop, equip, etc.
- **Visual Feedback:** Enhance hover effects, selection highlights
- **Tooltips:** Enhance ItemDetailsPanel with more information

---

## Quick Reference: Component Checklist

- [ ] ItemSlotPrefab created and configured
- [ ] DragGhostPrefab created and assigned to ItemSlotPrefab
- [ ] ItemGridUI component added and configured
- [ ] GridLayoutGroup configured (8 columns)
- [ ] ScrollRect configured
- [ ] EquipmentSlotsPanel created with 6 slots
- [ ] EquipmentSlotUI components added to each slot
- [ ] CharacterPortrait Image created behind equipment slots
- [ ] CharacterPortrait assigned to EquipmentSlotsPanel component
- [ ] CharacterDefinition assets updated with InventoryTabPortrait sprites
- [ ] DropIndicator Image created for each equipment slot
- [ ] DropIndicator assigned to EquipmentSlotUI components
- [ ] DragGhostPrefab assigned to EquipmentSlotUI components
- [ ] ItemDetailsPanel created and configured
- [ ] ItemContextMenu created and configured
- [ ] MenuItemPrefab created
- [ ] ItemButtonStyle assets created (5 types)
- [ ] ItemButtonStyleManager created and configured
- [ ] TreasuryManager created
- [ ] CharacterNameText, AurumShardsText, WeightText created
- [ ] InventoryUI references assigned (all 3 text components)
- [ ] StatusMenuUI references verified
- [ ] All UI elements styled and positioned
- [ ] Tested in Play Mode

---

## Support

If you encounter issues not covered in this guide:
1. Check Unity Console for error messages
2. Verify all component references are assigned
3. Ensure all prefabs are properly configured
4. Check that CharacterState has inventory data
5. Verify EventSystem and Canvas are set up correctly

Good luck with your inventory system setup!
