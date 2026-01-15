# Battle Bookends Setup Guide

This guide explains how to set up the battle bookends system in Unity, including win condition notifications, battle statistics tracking, spoils screens, and loot selection.

## Overview

The battle bookends system provides:
- **Battle Start**: Win condition notification that blocks battle progression until acknowledged
- **Battle Statistics**: Comprehensive tracking of per-character and party-wide battle stats
- **Battle End**: Victory notification → spoils screen → loot selection → transition flow

## Prerequisites

- Battle scene is set up with `CombatInitiator`, `TurnManager`, `PartyManager`, and `LootManager`
- UI Canvas exists in the battle scene
- TextMeshPro is imported and configured

## Step 1: Create BattleStatisticsTracker GameObject

1. In the Battle Scene, create a new empty GameObject
2. Name it `BattleStatisticsTracker`
3. Add the `BattleStatisticsTracker` component (found in `Assets/Scripts/Combat/BattleStatisticsTracker.cs`)
4. The component will automatically initialize as a singleton and persist across scenes using `DontDestroyOnLoad`

**Important**: The `BattleStatisticsTracker` tracks:
- **Current Battle Stats**: Statistics for the current battle only (resets each battle)
- **Lifetime Totals**: Accumulated statistics across all battles (persists throughout the game)

At the end of each battle, current battle stats are automatically merged into lifetime totals. You can access lifetime totals via `BattleStatisticsTracker.Instance.GetLifetimeStatistics()` for display in menus.

## Step 2: Set Up Battle Stakes Notification UI

The stakes notification displays the win condition at battle start.

### 2.1 Create UI Panel

1. In the Battle Scene Canvas, create a new Panel (right-click Canvas → UI → Panel)
2. Name it `BattleStakesNotificationPanel`
3. Set it to fill the entire screen (Anchor Presets: Stretch-Stretch)
4. Add a semi-transparent background (Image component with dark color, ~200 alpha)

### 2.2 Add UI Elements

Inside the panel, create:

1. **Title Text**:
   - Create TextMeshPro - Text (TMP_Text)
   - Name: `TitleText`
   - Set text to "Battle Objective"
   - Center it near the top
   - Font size: 36-48

2. **Stakes Text**:
   - Create TextMeshPro - Text (TMP_Text)
   - Name: `StakesText`
   - Set text to placeholder (will be filled dynamically)
   - Center it in the middle
   - Font size: 24-32

3. **Acknowledge Button**:
   - Create Button (right-click panel → UI → Button - TextMeshPro)
   - Name: `AcknowledgeButton`
   - Set button text to "Begin Battle"
   - Position at the bottom center
   - Size: ~200x50

### 2.3 Add Script Component

1. Select the `BattleStakesNotificationPanel` GameObject
2. Add Component → `BattleStakesNotificationUI` (found in `Assets/Scripts/UI/BattleStakesNotificationUI.cs`)
3. Assign references in the Inspector:
   - **Notification Panel**: Drag the panel GameObject itself
   - **Title Text**: Drag the `TitleText` GameObject
   - **Stakes Text**: Drag the `StakesText` GameObject
   - **Acknowledge Button**: Drag the `AcknowledgeButton` GameObject

### 2.4 Configure Text Templates (Optional)

You can customize the text templates in the Inspector:
- **Kill All Template**: "Defeat All Enemies" (default)
- **Survive Rounds Template**: "Survive {0} Rounds" (default)
- **Protect Target Template**: "Defend {0}" (default)
- **Reach Location Template**: "Reach {0}" (default)
- **Custom Template**: "{0}" (default)

## Step 3: Set Up Victory Notification UI

The victory notification displays after battle victory.

### 3.1 Create UI Panel

1. In the Battle Scene Canvas, create a new Panel
2. Name it `VictoryNotificationPanel`
3. Set it to fill the entire screen
4. Add a semi-transparent background

### 3.2 Add UI Elements

Inside the panel, create:

1. **Title Text**:
   - Create TextMeshPro - Text
   - Name: `TitleText`
   - Set text to "Victory!"
   - Center it near the top
   - Font size: 48-60

2. **Message Text**:
   - Create TextMeshPro - Text
   - Name: `MessageText`
   - Set text to "You have emerged victorious!"
   - Center it in the middle
   - Font size: 24-32

3. **Acknowledge Button**:
   - Create Button
   - Name: `AcknowledgeButton`
   - Set button text to "Continue"
   - Position at the bottom center

### 3.3 Add Script Component

1. Select the `VictoryNotificationPanel` GameObject
2. Add Component → `VictoryNotificationUI`
3. Assign references:
   - **Notification Panel**: The panel GameObject
   - **Title Text**: The `TitleText` GameObject
   - **Message Text**: The `MessageText` GameObject
   - **Acknowledge Button**: The `AcknowledgeButton` GameObject

### 3.4 Configure Messages (Optional)

You can customize the messages in the Inspector:
- **Victory Title**: "Victory!" (default)
- **Victory Message**: "You have emerged victorious!" (default)

## Step 4: Set Up Battle Spoils UI

The spoils screen displays comprehensive battle statistics.

### 4.1 Create UI Panel

1. In the Battle Scene Canvas, create a new Panel
2. Name it `BattleSpoilsPanel`
3. Set it to fill the entire screen
4. Add a background

### 4.2 Create Party Totals Section

1. Inside the panel, create an empty GameObject named `PartyTotalsSection`
2. Add a Vertical Layout Group component
3. Create TextMeshPro - Text elements for each stat:
   - `TotalSPText`: "SP Earned: 0"
   - `TotalKillsText`: "Kills: 0"
   - `TotalCritsText`: "Critical Hits: 0"
   - `TotalDamageText`: "Damage Dealt: 0"
   - `TotalSkillsUsedText`: "Skills Used: 0"
   - `TotalSkillsMasteredText`: "Skills Mastered: 0"

### 4.3 Create Per-Character Section

1. Create an empty GameObject named `CharacterStatsContainer`
2. Add a Vertical Layout Group component
3. Add a Content Size Fitter (Vertical Fit: Preferred Size)
4. Optionally create a Scroll View if you expect many characters

**Note**: The script will dynamically create character stat entries. You can create a prefab for better styling (see below).

### 4.4 Create Skills Mastered Section

1. Create an empty GameObject named `SkillsMasteredSection`
2. Add a Vertical Layout Group component
3. Create an empty GameObject named `SkillsMasteredContainer` inside it
4. Add a Vertical Layout Group to the container

**Note**: The script will dynamically create skill mastered entries. You can create a prefab for better styling.

### 4.5 Add Continue Button

1. Create a Button at the bottom
2. Name: `ContinueButton`
3. Set text to "Continue"

### 4.6 Add Script Component

1. Select the `BattleSpoilsPanel` GameObject
2. Add Component → `BattleSpoilsUI`
3. Assign references:
   - **Spoils Panel**: The panel GameObject
   - **Title Text**: (Optional) A title text element
   - **Continue Button**: The `ContinueButton`
   - **Party Totals Section**: The `PartyTotalsSection` GameObject
   - **Total SP Text**: The `TotalSPText`
   - **Total Kills Text**: The `TotalKillsText`
   - **Total Crits Text**: The `TotalCritsText`
   - **Total Damage Text**: The `TotalDamageText`
   - **Total Skills Used Text**: The `TotalSkillsUsedText`
   - **Total Skills Mastered Text**: The `TotalSkillsMasteredText`
   - **Character Stats Container**: The `CharacterStatsContainer` GameObject
   - **Character Stat Prefab**: (Optional) A prefab for character stat entries
   - **Skills Mastered Section**: The `SkillsMasteredSection` GameObject
   - **Skills Mastered Container**: The `SkillsMasteredContainer` GameObject
   - **Skill Mastered Prefab**: (Optional) A prefab for skill mastered entries

### 4.7 Create Prefabs (Optional but Recommended)

For better styling, create prefabs:

**Character Stat Prefab**:
1. Create a Panel or Image GameObject
2. Add TextMeshPro - Text elements for:
   - Character name
   - SP, Kills, Crits
   - Damage, Skills Used
3. Add Horizontal/Vertical Layout Groups for organization
4. Save as prefab: `CharacterStatEntryPrefab`

**Skill Mastered Prefab**:
1. Create a TextMeshPro - Text GameObject
2. Set default text to placeholder
3. Save as prefab: `SkillMasteredEntryPrefab`

Assign these prefabs in the `BattleSpoilsUI` component.

## Step 5: Set Up Loot Selection UI

The loot selection screen allows players to choose which items to take.

### 5.1 Create UI Panel

1. In the Battle Scene Canvas, create a new Panel
2. Name it `LootSelectionPanel`
3. Set it to fill the entire screen
4. Add a background

### 5.2 Create Title and Buttons

1. Create a TextMeshPro - Text for the title: `TitleText` ("Battle Loot")
2. Create three buttons:
   - `TakeAllButton`: "Take All"
   - `LeaveAllButton`: "Leave All"
   - `ConfirmButton`: "Confirm"

### 5.3 Create Loot Items Container

1. Create an empty GameObject named `LootItemsContainer`
2. Add a Vertical Layout Group component
3. Add a Content Size Fitter (Vertical Fit: Preferred Size)
4. Optionally wrap in a Scroll View if you expect many items

### 5.4 Create Currency Section

1. Create an empty GameObject named `CurrencySection`
2. Add a TextMeshPro - Text: `CurrencyText` ("Aurum Shards: 0")

### 5.5 Create Weight/Capacity Display

1. Create TextMeshPro - Text elements:
   - `WeightText`: "Selected Weight: 0.00 kg"
   - `CapacityText`: "Capacity: 0.00 kg"

### 5.6 Add Script Component

1. Select the `LootSelectionPanel` GameObject
2. Add Component → `LootSelectionUI`
3. Assign references:
   - **Loot Panel**: The panel GameObject
   - **Title Text**: The `TitleText`
   - **Take All Button**: The `TakeAllButton`
   - **Leave All Button**: The `LeaveAllButton`
   - **Confirm Button**: The `ConfirmButton`
   - **Loot Items Container**: The `LootItemsContainer` GameObject
   - **Loot Item Prefab**: (Optional) A prefab for loot item entries
   - **Currency Section**: The `CurrencySection` GameObject
   - **Currency Text**: The `CurrencyText`
   - **Weight Text**: The `WeightText`
   - **Capacity Text**: The `CapacityText`

### 5.7 Create Loot Item Prefab (Optional but Recommended)

For better styling, create a prefab:

1. Create a Panel or Image GameObject
2. Add a Toggle component (for checkbox)
3. Add TextMeshPro - Text for item name and quantity
4. Add Horizontal Layout Group
5. Save as prefab: `LootItemEntryPrefab`

Assign this prefab in the `LootSelectionUI` component.

## Step 6: Verify Component Integration

### 6.1 CombatInitiator

The `CombatInitiator` should automatically:
- Find and use `BattleStakesNotificationUI` if present
- Initialize `BattleStatisticsTracker` when combat starts

No additional configuration needed if components are in the scene.

### 6.2 BattleEndHandler

The `BattleEndHandler` should automatically:
- Find and use `VictoryNotificationUI`, `BattleSpoilsUI`, and `LootSelectionUI` if present
- Show them in sequence after victory

No additional configuration needed if components are in the scene.

### 6.3 TurnManager

The `TurnManager` will automatically:
- Receive encounter data from `CombatInitiator`
- Check win conditions based on `EncounterData.VictoryCondition`

No additional configuration needed.

## Step 7: Configure EncounterData Assets

For each EncounterData asset:

1. Select the EncounterData asset in the Project window
2. In the Inspector, set:
   - **Victory Condition**: Choose from:
     - `KillAll` (default): "Defeat All Enemies"
     - `SurviveXRounds`: "Survive X Rounds" (set `Turn Limit` to X)
     - `ProtectTarget`: "Defend [Character]" (requires target unit - TODO)
     - `ReachLocation`: "Reach [Location]" (requires target position - TODO)
     - `Custom`: "Custom Objective" (requires custom text - TODO)
   - **Turn Limit**: Number of rounds for `SurviveXRounds` condition

## Step 8: Testing

### 8.1 Test Battle Start

1. Start a battle from the exploration scene
2. Verify the stakes notification appears
3. Verify the win condition text is correct
4. Click "Begin Battle"
5. Verify battle proceeds normally

### 8.2 Test Battle Statistics

1. During battle, perform actions (attacks, skills, etc.)
2. Verify statistics are being tracked (check Debug logs)
3. Win the battle
4. Verify spoils screen shows correct statistics

### 8.3 Test Battle End Flow

1. Win a battle
2. Verify victory notification appears
3. Click "Continue"
4. Verify spoils screen appears with statistics
5. Click "Continue"
6. Verify loot selection appears (if loot exists)
7. Select items and click "Confirm"
8. Verify transition back to exploration

### 8.4 Test Loot Selection

1. Win a battle with loot
2. Verify loot items appear in the selection screen
3. Test "Take All" button
4. Test "Leave All" button
5. Test individual item selection
6. Verify weight/capacity display updates
7. Click "Confirm" and verify items are added to inventory

## Quick Setup Checklist

Before testing, ensure:

1. **All UI Components Exist in Scene**:
   - `BattleStakesNotificationUI` component (can be on inactive GameObject)
   - `VictoryNotificationUI` component (can be on inactive GameObject)
   - `BattleSpoilsUI` component (can be on inactive GameObject)
   - `LootSelectionUI` component (can be on inactive GameObject)

2. **All Panel References Assigned**:
   - Each component must have its `notificationPanel` or `spoilsPanel` or `lootPanel` field assigned
   - The panel GameObject must exist (even if inactive initially)

3. **Panels Are Children of Canvas**:
   - All UI panels must be children of a Canvas GameObject
   - Canvas must have a GraphicRaycaster component

4. **Check Debug Console**:
   - Look for messages starting with "BattleEndHandler:", "CombatInitiator:", or component names
   - Error messages will indicate what's missing

## Troubleshooting

### Panels Don't Show Up At All

**Most Common Issues**:

1. **Component Not in Scene**:
   - Verify the UI component script is attached to a GameObject in the Battle Scene
   - The GameObject can be inactive, but the component must exist
   - Use `FindFirstObjectByType` in the scene hierarchy to verify

2. **Panel Reference Not Assigned**:
   - In the Inspector, check that the panel field is assigned
   - The panel GameObject must exist (create it if missing)
   - Panel can be inactive initially - it will be activated by the script

3. **Panel Not Under Canvas**:
   - UI elements must be children of a Canvas
   - Check the hierarchy: Canvas → YourPanel
   - Canvas must have GraphicRaycaster component

4. **Check Debug Logs**:
   - Open Unity Console (Window → General → Console)
   - Look for error messages when battle starts/ends
   - Messages will indicate which component is missing or which reference is null

### Stakes Notification Doesn't Appear

- Verify `BattleStakesNotificationUI` component is in the scene (can be on inactive GameObject)
- Verify the panel GameObject is assigned in the component
- Verify the panel GameObject exists (even if inactive initially)
- Check Debug logs for messages like "CombatInitiator: Found BattleStakesNotificationUI" or "BattleStakesNotificationUI not found"
- Ensure the panel GameObject is a child of a Canvas
- Check that `notificationPanel` field is assigned in the Inspector

### Statistics Not Tracking

- Verify `BattleStatisticsTracker` GameObject exists in the scene
- Check that it's initialized before combat starts
- Verify GameEvents are being raised (check Debug logs)
- The tracker persists across scenes - if you see duplicate instances, check for multiple GameObjects

### Lifetime Statistics Access

To display lifetime statistics in menus:
```csharp
if (BattleStatisticsTracker.Instance != null)
{
    BattleStatistics stats = BattleStatisticsTracker.Instance.GetLifetimeStatistics();
    int totalKills = stats.LifetimeTotalKills;
    int totalSP = stats.LifetimeTotalSPEarned;
    // etc.
}
```

### Spoils Screen Doesn't Show

- Verify `BattleSpoilsUI` component is in the scene (can be on inactive GameObject)
- Verify `spoilsPanel` GameObject is assigned in the component
- Verify all UI references are assigned (check for null reference warnings in Debug logs)
- Check Debug logs for "BattleEndHandler: Found BattleSpoilsUI" or "BattleSpoilsUI not found"
- Ensure the panel GameObject is a child of a Canvas
- Check that `BattleStatisticsTracker.Instance` is not null

### Loot Selection Doesn't Show

- Verify `LootSelectionUI` component is in the scene (can be on inactive GameObject)
- Verify `lootPanel` GameObject is assigned in the component
- Verify `LootManager.Instance` is not null
- Check Debug logs for "BattleEndHandler: Found LootSelectionUI" and loot count
- Ensure the panel GameObject is a child of a Canvas
- Loot selection only shows if there's loot or currency - check Debug logs for "Loot count: X, Currency: Y"

### Win Conditions Not Working

- Verify `EncounterData` has the correct `VictoryCondition` set
- For `SurviveXRounds`, verify `TurnLimit` is set correctly
- Check Debug logs for win condition checking

## UI Styling Tips

1. **Consistent Design**: Use the same color scheme and fonts across all panels
2. **Readability**: Ensure text is large enough and has good contrast
3. **Layout**: Use Layout Groups for automatic spacing and organization
4. **Animations**: Consider adding fade-in/fade-out animations for panels
5. **Prefabs**: Create prefabs for dynamic entries to maintain consistent styling

## Advanced Configuration

### Custom Win Condition Text

To customize win condition text, modify the templates in `BattleStakesNotificationUI`:
- Edit the template strings in the Inspector
- Use `{0}` as a placeholder for dynamic values

### Custom Statistics Display

To customize the statistics display:
- Create custom prefabs for character stats and skill mastered entries
- Modify `BattleSpoilsUI.FormatCharacterStats()` if needed
- Add additional UI elements for more detailed breakdowns

### Accessing Lifetime Statistics in Menus

The `BattleStatisticsTracker` automatically tracks lifetime totals. To display them in your status menu:

```csharp
using Riftbourne.Combat;

// In your menu UI script
void UpdateStatisticsDisplay()
{
    if (BattleStatisticsTracker.Instance == null) return;
    
    BattleStatistics lifetimeStats = BattleStatisticsTracker.Instance.GetLifetimeStatistics();
    
    // Display party-wide totals
    totalKillsText.text = $"Total Kills: {lifetimeStats.LifetimeTotalKills}";
    totalSPText.text = $"Total SP Earned: {lifetimeStats.LifetimeTotalSPEarned}";
    // etc.
    
    // Display per-character lifetime stats
    Dictionary<string, CharacterBattleStats> charStats = lifetimeStats.GetLifetimeCharacterStats();
    foreach (var kvp in charStats)
    {
        CharacterBattleStats stats = kvp.Value;
        // Display stats for each character
    }
}
```

### Debug Logging

All UI components now include extensive debug logging. Check the Unity Console for:
- Component discovery messages
- Panel activation confirmations
- Missing reference warnings
- Flow progression messages

If panels don't appear, check the Console for specific error messages indicating what's missing.

## Next Steps

- Add animations to UI panels
- Create custom prefabs for better styling
- Implement remaining win conditions (ProtectTarget, ReachLocation, Custom)
- Add sound effects for UI interactions
- Add visual effects for victory/skills mastered
