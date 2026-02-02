# Combat Boost Icon Instructions

## Required Sprites

You need to create two sprite icons for the combat boost indicator:

### 1. CombatBoostActive.png
**Purpose:** Shows when player has an active combat boost
**Recommended Design:**
- Size: 512x512 pixels (PNG with transparency)
- Colors: Bright cyan/green with glow effect
- Style: Glowing pill/capsule or lightning bolt symbol
- Effect: Should look energized and "powered up"

**Color Suggestions:**
- Primary: #00FFC8 (Cyan)
- Secondary: #32FF96 (Light Green)
- Accent: #FFFFFF (White)

### 2. CombatBoostInactive.png
**Purpose:** Shows when player has NO active combat boost
**Recommended Design:**
- Size: 512x512 pixels (PNG with transparency)
- Colors: Grey/dimmed version of active icon
- Style: Same shape as active but desaturated
- Effect: Should look dormant/inactive

**Color Suggestions:**
- Primary: #505050 (Dark Grey)
- Secondary: #646464 (Medium Grey)
- Accent: #8C8C8C (Light Grey)

## Design Ideas

**Option 1: Pill/Capsule Icon**
```
🔵 Active: Bright glowing capsule with sparkles
⚫ Inactive: Grey dimmed capsule
```

**Option 2: Lightning Bolt**
```
⚡ Active: Bright cyan lightning bolt with glow
⚪ Inactive: Grey lightning bolt
```

**Option 3: Boost Meter**
```
📊 Active: Full glowing bar with particles
📉 Inactive: Empty grey bar
```

## How to Create

### Using Photoshop/GIMP:
1. Create 512x512 transparent canvas
2. Draw your boost icon using suggested colors
3. Add glow/shadow effects for active version
4. Desaturate and dim for inactive version
5. Export as PNG

### Using Online Tools:
- Figma: https://www.figma.com/
- Canva: https://www.canva.com/
- Photopea: https://www.photopea.com/ (free Photoshop alternative)

### Using AI Image Generators:
Prompt example:
```
"Game UI icon, glowing cyan and green pill capsule,
bright neon style, transparent background, 512x512,
mobile game asset"
```

## Quick Placeholder Solution

If you need to test immediately, you can use these placeholder designs:

1. **Active:** Bright green circle (#00FF00)
2. **Inactive:** Grey circle (#808080)

Create these in MS Paint or any image editor, then replace with better designs later.

## Unity Import Settings

After creating the icons:

1. Place both PNG files in `Assets/Resources/Sprites/`
2. Select each sprite in Unity
3. Set Texture Type: `Sprite (2D and UI)`
4. Set Max Size: `512`
5. Click Apply

## Assigning in Unity

1. Open the Inventory scene
2. Find the `UIInventory` GameObject
3. In the Inspector, locate the `UIInventory` component
4. Assign:
   - `Combat Boost Active Sprite` → CombatBoostActive
   - `Combat Boost Inactive Sprite` → CombatBoostInactive
5. Create a new UI Image GameObject for `CombatBoostIndicator`
6. Assign it to the `Combat Boost Indicator` field

Done!
