# EntityManager 模組狀態

## ✅ 已完成的模組

### 1. EntityDataLoader.cs (279 行)
**狀態**: ✅ 完成
**職責**: 從 patroldata.txt 載入實體數據
**功能**:
- 解析 patroldata.txt 文件格式
- 支援 Enemy、Target、Player 三種類型
- 提供數據查詢方法

**主要方法**:
- `LoadPatrolData(TextAsset)` - 載入數據文件
- `GetEntitiesByType(EntityType)` - 獲取指定類型的實體
- `GetEntityData(int, EntityType)` - 獲取指定實體數據
- `GetPlayerData()` - 獲取 Player 數據

### 2. EntityItemManager.cs (155 行)
**狀態**: ✅ 完成
**職責**: 管理物品映射和裝備
**功能**:
- 物品名稱到 Prefab 的映射
- 為實體裝備物品

**主要方法**:
- `InitializeItemMappings(ItemMapping[])` - 初始化映射
- `GetItemPrefab(string)` - 獲取物品 Prefab
- `EquipItemsToEntity(MonoBehaviour, List<string>)` - 裝備物品

### 3. EntityPool.cs (227 行)
**狀態**: ✅ 完成
**職責**: Enemy 對象池管理
**功能**:
- 對象池的創建和回收
- 活躍/剔除/死亡狀態管理

**主要方法**:
- `GetPooledEnemy()` - 從池中獲取
- `ReturnEnemyToPool(Enemy)` - 返回到池
- `MarkEnemyActive(Enemy)` - 標記活躍
- `MarkEnemyCulled(Enemy)` - 標記剔除
- `MarkEnemyDead(Enemy)` - 標記死亡

### 4. AttackSystem.cs (235 行)
**狀態**: ✅ 完成
**職責**: 統一攻擊處理
**功能**:
- 處理所有實體的攻擊
- 傷害計算和範圍檢測
- 攻擊規則判斷

**主要方法**:
- `HandleAttack(Vector2, float, GameObject)` - 處理攻擊
- `CheckEntitiesInAttackRange(...)` - 範圍檢測
- `GetAttackDamage(GameObject)` - 獲取傷害值
- `ShouldAttackTarget(EntityType, EntityType)` - 攻擊規則
- `AddEntity(IEntity)` / `RemoveEntity(IEntity)` - 實體註冊

### 5. EntityPerformanceOptimizer.cs (295 行)
**狀態**: ✅ 完成
**職責**: 性能優化（剔除、批次處理）
**功能**:
- 視錐剔除處理
- 批次更新 AI
- 玩家位置快取

**主要方法**:
- `StartManagement()` - 開始管理循環
- `UpdateEnemyCullingOptimized()` - 剔除處理
- `CheckCulledEnemiesForReactivation()` - 重新激活檢查
- `UpdateCachedPlayerPosition()` - 更新位置快取

### 6. EntityEventManager.cs (201 行)
**狀態**: ✅ 完成
**職責**: 事件訂閱和管理
**功能**:
- 訂閱所有實體的攻擊事件
- 處理 Target 死亡和逃亡事件

**主要方法**:
- `SubscribeToPlayerEvents(Player)` - 訂閱 Player
- `SubscribeToEnemyEvents(Enemy)` - 訂閱 Enemy
- `AddTarget(Target)` - 添加 Target
- `UnsubscribeFromTargetEvents()` - 取消訂閱

### 7. EntitySpawner.cs (約 400 行)
**狀態**: ✅ 完成
**職責**: 實體生成和初始化
**功能**:
- 生成 Player、Enemy、Target
- 初始化實體屬性
- 裝備物品和設置數據

**主要方法**:
- `InitializePlayer()` - 初始化 Player
- `SpawnEnemy(Vector3, int)` - 生成 Enemy
- `SpawnTarget(Vector3, Vector3, int)` - 生成 Target
- `SpawnInitialEntities()` - 批量生成

## 📊 統計

- **總模組數**: 7 個
- **總代碼行數**: 約 1,592 行（拆分自 2,279 行）
- **平均模組大小**: 約 227 行
- **命名空間**: `Game.EntityManager`

## 🔄 下一步

### 8. 重構 EntityManager.cs
**狀態**: ⏳ 待進行
**目標**: 將 EntityManager.cs 重構為 Facade 模式的主控制器

**需要做的事情**:
1. 創建子系統實例
2. 初始化子系統
3. 委託方法調用到子系統
4. 保持公共 API 不變
5. 處理依賴注入

**預估大小**: 約 300-400 行（從 2,279 行減少）

## 📝 注意事項

1. **命名空間**: 所有模組使用 `Game.EntityManager` 命名空間
2. **依賴關係**: 模組之間有清晰的依賴關係，避免循環依賴
3. **測試**: 每個模組都可以獨立測試
4. **向後兼容**: 重構後的 EntityManager 應保持相同的公共 API

