# Inventory System - Issues and Fixes

This document tracks issues found during implementation and testing that need to be addressed.

---

## Issue #1: Loot System Architecture

**Status:** Needs Fix  
**Priority:** High  
**Date Identified:** After Part 3 Implementation

### Problem

The current loot system implementation has architectural issues:

1. **Currency Drops:** LootManager is rolling for currency drops independently (30% chance), but currency should ONLY come from enemy units if they actually have Aurum Shards in their inventory/currency.

2. **Loot Source:** LootManager is directly accessing enemy inventory (`enemy.Inventory`) to collect loot. This violates separation of concerns - enemies should manage their own loot tables and pass loot data to the LootManager.

3. **Loot Tables:** There's no loot table system. Enemies should have configurable loot tables (ScriptableObject or component) that define what they drop, with probability-based drops.

### Current Implementation Issues

**Location:** `Assets/Scripts/Combat/LootManager.cs`

**Problematic Code:**
```csharp
public void OnEnemyDeath(Unit enemy)
{
    // ❌ WRONG: Rolling for currency independently
    if (Random.value < 0.3f) // 30% chance
    {
        int currencyAmount = Random.Range(minCurrencyDrop, maxCurrencyDrop + 1);
        Debug.Log($"Enemy dropped {currencyAmount} Aurum Shards");
    }
    
    // ❌ WRONG: Directly accessing enemy inventory
    foreach (var slot in enemy.Inventory)
    {
        if (slot != null && slot.Item != null)
        {
            AddToBattleLoot(slot.Item, slot.Quantity);
        }
    }
}
```

### Correct Architecture

**Enemy-Controlled Loot:**
- Enemies should have a `LootTable` component or ScriptableObject
- Enemies generate their loot drops when they die
- Enemies pass loot data to LootManager (not inventory access)
- Currency only comes from enemy's actual AurumShards property

**Proposed Solution:**

1. **Create LootTable System:**
   - `LootTable` ScriptableObject with probability-based drops
   - `LootTableComponent` MonoBehaviour for per-enemy configuration
   - Each enemy can have multiple loot tables (guaranteed drops, random drops, etc.)

2. **Modify Enemy Death Flow:**
   ```
   Enemy Dies
     ↓
   Enemy.GenerateLoot() (uses LootTable)
     ↓
   Returns LootData (items + currency)
     ↓
   LootManager.AddLoot(LootData)
   ```

3. **LootManager Changes:**
   - Remove direct inventory access
   - Remove currency roll logic
   - Accept `LootData` struct/class from enemies
   - Only accumulate what enemies provide

### Implementation Plan

**Step 1: Create LootTable System**
- `Assets/Scripts/Items/LootTable.cs` - ScriptableObject
- `Assets/Scripts/Items/LootEntry.cs` - Serializable loot entry with probability
- `Assets/Scripts/Combat/LootData.cs` - Data structure for passing loot

**Step 2: Create Enemy Loot Component**
- `Assets/Scripts/Combat/EnemyLoot.cs` - Component for enemies
- References LootTable(s)
- Generates loot on death
- Passes to LootManager

**Step 3: Modify LootManager**
- Remove `OnEnemyDeath(Unit enemy)` method
- Add `AddLoot(LootData lootData)` method
- Remove currency roll logic
- Only handle accumulation and display

**Step 4: Modify TurnManager**
- Change `HandleUnitDied()` to call enemy's loot generation
- Enemy passes loot to LootManager

**Step 5: Update Unit/Enemy**
- Add `AurumShards` to loot if enemy has currency
- Or create currency item that can be dropped

### Code Structure Preview

```csharp
// LootEntry.cs
[System.Serializable]
public class LootEntry
{
    public Item item;
    [Range(0f, 1f)] public float dropChance = 1.0f;
    public int minQuantity = 1;
    public int maxQuantity = 1;
}

// LootTable.cs
[CreateAssetMenu(fileName = "New Loot Table", menuName = "Riftbourne/Items/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> guaranteedDrops = new List<LootEntry>();
    public List<LootEntry> randomDrops = new List<LootEntry>();
}

// LootData.cs
public struct LootData
{
    public List<InventorySlot> items;
    public int currency;
}

// EnemyLoot.cs
public class EnemyLoot : MonoBehaviour
{
    [SerializeField] private LootTable[] lootTables;
    [SerializeField] private bool dropCurrency = true;
    
    public LootData GenerateLoot()
    {
        LootData loot = new LootData();
        loot.items = new List<InventorySlot>();
        loot.currency = 0;
        
        // Generate from loot tables
        foreach (var table in lootTables)
        {
            // Process guaranteed drops
            // Process random drops (roll for each)
        }
        
        // Add currency if enabled
        if (dropCurrency)
        {
            Unit unit = GetComponent<Unit>();
            if (unit != null)
            {
                loot.currency = unit.AurumShards;
            }
        }
        
        return loot;
    }
}

// LootManager.cs (modified)
public void AddLoot(LootData lootData)
{
    // Add items
    foreach (var slot in lootData.items)
    {
        AddToBattleLoot(slot.Item, slot.Quantity);
    }
    
    // Add currency (if any)
    if (lootData.currency > 0)
    {
        // Store currency separately or create currency item
        // TODO: Decide on currency handling
    }
}
```

### Testing Requirements

After fix:
- [ ] Enemies with loot tables drop correct items
- [ ] Probability-based drops work correctly
- [ ] Currency only drops if enemy has AurumShards > 0
- [ ] No currency drops from enemies without currency
- [ ] Multiple loot tables per enemy work
- [ ] Guaranteed vs random drops work correctly

---

## Issue #2: Inventory Not Dynamic from CharacterData

**Status:** Needs Fix  
**Priority:** High  
**Date Identified:** After Part 3 Implementation

### Problem

The current inventory system is set statically in the Unit Inspector, but the game architecture uses a dynamic system where Units are created/informed by CharacterData (CharacterDefinition → CharacterState → Unit). Inventory should be populated dynamically from CharacterState when Units are created, not set manually in the Inspector.

**Current Flow:**
```
CharacterDefinition (ScriptableObject)
    ↓
CharacterState (Runtime - managed by PartyManager)
    ↓
Unit (Battle representation - created via UnitFactory)
```

**Current Issue:**
- Inventory is set manually in Unit Inspector (static)
- `Unit.UpdateFromCharacterState()` doesn't handle inventory
- `CharacterState` doesn't have inventory data
- Units created from CharacterState don't get inventory populated

### Current Implementation Issues

**Location:** `Assets/Scripts/Characters/Unit.cs`

**Problematic Code:**
```csharp
// Unit has inventory as SerializeField - set in Inspector
[SerializeField] private List<InventorySlot> inventory = new List<InventorySlot>();

// UpdateFromCharacterState() doesn't populate inventory
public void UpdateFromCharacterState(CharacterState state)
{
    // ... updates stats, equipment, skills ...
    // ❌ MISSING: Inventory population from CharacterState
}
```

**Location:** `Assets/Scripts/Characters/CharacterState.cs`

**Missing:**
- No inventory field in CharacterState
- No way to store/retrieve inventory from CharacterState

### Correct Architecture

**Inventory Should Flow:**
```
CharacterDefinition
    ↓ (starting inventory - optional)
CharacterState
    ↓ (runtime inventory - changes during gameplay)
Unit (battle representation)
    ↓ (syncs back to CharacterState after battle)
CharacterState (updated with battle changes)
```

**Proposed Solution:**

1. **Add Inventory to CharacterState:**
   - Add `List<InventorySlot> inventory` field
   - Add `List<InventorySlot> containerInventory` field
   - Add `ContainerItem[] containerSlots` field
   - Add `int aurumShards` field
   - Add inventory management methods (AddItem, RemoveItem, etc.)

2. **Add Starting Inventory to CharacterDefinition (Optional):**
   - Add `List<InventorySlot> startingInventory` field
   - Populated when CharacterState is first created from CharacterDefinition

3. **Modify Unit.UpdateFromCharacterState():**
   - Clear existing inventory
   - Copy inventory from CharacterState to Unit
   - Copy container inventory and slots
   - Copy AurumShards

4. **Modify Unit.ExportToCharacterState() (or create sync method):**
   - Copy inventory from Unit back to CharacterState after battle
   - Ensures inventory changes persist (items used, items gained, etc.)

5. **Modify UnitFactory:**
   - Ensure inventory is populated when creating Unit from CharacterState

### Implementation Plan

**Step 1: Add Inventory to CharacterState**
- `Assets/Scripts/Characters/CharacterState.cs`
- Add inventory fields (inventory, containerInventory, containerSlots, aurumShards)
- Add inventory management methods
- Initialize from CharacterDefinition starting inventory (if exists)

**Step 2: Add Starting Inventory to CharacterDefinition (Optional)**
- `Assets/Scripts/Characters/CharacterDefinition.cs`
- Add `startingInventory` field
- Add `startingAurumShards` field
- Used when creating new CharacterState

**Step 3: Modify Unit.UpdateFromCharacterState()**
- Clear Unit's inventory
- Copy inventory from CharacterState
- Copy container inventory
- Copy AurumShards
- Ensure all inventory data syncs

**Step 4: Create Unit.SyncInventoryToCharacterState()**
- Copy inventory from Unit back to CharacterState
- Called after battle ends or when returning to exploration
- Ensures inventory changes persist

**Step 5: Update CharacterStateFactory**
- When creating CharacterState from CharacterDefinition, initialize inventory from startingInventory

**Step 6: Update BattleSceneInitializer/UnitFactory**
- Ensure inventory syncs properly when creating Units

### Code Structure Preview

```csharp
// CharacterDefinition.cs (add)
[Header("Starting Inventory")]
[SerializeField] private List<InventorySlot> startingInventory = new List<InventorySlot>();
[SerializeField] private int startingAurumShards = 0;

public List<InventorySlot> StartingInventory => startingInventory;
public int StartingAurumShards => startingAurumShards;

// CharacterState.cs (add)
[Header("Inventory System")]
[SerializeField] private List<InventorySlot> inventory = new List<InventorySlot>();
[SerializeField] private List<InventorySlot> containerInventory = new List<InventorySlot>();
[SerializeField] private ContainerItem[] containerSlots = new ContainerItem[2];
[SerializeField] private int aurumShards = 0;

public List<InventorySlot> Inventory => inventory;
public List<InventorySlot> ContainerInventory => containerInventory;
public ContainerItem[] ContainerSlots => containerSlots;
public int AurumShards => aurumShards;

// Add inventory methods
public bool AddItem(Item item, int quantity = 1) { /* ... */ }
public bool RemoveItem(Item item, int quantity = 1) { /* ... */ }
public bool HasItem(Item item, int quantity = 1) { /* ... */ }
public void GainAurumShards(int amount) { /* ... */ }
public bool SpendAurumShards(int amount) { /* ... */ }

// Unit.cs (modify UpdateFromCharacterState)
public void UpdateFromCharacterState(CharacterState state)
{
    // ... existing code ...
    
    // ✅ ADD: Sync inventory
    inventory.Clear();
    foreach (var slot in state.Inventory)
    {
        if (slot != null && slot.Item != null)
        {
            inventory.Add(new InventorySlot(slot.Item, slot.Quantity));
        }
    }
    
    containerInventory.Clear();
    foreach (var slot in state.ContainerInventory)
    {
        if (slot != null && slot.Item != null)
        {
            containerInventory.Add(new InventorySlot(slot.Item, slot.Quantity));
        }
    }
    
    // Copy container slots
    for (int i = 0; i < containerSlots.Length && i < state.ContainerSlots.Length; i++)
    {
        containerSlots[i] = state.ContainerSlots[i];
    }
    
    // Copy currency
    aurumShards = state.AurumShards;
}

// Unit.cs (add new method)
public void SyncInventoryToCharacterState()
{
    if (characterState == null) return;
    
    // Copy inventory back to CharacterState
    characterState.inventory.Clear();
    foreach (var slot in inventory)
    {
        if (slot != null && slot.Item != null)
        {
            characterState.inventory.Add(new InventorySlot(slot.Item, slot.Quantity));
        }
    }
    
    // Copy container inventory
    characterState.containerInventory.Clear();
    foreach (var slot in containerInventory)
    {
        if (slot != null && slot.Item != null)
        {
            characterState.containerInventory.Add(new InventorySlot(slot.Item, slot.Quantity));
        }
    }
    
    // Copy container slots
    for (int i = 0; i < characterState.containerSlots.Length && i < containerSlots.Length; i++)
    {
        characterState.containerSlots[i] = containerSlots[i];
    }
    
    // Copy currency
    characterState.aurumShards = aurumShards;
}
```

### Testing Requirements

After fix:
- [ ] CharacterState has inventory fields
- [ ] CharacterDefinition can define starting inventory
- [ ] Units created from CharacterState have inventory populated
- [ ] Inventory changes in battle sync back to CharacterState
- [ ] Currency changes persist
- [ ] Container inventory syncs correctly
- [ ] Items used in battle are removed from CharacterState
- [ ] Items gained in battle are added to CharacterState
- [ ] Multiple battles don't duplicate inventory

### Related Systems

This fix affects:
- **Merchant System:** Should work with CharacterState inventory, not Unit inventory directly
- **Loot System:** Loot should be added to CharacterState, not Unit
- **Inventory UI:** Should display CharacterState inventory (or Unit inventory that syncs)
- **Save System:** CharacterState inventory should be saved/loaded

---

## Future Issues

*Additional issues will be added here as they are discovered.*

---

**Document Version:** 1.0  
**Last Updated:** After Part 3 Implementation  
**Related:** INVENTORY_ECOSYSTEM_PART3_DOCUMENTATION.md
