# Status Menu Troubleshooting Guide

If the Status Menu (TAB key) isn't working, follow these steps:

## Step 1: Check Unity Console for Errors

1. Open Unity Console (Window > General > Console)
2. Press Play
3. Look for any errors or warnings related to:
   - StatusMenuUI
   - PlayerInputActions
   - Input System

## Step 2: Regenerate Input Actions C# Class

**This is the most common issue!** Unity needs to regenerate the C# class after we modified the .inputactions file.

1. In Project window, find `Assets/PlayerInputActions.inputactions`
2. Select it
3. In Inspector, you should see "Input Actions" asset
4. Right-click the file and select **Reimport**
5. OR: Close and reopen Unity (this forces regeneration)
6. Check Console - you should see "Reimported PlayerInputActions.inputactions"

## Step 3: Verify StatusMenuUI is Set Up

1. In Hierarchy, find your **StatusMenuCanvas** (or the GameObject with StatusMenuUI script)
2. Select it
3. In Inspector, check:
   - ✅ **StatusMenuUI component exists**
   - ✅ **Status Menu Panel** is assigned (drag StatusMenuPanel from Hierarchy)
   - ✅ GameObject is **enabled** (checkbox at top of Inspector)
   - ✅ StatusMenuCanvas is **active** in Hierarchy

## Step 4: Check Input Actions Binding

1. Select `Assets/PlayerInputActions.inputactions` in Project
2. In Inspector, click **Open** (or double-click)
3. This opens the Input Actions editor
4. Expand **Gameplay** map
5. Find **StatusMenu** action
6. Verify it has a binding to **Tab** key
7. If missing, add binding:
   - Click **+** next to StatusMenu
   - Select **Add Binding**
   - Click the binding, then press TAB key on keyboard
   - Save (Ctrl+S)

## Step 5: Test with Debug Logging

I've added debug logging to the script. When you press TAB, you should see in Console:

```
[StatusMenuUI] OnEnable called - setting up input
[StatusMenuUI] StatusMenu input action subscribed successfully
[StatusMenuUI] TAB key pressed - OnStatusMenuToggle called
[StatusMenuUI] Panel is currently inactive, toggling...
[StatusMenuUI] Opening status menu - pausing game
```

If you DON'T see these messages:
- The input action isn't being triggered
- Check Step 2 (regenerate C# class)
- Check that StatusMenuUI GameObject is enabled

## Step 6: Manual Test - Check Input System

Add this temporary test to verify input is working:

1. Create a new script or add to StatusMenuUI's Update():

```csharp
private void Update()
{
    // Temporary test - remove after debugging
    if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
    {
        Debug.Log("[TEST] TAB key detected via Keyboard.current");
    }
}
```

If this works but StatusMenu doesn't, the issue is with the Input Actions setup.

## Step 7: Verify No Conflicts

Check if other scripts are consuming the TAB key:

1. Search your project for "tab" (case-insensitive)
2. Check if any other scripts use TAB key
3. Unity's Input System allows multiple listeners, but check for conflicts

## Step 8: Check GameObject Hierarchy

Make sure:
- StatusMenuCanvas is in the scene
- StatusMenuCanvas is **active** (not disabled)
- StatusMenuPanel is a child of StatusMenuCanvas
- StatusMenuPanel starts **inactive** (script sets it inactive in Awake)

## Step 9: Alternative - Use Keyboard.current Directly

If Input Actions still don't work, we can use direct keyboard input as a fallback. Let me know if you want me to add this.

## Quick Fix Checklist

- [ ] Reimported PlayerInputActions.inputactions
- [ ] StatusMenuUI script is on an active GameObject
- [ ] Status Menu Panel is assigned in Inspector
- [ ] No errors in Console
- [ ] TAB key binding exists in Input Actions editor
- [ ] StatusMenuCanvas is active in scene

## Still Not Working?

If none of the above works, check:

1. **Input System Package**: Make sure Unity's Input System package is installed
   - Window > Package Manager
   - Search for "Input System"
   - Should be version 1.7.0 or higher

2. **Project Settings**: 
   - Edit > Project Settings > Player
   - Under "Other Settings" > "Active Input Handling"
   - Should be "Input System Package (New)" or "Both"

3. **Console Errors**: Share any error messages you see
