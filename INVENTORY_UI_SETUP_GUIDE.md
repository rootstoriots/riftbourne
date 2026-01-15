# Inventory UI Setup Guide - CORRECTED

This guide provides detailed step-by-step instructions for setting up the Inventory UI **integrated into the StatusMenuUI system**.

## Important: Integration with StatusMenuUI

The Inventory UI is **NOT** a standalone system. It is integrated into the **StatusMenuUI's Inventory Tab**. You access it by:
1. Pressing **TAB** to open the Status Menu
2. Clicking the **Inventory** tab button

## Prerequisites

- StatusMenuUI is already set up in your scene
- StatusMenuUI has an `inventoryTabPanel` GameObject
- TextMeshPro is imported
- You have a scene with StatusMenuUI working

## Part 1: Locate StatusMenuUI Structure

### Step 1.1: Find StatusMenuUI in Scene

1. In Hierarchy, search for "StatusMenu" or look for the StatusMenuUI GameObject
2. The structure should look like:
   ```
   StatusMenuCanvas (or similar)
   ├── StatusMenuPanel
   │   ├── [Tab Buttons]
   │   ├── StatusTabPanel
   │   ├── EquipmentTabPanel
   │   ├── SkillsTabPanel
   │   ├── InventoryTabPanel  ← THIS IS WHERE WE WORK
   │   ├── JournalTabPanel
   │   └── MapTabPanel
   └── StatusMenuUI (component)
   ```

### Step 1.2: Verify InventoryTabPanel Exists

1. Expand StatusMenuPanel in Hierarchy
2. Look for **InventoryTabPanel** GameObject
3. If it doesn't exist:
   - Right-click on StatusMenuPanel
   - Select **Create Empty**
   - Rename to "InventoryTabPanel"
   - This panel should be a child of StatusMenuPanel

---

## Part 2: Create UI Elements Inside InventoryTabPanel

### Step 2.1: Create Stats Text (Top Section)

1. Right-click on **InventoryTabPanel** in Hierarchy
2. Select **UI > Text - TextMeshPro**
3. If prompted to import TextMeshPro essentials, click **Import TMP Essentials**
4. Rename it to "InventoryStatsText"

### Step 2.2: Configure StatsText RectTransform

1. Select **InventoryStatsText** in Hierarchy
2. In Inspector, find **Rect Transform** component
3. Set **Anchor Presets**:
   - Click anchor preset button
   - Select **top-left** (without Alt)
4. Set position and size:
   - **Pos X**: 20
   - **Pos Y**: -20 (negative because Y is measured from top)
   - **Width**: 400
   - **Height**: 150
   - **Pivot**: (0, 1) - top-left

### Step 2.3: Configure StatsText TextMeshPro Component

1. Still on **InventoryStatsText**, find **TextMeshPro - Text (UI)** component
2. Set text properties:
   - **Text**: Leave empty (will be set by script)
   - **Font Size**: 18
   - **Alignment**: Top Left
   - **Color**: White (R: 255, G: 255, B: 255, A: 255)
   - **Font Style**: Normal
3. **Enable Rich Text** (critical for color tags and bold text)
4. Set **Overflow**: Overflow

### Step 2.4: Create Inventory Text (Main Display)

1. Right-click on **InventoryTabPanel** in Hierarchy
2. Select **UI > Text - TextMeshPro**
3. Rename it to "InventoryItemsText"

### Step 2.5: Configure InventoryText RectTransform

1. Select **InventoryItemsText** in Hierarchy
2. In Inspector, find **Rect Transform** component
3. Set **Anchor Presets**: **top-left**
4. Set position and size:
   - **Pos X**: 20
   - **Pos Y**: -180 (below StatsText)
   - **Width**: 800 (or adjust to fit panel)
   - **Height**: 500 (or adjust based on panel size)
   - **Pivot**: (0, 1) - top-left

### Step 2.6: Configure InventoryText TextMeshPro Component

1. Still on **InventoryItemsText**, find **TextMeshPro - Text (UI)** component
2. Set text properties:
   - **Text**: Leave empty
   - **Font Size**: 16
   - **Alignment**: Top Left
   - **Color**: White
   - **Font Style**: Normal
3. **Enable Rich Text** (critical for rarity colors!)
4. Set **Overflow**: Overflow
5. Enable **Word Wrapping**

### Step 2.7: Optional - Add ScrollView for Long Lists

If you expect many items:

1. Right-click on **InventoryTabPanel**, select **UI > Scroll View**
2. Rename to "InventoryScrollView"
3. Set RectTransform:
   - **Anchor**: top-left
   - **Pos X**: 20
   - **Pos Y**: -180
   - **Width**: 800
   - **Height**: 500
4. Expand ScrollView in Hierarchy:
   - Find **Viewport** > **Content**
5. **Move InventoryItemsText**:
   - Drag **InventoryItemsText** from InventoryTabPanel to **Content** (under Viewport)
6. Configure Content:
   - Set **Content** RectTransform height to 1000 (or large value for scrolling)

**Note:** If using ScrollView, the InventoryItemsText will automatically scroll when content exceeds the viewport height.

---

## Part 3: Add InventoryUI Component

### Step 3.1: Add Component to InventoryTabPanel

1. Select **InventoryTabPanel** GameObject in Hierarchy
2. In Inspector, click **Add Component**
3. Search for "InventoryUI"
4. Click to add the component

### Step 3.2: Assign References

1. Still on **InventoryTabPanel**, find **Inventory UI** component
2. You'll see two fields:
   - **Inventory Text** (TextMeshProUGUI)
   - **Stats Text** (TextMeshProUGUI)

3. **Assign Inventory Text:**
   - Drag **InventoryItemsText** from Hierarchy into the **Inventory Text** field
   - (If using ScrollView, still drag InventoryItemsText, not Content)

4. **Assign Stats Text:**
   - Drag **InventoryStatsText** from Hierarchy into the **Stats Text** field

### Step 3.3: Verify Component Setup

Your InventoryUI component should look like:
```
Inventory UI (Script)
├── Inventory Text: InventoryItemsText (TextMeshProUGUI)
└── Stats Text: InventoryStatsText (TextMeshProUGUI)
```

**Important:** There is NO "Inventory Panel" field anymore - the component is ON the panel itself.

---

## Part 4: Connect to StatusMenuUI

### Step 4.1: Find StatusMenuUI Component

1. Find the GameObject that has the **StatusMenuUI** component
2. This is usually on a parent GameObject (like "StatusMenuCanvas" or "StatusMenuUI")
3. Select that GameObject

### Step 4.2: Assign InventoryUI Reference

1. In Inspector, find **Status Menu UI** component
2. Scroll down to find **Inventory Tab** section
3. You should see:
   - **Inventory UI** field (currently empty)

4. **Assign Inventory UI:**
   - Drag **InventoryTabPanel** from Hierarchy into the **Inventory UI** field
   - (The InventoryUI component is on InventoryTabPanel, so dragging the panel assigns the component)

### Step 4.3: Verify StatusMenuUI Setup

Your StatusMenuUI component should have:
```
Status Menu UI (Script)
├── Tab Panels
│   └── Inventory Tab Panel: InventoryTabPanel (GameObject)
└── Inventory Tab
    └── Inventory UI: InventoryTabPanel (InventoryUI component)
```

---

## Part 5: Test the Setup

### Step 5.1: Ensure Scene Has PartyManager and Units

1. Check Hierarchy for **PartyManager** GameObject
2. Verify there are Units in the scene with items in inventory
3. If not, add items to a Unit's inventory in Inspector

### Step 5.2: Test Inventory Display

1. Press **Play** in Unity
2. Press **TAB** key to open Status Menu
3. Click the **Inventory** tab button
4. **Expected Results:**
   - InventoryTabPanel should become visible
   - StatsText should show:
     - Unit name (bold)
     - Aurum Shards amount
     - Weight / Capacity
     - Encumbrance percentage
   - InventoryText should show:
     - "=== Main Inventory ==="
     - List of items with:
       - Rarity colors (white/green/blue/purple/orange)
       - Item names
       - Quantities (x5)
       - Weights (X.XX kg)
     - "=== Containers ===" section
     - Container slots status

### Step 5.3: Test Tab Switching

1. Click other tabs (Status, Equipment, etc.)
2. Click Inventory tab again
3. Verify inventory refreshes each time you switch to it
4. Verify it shows the currently selected character

---

## Part 6: Troubleshooting

### Issue: "Inventory Tab shows nothing"

**Solutions:**
- Verify InventoryTabPanel has InventoryUI component
- Check that StatusMenuUI has InventoryUI reference assigned
- Verify InventoryItemsText and InventoryStatsText references are assigned
- Check Console for errors
- Ensure units have items in inventory

### Issue: "Text doesn't appear"

**Solutions:**
- Verify TextMeshPro fonts are imported
- Check Rich Text is enabled on both text components
- Verify text components are children of InventoryTabPanel
- Check RectTransform positions and sizes
- Ensure InventoryTabPanel is active when Status Menu is open

### Issue: "Rarity colors don't show"

**Solutions:**
- Enable Rich Text on InventoryItemsText
- Verify items have Rarity set (not None)
- Check GetRarityColorTag() is working (check Console)

### Issue: "Inventory doesn't refresh when switching characters"

**Solutions:**
- Verify StatusMenuUI is calling RefreshInventoryTab()
- Check that currentUnit/currentCharacterState is being passed
- Verify InventoryUI.RefreshDisplay() is being called

### Issue: "Can't find InventoryTabPanel"

**Solutions:**
- Check it's a child of StatusMenuPanel
- Verify it's named exactly "InventoryTabPanel"
- Check it's not disabled
- Look in StatusMenuPanel's children in Hierarchy

### Issue: "StatusMenuUI doesn't have Inventory UI field"

**Solutions:**
- Verify you're using the updated StatusMenuUI.cs
- Check the script compiled without errors
- Look for "Inventory Tab" section in StatusMenuUI Inspector
- The field should be under [Header("Inventory Tab")]

---

## Part 7: Final Checklist

Before considering setup complete, verify:

- [ ] InventoryTabPanel exists as child of StatusMenuPanel
- [ ] InventoryStatsText exists inside InventoryTabPanel
- [ ] InventoryItemsText exists inside InventoryTabPanel
- [ ] Both text components have Rich Text enabled
- [ ] InventoryUI component is on InventoryTabPanel
- [ ] InventoryUI has both text references assigned
- [ ] StatusMenuUI has InventoryUI reference assigned
- [ ] StatusMenuUI has InventoryTabPanel reference assigned
- [ ] Pressing TAB opens Status Menu
- [ ] Clicking Inventory tab shows inventory
- [ ] Items display with correct names and quantities
- [ ] Rarity colors display correctly
- [ ] Stats display correctly (name, currency, weight)
- [ ] No errors in Console

---

## Quick Reference: Hierarchy Structure

```
StatusMenuCanvas (or StatusMenuUI GameObject)
├── StatusMenuPanel
│   ├── [Tab Buttons]
│   ├── StatusTabPanel
│   ├── EquipmentTabPanel
│   ├── SkillsTabPanel
│   ├── InventoryTabPanel ← InventoryUI component here
│   │   ├── InventoryStatsText (TextMeshProUGUI)
│   │   └── InventoryItemsText (TextMeshProUGUI)
│   ├── JournalTabPanel
│   └── MapTabPanel
└── StatusMenuUI (component)
    └── References:
        ├── Inventory Tab Panel → InventoryTabPanel
        └── Inventory UI → InventoryTabPanel (with InventoryUI component)
```

---

## Key Differences from Standalone Setup

**What Changed:**
- ❌ NO standalone Canvas needed (uses StatusMenuUI's Canvas)
- ❌ NO standalone InventoryPanel (uses InventoryTabPanel)
- ❌ NO 'I' key functionality (uses TAB + Inventory tab)
- ❌ NO InventoryUI GameObject (component is on InventoryTabPanel)
- ✅ Integrated into StatusMenuUI system
- ✅ Works with tab system
- ✅ Respects StatusMenuUI's pause/time scale

**How to Access:**
1. Press **TAB** → Opens Status Menu
2. Click **Inventory** tab → Shows inventory

---

## Next Steps

After setup:
1. Test with different characters
2. Test with different item types and rarities
3. Test with empty inventory
4. Test with overencumbered units
5. Verify inventory updates when items change
6. Consider future enhancements (icons, tooltips, etc.)

---

**Document Version:** 2.0 (Corrected for StatusMenuUI Integration)  
**Last Updated:** After Integration Fix  
**Related:** INVENTORY_ECOSYSTEM_PART3_DOCUMENTATION.md
