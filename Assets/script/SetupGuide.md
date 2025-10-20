# 視野系統設定指南

## 🏗️ 場景設定

### 1. Grid 設定
```
Grid (GameObject)
├── Tilemap (牆壁)
│   ├── Tilemap Renderer
│   │   └── Sorting Layer: "Walls"
│   └── Tilemap Collider 2D
│       └── Layer: "Walls"
├── Tilemap (地板)
│   ├── Tilemap Renderer
│   │   └── Sorting Layer: "Floor"
│   └── Tilemap Collider 2D (可選)
│       └── Layer: "Floor"
└── Tilemap (物件/桌子)
    ├── Tilemap Renderer
    │   └── Sorting Layer: "Objects"
    └── Tilemap Collider 2D
        └── Layer: "Objects"
```

### 2. Layer 設定
在 Unity 的 Layers 設定中創建以下層級：
- **Walls** (牆壁層)
- **Objects** (物件層，如桌子)
- **Player** (玩家層)
- **Enemy** (敵人層)

## 🎮 Player 設定

### Player GameObject 設定：
```
Player (GameObject)
├── Layer: "Player"
├── Tag: "Player"
├── PlayerController (Script)
├── Rigidbody2D
│   ├── Body Type: Dynamic
│   └── Collision Detection: Continuous
├── Collider2D (CircleCollider2D 或 BoxCollider2D)
└── WeaponHolder (Script)
    └── Weapon (Knife 或 Gun)
```

### PlayerController 參數：
- `isSquatting`: 蹲下狀態
- `squatSpeedMultiplier`: 0.5 (蹲下時速度減半)

## 👾 Enemy 設定

### Enemy GameObject 設定：
```
Enemy (GameObject)
├── Layer: "Enemy"
├── Tag: "Enemy"
├── Enemy (Script)
├── EnemyDetection (Script)
│   ├── obstacleLayerMask: "Walls" 層
│   ├── objectLayerMask: "Objects" 層
│   ├── useRaycastDetection: true
│   └── rayCount: 8
├── EnemyMovement (Script)
├── EnemyVisualizer (Script)
├── EnemyAttackController (Script)
├── WeaponHolder (Script)
├── Rigidbody2D
└── Collider2D
```

### EnemyDetection 參數：
- `viewRange`: 8f (視野範圍)
- `viewAngle`: 90f (視野角度)
- `chaseRange`: 15f (追擊範圍)
- `obstacleLayerMask`: 選擇 "Walls" 層
- `objectLayerMask`: 選擇 "Objects" 層
- `useRaycastDetection`: true
- `rayCount`: 8

## 🗺️ Tilemap 設定

### 牆壁 Tilemap：
```
Tilemap (Walls)
├── Tilemap Renderer
│   ├── Material: 2D Sprite Material
│   └── Sorting Layer: "Walls"
├── Tilemap Collider 2D
│   ├── Layer: "Walls"
│   └── Is Trigger: false
└── Composite Collider 2D (可選，用於優化)
```

### 物件 Tilemap (桌子等)：
```
Tilemap (Objects)
├── Tilemap Renderer
│   ├── Material: 2D Sprite Material
│   └── Sorting Layer: "Objects"
├── Tilemap Collider 2D
│   ├── Layer: "Objects"
│   └── Is Trigger: false
└── Composite Collider 2D (可選)
```

## 🎯 重要設定檢查清單

### ✅ Physics2D 設定：
1. 確保 Physics2D 設定正確
2. 檢查 Collision Matrix 設定
3. 確保射線檢測能正確工作

### ✅ Layer 設定：
1. 創建必要的 Layer
2. 設定正確的 LayerMask
3. 確保 Collider 在正確的 Layer 上

### ✅ Tag 設定：
1. Player 物件設為 "Player" tag
2. Enemy 物件設為 "Enemy" tag
3. 牆壁物件設為 "Wall" tag (可選)

### ✅ Sorting Layer 設定：
1. 創建 "Walls", "Floor", "Objects" Sorting Layer
2. 設定正確的順序：Floor < Objects < Walls

## 🔧 除錯設定

### EnemyVisualizer 設定：
- `showDebugGizmos`: true (在 Scene 視圖中顯示視野)
- 設定適當的顏色來區分不同狀態

### 除錯輸出：
- 確保 Console 中能看到視野檢測的除錯訊息
- 檢查射線檢測是否正確工作

## 🎮 測試步驟

1. **基本視野測試**：
   - 玩家在敵人視野範圍內，敵人應該能看到玩家
   - 玩家在視野範圍外，敵人應該看不到玩家

2. **牆壁遮擋測試**：
   - 玩家在牆壁後，敵人應該看不到玩家
   - 玩家在牆壁前，敵人應該能看到玩家

3. **蹲下功能測試**：
   - 玩家蹲下躲在桌子後，敵人應該看不到玩家
   - 玩家站立在桌子後，敵人應該能看到玩家

4. **視覺化測試**：
   - 在 Scene 視圖中檢查敵人的視野扇形
   - 確認視野扇形被牆壁正確遮擋
