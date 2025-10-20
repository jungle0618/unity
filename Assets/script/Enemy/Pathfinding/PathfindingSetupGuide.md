# 敵人路徑規劃系統設定指南

## 🎯 系統概述

新的路徑規劃系統讓敵人能夠智能地避開牆壁和物件，找到到達目標的路徑。系統使用貪心算法進行路徑規劃，並整合了Unity的Tilemap系統。貪心算法計算速度快，但可能不是最短路徑。

## 🏗️ 系統組件

### 核心組件
1. **PathfindingNode** - 路徑規劃節點
2. **PathfindingGrid** - 網格地圖管理
3. **GreedyPathfinding** - 貪心路徑規劃算法
4. **PathfindingVisualizer** - 路徑可視化
5. **EnemyMovement** - 更新的敵人移動控制器

## 🛠️ 設定步驟

### 1. 場景設定

#### 創建路徑規劃管理器
```
PathfindingManager (GameObject)
├── PathfindingGrid (Script)
│   ├── Cell Size: 1
│   ├── Grid Width: 50
│   ├── Grid Height: 50
│   ├── Wall Tilemap: 牆壁Tilemap
│   └── Object Tilemap: 物件Tilemap
├── GreedyPathfinding (Script)
│   ├── Pathfinding Grid: PathfindingGrid
│   ├── Show Debug Path: true
│   ├── Path Color: Green
│   └── Explored Color: Yellow
└── PathfindingVisualizer (Script)
    ├── Show Path: true
    ├── Show Grid: false
    ├── Show Explored Nodes: false
    └── Path Color: Green
```

### 2. Tilemap設定

#### 牆壁Tilemap
```
Wall Tilemap
├── Tilemap Renderer
│   └── Sorting Layer: "Walls"
├── Tilemap Collider 2D
│   ├── Layer: "Walls"
│   └── Is Trigger: false
└── Composite Collider 2D (可選)
```

#### 物件Tilemap
```
Object Tilemap
├── Tilemap Renderer
│   └── Sorting Layer: "Objects"
├── Tilemap Collider 2D
│   ├── Layer: "Objects"
│   └── Is Trigger: false
└── Composite Collider 2D (可選)
```

### 3. 敵人設定

#### 更新敵人GameObject
```
Enemy (GameObject)
├── Enemy (Script)
├── EnemyDetection (Script)
├── EnemyMovement (Script)
│   ├── Use Pathfinding: true
│   ├── Pathfinding: GreedyPathfinding
│   ├── Path Update Interval: 0.5
│   └── Path Reach Threshold: 0.5
├── Rigidbody2D
└── Collider2D
```

## ⚙️ 參數說明

### PathfindingGrid 參數
- **Cell Size**: 網格單元大小（建議1.0）
- **Grid Width/Height**: 網格寬度和高度
- **Grid Offset**: 網格偏移量
- **Wall Tilemap**: 牆壁Tilemap引用
- **Object Tilemap**: 物件Tilemap引用
- **Obstacle Layer Mask**: 障礙物層遮罩

### GreedyPathfinding 參數
- **Pathfinding Grid**: 路徑規劃網格引用
- **Show Debug Path**: 是否顯示除錯路徑
- **Path Color**: 路徑顏色
- **Explored Color**: 探索節點顏色

### EnemyMovement 參數
- **Use Pathfinding**: 是否使用路徑規劃
- **Pathfinding**: 貪心路徑規劃組件引用
- **Path Update Interval**: 路徑更新間隔（秒）
- **Path Reach Threshold**: 到達路徑點的距離閾值

## 🎮 使用方法

### 基本移動
```csharp
// 使用路徑規劃移動到目標
enemyMovement.MoveTowardsWithPathfinding(targetPosition, speedMultiplier);

// 智能移動（自動選擇是否使用路徑規劃）
enemyMovement.MoveTowardsSmart(targetPosition, speedMultiplier);

// 直接移動（不使用路徑規劃）
enemyMovement.MoveTowards(targetPosition, speedMultiplier);
```

### 路徑管理
```csharp
// 清除當前路徑
enemyMovement.ClearPath();

// 檢查是否有有效路徑
bool hasPath = enemyMovement.HasValidPath();

// 獲取當前路徑
List<PathfindingNode> path = enemyMovement.GetCurrentPath();

// 獲取下一個路徑點
Vector2 nextPoint = enemyMovement.GetNextPathPoint();
```

## 🔧 除錯功能

### 可視化設定
1. 在Scene視圖中可以看到：
   - 綠色線條：當前路徑
   - 黃色方塊：探索過的節點
   - 白色/紅色方塊：可行走/不可行走的網格

### 除錯信息
- Console中會顯示路徑規劃的詳細信息
- 可以通過PathfindingVisualizer組件控制可視化選項

## ⚠️ 注意事項

### 貪心算法特性
1. **計算速度快**: 比A*算法更快，適合即時計算
2. **路徑可能不是最短**: 貪心算法選擇當前最優解，可能導致較長的路徑
3. **可能陷入局部最優**: 在某些複雜地形中可能找不到路徑
4. **回溯機制**: 當遇到死路時會自動回溯

### 性能優化
1. **網格大小**: 不要設定過大的網格，會影響性能
2. **更新頻率**: 適當調整路徑更新間隔
3. **路徑簡化**: 系統會自動簡化路徑，移除不必要的節點
4. **最大迭代次數**: 設定為1000，防止無限循環

### 常見問題
1. **找不到路徑**: 檢查目標位置是否可行走，或地形是否過於複雜
2. **路徑不更新**: 檢查PathfindingGrid設定
3. **性能問題**: 減少網格大小或增加更新間隔
4. **路徑過長**: 這是貪心算法的正常行為，可考慮使用路徑簡化

## 🎯 最佳實踐

### 網格設定
- 網格大小應該與遊戲世界比例匹配
- 牆壁和物件應該完全覆蓋網格單元
- 使用適當的Layer遮罩

### 路徑規劃
- 在敵人狀態改變時清除舊路徑
- 定期更新路徑以應對動態障礙物
- 使用路徑簡化減少計算量

### 性能優化
- 只在需要時啟用路徑規劃
- 使用適當的更新間隔
- 避免過於頻繁的路徑重新計算
