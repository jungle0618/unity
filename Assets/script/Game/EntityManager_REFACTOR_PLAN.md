# EntityManager 重構計劃

## 目標

將 2279 行的 `EntityManager.cs` 拆分成多個專注於單一職責的模組，提高代碼可維護性和可測試性。

## 建議的模組結構

```
Game/
├── EntityManager.cs              # 主控制器（Facade 模式）
├── EntityManager/
│   ├── EntitySpawner.cs         # 實體生成系統
│   ├── EntityPool.cs            # 對象池管理
│   ├── EntityDataLoader.cs      # 數據載入系統
│   ├── AttackSystem.cs          # 統一攻擊系統
│   ├── EntityPerformanceOptimizer.cs  # 性能優化系統
│   ├── EntityItemManager.cs     # 物品管理系統
│   └── EntityEventManager.cs    # 事件管理系統
└── EntityManager_README.md      # 文檔
```

## 各模組職責

### 1. EntitySpawner.cs
**職責**：實體生成和初始化
- `SpawnEnemy()` - 生成 Enemy
- `SpawnTarget()` - 生成 Target
- `InitializePlayer()` - 初始化 Player
- `SpawnInitialEnemies()` - 批量生成初始實體
- `EquipPlayerItems()` - 裝備玩家物品

**依賴**：
- EntityPool（獲取 Enemy）
- EntityDataLoader（獲取實體數據）
- EntityItemManager（裝備物品）
- EntityEventManager（訂閱事件）

### 2. EntityPool.cs
**職責**：Enemy 對象池管理
- `GetPooledEnemy()` - 從池中獲取 Enemy
- `ReturnEnemyToPool()` - 返回到池中
- `CreatePooledEnemy()` - 創建新的 Enemy
- `InitializePool()` - 初始化對象池
- `ClearPool()` - 清空對象池

**依賴**：
- Enemy Prefab

### 3. EntityDataLoader.cs
**職責**：從 patroldata.txt 載入數據
- `LoadPatrolData()` - 載入數據文件
- `CreateDefaultPatrolData()` - 創建默認數據
- `GetEnemyData()` - 獲取 Enemy 數據
- `GetTargetData()` - 獲取 Target 數據
- `GetPlayerData()` - 獲取 Player 數據

**數據結構**：
- `EnemyData` 類
- `EntityType` 枚舉

### 4. AttackSystem.cs
**職責**：統一攻擊處理
- `HandleAttack()` - 處理攻擊事件
- `CheckEntitiesInAttackRange()` - 檢查攻擊範圍
- `GetAttackDamage()` - 獲取傷害值
- `ShouldAttackTarget()` - 判斷攻擊規則
- `GetEntityType()` - 獲取實體類型

**依賴**：
- IEntity 接口（activeEntities 註冊表）

### 5. EntityPerformanceOptimizer.cs
**職責**：性能優化（剔除、批次處理）
- `UpdateEntityManagement()` - 統一管理更新
- `CullEnemies()` - 視錐剔除處理
- `UpdateEnemyAI()` - 批次更新 AI
- `UpdateCachedPlayerPosition()` - 更新玩家位置快取
- `CheckCulledEnemiesForReactivation()` - 檢查剔除的敵人

**配置參數**：
- `cullingDistance`
- `updateInterval`
- `aiUpdateInterval`
- `enemiesPerFrameUpdate`

### 6. EntityItemManager.cs
**職責**：物品映射和裝備管理
- `InitializeItemMappings()` - 初始化物品映射
- `GetItemPrefab()` - 獲取物品 Prefab
- `EquipItemsToEntity()` - 為實體裝備物品

**數據結構**：
- `ItemMapping` 類
- `itemNameToPrefab` 字典

### 7. EntityEventManager.cs
**職責**：事件訂閱和管理
- `SubscribeToPlayerEvents()` - 訂閱 Player 事件
- `SubscribeToTargetEvents()` - 訂閱 Target 事件
- `SubscribeToEnemyEvents()` - 訂閱 Enemy 事件
- `HandleTargetDied()` - 處理 Target 死亡
- `HandleTargetReachedEscapePoint()` - 處理 Target 逃亡

## 重構後的 EntityManager

`EntityManager.cs` 將作為 **Facade 模式**的主控制器：

```csharp
public class EntityManager : MonoBehaviour
{
    // 子系統引用
    private EntitySpawner spawner;
    private EntityPool pool;
    private EntityDataLoader dataLoader;
    private AttackSystem attackSystem;
    private EntityPerformanceOptimizer optimizer;
    private EntityItemManager itemManager;
    private EntityEventManager eventManager;
    
    // 公共屬性（委託給子系統）
    public Player Player => spawner.Player;
    public int ActiveEnemyCount => pool.ActiveEnemyCount;
    
    // 初始化（組裝子系統）
    private void Start()
    {
        InitializeSubsystems();
        // ...
    }
}
```

## 重構步驟

1. ✅ 創建重構計劃文檔（本文件）
2. ✅ 創建 EntityDataLoader.cs - 數據載入系統
3. ✅ 創建 EntityItemManager.cs - 物品管理系統
4. ✅ 創建 EntityPool.cs - 對象池管理系統
5. ✅ 創建 AttackSystem.cs - 統一攻擊系統
6. ✅ 創建 EntityPerformanceOptimizer.cs - 性能優化系統
7. ✅ 創建 EntityEventManager.cs - 事件管理系統
8. ✅ 創建 EntitySpawner.cs - 實體生成系統
9. ⏳ 重構 EntityManager.cs 作為主控制器（Facade）
10. ⏳ 更新文檔和測試

## 優勢

1. **單一職責原則**：每個類只負責一個功能
2. **可測試性**：每個模組可以獨立測試
3. **可維護性**：代碼更易理解和修改
4. **可擴展性**：新增功能更容易
5. **代碼重用**：子系統可以在其他地方重用

## 注意事項

1. **保持公共 API 不變**：確保外部系統不受影響
2. **逐步遷移**：一個模組一個模組地遷移
3. **測試覆蓋**：確保重構後功能正常
4. **文檔更新**：更新 README 反映新架構

