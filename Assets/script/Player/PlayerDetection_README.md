# PlayerDetection 系統使用說明

## 概述
`PlayerDetection` 系統是一個類似於 `EnemyDetection` 的玩家視野檢測系統，用於判斷哪些敵人在玩家的視野中，並與 `EnemyManager` 整合來控制敵人的渲染，實現性能優化。

## 主要功能

### 1. 視野檢測
- **距離檢測**：檢查敵人是否在玩家視野範圍內
- **角度檢測**：檢查敵人是否在玩家視野角度內
- **遮擋檢測**：使用射線檢測檢查是否有障礙物遮擋
- **攝影機剔除**：檢查敵人是否在攝影機視野外

### 2. 渲染控制
- **不 deactivate 敵人**：只控制渲染組件的啟用/停用
- **保持 AI 運行**：敵人 AI 邏輯繼續運行，只是不渲染
- **動態更新**：根據玩家移動和視野變化動態更新

### 3. 性能優化
- **分批處理**：每幀只檢查部分敵人，分散 CPU 負載
- **快取渲染器**：快取敵人的渲染器組件，避免重複查找
- **攝影機剔除**：自動剔除攝影機視野外的敵人

## 系統架構

### 核心組件
1. **PlayerDetection.cs** - 主要的檢測邏輯
2. **PlayerDetectionSetup.cs** - 配置輔助腳本
3. **EnemyManager.cs** - 整合敵人管理

### 工作流程
```
玩家移動/旋轉 → PlayerDetection 更新 → 檢查敵人可見性 → 控制敵人渲染 → 性能優化
```

## 使用方法

### 1. 基本設置

#### 在玩家 GameObject 上添加組件：
```csharp
// 添加 PlayerDetection 組件
PlayerDetection playerDetection = playerGameObject.AddComponent<PlayerDetection>();

// 添加 PlayerDetectionSetup 組件（可選，用於配置）
PlayerDetectionSetup setup = playerGameObject.AddComponent<PlayerDetectionSetup>();
```

#### 在 EnemyManager 中啟用整合：
```csharp
// 在 EnemyManager Inspector 中設定
Enable Player Detection: true
Auto Register With Player Detection: true
```

### 2. 參數配置

#### PlayerDetection 參數：
- **View Range**：視野範圍（建議：5-15）
- **View Angle**：視野角度（建議：60-120°）
- **Obstacle Layer Mask**：障礙物層遮罩
- **Update Interval**：更新間隔（建議：0.1s）
- **Enemies Per Frame Check**：每幀檢查的敵人數量（建議：5）

#### PlayerDetectionSetup 參數：
- **Walls Layer Name**：牆壁層名稱（預設：Walls）
- **Objects Layer Name**：物件層名稱（預設：Objects）
- **Show Debug Gizmos**：顯示除錯視野
- **Visible Enemy Color**：可見敵人顏色
- **Hidden Enemy Color**：隱藏敵人顏色

### 3. 程式化控制

```csharp
// 獲取 PlayerDetection 組件
PlayerDetection detection = GetComponent<PlayerDetection>();

// 設定偵測參數
detection.SetDetectionParameters(10f, 90f); // 10單位範圍，90度角度

// 強制更新所有敵人可見性
detection.ForceUpdateAllEnemies();

// 獲取可見敵人列表
List<Enemy> visibleEnemies = detection.GetVisibleEnemies();

// 檢查特定敵人是否可見
bool isVisible = detection.IsEnemyVisible(enemy);

// 從 EnemyManager 控制
EnemyManager enemyManager = FindFirstObjectByType<EnemyManager>();
enemyManager.SetPlayerDetection(true, true); // 啟用偵測，自動註冊
enemyManager.ForceUpdateEnemyVisibility(); // 強制更新
```

## 性能優化策略

### 1. 分批處理
- 每幀只檢查部分敵人，避免一次性處理所有敵人
- 可調整 `enemiesPerFrameCheck` 參數來平衡性能和響應速度

### 2. 攝影機剔除
- 自動剔除攝影機視野外的敵人
- 可調整 `cameraCullMargin` 參數來設定剔除邊距

### 3. 更新頻率控制
- 可調整 `updateInterval` 參數來控制更新頻率
- 建議值：0.1-0.2 秒

### 4. 渲染器快取
- 自動快取敵人的渲染器組件
- 避免每幀重複查找渲染器

## 除錯功能

### 1. Scene 視圖除錯
啟用 `Show Debug Gizmos` 後可以看到：
- **青色扇形**：玩家視野範圍
- **綠色圓圈**：可見敵人
- **紅色圓圈**：隱藏敵人
- **連線**：玩家到可見敵人的連線

### 2. Console 除錯信息
- 初始化狀態
- 敵人註冊/移除信息
- 可見性更新統計
- 性能警告

### 3. 統計信息
```csharp
// 獲取統計信息
int visibleCount = detection.VisibleEnemyCount;
int hiddenCount = detection.HiddenEnemyCount;

// 從 EnemyManager 獲取
int visibleFromManager = enemyManager.GetVisibleEnemyCount();
int hiddenFromManager = enemyManager.GetHiddenEnemyCount();
```

## 與 EnemyDetection 的差異

| 特性 | EnemyDetection | PlayerDetection |
|------|----------------|-----------------|
| 檢測目標 | 玩家 | 敵人 |
| 渲染控制 | 無 | 控制敵人渲染 |
| 性能優化 | 攝影機剔除 | 分批處理 + 攝影機剔除 |
| 整合對象 | DangerousManager | EnemyManager |
| 主要用途 | AI 行為 | 渲染優化 |

## 注意事項

### 1. 依賴關係
- 需要 `PlayerController` 組件來獲取玩家方向
- 需要 `EnemyManager` 來管理敵人
- 需要正確設定 LayerMask

### 2. 性能考慮
- 敵人數量過多時，建議降低 `enemiesPerFrameCheck`
- 可以增加 `updateInterval` 來降低更新頻率
- 攝影機剔除可以顯著提升性能

### 3. 渲染控制
- 只控制渲染器，不 deactivate 敵人
- 敵人 AI 邏輯繼續運行
- 可以手動控制特定敵人的渲染

## 故障排除

### 常見問題
1. **敵人不可見**：檢查 LayerMask 設定和障礙物層
2. **性能問題**：調整更新頻率和每幀檢查數量
3. **偵測不準確**：檢查視野範圍和角度設定
4. **整合失敗**：確認 EnemyManager 中的設定

### 調試步驟
1. 啟用 `Show Debug Gizmos` 查看視野範圍
2. 檢查 Console 中的初始化信息
3. 使用 `ForceUpdateEnemyVisibility()` 強制更新
4. 檢查 LayerMask 設定是否正確

## 擴展功能

可以根據需要擴展以下功能：
- 添加音效提示（敵人進入/離開視野）
- 實現動態視野範圍調整
- 添加敵人類型過濾
- 整合 UI 顯示可見敵人數量
- 添加視野障礙物檢測優化
