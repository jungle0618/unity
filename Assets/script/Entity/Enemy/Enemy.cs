using UnityEngine;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyDetection))]
[RequireComponent(typeof(EnemyVisualizer))]
[RequireComponent(typeof(EntityHealth))]
[RequireComponent(typeof(EntityStats))]
[RequireComponent(typeof(EnemyAIHandler))]
public class Enemy : BaseEntity<EnemyState>, IEntity
{
    // 組件引用（使用屬性來訪問具體類型，避免序列化衝突）
    private EnemyMovement enemyMovement => movement as EnemyMovement;
    private EnemyDetection enemyDetection => detection as EnemyDetection;
    private EnemyVisualizer enemyVisualizer => visualizer as EnemyVisualizer;
    // 注意：entityHealth 和 entityStats 已移至基類 BaseEntity
    private EnemyAIHandler aiHandler;
    // 狀態機需要單獨存儲，因為它是在運行時創建的，不是組件
    [System.NonSerialized] private EnemyStateMachine enemyStateMachineInstance;
    private EnemyStateMachine enemyStateMachine => enemyStateMachineInstance ?? (enemyStateMachineInstance = stateMachine as EnemyStateMachine);
    private EnemyAttackController attackController;
    
    // 基類已經有這些，但我們需要具體類型的引用
    // BaseMovement, BaseDetection, BaseVisualizer 已由基類管理

    [Header("AI 參數")]
    [SerializeField] private float alertTime = 2f;
    [SerializeField] private float attackCooldown = 0.5f;
    [Tooltip("敵人會在這個距離內嘗試攻擊（實際攻擊範圍由武器決定）")]
    [SerializeField] private float attackDetectionRange = 3f;
    [Tooltip("是否使用武器的實際攻擊範圍（對持槍敵人啟用此選項）")]
    [SerializeField] private bool useWeaponAttackRange = true;
    
    [Header("移動速度乘數")]
    [Tooltip("追擊速度倍數（相對於基礎速度）")]
    [SerializeField] private float chaseSpeedMultiplier = 1.5f;

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

    // 視覺化控制
    private bool canVisualize = false; // 是否可以視覺化（由 EnemyManager 控制）

    // 公共屬性
    public EnemyState CurrentState => enemyStateMachine?.CurrentState ?? EnemyState.Dead;
    public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
    // 血量相關屬性（MaxHealth, CurrentHealth, HealthPercentage）已由基類 BaseEntity 統一提供
    // IsDead 和 Position 已由基類 BaseEntity 提供，無需重複定義
    // 血量變化事件（OnHealthChanged）已由基類 BaseEntity 統一提供
    
    /// <summary>
    /// 獲取有效攻擊範圍（根據武器類型自動調整）
    /// </summary>
    public float GetEffectiveAttackRange()
    {
        // 優先使用 attackController 的方法（如果存在）
        if (attackController != null)
        {
            return attackController.GetEffectiveAttackRange();
        }
        
        // 如果啟用了使用武器攻擊範圍選項
        if (useWeaponAttackRange && itemHolder != null && itemHolder.CurrentWeapon != null)
        {
            // 檢查是否為遠程武器（槍械）
            if (itemHolder.CurrentWeapon is RangedWeapon rangedWeapon)
            {
                return rangedWeapon.AttackRange;
            }
            // 檢查是否為近戰武器（刀械）
            else if (itemHolder.CurrentWeapon is MeleeWeapon meleeWeapon)
            {
                return meleeWeapon.AttackRange;
            }
        }
        
        // 否則使用預設的攻擊偵測範圍
        return attackDetectionRange;
    }
    
    /// <summary>
    /// 設定是否可以視覺化（由 EnemyManager 調用）
    /// </summary>
    public void SetCanVisualize(bool canVisualize)
    {
        this.canVisualize = canVisualize;
        
        // 通知 Visualizer 更新狀態（會處理所有渲染組件，包括物品）
        if (enemyVisualizer != null)
        {
            enemyVisualizer.SetCanVisualize(canVisualize);
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

    #region Animation & Sound Events

    // State transition events - expose from state machine
    public event System.Action<EnemyState, EnemyState> OnEnemyStateChanged
    {
        add { if (enemyStateMachineInstance != null) enemyStateMachineInstance.OnStateChanged += value; }
        remove { if (enemyStateMachineInstance != null) enemyStateMachineInstance.OnStateChanged -= value; }
    }
    
    public event System.Action OnStartedMoving;
    public event System.Action OnStoppedMoving;
    public event System.Action OnStartedChasing;
    public event System.Action OnStoppedChasing;
    public event System.Action OnEnteredPatrol;
    public event System.Action OnEnteredAlert;
    public event System.Action OnEnteredSearch;
    public event System.Action OnEnteredReturn;

    // Detection events
    public event System.Action OnPlayerSpotted;
    public event System.Action OnPlayerLost;

    // Movement events
    public event System.Action<Vector2> OnMovementDirectionChanged;
    public event System.Action<float> OnSpeedChanged;

    // Internal tracking for movement events
    private bool wasMoving = false;
    private Vector2 lastDirection = Vector2.zero;
    private float lastSpeed = 0f;
    private EnemyState lastState = EnemyState.Dead;
    private bool hadPlayerInSight = false;

    #endregion

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
        // entityHealth 已在基類 BaseEntity.InitializeComponents() 中獲取
        entityStats = GetComponent<EntityStats>();
        aiHandler = GetComponent<EnemyAIHandler>();
        
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
        
        // 初始化 AI Handler（如果尚未初始化）
        if (aiHandler != null && entityHealth != null)
        {
            float effectiveRange = GetEffectiveAttackRange();
            aiHandler.Initialize(effectiveRange, 3f);
        }
        SetCanVisualize(false);
    }

    protected override void Update()
    {
        base.Update(); // 調用基類 Update
        
        // 更新快取位置
        UpdateCachedData();
        
        // Fire animation events based on state changes
        CheckAndFireAnimationEvents();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 調用基類 FixedUpdate（會調用 FixedUpdateEntity）
        
        if (IsDead || !isInitialized) return;

        // AI 決策使用間隔更新（減少 CPU 負載）
        if (Time.time - lastAIUpdateTime >= aiUpdateInterval)
        {
            if (aiHandler != null)
            {
                // 更新快取數據給 AI Handler
                aiHandler.UpdateCachedData(cachedPosition, cachedDirectionToPlayer, cachedCanSeePlayer);
                aiHandler.UpdateAIDecision();
            }
            lastAIUpdateTime = Time.time;
        }

        // 移動執行每幀更新（確保移動流暢）
        if (aiHandler != null)
        {
            aiHandler.ExecuteMovement();
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

        // 設定 EnemyDetection 的狀態機引用
        if (enemyDetection != null && enemyStateMachineInstance != null)
        {
            enemyDetection.SetStateMachine(enemyStateMachineInstance);
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
            
        // 驗證新添加的組件
        if (entityHealth == null)
        {
            Debug.LogError($"{gameObject.name}: Missing EntityHealth component! Auto-adding...");
            entityHealth = gameObject.AddComponent<EntityHealth>();
        }
        
        if (entityStats == null)
        {
            Debug.LogError($"{gameObject.name}: Missing EntityStats component! Auto-adding...");
            entityStats = gameObject.AddComponent<EntityStats>();
        }
        
        if (aiHandler == null)
        {
            Debug.LogError($"{gameObject.name}: Missing EnemyAIHandler component! Auto-adding...");
            aiHandler = gameObject.AddComponent<EnemyAIHandler>();
        }
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
    /// 初始化基礎數值（覆寫基類方法）
    /// </summary>
    protected override void InitializeBaseValues()
    {
        base.InitializeBaseValues(); // 調用基類方法

        // 設定敵人專屬的基礎速度（快於玩家走路的 5f）
        if (baseSpeed <= 2f) // 如果還是預設值，設定為敵人的基礎速度
        {
            baseSpeed = 6.0f; // Enemy 的基礎速度，明顯快於 Player 走路 (5f)
        }

        // 從組件讀取基礎值（如果基類尚未設定）
        // 注意：由於 EnemyDetection 現在使用 currentViewRange/currentViewAngle，
        // 我們需要確保基礎值在 EnemyDetection 初始化時設定
        if (enemyDetection != null)
        {
            // 如果基礎值未設定，嘗試從 Detection 的預設值獲取
            // 但由於 EnemyDetection 不再直接定義這些值，我們使用預設值
            if (baseViewRange <= 0f) baseViewRange = 8f; // Enemy 的預設視野範圍
            if (baseViewAngle <= 0f) baseViewAngle = 90f; // Enemy 的預設視野角度
        }
        
        // 初始化 EnemyDetection 的當前視野範圍和角度（使用基礎值）
        if (enemyDetection != null)
        {
            enemyDetection.SetDetectionParameters(baseViewRange, baseViewAngle, enemyDetection.ChaseRange);
        }
    }

    /// <summary>
    /// 初始化敵人（由 EnemyManager 呼叫）
    /// </summary>
    public void Initialize(Transform playerTarget)
    {
        // 確保所有必需的組件都已存在（在初始化前再次檢查）
        if (entityHealth == null)
        {
            Debug.LogWarning($"{gameObject.name}: EntityHealth is null during Initialize, adding component...");
            entityHealth = gameObject.AddComponent<EntityHealth>();
        }
        
        if (entityStats == null)
        {
            Debug.LogWarning($"{gameObject.name}: EntityStats is null during Initialize, adding component...");
            entityStats = gameObject.AddComponent<EntityStats>();
        }
        
        if (aiHandler == null)
        {
            Debug.LogWarning($"{gameObject.name}: EnemyAIHandler is null during Initialize, adding component...");
            aiHandler = gameObject.AddComponent<EnemyAIHandler>();
        }
        
        // 初始化 EntityHealth（基類已訂閱死亡事件）
        if (entityHealth != null)
        {
            entityHealth.InitializeHealth();
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Failed to initialize EntityHealth!");
        }
        
        // 初始化 AI Handler
        if (aiHandler != null)
        {
            float effectiveRange = GetEffectiveAttackRange();
            aiHandler.Initialize(effectiveRange, 3f); // searchTime = 3f
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Failed to initialize EnemyAIHandler!");
        }

        SetTarget(playerTarget);

        // 初始化patrol locations（通過 AI Handler）
        InitializePatrolLocations();

        if (enemyStateMachine != null)
        {
            enemyStateMachine.ChangeState(EnemyState.Patrol);
        }

        isInitialized = true;
        lastAIUpdateTime = Time.time + Random.Range(0f, aiUpdateInterval); // 錯開初始更新時間
        
        Debug.Log($"{gameObject.name}: Enemy initialized successfully. Active: {gameObject.activeSelf}, InHierarchy: {gameObject.activeInHierarchy}");
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
    // TakeDamage() 已由基類 BaseEntity 統一實現
    // 基類會自動處理傷害減少（從 EntityStats 獲取）和死亡流程
    
    /// <summary>
    /// 獲取實體類型（實現 IEntity 接口）
    /// </summary>
    public EntityManager.EntityType GetEntityType()
    {
        return EntityManager.EntityType.Enemy;
    }

    /// <summary>
    /// 敵人死亡處理（覆寫基類方法）
    /// </summary>
    protected override void OnDeath()
    {
        enemyStateMachine?.ChangeState(EnemyState.Dead);
        gameObject.SetActive(false);
        OnEnemyDied?.Invoke(this);
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
        if (locations == null || locations.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: SetPatrolLocations called with null or empty array");
            return;
        }
        
        // 設置 AI Handler 的巡邏點
        if (aiHandler != null)
        {
            aiHandler.SetPatrolLocations(locations);
        }
        
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
        if (aiHandler != null)
        {
            Vector3[] locations = aiHandler.GetPatrolLocations();
            if (locations != null && locations.Length > 0)
            {
                int index = aiHandler.GetCurrentPatrolIndex();
                if (index >= 0 && index < locations.Length)
                {
                    return locations[index];
                }
            }
        }
        return transform.position;
    }

    /// <summary>
    /// 取得第一個patrol location（spawn point）
    /// </summary>
    public Vector3 GetFirstPatrolLocation()
    {
        if (aiHandler != null)
        {
            Vector3[] locations = aiHandler.GetPatrolLocations();
            if (locations != null && locations.Length > 0)
            {
                return locations[0];
            }
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

    #endregion

    #region Animation Event Firing Logic

    /// <summary>
    /// Check and fire animation events based on state and movement changes
    /// </summary>
    private void CheckAndFireAnimationEvents()
    {
        if (!isInitialized || enemyStateMachine == null) return;

        // Check state changes
        EnemyState currentState = enemyStateMachine.CurrentState;
        if (currentState != lastState)
        {
            // Fire state-specific enter events
            switch (currentState)
            {
                case EnemyState.Patrol:
                    OnEnteredPatrol?.Invoke();
                    break;
                case EnemyState.Alert:
                    OnEnteredAlert?.Invoke();
                    break;
                case EnemyState.Chase:
                    OnStartedChasing?.Invoke();
                    break;
                case EnemyState.Search:
                    OnEnteredSearch?.Invoke();
                    break;
                case EnemyState.Return:
                    OnEnteredReturn?.Invoke();
                    break;
            }

            // Fire exit events for previous state
            if (lastState == EnemyState.Chase)
            {
                OnStoppedChasing?.Invoke();
            }

            lastState = currentState;
        }

        // Check movement state
        bool isCurrentlyMoving = enemyMovement != null && enemyMovement.GetSpeed() > 0.01f;
        if (isCurrentlyMoving != wasMoving)
        {
            if (isCurrentlyMoving)
                OnStartedMoving?.Invoke();
            else
                OnStoppedMoving?.Invoke();

            wasMoving = isCurrentlyMoving;
        }

        // Check movement direction changes
        if (enemyMovement != null)
        {
            Vector2 currentDirection = enemyMovement.GetMovementDirection();
            if (Vector2.Distance(currentDirection, lastDirection) > 0.1f)
            {
                OnMovementDirectionChanged?.Invoke(currentDirection);
                lastDirection = currentDirection;
            }
        }

        // Check speed changes
        if (enemyMovement != null)
        {
            float currentSpeed = enemyMovement.GetSpeed();
            if (Mathf.Abs(currentSpeed - lastSpeed) > 0.01f)
            {
                OnSpeedChanged?.Invoke(currentSpeed);
                lastSpeed = currentSpeed;
            }
        }

        // Check player detection changes
        bool currentlySeesPlayer = enemyDetection != null && enemyDetection.CanSeePlayer();
        if (currentlySeesPlayer != hadPlayerInSight)
        {
            if (currentlySeesPlayer)
                OnPlayerSpotted?.Invoke();
            else
                OnPlayerLost?.Invoke();

            hadPlayerInSight = currentlySeesPlayer;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 根據危險等級更新所有屬性
    /// </summary>
    public void UpdateDangerLevelStats(float viewRangeMultiplier, float viewAngleMultiplier, float speedMultiplier, float damageReduction)
    {
        // 使用 EntityStats 更新屬性乘數
        if (entityStats != null)
        {
            entityStats.UpdateDangerLevelStats(viewRangeMultiplier, viewAngleMultiplier, speedMultiplier, damageReduction);
        }
        
        // 更新視野範圍和角度（使用基類的基礎數值）
        if (enemyDetection != null)
        {
            float newViewRange = BaseViewRange * viewRangeMultiplier;
            float newViewAngle = BaseViewAngle * viewAngleMultiplier;
            enemyDetection.SetDetectionParameters(newViewRange, newViewAngle, enemyDetection.ChaseRange);
        }
        
        // 更新移動速度（使用基類的基礎數值）
        if (enemyMovement != null)
        {
            float newSpeed = BaseSpeed * speedMultiplier;
            enemyMovement.SetSpeed(newSpeed);
        }
        
        // 更新 EntityHealth 的傷害減少
        if (entityHealth != null)
        {
            entityHealth.SetDamageReduction(damageReduction);
        }
    }
    
    /// <summary>
    /// 設定移動速度倍數（用於危險等級調整，已廢棄，請使用 UpdateDangerLevelStats）
    /// </summary>
    [System.Obsolete("請使用 UpdateDangerLevelStats 方法")]
    public void SetSpeedMultiplier(float multiplier)
    {
        if (entityStats != null)
        {
            entityStats.SetSpeedMultiplier(multiplier);
        }
        if (enemyMovement != null)
        {
            float newSpeed = BaseSpeed * multiplier;
            enemyMovement.SetSpeed(newSpeed);
        }
    }

    /// <summary>
    /// 設定傷害減少（用於危險等級調整，已廢棄，請使用 UpdateDangerLevelStats）
    /// </summary>
    [System.Obsolete("請使用 UpdateDangerLevelStats 方法")]
    public void SetDamageReduction(float reduction)
    {
        if (entityStats != null)
        {
            entityStats.SetDamageReduction(reduction);
        }
        if (entityHealth != null)
        {
            entityHealth.SetDamageReduction(reduction);
        }
    }
    
    // GetDamageReduction() 已由基類 BaseEntity 統一提供

    /// <summary>
    /// 通知敵人玩家的位置（由外部系統呼叫，如槍聲警報）
    /// 根據 enemy_ai.md：視野外且不在 Chase/Search 狀態時，不應對槍聲做出反應
    /// </summary>
    /// <param name="playerPosition">玩家位置</param>
    public void NotifyPlayerPosition(Vector2 playerPosition)
    {
        if (IsDead) return;

        // 檢查是否應該對槍聲做出反應（考慮攝影機剔除）
        // 根據 enemy_ai.md：視野外且不在 Chase/Search 狀態時，不應對外在事物做出反應
        if (enemyDetection != null && !enemyDetection.ShouldUpdateAI())
        {
            // 視野外且不在 Chase/Search 狀態，不反應
            return;
        }

        // 記錄玩家位置（通過 AI Handler）
        if (aiHandler != null)
        {
            aiHandler.RecordLastSeenPosition(playerPosition);
        }

        // 根據當前狀態決定如何反應
        if (enemyStateMachine != null)
        {
            EnemyState currentState = enemyStateMachine.CurrentState;
            
            // 如果在巡邏狀態，切換到警戒狀態
            if (currentState == EnemyState.Patrol)
            {
                enemyStateMachine.ChangeState(EnemyState.Alert);
                Debug.Log($"{gameObject.name}: 聽到槍聲，進入警戒狀態");
            }
            // 如果在警戒或返回狀態，切換到搜索狀態
            else if (currentState == EnemyState.Alert || currentState == EnemyState.Return)
            {
                if (aiHandler != null)
                {
                    aiHandler.ClearSearchState();
                }
                enemyStateMachine.ChangeState(EnemyState.Search);
                Debug.Log($"{gameObject.name}: 聽到槍聲，前往玩家位置搜索");
            }
            // 如果已經在追擊或搜索狀態，更新最後看到的位置即可
            else if (currentState == EnemyState.Chase || currentState == EnemyState.Search)
            {
                Debug.Log($"{gameObject.name}: 聽到槍聲，更新玩家位置");
            }
        }
    }

    #endregion

    #region AI 邏輯

    // 注意：AI 邏輯已移至 EnemyAIHandler 組件
    // 這裡保留區域標記以維持代碼結構


    #endregion

    #region 清理

    protected override void OnDestroy()
    {
        // 調用基類的 OnDestroy 進行基礎清理
        base.OnDestroy();
        
        // Enemy 特定的清理邏輯
        // 注意：狀態機事件現在通過 OnEnemyStateChanged 屬性暴露，不需要在這裡取消訂閱
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
        // 從 AI Handler 獲取巡邏點
        Vector3[] locations = null;
        if (aiHandler != null)
        {
            locations = aiHandler.GetPatrolLocations();
        }
        
        if (locations != null && locations.Length > 0)
        {
            // 繪製patrol locations
            for (int i = 0; i < locations.Length; i++)
            {
                Vector3 pos = locations[i];
                
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
                if (i < locations.Length - 1)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos, locations[i + 1]);
                }
                else
                {
                    // 最後一個點連回第一個點
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos, locations[0]);
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