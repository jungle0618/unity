using UnityEngine;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Enemy 主控制器：整合各個組件，處理 AI 邏輯 (優化版本)
/// 職責：協調各組件、處理狀態轉換、對外接口
/// 優化：最大更新頻率，增加武器系統，批次處理
/// 繼承 BaseEntity 以統一架構
/// </summary>
[RequireComponent(typeof(EnemyMovement), typeof(EnemyDetection), typeof(EnemyVisualizer))]
public class Enemy : BaseEntity<EnemyState>
{
    // 組件引用（使用屬性來訪問具體類型，避免序列化衝突）
    private EnemyMovement enemyMovement => movement as EnemyMovement;
    private EnemyDetection enemyDetection => detection as EnemyDetection;
    private EnemyVisualizer enemyVisualizer => visualizer as EnemyVisualizer;
    // 狀態機需要單獨存儲，因為它是在運行時創建的，不是組件
    [System.NonSerialized] private EnemyStateMachine enemyStateMachineInstance;
    private EnemyStateMachine enemyStateMachine => enemyStateMachineInstance ?? (enemyStateMachineInstance = stateMachine as EnemyStateMachine);
    private EnemyAttackController attackController;
    
    // 基類已經有這些，但我們需要具體類型的引用
    // BaseMovement, BaseDetection, BaseVisualizer 已由基類管理

    [Header("血量設定")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("AI 參數")]
    [SerializeField] private float alertTime = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [Tooltip("敵人會在這個距離內嘗試攻擊（實際攻擊範圍由武器決定）")]
    [SerializeField] private float attackDetectionRange = 3f;

    // 效能優化變數
    private float aiUpdateInterval = 0.15f;
    private float lastAIUpdateTime = 0f;
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

    // 追擊相關變數
    private Vector2 lastSeenPlayerPosition;
    private bool hasLastSeenPosition = false;
    private float lastSeenTime = 0f;
    private float searchTime = 3f; // 在最後看到位置搜索的時間
    private bool hasReachedSearchLocation = false; // 是否已到達搜索位置

    // 視覺化控制
    private bool canVisualize = true; // 是否可以視覺化（由 EnemyManager 控制）

    // 公共屬性
    public EnemyState CurrentState => enemyStateMachine?.CurrentState ?? EnemyState.Dead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    // IsDead 和 Position 已由基類 BaseEntity 提供，無需重複定義
    
    // 血量變化事件
    public event System.Action<int, int> OnHealthChanged; // 當前血量, 最大血量
    
    /// <summary>
    /// 設定是否可以視覺化（由 EnemyManager 調用）
    /// </summary>
    public void SetCanVisualize(bool canVisualize)
    {
        this.canVisualize = canVisualize;
        
        // 通知 Visualizer 更新狀態
        if (enemyVisualizer != null)
        {
            enemyVisualizer.SetCanVisualize(canVisualize);
        }
        
        // 處理武器的 renderer，防止看的到武器看不到人
        if (itemHolder != null && itemHolder.CurrentWeapon != null)
        {
            SpriteRenderer weaponRenderer = itemHolder.CurrentWeapon.GetComponent<SpriteRenderer>();
            if (weaponRenderer != null)
            {
                weaponRenderer.enabled = canVisualize;
            }
        }
    }
    
    /// <summary>
    /// 獲取是否可以視覺化
    /// </summary>
    public bool GetCanVisualize()
    {
        return canVisualize;
    }
    
    // 保留 cachedPosition 用於內部優化（性能優化）

    // 事件
    public System.Action<Enemy> OnEnemyDied;

    #region Unity 生命週期

    protected override void Awake()
    {
        // 重要：必須在 base.Awake() 之前初始化狀態機
        // 因為 base.Awake() 會調用 ValidateComponents() 來檢查狀態機是否存在
        enemyStateMachineInstance = new EnemyStateMachine(alertTime);
        base.stateMachine = enemyStateMachineInstance; // 賦值給基類引用
        
        base.Awake(); // 調用基類 Awake，初始化基類的組件引用
        
        // 獲取具體類型的組件引用（用於訪問 Enemy 特有的方法）
        movement = GetComponent<EnemyMovement>();
        detection = GetComponent<EnemyDetection>();
        visualizer = GetComponent<EnemyVisualizer>();
        
        InitializeEnemyComponents();
    }

    protected override void Start()
    {
        base.Start(); // 調用基類 Start
        
        // 確保初始化完成後再開始 AI
        if (enemyStateMachine != null && isInitialized)
        {
            enemyStateMachine.ChangeState(EnemyState.Patrol);
        }
    }

    protected override void Update()
    {
        base.Update(); // 調用基類 Update
        
        // 更新快取位置
        UpdateCachedData();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 調用基類 FixedUpdate（會調用 FixedUpdateEntity）
        
        if (IsDead || !isInitialized) return;

        // 使用自定義更新間隔而非每幀更新
        if (Time.time - lastAIUpdateTime >= aiUpdateInterval)
        {
            UpdateAI();
            lastAIUpdateTime = Time.time;
        }
    }
    
    protected override void FixedUpdateEntity()
    {
        // 基類的 FixedUpdateEntity 會更新狀態機
        // 這裡不需要額外實現，因為 UpdateAI 在 FixedUpdate 中調用
    }
    



    #endregion

    #region 初始化

    /// <summary>
    /// 初始化 Enemy 特有組件（在基類初始化之後調用）
    /// </summary>
    private void InitializeEnemyComponents()
    {
        // ItemHolder 已由基類初始化
        attackController = GetComponent<EnemyAttackController>();

        // 初始化 Visualizer 的初始狀態
        if (enemyVisualizer != null)
        {
            enemyVisualizer.SetCanVisualize(canVisualize);
        }

        // 如果沒有 EnemyAttackController，嘗試添加一個
        if (attackController == null)
        {
            attackController = gameObject.AddComponent<EnemyAttackController>();
            attackController.SetAttackCooldown(attackCooldown);
        }

        // 狀態機已在 Awake 中初始化，這裡只需設定組件關聯
        // 設定組件關聯
        if (enemyVisualizer != null && enemyStateMachineInstance != null)
        {
            enemyVisualizer.SetStateMachine(enemyStateMachineInstance);
        }

        // 監聽狀態變更事件
        if (enemyStateMachineInstance != null)
        {
            enemyStateMachineInstance.OnStateChanged += OnStateChanged;
        }

        // 初始化快取數據
        cachedPosition = transform.position;

        // 驗證必要組件
        ValidateEnemyComponents();
    }
    
    /// <summary>
    /// 初始化實體（實現基類抽象方法）
    /// </summary>
    protected override void InitializeEntity()
    {
        // 基礎初始化已完成（在 Awake 中）
        // 這裡只需要標記為已初始化
        // 實際的目標設定和位置設定由 Initialize(Transform) 方法處理
    }

    /// <summary>
    /// 驗證 Enemy 特有組件
    /// </summary>
    private void ValidateEnemyComponents()
    {
        if (enemyMovement == null)
            Debug.LogError($"{gameObject.name}: Missing EnemyMovement component!");

        if (enemyDetection == null)
            Debug.LogError($"{gameObject.name}: Missing EnemyDetection component!");

        if (enemyVisualizer == null)
            Debug.LogWarning($"{gameObject.name}: Missing EnemyVisualizer component!");

        if (enemyStateMachine == null)
            Debug.LogError($"{gameObject.name}: Failed to initialize EnemyStateMachine!");
    }

    private void UpdateCachedData()
    {
        if (Time.time - cacheUpdateTime >= CACHE_UPDATE_INTERVAL)
        {
            cachedPosition = transform.position;

            // 只在需要時更新偵測資訊
            if (enemyDetection != null && !IsDead)
            {
                cachedCanSeePlayer = enemyDetection.CanSeePlayer();
                cachedDirectionToPlayer = enemyDetection.GetDirectionToTarget();
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
        // 初始化生命值
        currentHealth = maxHealth;
        
        // 觸發血量變化事件，初始化 Visualizer 的顏色
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (enemyDetection != null)
        {
            enemyDetection.SetTarget(playerTarget);
        }

        // 初始化patrol locations
        InitializePatrolLocations();

        // 設定敵人位置到第一個location
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            transform.position = patrolLocations[0];
            cachedPosition = patrolLocations[0];
        }

        if (enemyStateMachine != null)
        {
            enemyStateMachine.ChangeState(EnemyState.Patrol);
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
    /// 敵人受到傷害
    /// </summary>
    /// <param name="damage">傷害值</param>
    /// <param name="source">傷害來源</param>
    public void TakeDamage(int damage, string source = "")
    {
        if (IsDead) return;

        // 扣除生命值
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (!string.IsNullOrEmpty(source))
        {
            Debug.Log($"敵人 {gameObject.name} 受到 {damage} 點傷害 (來源: {source})，剩餘生命值: {currentHealth}/{maxHealth}");
        }
        
        // 觸發血量變化事件
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 生命值歸零時死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 敵人死亡（覆寫基類方法）
    /// </summary>
    public override void Die()
    {
        if (IsDead) return;

        enemyStateMachine?.ChangeState(EnemyState.Dead);
        movement?.StopMovement(); // 基類方法，使用基類欄位

        // 通知外部
        OnEnemyDied?.Invoke(this);
        Debug.LogError($"{gameObject.name}: Dropping all items1");
        DropAllItems();
    }
    
    /// <summary>
    /// 死亡處理（覆寫基類方法）
    /// </summary>
    protected override void OnDeath()
    {
        
        
        // Enemy 特定的死亡邏輯已在 Die() 中處理
        // 基類的 Die() 已經處理了停止移動，這裡可以添加其他清理邏輯
    }

    /// <summary>
    /// 設定巡邏點
    /// </summary>
        public void SetPatrolPoints(Transform[] points)
    {
        enemyMovement?.SetPatrolPoints(points);
    }

    /// <summary>
    /// 設定patrol locations
    /// </summary>
    public void SetPatrolLocations(Vector3[] locations)
    {
        patrolLocations = locations;
        currentPatrolIndex = 0;
        
        // 更新movement組件的patrol points
        if (enemyMovement != null && locations != null && locations.Length > 0)
        {
            Transform[] patrolTransforms = new Transform[locations.Length];
            for (int i = 0; i < locations.Length; i++)
            {
                GameObject patrolPoint = new GameObject($"PatrolPoint_{i}");
                patrolPoint.transform.position = locations[i];
                patrolTransforms[i] = patrolPoint.transform;
            }
            enemyMovement.SetPatrolPoints(patrolTransforms);
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
    /// 嘗試攻擊玩家 - 完全由 ItemHolder 處理攻擊邏輯
    /// </summary>
    public bool TryAttackPlayer(Transform playerTransform)
    {
        if (attackController == null) return false;
        return attackController.TryAttackPlayer(playerTransform);
    }

    /// <summary>
    /// 設定FOV倍數（用於危險等級調整）
    /// </summary>
    public void SetFovMultiplier(float multiplier)
    {
        if (enemyDetection != null)
        {
            // 這裡需要根據EnemyDetection的實際API來調整
            // 假設有SetViewRange方法
            // enemyDetection.SetViewRange(enemyDetection.ViewRange * multiplier);
            Debug.Log($"{gameObject.name}: FOV倍數設定為 {multiplier}");
        }
    }

    /// <summary>
    /// 設定移動速度倍數（用於危險等級調整）
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        if (enemyMovement != null)
        {
            // 這裡需要根據EnemyMovement的實際API來調整
            // 假設有SetSpeed方法
            // enemyMovement.SetSpeed(enemyMovement.Speed * multiplier);
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
        if (enemyStateMachine == null || enemyDetection == null || enemyMovement == null) return;

        // 更新計時器
        enemyStateMachine.UpdateAlertTimer();

        // 根據當前狀態執行對應邏輯
        switch (enemyStateMachine.CurrentState)
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

            case EnemyState.Search:
                HandleSearchState();
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
            if (enemyStateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Alert);
            }
            return;
        }

        // 沿著locations移動
        if (enemyMovement != null)
        {
            if (patrolLocations != null && patrolLocations.Length > 0)
            {
                enemyMovement.MoveAlongLocations(patrolLocations, currentPatrolIndex);
                
                // 更新視野方向跟隨移動方向
                Vector2 movementDirection = enemyMovement.GetMovementDirection();
                if (movementDirection.magnitude > 0.1f && enemyDetection != null)
                {
                    enemyDetection.SetViewDirection(movementDirection);
                }
                
                // 檢查是否到達當前location
                if (enemyMovement.HasArrivedAtLocation(patrolLocations[currentPatrolIndex]))
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolLocations.Length;
                }
            }
            else
            {
                enemyMovement.PerformPatrol();
                
                // 更新視野方向跟隨移動方向
                Vector2 movementDirection = enemyMovement.GetMovementDirection();
                if (movementDirection.magnitude > 0.1f && enemyDetection != null)
                {
                    enemyDetection.SetViewDirection(movementDirection);
                }
            }
        }
    }

    private void HandleAlertState()
    {
        if (cachedCanSeePlayer)
        {
            if (enemyStateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Chase);
            }
        }
        else if (enemyStateMachine != null && enemyStateMachine.IsAlertTimeUp())
        {
            if (enemyStateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Patrol);
            }
        }
        else
        {
            // 在Alert狀態時也沿著locations移動
            if (enemyMovement != null)
            {
                if (patrolLocations != null && patrolLocations.Length > 0)
                {
                    enemyMovement.MoveAlongLocations(patrolLocations, currentPatrolIndex);
                    
                    // 更新視野方向跟隨移動方向
                    Vector2 movementDirection = enemyMovement.GetMovementDirection();
                    if (movementDirection.magnitude > 0.1f && enemyDetection != null)
                    {
                        enemyDetection.SetViewDirection(movementDirection);
                    }
                    
                    // 檢查是否到達當前location
                    if (enemyMovement.HasArrivedAtLocation(patrolLocations[currentPatrolIndex]))
                    {
                        currentPatrolIndex = (currentPatrolIndex + 1) % patrolLocations.Length;
                    }
                }
                else
                {
                    enemyMovement.StopMovement();
                }
            }
        }
    }

    private void HandleChaseState()
    {
        if (enemyDetection != null && enemyDetection.IsTargetOutOfChaseRange())
        {
            if (enemyStateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Return);
            }
            return;
        }

        // 檢查是否撞牆或卡住，如果是則轉到搜索狀態
        if (enemyMovement != null && enemyMovement.IsStuckOrHittingWall())
        {
            Debug.Log($"{gameObject.name}: 追擊時撞牆，轉到搜索狀態");
            hasReachedSearchLocation = false; // 重置到達標記

            hasLastSeenPosition = true;
            lastSeenTime = Time.time;
            if (enemyStateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Search);
            }
            return;
        }

        if (cachedCanSeePlayer)
        {
            // 記錄玩家位置
            Transform target = enemyDetection.GetTarget();
            if (target != null)
            {
                lastSeenPlayerPosition = target.position;
                hasLastSeenPosition = true;
                lastSeenTime = Time.time;
            }

            Vector2 targetPos = cachedPosition + cachedDirectionToPlayer;
            
            // 在追擊時，敵人朝向玩家方向
            if (movement != null)
            {
                enemyMovement.ChaseTarget(targetPos);
            }
            
            // 設定敵人朝向玩家
            if (enemyDetection != null)
            {
                enemyDetection.SetViewDirection(cachedDirectionToPlayer);
            }

            // 更新武器朝向玩家
            if (itemHolder != null)
            {
                itemHolder.UpdateWeaponDirection(cachedDirectionToPlayer);
            }

            // 檢查是否在攻擊偵測範圍內，然後嘗試攻擊
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
            // 沒看到玩家，如果有最後看到的位置，轉到搜索狀態
            if (hasLastSeenPosition)
            {
                hasReachedSearchLocation = false; // 重置到達標記
                if (stateMachine != null)
                {
                    enemyStateMachine.ChangeState(EnemyState.Search);
                }
            }
            else
            {
                if (enemyStateMachine != null)
                {
                    enemyStateMachine.ChangeState(EnemyState.Alert);
                }
            }
        }
    }

    private void HandleSearchState()
    {
        // 檢查是否超出追擊範圍
        if (enemyDetection != null && enemyDetection.IsTargetOutOfChaseRange())
        {
            if (enemyStateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Return);
            }
            return;
        }

        // 如果重新看到玩家，更新最後看到的位置並重新計算路徑
        if (cachedCanSeePlayer)
        {
            Transform target = enemyDetection != null ? enemyDetection.GetTarget() : null;
            if (target != null)
            {
                // 更新最後看到的位置
                lastSeenPlayerPosition = target.position;
                hasLastSeenPosition = true;
                lastSeenTime = Time.time;
                
                // 清除路徑，強制重新計算到新位置的路徑
                if (movement != null)
                {
                    enemyMovement.ClearPath();
                }
                
                Debug.Log($"{gameObject.name}: 搜索時看到玩家，更新目標位置到 {lastSeenPlayerPosition}");
            }
        }

        // 檢查是否還有最後看到的位置
        if (!hasLastSeenPosition)
        {
            Debug.Log($"{gameObject.name}: 沒有最後看到玩家的位置，轉到警戒狀態");
            if (enemyStateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Alert);
            }
            return;
        }

        // 移動到最後看到玩家的位置，朝向跟隨路徑方向
        if (movement != null)
        {
            enemyMovement.ChaseTargetWithRotation(lastSeenPlayerPosition, enemyDetection);
        }

        // 檢查是否到達最後看到的位置
        if (Vector2.Distance(cachedPosition, lastSeenPlayerPosition) < 1f)
        {
            if (!hasReachedSearchLocation)
            {
                // 剛到達搜索位置，標記已到達
                hasReachedSearchLocation = true;
                Debug.Log($"{gameObject.name}: 已到達搜索位置，開始搜索玩家");
            }
        }

        // 只有在到達搜索位置後才能進行狀態轉換
        if (hasReachedSearchLocation)
        {
            // 檢查搜索時間是否過期
            if (Time.time - lastSeenTime > searchTime)
            {
                hasLastSeenPosition = false;
                hasReachedSearchLocation = false;
                if (stateMachine != null)
                {
                    enemyStateMachine.ChangeState(EnemyState.Alert);
                }
                return;
            }

            // 如果重新看到玩家，轉到追擊狀態
            if (cachedCanSeePlayer)
            {
                if (stateMachine != null)
                {
                    enemyStateMachine.ChangeState(EnemyState.Chase);
                }
                return;
            }
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
        else if (movement != null)
        {
            returnTarget = enemyMovement.GetReturnTarget();
        }
        else
        {
            returnTarget = cachedPosition; // 如果沒有movement組件，保持在原地
        }
        
        if (enemyMovement != null)
        {
            enemyMovement.MoveTowards(returnTarget, 1f);
        }

        if (enemyMovement != null && enemyMovement.HasArrivedAt(returnTarget))
        {
            // 重置patrol index到第一個location
            currentPatrolIndex = 0;
            if (stateMachine != null)
            {
                enemyStateMachine.ChangeState(EnemyState.Patrol);
            }
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

            case EnemyState.Search:
                // 可以在此處播放搜索音效或動畫
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

    protected override void OnDestroy()
    {
        // 調用基類的 OnDestroy 進行基礎清理
        base.OnDestroy();
        
        // 處理 Enemy 特定的清理邏輯
        if (enemyStateMachineInstance != null)
        {
            enemyStateMachineInstance.OnStateChanged -= OnStateChanged;
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
        if (itemHolder != null && itemHolder.CurrentWeapon != null)
        {
            Gizmos.color = Color.red;
            float range = 0f;
            
            // 根據武器類型取得攻擊範圍
            if (itemHolder.CurrentWeapon is MeleeWeapon meleeWeapon)
            {
                range = meleeWeapon.AttackRange;
            }
            else if (itemHolder.CurrentWeapon is RangedWeapon rangedWeapon)
            {
                range = rangedWeapon.AttackRange;
            }
            
            if (range > 0f)
            {
                Gizmos.DrawWireSphere(transform.position, range);
            }
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