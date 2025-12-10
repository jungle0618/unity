# Neon Text Effect Setup Guide

## How to Add Neon Effect to "Deadly Stealth Assassin"

### Step 1: Find the Text in MainMenuScene

1. Open the **MainMenuScene**
2. Find the TextMeshPro text that displays "Deadly Stealth Assassin" in the Hierarchy
3. Select it

### Step 2: Add the NeonTextEffect Component

1. With the text selected, click **Add Component** in the Inspector
2. Search for **"NeonTextEffect"**
3. Add the component

### Step 3: Configure the Effect

The component comes pre-configured with good defaults, but you can customize:

#### **Neon Colors:**
- **Neon Color**: The main text color (default: Cyan `#00FFFF`)
  - Try: Electric Blue `#00D4FF`, Hot Pink `#FF006E`, Lime Green `#39FF14`
- **Shadow Color**: The jumping shadow color (default: Magenta `#FF00FF`)
  - Try: Purple `#9D00FF`, Orange `#FF6600`, Red `#FF0033`

#### **Glow Animation:**
- **Animate Glow**: ✅ Check to enable pulsing glow
- **Glow Speed**: `2.0` (how fast it pulses)
- **Min Glow Intensity**: `0.5` (dimmest point)
- **Max Glow Intensity**: `1.5` (brightest point)

#### **Shadow Jump Animation:**
- **Animate Shadow**: ✅ Check to enable jumping shadows
- **Jump Speed**: `3.0` (how fast shadows move)
- **Jump Height**: `5.0` (vertical movement range)
- **Jump Distance**: `3.0` (horizontal movement range)

#### **Shadow Settings:**
- **Number Of Shadows**: `3` (more = more dramatic)
- **Shadow Spacing**: `2.0` (spacing between shadow layers)

### Step 4: Test the Effect

1. Press **Play** in the Unity Editor
2. You should see:
   - ✨ The text glowing with a pulsing neon effect
   - 👻 Multiple colored shadows jumping around the text
   - 🌈 Smooth, eye-catching animation

## Recommended Presets

### **Classic Cyberpunk (Cyan/Magenta)**
```
Neon Color: #00FFFF (Cyan)
Shadow Color: #FF00FF (Magenta)
Number Of Shadows: 3
Jump Height: 5
Jump Distance: 3
```

### **Electric Assassin (Blue/Purple)**
```
Neon Color: #0099FF (Electric Blue)
Shadow Color: #9D00FF (Purple)
Number Of Shadows: 4
Jump Height: 7
Jump Distance: 4
```

### **Deadly Pink (Hot Pink/Red)**
```
Neon Color: #FF006E (Hot Pink)
Shadow Color: #FF0033 (Red)
Number Of Shadows: 3
Jump Height: 6
Jump Distance: 5
```

### **Stealth Green (Lime/Yellow)**
```
Neon Color: #39FF14 (Lime Green)
Shadow Color: #FFD700 (Gold)
Number Of Shadows: 2
Jump Height: 4
Jump Distance: 3
```

### **Minimal Ghost (White/Blue)**
```
Neon Color: #FFFFFF (White)
Shadow Color: #0080FF (Blue)
Number Of Shadows: 2
Jump Height: 3
Jump Distance: 2
```

## Advanced Customization

### **More Dramatic Effect:**
- Increase `Number Of Shadows` to 5-6
- Increase `Jump Height` to 10-15
- Increase `Jump Distance` to 5-8
- Increase `Shadow Spacing` to 3-4

### **Subtle Effect:**
- Decrease `Number Of Shadows` to 1-2
- Decrease `Jump Height` to 2-3
- Decrease `Jump Distance` to 1-2
- Set `Glow Speed` to 1.0

### **Fast & Energetic:**
- Increase `Jump Speed` to 5-6
- Increase `Glow Speed` to 4-5
- Use bright, contrasting colors

### **Slow & Mysterious:**
- Decrease `Jump Speed` to 1-2
- Decrease `Glow Speed` to 1.0
- Use darker shadow colors

## Features

✅ **Auto-sync text**: Shadows automatically update if you change the text
✅ **Multiple shadows**: Creates layered shadow effect
✅ **Pulsing glow**: Main text pulses with neon intensity
✅ **Jumping animation**: Shadows move in smooth, wave-like patterns
✅ **Color customization**: Full control over neon and shadow colors
✅ **Performance optimized**: Uses material instances to avoid affecting other UI

## Tips

1. **For best results**: Use a bold, thick font (like "Bebas Neue" or "Impact")
2. **Dark backgrounds**: Neon effects look best against dark backgrounds
3. **Font size**: Larger text (60-100pt) shows the effect better
4. **Contrast**: Use contrasting colors (Cyan/Magenta, Blue/Orange, etc.)
5. **Layer order**: The script automatically manages shadow layering

## Troubleshooting

**Problem**: Shadows not visible
- Solution: Increase `Shadow Color` alpha value
- Solution: Make sure text background is dark enough

**Problem**: Effect too subtle
- Solution: Increase `Number Of Shadows`
- Solution: Increase `Jump Height` and `Jump Distance`
- Solution: Use brighter, more contrasting colors

**Problem**: Effect too chaotic
- Solution: Decrease `Number Of Shadows` to 1-2
- Solution: Decrease `Jump Speed` and `Glow Speed`
- Solution: Reduce `Jump Height` and `Jump Distance`

**Problem**: Performance issues
- Solution: Reduce `Number Of Shadows`
- Solution: Disable `Animate Glow` if not needed
- Solution: Use simpler fonts

Enjoy your neon title! 🎮✨
