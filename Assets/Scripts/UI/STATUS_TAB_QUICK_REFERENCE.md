# Status Tab Quick Reference

## What Changed

### Code Changes
1. ✅ **CharacterDefinition** - Added `fullName` and `title` fields
2. ✅ **StatusMenuUI** - Complete redesign:
   - Party portraits at top (6 slots)
   - Clickable portraits to switch character
   - Large portrait display
   - Full name and title display
   - Narrative skills integrated into status tab
   - Narrative skills tab removed/hidden

### Unity Setup Required

#### Required Assignments in StatusMenuUI Inspector:

**Party Portraits:**
- `partyPortraitsContainer` - Container GameObject for portraits
- `partyPortraitPrefab` - Prefab for portrait UI (optional, will create basic if null)
- `maxPartyPortraits` - Set to 6

**Character Display:**
- `largePortraitImage` - Image component for large portrait
- `characterFullNameText` - TextMeshPro for full name
- `characterTitleText` - TextMeshPro for title

**Narrative Skills:**
- `narrativeSkillsSection` - Container GameObject
- All 9 TextMeshPro fields (perception, interpretive, empathic × level, threshold, description)

## Quick Setup Checklist

- [ ] Create "PartyPortraitsContainer" GameObject in Status Tab Panel
- [ ] Add Horizontal Layout Group to container
- [ ] Create "LargePortrait" Image in Status Tab Panel
- [ ] Create "CharacterFullName" TextMeshPro
- [ ] Create "CharacterTitle" TextMeshPro
- [ ] Create "NarrativeSkillsSection" GameObject
- [ ] Create 9 TextMeshPro components for narrative skills (3 skills × 3 fields each)
- [ ] Assign all fields in StatusMenuUI Inspector
- [ ] Update CharacterDefinition assets with fullName and title
- [ ] Test in Play Mode

## UI Structure Example

```
StatusTabPanel
├── PartyPortraitsContainer (Horizontal Layout)
│   └── [Portraits created dynamically]
├── LargePortrait (Image)
├── CharacterFullName (TextMeshPro)
├── CharacterTitle (TextMeshPro)
├── [Existing stat fields]
└── NarrativeSkillsSection
    ├── PerceptionLevel (TextMeshPro)
    ├── PerceptionThreshold (TextMeshPro)
    ├── PerceptionDescription (TextMeshPro)
    ├── InterpretiveLevel (TextMeshPro)
    ├── InterpretiveThreshold (TextMeshPro)
    ├── InterpretiveDescription (TextMeshPro)
    ├── EmpathicLevel (TextMeshPro)
    ├── EmpathicThreshold (TextMeshPro)
    └── EmpathicDescription (TextMeshPro)
```

## Testing

1. Enter Play Mode
2. Ensure party has members (check Console)
3. Press TAB
4. Verify portraits appear
5. Click portrait → character switches
6. Verify all data displays correctly
