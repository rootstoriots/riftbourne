# Faction and AI System Setup Guide

This guide explains how to configure and use the Faction, Unit Type, and AI Behavior systems in Unity.

## Table of Contents
1. [Faction System Setup](#faction-system-setup)
2. [Unit Type Configuration](#unit-type-configuration)
3. [AI Behavior Configuration](#ai-behavior-configuration)
4. [Complete Setup Example](#complete-setup-example)

---

## Faction System Setup

### Step 1: Create Faction ScriptableObjects (Optional but Recommended)

1. **Create Faction Data Assets:**
   - Right-click in Project window
   - Select: `Create > Riftbourne > Faction Data`
   - Create one for each custom faction you want (e.g., "PlayerFaction", "OrcFaction", "ElfFaction")
   - Name them descriptively

2. **Configure Each Faction:**
   - Select a Faction Data asset
   - In Inspector:
     - Set `Faction Name` (display name)
     - Set `Description` (optional)
     - Set `Faction Color` (for UI/minimap)
     - Check `Is Player Faction` if this is the player's faction
     - Check `Is Neutral By Default` if this faction should be neutral

3. **Create Faction Registry:**
   - Right-click in Project window
   - Select: `Create > Riftbourne > Faction Registry`
   - Name it "FactionRegistry"
   - **Important:** Place it in a `Resources` folder (create one if needed: `Assets/Resources/FactionRegistry.asset`)
   - In Inspector:
     - Add all your Faction Data assets to "Registered Factions" list
     - Optionally assign `Player Faction` → your player faction asset (for convenience - can also be identified by IsPlayerFaction flag)
   - **Note:** Factions are automatically registered from units at battle start, so you don't need to manually register every faction here. However, it's recommended to register all factions you'll use for better organization.

### Step 2: Configure Faction Relationships

**Relationships are now defined directly in each FactionData asset!** This is much cleaner - each faction knows who it's hostile/allied/neutral to.

1. **Configure Relationships in Each Faction:**
   - Select a Faction Data asset (e.g., "PlayerFaction")
   - In Inspector, expand "Faction Relationships"
   - Click "+" to add relationship entries
   - For each entry:
     - Assign `Target Faction` → drag another Faction Data asset
     - Set `Relationship` (Hostile, Neutral, or Ally)
   - Repeat for all factions

   **Example:**
   - PlayerFaction → OrcFaction = Hostile
   - PlayerFaction → ElfFaction = Ally
   - OrcFaction → ElfFaction = Hostile

2. **Add FactionRelationship Component to Scene:**
   - Create empty GameObject in scene (or use existing manager GameObject)
   - Add Component: `FactionRelationship`
   - Assign your `FactionRegistry` asset to the "Faction Registry" field
   - The relationships will be automatically loaded from all registered FactionData assets on Awake

### Alternative: Manual Configuration (In-Scene)

If you prefer to configure relationships directly in the scene:

1. **Add FactionRelationship Component:**
   - Create empty GameObject in scene
   - Add Component: `FactionRelationship`

2. **Configure Relationships in Inspector:**
   - Expand "Faction Relationships" > "Relationships"
   - Click "+" to add entries
   - For each entry, you can use **either**:
     - **ScriptableObject Factions**: Assign `Faction Data 1` and `Faction Data 2`
     - **Enum Factions**: Use `Faction1` and `Faction2` dropdowns
   - Configure each relationship manually

### Default Behavior
- **Same Faction**: Always Allied (automatic)
- **Different Factions**: Hostile by default (if not explicitly configured)

---

## Unit Type Configuration

Unit Types are configured directly on each Unit component:

1. **Select a Unit GameObject** (player or enemy prefab/instance)

2. **In the Unit Component:**
   - Find "Unit Type" section
   - Set `Unit Type` dropdown:
     - **Beast**: Prefers melee attacks and melee skill attacks
     - **Soldier**: Can be melee or ranged depending on equipped items
     - **Magi**: Prioritizes skill attacks, ground skills

3. **Set Faction (Two Options):**

   **Option 1: ScriptableObject Faction (Recommended for Custom Factions)**
   - Find "Faction Assignment" section
   - Assign `Faction Data` → drag your Faction Data ScriptableObject asset
   - Leave `Faction` enum as fallback (will be ignored if Faction Data is assigned)

   **Option 2: Enum Faction (Simple Cases)**
   - Find "Faction Assignment" section
   - Leave `Faction Data` empty
   - Set `Faction` dropdown:
     - **Player**: For player-controlled units
     - **Faction1-3**: For enemy factions
     - **Neutral**: For neutral units

**Note:** Unit Type is separate from AI Behavior. A Beast can have Support behavior, etc.

---

## AI Behavior Configuration

### Step 1: Create AI Behavior Data Assets

1. **Create Behavior Assets:**
   - Right-click in Project window
   - Select: `Create > Riftbourne > AI Behavior`
   - Create one for each behavior type:
     - `BerserkerBehavior` - Aggressive, attacks closest/low HP
     - `SupportBehavior` - Heals/buffs allies, attacks when safe
     - `CowardBehavior` - Defensive, retreats when low HP
     - `ProtectorBehavior` - Tanks, protects allies

2. **Configure Each Behavior:**
   - Select the asset
   - Set `Behavior Type` dropdown
   - Adjust parameters:
     - **Target Selection**: Low HP Weight, Proximity Weight, Threat Weight
     - **Movement**: Aggression Level, Hazard Avoidance
     - **Action Preferences**: Skill Preference, Support Preference, Retreat Threshold

### Step 2: Assign to Units

1. **Select Unit GameObject** (enemy or AI ally)

2. **Add AIController Component** (if not already present)

3. **Assign Behavior:**
   - In AIController component
   - Find "AI Settings" section
   - Drag your `AIBehaviorData` asset to "Behavior Data" field

4. **Set Faction:**
   - Ensure Unit's `Faction` is set correctly
   - AI will target enemies based on faction relationships

---

## Complete Setup Example

### Example: Setting up a Berserker Enemy

1. **Create/Select Enemy Prefab:**
   - Has `Unit` component
   - Has `AIController` component

2. **Configure Unit:**
   - `Faction`: Faction1
   - `Unit Type`: Beast (or Soldier/Magi)
   - `Is Player Controlled`: false

3. **Configure AIController:**
   - `Behavior Data`: Assign your BerserkerBehavior asset
   - `Thinking Delay`: 0.5 (optional)

4. **Ensure Faction Relationships:**
   - FactionRelationship component exists in scene
   - Relationship configured: Player ↔ Faction1 = Hostile

### Example: Setting up an AI Ally

1. **Create/Select Ally Prefab:**
   - Has `Unit` component
   - Has `AIController` component

2. **Configure Unit:**
   - `Faction`: Player (or create new allied faction)
   - `Unit Type`: Support (Magi type works well)
   - `Is Player Controlled`: false (AI controls it)

3. **Configure AIController:**
   - `Behavior Data`: Assign SupportBehavior asset

4. **Ensure Faction Relationships:**
   - Relationship configured: Player ↔ Player = Ally (automatic)
   - Relationship configured: Player ↔ EnemyFaction = Hostile

---

## Quick Reference

### Faction Enum Values
- `Player` - Player-controlled units
- `Faction1`, `Faction2`, `Faction3` - Enemy factions
- `Neutral` - Neutral units

### Unit Type Enum Values
- `Beast` - Melee-focused
- `Soldier` - Flexible (melee/ranged)
- `Magi` - Skill-focused

### AI Behavior Types
- `Berserker` - Aggressive attacker
- `Support` - Healer/buffer
- `Coward` - Defensive/retreating
- `Protector` - Tank/guardian

### Relationship Types
- `Hostile` - Will attack each other
- `Neutral` - No special relationship
- `Ally` - Will help each other

---

## Tips

1. **Reusability**: Use ScriptableObjects for faction relationships and AI behaviors so you can reuse them across multiple scenes/prefabs.

2. **Testing**: Start with default behaviors, then fine-tune parameters based on gameplay testing.

3. **Faction Strategy**: You can have multiple enemy factions fight each other by setting Faction1 ↔ Faction2 = Hostile.

4. **AI Allies**: Set unit's Faction to Player but IsPlayerControlled to false, and assign a Support or Protector behavior.

5. **Unit Type vs Behavior**: Remember - Unit Type affects available actions, Behavior affects decision-making. A Beast can be Support type!

---

## Troubleshooting

**Q: AI units aren't attacking each other**
- Check FactionRelationship component exists in scene
- Verify faction relationships are set to Hostile
- Ensure units have different factions

**Q: AI behavior not working**
- Verify AIController component is attached
- Check Behavior Data asset is assigned
- Ensure unit's Faction is set correctly

**Q: Units blocking each other's paths**
- Check faction relationships - allies shouldn't block
- Verify FactionRelationship component is in scene

**Q: Turn order issues**
- Units are grouped by faction in turn windows
- Check that factions are set correctly on all units

