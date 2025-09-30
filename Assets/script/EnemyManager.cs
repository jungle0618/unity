using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// EnemyManager 類別：專注於敵人的整體管理 (優化版本)
/// 職責：生成、回收、性能優化、全域控制
/// 優化：降低 CPU 和 RAM 消耗，減少 Update 頻率
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("敵人設定")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int maxActiveEnemies = 15; // 降低數量
    [SerializeField] private int poolSize = 30; // 降低 pool 大小


    [Header("性能優化")]
    [SerializeField] private float cullingDistance = 25f; // 降低距離
    [SerializeField] private float updateInterval = 0.2f; // 降低更新頻率
    [SerializeField] private int enemiesPerFrameUpdate = 3; // 降低每幀處理數量
    [SerializeField] private float aiUpdateInterval = 0.15f; // AI 更新間隔

    [Header("除錯資訊")]
    [SerializeField] private bool showDebugInfo = false;

    // 敵人管理 - 使用 HashSet 提高查找效率
    private Queue<Enemy> enemyPool = new Queue<Enemy>();
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private HashSet<Enemy> culledEnemies = new HashSet<Enemy>();
    private List<Enemy> deadEnemies = new List<Enemy>(); // 保持 List 用於統計

    // 性能優化
    private Coroutine managementCoroutine;
    private WaitForSeconds updateWait;
    private WaitForSeconds aiUpdateWait;

    // 批次處理
    private int currentUpdateIndex = 0;
    private List<Enemy> activeEnemiesList = new List<Enemy>(); // 用於批次處理的暫存 List

    // 距離計算優化 - 快取玩家位置
    private Vector3 cachedPlayerPosition;
    private float playerPositionUpdateTime = 0f;
    private const float PLAYER_POSITION_UPDATE_INTERVAL = 0.1f;

    // 統計資訊 - 使用屬性避免每次重新計算
    public int ActiveEnemyCount => activeEnemies.Count;
    public int PooledEnemyCount => enemyPool.Count;
    public int DeadEnemyCount => deadEnemies.Count;
    public int TotalEnemyCount => ActiveEnemyCount + PooledEnemyCount + culledEnemies.Count + DeadEnemyCount;

    #region Unity 生命週期

    private void Start()
    {
        InitializeManager();
    }

    private void Update()
    {
        // 只更新快取的玩家位置
        UpdateCachedPlayerPosition();
    }

    private void OnDestroy()
    {
        StopManagement();
        UnsubscribeFromPlayerEvents();

        // 清理事件訂閱
        foreach (var enemy in enemyPool)
        {
            if (enemy != null)
                enemy.OnEnemyDied -= HandleEnemyDied;
        }
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                enemy.OnEnemyDied -= HandleEnemyDied;
        }
        foreach (var enemy in culledEnemies)
        {
            if (enemy != null)
                enemy.OnEnemyDied -= HandleEnemyDied;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo || player == null) return;

        // 顯示剔除範圍
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cachedPlayerPosition != Vector3.zero ? cachedPlayerPosition : player.position, cullingDistance);
    }

    #endregion

    #region 初始化

    private void InitializeManager()
    {
        ValidateReferences();
        InitializePool();
        SubscribeToPlayerEvents();
        StartManagement();

        // 初始生成敵人
        SpawnInitialEnemies();
    }

    private void ValidateReferences()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemyManager: Enemy prefab is not assigned!");
            enabled = false;
            return;
        }

        if (player == null)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            if (player == null)
            {
                Debug.LogError("EnemyManager: Player reference not found!");
                enabled = false;
                return;
            }
        }

        updateWait = new WaitForSeconds(updateInterval);
        aiUpdateWait = new WaitForSeconds(aiUpdateInterval);
        cachedPlayerPosition = player.position;
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledEnemy();
        }

        Debug.Log($"EnemyManager: Initialized pool with {poolSize} enemies");
    }

    private Enemy CreatePooledEnemy()
    {
        GameObject enemyGO = Instantiate(enemyPrefab);
        Enemy enemy = enemyGO.GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError("EnemyManager: Enemy prefab missing Enemy component!");
            Destroy(enemyGO);
            return null;
        }

        // 設定事件監聽
        enemy.OnEnemyDied += HandleEnemyDied;


        // 暫時停用
        enemyGO.SetActive(false);
        enemyPool.Enqueue(enemy);

        return enemy;
    }



    #endregion

    #region 敵人生成與回收

    public void SpawnEnemy(Vector3 position)
    {
        if (activeEnemies.Count >= maxActiveEnemies)
        {
            if (showDebugInfo)
                Debug.LogWarning("EnemyManager: Reached maximum active enemies limit");
            return;
        }

        Enemy enemy = GetPooledEnemy();
        if (enemy == null) return;

        // 設置位置並初始化
        enemy.transform.position = position;
        enemy.gameObject.SetActive(true);
        enemy.Initialize(player);

        activeEnemies.Add(enemy);

        // 設定 AI 更新間隔（錯開更新時間以分散 CPU 負載）
        enemy.SetAIUpdateInterval(aiUpdateInterval + Random.Range(0f, aiUpdateInterval * 0.5f));
    }

    private Enemy GetPooledEnemy()
    {
        if (enemyPool.Count > 0)
        {
            return enemyPool.Dequeue();
        }

        // 池子空了，創建新的（如果允許的話）
        if (TotalEnemyCount < poolSize * 1.5f) // 降低擴展倍數
        {
            return CreatePooledEnemy();
        }

        return null;
    }

    private void ReturnEnemyToPool(Enemy enemy)
    {
        if (enemy == null) return;

        activeEnemies.Remove(enemy);
        culledEnemies.Remove(enemy);

        enemy.gameObject.SetActive(false);
        enemyPool.Enqueue(enemy);
    }

    private void SpawnInitialEnemies()
    {
        if (SpawnPointManager.Instance == null || !SpawnPointManager.Instance.HasValidSpawnPoints())
        {
            Debug.LogError("EnemyManager: SpawnPointManager not found or has no valid spawn points!");
            return;
        }

        var spawnPoints = SpawnPointManager.Instance.GetAllSpawnPoints();
        int enemiesToSpawn = Mathf.Min(maxActiveEnemies, spawnPoints.Length, poolSize);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 spawnPos = spawnPoints[i].position;
            SpawnEnemy(spawnPos);
        }

        Debug.Log($"EnemyManager: Spawned {activeEnemies.Count} enemies from {spawnPoints.Length} spawn points");
    }

    #endregion

    #region 性能管理

    private void UpdateCachedPlayerPosition()
    {
        if (Time.time - playerPositionUpdateTime >= PLAYER_POSITION_UPDATE_INTERVAL)
        {
            cachedPlayerPosition = player.position;
            playerPositionUpdateTime = Time.time;
        }
    }

    private void StartManagement()
    {
        if (managementCoroutine != null)
        {
            StopCoroutine(managementCoroutine);
        }

        managementCoroutine = StartCoroutine(ManagementLoop());
    }

    private void StopManagement()
    {
        if (managementCoroutine != null)
        {
            StopCoroutine(managementCoroutine);
            managementCoroutine = null;
        }
    }

    private IEnumerator ManagementLoop()
    {
        while (enabled)
        {
            yield return updateWait;

            UpdateEnemyCullingOptimized();

            if (showDebugInfo)
            {
                UpdateDebugInfo();
            }
        }
    }

    private void UpdateEnemyCullingOptimized()
    {
        // 批次處理：每次只處理部分敵人以分散 CPU 負載
        activeEnemiesList.Clear();
        activeEnemiesList.AddRange(activeEnemies);

        int enemiesToProcess = Mathf.Min(enemiesPerFrameUpdate, activeEnemiesList.Count);
        float cullingDistanceSqr = cullingDistance * cullingDistance; // 使用平方距離避免開根號

        // 處理活躍敵人
        for (int i = 0; i < enemiesToProcess; i++)
        {
            if (currentUpdateIndex >= activeEnemiesList.Count)
                currentUpdateIndex = 0;

            if (currentUpdateIndex < activeEnemiesList.Count)
            {
                Enemy enemy = activeEnemiesList[currentUpdateIndex];
                if (enemy == null || enemy.IsDead)
                {
                    activeEnemies.Remove(enemy);
                }
                else
                {
                    float distanceSqr = (enemy.Position - (Vector2)cachedPlayerPosition).sqrMagnitude;

                    if (distanceSqr > cullingDistanceSqr)
                    {
                        // 將敵人移到剔除列表
                        activeEnemies.Remove(enemy);
                        culledEnemies.Add(enemy);
                        enemy.gameObject.SetActive(false);
                    }
                }
                currentUpdateIndex++;
            }
        }

        // 簡化的剔除敵人重新激活檢查（降低頻率）
        if (Time.frameCount % 10 == 0) // 每 10 幀檢查一次
        {
            CheckCulledEnemiesForReactivation(cullingDistanceSqr);
        }
    }

    private void CheckCulledEnemiesForReactivation(float cullingDistanceSqr)
    {
        var culledList = new List<Enemy>(culledEnemies);

        foreach (var enemy in culledList)
        {
            if (enemy == null || enemy.IsDead)
            {
                culledEnemies.Remove(enemy);
                continue;
            }

            float distanceSqr = (enemy.Position - (Vector2)cachedPlayerPosition).sqrMagnitude;

            if (distanceSqr <= cullingDistanceSqr && activeEnemies.Count < maxActiveEnemies)
            {
                // 重新激活敵人
                culledEnemies.Remove(enemy);
                activeEnemies.Add(enemy);
                enemy.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    #region 事件處理

    private void SubscribeToPlayerEvents()
    {
        if (player == null) return;
        var wh = player.GetComponent<WeaponHolder>();
        if (wh != null)
            wh.OnAttackPerformed += HandlePlayerAttack;
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (player == null) return;
        var wh = player.GetComponent<WeaponHolder>();
        if (wh != null)
            wh.OnAttackPerformed -= HandlePlayerAttack;
    }

    private void HandlePlayerAttack(Vector2 attackCenter, float attackRange, GameObject attacker)
    {
        float rangeSqr = attackRange * attackRange;
        CheckEnemiesInAttackRange(activeEnemies, attackCenter, rangeSqr, attacker);

        // 降低對剔除敵人的檢查頻率
        if (Time.frameCount % 3 == 0)
        {
            CheckEnemiesInAttackRange(culledEnemies, attackCenter, rangeSqr, attacker);
        }
    }

    private void CheckEnemiesInAttackRange(HashSet<Enemy> enemySet, Vector2 attackCenter, float rangeSqr, GameObject attacker)
    {
        var enemyList = new List<Enemy>(enemySet);

        foreach (var enemy in enemyList)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (enemy.gameObject == attacker) continue;

            float distSqr = ((Vector2)enemy.Position - attackCenter).sqrMagnitude;
            if (distSqr <= rangeSqr)
                enemy.Die();
        }
    }

    private void HandleEnemyDied(Enemy deadEnemy)
    {
        // 敵人死亡後永久移除，不重生
        RemoveDeadEnemy(deadEnemy);

        if (showDebugInfo)
            Debug.Log($"EnemyManager: Enemy died. Remaining enemies: {activeEnemies.Count + culledEnemies.Count}");
    }

    private void RemoveDeadEnemy(Enemy deadEnemy)
    {
        if (deadEnemy == null) return;

        // 從所有集合中移除
        activeEnemies.Remove(deadEnemy);
        culledEnemies.Remove(deadEnemy);

        // 加入死亡列表（用於統計）
        if (!deadEnemies.Contains(deadEnemy))
        {
            deadEnemies.Add(deadEnemy);
        }
    }

    #endregion

    #region 公共 API

    public void PauseAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            enemy.gameObject.SetActive(false);
        }
    }

    public void ResumeAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            enemy.gameObject.SetActive(true);
        }
    }

    public void KillAllEnemies()
    {
        var allEnemies = new List<Enemy>(activeEnemies);
        allEnemies.AddRange(culledEnemies);

        foreach (var enemy in allEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.Die();
            }
        }
    }

    public int GetRemainingEnemyCount()
    {
        return activeEnemies.Count + culledEnemies.Count;
    }

    public bool HasLivingEnemies()
    {
        return GetRemainingEnemyCount() > 0;
    }

    public void SetMaxActiveEnemies(int newMax)
    {
        maxActiveEnemies = Mathf.Max(1, newMax);
    }

    #endregion

    #region 除錯資訊

    private void UpdateDebugInfo()
    {
        if (showDebugInfo && Time.frameCount % 60 == 0) // 降低日誌頻率
        {
            Debug.Log($"EnemyManager - Active: {activeEnemies.Count}, Culled: {culledEnemies.Count}, " +
                     $"Dead: {deadEnemies.Count}, Pooled: {enemyPool.Count}");
        }
    }

    #endregion
}