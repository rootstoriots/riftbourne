# Enemy Creation Guide

## Overview

This guide explains how to create different enemy types, variants, and bosses for Riftbourne's tactical combat system. You'll learn how to set up enemy prefabs, configure their stats, AI behavior, equipment, and skills.

## Table of Contents

1. [Enemy Prefab Structure](#enemy-prefab-structure)
2. [Creating Base Enemy Prefabs](#creating-base-enemy-prefabs)
3. [Enemy Archetypes](#enemy-archetypes)
4. [Creating Variants](#creating-variants)
5. [Creating Bosses](#creating-bosses)
6. [Using Enemies in Encounters](#using-enemies-in-encounters)
7. [Best Practices](#best-practices)
8. [Quick Reference](#quick-reference)

---

## Enemy Prefab Structure

### Required Components

Every enemy prefab must have:

1. **Unit Component** - Core combat stats and abilities
2. **AIController Component** - AI decision-making (automatically added by CombatInitiator if missing)
3. **Visual Representation** - 3D model, sprite, or placeholder (Capsule, Cube, etc.)

### Optional Components

- **EnemyLoot Component** - Defines loot dropped on death
- **HPDisplay Component** - Shows HP bar above unit
- **StatusEffectManager** - Handles status effects (usually auto-added)

---

## Creating Base Enemy Prefabs

### Step 1: Create Base Prefab

1. **Create GameObject:**
   - Right-click in Hierarchy → `3D Object → Capsule` (or use your enemy model)
   - Name it: `Enemy_[ArchetypeName]` (e.g., `Enemy_Goblin`, `Enemy_OrcWarrior`)

2. **Add Unit Component:**
   - Select GameObject
   - `Add Component → Unit`
   - Configure basic settings (see Step 2)

3. **Add AIController Component:**
   - `Add Component → AIController`
   - Leave `Behavior Data` empty for now (will assign later)

4. **Save as Prefab:**
   - Drag GameObject from Hierarchy to `Assets/Prefabs/Enemies/` folder
   - Delete instance from scene (keep prefab)

### Step 2: Configure Unit Component

**Basic Identity:**
- `Unit Name`: Display name (e.g., "Goblin Warrior", "Orc Shaman")
- `Is Player Controlled`: **Unchecked** (false)
- `Portrait`: Optional sprite for UI (leave empty for default)

**Faction Assignment:**
- **Option 1 (Recommended)**: Assign `Faction Data` ScriptableObject
- **Option 2**: Set `Faction` enum dropdown (Faction1, Faction2, Faction3, etc.)

**Unit Type:**
- `Unit Type`: Choose based on enemy role:
  - **Beast**: Melee-focused enemies (goblins, wolves, etc.)
  - **Soldier**: Flexible fighters (orcs, bandits, etc.)
  - **Magi**: Spellcasters (shamans, mages, etc.)

**Core Attributes:**
- `Strength`: Physical power, melee damage (5-15 for normal enemies)
- `Finesse`: Precision, ranged damage, evasion (5-15)
- `Focus`: Mental power, magic damage, resistance (5-15)
- `Speed`: Turn order priority (5-15, higher = goes first)
- `Luck`: Critical chance, item drops (5-10)

**Combat Stats:**
- `Max HP`: Health pool (50-200 for normal enemies, 500+ for bosses)
- `Current HP`: Leave at 0 (auto-set to Max HP on spawn)
- `Movement Range`: Grid cells the enemy can move (3-6 typical)

**Equipment (Optional):**
- `Melee Weapon`: Assign weapon item asset
- `Ranged Weapon`: Assign ranged weapon if applicable
- `Armor`: Assign armor item asset
- `Accessory1/2`: Assign accessory items
- `Codex`: Assign codex if enemy uses magic

**Skills:**
- `Known Skills`: Add Skill assets the enemy can use
- Skills should match the enemy's role (melee enemies get melee skills, etc.)

**Progression:**
- `Level`: Enemy level (1-20+)
- `Mantle`: Magical affinity (if enemy uses magic)

### Step 3: Configure AIController Component

1. **Create AI Behavior Data:**
   - Right-click in Project → `Create → Riftbourne → AI Behavior`
   - Name it: `AI_[BehaviorType]_[EnemyName]` (e.g., `AI_Berserker_Goblin`)
   - Configure behavior (see [AI Behavior Configuration](#ai-behavior-configuration))

2. **Assign to AIController:**
   - Select enemy prefab
   - In AIController component
   - Drag AI Behavior Data asset to `Behavior Data` field

3. **Set Thinking Delay:**
   - `Thinking Delay`: 0.3-0.8 seconds (visual delay before AI acts)

### Step 4: Configure Faction (If Using FactionData)

1. **Create/Select Faction Data:**
   - Right-click → `Create → Riftbourne → Faction Data`
   - Name it: `Faction_[Name]` (e.g., `Faction_Goblin`, `Faction_Orc`)
   - Configure faction relationships

2. **Assign to Unit:**
   - Select enemy prefab
   - In Unit component → `Faction Assignment`
   - Drag Faction Data to `Faction Data` field

---

## Enemy Archetypes

### Melee Fighter (Beast/Soldier)

**Role**: Frontline attacker, engages in melee combat

**Stats:**
- High Strength (8-12)
- Moderate Finesse (5-8)
- Low Focus (3-5)
- Moderate Speed (6-9)
- Moderate HP (80-150)

**Equipment:**
- Melee Weapon (sword, axe, club)
- Armor (light to medium)
- Accessory (damage boost or HP boost)

**Skills:**
- Melee attack skills (Slash, Charge, etc.)
- Defensive skills (Block, Parry)

**AI Behavior:**
- **Berserker** (aggressive) or **Protector** (tank)

**Example Names:**
- Goblin Warrior
- Orc Fighter
- Bandit Thug

---

### Ranged Attacker (Soldier)

**Role**: Backline damage dealer, attacks from distance

**Stats:**
- Moderate Strength (5-8)
- High Finesse (8-12)
- Low Focus (3-5)
- High Speed (7-10)
- Low HP (60-100)

**Equipment:**
- Ranged Weapon (bow, crossbow)
- Light Armor
- Accessory (accuracy or crit boost)

**Skills:**
- Ranged attack skills (Shoot, Snipe, etc.)
- Movement skills (Retreat, Reposition)

**AI Behavior:**
- **Berserker** (aggressive) or **Coward** (defensive)

**Example Names:**
- Goblin Archer
- Orc Hunter
- Bandit Sniper

---

### Spellcaster (Magi)

**Role**: Magic damage dealer, support, or crowd control

**Stats:**
- Low Strength (3-5)
- Moderate Finesse (5-7)
- High Focus (8-12)
- Moderate Speed (6-9)
- Low HP (50-90)

**Equipment:**
- Codex (magic focus)
- Light Armor or Robes
- Accessory (magic power or mana boost)

**Skills:**
- Magic attack skills (Fireball, Lightning Bolt, etc.)
- Support skills (Heal, Buff) if Support type
- AOE skills (Blizzard, Chain Lightning)

**AI Behavior:**
- **Berserker** (aggressive caster) or **Support** (healer/buffer)

**Example Names:**
- Goblin Shaman
- Orc Warlock
- Bandit Mage

---

### Tank/Protector (Beast/Soldier)

**Role**: High HP, draws aggro, protects allies

**Stats:**
- High Strength (7-10)
- Moderate Finesse (4-6)
- Low Focus (3-5)
- Low Speed (4-7)
- Very High HP (150-300)

**Equipment:**
- Melee Weapon (shield + weapon)
- Heavy Armor
- Accessory (HP boost or defense boost)

**Skills:**
- Defensive skills (Taunt, Guard, Shield Bash)
- Self-heal or damage reduction skills

**AI Behavior:**
- **Protector** (tank behavior)

**Example Names:**
- Orc Guardian
- Goblin Brute
- Bandit Enforcer

---

### Support/Healer (Magi)

**Role**: Heals allies, buffs team, debuffs enemies

**Stats:**
- Low Strength (3-5)
- Moderate Finesse (5-7)
- High Focus (8-12)
- Moderate Speed (6-9)
- Moderate HP (70-120)

**Equipment:**
- Codex (healing focus)
- Light Armor
- Accessory (healing power or support boost)

**Skills:**
- Healing skills (Heal, Regenerate, etc.)
- Buff skills (Haste, Shield, etc.)
- Debuff skills (Slow, Weakness, etc.)

**AI Behavior:**
- **Support** (prioritizes helping allies)

**Example Names:**
- Orc Shaman (healer variant)
- Goblin Witch Doctor
- Bandit Alchemist

---

### Assassin/Rogue (Soldier)

**Role**: High damage, low HP, targets weak enemies

**Stats:**
- Moderate Strength (6-9)
- Very High Finesse (10-14)
- Low Focus (3-5)
- Very High Speed (9-12)
- Very Low HP (40-70)

**Equipment:**
- Dual weapons or high-damage melee
- Light Armor
- Accessory (crit boost or speed boost)

**Skills:**
- High-damage single-target skills (Backstab, Assassinate)
- Movement skills (Dash, Teleport)

**AI Behavior:**
- **Berserker** (aggressive) or **Coward** (hit-and-run)

**Example Names:**
- Goblin Assassin
- Orc Rogue
- Bandit Cutthroat

---

## Creating Variants

Variants are different versions of the same enemy type with modified stats, equipment, or skills. Useful for difficulty scaling and variety.

### Method 1: Duplicate and Modify Prefab

1. **Duplicate Base Prefab:**
   - Right-click base prefab → `Duplicate`
   - Rename: `Enemy_[Type]_[Variant]` (e.g., `Enemy_Goblin_Elite`, `Enemy_Orc_Shaman`)

2. **Modify Stats:**
   - Select variant prefab
   - Adjust attributes (increase Strength for "Elite" variant, etc.)
   - Adjust HP (higher for "Elite", lower for "Weak")

3. **Modify Equipment:**
   - Change weapon to different tier
   - Upgrade armor
   - Add/remove accessories

4. **Modify Skills:**
   - Add new skills for "Elite" variant
   - Remove skills for "Weak" variant

### Method 2: Use Prefab Variants (Unity Feature)

1. **Create Base Prefab:**
   - Set up base enemy with default stats

2. **Create Variant:**
   - Right-click base prefab → `Create → Prefab Variant`
   - Name it: `Enemy_[Type]_[Variant]`

3. **Override Properties:**
   - Select variant
   - Modify stats, equipment, skills
   - Changes are stored in variant, base remains unchanged

**Advantages:**
- Variants inherit base properties
- Easy to update base and propagate changes
- Cleaner organization

### Variant Examples

**Elite Variant:**
- +2 to all attributes
- +50% HP
- Better equipment
- Additional skill

**Weak Variant:**
- -2 to all attributes
- -30% HP
- Basic equipment
- Fewer skills

**Veteran Variant:**
- +1 to all attributes
- +25% HP
- Better equipment
- Same skills but higher level

---

## Creating Bosses

Bosses are special enemies with unique mechanics, higher stats, and often multiple phases.

### Boss Characteristics

1. **High Stats:**
   - Very High HP (500-2000+)
   - High attributes (12-20+)
   - Multiple skills
   - Special equipment

2. **Unique Mechanics:**
   - Multiple phases
   - Special abilities
   - Summoning minions
   - Environmental interactions

3. **Visual Distinction:**
   - Larger model
   - Unique appearance
   - Special effects

### Step 1: Create Boss Prefab

1. **Create Base:**
   - Follow "Creating Base Enemy Prefabs" steps
   - Use larger model or scale up GameObject

2. **Configure Stats:**
   - `Max HP`: 500-2000+ (depending on boss tier)
   - `Strength/Finesse/Focus`: 12-20+
   - `Speed`: 8-12 (bosses should act frequently)
   - `Movement Range`: 4-6 (bosses are mobile)

3. **Configure Equipment:**
   - Unique boss equipment
   - Multiple accessories for power
   - Legendary-tier items

4. **Configure Skills:**
   - 5-10+ skills
   - Mix of attack, defense, and special abilities
   - AOE skills for crowd control
   - Self-heal or regeneration

### Step 2: Configure Boss AI

1. **Create Custom AI Behavior:**
   - Create AI Behavior Data asset
   - Name: `AI_Boss_[BossName]`
   - Set `Behavior Type`: Usually **Berserker** or **Protector**
   - Adjust parameters:
     - High `Aggression Level` (0.8-1.0)
     - High `Skill Preference` (0.6-0.9)
     - Moderate `Hazard Avoidance` (0.4-0.6)

2. **Assign to Boss:**
   - Select boss prefab
   - Assign AI Behavior Data to AIController

### Step 3: Boss-Specific Components (Future Enhancement)

For now, bosses use standard components. Future enhancements could include:
- Boss-specific scripts for phases
- Special ability triggers
- Minion summoning systems

### Boss Examples

**Tier 1 Boss (Early Game):**
- HP: 500-800
- Attributes: 12-15
- 5-7 skills
- Standard AI behavior

**Tier 2 Boss (Mid Game):**
- HP: 800-1200
- Attributes: 15-18
- 7-10 skills
- Custom AI behavior

**Tier 3 Boss (Late Game):**
- HP: 1200-2000+
- Attributes: 18-20+
- 10+ skills
- Custom AI behavior with special mechanics

---

## Using Enemies in Encounters

### Step 1: Add Enemy to EncounterData

1. **Open EncounterData Asset:**
   - Navigate to your encounter asset
   - Expand "Enemy Configuration"

2. **Add Enemy Spawn:**
   - Click "+" to add new spawn
   - Assign `Enemy Prefab` → drag enemy prefab
   - Set `Grid Position` (X, Y) where enemy spawns
   - Set `Level` (for future stat scaling)
   - Optionally assign `Faction Data` override

3. **Repeat for All Enemies:**
   - Add spawns for each enemy in the encounter
   - Position them strategically

### Step 2: Test Encounter

1. **Assign to CombatInitiator:**
   - Select CombatInitiator in scene
   - Drag EncounterData to `Test Encounter` field
   - Enter Play Mode

2. **Verify:**
   - Enemies spawn at correct positions
   - Enemies have correct stats
   - Enemies use correct AI behavior
   - Enemies appear in turn order

---

## Best Practices

### Organization

**Folder Structure:**
```
Assets/
├── Prefabs/
│   └── Enemies/
│       ├── Melee/
│       │   ├── Enemy_GoblinWarrior.prefab
│       │   ├── Enemy_GoblinWarrior_Elite.prefab
│       │   └── Enemy_OrcFighter.prefab
│       ├── Ranged/
│       │   ├── Enemy_GoblinArcher.prefab
│       │   └── Enemy_OrcHunter.prefab
│       ├── Magic/
│       │   ├── Enemy_GoblinShaman.prefab
│       │   └── Enemy_OrcWarlock.prefab
│       └── Bosses/
│           ├── Boss_GoblinChieftain.prefab
│           └── Boss_OrcWarlord.prefab
├── Resources/
│   └── AIBehaviors/
│       ├── AI_Berserker_Goblin.asset
│       ├── AI_Berserker_Orc.asset
│       ├── AI_Support_Shaman.asset
│       └── AI_Boss_Chieftain.asset
└── Enum/
    └── Factions/
        ├── Faction_Goblin.asset
        └── Faction_Orc.asset
```

### Naming Conventions

**Prefabs:**
- `Enemy_[Type]_[Variant]` (e.g., `Enemy_Goblin_Warrior`, `Enemy_Goblin_Elite`)
- `Boss_[Name]` (e.g., `Boss_GoblinChieftain`)

**AI Behaviors:**
- `AI_[BehaviorType]_[EnemyType]` (e.g., `AI_Berserker_Goblin`)

**Factions:**
- `Faction_[Name]` (e.g., `Faction_Goblin`, `Faction_Orc`)

### Stat Balancing

**Early Game Enemies (Level 1-5):**
- Attributes: 5-8
- HP: 50-100
- Basic equipment
- 1-3 skills

**Mid Game Enemies (Level 6-10):**
- Attributes: 8-12
- HP: 100-200
- Improved equipment
- 3-5 skills

**Late Game Enemies (Level 11-15):**
- Attributes: 12-16
- HP: 200-400
- Advanced equipment
- 5-7 skills

**Bosses:**
- Attributes: 15-20+
- HP: 500-2000+
- Legendary equipment
- 7-10+ skills

### AI Behavior Selection

**Match Behavior to Role:**
- Melee fighters → Berserker or Protector
- Ranged attackers → Berserker or Coward
- Spellcasters → Berserker or Support
- Tanks → Protector
- Healers → Support
- Assassins → Berserker or Coward

**Adjust Parameters:**
- Aggressive enemies: High `Aggression Level` (0.7-1.0)
- Defensive enemies: Low `Aggression Level` (0.3-0.5)
- Skill-focused: High `Skill Preference` (0.6-0.9)
- Support-focused: High `Support Preference` (0.5-0.8)

---

## Quick Reference

### Unit Component Checklist

- [ ] Unit Name set
- [ ] Is Player Controlled = false
- [ ] Faction assigned (FactionData or enum)
- [ ] Unit Type selected (Beast/Soldier/Magi)
- [ ] Attributes configured (Strength, Finesse, Focus, Speed, Luck)
- [ ] Max HP set
- [ ] Movement Range set
- [ ] Equipment assigned (if applicable)
- [ ] Skills added (if applicable)
- [ ] Level set

### AIController Component Checklist

- [ ] AIController component present
- [ ] AI Behavior Data assigned
- [ ] Thinking Delay set (0.3-0.8)

### EncounterData Checklist

- [ ] Enemy prefabs assigned to spawn definitions
- [ ] Grid positions set for each enemy
- [ ] Level set (if using level scaling)
- [ ] Faction Data override set (if needed)

### Common Enemy Configurations

**Basic Melee Enemy:**
- Unit Type: Beast
- Strength: 8, Finesse: 5, Focus: 3, Speed: 6
- HP: 100
- Melee Weapon assigned
- AI: Berserker

**Basic Ranged Enemy:**
- Unit Type: Soldier
- Strength: 5, Finesse: 9, Focus: 4, Speed: 8
- HP: 70
- Ranged Weapon assigned
- AI: Berserker

**Basic Caster Enemy:**
- Unit Type: Magi
- Strength: 3, Finesse: 5, Focus: 10, Speed: 7
- HP: 60
- Codex assigned
- Magic skills assigned
- AI: Berserker or Support

**Basic Boss:**
- Unit Type: Soldier or Beast
- Strength: 15, Finesse: 12, Focus: 10, Speed: 10
- HP: 800
- Multiple equipment pieces
- 7+ skills
- AI: Berserker (custom parameters)

---

## Examples

### Example 1: Goblin Warrior (Basic Melee)

**Prefab Setup:**
1. Create GameObject with Capsule mesh
2. Add Unit component:
   - Unit Name: "Goblin Warrior"
   - Faction: Faction1 (or create Faction_Goblin)
   - Unit Type: Beast
   - Strength: 8, Finesse: 5, Focus: 3, Speed: 6, Luck: 5
   - Max HP: 80
   - Movement Range: 4
   - Melee Weapon: Basic Sword
   - Skills: Slash, Charge
3. Add AIController:
   - Behavior Data: AI_Berserker_Goblin
   - Thinking Delay: 0.5

**AI Behavior Setup:**
1. Create AI Behavior Data: `AI_Berserker_Goblin`
2. Behavior Type: Berserker
3. Aggression Level: 0.8
4. Skill Preference: 0.3
5. Support Preference: 0.0

### Example 2: Orc Shaman (Support Caster)

**Prefab Setup:**
1. Create GameObject with model
2. Add Unit component:
   - Unit Name: "Orc Shaman"
   - Faction: Faction_Orc
   - Unit Type: Magi
   - Strength: 4, Finesse: 6, Focus: 11, Speed: 7, Luck: 5
   - Max HP: 90
   - Movement Range: 3
   - Codex: Shaman Codex
   - Skills: Heal, Regenerate, Fireball
3. Add AIController:
   - Behavior Data: AI_Support_Shaman
   - Thinking Delay: 0.6

**AI Behavior Setup:**
1. Create AI Behavior Data: `AI_Support_Shaman`
2. Behavior Type: Support
3. Aggression Level: 0.4
4. Skill Preference: 0.7
5. Support Preference: 0.8

### Example 3: Goblin Chieftain (Boss)

**Prefab Setup:**
1. Create GameObject with larger model
2. Add Unit component:
   - Unit Name: "Goblin Chieftain"
   - Faction: Faction_Goblin
   - Unit Type: Beast
   - Strength: 16, Finesse: 12, Focus: 8, Speed: 10, Luck: 7
   - Max HP: 1200
   - Movement Range: 5
   - Melee Weapon: Chieftain's Axe
   - Armor: Chieftain's Armor
   - Accessory1: Power Amulet
   - Skills: Slash, Charge, War Cry, Regenerate, AOE Attack
3. Add AIController:
   - Behavior Data: AI_Boss_Chieftain
   - Thinking Delay: 0.8

**AI Behavior Setup:**
1. Create AI Behavior Data: `AI_Boss_Chieftain`
2. Behavior Type: Berserker
3. Aggression Level: 0.9
4. Skill Preference: 0.7
5. Support Preference: 0.2

---

## Troubleshooting

**Q: Enemy doesn't spawn in encounter**
- Check enemy prefab has Unit component
- Verify grid position is within grid bounds
- Check Console for error messages

**Q: Enemy doesn't take turns**
- Verify AIController component exists
- Check AI Behavior Data is assigned
- Ensure enemy has valid faction (not Player)

**Q: Enemy uses wrong AI behavior**
- Verify AI Behavior Data is assigned to AIController
- Check Behavior Type matches expected type
- Ensure AIController component is enabled

**Q: Enemy stats seem wrong**
- Check Unit component attributes
- Verify equipment is assigned correctly
- Check if level scaling is affecting stats

**Q: Enemy doesn't appear in turn order**
- TurnOrderUI should auto-refresh, but may need delay
- Check Console for TurnOrderUI messages
- Verify enemy is registered with TurnManager

---

## Next Steps

1. Create base enemy prefabs for each archetype
2. Create variants (Elite, Weak, Veteran)
3. Create boss prefabs
4. Set up AI Behavior Data assets
5. Create Faction Data assets
6. Test enemies in EncounterData
7. Balance stats based on gameplay testing
