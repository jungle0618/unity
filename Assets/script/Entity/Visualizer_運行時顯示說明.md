# Visualizer 運行時顯示功能說明

## 概述

現在 `BaseVisualizer`、`PlayerVisualizer` 和 `EnemyVisualizer` 都支援在正式執行時（Game 視圖或 Build）顯示視野範圍！

## 功能特點

### ✅ 運行時視野顯示
- 使用 **LineRenderer** 繪製視野輪廓線
- 使用 **MeshRenderer** 繪製實心視野區域（可選）
- 支援射線檢測遮擋物（Objects 和 Walls 圖層）
- 實時更新視野方向和範圍

### ✅ 保留原有功能
- Scene 視圖中的 Gizmos 顯示（用於編輯器調試）
- 血量顏色系統（根據血量變化 Sprite 顏色）
- 巡邏路徑顯示（僅 EnemyVisualizer）

## 使用方法

### 1. 啟用運行時視野顯示

在 Unity Inspector 中找到 PlayerVisualizer 或 EnemyVisualizer 組件：

**運行時視野顯示設定**
- ✅ **Show Runtime Vision**: 勾選以啟用運行時視野顯示
- ✅ **Use Runtime Mesh**: 勾選以顯示實心視野區域（不勾選則只顯示輪廓線）
- **Vision Material**: （可選）設置自訂材質
- **Runtime Vision Color**: 設置視野顏色（支援透明度）
- **Runtime Line Width**: 設置輪廓線寬度

### 2. 設置材質（可選但建議）

為了獲得更好的視覺效果，建議創建一個支援透明的材質：

1. 在 Unity 中創建新材質：`Assets > Create > Material`
2. 命名為 `VisionMaterial`
3. 設置 Shader 為 `Sprites/Default` 或 `Unlit/Transparent`
4. 將材質拖拽到 Visualizer 的 `Vision Material` 欄位

### 3. 調整顏色

**建議的顏色設置：**

- **玩家視野（PlayerVisualizer）**
  - Runtime Vision Color: `Cyan (0, 255, 255, 77)` - 青色半透明
  
- **敵人視野（EnemyVisualizer）**
  - Runtime Vision Color: `Yellow (255, 255, 0, 77)` - 黃色半透明

### 4. 運行時控制

可以通過腳本動態控制視野顯示：

```csharp
// 獲取 Visualizer 組件
PlayerVisualizer visualizer = GetComponent<PlayerVisualizer>();

// 啟用/停用運行時視野顯示
visualizer.SetShowRuntimeVision(true);

// 檢查當前狀態
bool isShowing = visualizer.GetShowRuntimeVision();
```

## Inspector 設定選項說明

### 視覺化設定
- **Show Debug Gizmos**: 是否在 Scene 視圖顯示 Gizmos（編輯器專用）

### 運行時視野顯示設定
- **Show Runtime Vision**: 是否在 Game 視圖/Build 中顯示視野範圍
- **Use Runtime Mesh**: 是否使用 Mesh 繪製實心區域（關閉則只顯示輪廓線）
- **Vision Material**: 視野範圍的材質（留空則使用默認材質）
- **Runtime Vision Color**: 運行時視野顏色（RGBA，支援透明度）
- **Runtime Line Width**: 輪廓線寬度（單位：Unity units）

### 視野範圍設定
- **Obstacle Layers**: 障礙物圖層遮罩（用於射線檢測）

### 血量顏色設定
- **Use Health Color**: 是否啟用血量顏色系統
- **Healthy Color**: 健康時的顏色
- **Damaged Color**: 受傷時的顏色
- **Critical Color**: 危險時的顏色
- **Critical Health Threshold**: 危險血量閾值（0-1）
- **Damaged Health Threshold**: 受傷血量閾值（0-1）

## 性能考量

### 優化建議

1. **射線數量**: 預設每 2 度一條射線，視野角度越大射線越多
2. **更新頻率**: 視野每幀更新，如果性能有問題可以考慮降低更新頻率
3. **Mesh vs Line**: 
   - 只使用 LineRenderer（關閉 Use Runtime Mesh）性能最佳
   - 使用 Mesh 可以顯示實心區域，但會增加一些性能開銷

### 優化示例

如果需要降低更新頻率（修改 PlayerVisualizer.cs 或 EnemyVisualizer.cs）：

```csharp
private float visionUpdateInterval = 0.1f; // 每 0.1 秒更新一次
private float lastVisionUpdateTime = 0f;

private void UpdateRuntimeVisionDisplay()
{
    if (!showRuntimeVision || player == null) return;
    
    // 限制更新頻率
    if (Time.time - lastVisionUpdateTime < visionUpdateInterval) return;
    lastVisionUpdateTime = Time.time;
    
    // ... 原有的更新代碼 ...
}
```

## 常見問題

### Q: 為什麼運行時看不到視野範圍？
A: 檢查以下項目：
1. 確認 `Show Runtime Vision` 已勾選
2. 確認遊戲正在運行（Play Mode）
3. 檢查 Camera 是否能看到視野範圍的位置
4. 檢查材質是否設置正確

### Q: 視野顏色太暗或太亮？
A: 調整 `Runtime Vision Color` 的 Alpha 值（透明度）：
- Alpha 值 0 = 完全透明
- Alpha 值 255 = 完全不透明
- 建議值：50-100（約 20%-40%）

### Q: 視野輪廓線太粗或太細？
A: 調整 `Runtime Line Width`：
- 建議值：0.02 - 0.1
- 預設值：0.05

### Q: 如何只顯示輪廓線，不顯示實心區域？
A: 取消勾選 `Use Runtime Mesh`

### Q: 視野範圍沒有考慮遮擋物？
A: 確認 `Obstacle Layers` 設置正確，應包含 `Objects` 和 `Walls` 圖層

## 技術細節

### 視野繪製原理

1. **射線檢測**: 從中心點發射多條射線，檢測障礙物
2. **終點計算**: 每條射線的終點為障礙物碰撞點或最大範圍
3. **LineRenderer**: 連接所有終點形成輪廓
4. **MeshRenderer**: 創建三角形 Mesh 填充視野區域

### 自動創建的子物件

啟用運行時視野後，會自動創建以下子物件：

- **VisionLine**: 包含 LineRenderer 組件（輪廓線）
- **VisionMesh**: 包含 MeshFilter 和 MeshRenderer 組件（實心區域）

這些物件會在遊戲結束時自動清理。

## 版本資訊

- **版本**: 1.1
- **日期**: 2025-11-02
- **支援的 Unity 版本**: Unity 2019.4 或更高版本

### 更新日誌

#### v1.1 (2025-11-02)
- ✅ 修復：Runtime Mesh 塗色範圍穿透牆壁的問題
  - 修正 Mesh 頂點座標轉換邏輯（現在使用正確的座標系）
  - 調整 VisionMesh GameObject 的位置，避免 Z 軸偏移問題
  - 現在 Runtime Mesh 和 Runtime Line 都能正確遵守射線檢測結果

#### v1.0 (2025-11-02)
- 初始版本：添加運行時視野顯示功能

## 相關腳本

- `BaseVisualizer.cs` - 基礎視覺化類別
- `PlayerVisualizer.cs` - 玩家視覺化組件
- `EnemyVisualizer.cs` - 敵人視覺化組件

