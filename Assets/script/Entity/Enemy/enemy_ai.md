# Enemy AI 系統文檔

## 📋 目錄

1. [系統概述](#系統概述)
2. [組件架構](#組件架構)
3. [狀態機系統](#狀態機系統)
4. [AI 決策流程](#ai-決策流程)
5. [各狀態詳細說明](#各狀態詳細說明)
6. [區域系統整合](#區域系統整合)
7. [攻擊系統](#攻擊系統)
8. [性能優化](#性能優化)
9. [API 參考](#api-參考)

---

## 系統概述

Enemy AI 系統是一個基於狀態機的智能敵人行為系統，負責處理敵人的巡邏、警戒、追擊、搜索和返回等行為。系統採用組件化設計，將 AI 邏輯、移動、偵測、攻擊等功能分離，便於維護和擴展。

### 核心特性

- **狀態機驅動**：使用 `EnemyStateMachine` 管理敵人的狀態轉換
- **智能決策**：`EnemyAIHandler` 負責所有 AI 決策邏輯
- **區域感知**：整合 Guard Area 和 Safe Area 系統，根據玩家位置和狀態調整行為
- **性能優化**：使用間隔更新和攝影機剔除減少 CPU 負載
- **路徑規劃**：支援直接追擊和路徑規劃兩種移動方式

---

## 組件架構

### 核心組件

```
Enemy (BaseEntity)
├── EnemyStateMachine      # 狀態機（運行時創建）
├── EnemyAIHandler         # AI 決策邏輯
├── EnemyMovement          # 移動控制
├── EnemyDetection         # 偵測系統
├── EnemyVisualizer        # 視覺化
├── EnemyAttackController  # 攻擊控制（可選）
├── EntityHealth           # 血量管理
└── EntityStats            # 屬性管理
```

### 組件職責

| 組件 | 職責 |
|------|------|
| **Enemy** | 主控制器，整合所有組件，提供對外接口 |
| **EnemyStateMachine** | 管理狀態轉換，處理狀態進入/退出邏輯 |
| **EnemyAIHandler** | 執行 AI 決策，處理各狀態的行為邏輯 |
| **EnemyMovement** | 處理移動邏輯，支援巡邏、追擊、路徑規劃 |
| **EnemyDetection** | 視野偵測，判斷是否看到玩家 |
| **EnemyAttackController** | 攻擊邏輯，處理攻擊冷卻和範圍檢查 |
| **EnemyVisualizer** | 視覺化顯示，包括視野範圍和血量條 |

---

## 狀態機系統

### 狀態定義

```csharp
public enum EnemyState
{
    Patrol,     // 巡邏：沿著巡邏點移動
    Alert,      // 警戒：看到玩家但未追擊（等待或判斷是否追擊）
    Chase,      // 追擊：主動追擊玩家
    Search,     // 搜索：到最後看到玩家的位置搜索
    Return,     // 返回：返回巡邏起點
    Dead        // 死亡：敵人已死亡
}
```

### 狀態轉換圖

```
[Patrol] ──(看到玩家)──> [Alert]
   ↑                          │
   │                          │
   │                    (應該追擊)──> [Chase]
   │                          │          │
   │                    (警戒時間到)      │
   │                          │          │
   │                          ↓          │
   │                      [Patrol]       │
   │                                     │
   │                          (失去視線) │
   │                                     │
   │                          (超出追擊範圍) │
   │                                     │
   └──(返回起點)──<──[Return] <─────────┘
         │
         │
   (到達起點)
         │
         ↓
   [Patrol]

[Chase] ──(失去視線)──> [Search]
   ↑                          │
   │                          │
   │                    (搜索時間到)──> [Alert]
   │                          │
   │                    (重新看到玩家)──> [Chase]
   │
   └──(卡住/撞牆)──────────────┘
```

### 狀態轉換條件

| 從狀態 | 到狀態 | 條件 |
|--------|--------|------|
| Patrol | Alert | 看到玩家 |
| Alert | Chase | 看到玩家 **且** 應該追擊（區域判斷） |
| Alert | Patrol | 警戒時間結束 |
| Chase | Search | 失去玩家視線 |
| Chase | Return | 玩家超出追擊範圍 |
| Chase | Alert | 看到玩家但不應該追擊（Safe Area + 空手） |
| Search | Chase | 重新看到玩家 |
| Search | Alert | 搜索時間結束 |
| Search | Return | 超出追擊範圍 |
| Return | Patrol | 到達返回目標 |
| Return | Chase | 看到玩家 |

---

## AI 決策流程

### 更新流程

```
FixedUpdate (Enemy)
    │
    ├──> UpdateCachedData()          # 更新快取數據（位置、方向、是否看到玩家）
    │
    └──> aiHandler.UpdateAIDecision() # AI 決策更新（間隔更新）
            │
            ├──> 檢查 ShouldUpdateAI() # 攝影機剔除檢查
            │
            ├──> 更新 Alert Timer
            │
            └──> 根據當前狀態執行對應處理
                    │
                    ├──> HandlePatrolState()
                    ├──> HandleAlertState()
                    ├──> HandleChaseState()
                    ├──> HandleSearchState()
                    └──> HandleReturnState()

Update (Enemy)
    │
    └──> aiHandler.ExecuteMovement() # 執行移動（每幀更新，確保流暢）
```

### 更新頻率

- **AI 決策更新**：`aiUpdateInterval`（預設 0.15 秒，約 6-7 FPS）
- **移動執行**：每幀更新（確保移動流暢）
- **快取數據更新**：`CACHE_UPDATE_INTERVAL`（預設 0.1 秒）

### 攝影機剔除優化

當敵人不在攝影機範圍內 **且** 不在 Chase 或 Search 狀態時，跳過 AI 決策更新：

```csharp
if (!enemyDetection.ShouldUpdateAI())
{
    // 視野外且不在 Chase/Search 狀態，跳過 AI 更新
    return;
}
```

**注意**：移動邏輯仍會在 `ExecuteMovement()` 中執行，確保敵人能繼續移動。

---

## 各狀態詳細說明

### 1. Patrol（巡邏）

**行為**：
- 沿著設定的巡邏點循環移動
- 如果沒有巡邏點，使用 `EnemyMovement.PerformPatrol()`
- 視野方向跟隨移動方向

**狀態轉換**：
- 看到玩家 → `Alert`

**移動方式**：
```csharp
enemyMovement.MoveAlongLocations(patrolLocations, currentPatrolIndex);
```

---

### 2. Alert（警戒）

**行為**：
- 看到玩家但尚未決定是否追擊
- 繼續沿著巡邏點移動（如果有的話）
- 更新警戒計時器

**狀態轉換**：
- 看到玩家 **且** 應該追擊 → `Chase`
- 警戒時間結束 → `Patrol`

**區域判斷**：
- **Guard Area**：始終追擊
- **Safe Area**：
  - 玩家持有武器 → 追擊
  - 危險等級觸發 → 追擊
  - 玩家空手且危險等級安全 → 不追擊（保持在 Alert）

---

### 3. Chase（追擊）

**行為**：
- 直接追擊玩家位置
- 更新武器方向朝向玩家
- 更新視野方向朝向玩家
- 在攻擊範圍內嘗試攻擊

**狀態轉換**：
- 失去玩家視線 → `Search`
- 玩家超出追擊範圍 → `Return`
- 卡住/撞牆 → `Search`
- 看到玩家但不應該追擊 → `Alert`

**移動方式**：
```csharp
enemyMovement.ChaseTarget(playerPosition);
```

**攻擊檢查**：
- 使用 `GetEffectiveAttackRange()` 獲取有效攻擊範圍
- 支援近戰武器和遠程武器（槍械）的不同攻擊範圍
- 在攻擊範圍內調用 `enemy.TryAttackPlayer()`

---

### 4. Search（搜索）

**行為**：
- 移動到最後看到玩家的位置
- 到達搜索位置後，在該位置停留一段時間（`searchTime`，預設 3 秒）
- 如果重新看到玩家，立即轉到追擊狀態

**狀態轉換**：
- 重新看到玩家 → `Chase`
- 搜索時間結束 → `Alert`
- 超出追擊範圍 → `Return`

**移動方式**：
```csharp
enemyMovement.ChaseTargetWithRotation(lastSeenPosition, enemyDetection);
```

**特殊邏輯**：
- 如果搜索時重新看到玩家，更新最後看到的位置並清除路徑，強制重新計算路徑

---

### 5. Return（返回）

**行為**：
- 使用路徑規劃返回第一個巡邏點（或起始位置）
- 如果看到玩家，立即轉到追擊狀態

**狀態轉換**：
- 看到玩家 → `Chase`
- 到達返回目標 → `Patrol`（重置巡邏索引為 0）

**移動方式**：
```csharp
enemyMovement.MoveTowardsWithPathfinding(returnTarget, 1f);
```

---

### 6. Dead（死亡）

**行為**：
- 停止所有移動
- 禁用 GameObject
- 觸發死亡事件

**狀態轉換**：
- 無（終止狀態）

---

## 區域系統整合

### Guard Area（守衛區域）

**行為**：
- 敵人始終追擊玩家
- 敵人始終可以攻擊玩家

**判斷邏輯**：
```csharp
if (AreaManager.Instance.IsInGuardArea(playerPosition))
{
    return true; // 追擊/攻擊
}
```

---

### Safe Area（安全區域）

**行為**：
- 只有在特定條件下才追擊/攻擊玩家

**追擊條件**：
- 玩家持有武器 **OR**
- 危險等級被觸發（`DangerLevel > Safe`）

**判斷邏輯**：
```csharp
bool playerHasWeapon = playerItemHolder.IsCurrentItemWeapon;
bool isDangerTriggered = dangerManager.CurrentDangerLevelType != DangerLevel.Safe;
bool shouldChase = playerHasWeapon || isDangerTriggered;
```

**應用場景**：
- `EnemyAIHandler.ShouldChasePlayer()` - 判斷是否應該追擊
- `EnemyAttackController.ShouldAttackPlayer()` - 判斷是否應該攻擊

---

### 系統開關

可以通過 `GameSettings.UseGuardAreaSystem` 來啟用/停用區域系統：

```csharp
if (!GameSettings.Instance.UseGuardAreaSystem)
{
    return true; // 原始行為：總是追擊/攻擊
}
```

---

## 攻擊系統

### 攻擊流程

```
Chase State
    │
    ├──> 檢查距離 <= GetEffectiveAttackRange()
    │
    ├──> enemy.TryAttackPlayer(playerTransform)
    │         │
    │         ├──> EnemyAttackController.TryAttackPlayer()
    │         │         │
    │         │         ├──> 檢查 ShouldAttackPlayer() (區域判斷)
    │         │         ├──> 檢查攻擊冷卻
    │         │         ├──> 檢查距離
    │         │         ├──> 更新武器方向
    │         │         └──> itemHolder.TryAttack()
    │         │
    │         └──> 返回攻擊是否成功
    │
    └──> 更新攻擊冷卻計時器
```

### 攻擊範圍

**有效攻擊範圍**（`GetEffectiveAttackRange()`）：
- 如果啟用 `useWeaponAttackRange`：
  - **遠程武器**（RangedWeapon）：使用 `weapon.AttackRange`
  - **近戰武器**（MeleeWeapon）：使用 `weapon.AttackRange`
- 否則：使用 `attackDetectionRange`（預設 3f）

### 攻擊冷卻

- 預設冷卻時間：`attackCooldown`（可在 Inspector 中設定）
- 每次攻擊成功後更新 `lastAttackTime`

---

## 性能優化

### 1. 間隔更新

**AI 決策更新**：
- 使用 `aiUpdateInterval`（預設 0.15 秒）控制更新頻率
- 在 `Enemy.FixedUpdate()` 中檢查時間間隔

**快取數據更新**：
- 使用 `CACHE_UPDATE_INTERVAL`（預設 0.1 秒）
- 減少重複計算位置、方向等數據

### 2. 攝影機剔除

**條件**：
- 敵人不在攝影機範圍內
- **且** 不在 Chase 或 Search 狀態

**實現**：
```csharp
if (!enemyDetection.ShouldUpdateAI())
{
    return; // 跳過 AI 更新
}
```

**注意**：移動邏輯仍會執行，確保敵人能繼續移動。

### 3. 快取數據

**快取的數據**：
- `cachedPosition` - 敵人位置
- `cachedDirectionToPlayer` - 到玩家的方向
- `cachedCanSeePlayer` - 是否看到玩家

**更新時機**：
- 在 `Enemy.UpdateCachedData()` 中定期更新
- 傳遞給 `EnemyAIHandler.UpdateCachedData()` 供 AI 使用

---

## API 參考

### Enemy 類

#### 初始化
```csharp
public void Initialize(Transform playerTarget)
```
初始化敵人，設定目標並開始 AI。

#### 狀態控制
```csharp
public void ForceChangeState(EnemyState newState)
```
強制改變敵人狀態（用於調試或特殊情況）。

#### 攻擊
```csharp
public bool TryAttackPlayer(Transform playerTransform)
```
嘗試攻擊玩家（檢查距離、冷卻等）。

#### 屬性查詢
```csharp
public float GetEffectiveAttackRange()
```
獲取有效攻擊範圍（根據武器類型自動調整）。

---

### EnemyAIHandler 類

#### 初始化
```csharp
public void Initialize(float attackRange, float searchTime)
```
初始化 AI Handler，設定攻擊範圍和搜索時間。

#### 巡邏點管理
```csharp
public void SetPatrolLocations(Vector3[] locations)
public Vector3[] GetPatrolLocations()
public int GetCurrentPatrolIndex()
```
設定和獲取巡邏點。

#### 數據更新
```csharp
public void UpdateCachedData(Vector2 position, Vector2 directionToPlayer, bool canSeePlayer)
```
更新快取數據（由 Enemy 調用）。

#### AI 更新
```csharp
public void UpdateAIDecision()
```
執行 AI 決策更新（間隔更新）。

#### 移動執行
```csharp
public void ExecuteMovement()
```
執行移動（每幀更新，確保流暢）。

---

### EnemyStateMachine 類

#### 狀態轉換
```csharp
public override void ChangeState(EnemyState newState)
```
改變狀態，觸發狀態進入/退出邏輯。

#### 警戒計時器
```csharp
public void UpdateAlertTimer()
public bool IsAlertTimeUp()
```
更新和檢查警戒計時器。

---

### EnemyAttackController 類

#### 攻擊
```csharp
public bool TryAttackPlayer(Transform playerTransform)
```
嘗試攻擊玩家。

#### 攻擊範圍
```csharp
public float GetEffectiveAttackRange()
```
獲取有效攻擊範圍。

#### 攻擊冷卻
```csharp
public void SetAttackCooldown(float cooldownSeconds)
```
設定攻擊冷卻時間。

---

## 配置參數

### Enemy Inspector 參數

| 參數 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `alertTime` | float | 2f | 警戒狀態持續時間（秒） |
| `attackCooldown` | float | 0.5f | 攻擊冷卻時間（秒） |
| `attackDetectionRange` | float | 3f | 攻擊偵測範圍 |
| `useWeaponAttackRange` | bool | true | 是否使用武器的實際攻擊範圍 |
| `chaseSpeedMultiplier` | float | 1.5f | 追擊速度倍數 |
| `pickupRange` | float | 2f | 物品撿取範圍 |

### EnemyAIHandler 初始化參數

| 參數 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `attackRange` | float | - | 攻擊範圍（從 Enemy.GetEffectiveAttackRange() 獲取） |
| `searchTime` | float | 3f | 搜索狀態持續時間（秒） |

---

## 調試技巧

### 1. 狀態轉換日誌

狀態轉換時會輸出日誌：
```
ChangeState: Patrol -> Alert
ChangeState: Alert -> Chase
```

### 2. 區域判斷日誌

區域判斷時會輸出詳細日誌：
```
[EnemyAI] Player in GUARD AREA - will chase
[EnemyAI] Player in SAFE AREA with WEAPON - will chase
[EnemyAI] Player in SAFE AREA with EMPTY HANDS and danger is SAFE - will NOT chase
```

### 3. 搜索狀態日誌

搜索狀態的關鍵事件會輸出日誌：
```
{enemyName}: 追擊時卡住，轉到搜索狀態
{enemyName}: 已到達搜索位置，開始搜索玩家
{enemyName}: 搜索時看到玩家，更新目標位置到 {position}
```

### 4. Gizmos 視覺化

在 Scene 視圖中選擇 Enemy GameObject，可以看到：
- 視野範圍（視野錐形）
- 攻擊範圍（圓形）
- 巡邏點（如果設定）

---

## 常見問題

### Q: 敵人為什麼不追擊玩家？

**可能原因**：
1. 玩家在 Safe Area 且空手且危險等級為 Safe
2. 敵人不在攝影機範圍內且不在 Chase/Search 狀態（被剔除）
3. 敵人狀態機未正確初始化

**解決方法**：
- 檢查 `AreaManager.Instance.IsInGuardArea()` 返回值
- 檢查 `GameSettings.Instance.UseGuardAreaSystem` 是否啟用
- 檢查敵人狀態是否正確

---

### Q: 敵人為什麼卡住不動？

**可能原因**：
1. 路徑規劃失敗
2. 移動目標被障礙物阻擋
3. 敵人狀態未正確更新

**解決方法**：
- 檢查 `enemyMovement.IsStuckOrHittingWall()` 返回值
- 檢查路徑規劃網格是否正確設定
- 檢查敵人是否在 Chase 狀態（會自動轉到 Search）

---

### Q: 敵人攻擊範圍不正確？

**可能原因**：
1. `useWeaponAttackRange` 未啟用
2. 武器未正確裝備
3. 武器類型不支援

**解決方法**：
- 啟用 `useWeaponAttackRange`
- 檢查 `itemHolder.CurrentWeapon` 是否為 null
- 檢查武器是否為 `RangedWeapon` 或 `MeleeWeapon`

---

## 版本歷史

- **v2.0** (2025-12)
  - 整合區域系統（Guard Area / Safe Area）
  - 優化性能（攝影機剔除、間隔更新）
  - 移除重複的 detection 方法
  - 統一 chaseRange 到 EnemyDetection

- **v1.0** (2025-11)
  - 初始版本
  - 基本狀態機系統
  - 巡邏、追擊、搜索功能

---

**最後更新**：2025-12  
**維護者**：開發團隊

