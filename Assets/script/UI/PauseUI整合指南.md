# PauseMenuUI 整合到 GameUIManager 指南

## 📋 概述

已將 `PauseMenuUI` 整合到統一的 `GameUIManager` 架構中，通過新增的 `PauseUIManager` 來管理。

---

## 🎯 整合架構

```
GameUIManager (總協調器)
├── HealthUIManager
├── DangerUIManager
├── HotbarUIManager
├── TilemapMapUIManager
└── PauseUIManager ⭐ 新增
    └── PauseMenuUI (現有的，保持不變)
```

---

## 🔧 Unity 設定步驟

### 步驟 1：創建 PauseUIManager GameObject

在 Canvas 下創建：

```
Canvas
└── PausePanel (GameObject)
    ├── PauseUIManager (Component) ← 新增
    └── PauseMenuUI (Component) ← 現有的，保持不變
        └── PauseMenuPanel (GameObject)
            ├── ResumeButton
            ├── RestartButton
            └── MainMenuButton
```

### 步驟 2：設定 PauseUIManager

在 `PausePanel` 上添加 `PauseUIManager` 組件：

```
PauseUIManager:
  Pause Menu UI: 拖入 PauseMenuUI 組件
  Auto Find Pause Menu: ✅ 勾選（如果只有一個）
  Auto Subscribe To Game Manager: ✅ 勾選（推薦）
```

### 步驟 3：連接到 GameUIManager

在 Canvas 的 `GameUIManager` 組件中：

```
UI Managers:
  Pause UI Manager: 拖入 PausePanel 上的 PauseUIManager
```

---

## ✅ 整合完成後的運作方式

### 自動運作

1. **GameManager 控制暫停**：
   - 按 ESC 鍵 → `GameManager.TogglePause()`
   - `GameManager` 狀態變為 `Paused`
   - 觸發 `OnGameStateChanged` 事件

2. **PauseUIManager 自動響應**：
   - 監聽 `GameManager.OnGameStateChanged`
   - 當狀態為 `Paused` 時自動顯示
   - 其他狀態時自動隱藏

3. **PauseMenuUI 處理按鈕**：
   - Resume 按鈕 → `GameManager.ResumeGame()`
   - Restart 按鈕 → `GameManager.RestartGame()`
   - Main Menu 按鈕 → `GameManager.ReturnToMainMenu()`

### 不需要手動控制

暫停選單會**自動跟隨 GameManager 的狀態**，不需要手動調用 `SetVisible()`。

---

## 📝 程式碼使用範例

### 基本使用（自動模式）

```csharp
// 不需要任何程式碼！
// 系統會自動處理：
// - ESC 鍵 → 顯示/隱藏暫停選單
// - 按鈕點擊 → 執行對應操作
```

### 手動控制（如果需要）

```csharp
// 獲取暫停選單管理器
PauseUIManager pauseManager = gameUIManager.GetPauseUIManager();

// 手動顯示/隱藏（通常不需要）
pauseManager.SetVisible(true);
pauseManager.SetVisible(false);

// 獲取 PauseMenuUI 引用
PauseMenuUI pauseMenu = pauseManager.GetPauseMenuUI();
```

### 與 GameManager 整合

```csharp
// GameManager 已經處理了暫停邏輯
// 不需要額外程式碼

// 如果想在暫停時做其他事情：
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
    if (newState == GameManager.GameState.Paused)
    {
        // 暫停時的額外邏輯
        Debug.Log("遊戲已暫停");
    }
    else if (newState == GameManager.GameState.Playing)
    {
        // 恢復時的額外邏輯
        Debug.Log("遊戲已恢復");
    }
}
```

---

## 🔄 與現有系統的兼容性

### ✅ 保持不變的功能

- **PauseMenuUI** 的所有功能保持不變
- **GameManager** 的暫停邏輯保持不變
- **按鈕功能** 完全保持不變

### ✨ 新增的功能

- **統一管理** - 通過 `GameUIManager` 統一管理
- **更好的架構** - 符合模組化設計
- **易於擴展** - 可以輕鬆添加更多暫停相關 UI

---

## 🎨 可選：暫停時隱藏其他 UI

如果需要暫停時隱藏某些 UI（例如地圖、血條等），可以這樣做：

```csharp
public class PauseUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameUIManager gameUIManager;
    
    private void OnGameStateChanged(GameManager.GameState oldState, 
                                    GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.Paused)
        {
            SetVisible(true);
            
            // 可選：隱藏其他 UI
            if (gameUIManager != null)
            {
                gameUIManager.SetMapUIVisible(false);
                // gameUIManager.SetHealthUIVisible(false); // 可選
            }
        }
        else
        {
            SetVisible(false);
            
            // 可選：恢復其他 UI
            if (gameUIManager != null)
            {
                // gameUIManager.SetMapUIVisible(true); // 可選
            }
        }
    }
}
```

---

## 🐛 故障排除

### Q1: 暫停選單不顯示？

**檢查**：
1. ✅ `PauseUIManager` 已添加到 GameObject
2. ✅ `PauseMenuUI` 引用已設定
3. ✅ `Auto Subscribe To Game Manager` 已勾選
4. ✅ `GameManager.Instance` 存在
5. ✅ 按 ESC 鍵時 GameManager 狀態變為 `Paused`

**Debug**：
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
    };
}
```

### Q2: 按鈕沒有反應？

**檢查**：
1. ✅ `PauseMenuUI` 的按鈕引用已設定
2. ✅ `GameManager.Instance` 存在
3. ✅ 按鈕事件已正確綁定（在 `PauseMenuUI.Start()` 中）

### Q3: 重複顯示？

如果 `PauseMenuUI` 和 `PauseUIManager` 都訂閱了事件，可能會重複處理，但不會造成問題（兩者都做相同的事情）。

如果想避免重複，可以：
- 在 `PauseMenuUI` 中移除 `OnGameStateChanged` 訂閱
- 只讓 `PauseUIManager` 處理顯示/隱藏

---

## 📊 整合前後對比

### 之前

```
GameManager (ESC 鍵)
    ↓ 觸發事件
PauseMenuUI (直接訂閱)
    ↓ 顯示/隱藏
```

### 之後

```
GameManager (ESC 鍵)
    ↓ 觸發事件
PauseUIManager (訂閱)
    ↓ 控制顯示/隱藏
    ↓
PauseMenuUI (處理按鈕)
    ↓ 執行操作
GameManager
```

**優點**：
- ✅ 統一的 UI 管理架構
- ✅ 更容易擴展和維護
- ✅ 與其他 UI 系統一致

---

## ✅ 檢查清單

完成以下步驟即可完成整合：

- [ ] 創建 `PausePanel` GameObject
- [ ] 添加 `PauseUIManager` 組件
- [ ] 設定 `PauseMenuUI` 引用
- [ ] 在 `GameUIManager` 中連接 `PauseUIManager`
- [ ] 測試 ESC 鍵暫停功能
- [ ] 測試按鈕功能（Resume、Restart、Main Menu）

---

## 🎊 完成！

現在您的暫停選單已經整合到統一的 UI 管理系統中！

**關鍵優勢**：
- ✨ 保持現有功能不變
- ✨ 符合統一的架構設計
- ✨ 自動跟隨 GameManager 狀態
- ✨ 易於維護和擴展

如果遇到任何問題，請查看 Console 的錯誤訊息或參考本文檔的故障排除部分！

