# Combat Boost UI Indicator - Setup Guide

## ✅ What's Been Implemented

All the code for the combat boost UI indicator is ready! Here's what was added:

### Code Changes

1. **UIInventory.cs** - Added combat boost UI support
   - New sprite fields for active/inactive states
   - `CombatBoostIndicator` GameObject reference
   - `InitializeCombatBoostIndicator()` - Sets up indicator on start
   - `UpdateCombatBoostIndicator(bool)` - Updates visual state

2. **InventoryBackend.cs** - Wired up UI updates
   - Calls `UpdateCombatBoostIndicator(true)` when active boost detected
   - Calls `UpdateCombatBoostIndicator(false)` when no boost or expired

3. **Sprite Instructions** - See `Assets/Resources/Sprites/BOOST_ICONS_README.md`

## 🎨 Step 1: Create the Icon Sprites

You need two PNG icons (512x512 with transparency):

### Option A: Quick Placeholder (5 minutes)
Create simple solid color circles in MS Paint or any image editor:
- **CombatBoostActive.png** - Bright green circle (#00FF00)
- **CombatBoostInactive.png** - Grey circle (#808080)

### Option B: Professional Icons (recommended)
Use Figma, Photoshop, or AI image generator to create:
- **Active:** Glowing cyan/green pill or lightning bolt with sparkle effects
- **Inactive:** Grey dimmed version of the same icon

See `Assets/Resources/Sprites/BOOST_ICONS_README.md` for detailed design specifications.

### Save Location
```
Assets/Resources/Sprites/CombatBoostActive.png
Assets/Resources/Sprites/CombatBoostInactive.png
```

## 🎮 Step 2: Unity Setup

### 2.1 Import Sprite Settings

1. Open Unity
2. Navigate to `Assets/Resources/Sprites/`
3. Select `CombatBoostActive.png`
4. In Inspector:
   - Texture Type: `Sprite (2D and UI)`
   - Max Size: `512`
   - Click **Apply**
5. Repeat for `CombatBoostInactive.png`

### 2.2 Create UI GameObject

1. Open the **Inventory** scene
2. In Hierarchy, find the Canvas where abilities are displayed (probably near `StakingAbility`)
3. Right-click → **UI → Image**
4. Rename it to `CombatBoostIndicator`
5. Position it next to other ability icons (StakingAbility, FarmingAbility)
6. Adjust RectTransform:
   - Width: 80-100
   - Height: 80-100
   - Anchor to appropriate corner

### 2.3 Assign References

1. Select the `UIInventory` GameObject in Hierarchy
2. In Inspector, find the `UIInventory` component
3. Assign the following:
   - **Combat Boost Active Sprite** → Drag `CombatBoostActive` sprite
   - **Combat Boost Inactive Sprite** → Drag `CombatBoostInactive` sprite
   - **Combat Boost Indicator** → Drag the `CombatBoostIndicator` GameObject

### 2.4 Save Scene

Press `Ctrl+S` to save the scene.

## 🧪 Step 3: Test It

### Test in Unity Editor

1. **Play the Inventory scene**
2. Check the Console for logs:
   ```
   💊 Fetching combat boost for: 0xA5e82D9C3d80B4dDB93766874A3c13c19eb3Da54
   💊 Combat Boost Indicator initialized (inactive state)
   ```
3. If a boost is active on the test wallet, you'll see:
   ```
   ✅ Active Combat Boost Found!
   💊 Combat Boost Indicator: ACTIVE (boost enabled)
   ```
4. The indicator icon should change from grey to bright green/cyan

### Expected Behavior

**With Active Boost:**
- Indicator shows bright active sprite (green/cyan glow)
- Console logs: "💊 Combat Boost Indicator: ACTIVE"

**Without Active Boost:**
- Indicator shows grey inactive sprite
- Console logs: "💊 Combat Boost Indicator: INACTIVE"

## 📋 Visual Reference

```
┌─────────────────────────────┐
│  Inventory Scene            │
│                             │
│  ┌────┐  ┌────┐  ┌────┐   │
│  │ 🛡️ │  │ 🌾 │  │ 💊 │   │  ← Abilities row
│  └────┘  └────┘  └────┘   │
│  Shield  Farming  Boost    │
│                             │
│  [Hero Cards Below]        │
└─────────────────────────────┘
```

The Combat Boost Indicator should be positioned alongside the existing Shield and Farming ability icons.

## 🔧 Troubleshooting

### Indicator Not Showing
**Problem:** CombatBoostIndicator is null
**Solution:** Make sure you assigned the GameObject reference in UIInventory component

### Sprites Not Assigned Warning
**Problem:** Logs show "CombatBoostActiveSprite is not assigned!"
**Solution:**
1. Select UIInventory GameObject
2. Drag both sprites into the Inspector fields
3. Save scene

### Indicator Not Updating
**Problem:** Icon stays grey even with active boost
**Solution:**
1. Check Console for backend errors
2. Verify test wallet has active boost on DEV environment
3. Check that `UIInventory.instance` exists when boost is received

### Wrong Sprite Showing
**Problem:** Active/inactive sprites are swapped
**Solution:** Swap the sprite assignments in UIInventory Inspector

## 🎯 Next Steps

After setting up the UI indicator, you may want to add:

1. **Tooltip/Hover Info** - Show boost details on hover
2. **Countdown Timer** - Display remaining boost time
3. **Click Handler** - Open boost purchase page
4. **Animation** - Pulsing glow effect for active state

These are optional enhancements and not required for MVP.

## 📝 Summary

✅ Code implementation: **COMPLETE**
⏳ Icon creation: **YOUR TASK** (see Step 1)
⏳ Unity setup: **YOUR TASK** (see Step 2)
⏳ Testing: **YOUR TASK** (see Step 3)

Once you complete Steps 1-3, the combat boost indicator will be fully functional in your game!

---

**Need Help?** Check `Assets/Resources/Sprites/BOOST_ICONS_README.md` for detailed icon design specs.
