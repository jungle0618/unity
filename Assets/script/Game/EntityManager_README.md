# EntityManager 文件

## 概述

`EntityManager` 是遊戲的核心實體管理系統，負責統一管理所有遊戲實體（Player、Enemy、Target）的生命週期、生成、回收、攻擊處理和性能優化。

## 主要職責

### 1. 實體生命週期管理
- **生成實體**：從 Prefab 生成 Player、Enemy、Target
- **對象池管理**：使用對象池優化 Enemy 的創建和銷毀
- **實體註冊**：維護統一的實體註冊表（`activeEntities`），用於統一訪問所有實體
- **實體回收**：處理死亡實體的回收和清理

### 2. 數據驅動實體生成
- **讀取配置數據**：從 `patroldata.txt` 讀取實體數據
  - 實體位置
  - 巡邏點（Enemy）
  - 逃亡點（Target）
  - 初始物品
  - 實體類型（Enemy/Target/Player）
- **初始化實體屬性**：根據數據設置實體位置、裝備物品等

### 3. 統一攻擊系統
- **攻擊處理**：統一處理所有實體的攻擊（Player、Enemy、Target）
- **傷害計算**：從武器的 `AttackDamage` 獲取傷害值
- **攻擊規則**：定義不同實體類型之間的攻擊規則
  - Player 可以攻擊 Enemy 和 Target
  - Enemy 可以攻擊 Player
  - Target 不能攻擊（未武裝）
- **範圍檢測**：檢查攻擊範圍內的所有實體並造成傷害

### 4. 性能優化
- **視錐剔除（Frustum Culling）**：根據相機可見性暫停不可見實體的 AI 更新
- **批次處理**：每幀處理有限數量的敵人，避免性能峰值
- **更新頻率控制**：
  - 管理更新間隔：`updateInterval`（預設 0.2 秒）
  - AI 更新間隔：`aiUpdateInterval`（預設 0.15 秒）
  - 每幀處理數量：`enemiesPerFrameUpdate`（預設 3 個）
- **位置快取**：快取玩家位置，減少距離計算開銷

### 5. 危險等級系統整合
- **屬性調整**：根據當前危險等級（Safe/Low/Medium/High/Critical）調整實體屬性
  - 視野範圍乘數（`viewRangeMultiplier`）
  - 視野角度乘數（`viewAngleMultiplier`）
  - 移動速度乘數（`speedMultiplier`）
  - 傷害減少（`damageReduction`）
- **動態更新**：當危險等級變化時，更新所有活躍敵人的屬性

### 6. 玩家偵測系統整合
- **自動註冊**：自動將生成的 Enemy 註冊到 `PlayerDetection` 系統
- **可見性管理**：處理 Enemy 的可見性變化事件

### 7. 物品管理
- **物品映射**：維護物品名稱到 Prefab 的映射表
- **裝備管理**：為 Player 裝備初始物品（從 `patroldata.txt` 讀取）
- **物品分配**：為 Enemy 分配初始物品

### 8. 事件系統
- **`OnPlayerReady`**：當 Player 初始化完成並設置好位置後觸發
- **`OnManagerInitialized`**：當 EntityManager 完全初始化完成後觸發

## 核心數據結構

### EntityType 枚舉
```csharp
public enum EntityType
{
    None,   // 無效類型
    Enemy,  // 敵人
    Target, // 目標
    Player  // 玩家（僅用於設置初始位置）
}
```

### EnemyData 類
用於儲存從 `patroldata.txt` 讀取的實體數據：
- `entityIndex`：實體索引
- `type`：實體類型（EntityType）
- `itemNames`：初始物品列表
- `patrolPoints`：巡邏點陣列（Enemy）
- `escapePoint`：逃亡點（Target）

### ItemMapping 類
物品名稱到 Prefab 的映射：
- `itemName`：物品名稱或類型
- `itemPrefab`：對應的 Item Prefab

### DangerLevelMultipliers 類
危險等級屬性乘數：
- `viewRangeMultiplier`：視野範圍乘數
- `viewAngleMultiplier`：視野角度乘數
- `speedMultiplier`：移動速度乘數
- `damageReduction`：傷害減少比例（0-1）

## 主要方法

### 實體生成
- `InitializePlayerReferences()`：初始化玩家引用（從 Prefab 生成）
- `SpawnEnemy(Vector3 position, int enemyIndex)`：生成 Enemy
- `SpawnTarget(Vector3 position, Vector3 escapePoint, int targetIndex)`：生成 Target
- `SpawnInitialEnemies()`：生成所有初始實體（從數據讀取）

### 對象池管理
- `GetPooledEnemy()`：從對象池獲取 Enemy
- `ReturnEnemyToPool(Enemy enemy)`：將 Enemy 返回到對象池
- `CreatePooledEnemy()`：創建新的 Enemy 並加入對象池

### 攻擊處理
- `HandleAttack(Vector2 attackCenter, float attackRange, GameObject attacker)`：統一處理所有實體的攻擊
- `CheckEntitiesInAttackRange(...)`：檢查攻擊範圍內的所有實體並造成傷害
- `GetAttackDamage(GameObject attacker)`：從攻擊者獲取攻擊傷害值
- `ShouldAttackTarget(EntityType attackerType, EntityType targetType)`：判斷攻擊者是否應該攻擊目標

### 數據載入
- `LoadPatrolData()`：從 `patroldata.txt` 載入實體數據
- `InitializeItemMappings()`：初始化物品名稱映射

### patroldata.txt 文件格式

文件格式說明：

```
# 註釋行（以 # 開頭）
# 每個實體定義包含 3-4 行，用空行分隔

# Enemy 格式
{EntityIndex} {EntityType}
{Items}  # 用分號分隔多個物品，使用 "None" 表示無物品
{Patrol Points}  # x,y 格式，用 | 分隔多個點

# Target 格式（需要第 4 行）
{EntityIndex} Target
{Items}
{Patrol Points}  # 第一個點為初始位置
{Escape Point}  # x,y 格式，逃亡點

# Player 格式（只需要第一個點作為初始位置）
{EntityIndex} Player
{Items}
{Initial Position}  # x,y 格式
```

**範例**：
```
# Enemy 0
0 Enemy
Knife;RedKey
20,50

# Enemy 1 (多個巡邏點)
1 Enemy
Knife;RedKey
22,41|35,41|35,14|22,14

# Target 0
0 Target
None
4,35
4,10

# Player
0 Player
Knife;Pistol
10,10
```

### 事件訂閱
- `SubscribeToPlayerEvents()`：訂閱 Player 的攻擊事件
- `SubscribeToTargetEvents()`：訂閱 Target 的死亡和逃亡事件
- `HandleTargetDied(Target target)`：處理 Target 死亡事件
- `HandleTargetReachedEscapePoint(Target target)`：處理 Target 到達逃亡點事件

### 性能優化
- `UpdateEntityManagement()`：統一管理實體更新（批次處理）
- `CullEnemies()`：視錐剔除處理
- `UpdateEnemyAI()`：批次更新 Enemy AI

## 執行順序

`EntityManager` 使用 `[DefaultExecutionOrder(50)]` 確保：
- 在 UnityEngine 系統之後執行
- 在其他遊戲系統（Player、GameManager 等）之前執行

## 初始化流程

1. **Start()** 執行順序：
   ```
   InitializePlayerReferences()      // 初始化玩家引用
   → InitializeItemMappings()         // 初始化物品映射
   → LoadPatrolData()                 // 載入 patrol data
   → InitializePlayerDetection()      // 初始化玩家偵測系統
   → InitializeDangerousManager()     // 初始化危險等級系統
   → InitializeManager()               // 初始化管理器（開始協程）
   ```

2. **SpawnInitialEnemies()** 執行順序：
   ```
   設置 Player 初始位置
   → 裝備 Player 初始物品
   → 訂閱 Player 攻擊事件
   → 觸發 OnPlayerReady 事件
   → 生成所有 Target
   → 生成所有 Enemy
   → 觸發 OnManagerInitialized 事件
   ```

## 配置參數

### 實體管理
- `maxActiveEnemies`：最大活躍敵人數量（預設 36）
- `poolSize`：對象池大小（預設 50）

### 性能優化
- `cullingDistance`：剔除距離（預設 25）
- `updateInterval`：管理更新間隔（預設 0.2 秒）
- `aiUpdateInterval`：AI 更新間隔（預設 0.15 秒）
- `enemiesPerFrameUpdate`：每幀處理的敵人數量（預設 3）

### 危險等級乘數
- `safeLevel`：Safe 等級的屬性乘數
- `lowLevel`：Low 等級的屬性乘數
- `mediumLevel`：Medium 等級的屬性乘數
- `highLevel`：High 等級的屬性乘數
- `criticalLevel`：Critical 等級的屬性乘數

## 公共 API

### 屬性
- `Player`：獲取 Player 實例
- `PlayerTransform`：獲取 Player 的 Transform
- `PlayerPosition`：獲取 Player 位置
- `PlayerEulerAngles`：獲取 Player 旋轉角度
- `ActiveEnemyCount`：活躍敵人數量
- `PooledEnemyCount`：對象池中的敵人數量
- `DeadEnemyCount`：死亡敵人數量
- `TotalEnemyCount`：總敵人數量
- `ItemMappingDict`：物品映射字典（供 ItemManager 使用）

### 方法

#### 實體查詢
- `GetItemPrefab(string itemName)`：根據物品名稱獲取 Prefab
- `GetEnemyPatrolPoints(int enemyIndex)`：獲取指定 Enemy 的巡邏點
- `GetEnemyCount()`：獲取總 Enemy 數量
- `GetRemainingEnemyCount()`：獲取剩餘（未死亡）的 Enemy 數量
- `HasLivingEnemies()`：檢查是否還有存活的 Enemy
- `GetActiveTargetCount()`：獲取活躍的 Target 數量
- `AreAllTargetsDead()`：檢查是否所有 Target 都已死亡
- `GetVisibleEnemyCount()`：獲取可見的 Enemy 數量

#### 實體管理
- `SpawnEnemy(Vector3 position, int enemyIndex)`：生成 Enemy（公共 API）
- `LogAllEnemySpawnPoints()`：輸出所有敵人生成點資訊（用於除錯）
- `KillAllEnemies()`：殺死所有 Enemy（用於測試或遊戲結束）

#### 敵人警報和可見性
- `AlertNearbyEnemies(Vector2 position, float alertRange)`：警報範圍內的所有敵人（當玩家開槍時呼叫）
- `OnEnemyVisibilityChanged(Enemy enemy, bool isVisible)`：處理敵人可見性變化
- `SetEnemyVisualization(Enemy enemy, bool canVisualize)`：設置敵人視覺化狀態
- `ForceUpdateEnemyVisibility()`：強制更新所有敵人的可見性

#### 配置
- `SetMaxActiveEnemies(int newMax)`：動態設置最大活躍敵人數量
- `SetPlayerDetection(bool enabled, bool autoRegister)`：設置玩家偵測系統

### 事件
- `OnPlayerReady`：當 Player 初始化完成後觸發
- `OnManagerInitialized`：當 EntityManager 完全初始化完成後觸發

## 依賴關係

### 依賴的系統
- `Player`：玩家實體
- `Enemy`：敵人間體
- `Target`：目標實體
- `DangerousManager`：危險等級管理器
- `PlayerDetection`：玩家偵測系統
- `ItemManager`：物品管理器（使用物品映射）

### 被依賴的系統
- `GameManager`：訂閱 `OnPlayerReady` 事件
- `UI 系統`：訂閱 `OnPlayerReady` 事件以獲取 Player 引用
- `ItemManager`：使用物品映射字典

## 使用示例

### 訂閱事件

```csharp
// 在 GameManager 或其他系統中
void Start()
{
    entityManager.OnPlayerReady += OnPlayerReady;
    entityManager.OnManagerInitialized += OnManagerInitialized;
}

void OnPlayerReady()
{
    // Player 已經準備就緒，可以安全訪問
    Player player = entityManager.Player;
    // 進行初始化...
}

void OnManagerInitialized()
{
    // EntityManager 完全初始化完成
    Debug.Log($"生成 {entityManager.ActiveEnemyCount} 個敵人");
}
```

### 查詢實體狀態

```csharp
// 檢查是否所有目標都已死亡
if (entityManager.AreAllTargetsDead())
{
    // 觸發勝利條件
}

// 獲取剩餘敵人數量
int remainingEnemies = entityManager.GetRemainingEnemyCount();

// 檢查是否還有存活的敵人
if (!entityManager.HasLivingEnemies())
{
    // 所有敵人都已死亡
}
```

### 警報附近敵人

```csharp
// 當玩家開槍時，警報附近的敵人
void OnPlayerShoot(Vector2 shootPosition)
{
    float alertRange = 15f; // 警報範圍
    entityManager.AlertNearbyEnemies(shootPosition, alertRange);
}
```

### 動態調整配置

```csharp
// 根據遊戲進度調整最大敵人數量
entityManager.SetMaxActiveEnemies(50);

// 啟用/禁用玩家偵測系統
entityManager.SetPlayerDetection(true, true);
```

## 注意事項

1. **執行順序**：確保 `EntityManager` 在 `Player`、`GameManager` 之前初始化（使用 `[DefaultExecutionOrder(50)]`）
2. **Prefab 設定**：必須在 Inspector 中設定 `playerPrefab`、`enemyPrefab`、`targetPrefab`
3. **數據文件**：必須設定 `patrolDataFile`（`patroldata.txt`），否則會使用默認數據
4. **物品映射**：必須在 Inspector 中設定物品映射表，否則 Enemy 無法裝備物品
5. **事件訂閱**：其他系統應訂閱 `OnPlayerReady` 事件，而不是直接訪問 `Player` 屬性（因為 Player 可能尚未初始化）
6. **實體註冊表**：所有實體（Player、Enemy、Target）都會自動註冊到 `activeEntities`（`HashSet<IEntity>`），用於統一攻擊處理
7. **對象池**：只有 Enemy 使用對象池，Player 和 Target 不使用對象池

## 除錯

啟用 `showDebugInfo` 可以查看詳細的除錯資訊：
- 實體生成日誌
- 攻擊處理日誌
- 性能優化資訊
- 事件觸發日誌

### 常用除錯方法

```csharp
// 輸出所有敵人生成點資訊
entityManager.LogAllEnemySpawnPoints();

// 檢查實體狀態
Debug.Log($"活躍敵人: {entityManager.ActiveEnemyCount}");
Debug.Log($"對象池: {entityManager.PooledEnemyCount}");
Debug.Log($"死亡敵人: {entityManager.DeadEnemyCount}");
Debug.Log($"總敵人: {entityManager.TotalEnemyCount}");
```

## 常見問題

### Q: Player 為 null 怎麼辦？
**A**: 確保訂閱 `OnPlayerReady` 事件，而不是在 `Start()` 中直接訪問 `Player` 屬性。

### Q: 敵人沒有生成？
**A**: 檢查：
1. `patrolDataFile` 是否已設定
2. `enemyPrefab` 是否已設定
3. `patroldata.txt` 文件格式是否正確
4. 查看 Console 是否有錯誤訊息

### Q: 攻擊沒有造成傷害？
**A**: 檢查：
1. 物品映射是否正確設定
2. 武器是否有 `AttackDamage` 屬性
3. 攻擊者是否訂閱了 `ItemHolder.OnAttackPerformed` 事件

### Q: 性能問題？
**A**: 調整以下參數：
- `updateInterval`：增加間隔以減少更新頻率
- `aiUpdateInterval`：增加 AI 更新間隔
- `enemiesPerFrameUpdate`：減少每幀處理數量
- `cullingDistance`：調整剔除距離

## 性能優化建議

### 對象池調整
- 如果敵人數量固定，將 `poolSize` 設為略大於 `maxActiveEnemies`
- 如果敵人會動態生成，將 `poolSize` 設為預期的最大數量

### 更新頻率調整
根據遊戲場景調整：
- **簡單場景**：可以增加 `updateInterval` 和 `aiUpdateInterval`
- **複雜場景**：減少間隔以確保響應性
- **性能瓶頸**：減少 `enemiesPerFrameUpdate` 的值

### 視錐剔除優化
- `cullingDistance` 應該根據遊戲視野範圍設定
- 過大的值會導致性能浪費，過小的值會導致敵人 AI 異常

### 批次處理優化
- `enemiesPerFrameUpdate` 建議根據目標 FPS 和敵人數量調整
- 例如：60 FPS，36 個敵人，每幀 3 個 = 每 12 幀（0.2 秒）更新一輪

## 架構設計說明

### IEntity 統一接口
EntityManager 使用 `IEntity` 接口統一管理所有實體：
- `Player`、`Enemy`、`Target` 都實現 `IEntity` 接口
- 統一攻擊系統通過 `activeEntities` 註冊表訪問所有實體
- 簡化了多態攻擊處理的實現

### 事件驅動設計
- 使用事件系統解耦各系統之間的依賴
- `OnPlayerReady` 確保其他系統在正確時機訪問 Player
- `OnManagerInitialized` 標記整個系統初始化完成

### 對象池模式
- 僅對 Enemy 使用對象池（因為可能大量生成）
- Player 和 Target 不使用對象池（通常只有一個實例）

## 未來擴展

- 支援更多實體類型（NPC、動物等）
- 動態實體生成（根據遊戲進度、玩家行為）
- 更細粒度的性能優化選項（按區域、按類型分別優化）
- 實體狀態持久化（保存/載入遊戲狀態）
- 實體組系統（按類型、按區域分組管理）
- 實體行為樹整合（更複雜的 AI 邏輯）

