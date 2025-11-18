using UnityEngine;

/// <summary>
/// Enemy AI 邏輯處理組件
/// 負責處理所有 AI 狀態邏輯和移動決策
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyAIHandler : MonoBehaviour
{
    private Enemy enemy;
    private EnemyStateMachine enemyStateMachine;
    private EnemyMovement enemyMovement;
    private EnemyDetection enemyDetection;
    private ItemHolder itemHolder;
    
    // 當前的移動目標和狀態
    private Vector2 currentMoveTarget;
    private bool shouldMove = false;
    private enum MoveType { None, Patrol, Chase, ChaseWithRotation, Return }
    private MoveType currentMoveType = MoveType.None;
    
    // Patrol locations（從 Enemy 獲取）
    private Vector3[] patrolLocations;
    private int currentPatrolIndex = 0;
    
    // 追擊相關變數
    private Vector2 lastSeenPlayerPosition;
    private bool hasLastSeenPosition = false;
    private float lastSeenTime = 0f;
    private float searchTime = 3f; // 在最後看到位置搜索的時間
    private bool hasReachedSearchLocation = false; // 是否已到達搜索位置
    
    // AI 參數（從 Enemy 獲取）
    private float attackDetectionRange = 3f;
    
    // 快取數據（從 Enemy 獲取）
    private Vector2 cachedPosition;
    private Vector2 cachedDirectionToPlayer;
    private bool cachedCanSeePlayer;
    
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError($"{gameObject.name}: EnemyAIHandler requires Enemy component!");
            enabled = false;
            return;
        }
    }
    
    private void Start()
    {
        // 獲取組件引用
        enemyStateMachine = enemy.StateMachine as EnemyStateMachine;
        enemyMovement = enemy.Movement as EnemyMovement;
        enemyDetection = enemy.Detection as EnemyDetection;
        itemHolder = enemy.ItemHolder;
    }
    
    /// <summary>
    /// 初始化 AI Handler（由 Enemy 調用）
    /// </summary>
    public void Initialize(float attackRange, float searchTime)
    {
        this.attackDetectionRange = attackRange;
        this.searchTime = searchTime;
    }
    
    /// <summary>
    /// 設定巡邏點
    /// </summary>
    public void SetPatrolLocations(Vector3[] locations)
    {
        patrolLocations = locations;
        currentPatrolIndex = 0;
    }
    
    /// <summary>
    /// 獲取巡邏點（用於調試和外部訪問）
    /// </summary>
    public Vector3[] GetPatrolLocations()
    {
        return patrolLocations;
    }
    
    /// <summary>
    /// 獲取當前巡邏索引
    /// </summary>
    public int GetCurrentPatrolIndex()
    {
        return currentPatrolIndex;
    }
    
    /// <summary>
    /// 更新快取數據（由 Enemy 調用）
    /// </summary>
    public void UpdateCachedData(Vector2 position, Vector2 directionToPlayer, bool canSeePlayer)
    {
        cachedPosition = position;
        cachedDirectionToPlayer = directionToPlayer;
        cachedCanSeePlayer = canSeePlayer;
    }
    
    /// <summary>
    /// AI 決策更新（使用間隔，減少 CPU 負載）
    /// 根據 enemy_ai.md：當不在 Chase 或 Search 狀態且不在攝影機範圍內時，不執行 AI 更新
    /// </summary>
    public void UpdateAIDecision()
    {
        if (enemyStateMachine == null || enemyDetection == null || enemyMovement == null) return;

        // 檢查是否應該更新 AI（考慮攝影機剔除）
        // 根據 enemy_ai.md：當不在 Chase 或 Search 狀態且不在攝影機範圍內時，不執行 AI 更新
        if (!enemyDetection.ShouldUpdateAI())
        {
            // 視野外且不在 Chase/Search 狀態，跳過 AI 更新
            // 移動邏輯仍會在 ExecuteMovement() 中執行
            return;
        }

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

    /// <summary>
    /// 執行移動（每幀更新，確保移動流暢）
    /// </summary>
    public void ExecuteMovement()
    {
        if (!shouldMove || enemyMovement == null) return;

        switch (currentMoveType)
        {
            case MoveType.Chase:
                enemyMovement.ChaseTarget(currentMoveTarget);
                break;

            case MoveType.ChaseWithRotation:
                enemyMovement.ChaseTargetWithRotation(currentMoveTarget, enemyDetection);
                break;

            case MoveType.Return:
                enemyMovement.MoveTowardsWithPathfinding(currentMoveTarget, 1f);
                break;
        }
    }
    
    /// <summary>
    /// 記錄玩家最後位置（用於搜索狀態）
    /// </summary>
    public void RecordLastSeenPosition(Vector2 position)
    {
        lastSeenPlayerPosition = position;
        hasLastSeenPosition = true;
        lastSeenTime = Time.time;
    }
    
    /// <summary>
    /// 清除搜索狀態（用於狀態轉換）
    /// </summary>
    public void ClearSearchState()
    {
        hasLastSeenPosition = false;
        hasReachedSearchLocation = false;
        shouldMove = false;
    }
    
    /// <summary>
    /// 判斷是否應該追擊玩家（基於區域類型和玩家狀態）
    /// Guard Area: 始終追擊
    /// Safe Area: 只有當玩家持有武器或危險等級觸發時才追擊
    /// </summary>
    private bool ShouldChasePlayer()
    {
        // 如果沒有偵測組件或目標，不追擊
        if (enemyDetection == null) return false;
        Transform target = enemyDetection.GetTarget();
        if (target == null) return false;
        
        // 【新增】檢查是否啟用 Guard Area System
        // 如果停用，使用原始行為（總是追擊）
        if (GameSettings.Instance != null && !GameSettings.Instance.UseGuardAreaSystem)
        {
            Debug.Log($"[EnemyAI] Guard Area System disabled - will always chase when seeing player");
            return true; // 原始行為：看到就追
        }
        
        // 檢查玩家位置所在區域
        Vector3 playerPosition = target.position;
        
        // 如果 AreaManager 不存在，默認為 Guard Area 行為（向後兼容）
        if (AreaManager.Instance == null)
        {
            return true;
        }
        
        // 如果在 Guard Area，始終追擊
        if (AreaManager.Instance.IsInGuardArea(playerPosition))
        {
            Debug.Log($"[EnemyAI] Player in GUARD AREA - will chase");
            return true;
        }
        
        // 在 Safe Area 中，檢查玩家是否持有武器
        Player player = target.GetComponent<Player>();
        if (player == null) return true; // 找不到 Player 組件，默認追擊
        
        ItemHolder playerItemHolder = player.GetComponent<ItemHolder>();
        if (playerItemHolder == null) return true; // 找不到 ItemHolder，默認追擊
        
        // 檢查玩家是否持有武器
        bool playerHasWeapon = playerItemHolder.IsCurrentItemWeapon;
        
        // 檢查是否危險等級被觸發
        bool isDangerTriggered = false;
        DangerousManager dangerManager = DangerousManager.Instance;
        if (dangerManager != null)
        {
            // 危險等級 > Safe 時視為觸發
            isDangerTriggered = dangerManager.CurrentDangerLevelType != DangerousManager.DangerLevel.Safe;
        }
        
        // Safe Area 邏輯：玩家持有武器 OR 危險等級觸發 → 追擊
        bool shouldChase = playerHasWeapon || isDangerTriggered;
        
        if (!shouldChase)
        {
            Debug.Log($"[EnemyAI] Player in SAFE AREA with EMPTY HANDS and danger is SAFE - will NOT chase");
        }
        else if (playerHasWeapon)
        {
            Debug.Log($"[EnemyAI] Player in SAFE AREA with WEAPON - will chase");
        }
        else if (isDangerTriggered)
        {
            Debug.Log($"[EnemyAI] Player in SAFE AREA but danger is TRIGGERED - will chase");
        }
        
        return shouldChase;
    }
    
    /// <summary>
    /// 重置巡邏索引
    /// </summary>
    public void ResetPatrolIndex()
    {
        currentPatrolIndex = 0;
    }

    private void HandlePatrolState()
    {
        if (cachedCanSeePlayer)
        {
            enemyStateMachine?.ChangeState(EnemyState.Alert);
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
                if (movementDirection.magnitude > 0.1f)
                {
                    enemyDetection?.SetViewDirection(movementDirection);
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
                if (movementDirection.magnitude > 0.1f)
                {
                    enemyDetection?.SetViewDirection(movementDirection);
                }
            }
        }
    }

    private void HandleAlertState()
    {
        if (cachedCanSeePlayer)
        {
            // 【新增】檢查是否應該追擊玩家（基於區域和玩家狀態）
            if (ShouldChasePlayer())
            {
                enemyStateMachine?.ChangeState(EnemyState.Chase);
            }
            // 如果不應該追擊，保持在 Alert 狀態
        }
        else if (enemyStateMachine?.IsAlertTimeUp() == true)
        {
            enemyStateMachine?.ChangeState(EnemyState.Patrol);
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
                    if (movementDirection.magnitude > 0.1f)
                    {
                        enemyDetection?.SetViewDirection(movementDirection);
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
        // 【新增】檢查是否應該繼續追擊
        if (cachedCanSeePlayer && !ShouldChasePlayer())
        {
            // 看到玩家但不應該追擊（例如：在安全區且玩家沒武器且危險等級安全）
            Debug.Log($"{gameObject.name}: Can see player but should NOT chase - returning to Alert");
            shouldMove = false;
            enemyStateMachine?.ChangeState(EnemyState.Alert);
            return;
        }
        
        // 檢查是否超出追擊範圍
        if (enemyDetection != null && enemyDetection.IsTargetOutOfChaseRange())
        {
            shouldMove = false;
            enemyStateMachine?.ChangeState(EnemyState.Return);
            return;
        }

        // 檢查是否撞牆或卡住，如果是則轉到搜索狀態
        if (enemyMovement != null && enemyMovement.IsStuckOrHittingWall())
        {
            Debug.Log($"{gameObject.name}: 追擊時卡住，轉到搜索狀態");
            shouldMove = false;
            hasReachedSearchLocation = false;
            hasLastSeenPosition = true;
            lastSeenTime = Time.time;
            enemyStateMachine?.ChangeState(EnemyState.Search);
            return;
        }

        if (cachedCanSeePlayer)
        {
            // 記錄玩家位置
            Transform target = enemyDetection?.GetTarget();
            if (target != null)
            {
                RecordLastSeenPosition(target.position);

                // 設定移動目標和類型（ExecuteMovement 會每幀執行）
                currentMoveTarget = target.position;
                currentMoveType = MoveType.Chase;
                shouldMove = true;
                
                // 設定敵人朝向玩家
                enemyDetection?.SetViewDirection(cachedDirectionToPlayer);

                // 更新武器朝向玩家
                itemHolder?.UpdateWeaponDirection(cachedDirectionToPlayer);

                // 檢查是否在攻擊偵測範圍內，然後嘗試攻擊
                // 使用 Enemy 的 GetEffectiveAttackRange() 來獲取實際攻擊範圍（支援槍械的遠距離攻擊）
                float effectiveAttackRange = enemy != null ? enemy.GetEffectiveAttackRange() : attackDetectionRange;
                float distanceToTarget = Vector2.Distance(cachedPosition, target.position);
                if (distanceToTarget <= effectiveAttackRange && enemy != null)
                {
                    enemy.TryAttackPlayer(target);
                }
            }
        }
        else
        {
            // 失去視線，轉到搜索狀態
            shouldMove = false;
            if (hasLastSeenPosition)
            {
                hasReachedSearchLocation = false;
                enemyStateMachine?.ChangeState(EnemyState.Search);
            }
            else
            {
                enemyStateMachine?.ChangeState(EnemyState.Alert);
            }
        }
    }

    private void HandleSearchState()
    {
        // 檢查是否超出追擊範圍
        if (enemyDetection?.IsTargetOutOfChaseRange() == true)
        {
            shouldMove = false;
            enemyStateMachine?.ChangeState(EnemyState.Return);
            return;
        }

        // 如果重新看到玩家，更新最後看到的位置並重新計算路徑
        if (cachedCanSeePlayer)
        {
            Transform target = enemyDetection?.GetTarget();
            if (target != null)
            {
                // 更新最後看到的位置
                RecordLastSeenPosition(target.position);
                
                // 清除路徑，強制重新計算到新位置的路徑
                enemyMovement?.ClearPath();
                
                Debug.Log($"{gameObject.name}: 搜索時看到玩家，更新目標位置到 {lastSeenPlayerPosition}");
            }
        }

        // 檢查是否還有最後看到的位置
        if (!hasLastSeenPosition)
        {
            Debug.Log($"{gameObject.name}: 沒有最後看到玩家的位置，轉到警戒狀態");
            shouldMove = false;
            enemyStateMachine?.ChangeState(EnemyState.Alert);
            return;
        }

        // 設定移動目標和類型（ExecuteMovement 會每幀執行）
        currentMoveTarget = lastSeenPlayerPosition;
        currentMoveType = MoveType.ChaseWithRotation;
        shouldMove = true;

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
                shouldMove = false;
                hasLastSeenPosition = false;
                hasReachedSearchLocation = false;
                enemyStateMachine?.ChangeState(EnemyState.Alert);
                return;
            }

            // 如果重新看到玩家，轉到追擊狀態
            if (cachedCanSeePlayer)
            {
                enemyStateMachine?.ChangeState(EnemyState.Chase);
                return;
            }
        }
    }

    private void HandleReturnState()
    {
        // 根據 enemy_ai.md：Return 狀態看到玩家時應該轉到 Chase
        if (cachedCanSeePlayer)
        {
            enemyStateMachine?.ChangeState(EnemyState.Chase);
            return;
        }

        Vector2 returnTarget;
        
        // 優先使用第一個patrol location作為返回目標
        if (patrolLocations != null && patrolLocations.Length > 0)
        {
            returnTarget = patrolLocations[0];
        }
        else
        {
            returnTarget = enemyMovement?.GetReturnTarget() ?? cachedPosition;
        }
        
        // 設定移動目標和類型（ExecuteMovement 會每幀執行）
        currentMoveTarget = returnTarget;
        currentMoveType = MoveType.Return;
        shouldMove = true;

        if (enemyMovement?.HasArrivedAt(returnTarget) == true)
        {
            // 重置patrol index到第一個location
            shouldMove = false;
            currentPatrolIndex = 0;
            enemyStateMachine?.ChangeState(EnemyState.Patrol);
        }
    }
}

