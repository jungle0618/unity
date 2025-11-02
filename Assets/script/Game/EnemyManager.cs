using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
    [SerializeField] private int maxActiveEnemies = 36; // 最大數量 - 設定為所有敵人數量
    [SerializeField] private int poolSize = 50; // 最大 pool 大小 - 增加以容納所有敵人

    [Header("敵人武器設定")]
    [SerializeField] private GameObject[] enemyWeaponPrefabs; // 敵人可用的武器 Prefabs
    [SerializeField] private bool autoEquipWeapons = false; // 是否自動為敵人裝備武器

    [Header("Patrol Data 設定")]
    [SerializeField] private TextAsset patrolDataFile;


    [Header("效能優化")]
    [SerializeField] private float cullingDistance = 25f; // 最大距離
    [SerializeField] private float updateInterval = 0.2f; // 最大更新頻率
    [SerializeField] private int enemiesPerFrameUpdate = 3; // 最大每幀處理數量
    [SerializeField] private float aiUpdateInterval = 0.15f; // AI 更新間隔

    [Header("除錯資訊")]
    [SerializeField] private bool showDebugInfo = false;

    [Header("玩家偵測整合")]
    [SerializeField] private bool enablePlayerDetection = true; // 啟用玩家偵測系統
    [SerializeField] private bool autoRegisterWithPlayerDetection = true; // 自動註冊到玩家偵測系統

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

    // 玩家偵測系統引用
    private PlayerDetection playerDetection;

    #region Unity 生命週期

    private void Start()
    {
        // 載入patrol data
        LoadPatrolData();
        
        // 初始化玩家偵測系統
        InitializePlayerDetection();
        
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
                Handles.color = Color.white;
                Handles.Label(pos + Vector3.up * 0.8f, $"E{enemyIndex + 1}P{i + 1}");
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
            // 按照敵人索引順序排列，確保索引連續性
            var sortedEnemyIndices = enemyPatrolDict.Keys.OrderBy(x => x).ToList();
            
            for (int i = 0; i < sortedEnemyIndices.Count; i++)
            {
                int enemyIndex = sortedEnemyIndices[i];
                if (enemyPatrolDict.ContainsKey(enemyIndex))
                {
                    // 移除Vector3.zero的patrol points
                    List<Vector3> validPatrolPoints = enemyPatrolDict[enemyIndex].Where(p => p != Vector3.zero).ToList();
                    if (validPatrolPoints.Count > 0)
                    {
                        enemyPatrolData.Add(validPatrolPoints.ToArray());
                        Debug.Log($"EnemyManager: Loaded {validPatrolPoints.Count} patrol points for enemy {enemyIndex}");
                    }
                    else
                    {
                        // 如果沒有有效的patrol points，創建預設的
                        Vector3[] defaultPatrol = new Vector3[2];
                        defaultPatrol[0] = new Vector3(enemyIndex * 10f, 0f, 0f);
                        defaultPatrol[1] = new Vector3(enemyIndex * 10f + 5f, 0f, 0f);
                        enemyPatrolData.Add(defaultPatrol);
                        Debug.LogWarning($"EnemyManager: No valid patrol points for enemy {enemyIndex}, created default patrol");
                    }
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
    
    /// <summary>
    /// 獲取所有敵人的生成點資訊（用於除錯）
    /// </summary>
    public void LogAllEnemySpawnPoints()
    {
        Debug.Log("=== 所有敵人生成點資訊 ===");
        for (int i = 0; i < enemyPatrolData.Count; i++)
        {
            Vector3[] patrolPoints = GetEnemyPatrolPoints(i);
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                string patrolInfo = string.Join(" -> ", patrolPoints.Select(p => $"({p.x:F1},{p.y:F1})"));
                Debug.Log($"敵人 {i}: 生成點 {patrolPoints[0]} | 巡邏路線: {patrolInfo}");
            }
            else
            {
                Debug.LogWarning($"敵人 {i}: 沒有有效的巡邏點");
            }
        }
        Debug.Log($"總共 {enemyPatrolData.Count} 個敵人");
    }

    #endregion

    #region 初始化

    private void InitializeManager()
    {
        ValidateReferences();
        InitializePool();
        SubscribeToPlayerEvents();
        StartManagement();

        // 延遲生成初始敵人，確保池初始化完成
        StartCoroutine(DelayedSpawnInitialEnemies());
    }
    
    /// <summary>
    /// 延遲生成初始敵人（確保池初始化完成）
    /// </summary>
    private IEnumerator DelayedSpawnInitialEnemies()
    {
        // 等待池初始化完成（至少 0.2 秒，確保所有敵人 Awake/Start 都執行完）
        yield return new WaitForSeconds(0.2f);
        
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

        // 檢查敵人 prefab 是否有必要的組件
        Enemy enemyComponent = enemyPrefab.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogError("EnemyManager: Enemy prefab missing Enemy component!");
            enabled = false;
            return;
        }

        updateWait = new WaitForSeconds(updateInterval);
        aiUpdateWait = new WaitForSeconds(aiUpdateInterval);
        cachedPlayerPosition = player.position;
    }

    private void InitializePool()
    {
        // 使用協程來分批初始化，避免一次性創建太多物件導致卡頓
        StartCoroutine(InitializePoolCoroutine());
    }
    
    /// <summary>
    /// 協程初始化池（分批創建敵人）
    /// </summary>
    private IEnumerator InitializePoolCoroutine()
    {
        int enemiesCreated = 0;
        
        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledEnemy();
            enemiesCreated++;
            
            // 每創建 10 個敵人等待一幀，避免卡頓
            if (enemiesCreated % 10 == 0)
            {
                yield return null;
            }
        }
        
        // 等待所有敵人初始化完成（多等幾幀確保 Awake/Start 都執行完）
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log($"EnemyManager: Initialized pool with {enemyPool.Count} enemies");
    }

    private Enemy CreatePooledEnemy()
    {
        GameObject enemyGO = Instantiate(enemyPrefab);
        
        // 先啟用以觸發 Awake 和 Start，確保所有組件正確初始化
        enemyGO.SetActive(true);
        
        Enemy enemy = enemyGO.GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError("EnemyManager: Enemy prefab missing Enemy component!");
            Destroy(enemyGO);
            return null;
        }

        // 設定事件監聽（需要在 Awake 之後，因為 Awake 中會初始化組件）
        enemy.OnEnemyDied += HandleEnemyDied;

        // 先加入池，然後延遲禁用（確保 GetPooledEnemy 可以立即取到）
        enemyPool.Enqueue(enemy);
        
        // 等待一幀確保所有初始化完成（Awake 和 Start 都已執行）後再禁用
        StartCoroutine(DeactivateAfterFrame(enemyGO));

        return enemy;
    }
    
    /// <summary>
    /// 等待一幀後禁用敵人（用於確保初始化完成）
    /// </summary>
    private IEnumerator DeactivateAfterFrame(GameObject enemyGO)
    {
        yield return null; // 等待一幀，確保 Awake 和 Start 都執行完畢
        
        if (enemyGO != null)
        {
            enemyGO.SetActive(false);
        }
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
        if (enemy == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("EnemyManager: No enemy available in pool!");
            return;
        }

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
        
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError($"EnemyManager: No valid patrol points for enemy index {enemyIndex}");
            ReturnEnemyToPool(enemy);
            return;
        }
        
        // 檢查敵人組件是否已初始化（通過基類的公共屬性檢查）
        if (enemy.Movement == null || enemy.Detection == null)
        {
            Debug.LogError($"EnemyManager: Enemy components not initialized! Movement: {enemy.Movement != null}, Detection: {enemy.Detection != null}");
            ReturnEnemyToPool(enemy);
            return;
        }
        
        // 驗證並自動裝備武器（如果需要）
        if (enemy.ItemHolder != null)
        {
            if (enemy.ItemHolder.ItemCount == 0)
            {
                if (autoEquipWeapons && enemyWeaponPrefabs != null && enemyWeaponPrefabs.Length > 0)
                {
                    // 自動裝備隨機武器
                    GameObject weaponPrefab = enemyWeaponPrefabs[Random.Range(0, enemyWeaponPrefabs.Length)];
                    enemy.ItemHolder.EquipFromPrefab(weaponPrefab);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"EnemyManager: 自動為 {enemy.name} 裝備武器 {weaponPrefab.name}");
                    }
                }
                else if (!autoEquipWeapons)
                {
                    Debug.LogWarning($"EnemyManager: Enemy {enemy.name} has ItemHolder but no items assigned! 請在 Enemy Prefab 的 ItemHolder 中設定 Item Prefabs，或在 EnemyManager 啟用 Auto Equip Weapons。");
                }
            }
        }
        
        // 先設定 patrol locations
        enemy.SetPatrolLocations(patrolPoints);
        
        // 啟用敵人（會觸發 OnEnable，但不會觸發 Awake/Start，因為它們只在首次創建時執行）
        enemy.gameObject.SetActive(true);
        
        // 初始化敵人（這會將敵人移動到第一個 patrol point）
        // Enemy.Initialize() 會設定目標、位置和狀態
        if (enemy != null && enemy.gameObject.activeInHierarchy)
        {
            enemy.Initialize(player);
            
            if (showDebugInfo)
            {
                Debug.Log($"EnemyManager: Initialized enemy at {enemy.transform.position}");
            }
        }
        else
        {
            Debug.LogError($"EnemyManager: Failed to initialize enemy - enemy is null or not active!");
            ReturnEnemyToPool(enemy);
            return;
        }

        activeEnemies.Add(enemy);

        // 設定 AI 更新間隔（錯開更新時間以分散 CPU 負載）
        enemy.SetAIUpdateInterval(aiUpdateInterval + Random.Range(0f, aiUpdateInterval * 0.5f));
        
        // 註冊到玩家偵測系統
        if (enablePlayerDetection && autoRegisterWithPlayerDetection && playerDetection != null)
        {
            playerDetection.AddEnemy(enemy);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyManager: Spawned enemy at {enemy.transform.position} with {patrolPoints.Length} patrol points");
        }
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

        // 生成所有可用的敵人
        int enemiesToSpawn = Mathf.Min(maxActiveEnemies, enemyPatrolData.Count);
        
        Debug.Log($"EnemyManager: Attempting to spawn ALL {enemiesToSpawn} enemies from {enemyPatrolData.Count} available patrol data sets");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // 使用敵人索引 i，敵人會自動移動到對應 patrol data 的第一個位置
            SpawnEnemy(Vector3.zero, i); // position 參數會被忽略，因為敵人會移動到 patrol point
            
            if (showDebugInfo)
            {
                Vector3[] patrolPoints = GetEnemyPatrolPoints(i);
                Debug.Log($"EnemyManager: Spawning enemy {i} at {patrolPoints[0]} with {patrolPoints.Length} patrol points");
            }
        }

        Debug.Log($"EnemyManager: Successfully spawned ALL {activeEnemies.Count} enemies with patrol data from file");
        
        // 顯示所有敵人的生成點資訊
        LogAllEnemySpawnPoints();
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
        var ih = player.GetComponent<ItemHolder>();
        if (ih != null)
            ih.OnAttackPerformed += HandlePlayerAttack;
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (player == null) return;
        var ih = player.GetComponent<ItemHolder>();
        if (ih != null)
            ih.OnAttackPerformed -= HandlePlayerAttack;
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

        // 從玩家偵測系統移除
        if (enablePlayerDetection && playerDetection != null)
        {
            playerDetection.RemoveEnemy(deadEnemy);
        }

        // 除錯加除錯入除錯死除錯亡除錯列除錯表除錯（除錯用除錯於除錯統除錯計除錯）除錯
        if (!deadEnemies.Contains(deadEnemy))
        {
            deadEnemies.Add(deadEnemy);
        }
    }

    #endregion

    #region 敵人視覺化控制（由 PlayerDetection 觸發）

    /// <summary>
    /// 當敵人可見性改變時調用（由 PlayerDetection 調用）
    /// 處理敵人及其武器的所有視覺化組件（Renderer, SpriteRenderer, Visualizer）
    /// </summary>
    public void OnEnemyVisibilityChanged(Enemy enemy, bool isVisible)
    {
        if (enemy == null) return;
        
        // 禁用所有 Renderer 組件（包括 SpriteRenderer, MeshRenderer, LineRenderer 等）
        // 使用 GetComponentsInChildren 包含所有子物件（包括武器）
        Renderer[] allRenderers = enemy.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = isVisible;
            }
        }
        
        // 禁用 Visualizer 組件（控制 Gizmos 繪製等視覺化邏輯）
        BaseVisualizer visualizer = enemy.GetComponent<BaseVisualizer>();
        if (visualizer != null)
        {
            visualizer.enabled = isVisible;
        }
        
        // 根據可見性設定是否可以執行視覺化邏輯（會自動處理武器的 renderer 和視覺化狀態）
        enemy.SetCanVisualize(isVisible);
    }

    /// <summary>
    /// 設定單個敵人是否可以執行視覺化邏輯（只影響 Gizmos 等，不影響 SpriteRenderer）
    /// </summary>
    public void SetEnemyVisualization(Enemy enemy, bool canVisualize)
    {
        if (enemy == null) return;
        
        enemy.SetCanVisualize(canVisualize);
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

    #region 玩家偵測系統整合

    /// <summary>
    /// 初始化玩家偵測系統
    /// </summary>
    private void InitializePlayerDetection()
    {
        if (!enablePlayerDetection) return;
        
        // 查找玩家偵測組件
        if (player != null)
        {
            playerDetection = player.GetComponent<PlayerDetection>();
            if (playerDetection == null)
            {
                Debug.LogWarning("[EnemyManager] 找不到 PlayerDetection 組件，玩家偵測功能將被停用");
                enablePlayerDetection = false;
                return;
            }
        }
        else
        {
            Debug.LogWarning("[EnemyManager] 找不到玩家引用，玩家偵測功能將被停用");
            enablePlayerDetection = false;
            return;
        }
        
        Debug.Log("[EnemyManager] 玩家偵測系統初始化完成");
    }
    
    /// <summary>
    /// 設定玩家偵測系統
    /// </summary>
    public void SetPlayerDetection(bool enabled, bool autoRegister = true)
    {
        enablePlayerDetection = enabled;
        autoRegisterWithPlayerDetection = autoRegister;
        
        if (enabled && playerDetection == null)
        {
            InitializePlayerDetection();
        }
        
        Debug.Log($"[EnemyManager] 玩家偵測系統設定 - 啟用: {enabled}, 自動註冊: {autoRegister}");
    }
    
    /// <summary>
    /// 獲取玩家偵測系統引用
    /// </summary>
    public PlayerDetection GetPlayerDetection()
    {
        return playerDetection;
    }
    
    /// <summary>
    /// 強制更新所有敵人的可見性
    /// </summary>
    public void ForceUpdateEnemyVisibility()
    {
        if (enablePlayerDetection && playerDetection != null)
        {
            playerDetection.ForceUpdateAllEnemies();
        }
    }
    
    /// <summary>
    /// 獲取可見敵人數量
    /// </summary>
    public int GetVisibleEnemyCount()
    {
        if (enablePlayerDetection && playerDetection != null)
        {
            return playerDetection.VisibleEnemyCount;
        }
        return activeEnemies.Count; // 如果沒有偵測系統，返回所有活躍敵人
    }
    
    /// <summary>
    /// 獲取隱藏敵人數量
    /// </summary>
    public int GetHiddenEnemyCount()
    {
        if (enablePlayerDetection && playerDetection != null)
        {
            return playerDetection.HiddenEnemyCount;
        }
        return 0; // 如果沒有偵測系統，沒有隱藏敵人
    }
    
    /// <summary>
    /// 重新生成所有敵人（用於除錯或重新開始）
    /// </summary>
    public void RespawnAllEnemies()
    {
        // 清除所有現有敵人
        KillAllEnemies();
        
        // 等待一幀讓死亡事件處理完成
        StartCoroutine(RespawnAfterDelay());
    }
    
    /// <summary>
    /// 生成所有敵人（用於手動觸發）
    /// </summary>
    public void SpawnAllEnemies()
    {
        SpawnInitialEnemies();
    }
    
    private IEnumerator RespawnAfterDelay()
    {
        yield return null; // 等待一幀
        
        // 重新生成敵人
        SpawnInitialEnemies();
        
        Debug.Log($"EnemyManager: Respawned {activeEnemies.Count} enemies");
    }

    #endregion
}