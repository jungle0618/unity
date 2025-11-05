# Unity Script Execution Order 設定指南

## 概述

為了確保 EntityManager 和依賴 Player 的組件正確初始化，需要設定 Unity 的 Script Execution Order。

**重要更新**：
- GameManager 現在負責協調所有管理器的初始化順序
- Target 管理已移至 GameManager，不再由 Player 處理
- 使用 `[DefaultExecutionOrder]` 屬性確保基本執行順序
- GameManager 提供初始化協調邏輯作為額外保障

## 執行順序設定

### 推薦的執行順序

**注意**：Unity 的默認執行順序（UnityEngine 系統）通常在 `0` 左右。
我們的腳本應該在 UnityEngine 系統之後執行，但要在其他遊戲系統之前。

1. **EntityManager** (優先級: `50`)
   - 負責初始化 Player 位置和所有實體
   - 必須在其他需要 Player 的系統之前執行

2. **Player** (優先級: `100`)
   - Player 自身的初始化
   - 在 EntityManager 之後執行，確保 EntityManager 已設置好 Player

3. **GameManager** (優先級: `150`)
   - 遊戲狀態管理
   - **管理器初始化協調**：負責協調所有管理器的初始化順序
   - **Target 管理**：記錄和管理所有 Target 實體（不再由 Player 管理）
   - 在 Player 初始化之後執行

4. **其他系統管理器** (優先級: `200`)
   - DangerousManager
   - ItemManager
   - 等其他系統

5. **UI 管理器** (優先級: `300`)
   - GameUIManager
   - HealthUIManager
   - DangerUIManager
   - HotbarUIManager
   - TilemapMapUIManager
   - 在所有遊戲系統初始化完成後執行

## 設定步驟

### 方法 1: 使用 Unity Editor 設定

1. 打開 `Edit > Project Settings > Script Execution Order`
2. 點擊 `+` 添加以下腳本：
   - `EntityManager`: `50`
   - `Player`: `100`
   - `GameManager`: `150`
   - `DangerousManager`: `200`（如果存在）
   - `ItemManager`: `200`（如果存在）
   - `GameUIManager`: `300`
   - `HealthUIManager`: `300`
   - `DangerUIManager`: `300`
   - `HotbarUIManager`: `300`
   - `TilemapMapUIManager`: `300`

### 方法 2: 使用 Script 屬性（推薦）

在腳本中添加 `[DefaultExecutionOrder]` 屬性：

```csharp
// EntityManager.cs
[DefaultExecutionOrder(50)]
public class EntityManager : MonoBehaviour
{
    // ...
}

// Player.cs
[DefaultExecutionOrder(100)]
public class Player : BaseEntity<PlayerState>
{
    // ...
}

// GameManager.cs
[DefaultExecutionOrder(150)]
public class GameManager : MonoBehaviour
{
    // ...
}

// GameUIManager.cs
[DefaultExecutionOrder(300)]
public class GameUIManager : MonoBehaviour
{
    // ...
}
```

## 事件系統（備用方案）

除了執行順序，我們還實現了事件系統作為備用方案：

- `EntityManager.OnPlayerReady`: 當 Player 初始化完成並設置好位置後觸發
- `EntityManager.OnManagerInitialized`: 當 EntityManager 完全初始化完成後觸發
- `GameManager.OnPhaseInitialized`: 當特定初始化階段完成時觸發（CoreSystems, GameSystems, UISystems）

UI 管理器會自動訂閱這些事件，確保在 Player 準備好後才初始化。

## GameManager 初始化協調

GameManager 提供自動初始化協調功能（`autoInitializeManagers`）：

1. **階段 1: 核心系統** (CoreSystems)
   - 等待 EntityManager 初始化完成
   - 觸發 `OnPhaseInitialized(CoreSystems)` 事件

2. **階段 2: 遊戲系統** (GameSystems)
   - 等待 DangerousManager 和 ItemManager 初始化完成
   - 觸發 `OnPhaseInitialized(GameSystems)` 事件

3. **階段 3: UI 系統** (UISystems)
   - 等待 GameUIManager 初始化完成
   - 觸發 `OnPhaseInitialized(UISystems)` 事件

## Target 管理

**重要變更**：Target 管理已從 Player 移至 GameManager

- **GameManager** 負責記錄所有 Target 實體
- **EntityEventManager** 在添加 Target 時自動註冊到 GameManager
- **GameManager.AreAllTargetsDead()** 用於檢查勝利條件
- Player 不再需要 Target 參數或引用

**使用方式**：
```csharp
// EntityEventManager 自動註冊
GameManager.Instance.RegisterTarget(target);

// GameManager 檢查勝利條件
if (GameManager.Instance.AreAllTargetsDead())
{
    // 所有 Target 已死亡
}
```

## 初始化流程

```
1. EntityManager.Awake() / Start() [50]
   ├─ InitializePlayerReferences()（從 Prefab 生成 Player）
   ├─ LoadPatrolData()
   ├─ SetPlayerInitialPosition()
   └─ OnPlayerReady?.Invoke() ✅

2. Player.Awake() / Start() [100]
   └─ 初始化 Player 組件

3. GameManager.Start() [150]
   ├─ SetupPlayerEventListeners()
   └─ InitializeManagersSequentially()（協調初始化）
      ├─ 等待 EntityManager 準備完成
      ├─ 等待 DangerousManager 和 ItemManager 準備完成
      └─ 等待 GameUIManager 準備完成

4. 其他系統管理器.Start() [200]
   └─ DangerousManager, ItemManager 等

5. UI Managers.Start() [300]
   ├─ 如果 Player 已準備好：直接初始化
   └─ 如果 Player 未準備好：訂閱 OnPlayerReady 事件
```

## 注意事項

1. **執行順序不是絕對的**：即使在同一個優先級，執行順序仍然可能不確定
2. **使用事件系統更可靠**：事件系統可以確保在正確的時機初始化
3. **檢查 null 引用**：始終檢查 Player 是否為 null 再使用
4. **延遲初始化**：如果必須在 Update 中訪問，可以在第一幀延遲初始化

## 最佳實踐

✅ **推薦做法**：
- 使用 `[DefaultExecutionOrder]` 屬性設定執行順序
- 啟用 GameManager 的自動初始化協調（`autoInitializeManagers = true`）
- UI 管理器訂閱 `EntityManager.OnPlayerReady` 事件
- 在初始化方法中檢查 Player 是否為 null
- 使用 `GameManager.Instance.RegisterTarget()` 註冊 Target（通常由 EntityEventManager 自動處理）

❌ **不推薦做法**：
- 直接在 Awake() 中假設 Player 已準備好
- 不檢查 null 引用就訪問 Player
- 硬編碼延遲（如 `yield return new WaitForSeconds(1)`）
- 在 Player 中管理 Target 引用（已移至 GameManager）

