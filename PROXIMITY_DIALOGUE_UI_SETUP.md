# Proximity Dialogue UI Setup Guide

This guide explains how to manually create the 2D subtitle UI for proximity dialogue in Unity.

## Overview

The dialogue UI is now a manually-created GameObject in your scene that gets activated/deactivated by the `ProximityDialogueUI` script. This gives you full control over positioning, styling, and layout in the Unity Editor.

## Step 1: Create the Dialogue UI GameObject

1. In your exploration scene, right-click in Hierarchy
2. Select **UI** → **Canvas** (or create empty GameObject and add Canvas component)
3. Name it "ProximityDialogueCanvas"

### Configure Canvas

1. Select the Canvas
2. In Inspector, set:
   - **Render Mode**: Screen Space - Overlay
   - **Sort Order**: 1000 (or high number to appear on top)

3. Add **Canvas Scaler** component:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080
   - **Match**: 0.5 (or your preference)

4. Add **Graphic Raycaster** component (if not auto-added)

## Step 2: Create Dialogue Panel

1. Right-click on "ProximityDialogueCanvas" in Hierarchy
2. Select **UI** → **Panel** (or create empty GameObject and add Image component)
3. Name it "DialoguePanel"

### Configure Dialogue Panel

1. Select "DialoguePanel"
2. In Inspector, find the **Rect Transform**:
   - Click the anchor preset box (top-left of Rect Transform)
   - Hold **Shift + Alt** and click the **bottom-center** preset
   - This anchors it to the bottom center of the screen

3. Set **Rect Transform** values:
   - **Width**: 1200 (or your preferred width)
   - **Height**: 80 (or your preferred height)
   - **Pos Y**: 50 (distance from bottom of screen)
   - **Pos X**: 0 (centered)

4. If using **Image** component (from Panel):
   - **Color**: Semi-transparent black (0, 0, 0, 0.7)
   - **Raycast Target**: Unchecked (optional)

## Step 3: Create Text Component

1. Right-click on "DialoguePanel" in Hierarchy
2. Select **UI** → **Text - TextMeshPro** (recommended) or **UI** → **Text - Legacy**
3. Name it "DialogueText"

### Configure Text

1. Select "DialogueText"
2. In Inspector, set **Rect Transform**:
   - **Anchor Presets**: Stretch to fill parent (Alt + Shift + click stretch preset)
   - **Left/Right/Top/Bottom**: 20 (padding inside panel)

3. Configure **Text** component:
   - **Text**: "Sample Dialogue Text" (for preview)
   - **Font**: Your preferred font (or leave default)
   - **Font Size**: 24 (adjust as needed)
   - **Alignment**: Center (horizontal and vertical)
   - **Color**: White
   - **Wrap**: Enabled

## Step 4: Add ProximityDialogueUI Script

1. Create an empty GameObject in the scene (or use an existing manager GameObject)
2. Name it "ProximityDialogueUI"
3. Add Component → Search "ProximityDialogueUI" → Add

### Configure ProximityDialogueUI Component

1. Select "ProximityDialogueUI" GameObject
2. In Inspector, find **ProximityDialogueUI** component
3. Assign references:
   - **Dialogue Panel**: Drag "DialoguePanel" from Hierarchy
   - **Dialogue Text**: Drag "DialogueText" from Hierarchy
   - **Background Image**: (Optional) Drag the Image component from "DialoguePanel"

4. Configure settings:
   - **Fade Duration**: 0.5 (seconds for fade in/out)

## Step 5: Test

1. Enter Play Mode
2. Move player near an NPC with proximity dialogue
3. The dialogue should appear at the bottom of the screen with fade animation

## Troubleshooting

### Dialogue doesn't appear

- ✅ Check that "DialoguePanel" is assigned in ProximityDialogueUI component
- ✅ Check that "DialogueText" is assigned
- ✅ Verify the panel is positioned at bottom of screen (check Rect Transform)
- ✅ Check Console for error messages

### Dialogue appears in wrong position

- Check Rect Transform anchor presets on "DialoguePanel"
- Ensure it's anchored to bottom-center or bottom-left
- Adjust Pos Y value to move it up/down from bottom

### Text doesn't show

- ✅ Verify "DialogueText" is assigned in ProximityDialogueUI
- ✅ Check that Text component has content
- ✅ Verify font is assigned
- ✅ Check Text color (should be visible)

### Fade animation not working

- ✅ Ensure CanvasGroup component exists on "DialoguePanel"
- ✅ The script will auto-add CanvasGroup if missing
- ✅ Check Fade Duration setting (should be > 0)

## Styling Tips

- **Background**: Adjust Image color and alpha for different styles
- **Text**: Use TextMeshPro for better text rendering and effects
- **Positioning**: Adjust Rect Transform values to fine-tune position
- **Size**: Adjust panel width/height to fit your design
- **Padding**: Adjust text Rect Transform margins for spacing

## Example Setup

```
ProximityDialogueCanvas (Canvas)
└── DialoguePanel (Image/Panel)
    └── DialogueText (Text)
```

The ProximityDialogueUI script should be on a separate GameObject (not a child of the canvas).

## Integration with Manager

The `ProximityDialogueManager` will automatically find the `ProximityDialogueUI` component in the scene, or create one if missing. However, you should set up the UI manually as described above for best results.
