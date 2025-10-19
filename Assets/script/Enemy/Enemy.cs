using UnityEngine;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Enemy 主控制器：整合各個組件，處理 AI 邏輯 (優化版本)
/// 職責：協調各組件、處理狀態轉換、對外接口
/// 優化：最大更新頻率，增加武器系統，批次處理
/// </summary>
[RequireComponent(typeof(EnemyMovement), typeof(EnemyDetection), typeof(EnemyVisualizer))]
public class Enemy : MonoBehaviour
{
    // 組件引用
    private EnemyMovement movement;
    private EnemyDetection detection;
    private EnemyVisualizer visualizer;
    private EnemyStateMachine stateMachine;
    private WeaponHolder weaponHolder;

    [Header("AI 參數")]
    [SerializeField] private float alertTime = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [Tooltip("敵人會在這個距離內嘗試攻擊（實際攻擊範圍由武器決定）")]
    [SerializeField] private float attackDetectionRange = 3f;

    // 效能優化變數
    private float aiUpdateInterval = 0.15f;
    private float lastAIUpdateTime = 0f;
    private float lastAttackTime = 0f;
    private bool isInitialized = false;

    // 快取變數以減少 GC 分配
    private Vector2 cachedPosition;
    private Vector2 cachedDirectionToPlayer;
    private bool cachedCanSeePlayer;
    private float cacheUpdateTime = 0f;
    private const float CACHE_UPDATE_INTERVAL = 0.1f;

    // Patrol locations
    private Vector3[] patrolLocations;
    private int currentPatrolIndex = 0;

    // 公共屬性
    public EnemyState CurrentState => stateMachine?.CurrentState ?? EnemyState.Dead;
    public bool IsDead => stateMachine?.IsDead ?? true;
    public Vector2 Position => cachedPosition;

    // 事件
    public System.Action<Enemy> OnEnemyDied;

    #region Unity 生命週期

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        // 確保初始化完成後再開始 AI
        if (stateMachine != null && isInitialized)
        {
            stateMachine.ChangeState(EnemyState.Patrol);
        }
    }

    private void Update()
    {
        // 更新快取位置
        UpdateCachedData();
    }

    private void FixedUpdate()
    {
        if (IsDead || !isInitialized) return;

        // 使用自定義更新間隔而非每幀更新
        if (Time.time - lastAIUpdateTime >= aiUpdateInterval)
        {
            UpdateAI();
            lastAIUpdateTime = Time.time;
        }
        UpdateRotation();
    }
    [SerializeField] private float idleRotationInterval = 2f; // 每隔幾秒旋轉一次
    [SerializeField] private float idleRotationAngle = 30f;   // 每次旋轉的角度

    private float idleRotationTimer = 0f;

    private void UpdateRotation()
    {
        if (stateMachine == null) return;

        if (stateMachine.CurrentState == EnemyState.Chase)
        {
            // Chase 狀態 → 面向玩家
            Vector2 directionToPlayer = cachedDirectionToPlayer.normalized;
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            // 非 Chase 狀態 → 間隔旋轉
            idleRotationTimer += Time.deltaTime;
            if (idleRotationTimer >= idleRotationInterval)
            {
                idleRotationTimer = 0f;

                // 隨機順時針或逆時針
                float randomAngle = (Random.value > 0.5f ? 1f : -1f) * idleRotationAngle;

                // 應用旋轉
                transform.Rotate(0f, 0f, randomAngle);
            }
        }
    }



    #endregion

    #region 初始化

    private void InitializeComponents()
    {
        // 獲取組件
        movement = GetComponent<EnemyMovement>();
        detection = GetComponent<EnemyDetection>();
        visualizer = GetComponent<EnemyVisualizer>();
        weaponHolder = GetComponent<WeaponHolder>();

        // 如果沒有 WeaponHolder，嘗試添加一個
        if (weaponHolder == null)
        {
            weaponHolder = gameObject.AddComponent<WeaponHolder>();
        }

        // 初始化狀態機
        stateMachine = new EnemyStateMachine(alertTime);

        // 設定組件關聯
        if (visualizer != null)
        {
            visualizer.SetStateMachine(stateMachine);
        }

        // 監聽狀態變更事件
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += OnStateChanged;
        }

        // 初始化快取數據
        cachedPosition = transform.position;

        // 驗證必要組件
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (movement == null)
            Debug.LogError($"{gameObject.name}: Missing EnemyMovement component!");

        if (detection == null)
            Debug.LogError($"{gameObject.name}: Missing EnemyDetection component!");

        if (visualizer == null)
            Debug.LogWarning($"{gameObject.name}: Missing EnemyVisualizer component!");

        if (weaponHolder == null)
            Debug.LogWarning($"{gameObject.name}: Missing WeaponHolder component!");
    }

    private void UpdateCachedData()
    {
        if (Time.time - cacheUpdateTime >= CACHE_UPDATE_INTERVAL)
        {
            cachedPosition = transform.position;

            // 只在需要時更新偵測資訊
            if (detection != null && !IsDead)
            {
                cachedCanSeePlayer = detection.CanSeePlayer();
                cachedDirectionToPlayer = detection.GetDirectionToTarget();
            }

            cacheUpdateTime = Time.time;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化敵人（由 EnemyManager 呼叫）
    /// </summary>
    public void Initialize(Transform playerTarget)
    {
        if (detection != null)
        {
            detection.SetTarget(playerTarget);
        }

        // 初始化patrol locations
        InitializePatrolLocations();

        // 設定敵人位置到第一個location
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            transform.position = patrolLocations[0];
            cachedPosition = patrolLocations[0];
        }

        if (stateMachine != null)
        {
            stateMachine.ChangeState(EnemyState.Patrol);
        }

        isInitialized = true;
        lastAIUpdateTime = Time.time + Random.Range(0f, aiUpdateInterval); // 錯開初始更新時間
    }

    /// <summary>
    /// 設定 AI 更新間隔（由 EnemyManager 呼叫以錯開更新時間）
    /// </summary>
    public void SetAIUpdateInterval(float interval)
    {
        aiUpdateInterval = Mathf.Max(0.1f, interval); // 確保不會小於 0.1 秒
    }

    /// <summary>
    /// 敵人死亡
    /// </summary>
    public void Die()
    {
        if (IsDead) return;

        stateMachine?.ChangeState(EnemyState.Dead);
        movement?.StopMovement();

        // 通知外部
        OnEnemyDied?.Invoke(this);
    }

    /// <summary>
    /// 設定巡邏點
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        movement?.SetPatrolPoints(points);
    }

    /// <summary>
    /// 設定patrol locations
    /// </summary>
    public void SetPatrolLocations(Vector3[] locations)
    {
        patrolLocations = locations;
        currentPatrolIndex = 0;
        
        // 更新movement組件的patrol points
        if (movement != null && locations != null && locations.Length > 0)
        {
            Transform[] patrolTransforms = new Transform[locations.Length];
            for (int i = 0; i < locations.Length; i++)
            {
                GameObject patrolPoint = new GameObject($"PatrolPoint_{i}");
                patrolPoint.transform.position = locations[i];
                patrolTransforms[i] = patrolPoint.transform;
            }
            movement.SetPatrolPoints(patrolTransforms);
        }
    }

    /// <summary>
    /// 初始化patrol locations（由EnemyManager設定）
    /// </summary>
    private void InitializePatrolLocations()
    {
        // Patrol locations現在由EnemyManager在SpawnEnemy時設定
        // 這裡不需要做任何事情
    }

    /// <summary>
    /// 取得當前patrol location
    /// </summary>
    public Vector3 GetCurrentPatrolLocation()
    {
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            return patrolLocations[currentPatrolIndex];
        }
        return transform.position;
    }

    /// <summary>
    /// 取得第一個patrol location（spawn point）
    /// </summary>
    public Vector3 GetFirstPatrolLocation()
    {
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            return patrolLocations[0];
        }
        return transform.position;
    }

    /// <summary>
    /// 強制改變狀態（供外部系統使用）
    /// </summary>
    public void ForceChangeState(EnemyState newState)
    {
        stateMachine?.ChangeState(newState);
    }

    /// <summary>
    /// 嘗試攻擊玩家 - 完全由 WeaponHolder 處理攻擊邏輯
    /// </summary>
    public bool TryAttackPlayer(Transform playerTransform)
    {
        if (weaponHolder == null || playerTransform == null) return false;

        // 檢查攻擊冷卻時間
        if (Time.time - lastAttackTime < attackCooldown) return false;

        // 更新武器朝向玩家
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        weaponHolder.UpdateWeaponDirection(directionToPlayer);

        // 檢查武器是否可以攻擊（包含距離檢查等）
        if (!weaponHolder.CanAttack()) return false;

        // 嘗試攻擊 - 讓 WeaponHolder 處理所有攻擊邏輯
        bool attackSucceeded = weaponHolder.TryAttack(gameObject);

        if (attackSucceeded)
        {
            lastAttackTime = Time.time;
        }

        return attackSucceeded;
    }

    /// <summary>
    /// 設定FOV倍數（用於危險等級調整）
    /// </summary>
    public void SetFovMultiplier(float multiplier)
    {
        if (detection != null)
        {
            // 這裡需要根據EnemyDetection的實際API來調整
            // 假設有SetViewRange方法
            // detection.SetViewRange(detection.ViewRange * multiplier);
            Debug.Log($"{gameObject.name}: FOV倍數設定為 {multiplier}");
        }
    }

    /// <summary>
    /// 設定移動速度倍數（用於危險等級調整）
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        if (movement != null)
        {
            // 這裡需要根據EnemyMovement的實際API來調整
            // 假設有SetSpeed方法
            // movement.SetSpeed(movement.Speed * multiplier);
            Debug.Log($"{gameObject.name}: 速度倍數設定為 {multiplier}");
        }
    }

    /// <summary>
    /// 設定傷害減少（用於危險等級調整）
    /// </summary>
    public void SetDamageReduction(float reduction)
    {
        // 這裡可以設定敵人受到的傷害減少
        // 例如：在Enemy類中添加一個damageReduction字段
        // damageReduction = reduction;
        Debug.Log($"{gameObject.name}: 傷害減少設定為 {reduction:P0}");
    }

    #endregion

    #region AI 邏輯

    private void UpdateAI()
    {
        if (stateMachine == null || detection == null || movement == null) return;

        // 更新計時器
        stateMachine.UpdateAlertTimer();

        // 根據當前狀態執行對應邏輯
        switch (stateMachine.CurrentState)
        {
            case EnemyState.Patrol:
                HandlePatrolState();
                break;

            case EnemyState.Alert:
                HandleAlertState();
                break;

            case EnemyState.Chase:
                HandleChaseState();
                break;

            case EnemyState.Return:
                HandleReturnState();
                break;
        }
    }

    private void HandlePatrolState()
    {
        if (cachedCanSeePlayer)
        {
            stateMachine.ChangeState(EnemyState.Alert);
            return;
        }

        // 沿著locations移動
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            movement.MoveAlongLocations(patrolLocations, currentPatrolIndex);
            
            // 檢查是否到達當前location
            if (movement.HasArrivedAtLocation(patrolLocations[currentPatrolIndex]))
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolLocations.Length;
            }
        }
        else
        {
            movement.PerformPatrol();
        }
    }

    private void HandleAlertState()
    {
        if (cachedCanSeePlayer)
        {
            stateMachine.ChangeState(EnemyState.Chase);
        }
        else if (stateMachine.IsAlertTimeUp())
        {
            stateMachine.ChangeState(EnemyState.Patrol);
        }
        else
        {
            // 在Alert狀態時也沿著locations移動
            if (patrolLocations != null && patrolLocations.Length > 0)
            {
                movement.MoveAlongLocations(patrolLocations, currentPatrolIndex);
                
                // 檢查是否到達當前location
                if (movement.HasArrivedAtLocation(patrolLocations[currentPatrolIndex]))
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolLocations.Length;
                }
            }
            else
            {
                movement.StopMovement();
            }
        }
    }

    private void HandleChaseState()
    {
        if (detection.IsTargetOutOfChaseRange())
        {
            stateMachine.ChangeState(EnemyState.Return);
            return;
        }

        if (cachedCanSeePlayer)
        {
            Vector2 targetPos = cachedPosition + cachedDirectionToPlayer;
            movement.ChaseTarget(targetPos);

            // 更新武器朝向玩家
            if (weaponHolder != null)
            {
                weaponHolder.UpdateWeaponDirection(cachedDirectionToPlayer);
            }

            // 檢查是否在攻擊偵測範圍內，然後嘗試攻擊
            Transform target = detection.GetTarget();
            if (target != null)
            {
                float distanceToTarget = Vector2.Distance(cachedPosition, target.position);
                if (distanceToTarget <= attackDetectionRange)
                {
                    TryAttackPlayer(target);
                }
            }
        }
        else
        {
            stateMachine.ChangeState(EnemyState.Alert);
        }
    }

    private void HandleReturnState()
    {
        Vector2 returnTarget;
        
        // 優先使用第一個patrol location作為返回目標
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            returnTarget = patrolLocations[0];
        }
        else
        {
            returnTarget = movement.GetReturnTarget();
        }
        
        movement.MoveTowards(returnTarget, 1f);

        if (movement.HasArrivedAt(returnTarget))
        {
            // 重置patrol index到第一個location
            currentPatrolIndex = 0;
            stateMachine.ChangeState(EnemyState.Patrol);
        }
    }

    #endregion

    #region 事件處理

    private void OnStateChanged(EnemyState oldState, EnemyState newState)
    {
        // 處理狀態轉換的特殊邏輯
        switch (newState)
        {
            case EnemyState.Dead:
                HandleDeathState();
                break;

            case EnemyState.Alert:
                // 可以在此處播放警戒音效或動畫
                break;

            case EnemyState.Chase:
                // 可以在此處播放追擊音效或動畫
                break;
        }

        // 降低日誌頻率
        if (Time.frameCount % 60 == 0) // 每 60 幀才輸出一次
        {
            Debug.Log($"{gameObject.name}: State changed from {oldState} to {newState}");
        }
    }

    private void HandleDeathState()
    {
        // 禁用遊戲物件或播放死亡動畫
        gameObject.SetActive(false);
    }

    #endregion

    #region 清理

    private void OnDestroy()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= OnStateChanged;
        }
    }

    #endregion

    #region 除錯輔助

    private void OnDrawGizmosSelected()
    {
        // 顯示攻擊偵測範圍
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackDetectionRange);

        // 如果有武器，顯示武器的實際攻擊範圍
        if (weaponHolder != null && weaponHolder.CurrentWeapon != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, weaponHolder.CurrentWeapon.AttackRange);
        }

        // 顯示patrol locations
        DrawPatrolLocations();
    }

    private void OnDrawGizmos()
    {
        // 在非選中狀態下也顯示patrol locations（較淡的顏色）
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        DrawPatrolLocations();
    }

    private void DrawPatrolLocations()
    {
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            // 繪製patrol locations
            for (int i = 0; i < patrolLocations.Length; i++)
            {
                Vector3 pos = patrolLocations[i];
                
                // 第一個位置（spawn point）用不同顏色
                if (i == 0)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(pos, 0.5f);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(pos, 0.3f);
                }

                // 繪製連線
                if (i < patrolLocations.Length - 1)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos, patrolLocations[i + 1]);
                }
                else
                {
                    // 最後一個點連回第一個點
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos, patrolLocations[0]);
                }

                // 顯示編號
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(pos + Vector3.up * 0.8f, $"P{i + 1}");
#endif
            }
        }
    }

    #endregion
}