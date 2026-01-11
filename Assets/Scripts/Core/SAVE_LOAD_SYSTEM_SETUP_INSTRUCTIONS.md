# Save/Load System Setup Instructions

## Overview

This guide will help you set up the save/load system in Unity. The system includes:
- **Autosave**: 5 slots, automatically saves after battle ends
- **Quicksave**: 5 slots, triggered by F5 key
- **Manual Save**: Unlimited saves with custom names, accessible via system menu

## Prerequisites

All scripts have been created. You need to:
1. Set up the SaveManager GameObject
2. Create the System Menu UI
3. Connect all components
4. Test the system

## Step 1: Create SaveManager GameObject

1. In your scene (preferably a persistent scene or the first scene that loads), create a new GameObject:
   - Right-click in Hierarchy → **Create Empty**
   - Rename to "SaveManager"

2. Add the SaveManager component:
   - Select the SaveManager GameObject
   - In Inspector, click **Add Component**
   - Search for "SaveManager" and add it

3. Configure SaveManager:
   - **Max Autosave Slots**: 5 (default)
   - **Max Quicksave Slots**: 5 (default)
   - **Input Actions**: Leave empty (will be created automatically)

4. Ensure it persists:
   - The SaveManager script already uses `DontDestroyOnLoad`, so it will persist across scenes

## Step 2: Create System Menu UI Canvas

1. Create a new Canvas for the system menu:
   - Right-click in Hierarchy → **UI → Canvas**
   - Rename to "SystemMenuCanvas"
   - In Inspector:
     - **Render Mode**: Screen Space - Overlay
     - **Sort Order**: 30 (above Status Menu, which is 20)

2. Create the main panel:
   - Right-click on SystemMenuCanvas → **UI → Panel**
   - Rename to "SystemMenuPanel"
   - In Inspector:
     - Set **Anchor Presets** to stretch-stretch (hold Alt)
     - Set margins: Left: 200, Right: 200, Top: 100, Bottom: 100 (centered panel)
     - Set **Color**: Semi-transparent dark (e.g., R: 0.1, G: 0.1, B: 0.1, A: 0.95)

## Step 3: Create System Menu Buttons

1. Create button container:
   - Right-click on SystemMenuPanel → **UI → Panel**
   - Rename to "ButtonContainer"
   - Add **Vertical Layout Group** component:
     - **Spacing**: 20
     - **Padding**: Left: 20, Right: 20, Top: 20, Bottom: 20
     - **Child Alignment**: Middle Center
     - **Child Control Width**: true
     - **Child Control Height**: false

2. Create menu buttons (inside ButtonContainer):
   - Right-click on ButtonContainer → **UI → Button - TextMeshPro**
   - Rename to "SaveButton"
   - Set button text to "Save"
   - Duplicate this button 4 times:
     - **LoadButton** (text: "Load")
     - **SettingsButton** (text: "Settings")
     - **QuitButton** (text: "Quit")
     - **CloseButton** (text: "Close")

3. Arrange buttons vertically in the ButtonContainer

## Step 4: Create Save/Load UI Panels

### Save Panel

1. Create save panel:
   - Right-click on SystemMenuPanel → **UI → Panel**
   - Rename to "SavePanel"
   - Set **Anchor Presets** to stretch-stretch (hold Alt), margins to 0
   - Initially set **Active** to false

2. Add UI elements to SavePanel:
   - **Save Type Dropdown**:
     - Right-click on SavePanel → **UI → Dropdown - TextMeshPro**
     - Rename to "SaveTypeDropdown"
   - **Save Name Input**:
     - Right-click on SavePanel → **UI → Input Field - TextMeshPro**
     - Rename to "SaveNameInput"
     - Set **Placeholder** text to "Enter save name..."
   - **Save Confirm Button**:
     - Right-click on SavePanel → **UI → Button - TextMeshPro**
     - Rename to "SaveConfirmButton"
     - Set text to "Save"
   - **Save Cancel Button**:
     - Right-click on SavePanel → **UI → Button - TextMeshPro**
     - Rename to "SaveCancelButton"
     - Set text to "Cancel"
   - **Save Status Text**:
     - Right-click on SavePanel → **UI → Text - TextMeshPro**
     - Rename to "SaveStatusText"
     - Set text to ""

### Load Panel

1. Create load panel:
   - Right-click on SystemMenuPanel → **UI → Panel**
   - Rename to "LoadPanel"
   - Set **Anchor Presets** to stretch-stretch (hold Alt), margins to 0
   - Initially set **Active** to false

2. Add UI elements to LoadPanel:
   - **Save List Container**:
     - Right-click on LoadPanel → **UI → Panel**
     - Rename to "SaveListContainer"
     - Add **Vertical Layout Group**:
       - **Spacing**: 10
       - **Padding**: 20
       - **Child Control Width**: true
       - **Child Control Height**: false
     - Add **Content Size Fitter**:
       - **Vertical Fit**: Preferred Size
     - Add **Scroll Rect** component (optional, for scrolling):
       - **Content**: Assign SaveListContainer
       - **Vertical Scrollbar**: Create a scrollbar if needed
   - **Load Cancel Button**:
     - Right-click on LoadPanel → **UI → Button - TextMeshPro**
     - Rename to "LoadCancelButton"
     - Set text to "Cancel"
   - **Load Status Text**:
     - Right-click on LoadPanel → **UI → Text - TextMeshPro**
     - Rename to "LoadStatusText"
     - Set text to ""

3. Create Save Item Prefab (RECOMMENDED for better UI):
   - Right-click in Project window → **Create → Prefab**
   - Rename to "SaveItemPrefab"
   - Drag it into scene, set up UI:
     - **IMPORTANT**: Ensure the root GameObject has a **RectTransform** (not Transform)
     - If it has Transform, delete the prefab instance and create a new GameObject with **UI → Panel** instead
     - Add **Horizontal Layout Group** to root
     - Add **Button** component to root (makes entire item clickable)
     - Add **Image** component to root (for background)
   
   - Create child objects:
     - **Screenshot** (UI → Image): 80x80 pixels, for screenshot thumbnail
     - **InfoContainer** (empty GameObject with Vertical Layout Group)
       - **NameText** (UI → Text - TextMeshPro): For save name
       - **TimestampText** (UI → Text - TextMeshPro): For timestamp
       - **ChapterText** (UI → Text - TextMeshPro): For chapter name
       - **TypeText** (UI → Text - TextMeshPro): For save type
   
   - **Add SaveItemUI Component** (IMPORTANT):
     - Select the root GameObject of the prefab
     - Click **Add Component**
     - Search for "SaveItemUI" and add it
     - In the SaveItemUI component, assign all references:
       - **Screenshot Image**: Drag the "Screenshot" Image
       - **Save Name Text**: Drag the "NameText" TextMeshPro
       - **Timestamp Text**: Drag the "TimestampText" TextMeshPro
       - **Chapter Text**: Drag the "ChapterText" TextMeshPro
       - **Save Type Text**: Drag the "TypeText" TextMeshPro
       - **Load Button**: Drag the root Button component (or leave null to auto-find)
   
   - Drag back to Project to save as prefab
   - Delete from scene
   
   **Note**: If you get a "Prefab mismatch" warning, Unity will auto-fix it. To avoid this:
   - Always create UI elements using Unity's UI menu (UI → Panel, UI → Button, etc.)
   - Never create empty GameObjects and manually add RectTransform - use UI menu items instead
   
   **Why SaveItemUI Component?**
   - The SaveItemUI component holds direct references to all UI elements
   - This is more reliable than name-based matching
   - If you don't add this component, the system will try to find components by name (less reliable)

## Step 5: Connect Components

1. Select SystemMenuCanvas GameObject

2. Add SystemMenuUI component:
   - Click **Add Component**
   - Search for "SystemMenuUI" and add it

3. Assign references in SystemMenuUI:
   - **System Menu Panel**: SystemMenuPanel
   - **Save Button**: SaveButton
   - **Load Button**: LoadButton
   - **Settings Button**: SettingsButton
   - **Quit Button**: QuitButton
   - **Close Button**: CloseButton
   - **Save Load UI**: (We'll create this next)

4. Create SaveLoadUI GameObject:
   - Right-click on SystemMenuCanvas → **Create Empty**
   - Rename to "SaveLoadUI"
   - Add **SaveLoadUI** component

5. Assign references in SaveLoadUI:
   - **Save Panel**: SavePanel
   - **Load Panel**: LoadPanel
   - **Save Type Dropdown**: SaveTypeDropdown
   - **Save Name Input**: SaveNameInput
   - **Save Confirm Button**: SaveConfirmButton
   - **Save Cancel Button**: SaveCancelButton
   - **Save Status Text**: SaveStatusText
   - **Save List Container**: SaveListContainer
   - **Save Item Prefab**: SaveItemPrefab (if created) or leave null
   - **Load Cancel Button**: LoadCancelButton
   - **Load Status Text**: LoadStatusText

6. Connect SaveLoadUI to SystemMenuUI:
   - Select SystemMenuCanvas
   - In SystemMenuUI component, assign **Save Load UI** to the SaveLoadUI GameObject

## Step 6: Verify Input Actions

1. Open **PlayerInputActions.inputactions** in the Project window

2. Verify the Quicksave action exists:
   - Should have a "Quicksave" action bound to F5
   - If not, the SaveManager handles F5 directly via Update(), so it should still work

3. Regenerate C# class if needed:
   - Select PlayerInputActions.inputactions
   - In Inspector, click **Generate C# Class** (if available)
   - Or Unity should auto-generate it

## Step 7: Test the System

### Test Autosave:
1. Start the game
2. Complete a battle (or trigger battle end event)
3. Check save directory: `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Saves\Autosave\`
4. Should see `save_000.json` and screenshot

### Test Quicksave:
1. Press **F5** during gameplay
2. Check save directory: `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Saves\Quicksave\`
3. Should see `save_000.json` and screenshot

### Test System Menu:
1. Press **Escape** (when status menu is closed)
2. System menu should open
3. Click **Save** → Should show save panel
4. Select "Manual Save", enter a name, click **Save**
5. Check save directory: `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Saves\Manual\`
6. Should see your named save file

### Test Load:
1. Press **Escape** → Click **Load**
2. Should see list of saves with screenshots
3. Click on a save → Should load the game

## Troubleshooting

### Prefab Mismatch Warning (Transform vs RectTransform):
- **Cause**: A prefab has RectTransform but scene instance has Transform (or vice versa)
- **Fix**: Unity auto-fixes this, but to prevent it:
  - Always create UI elements using Unity's UI menu (UI → Panel, UI → Button, etc.)
  - Never create empty GameObjects for UI - use UI menu items which automatically get RectTransform
  - If warning persists, select the GameObject in scene, right-click → **Prefab → Revert** or **Prefab → Apply**

### SaveManager not found:
- Ensure SaveManager GameObject exists in scene
- Check that SaveManager component is attached
- Verify it's not being destroyed

### System menu doesn't open:
- Check that SystemMenuUI component is attached to SystemMenuCanvas
- Verify SystemMenuPanel is assigned
- Check that StatusMenuUI Escape handling is working

### Saves not appearing in load list:
- Check save file directory exists
- Verify save files were created (check file system)
- Check SaveLoadUI has SaveListContainer assigned
- Look for errors in Console

### Screenshots not loading:
- Verify screenshot files exist alongside save files
- Check screenshot path in SaveData is correct
- Ensure ScreenshotCapture is working (check Console for errors)

### Chapter progression not restoring:
- Verify ChapterManager.Instance exists
- Check that GetChapterProgressionState/SetChapterProgressionState methods exist
- Look for errors in Console during load

## File Structure

Save files are stored in:
```
Application.persistentDataPath/Saves/
├── Autosave/
│   ├── save_000.json
│   ├── save_001.json
│   └── Screenshots/
│       ├── save_000.png
│       └── save_001.png
├── Quicksave/
│   ├── save_000.json
│   └── Screenshots/
│       └── save_000.png
└── Manual/
    ├── MySave.json
    └── Screenshots/
        └── MySave.png
```

## Notes

- SaveManager handles F5 quicksave directly via Update() - no input action subscription needed
- Escape key handling is coordinated between StatusMenuUI and SystemMenuUI
- Screenshots are captured at end of frame to ensure everything is rendered
- Save slot rotation automatically shifts older saves down and deletes the oldest when full
- Manual saves are unlimited but should be managed by the user

## Next Steps

- Add confirmation dialog for Quit button
- Implement Settings panel
- Add save file deletion from UI
- Add save file renaming
- Consider adding save file compression for large game states
