using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
/// <summary>
/// EnemyManager 類別：專注於敵人的整體管理 (優化版本)
/// 職責：生成、回收、效能優化、全域控制
/// 優化：降低 CPU 和 RAM 消耗，減少 Update 頻率
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("敵人設定")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int maxActiveEnemies = 15; // 最大數量
    [SerializeField] private int poolSize = 30; // 最大 pool 大小

    [Header("Patrol Data 設定")]
    [SerializeField] private TextAsset patrolDataFile;


    [Header("效能優化")]
    [SerializeField] private float cullingDistance = 25f; // 最大距離
    [SerializeField] private float updateInterval = 0.2f; // 最大更新頻率
    [SerializeField] private int enemiesPerFrameUpdate = 3; // 最大每幀處理數量
    [SerializeField] private float aiUpdateInterval = 0.15f; // AI 更新間隔

    [Header("除錯資訊")]
    [SerializeField] private bool showDebugInfo = false;

    // 敵人管理 - 使用 HashSet 提升查找效率
    private Queue<Enemy> enemyPool = new Queue<Enemy>();
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    
    // Patrol Data 儲存
    private List<Vector3[]> enemyPatrolData = new List<Vector3[]>();
    private HashSet<Enemy> culledEnemies = new HashSet<Enemy>();
    private List<Enemy> deadEnemies = new List<Enemy>(); // 保持 List 用於統計

    // 效能優化
    private Coroutine managementCoroutine;
    private WaitForSeconds updateWait;
    private WaitForSeconds aiUpdateWait;

    // 除錯批除錯次除錯處除錯理除錯
    private int currentUpdateIndex = 0;
    private List<Enemy> activeEnemiesList = new List<Enemy>(); // 除錯用除錯於除錯批除錯次除錯處除錯理除錯的除錯暫除錯存除錯 除錯L除錯i除錯s除錯t除錯

    // 除錯距除錯離除錯計除錯算除錯優除錯化除錯 除錯-除錯 除錯快除錯取除錯玩除錯家除錯位除錯置除錯
    private Vector3 cachedPlayerPosition;
    private float playerPositionUpdateTime = 0f;
    private const float PLAYER_POSITION_UPDATE_INTERVAL = 0.1f;

    // 除錯統除錯計除錯資除錯訊除錯 除錯-除錯 除錯使除錯用除錯屬除錯性除錯避除錯免除錯每除錯次除錯重除錯新除錯計除錯算除錯
    public int ActiveEnemyCount => activeEnemies.Count;
    public int PooledEnemyCount => enemyPool.Count;
    public int DeadEnemyCount => deadEnemies.Count;
    public int TotalEnemyCount => ActiveEnemyCount + PooledEnemyCount + culledEnemies.Count + DeadEnemyCount;

    #region Unity 生命週期

    private void Start()
    {
        // 載入patrol data
        LoadPatrolData();
        
        InitializeManager();
    }

    private void Update()
    {
        UpdateCachedPlayerPosition();
    }

    private void OnDestroy()
    {
        StopManagement();
        UnsubscribeFromPlayerEvents();

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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cachedPlayerPosition != Vector3.zero ? cachedPlayerPosition : player.position, cullingDistance);
        
        // 顯示patrol points
        DrawPatrolPoints();
    }
    
    /// <summary>
    /// 在Scene視圖中顯示patrol points
    /// </summary>
    private void DrawPatrolPoints()
    {
        if (enemyPatrolData == null || enemyPatrolData.Count == 0) return;
        
        for (int enemyIndex = 0; enemyIndex < enemyPatrolData.Count; enemyIndex++)
        {
            Vector3[] patrolPoints = enemyPatrolData[enemyIndex];
            if (patrolPoints == null) continue;
            
            // 為每個敵人使用不同的顏色
            Color enemyColor = Color.HSVToRGB((float)enemyIndex / enemyPatrolData.Count, 0.8f, 1f);
            
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Vector3 pos = patrolPoints[i];
                
                // 第一個位置用較大的圓圈
                if (i == 0)
                {
                    Gizmos.color = enemyColor;
                    Gizmos.DrawWireSphere(pos, 0.5f);
                }
                else
                {
                    Gizmos.color = enemyColor;
                    Gizmos.DrawWireSphere(pos, 0.3f);
                }
                
                // 繪製連線
                if (i < patrolPoints.Length - 1)
                {
                    Gizmos.color = enemyColor;
                    Gizmos.DrawLine(pos, patrolPoints[i + 1]);
                }
                else
                {
                    // 最後一個點連回第一個點
                    Gizmos.color = enemyColor;
                    Gizmos.DrawLine(pos, patrolPoints[0]);
                }
                
                // 顯示編號
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(pos + Vector3.up * 0.8f, $"E{enemyIndex + 1}P{i + 1}");
#endif
            }
        }
    }

    #endregion

    #region Patrol Data 管理

    /// <summary>
    /// 從TextAsset載入所有敵人的patrol points
    /// </summary>
    private void LoadPatrolData()
    {
        enemyPatrolData.Clear();
        
        if (patrolDataFile == null)
        {
            Debug.LogError("EnemyManager: Patrol data file (TextAsset) is not assigned! Please assign the patroldata.txt file in the inspector.");
            CreateDefaultPatrolData();
            return;
        }
        
        try
        {
            string[] lines = patrolDataFile.text.Split('\n');
            Dictionary<int, List<Vector3>> enemyPatrolDict = new Dictionary<int, List<Vector3>>();
            
            foreach (string line in lines)
            {
                // 跳過註釋行和空行
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                    
                string[] parts = line.Split(',');
                if (parts.Length >= 5)
                {
                    int enemyIndex = int.Parse(parts[0].Trim());
                    int patrolIndex = int.Parse(parts[1].Trim());
                    float x = float.Parse(parts[2].Trim());
                    float y = float.Parse(parts[3].Trim());
                    float z = float.Parse(parts[4].Trim());
                    Debug.Log($"EnemyManager: Loading patrol data for enemy {enemyIndex} at patrol index {patrolIndex} with position ({x}, {y}, {z})");
                    if (!enemyPatrolDict.ContainsKey(enemyIndex))
                    {
                        enemyPatrolDict[enemyIndex] = new List<Vector3>();
                    }
                    
                    // 確保patrol points按順序排列
                    while (enemyPatrolDict[enemyIndex].Count <= patrolIndex)
                    {
                        enemyPatrolDict[enemyIndex].Add(Vector3.zero);
                    }
                    
                    enemyPatrolDict[enemyIndex][patrolIndex] = new Vector3(x, y, z);
                }
            }
            
            // 轉換為陣列格式，移除空的patrol points
            int maxEnemyIndex = enemyPatrolDict.Keys.Count > 0 ? enemyPatrolDict.Keys.Max() : -1;
            for (int i = 0; i <= maxEnemyIndex; i++)
            {
                if (enemyPatrolDict.ContainsKey(i))
                {
                    // 移除Vector3.zero的patrol points
                    List<Vector3> validPatrolPoints = enemyPatrolDict[i].Where(p => p != Vector3.zero).ToList();
                    enemyPatrolData.Add(validPatrolPoints.ToArray());
                }
                else
                {
                    // 如果某個敵人沒有patrol data，創建預設的
                    Vector3[] defaultPatrol = new Vector3[3];
                    for (int j = 0; j < 3; j++)
                    {
                        defaultPatrol[j] = new Vector3(i * 10f + j * 3f, 0f, 0f);
                    }
                    enemyPatrolData.Add(defaultPatrol);
                }
            }
            
            Debug.Log($"Loaded patrol data for {enemyPatrolData.Count} enemies from TextAsset: {patrolDataFile.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load patrol data: {e.Message}");
            CreateDefaultPatrolData();
        }
    }
    
    /// <summary>
    /// 創建預設的patrol data
    /// </summary>
    private void CreateDefaultPatrolData()
    {
        enemyPatrolData.Clear();
        
        // 為每個敵人創建不同數量的patrol points
        for (int enemyIndex = 0; enemyIndex < maxActiveEnemies; enemyIndex++)
        {
            // 每個敵人有不同數量的patrol points（2-5個）
            int patrolCount = 2 + (enemyIndex % 4); // 2, 3, 4, 5個patrol points
            
            Vector3[] patrolPoints = new Vector3[patrolCount];
            for (int patrolIndex = 0; patrolIndex < patrolCount; patrolIndex++)
            {
                float x = enemyIndex * 10f + patrolIndex * 3f;
                float y = 0f;
                float z = 0f;
                patrolPoints[patrolIndex] = new Vector3(x, y, z);
            }
            
            enemyPatrolData.Add(patrolPoints);
        }
        
        Debug.Log($"Created default patrol data for {enemyPatrolData.Count} enemies");
    }
    
    /// <summary>
    /// 取得指定敵人的patrol points
    /// </summary>
    public Vector3[] GetEnemyPatrolPoints(int enemyIndex)
    {
        if (enemyIndex >= 0 && enemyIndex < enemyPatrolData.Count)
        {
            return enemyPatrolData[enemyIndex];
        }
        
        Debug.LogWarning($"Invalid enemy index: {enemyIndex}");
        return new Vector3[0];
    }
    
    /// <summary>
    /// 取得敵人數量
    /// </summary>
    public int GetEnemyCount()
    {
        return enemyPatrolData.Count;
    }

    #endregion

    #region 初始化

    private void InitializeManager()
    {
        ValidateReferences();
        InitializePool();
        SubscribeToPlayerEvents();
        StartManagement();

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

        // 除錯設除錯定除錯事除錯件除錯監除錯聽除錯
        enemy.OnEnemyDied += HandleEnemyDied;


        // 除錯暫除錯時除錯停除錯用除錯
        enemyGO.SetActive(false);
        enemyPool.Enqueue(enemy);

        return enemy;
    }



    #endregion

    #region 敵人生成與回收

    public void SpawnEnemy(Vector3 position, int enemyIndex = -1)
    {
        if (activeEnemies.Count >= maxActiveEnemies)
        {
            if (showDebugInfo)
                Debug.LogWarning("EnemyManager: Reached maximum active enemies limit");
            return;
        }

        Enemy enemy = GetPooledEnemy();
        if (enemy == null) return;

        // 設定位置並初始化
        enemy.transform.position = position;
        enemy.gameObject.SetActive(true);
        
        // 分配patrol points
        Vector3[] patrolPoints;
        if (enemyIndex >= 0 && enemyIndex < enemyPatrolData.Count)
        {
            // 使用指定敵人的patrol points
            patrolPoints = GetEnemyPatrolPoints(enemyIndex);
        }
        else
        {
            // 使用隨機敵人的patrol points
            int randomIndex = Random.Range(0, enemyPatrolData.Count);
            patrolPoints = GetEnemyPatrolPoints(randomIndex);
        }
        Debug.Log($"EnemyManager: Enemy index: [{string.Join(", ", patrolPoints)}]");
        enemy.SetPatrolLocations(patrolPoints);
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

        // 除錯池除錯子除錯空除錯了除錯，除錯創除錯建除錯新除錯的除錯（除錯如除錯果除錯允除錯許除錯的除錯話除錯）除錯
        if (TotalEnemyCount < poolSize * 1.5f) // 除錯降除錯低除錯擴除錯展除錯倍除錯數除錯
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
        if (enemyPatrolData.Count == 0)
        {
            Debug.LogError("EnemyManager: No patrol data loaded!");
            return;
        }

        // 使用預設的spawn位置，並為每個敵人分配對應的patrol points
        Vector3[] defaultSpawnPositions = {
            new Vector3(0, 0, 0),
            new Vector3(5, 0, 0),
            new Vector3(-5, 0, 0),
            new Vector3(0, 5, 0),
            new Vector3(0, -5, 0)
        };
        
        int enemiesToSpawn = Mathf.Min(maxActiveEnemies, defaultSpawnPositions.Length, enemyPatrolData.Count);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy(defaultSpawnPositions[i], i); // 傳入敵人索引
        }

        Debug.Log($"EnemyManager: Spawned {activeEnemies.Count} enemies with patrol data from file");
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
        // 除錯批除錯次除錯處除錯理除錯：除錯每除錯次除錯只除錯處除錯理除錯部除錯分除錯敵除錯人除錯以除錯分除錯散除錯 除錯C除錯P除錯U除錯 除錯負除錯載除錯
        activeEnemiesList.Clear();
        activeEnemiesList.AddRange(activeEnemies);

        int enemiesToProcess = Mathf.Min(enemiesPerFrameUpdate, activeEnemiesList.Count);
        float cullingDistanceSqr = cullingDistance * cullingDistance; // 除錯使除錯用除錯平除錯方除錯距除錯離除錯避除錯免除錯開除錯根除錯號除錯

        // 除錯處除錯理除錯活除錯躍除錯敵除錯人除錯
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
                        // 除錯將除錯敵除錯人除錯移除錯到除錯剔除錯除除錯列除錯表除錯
                        activeEnemies.Remove(enemy);
                        culledEnemies.Add(enemy);
                        enemy.gameObject.SetActive(false);
                    }
                }
                currentUpdateIndex++;
            }
        }

        // 除錯簡除錯化除錯的除錯剔除錯除除錯敵除錯人除錯重除錯新除錯激除錯活除錯檢除錯查除錯（除錯降除錯低除錯頻除錯率除錯）除錯
        if (Time.frameCount % 10 == 0) // 除錯每除錯 除錯1除錯0除錯 除錯幀除錯檢除錯查除錯一除錯次除錯
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
                // 除錯重除錯新除錯激除錯活除錯敵除錯人除錯
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

        // 除錯降除錯低除錯對除錯剔除錯除除錯敵除錯人除錯的除錯檢除錯查除錯頻除錯率除錯
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
        // 除錯敵除錯人除錯死除錯亡除錯後除錯永除錯久除錯移除錯除除錯，除錯不除錯重除錯生除錯
        RemoveDeadEnemy(deadEnemy);

        if (showDebugInfo)
            Debug.Log($"EnemyManager: Enemy died. Remaining enemies: {activeEnemies.Count + culledEnemies.Count}");
    }

    private void RemoveDeadEnemy(Enemy deadEnemy)
    {
        if (deadEnemy == null) return;

        // 除錯從除錯所除錯有除錯集除錯合除錯中除錯移除錯除除錯
        activeEnemies.Remove(deadEnemy);
        culledEnemies.Remove(deadEnemy);

        // 除錯加除錯入除錯死除錯亡除錯列除錯表除錯（除錯用除錯於除錯統除錯計除錯）除錯
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

    /// <summary>
    /// 設定所有活躍敵人的FOV倍數
    /// </summary>
    public void SetAllEnemiesFovMultiplier(float multiplier)
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.SetFovMultiplier(multiplier);
            }
        }
    }

    /// <summary>
    /// 設定所有活躍敵人的移動速度倍數
    /// </summary>
    public void SetAllEnemiesSpeedMultiplier(float multiplier)
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.SetSpeedMultiplier(multiplier);
            }
        }
    }

    /// <summary>
    /// 設定所有活躍敵人的傷害減少
    /// </summary>
    public void SetAllEnemiesDamageReduction(float reduction)
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.SetDamageReduction(reduction);
            }
        }
    }

    /// <summary>
    /// 獲取所有活躍敵人的列表（用於外部調整）
    /// </summary>
    public List<Enemy> GetAllActiveEnemies()
    {
        return new List<Enemy>(activeEnemies);
    }

    #endregion

    #region 除錯資訊

    private void UpdateDebugInfo()
    {
        if (showDebugInfo && Time.frameCount % 60 == 0) // 除錯降除錯低除錯日除錯誌除錯頻除錯率除錯
        {
            Debug.Log($"EnemyManager - Active: {activeEnemies.Count}, Culled: {culledEnemies.Count}, " +
                     $"Dead: {deadEnemies.Count}, Pooled: {enemyPool.Count}");
        }
    }

    #endregion
}