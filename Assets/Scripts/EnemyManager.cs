using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// EnemyManager ???O?G?M?`???H???????z (?u?????)
/// ??d?G????B?^???B????u??B??????
/// ?u??G???C CPU ?M RAM ????A??? Update ?W?v
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("??H?]?w")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int maxActiveEnemies = 15; // ???C??q
    [SerializeField] private int poolSize = 30; // ???C pool ?j?p


    [Header("????u??")]
    [SerializeField] private float cullingDistance = 25f; // ???C?Z??
    [SerializeField] private float updateInterval = 0.2f; // ???C??s?W?v
    [SerializeField] private int enemiesPerFrameUpdate = 3; // ???C?C?V?B?z??q
    [SerializeField] private float aiUpdateInterval = 0.15f; // AI ??s???j

    [Header("??????T")]
    [SerializeField] private bool showDebugInfo = false;

    // ??H??z - ??? HashSet ?????d???v
    private Queue<Enemy> enemyPool = new Queue<Enemy>();
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private HashSet<Enemy> culledEnemies = new HashSet<Enemy>();
    private List<Enemy> deadEnemies = new List<Enemy>(); // ?O?? List ?Ω??p

    // ????u??
    private Coroutine managementCoroutine;
    private WaitForSeconds updateWait;
    private WaitForSeconds aiUpdateWait;

    // ???B?z
    private int currentUpdateIndex = 0;
    private List<Enemy> activeEnemiesList = new List<Enemy>(); // ?Ω???B?z????s List

    // ?Z???p???u?? - ??????a??m
    private Vector3 cachedPlayerPosition;
    private float playerPositionUpdateTime = 0f;
    private const float PLAYER_POSITION_UPDATE_INTERVAL = 0.1f;

    // ??p??T - ????????K?C?????s?p??
    public int ActiveEnemyCount => activeEnemies.Count;
    public int PooledEnemyCount => enemyPool.Count;
    public int DeadEnemyCount => deadEnemies.Count;
    public int TotalEnemyCount => ActiveEnemyCount + PooledEnemyCount + culledEnemies.Count + DeadEnemyCount;

    #region Unity ??R?g??

    private void Start()
    {
        InitializeManager();
    }

    private void Update()
    {
        // ?u??s????????a??m
        UpdateCachedPlayerPosition();
    }

    private void OnDestroy()
    {
        StopManagement();
        UnsubscribeFromPlayerEvents();

        // ?M?z???q?\
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

        // ???簣?d??
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cachedPlayerPosition != Vector3.zero ? cachedPlayerPosition : player.position, cullingDistance);
    }

    #endregion

    #region ??l??

    private void InitializeManager()
    {
        ValidateReferences();
        InitializePool();
        SubscribeToPlayerEvents();
        StartManagement();

        // ??l?????H
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

        // ?]?w?????
        enemy.OnEnemyDied += HandleEnemyDied;


        // ??????
        enemyGO.SetActive(false);
        enemyPool.Enqueue(enemy);

        return enemy;
    }



    #endregion

    #region ??H????P?^??

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

        // ?]?m??m???l??
        enemy.transform.position = position;
        enemy.gameObject.SetActive(true);
        enemy.Initialize(player);

        activeEnemies.Add(enemy);

        // ?]?w AI ??s???j?]???}??s????H???? CPU ?t???^
        enemy.SetAIUpdateInterval(aiUpdateInterval + Random.Range(0f, aiUpdateInterval * 0.5f));
    }

    private Enemy GetPooledEnemy()
    {
        if (enemyPool.Count > 0)
        {
            return enemyPool.Dequeue();
        }

        // ???l??F?A?Ы?s???]?p?G???\????^
        if (TotalEnemyCount < poolSize * 1.5f) // ???C?X?i????
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

    #region ????z

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
        // ???B?z?G?C???u?B?z??????H?H???? CPU ?t??
        activeEnemiesList.Clear();
        activeEnemiesList.AddRange(activeEnemies);

        int enemiesToProcess = Mathf.Min(enemiesPerFrameUpdate, activeEnemiesList.Count);
        float cullingDistanceSqr = cullingDistance * cullingDistance; // ??Υ???Z????K?}???

        // ?B?z???D??H
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
                        // ?N??H????簣?C??
                        activeEnemies.Remove(enemy);
                        culledEnemies.Add(enemy);
                        enemy.gameObject.SetActive(false);
                    }
                }
                currentUpdateIndex++;
            }
        }

        // ?????簣??H???s?E????d?]???C?W?v?^
        if (Time.frameCount % 10 == 0) // ?C 10 ?V??d?@??
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
                // ???s?E????H
                culledEnemies.Remove(enemy);
                activeEnemies.Add(enemy);
                enemy.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    #region ???B?z

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

        // ???C??簣??H????d?W?v
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
        // ??H???`???[?????A??????
        RemoveDeadEnemy(deadEnemy);

        if (showDebugInfo)
            Debug.Log($"EnemyManager: Enemy died. Remaining enemies: {activeEnemies.Count + culledEnemies.Count}");
    }

    private void RemoveDeadEnemy(Enemy deadEnemy)
    {
        if (deadEnemy == null) return;

        // ?q??????X??????
        activeEnemies.Remove(deadEnemy);
        culledEnemies.Remove(deadEnemy);

        // ?[?J???`?C??]?Ω??p?^
        if (!deadEnemies.Contains(deadEnemy))
        {
            deadEnemies.Add(deadEnemy);
        }
    }

    #endregion

    #region ???@ API

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

    #region ??????T

    private void UpdateDebugInfo()
    {
        if (showDebugInfo && Time.frameCount % 60 == 0) // ???C??x?W?v
        {
            Debug.Log($"EnemyManager - Active: {activeEnemies.Count}, Culled: {culledEnemies.Count}, " +
                     $"Dead: {deadEnemies.Count}, Pooled: {enemyPool.Count}");
        }
    }

    #endregion
}