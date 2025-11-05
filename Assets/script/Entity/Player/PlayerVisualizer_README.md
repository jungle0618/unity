# PlayerVisualizer 使用說明

## 概述
`PlayerVisualizer` 是一個類似於 `EnemyVisualizer` 的玩家視野視覺化組件，用於在 Unity 編輯器中顯示玩家的視野範圍、武器方向、移動方向等視覺化信息。

## 功能特點

### 1. 視野範圍顯示
- 顯示玩家的視野扇形範圍
- 可調整視野距離和角度
- 支援實心扇形和邊界線顯示

### 2. 方向指示
- **武器方向**：顯示玩家當前瞄準方向（紅色箭頭）
- **移動方向**：顯示玩家移動方向（綠色箭頭）

### 3. 狀態顏色系統
- **血量顏色**：根據血量百分比顯示不同顏色
  - 健康：白色
  - 受傷：黃色（60%以下）
  - 危險：紅色（30%以下）
- **狀態顏色**：根據玩家狀態顯示顏色
  - 正常：青色
  - 警戒：黃色
  - 戰鬥：紅色
  - 死亡：灰色

## 使用方法

### 1. 添加組件
將 `PlayerVisualizer` 組件添加到玩家 GameObject 上：
```csharp
// 在玩家 GameObject 上添加 PlayerVisualizer 組件
PlayerVisualizer visualizer = playerGameObject.AddComponent<PlayerVisualizer>();
```

### 2. 配置參數
在 Inspector 中調整以下參數：

#### 視覺化設定
- `Show Debug Gizmos`：是否顯示調試 Gizmos
- `Normal Color`：正常狀態顏色
- `Alert Color`：警戒狀態顏色
- `Combat Color`：戰鬥狀態顏色
- `Dead Color`：死亡狀態顏色

#### 血量顏色設定
- `Use Health Color`：是否使用血量顏色
- `Healthy Color`：健康血量顏色
- `Damaged Color`：受傷血量顏色
- `Critical Color`：危險血量顏色
- `Critical Health Threshold`：危險血量閾值（0.3 = 30%）
- `Damaged Health Threshold`：受傷血量閾值（0.6 = 60%）

#### 視野範圍設定
- `Show Vision Range`：是否顯示視野範圍
- `View Range`：視野距離
- `View Angle`：視野角度（度）
- `Show Weapon Direction`：是否顯示武器方向
- `Show Movement Direction`：是否顯示移動方向

### 3. 程式化控制
```csharp
// 獲取 PlayerVisualizer 組件
PlayerVisualizer visualizer = GetComponent<PlayerVisualizer>();

// 設定視野參數
visualizer.SetVisionParameters(8f, 90f); // 8單位距離，90度角度

// 設定狀態
visualizer.SetCombatState(true);  // 進入戰鬥狀態
visualizer.SetAlertState(false);  // 取消警戒狀態

// 設定顏色
visualizer.SetStateColors(Color.blue, Color.yellow, Color.red, Color.gray);
visualizer.SetHealthColors(Color.white, Color.yellow, Color.red);

// 開關視覺化
visualizer.SetShowDebugGizmos(true);
visualizer.ForceShowVisualization(); // 強制顯示
```

## 與 PlayerController 的整合

`PlayerVisualizer` 會自動與 `PlayerController` 整合：
- 自動獲取玩家的武器瞄準方向
- 監聽血量變化事件
- 根據玩家狀態更新視覺化

## 調試功能

### 在 Scene 視圖中
- 選中玩家 GameObject 時會顯示詳細的視野範圍
- 顯示玩家當前狀態文字
- 顯示武器和移動方向箭頭

### 在 Game 視圖中
- 根據設定顯示或隱藏視覺化元素
- 支援運行時動態調整參數

## 注意事項

1. **性能考慮**：視覺化功能主要在編輯器中使用，發布版本中會自動禁用
2. **依賴關係**：需要 `PlayerController` 組件才能正常工作
3. **材質設定**：可以配合 `PlayerFOVMaterial.mat` 使用，實現更豐富的視覺效果

## 擴展功能

可以根據需要擴展以下功能：
- 添加敵人檢測範圍顯示
- 實現動態視野範圍調整
- 添加音效提示
- 整合 UI 顯示

## 故障排除

### 常見問題
1. **視覺化不顯示**：檢查 `Show Debug Gizmos` 是否開啟
2. **方向不正確**：確認 `PlayerController` 組件存在且正常工作
3. **顏色不更新**：檢查血量變化事件是否正確訂閱

### 調試信息
組件會在 Console 中輸出調試信息，包括：
- 初始化狀態
- 視覺化開關狀態
- 扇形繪製參數
