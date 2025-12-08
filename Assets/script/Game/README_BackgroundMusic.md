# Background Music Setup Guide

## Quick Setup Instructions

### For MainMenuScene:

1. **Create a GameObject for the music**
   - In the MainMenuScene, create a new empty GameObject
   - Name it "MainMenuMusic"

2. **Add the BackgroundMusicManager component**
   - Select the "MainMenuMusic" GameObject
   - Click "Add Component"
   - Search for and add "BackgroundMusicManager"

3. **Configure the music**
   - In the Inspector, find the "Music Clip" field
   - Drag and drop: `Assets/Soundtrack/Soundwave Sphere - Neon Collapse.mp3`
   - Set "Volume Multiplier" to `1.0` (full volume)

### For GameScene:

1. **Create a GameObject for the music**
   - In the GameScene, create a new empty GameObject
   - Name it "GameMusic"

2. **Add the BackgroundMusicManager component**
   - Select the "GameMusic" GameObject
   - Click "Add Component"
   - Search for and add "BackgroundMusicManager"

3. **Configure the music**
   - In the Inspector, find the "Music Clip" field
   - Drag and drop: `Assets/Soundtrack/wekont - headhunter.mp3.mp3`
   - Set "Volume Multiplier" to `0.5` or `0.6` (quieter for ambient BGM)

## Features

✅ **Automatic looping** - Music plays on loop automatically
✅ **Volume control** - Respects GameSettings master volume and music volume
✅ **Scene-specific** - Each scene has its own music GameObject
✅ **Adjustable volume** - Use Volume Multiplier to make music quieter or louder

## Volume Multiplier Guide

- `1.0` = Full volume (100%)
- `0.8` = 80% volume
- `0.6` = 60% volume (good for ambient background)
- `0.5` = 50% volume (quieter ambient)
- `0.4` = 40% volume (very quiet ambient)

The final volume will be: `Master Volume × Music Volume × Volume Multiplier`

## Notes

- The music will automatically start playing when the scene loads
- Players can adjust music volume in the Settings menu
- The script continuously updates volume to respond to settings changes
- Music will NOT persist between scenes (each scene has its own music)
