# GameOverUI 結算頁面設定指南

## 📋 概述

本指南將幫助您在 Unity 中設定遊戲結束結算頁面。當玩家死亡或遊戲失敗時，會顯示結算頁面，展示遊戲統計數據（擊殺數、遊戲時間、最高分等），並提供「重新開始」和「返回主選單」按鈕。

---

## 🎯 整合架構

```
GameUIManager (總協調器)
├── HealthUIManager
├── DangerUIManager
├── HotbarUIManager
├── TilemapMapUIManager
├── PauseUIManager
└── GameOverUIManager ⭐ 新增
    └── GameOverUI (結算頁面 UI)
```

---

## 🔧 Unity 設定步驟

### 步驟 1：創建結算頁面 UI 結構

在 Canvas 下創建以下結構：

```
Canvas
└── GameOverPanel (GameObject)
    ├── GameOverUIManager (Component) ← 新增
    ├── GameOverUI (Component) ← 新增
    └── GameOverContentPanel (GameObject) ← UI 面板
        ├── TitleText (TextMeshProUGUI) - 可選，顯示 "遊戲結束" 等標題
        ├── StatisticsPanel (GameObject) - 統計數據容器
        │   ├── EnemiesKilledText (TextMeshProUGUI) - 擊殺數
        │   └── GameTimeText (TextMeshProUGUI) - 遊戲時間
        └── ButtonsPanel (GameObject) - 按鈕容器
            ├── RestartButton (Button) - 重新開始
            └── MainMenuButton (Button) - 返回主選單
```

**詳細說明**：
1. 在 Canvas 下右鍵 → `Create Empty`，命名為 `GameOverPanel`
2. 在 `GameOverPanel` 下創建 `GameOverContentPanel`（這是實際的 UI 面板）
3. 在 `GameOverContentPanel` 下創建所需的 UI 元素

---

### 步驟 2：設定 UI 元素

#### 2.1 創建文字元素（TextMeshProUGUI）

**擊殺數文字**：
1. 在 `StatisticsPanel` 下右鍵 → `UI` → `Text - TextMeshPro`
2. 命名為 `EnemiesKilledText`
3. 設定文字內容（例如："擊殺數: 0"）
4. 調整字體大小、顏色等樣式

**遊戲時間文字**：
1. 在 `StatisticsPanel` 下右鍵 → `UI` → `Text - TextMeshPro`
2. 命名為 `GameTimeText`
3. 設定文字內容（例如："遊戲時間: 0.0 秒"）

#### 2.2 創建按鈕元素（Button）

**重新開始按鈕**：
1. 在 `ButtonsPanel` 下右鍵 → `UI` → `Button - TextMeshPro`
2. 命名為 `RestartButton`
3. 設定按鈕文字為 "重新開始"

**返回主選單按鈕**：
1. 在 `ButtonsPanel` 下右鍵 → `UI` → `Button - TextMeshPro`
2. 命名為 `MainMenuButton`
3. 設定按鈕文字為 "返回主選單"

---

### 步驟 3：添加組件並設定

#### 3.1 添加 GameOverUIManager 組件

1. 選中 `GameOverPanel` GameObject
2. 在 Inspector 中點擊 `Add Component`
3. 搜尋並添加 `GameOverUIManager` 組件
4. 設定以下欄位：
   ```
   Game Over UI Reference: 留空（會自動尋找）
   Auto Find Game Over UI: ✅ 勾選
   Auto Subscribe To Game Manager: ✅ 勾選（推薦）
   ```

#### 3.2 添加 GameOverUI 組件

1. 選中 `GameOverPanel` GameObject（與 GameOverUIManager 同一個）
2. 在 Inspector 中點擊 `Add Component`
3. 搜尋並添加 `GameOverUI` 組件
4. 設定以下欄位：
   ```
    Game Over Panel: 拖入 GameOverContentPanel GameObject
    Enemies Killed Text: 拖入 EnemiesKilledText 組件
    Game Time Text: 拖入 GameTimeText 組件
    Restart Button: 拖入 RestartButton 組件
    Main Menu Button: 拖入 MainMenuButton 組件
    ```

**可選設定**（文字格式）：
- `Enemies Killed Format`: 預設為 "擊殺數: {0}"
- `Game Time Format`: 預設為 "遊戲時間: {0:F1} 秒"

---

### 步驟 4：連接到 GameUIManager

1. 在 Hierarchy 中找到 Canvas（或包含 `GameUIManager` 的 GameObject）
2. 選中該 GameObject
3. 在 Inspector 中找到 `GameUIManager` 組件
4. 在 `Game Process UI Managers` 區塊中：
   - 將 `GameOverPanel` 上的 `GameOverUIManager` 組件拖入 `Game Over UI Manager` 欄位

---

### 步驟 5：設定初始狀態

1. 選中 `GameOverContentPanel` GameObject
2. 在 Inspector 中取消勾選 `Active`（初始隱藏）
3. 這樣結算頁面在遊戲開始時不會顯示

---

## ✅ 整合完成後的運作方式

### 自動運作流程

1. **玩家死亡或遊戲失敗**：
   - 玩家死亡 → `GameManager.HandlePlayerDeath()`
   - Target 到達逃亡點 → `GameManager.OnTargetReachedEscapePoint()`
   - `GameManager` 狀態變為 `GameOver`
   - 觸發 `OnGameStateChanged` 事件

2. **GameOverUIManager 自動響應**：
   - 監聽 `GameManager.OnGameStateChanged`
   - 當狀態為 `GameOver` 時自動顯示結算頁面
   - 其他狀態時自動隱藏

3. **GameOverUI 更新統計數據**：
   - 自動從 `GameManager` 獲取統計數據
   - 更新擊殺數、遊戲時間顯示

4. **按鈕功能**：
   - **重新開始** 按鈕 → `GameManager.RestartGame()`（重新載入遊戲場景）
   - **返回主選單** 按鈕 → `GameManager.ReturnToMainMenu()`（返回主選單場景）

### 不需要手動控制

結算頁面會**自動跟隨 GameManager 的狀態**，不需要手動調用 `SetVisible()`。

---

## 📝 程式碼使用範例

### 基本使用（自動模式）

```csharp
// 不需要任何程式碼！
// 系統會自動處理：
// - 玩家死亡 → 顯示結算頁面
// - 統計數據自動更新
// - 按鈕點擊 → 執行對應操作
```

### 手動控制（如果需要）

```csharp
// 獲取結算頁面管理器
GameOverUIManager gameOverManager = gameUIManager.GetGameOverUIManager();

// 手動顯示/隱藏（通常不需要）
gameOverManager.SetVisible(true);
gameOverManager.SetVisible(false);

// 獲取 GameOverUI 引用
GameOverUI gameOverUI = gameOverManager.GetGameOverUI();
```

### 與 GameManager 整合

```csharp
// GameManager 已經處理了遊戲結束邏輯
// 不需要額外程式碼

// 如果想在遊戲結束時做其他事情：
void Start()
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }
}

private void OnGameStateChanged(GameManager.GameState oldState, 
                                 GameManager.GameState newState)
{
    if (newState == GameManager.GameState.GameOver)
    {
        // 遊戲結束時的額外邏輯
        Debug.Log("遊戲已結束");
        
        // 可以獲取統計數據
        int enemiesKilled = GameManager.Instance.GetEnemiesKilled();
        float gameTime = GameManager.Instance.GetGameTime();
        int highScore = GameManager.Instance.GetHighScore();
    }
}
```

---

## 🎨 UI 設計建議

### 視覺設計

1. **背景**：
   - 建議使用半透明黑色背景（Alpha: 200-230）
   - 覆蓋整個螢幕，讓玩家專注於結算頁面

2. **面板設計**：
   - 使用圓角矩形面板
   - 居中顯示
   - 適當的內邊距和間距

3. **文字樣式**：
   - 標題使用較大字體（24-32）
   - 統計數據使用中等字體（18-24）
   - 使用清晰的顏色對比

4. **按鈕設計**：
   - 使用明顯的按鈕樣式
   - 適當的按鈕大小（易於點擊）
   - 懸停和點擊效果

### 佈局建議

```
┌─────────────────────────────┐
│     遊戲結束 / Game Over     │  ← 標題（可選）
├─────────────────────────────┤
│                             │
│   擊殺數: 15                │
│   遊戲時間: 120.5 秒        │
│                             │
├─────────────────────────────┤
│   [重新開始]  [返回主選單]   │
└─────────────────────────────┘
```

---

## 🔄 與現有系統的兼容性

### ✅ 保持不變的功能

- **GameManager** 的遊戲結束邏輯保持不變
- **統計數據追蹤** 功能保持不變
- **場景切換** 功能保持不變

### ✨ 新增的功能

- **結算頁面顯示** - 不再直接返回主選單，先顯示統計數據
- **統計數據展示** - 自動顯示擊殺數、遊戲時間
- **統一管理** - 通過 `GameUIManager` 統一管理
- **更好的用戶體驗** - 玩家可以查看自己的表現

---

## 🐛 故障排除

### Q1: 結算頁面不顯示？

**檢查清單**：
1. ✅ `GameOverUIManager` 已添加到 `GameOverPanel`
2. ✅ `GameOverUI` 已添加到 `GameOverPanel`
3. ✅ `Game Over Panel` 欄位已設定（指向 `GameOverContentPanel`）
4. ✅ `Auto Subscribe To Game Manager` 已勾選
5. ✅ `GameManager.Instance` 存在
6. ✅ 玩家死亡時 GameManager 狀態變為 `GameOver`
7. ✅ `GameOverContentPanel` 初始狀態為 Active（或會在顯示時自動啟用）

**Debug 方法**：
```csharp
void Start()
{
    // 檢查 GameManager
    if (GameManager.Instance == null)
    {
        Debug.LogError("GameManager.Instance 不存在！");
        return;
    }
    
    // 檢查訂閱
    GameManager.Instance.OnGameStateChanged += (oldState, newState) =>
    {
        Debug.Log($"遊戲狀態變化: {oldState} -> {newState}");
        
        if (newState == GameManager.GameState.GameOver)
        {
            Debug.Log("遊戲結束狀態已觸發！");
        }
    };
}
```

### Q2: 統計數據不顯示或顯示錯誤？

**檢查清單**：
1. ✅ `Enemies Killed Text` 欄位已設定
2. ✅ `Game Time Text` 欄位已設定
3. ✅ `GameManager.Instance` 存在且正常運作

**Debug 方法**：
```csharp
// 在 GameOverUI.UpdateStatistics() 中添加 Debug
private void UpdateStatistics()
{
    if (GameManager.Instance == null)
    {
        Debug.LogError("GameManager.Instance 不存在！");
        return;
    }
    
    int enemiesKilled = GameManager.Instance.GetEnemiesKilled();
    float gameTime = GameManager.Instance.GetGameTime();
    
    Debug.Log($"統計數據 - 擊殺: {enemiesKilled}, 時間: {gameTime}");
    
    // ... 更新 UI
}
```

### Q3: 按鈕沒有反應？

**檢查清單**：
1. ✅ `Restart Button` 欄位已設定
2. ✅ `Main Menu Button` 欄位已設定
3. ✅ `GameManager.Instance` 存在
4. ✅ 按鈕事件已正確綁定（在 `GameOverUI.Start()` 中）

**Debug 方法**：
```csharp
// 在 GameOverUI 的按鈕點擊方法中添加 Debug
private void OnRestartClicked()
{
    Debug.Log("[GameOverUI] Restart button clicked");
    if (GameManager.Instance == null)
    {
        Debug.LogError("GameManager.Instance 不存在！");
        return;
    }
    GameManager.Instance.RestartGame();
}
```

### Q4: 結算頁面在遊戲開始時就顯示？

**解決方法**：
1. 確保 `GameOverContentPanel` 初始狀態為 **非 Active**
2. 檢查 `GameOverUIManager` 的 `Initialize()` 是否正確調用 `SetVisible(false)`

### Q5: 文字格式不正確？

**檢查**：
- 確認 `GameOverUI` 組件中的格式字串設定正確
- 格式字串必須包含 `{0}` 作為數值佔位符
- 例如：`"擊殺數: {0}"`、`"遊戲時間: {0:F1} 秒"`

---

## 📊 整合前後對比

### 之前

```
玩家死亡
    ↓
GameManager.HandlePlayerDeath()
    ↓
GameOver() → 狀態變為 GameOver
    ↓
延遲 2 秒後自動返回主選單
```

### 之後

```
玩家死亡
    ↓
GameManager.HandlePlayerDeath()
    ↓
GameOver() → 狀態變為 GameOver
    ↓
觸發 OnGameStateChanged 事件
    ↓
GameOverUIManager 自動顯示結算頁面
    ↓
GameOverUI 更新統計數據
    ↓
玩家選擇操作（重新開始 / 返回主選單）
```

**優點**：
- ✅ 玩家可以查看自己的表現
- ✅ 更好的用戶體驗
- ✅ 統一的 UI 管理架構
- ✅ 更容易擴展和維護

---

## ✅ 檢查清單

完成以下步驟即可完成設定：

- [ ] 創建 `GameOverPanel` GameObject
- [ ] 創建 `GameOverContentPanel` UI 面板
- [ ] 創建統計數據文字元素（擊殺數、遊戲時間）
- [ ] 創建按鈕元素（重新開始、返回主選單）
- [ ] 添加 `GameOverUIManager` 組件並設定
- [ ] 添加 `GameOverUI` 組件並連接所有 UI 元素
- [ ] 在 `GameUIManager` 中連接 `GameOverUIManager`
- [ ] 設定 `GameOverContentPanel` 初始為非 Active
- [ ] 測試玩家死亡時結算頁面是否顯示
- [ ] 測試統計數據是否正確顯示
- [ ] 測試按鈕功能（重新開始、返回主選單）

---

## 🎊 完成！

現在您的遊戲結束結算頁面已經設定完成！

**關鍵功能**：
- ✨ 玩家死亡時自動顯示結算頁面
- ✨ 自動顯示統計數據（擊殺數、遊戲時間）
- ✨ 提供重新開始和返回主選單選項
- ✨ 統一的 UI 管理架構
- ✨ 自動跟隨 GameManager 狀態
- ✨ 正確追蹤擊殺數（敵人死亡時自動更新）

**注意事項**：
- ⚠️ 勝利頁面目前尚未實作，勝利時仍會自動返回主選單
- ⚠️ 確保 `GameManager.Instance` 存在且正常運作
- ⚠️ 確保所有 UI 元素引用都已正確設定

如果遇到任何問題，請查看 Console 的錯誤訊息或參考本文檔的故障排除部分！

---

## 📚 相關檔案

- `GameOverUI.cs` - 結算頁面 UI 邏輯
- `GameOverUIManager.cs` - 結算頁面管理器
- `GameManager.cs` - 遊戲管理器（處理遊戲結束邏輯）
- `GameUIManager.cs` - UI 總協調器

