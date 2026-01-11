# Weapon Proficiency System - Implementation Guide

## Overview

This guide covers how to properly implement the weapon proficiency system in battle scenes and display proficiency information in the status UI.

## Part 1: Battle Scene Implementation

### Step 1: Verify Unit Initialization

The `WeaponProficiencyManager` is automatically initialized in `Unit.Awake()`. However, you need to ensure proficiency data is loaded when creating Units from CharacterState.

**Location:** `Assets/Scripts/Characters/Unit.cs`

The `UpdateFromCharacterState()` method should already sync proficiencies. Verify it includes:

```csharp
// Update weapon proficiencies
if (weaponProficiencyManager != null && state.WeaponProficiencies != null)
{
    Dictionary<WeaponFamily, WeaponProficiency> proficiencyData = new Dictionary<WeaponFamily, WeaponProficiency>();
    foreach (var kvp in state.WeaponProficiencies)
    {
        proficiencyData[kvp.Key] = kvp.Value;
    }
    weaponProficiencyManager.InitializeFromData(proficiencyData);
}
```

### Step 2: Verify Proficiency Tracking in Combat

The system automatically tracks proficiency during combat. Verify these integration points:

**Melee Attacks:**
- `AttackAction.ExecuteMeleeAttack()` determines weapon family from equipped melee weapon
- Gets proficiency tier and passes to `CombatCalculator.CalculateAttack()`
- Records combat outcome (hit, kill, crit) for proficiency advancement

**Ranged Attacks:**
- `AttackAction.ExecuteRangedAttack()` determines weapon family from equipped ranged weapon
- Same proficiency flow as melee attacks

**Skills:**
- `SkillExecutor.ExecuteSkill()` determines weapon family from equipped weapon
- Applies proficiency stat efficiency multiplier to skill damage
- Records combat outcomes for proficiency advancement

### Step 3: Configure Proficiency Settings

1. **Create ProficiencySettings asset:**
   - `Assets > Create > Riftbourne > Proficiency Settings`
   - Name it `ProficiencySettings`
   - Place in `Assets/Resources/` folder
   - Adjust thresholds as needed (see Part 5 for details)

2. **Default thresholds:**
   - Untrained → Familiar: 4 meaningful hits
   - Familiar → Trained: 12 meaningful outcomes (hits + kills)
   - Trained → Specialized: 27 meaningful outcomes + 3 crits

### Step 4: Test Proficiency Advancement

1. **Start a battle** with a character using a weapon
2. **Perform attacks** against meaningful enemies (not trivial encounters)
3. **Check console logs** for proficiency advancement messages:
   - "Weapon proficiency [Family] advanced to [Tier]!"
4. **Verify advancement** matches your configured thresholds

### Step 5: Meaningful Encounter Detection

The system filters trivial encounters automatically. An encounter is considered meaningful if:
- Target has ≥50% of attacker's max HP
- Target is alive
- Target's attack power ≥60% of attacker's attack power OR target's HP ≥70% of attacker's HP

**To adjust these thresholds**, modify `WeaponProficiencyManager.IsMeaningfulEncounter()`.

## Part 2: Status UI Implementation

### Step 1: Add UI Elements to Status Menu

**Location:** Unity Editor → StatusMenuUI GameObject

1. **Open the Status Menu scene/prefab**
2. **In the Status Tab Panel**, add a new section for Weapon Proficiencies:
   - Create a new `GameObject` named "ProficiencySection"
   - Add a `Vertical Layout Group` component
   - Add a `Content Size Fitter` component (set to "Preferred Size")

3. **Add Header:**
   - Create a `TMP_Text` child named "ProficiencyHeader"
   - Set text to "Weapon Proficiencies"
   - Style as a section header

4. **Add Container for Proficiency List:**
   - Create a `GameObject` child named "ProficiencyListContainer"
   - Add `Vertical Layout Group` component
   - Add `Content Size Fitter` component

### Step 2: Create Proficiency Display Prefab

1. **Create a prefab** for individual proficiency entries:
   - Create `GameObject` named "ProficiencyEntryPrefab"
   - Add `Horizontal Layout Group` component
   - Add two `TMP_Text` children:
     - "WeaponFamilyText" (left) - displays weapon family name
     - "TierText" (right) - displays current tier

2. **Layout Example:**
   ```
   ProficiencyEntryPrefab
   ├── WeaponFamilyText: "Sword"
   └── TierText: "Trained"
   ```

### Step 3: Update StatusMenuUI Script

**Location:** `Assets/Scripts/UI/StatusMenuUI.cs`

1. **Add Serialized Fields:**

```csharp
[Header("Status Tab UI - Weapon Proficiencies")]
[SerializeField] private GameObject proficiencySection;
[SerializeField] private Transform proficiencyListContainer;
[SerializeField] private GameObject proficiencyEntryPrefab;
```

2. **Update RefreshProficiencyDisplay() Method:**

Replace the placeholder method with:

```csharp
/// <summary>
/// Refresh weapon proficiency display.
/// </summary>
private void RefreshProficiencyDisplay(CharacterState character)
{
    if (proficiencySection == null || proficiencyListContainer == null)
    {
        return; // UI elements not set up yet
    }
    
    // Show/hide section based on whether character exists
    if (proficiencySection != null)
    {
        proficiencySection.SetActive(character != null || currentUnit != null);
    }
    
    if (character == null && currentUnit == null)
    {
        return;
    }
    
    // Get proficiencies from CharacterState or Unit
    SerializableDictionary<WeaponFamily, WeaponProficiency> proficiencies = null;
    if (character != null && character.WeaponProficiencies != null)
    {
        proficiencies = character.WeaponProficiencies;
    }
    else if (currentUnit != null && currentUnit.WeaponProficiencyManager != null)
    {
        var unitProfs = currentUnit.WeaponProficiencyManager.GetAllProficiencies();
        proficiencies = new SerializableDictionary<WeaponFamily, WeaponProficiency>();
        foreach (var kvp in unitProfs)
        {
            proficiencies[kvp.Key] = kvp.Value;
        }
    }
    
    if (proficiencies == null || proficiencyListContainer == null)
    {
        return;
    }
    
    // Clear existing entries
    foreach (Transform child in proficiencyListContainer)
    {
        if (child != null)
        {
            Destroy(child.gameObject);
        }
    }
    
    // Display proficiencies (only show if above Untrained)
    bool hasAnyProficiency = false;
    foreach (var kvp in proficiencies)
    {
        if (kvp.Value != null && kvp.Value.CurrentTier != WeaponProficiencyTier.Untrained)
        {
            hasAnyProficiency = true;
            
            if (proficiencyEntryPrefab != null)
            {
                GameObject entry = Instantiate(proficiencyEntryPrefab, proficiencyListContainer);
                
                // Find text components
                TMP_Text familyText = entry.transform.Find("WeaponFamilyText")?.GetComponent<TMP_Text>();
                TMP_Text tierText = entry.transform.Find("TierText")?.GetComponent<TMP_Text>();
                
                if (familyText != null)
                {
                    familyText.text = GetWeaponFamilyDisplayName(kvp.Key);
                }
                
                if (tierText != null)
                {
                    tierText.text = GetTierDisplayName(kvp.Value.CurrentTier);
                }
            }
        }
    }
    
    // Hide section if no proficiencies to show
    if (proficiencySection != null)
    {
        proficiencySection.SetActive(hasAnyProficiency);
    }
}

/// <summary>
/// Get display name for weapon family.
/// </summary>
private string GetWeaponFamilyDisplayName(WeaponFamily family)
{
    switch (family)
    {
        case WeaponFamily.ShortBlade: return "Short Blades";
        case WeaponFamily.Sword: return "Swords";
        case WeaponFamily.HeavyBlade: return "Heavy Blades";
        case WeaponFamily.OneHandedBlunt: return "One-Handed Blunt";
        case WeaponFamily.TwoHandedBlunt: return "Two-Handed Blunt";
        case WeaponFamily.Spear: return "Spears";
        case WeaponFamily.Staff: return "Staves";
        case WeaponFamily.Polearm: return "Polearms";
        case WeaponFamily.Gloves: return "Unarmed";
        case WeaponFamily.Bows: return "Bows";
        case WeaponFamily.Crossbows: return "Crossbows";
        case WeaponFamily.Handguns: return "Handguns";
        case WeaponFamily.Rifles: return "Rifles";
        default: return family.ToString();
    }
}

/// <summary>
/// Get display name for proficiency tier.
/// </summary>
private string GetTierDisplayName(WeaponProficiencyTier tier)
{
    switch (tier)
    {
        case WeaponProficiencyTier.Untrained: return "Untrained";
        case WeaponProficiencyTier.Familiar: return "Familiar";
        case WeaponProficiencyTier.Trained: return "Trained";
        case WeaponProficiencyTier.Specialized: return "Specialized";
        default: return tier.ToString();
    }
}
```

3. **Also call RefreshProficiencyDisplay() for Unit fallback:**

In the `RefreshStatusDisplay()` method, add:

```csharp
// Fallback to Unit (backward compatibility)
if (currentUnit != null)
{
    // ... existing code ...
    
    // Narrative skills fallback
    RefreshNarrativeSkillsDisplay(null);
    
    // Weapon Proficiencies fallback
    RefreshProficiencyDisplay(null);
}
```

### Step 4: Wire Up UI Elements in Unity Editor

1. **Select StatusMenuUI GameObject** in the scene
2. **In Inspector**, find the "Status Menu UI" component
3. **Expand "Status Tab UI - Weapon Proficiencies"** section
4. **Assign references:**
   - `Proficiency Section`: Drag the ProficiencySection GameObject
   - `Proficiency List Container`: Drag the ProficiencyListContainer GameObject
   - `Proficiency Entry Prefab`: Drag the ProficiencyEntryPrefab prefab

### Step 5: Optional - Add Progress Indicators

To show progress toward next tier, you can enhance the proficiency entry:

1. **Add a progress bar** to `ProficiencyEntryPrefab`:
   - Add `Slider` or `Image` component for visual progress
   - Update `RefreshProficiencyDisplay()` to calculate and display progress

2. **Progress calculation example:**

```csharp
// In RefreshProficiencyDisplay(), after creating entry:
Slider progressBar = entry.transform.Find("ProgressBar")?.GetComponent<Slider>();
if (progressBar != null && kvp.Value != null)
{
    float progress = CalculateTierProgress(kvp.Value);
    progressBar.value = progress;
}

// Helper method:
private float CalculateTierProgress(WeaponProficiency proficiency)
{
    int totalOutcomes = proficiency.GetTotalOutcomes();
    
    switch (proficiency.CurrentTier)
    {
        case WeaponProficiencyTier.Untrained:
            return Mathf.Clamp01(totalOutcomes / 4f); // 4 hits needed
        case WeaponProficiencyTier.Familiar:
            return Mathf.Clamp01((totalOutcomes - 4) / 8f); // 8 more outcomes needed
        case WeaponProficiencyTier.Trained:
            return Mathf.Clamp01((totalOutcomes - 12) / 15f); // 15 more outcomes needed
        case WeaponProficiencyTier.Specialized:
            return 1f; // Max tier
        default:
            return 0f;
    }
}
```

## Part 3: Testing Checklist

### Battle Testing

- [ ] Start battle with character using a weapon
- [ ] Perform melee attacks - verify proficiency is tracked
- [ ] Perform ranged attacks - verify proficiency is tracked
- [ ] Use skills - verify proficiency affects damage
- [ ] Check console for advancement messages
- [ ] Verify trivial encounters don't advance proficiency
- [ ] Verify meaningful encounters do advance proficiency

### UI Testing

- [ ] Open status menu (TAB key)
- [ ] Select character with proficiencies
- [ ] Verify proficiency section appears
- [ ] Verify weapon families are displayed correctly
- [ ] Verify tier names are displayed correctly
- [ ] Verify only non-Untrained proficiencies are shown
- [ ] Test with multiple characters
- [ ] Test after proficiency advancement

### Save/Load Testing

- [ ] Save game with character having proficiencies
- [ ] Load game - verify proficiencies are restored
- [ ] Enter battle - verify proficiencies are synced to Unit
- [ ] Advance proficiency in battle
- [ ] Return to exploration - verify proficiency is saved
- [ ] Reload - verify advanced proficiency persists

## Part 4: Troubleshooting

### Proficiencies Not Advancing

1. **Check if encounters are meaningful:**
   - Add debug logs in `IsMeaningfulEncounter()`
   - Verify enemy stats meet thresholds

2. **Check milestone thresholds:**
   - Verify `WeaponProficiency.RecordCombatOutcome()` is being called
   - Check `CheckForAdvancement()` logic

3. **Check weapon family detection:**
   - Verify `WeaponFamilyHelper.GetWeaponFamily()` returns correct family
   - Check equipment is properly equipped

### UI Not Displaying

1. **Check UI element references:**
   - Verify all serialized fields are assigned in Inspector
   - Check GameObject names match script expectations

2. **Check proficiency data:**
   - Add debug logs in `RefreshProficiencyDisplay()`
   - Verify `character.WeaponProficiencies` is not null
   - Verify proficiencies contain data

3. **Check tier filtering:**
   - Verify only non-Untrained proficiencies are shown
   - Test with character that has Familiar+ tier

### Proficiency Effects Not Applying

1. **Check CombatCalculator:**
   - Verify `proficiencyTier` parameter is being passed
   - Check `ProficiencyEffects.GetStatEfficiencyMultiplier()` returns correct values

2. **Check stat calculations:**
   - Verify stat efficiency multiplier is applied to Finesse/Luck/Focus
   - Check variance reduction is applied to hit chance

3. **Add debug logs:**
   - Log proficiency tier in `CombatCalculator.CalculateAttack()`
   - Log stat multipliers being applied

## Part 5: Configuring Proficiency Settings

### Creating ProficiencySettings Asset

The proficiency system uses a configurable ScriptableObject for all thresholds. This allows you to adjust values in the Unity Editor without code changes.

1. **Create the Asset:**
   - In Unity Editor: `Assets > Create > Riftbourne > Proficiency Settings`
   - Name it `ProficiencySettings`
   - Place it in `Assets/Resources/` folder (for auto-loading)

2. **Configure Thresholds:**
   - **Untrained to Familiar Hits:** Number of meaningful hits needed (default: 4)
   - **Familiar to Trained Outcomes:** Total outcomes needed (default: 12)
   - **Trained to Specialized Outcomes:** Total outcomes needed (default: 27)
   - **Trained to Specialized Crits:** Critical hits needed (default: 3)

3. **Configure Meaningful Encounter Detection:**
   - **Meaningful Encounter Min HP Ratio:** Target must have at least this % of attacker's HP (default: 0.5 = 50%)
   - **Trivial Encounter Attack Ratio:** If target's attack is below this AND HP is low, it's trivial (default: 0.6 = 60%)
   - **Trivial Encounter HP Ratio:** If target's HP is below this AND attack is low, it's trivial (default: 0.7 = 70%)

### Adjusting Settings

Simply modify the values in the `ProficiencySettings` asset in Unity Editor:
- Lower values = faster advancement
- Higher values = slower advancement
- Adjust encounter thresholds to make more/fewer encounters count as "meaningful"

### Alternative: Direct Reference

If you don't want to use Resources loading, you can:
1. Create the `ProficiencySettings` asset anywhere in your project
2. Assign it directly to a manager or reference it in code
3. Set `ProficiencySettings.Instance = yourSettingsAsset;` before use

### Customizing Tier Effects

**Location:** `Assets/Scripts/Combat/ProficiencyEffects.cs`

Modify stat efficiency, variance reduction, or handling bonuses:

```csharp
case WeaponProficiencyTier.Trained:
    return 1.1f; // Change multiplier
    break;
```

## Part 6: Testing Proficiency Advancement

### Quick Testing Setup

For rapid testing where you want to advance proficiency in a single battle:

1. **Enable Testing Mode:**
   - Open your `ProficiencySettings` asset in Unity Editor
   - Check the **"Testing Mode"** checkbox
   - This sets all thresholds to 1 (1 hit, 1 outcome, 1 crit per tier)

2. **Alternative: Manual Threshold Adjustment:**
   - Set **Untrained to Familiar Hits** to `1`
   - Set **Familiar to Trained Outcomes** to `1`
   - Set **Trained to Specialized Outcomes** to `1`
   - Set **Trained to Specialized Crits** to `1`

3. **Make All Encounters Meaningful:**
   - Set **Meaningful Encounter Min HP Ratio** to `0.0` (0%)
   - Set **Trivial Encounter Attack Ratio** to `0.0` (0%)
   - Set **Trivial Encounter HP Ratio** to `0.0` (0%)
   - This ensures every attack counts toward proficiency

### Using ProficiencyTestingHelper (Runtime)

You can also use the helper class at runtime:

```csharp
// Enable testing mode programmatically
ProficiencyTestingHelper.SetTestingThresholds();

// Force advance a specific proficiency
ProficiencyTestingHelper.ForceAdvanceProficiency(unit, WeaponFamily.Sword, WeaponProficiencyTier.Specialized);
```

### Testing Checklist

- [ ] Enable testing mode or set thresholds to 1
- [ ] Set encounter thresholds to 0.0
- [ ] Start battle with character using a weapon
- [ ] Perform 1 attack - should advance to Familiar
- [ ] Perform 1 more attack - should advance to Trained
- [ ] Perform 1 crit - should advance to Specialized
- [ ] Verify proficiency effects are applied in combat

## Notes

- Proficiencies are automatically initialized as "Untrained" when first used
- Only meaningful combat outcomes advance proficiency
- Proficiencies persist through save/load
- Each weapon family tracks independently
- Proficiency effects apply to all combat actions using that weapon family
- **Remember to disable testing mode before release!**
