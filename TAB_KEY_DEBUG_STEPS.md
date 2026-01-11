# TAB Key Not Working - Debug Steps

If you're getting NO debug messages at all, the script isn't running. Follow these steps:

## Step 1: Verify Script Exists in Scene

1. **In Hierarchy**, look for:
   - `StatusMenuCanvas` (or whatever you named it)
   - OR any GameObject that has `StatusMenuUI` component

2. **If you don't see it:**
   - The UI wasn't set up yet
   - Go back to `NARRATIVE_SKILLS_SETUP_GUIDE.md` Part 2
   - Create the Status Menu UI first

## Step 2: Quick Test - Add Test Script

I've created a simple test script. Let's verify input is working:

1. **In Hierarchy**, right-click and create an **Empty GameObject**
2. Name it "TestInput"
3. **Add Component** → Search for "StatusMenuUITest"
4. Press Play
5. Press TAB
6. **Check Console** - you should see:
   ```
   [TEST] StatusMenuUITest script is running!
   [TEST] TAB key detected via Keyboard.current!
   ```

**If you see these messages:**
- ✅ Input System is working
- ✅ The problem is with StatusMenuUI setup
- Go to Step 3

**If you DON'T see any messages:**
- ❌ The script isn't running
- Check: Is the GameObject enabled? Is the script enabled?

## Step 3: Check StatusMenuUI Setup

1. **Find StatusMenuCanvas** in Hierarchy
2. **Select it**
3. **In Inspector**, check:
   - ✅ GameObject is **enabled** (checkbox at top)
   - ✅ **StatusMenuUI** component exists
   - ✅ **StatusMenuUI** component is **enabled** (checkbox on component)
   - ✅ **Status Menu Panel** field is assigned

4. **If Status Menu Panel is NULL:**
   - The panel wasn't assigned
   - Drag `StatusMenuPanel` from Hierarchy into that field

## Step 4: Verify Script is Running

1. **Select StatusMenuCanvas** in Hierarchy
2. **In Inspector**, find StatusMenuUI component
3. **Press Play**
4. **Check Console** - you should see:
   ```
   [StatusMenuUI] Awake() called - script is active!
   [StatusMenuUI] Start() called
   ```

**If you DON'T see these:**
- The script isn't attached or GameObject is disabled
- Check Step 3 again

## Step 5: Test TAB Key Directly

1. **Press Play**
2. **Press TAB**
3. **Check Console** for:
   ```
   [StatusMenuUI] TAB key detected in Update() - toggling menu
   ```

**If you see this but menu doesn't open:**
- The panel reference is missing
- Check Step 3 - assign Status Menu Panel

## Step 6: Common Issues

### Issue: "StatusMenuCanvas doesn't exist"
**Solution:** You need to create the UI first. Follow `NARRATIVE_SKILLS_SETUP_GUIDE.md` Part 2.

### Issue: "Status Menu Panel is null"
**Solution:** 
1. Make sure you created `StatusMenuPanel` as a child of `StatusMenuCanvas`
2. Drag it from Hierarchy into the StatusMenuUI component's "Status Menu Panel" field

### Issue: "No debug messages at all"
**Solution:**
1. Check that the GameObject with StatusMenuUI is **active** in Hierarchy
2. Check that the **StatusMenuUI component is enabled** (checkbox checked)
3. Make sure you're in **Play mode** (not Edit mode)

### Issue: "Input System errors in Console"
**Solution:**
1. Window > Package Manager
2. Search "Input System"
3. Make sure it's installed (version 1.7.0+)
4. Edit > Project Settings > Player > Active Input Handling
5. Set to "Input System Package (New)" or "Both"

## Step 7: Manual Panel Toggle Test

If TAB still doesn't work, let's test if the panel toggle works at all:

1. **Select StatusMenuPanel** in Hierarchy
2. **In Inspector**, toggle the **active checkbox** (top left)
3. **Does the panel appear/disappear?**
   - ✅ Yes: Panel works, issue is with input
   - ❌ No: Panel setup issue

## Still Not Working?

Share with me:
1. **What do you see in Console when you press Play?** (any StatusMenuUI messages?)
2. **What do you see when you press TAB?** (any messages?)
3. **Does StatusMenuCanvas exist in your scene?**
4. **Is Status Menu Panel assigned in the StatusMenuUI component?**
