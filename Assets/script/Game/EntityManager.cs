using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Game.EntityManager;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// EntityManager 類別：管理所有實體（敵人、玩家、目標）
/// 職責：作為 Facade 主控制器，整合所有子系統
/// 優化：降低 CPU 和 RAM 消耗，減少 Update 頻率
/// </summary>
[DefaultExecutionOrder(50)] // 在 UnityEngine 系統之後執行，但在其他遊戲系統之前
public class EntityManager : MonoBehaviour
{
    [Header("實體 Prefab 設定")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject targetPrefab;
    
    [Header("玩家管理")]
    private Player player; // 運行時生成的 Player 實例（由 EntitySpawner 管理）

    // 使用命名空間中的 ItemMapping 類型（為了 Unity 序列化兼容，保留一個別名）
    [System.Serializable]
    public class ItemMapping : Game.EntityManager.ItemMapping { }

    [Header("物品映射設定（統一管理）")]
    [Tooltip("物品名稱/類型到 Prefab 的映射，用於敵人生成和世界物品生成")]
    [SerializeField] private ItemMapping[] itemMappings;
    
    // 公共 API：供 ItemManager 使用（委託給子系統）
    public Dictionary<string, GameObject> ItemMappingDict => itemManager?.ItemMappingDict ?? new Dictionary<string, GameObject>();
    
    /// <summary>
    /// 根據物品名稱/類型獲取 Prefab
    /// </summary>
    public GameObject GetItemPrefab(string itemName)
    {
        return itemManager?.GetItemPrefab(itemName);
    }

    [Header("Patrol Data 設定")]
    [SerializeField] private TextAsset patrolDataFile;

    [Header("AI 設定")]
    [SerializeField] private float aiUpdateInterval = 0.15f; // AI 更新間隔

    [Header("除錯資訊")]
    [SerializeField] private bool showDebugInfo = false;

    [Header("玩家偵測整合")]
    [SerializeField] private bool enablePlayerDetection = true; // 啟用玩家偵測系統
    [SerializeField] private bool autoRegisterWithPlayerDetection = true; // 自動註冊到玩家偵測系統
    
    // 使用命名空間中的 DangerLevelMultipliers 類型（為了 Unity 序列化兼容，保留一個別名）
    [System.Serializable]
    public class DangerLevelMultipliers : Game.EntityManager.EntitySpawner.DangerLevelMultipliers { }
    
    [Header("危險等級調整參數")]
    [Tooltip("五種危險等級對應的屬性乘數")]
    [SerializeField] private DangerLevelMultipliers safeLevel = new DangerLevelMultipliers { viewRangeMultiplier = 1.0f, viewAngleMultiplier = 1.0f, speedMultiplier = 1.0f, damageReduction = 0f };
    [SerializeField] private DangerLevelMultipliers lowLevel = new DangerLevelMultipliers { viewRangeMultiplier = 1.1f, viewAngleMultiplier = 1.1f, speedMultiplier = 1.1f, damageReduction = 0.1f };
    [SerializeField] private DangerLevelMultipliers mediumLevel = new DangerLevelMultipliers { viewRangeMultiplier = 1.3f, viewAngleMultiplier = 1.3f, speedMultiplier = 1.3f, damageReduction = 0.2f };
    [SerializeField] private DangerLevelMultipliers highLevel = new DangerLevelMultipliers { viewRangeMultiplier = 1.6f, viewAngleMultiplier = 1.6f, speedMultiplier = 1.6f, damageReduction = 0.3f };
    [SerializeField] private DangerLevelMultipliers criticalLevel = new DangerLevelMultipliers { viewRangeMultiplier = 2.0f, viewAngleMultiplier = 2.0f, speedMultiplier = 2.0f, damageReduction = 0.5f };

    // 實體類型枚舉（保留用於 IEntity 接口，但內部統一使用 EntityDataLoader.EntityType）
    public enum EntityType
    {
        None,   // 無效類型
        Enemy,  // 敵人
        Target, // 目標
        Player  // 玩家（僅用於設置初始位置）
    }
    
    // 注意：EnemyData 類已移除，統一使用 EntityDataLoader.EntityData

    // ========== 子系統引用 ==========
    private EntityDataLoader dataLoader;
    private EntityItemManager itemManager;
    private AttackSystem attackSystem;
    private EntityEventManager eventManager;
    private EntitySpawner spawner;
    private WinConditionManager winConditionManager;

    // 注意：統一實體註冊表已移至 AttackSystem，通過 attackSystem.ActiveEntities 訪問

    // 統計資訊 - 委託給子系統
    public int ActiveEnemyCount => GetActiveEnemyCount();
    public int DeadEnemyCount => GetDeadEnemyCount();
    public int TotalEnemyCount => ActiveEnemyCount + DeadEnemyCount;

    // 玩家偵測系統引用
    private PlayerDetection playerDetection;
    
    // 危險等級管理
    private DangerousManager dangerousManager;

    // 玩家相關公共屬性（委託給 spawner）
    public Player Player => spawner?.Player ?? player;
    public Transform PlayerTransform => Player != null ? Player.transform : null;
    public Vector2 PlayerPosition => Player != null ? (Vector2)Player.transform.position : Vector2.zero;
    public Vector3 PlayerEulerAngles => Player != null ? Player.transform.eulerAngles : Vector3.zero;
    
    // 初始化完成事件
    public event System.Action OnPlayerReady; // 當 Player 初始化完成並設置好位置後觸發
    public event System.Action OnManagerInitialized; // 當 EntityManager 完全初始化完成後觸發

    #region Unity 生命週期

    private void Start()
    {
        InitializeSubsystems();
    }
    
    /// <summary>
    /// 初始化所有子系統
    /// </summary>
    private void InitializeSubsystems()
    {
        // 1. 初始化數據載入器
        dataLoader = new EntityDataLoader(showDebugInfo);
        if (patrolDataFile != null)
        {
            dataLoader.LoadPatrolData(patrolDataFile);
        }

        // 2. 初始化物品管理器（必須在生成實體之前完成）
        // 驗證 itemMappings 是否已配置
        if (itemMappings == null || itemMappings.Length == 0)
        {
            Debug.LogError("[EntityManager] itemMappings 未配置！無法生成帶有物品的實體。請在 Inspector 中設置 itemMappings。");
            return;
        }
        
        itemManager = new EntityItemManager(showDebugInfo);
        // 轉換 ItemMapping[] 為 Game.EntityManager.ItemMapping[]
        var itemMappingsArray = new Game.EntityManager.ItemMapping[itemMappings.Length];
        for (int i = 0; i < itemMappings.Length; i++)
        {
            itemMappingsArray[i] = itemMappings[i];
        }
        itemManager.InitializeItemMappings(itemMappingsArray);
        
        // 驗證 itemManager 是否成功初始化
        if (itemManager.ItemMappingDict == null || itemManager.ItemMappingDict.Count == 0)
        {
            Debug.LogError("[EntityManager] itemManager 初始化失敗！物品映射為空。無法生成帶有物品的實體。");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[EntityManager] Item mappings 已確定：{itemManager.ItemMappingDict.Count} 個物品映射");
        }

        // 3. 初始化攻擊系統
        attackSystem = new AttackSystem(showDebugInfo);
        
        // 4. 初始化事件管理器
        eventManager = new EntityEventManager(attackSystem, showDebugInfo);
        eventManager.OnTargetDied += HandleTargetDied;
        eventManager.OnTargetReachedEscapePoint += HandleTargetReachedEscapePoint;

        // 5. 初始化系統引用
        InitializePlayerDetection();
        InitializeDangerousManager();

        // 6. 初始化生成器（需要所有其他子系統）
        InitializeSpawner();

        // 7. 初始化勝利條件管理器
        InitializeWinConditionManager();

        // 8. 開始生成實體
        StartCoroutine(DelayedSpawnInitialEntities());
    }
    
    /// <summary>
    /// 初始化勝利條件管理器
    /// </summary>
    private void InitializeWinConditionManager()
    {
        // 從 dataLoader 尋找 Exit 點
        Vector3 exitPoint = Vector3.zero;
        
        if (dataLoader != null && dataLoader.EntityDataList != null)
        {
            foreach (var data in dataLoader.EntityDataList)
            {
                if (data.type == EntityDataLoader.EntityType.Exit)
                {
                    if (data.patrolPoints != null && data.patrolPoints.Length > 0)
                    {
                        exitPoint = data.patrolPoints[0];
                        break;
                    }
                }
            }
        }
        
        if (exitPoint == Vector3.zero)
        {
            Debug.LogWarning("[EntityManager] 未在 patroldata.txt 中找到 Exit 點，使用預設位置 (4, 54)");
            exitPoint = new Vector3(4, 54, 0);
        }
        
        // 創建 WinConditionManager GameObject
        GameObject winConditionObj = new GameObject("WinConditionManager");
        winConditionManager = winConditionObj.AddComponent<WinConditionManager>();
        winConditionManager.Initialize(exitPoint);
        
        if (showDebugInfo)
        {
            Debug.Log($"[EntityManager] WinConditionManager 已初始化，出口點: {exitPoint}");
        }
    }
    
    /// <summary>
    /// 初始化生成器
    /// </summary>
    private void InitializeSpawner()
    {
        spawner = new EntitySpawner(
            playerPrefab, enemyPrefab, targetPrefab,
            dataLoader, itemManager, eventManager, attackSystem,
            dangerousManager, playerDetection,
            enablePlayerDetection, autoRegisterWithPlayerDetection,
            aiUpdateInterval,
            GetMultipliersForLevel,
            showDebugInfo
        );
        
        // 初始化 Player
        if (spawner.InitializePlayer())
        {
            player = spawner.Player;
            
            // 註冊 Player 到統一實體註冊表（通過 AttackSystem）
            if (player is IEntity playerEntity)
            {
                attackSystem.AddEntity(playerEntity);
            }
        }
    }
    
    /// <summary>
    /// 延遲生成初始實體
    /// </summary>
    private IEnumerator DelayedSpawnInitialEntities()
    {
        // 等待一幀確保所有系統已初始化
        yield return null;
        
        // 驗證 itemManager 是否準備好
        if (!IsItemManagerReady())
        {
            Debug.LogError("[EntityManager] itemManager 未準備好，無法生成實體！");
            yield break;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("[EntityManager] 開始生成實體（item mappings 已確定）");
        }
        
        // 生成所有實體
        spawner?.SpawnInitialEntities();
        
        // 訂閱所有活躍敵人的死亡事件（用於統計擊殺數）
        SubscribeToAllEnemyDeathEvents();
        
        // 觸發 Player 準備就緒事件
        OnPlayerReady?.Invoke();
        
        // 觸發完全初始化完成事件
        OnManagerInitialized?.Invoke();
    }
    
    /// <summary>
    /// 檢查 itemManager 是否準備好
    /// </summary>
    private bool IsItemManagerReady()
    {
        return itemManager != null && itemManager.ItemMappingDict != null && itemManager.ItemMappingDict.Count > 0;
    }
    

    private void OnDestroy()
    {
        eventManager?.Cleanup();
        
        // 取消危險等級事件監聽
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged -= OnDangerLevelChanged;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo || Player == null) return;

        // 顯示玩家位置
        Gizmos.color = Color.green;
        Vector3 playerPos = Player.transform.position;
        Gizmos.DrawWireSphere(playerPos, 1f);
        
        // 顯示patrol points
        DrawPatrolPoints();
    }
    
    /// <summary>
    /// 在Scene視圖中顯示patrol points
    /// </summary>
    private void DrawPatrolPoints()
    {
        var enemyDataList = dataLoader?.GetEntitiesByType(EntityDataLoader.EntityType.Enemy) ?? new List<EntityDataLoader.EntityData>();
        if (enemyDataList.Count == 0) return;
        
        for (int enemyIndex = 0; enemyIndex < enemyDataList.Count; enemyIndex++)
        {
            var enemyData = enemyDataList[enemyIndex];
            if (enemyData == null || enemyData.patrolPoints == null || enemyData.patrolPoints.Length == 0) continue;
            
            Vector3[] patrolPoints = enemyData.patrolPoints;
            Color enemyColor = Color.HSVToRGB((float)enemyIndex / enemyDataList.Count, 0.8f, 1f);
            
            Gizmos.color = enemyColor;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Vector3 pos = patrolPoints[i];
                float radius = i == 0 ? 0.5f : 0.3f;
                Gizmos.DrawWireSphere(pos, radius);
                
                Vector3 nextPos = i < patrolPoints.Length - 1 ? patrolPoints[i + 1] : patrolPoints[0];
                Gizmos.DrawLine(pos, nextPos);
                
#if UNITY_EDITOR
                Handles.color = Color.white;
                Handles.Label(pos + Vector3.up * 0.8f, $"E{enemyIndex + 1}P{i + 1}");
#endif
            }
        }
    }

    #endregion

    #region 事件處理

    /// <summary>
    /// 訂閱所有活躍敵人的死亡事件
    /// </summary>
    private void SubscribeToAllEnemyDeathEvents()
    {
        if (spawner == null) return;
        
        var allEnemies = spawner.SpawnedEnemies;
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                // 先取消訂閱（避免重複訂閱）
                enemy.OnEnemyDied -= HandleEnemyDied;
                // 再訂閱
                enemy.OnEnemyDied += HandleEnemyDied;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[EntityManager] Subscribed to {allEnemies.Count} enemies' death events");
        }
    }
    
    /// <summary>
    /// 處理敵人死亡
    /// </summary>
    private void HandleEnemyDied(Enemy deadEnemy)
    {
        if (deadEnemy == null) return;
        
        // 取消訂閱死亡事件
        deadEnemy.OnEnemyDied -= HandleEnemyDied;
        
        // 從統一實體註冊表移除
        if (deadEnemy is IEntity deadEnemyEntity)
        {
            attackSystem?.RemoveEntity(deadEnemyEntity);
        }
        
        // 通知 GameManager 註冊擊殺數
        GameManager.Instance?.RegisterEnemyKill();
    }
    
    /// <summary>
    /// 處理 Target 死亡
    /// </summary>
    private void HandleTargetDied(Target deadTarget)
    {
        if (deadTarget == null) return;
        
        // 從統一實體註冊表移除（通過 AttackSystem）
        if (deadTarget is IEntity deadTargetEntity)
        {
            attackSystem?.RemoveEntity(deadTargetEntity);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[EntityManager] Target died: {deadTarget.gameObject.name}");
        }
        
        // 注意：Target 死亡事件已由 WinConditionManager 直接監聽，不需要通過 GameManager
    }

    /// <summary>
    /// 處理 Target 到達逃亡點
    /// </summary>
    private void HandleTargetReachedEscapePoint(Target target)
    {
        if (target == null) return;
        
        if (showDebugInfo)
        {
            Debug.Log($"[EntityManager] Target reached escape point: {target.gameObject.name}");
        }
        
        // 注意：Target 到達逃亡點事件已由 WinConditionManager 直接監聽，不需要通過 GameManager
    }

    /// <summary>
    /// 當危險等級改變時調用
    /// </summary>
    private void OnDangerLevelChanged(DangerousManager.DangerLevel level)
    {
        UpdateAllEnemiesDangerLevel();
    }

    /// <summary>
    /// 根據當前危險等級更新所有敵人的屬性
    /// </summary>
    private void UpdateAllEnemiesDangerLevel()
    {
        if (dangerousManager == null) return;
        
        DangerousManager.DangerLevel currentLevel = dangerousManager.CurrentDangerLevelType;
        var multipliers = GetMultipliersForLevel(currentLevel);
        
        var allEnemies = GetAllActiveEnemies();
        
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.UpdateDangerLevelStats(
                    multipliers.viewRangeMultiplier,
                    multipliers.viewAngleMultiplier,
                    multipliers.speedMultiplier,
                    multipliers.damageReduction
                );
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[EntityManager] Updated all enemies for danger level {currentLevel}");
        }
    }

    /// <summary>
    /// 根據危險等級獲取對應的乘數
    /// </summary>
    private Game.EntityManager.EntitySpawner.DangerLevelMultipliers GetMultipliersForLevel(DangerousManager.DangerLevel level)
    {
        switch (level)
        {
            case DangerousManager.DangerLevel.Safe:
                return ConvertToSpawnerMultipliers(safeLevel);
            case DangerousManager.DangerLevel.Low:
                return ConvertToSpawnerMultipliers(lowLevel);
            case DangerousManager.DangerLevel.Medium:
                return ConvertToSpawnerMultipliers(mediumLevel);
            case DangerousManager.DangerLevel.High:
                return ConvertToSpawnerMultipliers(highLevel);
            case DangerousManager.DangerLevel.Critical:
                return ConvertToSpawnerMultipliers(criticalLevel);
            default:
                return ConvertToSpawnerMultipliers(safeLevel);
        }
    }
    
    /// <summary>
    /// 轉換 DangerLevelMultipliers 為 EntitySpawner.DangerLevelMultipliers
    /// </summary>
    private Game.EntityManager.EntitySpawner.DangerLevelMultipliers ConvertToSpawnerMultipliers(DangerLevelMultipliers multipliers)
    {
        return new Game.EntityManager.EntitySpawner.DangerLevelMultipliers
        {
            viewRangeMultiplier = multipliers.viewRangeMultiplier,
            viewAngleMultiplier = multipliers.viewAngleMultiplier,
            speedMultiplier = multipliers.speedMultiplier,
            damageReduction = multipliers.damageReduction
        };
    }

    #endregion

    #region 公共 API - 查詢

    /// <summary>
    /// 取得指定敵人的patrol points
    /// </summary>
    public Vector3[] GetEnemyPatrolPoints(int enemyIndex)
    {
        var enemyData = dataLoader?.GetEntityData(enemyIndex, EntityDataLoader.EntityType.Enemy);
        return enemyData?.patrolPoints ?? new Vector3[0];
    }
    
    /// <summary>
    /// 取得敵人數量（只計算 Enemy 類型）
    /// </summary>
    public int GetEnemyCount()
    {
        var enemyList = dataLoader?.GetEntitiesByType(EntityDataLoader.EntityType.Enemy);
        return enemyList?.Count ?? 0;
    }
    
    /// <summary>
    /// 獲取所有敵人的生成點資訊（用於除錯）
    /// </summary>
    public void LogAllEnemySpawnPoints()
    {
        if (!showDebugInfo) return;
        
        var enemyDataList = dataLoader?.GetEntitiesByType(EntityDataLoader.EntityType.Enemy) ?? new List<EntityDataLoader.EntityData>();
        
        Debug.Log("=== 所有敵人生成點資訊 ===");
        for (int i = 0; i < enemyDataList.Count; i++)
        {
            var data = enemyDataList[i];
            if (data != null && data.patrolPoints != null && data.patrolPoints.Length > 0)
            {
                string patrolInfo = string.Join(" -> ", data.patrolPoints.Select(p => $"({p.x:F1},{p.y:F1})"));
                string itemsInfo = data.itemNames.Count > 0 ? string.Join(", ", data.itemNames) : "None";
                Debug.Log($"敵人 {data.entityIndex} ({data.type}): 生成點 {data.patrolPoints[0]} | 物品: [{itemsInfo}] | 巡邏路線: {patrolInfo}");
            }
            else
            {
                Debug.LogWarning($"敵人 {i}: 沒有有效的資料");
            }
        }
        Debug.Log($"總共 {enemyDataList.Count} 個敵人");
    }

    /// <summary>
    /// 獲取活躍的 Enemy 數量
    /// </summary>
    private int GetActiveEnemyCount()
    {
        return CountEnemies(enemy => enemy != null && !enemy.IsDead);
    }
    
    /// <summary>
    /// 獲取死亡的 Enemy 數量
    /// </summary>
    private int GetDeadEnemyCount()
    {
        return CountEnemies(enemy => enemy != null && enemy.IsDead);
    }
    
    /// <summary>
    /// 統計符合條件的敵人數量（共用方法）
    /// </summary>
    private int CountEnemies(System.Func<Enemy, bool> predicate)
    {
        if (spawner == null) return 0;
        int count = 0;
        foreach (var enemy in spawner.SpawnedEnemies)
        {
            if (predicate(enemy))
            {
                count++;
            }
        }
        return count;
    }


    /// <summary>
    /// 獲取活躍的 Target 數量
    /// </summary>
    public int GetActiveTargetCount()
    {
        var targets = eventManager?.ActiveTargets;
        if (targets == null) return 0;
        return targets.Count(target => target != null && !target.IsDead);
    }

    /// <summary>
    /// 檢查是否所有 Target 都已死亡
    /// </summary>
    public bool AreAllTargetsDead()
    {
        var targets = eventManager?.ActiveTargets;
        if (targets == null || targets.Count == 0) return true;
        return !targets.Any(target => target != null && !target.IsDead);
    }

    /// <summary>
    /// 獲取可見實體數量（Enemy 和 Target）
    /// </summary>
    public int GetVisibleEntityCount()
    {
        if (enablePlayerDetection && playerDetection != null)
        {
            return playerDetection.VisibleEntityCount;
        }
        return ActiveEnemyCount + GetActiveTargetCount();
    }
    
    /// <summary>
    /// 獲取隱藏實體數量（Enemy 和 Target）
    /// </summary>
    public int GetHiddenEntityCount()
    {
        if (enablePlayerDetection && playerDetection != null)
        {
            return playerDetection.HiddenEntityCount;
        }
        return 0;
    }

    /// <summary>
    /// 獲取所有活躍敵人的列表
    /// </summary>
    public List<Enemy> GetAllActiveEnemies()
    {
        if (spawner == null) return new List<Enemy>();
        return spawner.SpawnedEnemies.Where(enemy => enemy != null && !enemy.IsDead).ToList();
    }

    /// <summary>
    /// 獲取所有活躍 Target 的列表
    /// </summary>
    public List<Target> GetAllActiveTargets()
    {
        return eventManager?.ActiveTargets ?? new List<Target>();
    }

    #endregion

    #region 公共 API - 實體管理

    /// <summary>
    /// 殺死所有 Enemy
    /// </summary>
    public void KillAllEnemies()
    {
        var allEnemies = GetAllActiveEnemies();
        foreach (var enemy in allEnemies)
        {
            enemy.Die();
        }
    }

    /// <summary>
    /// 重新生成所有敵人
    /// </summary>
    public void RespawnAllEnemies()
    {
        KillAllEnemies();
        StartCoroutine(RespawnAfterDelay());
    }

    /// <summary>
    /// 生成所有敵人（用於手動觸發）
    /// </summary>
    public void SpawnAllEnemies()
    {
        spawner?.SpawnInitialEntities();
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return null;
        spawner?.SpawnInitialEntities();
        Debug.Log($"EntityManager: Respawned {ActiveEnemyCount} enemies");
    }

    #endregion

    #region 公共 API - 警報和可見性

    /// <summary>
    /// 警報範圍內的所有敵人
    /// </summary>
    public void AlertNearbyEnemies(Vector2 position, float alertRange)
    {
        if (Player == null) return;

        float rangeSqr = alertRange * alertRange;
        
        // 檢查活躍的敵人
        var activeEnemies = GetAllActiveEnemies();
        int alertedCount = AlertEnemiesInList(activeEnemies, position, rangeSqr);

        if (showDebugInfo)
        {
            Debug.Log($"EntityManager: Alerted {alertedCount} enemies within {alertRange} units");
        }
    }

    private int AlertEnemiesInList(List<Enemy> enemyList, Vector2 position, float rangeSqr)
    {
        int count = 0;

        foreach (var enemy in enemyList)
        {
            if (enemy == null || enemy.IsDead) continue;

            float distSqr = ((Vector2)enemy.Position - position).sqrMagnitude;
            if (distSqr <= rangeSqr)
            {
                enemy.NotifyPlayerPosition(position);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 設置實體視覺化狀態（統一處理 Enemy 和 Target）
    /// </summary>
    public void SetEntityVisualization(IEntity entity, bool canVisualize)
    {
        if (entity == null) return;
        
        EntityType entityType = entity.GetEntityType();
        
        // 排除 Player
        if (entityType == EntityType.Player)
        {
            return;
        }
        
        if (entityType == EntityType.Enemy)
        {
            Enemy enemy = entity.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.SetCanVisualize(canVisualize);
            }
        }
        else if (entityType == EntityType.Target)
        {
            Target target = entity.gameObject.GetComponent<Target>();
            if (target != null)
            {
                target.SetCanVisualize(canVisualize);
            }
        }
    }
    
    /// <summary>
    /// 強制更新所有實體的可見性（Enemy 和 Target）
    /// </summary>
    public void ForceUpdateEntityVisibility()
    {
        if (enablePlayerDetection && playerDetection != null)
        {
            playerDetection.ForceUpdateAllEntities();
        }
    }

    #endregion

    #region 公共 API - 配置

    /// <summary>
    /// 設置玩家偵測系統
    /// </summary>
    public void SetPlayerDetection(bool enabled, bool autoRegister = true)
    {
        enablePlayerDetection = enabled;
        autoRegisterWithPlayerDetection = autoRegister;
        
        if (enabled && playerDetection == null)
        {
            InitializePlayerDetection();
        }
        
        Debug.Log($"[EntityManager] 玩家偵測系統設定 - 啟用: {enabled}, 自動註冊: {autoRegister}");
    }

    /// <summary>
    /// 獲取玩家偵測系統引用
    /// </summary>
    public PlayerDetection GetPlayerDetection()
    {
        return playerDetection;
    }

    #endregion

    #region 公共 API - 實體生成

    /// <summary>
    /// 生成 Enemy（公共 API）
    /// </summary>
    public void SpawnEnemy(Vector3 position, int enemyIndex = -1)
    {
        spawner?.SpawnEnemy(position, enemyIndex);
        
        // 訂閱新生成敵人的死亡事件
        if (spawner != null && spawner.SpawnedEnemies.Count > 0)
        {
            var lastEnemy = spawner.SpawnedEnemies[spawner.SpawnedEnemies.Count - 1];
            if (lastEnemy != null)
            {
                lastEnemy.OnEnemyDied -= HandleEnemyDied;
                lastEnemy.OnEnemyDied += HandleEnemyDied;
            }
        }
    }

    #endregion

    #region 初始化輔助方法

    /// <summary>
    /// 初始化玩家偵測系統
    /// </summary>
    private void InitializePlayerDetection()
    {
        if (!enablePlayerDetection) return;
        
        Player currentPlayer = player ?? spawner?.Player;
        if (currentPlayer != null)
        {
            playerDetection = currentPlayer.GetComponent<PlayerDetection>();
            if (playerDetection == null)
            {
                Debug.LogWarning("[EntityManager] 找不到 PlayerDetection 組件，玩家偵測功能將被停用");
                enablePlayerDetection = false;
            }
        }
    }

    /// <summary>
    /// 初始化危險等級管理系統
    /// </summary>
    private void InitializeDangerousManager()
    {
        dangerousManager = DangerousManager.Instance;
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelChanged;
        }
    }

    #endregion
}