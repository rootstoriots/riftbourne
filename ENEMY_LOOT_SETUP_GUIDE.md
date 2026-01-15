# Enemy Loot Setup Guide

## Overview

This guide explains how to configure loot drops for enemies in Riftbourne. The loot system uses **LootTable** ScriptableObjects to define what items enemies drop when defeated, and the **EnemyLoot** component to attach loot tables to enemy prefabs.

## Table of Contents

1. [Loot System Architecture](#loot-system-architecture)
2. [Creating Loot Tables](#creating-loot-tables)
3. [Configuring Loot Entries](#configuring-loot-entries)
4. [Applying Loot to Enemies](#applying-loot-to-enemies)
5. [Currency Drops](#currency-drops)
6. [Best Practices](#best-practices)
7. [Examples](#examples)
8. [Troubleshooting](#troubleshooting)

---

## Loot System Architecture

### How Loot Works

When an enemy dies in combat:

1. **TurnManager** detects the enemy's death
2. **TurnManager** checks for an **EnemyLoot** component on the enemy
3. **EnemyLoot.GenerateLoot()** is called, which:
   - Processes all assigned **LootTable** ScriptableObjects
   - Generates items from guaranteed and random drops
   - Adds currency from the enemy's `AurumShards` property (if enabled)
4. **LootData** is passed to **LootManager**
5. **LootManager** accumulates all loot from all defeated enemies
6. After battle, **LootSelectionUI** displays all collected loot for the player to claim

### Key Components

- **LootTable** (ScriptableObject): Defines a collection of items that can drop
- **LootEntry** (Serializable Class): Defines a single item with drop probability and quantity range
- **EnemyLoot** (MonoBehaviour Component): Attached to enemy prefabs, references LootTable(s)
- **LootData** (Struct): Data structure passed from EnemyLoot to LootManager
- **LootManager** (MonoBehaviour Singleton): Accumulates and manages battle loot

### Important Notes

- **Enemy inventory items are NOT automatically dropped** - only items from LootTables are dropped
- **Currency drops** come from the enemy's `AurumShards` property, not from inventory
- **Multiple LootTables** can be assigned to a single enemy (all are processed)
- **Guaranteed drops** always drop (if item is valid)
- **Random drops** roll for probability on each enemy death

---

## Creating Loot Tables

### Step 1: Create LootTable Asset

1. **Right-click in Project window:**
   - Navigate to where you want to store loot tables (e.g., `Assets/Resources/LootTables/`)
   - Right-click → `Create → Riftbourne → Items → Loot Table`
   - Name it: `LootTable_[EnemyType]` (e.g., `LootTable_Goblin`, `LootTable_OrcWarrior`)

2. **LootTable Inspector:**
   - The asset will have two sections:
     - **Guaranteed Drops**: Items that always drop
     - **Random Drops**: Items that have a chance to drop

### Step 2: Add Guaranteed Drops

**Guaranteed drops** are items that will **always** drop when the enemy dies (assuming the item is valid).

1. **Expand "Guaranteed Drops" section**
2. **Click "+" to add a new LootEntry**
3. **Configure the LootEntry:**
   - **Item**: Drag an Item asset (weapon, consumable, accessory, etc.)
   - **Drop Chance**: Leave at `1.0` (100%) for guaranteed drops
   - **Min Quantity**: Minimum quantity that drops (e.g., `1`)
   - **Max Quantity**: Maximum quantity that drops (e.g., `3`)

**Example:**
- **Item**: `Consumable_Bandage`
- **Drop Chance**: `1.0` (100%)
- **Min Quantity**: `1`
- **Max Quantity**: `2`
- **Result**: Enemy always drops 1-2 Bandages

### Step 3: Add Random Drops

**Random drops** are items that have a **probability** of dropping when the enemy dies.

1. **Expand "Random Drops" section**
2. **Click "+" to add a new LootEntry**
3. **Configure the LootEntry:**
   - **Item**: Drag an Item asset
   - **Drop Chance**: Probability of drop (0.0 to 1.0)
     - `0.1` = 10% chance
     - `0.25` = 25% chance
     - `0.5` = 50% chance
     - `0.75` = 75% chance
     - `1.0` = 100% chance (same as guaranteed)
   - **Min Quantity**: Minimum quantity if drop succeeds
   - **Max Quantity**: Maximum quantity if drop succeeds

**Example:**
- **Item**: `Accessory_VitalityRing`
- **Drop Chance**: `0.15` (15% chance)
- **Min Quantity**: `1`
- **Max Quantity**: `1`
- **Result**: 15% chance to drop 1 Vitality Ring

### Step 4: Multiple Loot Entries

You can add multiple entries to both Guaranteed and Random drops:

**Guaranteed Drops:**
- Entry 1: Always drops 1-2 Bandages
- Entry 2: Always drops 1 Gold Coin

**Random Drops:**
- Entry 1: 25% chance to drop 1 Health Potion
- Entry 2: 10% chance to drop 1 Rare Weapon
- Entry 3: 50% chance to drop 1-3 Arrows

**Note:** Each random drop rolls independently, so multiple items can drop from the same enemy.

---

## Configuring Loot Entries

### LootEntry Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| **Item** | Item (ScriptableObject) | The item that can drop | `Consumable_Bandage`, `Weapon_Sword` |
| **Drop Chance** | Float (0.0-1.0) | Probability of drop | `0.25` = 25% chance |
| **Min Quantity** | Int | Minimum quantity if drop succeeds | `1` |
| **Max Quantity** | Int | Maximum quantity if drop succeeds | `3` |

### Drop Chance Guidelines

- **Guaranteed Drops**: Always use `1.0` (100%)
- **Common Items**: `0.5` - `0.75` (50-75% chance)
- **Uncommon Items**: `0.25` - `0.5` (25-50% chance)
- **Rare Items**: `0.1` - `0.25` (10-25% chance)
- **Very Rare Items**: `0.05` - `0.1` (5-10% chance)
- **Legendary Items**: `0.01` - `0.05` (1-5% chance)

### Quantity Guidelines

- **Consumables**: Usually `1-3` or `1-5`
- **Weapons/Armor**: Usually `1` (rarely stackable)
- **Currency Items**: `1-10` or `5-20` depending on value
- **Materials**: `1-5` or `3-10` depending on rarity

### Stack Size Limitation

The loot system automatically clamps quantities to the item's `MaxStackSize`. For example:
- If an item has `MaxStackSize = 5`
- And you set `Min Quantity = 1`, `Max Quantity = 10`
- The actual drop will be `1-5` (clamped to max stack size)

---

## Applying Loot to Enemies

### Step 1: Add EnemyLoot Component

1. **Select enemy prefab** in Project window (or instance in scene)
2. **Add Component:**
   - Click `Add Component` button
   - Search for `EnemyLoot`
   - Add `EnemyLoot` component

### Step 2: Assign Loot Tables

1. **In EnemyLoot component:**
   - Find **"Loot Tables"** section
   - **Size**: Set to number of loot tables you want (can be 0, 1, or multiple)
   - **Element 0, 1, 2, etc.**: Drag LootTable assets into each slot

2. **Multiple Loot Tables:**
   - You can assign multiple LootTables to a single enemy
   - All tables are processed when the enemy dies
   - Useful for organizing loot (e.g., "Common Loot", "Rare Loot", "Boss Loot")

**Example:**
- **Loot Tables Size**: `2`
- **Element 0**: `LootTable_Goblin_Common`
- **Element 1**: `LootTable_Goblin_Rare`
- **Result**: Enemy drops items from both tables

### Step 3: Configure Currency Drops

1. **In EnemyLoot component:**
   - Find **"Currency Settings"** section
   - **Drop Currency**: Checkbox to enable/disable currency drops
     - ✅ **Checked**: Enemy drops all `AurumShards` it has when killed
     - ❌ **Unchecked**: Enemy does not drop currency

2. **Set Enemy Currency:**
   - Select the enemy prefab
   - In **Unit component**, set **Aurum Shards** field
   - This is the amount of currency the enemy will drop (if `Drop Currency` is enabled)

**Example:**
- **Drop Currency**: ✅ Enabled
- **Unit.AurumShards**: `50`
- **Result**: Enemy drops 50 Aurum Shards when killed

### Step 4: Save Prefab

1. **If editing prefab instance in scene:**
   - Click **"Overrides"** dropdown at top of Inspector
   - Select **"Apply All"** to save changes to prefab

2. **If editing prefab directly:**
   - Changes are automatically saved

---

## Currency Drops

### How Currency Works

- **Currency Source**: Enemy's `Unit.AurumShards` property
- **Drop Control**: `EnemyLoot.DropCurrency` checkbox
- **Amount**: Whatever the enemy has in `AurumShards` (not configurable in EnemyLoot)

### Setting Enemy Currency

1. **Select enemy prefab**
2. **In Unit component:**
   - Find **"Aurum Shards"** field
   - Set value (e.g., `25`, `50`, `100`)
3. **In EnemyLoot component:**
   - Ensure **"Drop Currency"** is checked ✅

### Currency Guidelines

- **Weak Enemies**: 10-25 Aurum Shards
- **Normal Enemies**: 25-50 Aurum Shards
- **Elite Enemies**: 50-100 Aurum Shards
- **Bosses**: 100-500+ Aurum Shards

### Disabling Currency Drops

If you want an enemy to **not** drop currency:
- Set **"Drop Currency"** to ❌ **Unchecked** in EnemyLoot component
- The enemy's `AurumShards` will be ignored (not dropped)

---

## Best Practices

### Organization

**Folder Structure:**
```
Assets/
├── Resources/
│   └── LootTables/
│       ├── Common/
│       │   ├── LootTable_Goblin_Common.asset
│       │   ├── LootTable_Orc_Common.asset
│       │   └── LootTable_Bandit_Common.asset
│       ├── Rare/
│       │   ├── LootTable_Goblin_Rare.asset
│       │   └── LootTable_Orc_Rare.asset
│       └── Bosses/
│           ├── LootTable_Boss_GoblinChieftain.asset
│           └── LootTable_Boss_OrcWarlord.asset
```

### Naming Conventions

**LootTables:**
- `LootTable_[EnemyType]_[Rarity]` (e.g., `LootTable_Goblin_Common`)
- `LootTable_Boss_[BossName]` (e.g., `LootTable_Boss_GoblinChieftain`)

**EnemyLoot Components:**
- No specific naming needed (component name is automatic)
- Ensure prefab name is clear (e.g., `Enemy_GoblinWarrior`)

### Loot Balance

**Early Game Enemies:**
- Guaranteed: 1-2 consumables (bandages, basic potions)
- Random: 10-25% chance for basic weapons/armor
- Currency: 10-25 Aurum Shards

**Mid Game Enemies:**
- Guaranteed: 1-3 consumables, sometimes basic materials
- Random: 15-30% chance for improved weapons/armor
- Currency: 25-50 Aurum Shards

**Late Game Enemies:**
- Guaranteed: 2-5 consumables, materials
- Random: 20-40% chance for advanced weapons/armor, accessories
- Currency: 50-100 Aurum Shards

**Bosses:**
- Guaranteed: Multiple consumables, materials, sometimes unique items
- Random: 30-50% chance for rare/legendary items
- Currency: 100-500+ Aurum Shards

### Reusability

**Create Reusable LootTables:**
- `LootTable_Common_Consumables` - Used by multiple enemy types
- `LootTable_Common_Materials` - Shared material drops
- `LootTable_Rare_Weapons` - Rare weapon pool

**Assign Multiple Tables:**
- Enemy can reference both `LootTable_Common_Consumables` and `LootTable_Goblin_Specific`
- Reduces duplication and makes updates easier

### Testing

**Test Loot Drops:**
1. Enter Play Mode
2. Defeat an enemy with EnemyLoot configured
3. Check Console for loot generation logs
4. After battle, verify loot appears in LootSelectionUI
5. Test multiple times to verify probability-based drops

---

## Examples

### Example 1: Basic Goblin Warrior

**LootTable Setup:**
1. Create `LootTable_Goblin_Basic`
2. **Guaranteed Drops:**
   - Entry 1: `Consumable_Bandage`, Drop Chance: `1.0`, Quantity: `1-2`
3. **Random Drops:**
   - Entry 1: `Dagger_ScoutsDirk`, Drop Chance: `0.15`, Quantity: `1`
   - Entry 2: `Accessory_VitalityRing`, Drop Chance: `0.1`, Quantity: `1`

**EnemyLoot Setup:**
1. Add `EnemyLoot` component to `Enemy_GoblinWarrior` prefab
2. **Loot Tables:**
   - Size: `1`
   - Element 0: `LootTable_Goblin_Basic`
3. **Currency Settings:**
   - Drop Currency: ✅ Enabled
4. **Unit Component:**
   - Aurum Shards: `25`

**Result:**
- Always drops 1-2 Bandages
- 15% chance to drop Scout's Dirk
- 10% chance to drop Vitality Ring
- Always drops 25 Aurum Shards

---

### Example 2: Elite Orc with Multiple Loot Tables

**LootTable Setup:**
1. Create `LootTable_Orc_Common`
   - **Guaranteed:** 1-3 Bandages
   - **Random:** 20% chance for basic weapon
2. Create `LootTable_Orc_Rare`
   - **Random:** 10% chance for rare weapon, 5% chance for legendary accessory

**EnemyLoot Setup:**
1. Add `EnemyLoot` component to `Enemy_OrcWarrior_Elite` prefab
2. **Loot Tables:**
   - Size: `2`
   - Element 0: `LootTable_Orc_Common`
   - Element 1: `LootTable_Orc_Rare`
3. **Currency Settings:**
   - Drop Currency: ✅ Enabled
4. **Unit Component:**
   - Aurum Shards: `75`

**Result:**
- Always drops 1-3 Bandages
- 20% chance to drop basic weapon (from Common table)
- 10% chance to drop rare weapon (from Rare table)
- 5% chance to drop legendary accessory (from Rare table)
- Always drops 75 Aurum Shards

---

### Example 3: Boss with Rich Loot

**LootTable Setup:**
1. Create `LootTable_Boss_GoblinChieftain`
2. **Guaranteed Drops:**
   - Entry 1: `Consumable_Bandage`, Drop Chance: `1.0`, Quantity: `5-10`
   - Entry 2: `Item_Material_Rare`, Drop Chance: `1.0`, Quantity: `3-5`
   - Entry 3: `Accessory_BossUnique`, Drop Chance: `1.0`, Quantity: `1`
3. **Random Drops:**
   - Entry 1: `Weapon_Legendary`, Drop Chance: `0.3`, Quantity: `1`
   - Entry 2: `Armor_Legendary`, Drop Chance: `0.25`, Quantity: `1`
   - Entry 3: `Codex_Rare`, Drop Chance: `0.2`, Quantity: `1`

**EnemyLoot Setup:**
1. Add `EnemyLoot` component to `Boss_GoblinChieftain` prefab
2. **Loot Tables:**
   - Size: `1`
   - Element 0: `LootTable_Boss_GoblinChieftain`
3. **Currency Settings:**
   - Drop Currency: ✅ Enabled
4. **Unit Component:**
   - Aurum Shards: `250`

**Result:**
- Always drops 5-10 Bandages
- Always drops 3-5 Rare Materials
- Always drops 1 Unique Boss Accessory
- 30% chance to drop Legendary Weapon
- 25% chance to drop Legendary Armor
- 20% chance to drop Rare Codex
- Always drops 250 Aurum Shards

---

### Example 4: Enemy Without Loot

**EnemyLoot Setup:**
1. Add `EnemyLoot` component to enemy prefab
2. **Loot Tables:**
   - Size: `0` (no loot tables)
3. **Currency Settings:**
   - Drop Currency: ❌ Disabled

**Result:**
- Enemy drops nothing when killed
- Useful for environmental hazards, summoned minions, etc.

---

## Troubleshooting

### Q: Enemy doesn't drop any loot

**Check:**
1. ✅ Enemy prefab has `EnemyLoot` component
2. ✅ `EnemyLoot` has at least one LootTable assigned (Size > 0)
3. ✅ LootTable has at least one LootEntry with a valid Item
4. ✅ For random drops, verify drop chance > 0
5. ✅ Check Console for error messages

**Debug:**
- Check Console logs when enemy dies - should see "Adding loot to battle loot pool..."
- Verify `TurnManager.HandleUnitDied()` is calling `EnemyLoot.GenerateLoot()`

---

### Q: Enemy drops items but not currency

**Check:**
1. ✅ `EnemyLoot.DropCurrency` is checked ✅
2. ✅ Enemy's `Unit.AurumShards` is > 0
3. ✅ Enemy actually died (not just defeated)

**Debug:**
- Check Console for "Added X Aurum Shards to battle loot" message
- Verify enemy's Unit component has AurumShards set

---

### Q: Guaranteed drops aren't dropping

**Check:**
1. ✅ LootEntry has valid Item assigned (not null)
2. ✅ Drop Chance is `1.0` (100%)
3. ✅ Min Quantity > 0
4. ✅ Item asset exists and is valid

**Debug:**
- Check LootTable asset in Inspector - verify Item fields are assigned
- Test with a simple guaranteed drop (100% chance, quantity 1)

---

### Q: Random drops never drop (even after many kills)

**Check:**
1. ✅ Drop Chance > 0 (not 0.0)
2. ✅ Item is valid (not null)
3. ✅ You've tested enough times (low drop chances require many attempts)

**Debug:**
- Temporarily set Drop Chance to `1.0` to test if item drops
- Check Console for loot generation logs
- Verify LootEntry.Item is assigned in LootTable asset

---

### Q: Multiple LootTables not working

**Check:**
1. ✅ `EnemyLoot.LootTables` array has multiple entries
2. ✅ All LootTable assets are assigned (not null)
3. ✅ All LootTables have valid LootEntries

**Debug:**
- Check `EnemyLoot.GenerateLoot()` processes all tables in a loop
- Verify each LootTable asset exists and is valid

---

### Q: Loot appears in Console but not in LootSelectionUI

**This is a different issue:**
- Loot generation is working (EnemyLoot → LootManager)
- Issue is with LootSelectionUI display
- Check `BattleEndHandler.VictoryFlowCoroutine()` calls `LootSelectionUI.ShowLoot()`
- Verify LootSelectionUI is found and activated

---

### Q: Quantity is wrong (always 1, or clamped incorrectly)

**Check:**
1. ✅ Min Quantity ≤ Max Quantity
2. ✅ Max Quantity doesn't exceed item's MaxStackSize
3. ✅ Item's MaxStackSize is set correctly

**Note:** System automatically clamps quantity to `item.MaxStackSize`

---

## Quick Reference Checklist

### Creating a LootTable
- [ ] Right-click → Create → Riftbourne → Items → Loot Table
- [ ] Name it: `LootTable_[EnemyType]`
- [ ] Add Guaranteed Drops (Drop Chance = 1.0)
- [ ] Add Random Drops (Drop Chance = 0.0-1.0)
- [ ] Set Min/Max Quantity for each entry
- [ ] Assign valid Item assets

### Applying Loot to Enemy
- [ ] Add `EnemyLoot` component to enemy prefab
- [ ] Set Loot Tables Size (0, 1, or more)
- [ ] Drag LootTable assets into array
- [ ] Configure Drop Currency checkbox
- [ ] Set enemy's Unit.AurumShards (if dropping currency)
- [ ] Save prefab

### Testing
- [ ] Enter Play Mode
- [ ] Defeat enemy with EnemyLoot
- [ ] Check Console for loot logs
- [ ] Verify loot appears in LootSelectionUI after battle
- [ ] Test multiple times for probability-based drops

---

## Next Steps

1. Create base LootTables for common enemy types
2. Create variant LootTables (Common, Rare, Boss)
3. Apply EnemyLoot to all enemy prefabs
4. Balance drop rates and quantities through playtesting
5. Create unique loot tables for special enemies/bosses

---

## Related Documentation

- [Enemy Creation Guide](ENEMY_CREATION_GUIDE.md) - How to create enemy prefabs
- [Battle Bookends Setup Guide](BATTLE_BOOKENDS_SETUP_GUIDE.md) - How loot is displayed after battle
- [Inventory System Documentation](INVENTORY_ECOSYSTEM_PART3_DOCUMENTATION.md) - How items and inventory work
