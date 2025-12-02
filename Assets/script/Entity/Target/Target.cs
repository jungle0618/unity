using UnityEngine;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Target 主控制器：整合各個組件，處理 AI 邏輯 (優化版本)
/// 職責：協調各組件、處理狀態轉換、對外接口
/// 優化：最大更新頻率，增加武器系統，批次處理
/// 繼承 BaseEntity 以統一架構
/// 
/// 【封裝原則】
/// 所有 Target 狀態的修改都必須通過 Target 類的公共方法進行，禁止直接訪問內部組件來修改狀態。
/// 
/// ❌ 錯誤範例（禁止）：
///   - target.Movement.SetSpeed(2);  // 直接訪問 TargetMovement
///   - target.Detection.SetDetectionParameters(...);  // 直接訪問 TargetDetection
///   - target.gameObject.GetComponent<TargetMovement>().speed = 2;  // 直接修改屬性
/// 
/// ✅ 正確範例（推薦）：
///   - target.UpdateDangerLevelStats(viewRangeMult, viewAngleMult, speedMult, damageReduction);
///   - target.TakeDamage(10, "Player");
///   - target.SetCanVisualize(false);
///   - target.NotifyPlayerPosition(position);
///   - target.SetEscapePoint(position);
/// 
/// 如需修改 Target 的狀態，請使用以下公共方法：
///   - UpdateDangerLevelStats() - 更新危險等級相關屬性（速度、視野、傷害減少）
///   - TakeDamage() - 造成傷害
///   - SetCanVisualize() - 設定視覺化狀態
///   - NotifyPlayerPosition() - 通知玩家位置（用於槍聲警報等）
///   - SetEscapePoint() - 設定逃亡點
///   - SetPatrolPoints() / SetPatrolLocations() - 設定巡邏點
///   - ForceChangeState() - 強制改變狀態
/// </summary>
[RequireComponent(typeof(TargetMovement))]
[RequireComponent(typeof(TargetDetection))]
[RequireComponent(typeof(TargetVisualizer))]
[RequireComponent(typeof(EntityHealth))]
[RequireComponent(typeof(EntityStats))]
[RequireComponent(typeof(TargetAIHandler))]
public class Target : BaseEntity<TargetState>, IEntity
{
    // 組件引用（使用屬性來訪問具體類型，避免序列化衝突）
    private TargetMovement targetMovement => movement as TargetMovement;
    private TargetDetection targetDetection => detection as TargetDetection;
    private TargetVisualizer targetVisualizer => visualizer as TargetVisualizer;
    // 注意：entityHealth 已移至基類 BaseEntity
    private EntityStats entityStats;
    private TargetAIHandler aiHandler;
    // 狀態機需要單獨存儲，因為它是在運行時創建的，不是組件
    [System.NonSerialized] private TargetStateMachine targetStateMachineInstance;
    private TargetStateMachine targetStateMachine => targetStateMachineInstance ?? (targetStateMachineInstance = stateMachine as TargetStateMachine);
    
    // 基類已經有這些，但我們需要具體類型的引用
    // BaseMovement, BaseDetection, BaseVisualizer 已由基類管理

    [Header("逃亡設定")]
    [Tooltip("逃亡速度")]
    [SerializeField] private float escapeSpeed = 1.5f;
    

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

    // 視覺化控制
    private bool canVisualize = false;// 是否可以視覺化（由 EnemyManager 控制）

    // 事件：目標死亡和逃脫事件
    public event System.Action<Target> OnTargetDied;
    public event System.Action<Target> OnTargetReachedEscapePoint;

    #region Animation & Sound Events

    // State transition events - expose from state machine
    public event System.Action<TargetState, TargetState> OnTargetStateChanged
    {
        add { if (targetStateMachineInstance != null) targetStateMachineInstance.OnStateChanged += value; }
        remove { if (targetStateMachineInstance != null) targetStateMachineInstance.OnStateChanged -= value; }
    }
    
    public event System.Action OnStartedMoving;
    public event System.Action OnStoppedMoving;
    public event System.Action OnStartedEscaping;
    public event System.Action OnStoppedEscaping;

    // Escape events
    public event System.Action OnPlayerSpotted;
    public event System.Action<float> OnEscapeProgressChanged;

    // Movement events
    public event System.Action<Vector2> OnMovementDirectionChanged;

    // Internal tracking for events
    private bool wasMoving = false;
    private Vector2 lastDirection = Vector2.zero;
    private TargetState lastState = TargetState.Dead;
    private bool hadPlayerInSight = false;
    private float lastEscapeProgress = 0f;

    #endregion

    // 公共屬性
    public TargetState CurrentState => targetStateMachine?.CurrentState ?? TargetState.Stay;
    public int CurrentHealth => entityHealth != null ? entityHealth.CurrentHealth : 0;
    public int MaxHealth => entityHealth != null ? entityHealth.MaxHealth : 0;
    public float HealthPercentage => entityHealth != null ? entityHealth.HealthPercentage : 0f;
    // IsDead 和 Position 已由基類 BaseEntity 提供，無需重複定義
    
    // 血量變化事件（委託給 EntityHealth）
    public event System.Action<int, int> OnHealthChanged
    {
        add { if (entityHealth != null) entityHealth.OnHealthChanged += value; }
        remove { if (entityHealth != null) entityHealth.OnHealthChanged -= value; }
    }
    
    /// <summary>
    /// 設定是否可以視覺化（由 EnemyManager 調用）
    /// </summary>
    public void SetCanVisualize(bool canVisualize)
    {
        this.canVisualize = canVisualize;
        
        // 通知 Visualizer 更新狀態（會處理所有渲染組件，包括物品）
        if (targetVisualizer != null)
        {
            targetVisualizer.SetCanVisualize(canVisualize);
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

    #region Unity 生命週期

    protected override void Awake()
    {
        // 重要：必須在 base.Awake() 之前初始化狀態機
        // 因為 base.Awake() 會調用 ValidateComponents() 來檢查狀態機是否存在
        targetStateMachineInstance = new TargetStateMachine();
        base.stateMachine = targetStateMachineInstance; // 賦值給基類引用
        
        base.Awake(); // 調用基類 Awake，初始化基類的組件引用
        
        // 獲取具體類型的組件引用（用於訪問 Target 特有的方法）
        movement = GetComponent<TargetMovement>();
        detection = GetComponent<TargetDetection>();
        visualizer = GetComponent<TargetVisualizer>();
        entityHealth = GetComponent<EntityHealth>();
        entityStats = GetComponent<EntityStats>();
        aiHandler = GetComponent<TargetAIHandler>();
        
        // 訂閱 AI Handler 的事件
        if (aiHandler != null)
        {
            aiHandler.OnTargetReachedEscapePoint += HandleTargetReachedEscapePoint;
        }
        
        // 訂閱 EntityHealth 的死亡事件
        if (entityHealth != null)
        {
            entityHealth.OnEntityDied += HandleTargetDied;
        }
        
        InitializeTargetComponents();
    }

    protected override void Start()
    {
        base.Start(); // 調用基類 Start
        
        // 確保初始化完成後再開始 AI
        if (targetStateMachine != null && isInitialized)
        {
            targetStateMachine.ChangeState(TargetState.Stay);
        }
        SetCanVisualize(false);
        
        // 在地圖上註冊目標標記
        MapUIManager mapUI = FindFirstObjectByType<MapUIManager>();
        if (mapUI != null)
        {
            mapUI.AddTargetMarker(this);
        }
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
        
        if ((entityHealth != null && entityHealth.IsDead) || !isInitialized) return;

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
    /// 初始化 Target 特有組件（在基類初始化之後調用）
    /// </summary>
    private void InitializeTargetComponents()
    {
        // 初始化 Visualizer 的初始狀態
        if (targetVisualizer != null)
        {
            targetVisualizer.SetCanVisualize(canVisualize);
        }

        // 狀態機已在 Awake 中初始化，這裡只需設定組件關聯
        // 設定組件關聯
        if (targetVisualizer != null && targetStateMachineInstance != null)
        {
            targetVisualizer.SetStateMachine(targetStateMachineInstance);
        }

        // 設定 TargetDetection 的狀態機引用
        if (targetDetection != null && targetStateMachineInstance != null)
        {
            targetDetection.SetStateMachine(targetStateMachineInstance);
        }

        // 初始化快取數據
        cachedPosition = transform.position;

        // 驗證必要組件
        ValidateTargetComponents();
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
    /// 驗證 Target 特有組件
    /// </summary>
    private void ValidateTargetComponents()
    {
        if (targetMovement == null)
            Debug.LogError($"{gameObject.name}: Missing TargetMovement component!");

        if (targetDetection == null)
            Debug.LogError($"{gameObject.name}: Missing TargetDetection component!");

        if (targetVisualizer == null)
            Debug.LogWarning($"{gameObject.name}: Missing TargetVisualizer component!");

        if (targetStateMachine == null)
            Debug.LogError($"{gameObject.name}: Failed to initialize TargetStateMachine!");
            
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
            Debug.LogError($"{gameObject.name}: Missing TargetAIHandler component! Auto-adding...");
            aiHandler = gameObject.AddComponent<TargetAIHandler>();
        }
    }

    private void UpdateCachedData()
    {
        if (Time.time - cacheUpdateTime >= CACHE_UPDATE_INTERVAL)
        {
            cachedPosition = transform.position;

            // 只在需要時更新偵測資訊
            if (targetDetection != null && (entityHealth == null || !entityHealth.IsDead))
            {
                cachedCanSeePlayer = targetDetection.CanSeePlayer();
                cachedDirectionToPlayer = targetDetection.GetDirectionToTarget();
            }

            cacheUpdateTime = Time.time;
        }
    }

    #endregion

    #region Animation Event Firing Logic

    /// <summary>
    /// Check and fire animation events based on state and movement changes
    /// </summary>
    private void CheckAndFireAnimationEvents()
    {
        if (!isInitialized || targetStateMachine == null) return;

        // Check state changes
        TargetState currentState = targetStateMachine.CurrentState;
        if (currentState != lastState)
        {
            // Fire state-specific events
            if (currentState == TargetState.Escape)
            {
                OnStartedEscaping?.Invoke();
            }
            else if (lastState == TargetState.Escape)
            {
                OnStoppedEscaping?.Invoke();
            }

            lastState = currentState;
        }

        // Check movement state
        bool isCurrentlyMoving = targetMovement != null && targetMovement.GetSpeed() > 0.01f;
        if (isCurrentlyMoving != wasMoving)
        {
            if (isCurrentlyMoving)
                OnStartedMoving?.Invoke();
            else
                OnStoppedMoving?.Invoke();

            wasMoving = isCurrentlyMoving;
        }

        // Check movement direction changes
        if (targetMovement != null)
        {
            Vector2 currentDirection = targetMovement.GetMovementDirection();
            if (Vector2.Distance(currentDirection, lastDirection) > 0.1f)
            {
                OnMovementDirectionChanged?.Invoke(currentDirection);
                lastDirection = currentDirection;
            }
        }

        // Check player detection changes
        bool currentlySeesPlayer = targetDetection != null && targetDetection.CanSeePlayer();
        if (currentlySeesPlayer != hadPlayerInSight)
        {
            if (currentlySeesPlayer)
                OnPlayerSpotted?.Invoke();

            hadPlayerInSight = currentlySeesPlayer;
        }

        // Check escape progress changes (only when escaping)
        if (currentState == TargetState.Escape && aiHandler != null)
        {
            Vector3 escapePoint = aiHandler.GetEscapePoint();
            Vector3 startPosition = patrolLocations != null && patrolLocations.Length > 0 
                ? patrolLocations[0] 
                : transform.position;
            
            float totalDistance = Vector3.Distance(startPosition, escapePoint);
            if (totalDistance > 0f)
            {
                float currentDistance = Vector3.Distance(transform.position, escapePoint);
                float traveledDistance = totalDistance - currentDistance;
                float currentProgress = Mathf.Clamp01(traveledDistance / totalDistance);

                if (Mathf.Abs(currentProgress - lastEscapeProgress) > 0.05f)
                {
                    OnEscapeProgressChanged?.Invoke(currentProgress);
                    lastEscapeProgress = currentProgress;
                }
            }
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

        // 從組件讀取基礎值（如果基類尚未設定）
        // 注意：由於 TargetDetection 現在使用 currentViewRange/currentViewAngle，
        // 我們需要確保基礎值在 TargetDetection 初始化時設定
        if (targetDetection != null)
        {
            // 如果基礎值未設定，使用預設值
            if (baseViewRange <= 0f) baseViewRange = 8f; // Target 的預設視野範圍
            if (baseViewAngle <= 0f) baseViewAngle = 90f; // Target 的預設視野角度
        }
        
        if (targetMovement != null)
        {
            // 如果基礎速度未設定，使用預設值
            if (baseSpeed <= 0f) baseSpeed = 2f; // Target 的預設基礎速度
        }
        
        // 初始化 TargetDetection 的當前視野範圍和角度（使用基礎值）
        if (targetDetection != null)
        {
            targetDetection.SetDetectionParameters(baseViewRange, baseViewAngle, targetDetection.ChaseRange);
        }
    }

    /// <summary>
    /// 初始化目標（由 EntityManager 呼叫）
    /// </summary>
    public void Initialize(Transform playerTarget, Vector3 escapePointPosition)
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
            Debug.LogWarning($"{gameObject.name}: TargetAIHandler is null during Initialize, adding component...");
            aiHandler = gameObject.AddComponent<TargetAIHandler>();
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
            aiHandler.Initialize(escapePointPosition, escapeSpeed);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Failed to initialize TargetAIHandler!");
        }

        if (targetDetection != null)
        {
            targetDetection.SetTarget(playerTarget);
        }

        // 初始化patrol locations
        InitializePatrolLocations();

        // 設定目標位置到第一個location
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            transform.position = patrolLocations[0];
            cachedPosition = patrolLocations[0];
        }

        if (targetStateMachine != null)
        {
            targetStateMachine.ChangeState(TargetState.Stay);
        }

        isInitialized = true;
        lastAIUpdateTime = Time.time + Random.Range(0f, aiUpdateInterval); // 錯開初始更新時間
        
        Debug.Log($"{gameObject.name}: Target initialized successfully. Active: {gameObject.activeSelf}, InHierarchy: {gameObject.activeInHierarchy}");
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
        if (entityHealth == null) return;
        
        // 應用傷害減少（從 EntityStats 獲取）
        float damageReduction = entityStats != null ? entityStats.DamageReduction : 0f;
        entityHealth.SetDamageReduction(damageReduction);
        
        // 使用 EntityHealth 處理傷害（死亡事件會自動觸發 Die()）
        entityHealth.TakeDamage(damage, source, gameObject.name);
    }
    
    /// <summary>
    /// 獲取實體類型（實現 IEntity 接口）
    /// </summary>
    public EntityManager.EntityType GetEntityType()
    {
        return EntityManager.EntityType.Target;
    }

    /// <summary>
    /// 目標死亡處理（覆寫基類方法，處理 Target 特定邏輯）
    /// </summary>
    protected override void OnDeath()
    {
        // 檢查是否已經處理過死亡（避免重複處理）
        if (targetStateMachine != null && targetStateMachine.CurrentState == TargetState.Dead)
        {
            return;
        }

        // 改變狀態為 Dead
        targetStateMachine?.ChangeState(TargetState.Dead);

        // 通知外部（觸發 Target 特定事件）
        OnTargetDied?.Invoke(this);
        
        Debug.Log($"{gameObject.name}: Target died, dropping all items");
        
        // 注意：基類的 Die() 會自動調用 DropAllItems()，無需在此處調用
    }

    /// <summary>
    /// 設定巡邏點
    /// </summary>
        public void SetPatrolPoints(Transform[] points)
    {
        targetMovement?.SetPatrolPoints(points);
    }

    /// <summary>
    /// 設定patrol locations（用於初始位置）
    /// </summary>
    public void SetPatrolLocations(Vector3[] locations)
    {
        patrolLocations = locations;
        currentPatrolIndex = 0;
    }
    
    /// <summary>
    /// 設定逃亡點
    /// </summary>
    public void SetEscapePoint(Vector3 escapePointPosition)
    {
        if (aiHandler != null)
        {
            aiHandler.SetEscapePoint(escapePointPosition);
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
    public void ForceChangeState(TargetState newState)
    {
        stateMachine?.ChangeState(newState);
    }

    /// <summary>
    /// 目標不需要攻擊功能
    /// </summary>
    public bool TryAttackPlayer(Transform playerTransform)
    {
        // 目標不攻擊，總是返回 false
        return false;
    }

    /// <summary>
    /// 設定FOV倍數（用於危險等級調整）
    /// </summary>
    public void SetFovMultiplier(float multiplier)
    {
        if (targetDetection != null)
        {
            // 這裡需要根據TargetDetection的實際API來調整
            // 假設有SetViewRange方法
            // targetDetection.SetViewRange(targetDetection.ViewRange * multiplier);
            Debug.Log($"{gameObject.name}: FOV倍數設定為 {multiplier}");
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
        if (targetDetection != null)
        {
            float newViewRange = BaseViewRange * viewRangeMultiplier;
            float newViewAngle = BaseViewAngle * viewAngleMultiplier;
            targetDetection.SetDetectionParameters(newViewRange, newViewAngle, targetDetection.ChaseRange);
        }
        
        // 更新移動速度（使用基類的基礎數值）
        if (targetMovement != null)
        {
            float newSpeed = BaseSpeed * speedMultiplier;
            targetMovement.SetSpeed(newSpeed);
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
        if (targetMovement != null)
        {
            float newSpeed = BaseSpeed * multiplier;
            targetMovement.SetSpeed(newSpeed);
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
    
    /// <summary>
    /// 獲取傷害減少值（用於計算實際傷害）
    /// </summary>
    public float GetDamageReduction()
    {
        return entityStats != null ? entityStats.GetDamageReduction() : 0f;
    }

    /// <summary>
    /// 通知目標玩家的位置（由外部系統呼叫，如槍聲警報）
    /// 根據 target_ai.md：視野外且不在 Escape 狀態時，不應對槍聲做出反應
    /// </summary>
    /// <param name="playerPosition">玩家位置</param>
    public void NotifyPlayerPosition(Vector2 playerPosition)
    {
        if (entityHealth != null && entityHealth.IsDead) return;

        // 檢查是否應該對槍聲做出反應（考慮攝影機剔除）
        // 根據 target_ai.md：視野外且不在 Escape 狀態時，不應對外在事物做出反應
        if (targetDetection != null && !targetDetection.ShouldUpdateAI())
        {
            // 視野外且不在 Escape 狀態，不反應
            return;
        }

        // 如果當前是停留狀態，開始逃亡
        if (targetStateMachine != null && targetStateMachine.CurrentState == TargetState.Stay)
        {
            if (aiHandler != null)
            {
                aiHandler.StartEscape();
            }
            Debug.Log($"{gameObject.name}: 聽到槍聲，開始逃亡");
        }
    }

    #endregion

    #region AI 邏輯

    // 注意：AI 邏輯已移至 TargetAIHandler 組件
    // 這裡保留區域標記以維持代碼結構

    #endregion

    #region 事件處理

    private void OnStateChanged(TargetState oldState, TargetState newState)
    {
        // 處理狀態轉換的特殊邏輯
        switch (newState)
        {
            case TargetState.Dead:
                HandleDeathState();
                break;

            case TargetState.Escape:
                // 可以在此處播放逃亡音效或動畫
                Debug.Log($"{gameObject.name}: 開始逃亡！");
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

    /// <summary>
    /// 處理目標死亡（由 EntityHealth.OnEntityDied 調用）
    /// </summary>
    private void HandleTargetDied()
    {
        Debug.LogWarning($"[Target] {gameObject.name} 已死亡！");
        
        // 清除地圖上的逃亡路徑
        MapUIManager mapUI = FindFirstObjectByType<MapUIManager>();
        if (mapUI != null)
        {
            mapUI.HideEscapePoint();
            Debug.Log($"[Target] {gameObject.name} 死亡，已清除地圖上的逃亡路徑");
        }
        
        // 觸發事件通知外部系統（WinConditionManager）
        OnTargetDied?.Invoke(this);
    }

    /// <summary>
    /// 處理目標到達逃亡點（由 TargetAIHandler 調用）
    /// </summary>
    private void HandleTargetReachedEscapePoint(Target target)
    {
        Debug.LogWarning($"[Target] {gameObject.name} 已到達逃亡點！");
        
        // 觸發事件通知外部系統（WinConditionManager，GameManager）
        OnTargetReachedEscapePoint?.Invoke(this);
    }

    #endregion

    #region 清理

    protected override void OnDestroy()
    {
        // 調用基類的 OnDestroy 進行基礎清理
        base.OnDestroy();
        
        // 處理 Target 特定的清理邏輯
        if (targetStateMachineInstance != null)
        {
            targetStateMachineInstance.OnStateChanged -= OnStateChanged;
        }
        
        // 取消訂閱 AI Handler 事件
        if (aiHandler != null)
        {
            aiHandler.OnTargetReachedEscapePoint -= HandleTargetReachedEscapePoint;
        }
        
        // 取消訂閱 EntityHealth 事件
        if (entityHealth != null)
        {
            entityHealth.OnEntityDied -= HandleTargetDied;
        }
    }

    #endregion

    #region 除錯輔助

    private void OnDrawGizmosSelected()
    {
        // 顯示逃亡點
        if (aiHandler != null)
        {
            Vector3 escapePoint = aiHandler.GetEscapePoint();
            if (escapePoint != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(escapePoint, 0.5f);
                Gizmos.DrawLine(transform.position, escapePoint);
            }
        }

        // 顯示初始位置（patrol locations）
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