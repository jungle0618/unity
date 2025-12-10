using UnityEngine;

/// <summary>
/// 敵人偵測系統（繼承基礎偵測組件）
/// - 視野偵測、距離判斷
/// - 可選：自動面向目標
/// - 與 DangerousManager 串接危險係數更新
/// 
/// 【封裝說明】
/// 此類的屬性（如 viewRange, viewAngle, chaseRange）應通過 Enemy 類的公共方法進行修改，而不是直接訪問。
/// 注意：chaseRange 是 Enemy 專屬功能，不在基類中。
/// 正確方式：使用 Enemy.UpdateDangerLevelStats() 來更新偵測參數。
/// </summary>
public class EnemyDetection : BaseDetection
{
    [Header("偵測參數")]
    [SerializeField] private EnemyStateMachine stateMachine;
    [Tooltip("追擊範圍（與視野範圍獨立）")]
    [SerializeField] private float chaseRange = 15f;
    
    // 注意：viewRange 和 viewAngle 不再在這裡定義，應從 Enemy 的 BaseViewRange 和 BaseViewAngle 獲取
    // 實際視野範圍 = Enemy.BaseViewRange * 當前視野範圍倍數

    [Header("障礙物偵測")]
    // 圖層遮罩已移至 BaseDetection
    [SerializeField] private bool useRaycastDetection = false;

    [Header("旋轉設定")]
    [SerializeField] private bool lookAtTarget = false; // 是否自動面向玩家

    [Header("性能優化")]
    [SerializeField] private bool enableCameraCulling = true; // 啟用攝影機剔除
    [SerializeField] private float cameraCullMargin = 2f; // 攝影機剔除邊距

    public float ViewRange => GetViewRange();
    public float ViewAngle => GetViewAngle();
    public float ChaseRange => chaseRange;
    
    /// <summary>
    /// 獲取當前視野範圍（使用當前應用的視野範圍，由 Enemy.UpdateDangerLevelStats 設定）
    /// </summary>
    private float GetViewRange()
    {
        return currentViewRange;
    }
    
    /// <summary>
    /// 獲取當前視野角度（使用當前應用的視野角度，由 Enemy.UpdateDangerLevelStats 設定）
    /// </summary>
    private float GetViewAngle()
    {
        return currentViewAngle;
    }

    private DangerousManager dangerousManager;
    private Camera mainCamera;
    private Player player; // 快取 Player 引用以檢查蹲下狀態
    private Enemy enemy; // 快取 Enemy 引用以獲取 nearViewRange

    /// <summary>
    /// 設定狀態機引用（由 Enemy 調用）
    /// </summary>
    public void SetStateMachine(EnemyStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    protected override void Awake()
    {
        base.Awake(); // 調用基類 Awake
        dangerousManager = FindFirstObjectByType<DangerousManager>();
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // 快取 Player 和 Enemy 引用
        player = FindFirstObjectByType<Player>();
        enemy = GetComponent<Enemy>();
    }
    
    /// <summary>
    /// 覆寫基類方法，根據玩家蹲下狀態決定遮罩
    /// 當玩家蹲下時：walls + objects 都會遮擋視線
    /// 當玩家站立時：只有 walls 會遮擋視線
    /// </summary>
    protected override LayerMask GetObstacleLayerMask()
    {
        if (player != null && player.IsSquatting)
        {
            // 玩家蹲下時，walls 和 objects 都會遮擋視線
            return wallsLayerMask | objectsLayerMask;
        }
        else
        {
            // 玩家站立時，只有 walls 會遮擋視線（可以透過 objects 看到）
            return wallsLayerMask;
        }
    }

    private void Update()
    {
        // 每幀彙報距離、視野半徑與可視狀態給 DangerousManager
        if (dangerousManager == null) return;

        // 檢查是否應該進行偵測
        if (!ShouldPerformDetection())
        {
            // 如果不需要偵測，報告最大距離和不可見狀態
            dangerousManager.ReportEnemyPerception(float.MaxValue, 0f, false);
            return;
        }

        bool canSee = CanSeeCurrentTarget();
        float distance = GetDistanceToTarget();
        float currentViewRange = GetViewRange(); // 獲取當前視野半徑（最終值）
        
        // 報告距離、視野半徑和可見性
        dangerousManager.ReportEnemyPerception(distance, currentViewRange, canSee);
    }
    
    /// <summary>
    /// 判斷是否應該增加危險等級（基於區域和玩家狀態）
    /// </summary>
    private bool ShouldIncreaseDangerLevel(bool canSeePlayer)
    {
        // 如果看不到玩家，使用距離判定（保持原有邏輯）
        if (!canSeePlayer)
        {
            // 看不到時，仍然根據距離變化危險值，但不基於可見性
            return false; // 返回 false 表示不要報告 "看見"
        }
        
        // 【新增】檢查是否啟用 Guard Area System
        // 如果停用，使用原始行為（看到就增加危險）
        if (GameSettings.Instance != null && !GameSettings.Instance.UseGuardAreaSystem)
        {
            return true; // 原始行為：看到就增加危險
        }
        
        // 看得到玩家時，才判斷是否應該增加危險
        
        // 如果沒有目標，不增加危險
        if (target == null) return false;
        
        // 檢查玩家位置所在區域
        Vector3 playerPosition = target.position;
        
        // 如果 AreaManager 不存在，默認為 Guard Area 行為（向後兼容）
        if (AreaManager.Instance == null)
        {
            return true;
        }
        
        // 如果在 Guard Area，始終增加危險
        if (AreaManager.Instance.IsInGuardArea(playerPosition))
        {
            Debug.Log($"[EnemyDetection] Player in GUARD AREA - will attack regardless of weapon");
            return true;
        }
        
        // 在 Safe Area 中，檢查玩家是否持有武器
        Player targetPlayer = target.GetComponent<Player>();
        if (targetPlayer == null) return true; // 找不到 Player 組件，默認增加危險
        
        ItemHolder playerItemHolder = targetPlayer.GetComponent<ItemHolder>();
        if (playerItemHolder == null) return true; // 找不到 ItemHolder，默認增加危險
        
        // 檢查玩家是否持有武器
        bool playerHasWeapon = playerItemHolder.IsCurrentItemWeapon;
        
        if (!playerHasWeapon)
        {
            Debug.Log($"[EnemyDetection] Player in SAFE AREA with EMPTY HANDS - will NOT increase danger");
        }
        else
        {
            Debug.Log($"[EnemyDetection] Player in SAFE AREA with WEAPON - will increase danger");
        }
        
        // Safe Area 邏輯：只有當玩家持有武器時才增加危險
        return playerHasWeapon;
    }

    // CanSeePlayer() 已由基類 BaseDetection 統一提供

    /// <summary>
    /// 檢查是否可以看到指定目標（覆寫基類抽象方法）
    /// 檢查兩個視野範圍：
    /// 1. 主要視野（有角度限制）
    /// 2. 360度視野（全方向，近距離，僅在危險等級最高或Chase/Search狀態時有效）
    /// </summary>
    public override bool CanSeeTarget(Vector2 targetPos)
    {
        // 檢查是否應該進行偵測
        if (!ShouldPerformDetection()) return false;
        
        Vector2 currentPos = transform.position;
        Vector2 dirToTarget = targetPos - currentPos;
        float distanceToTarget = dirToTarget.magnitude;

        // 首先檢查360度視野（近距離全方向，僅在特定條件下有效）
        if (ShouldUseNearViewRange())
        {
            float nearViewRange = GetNearViewRange();
            if (distanceToTarget <= nearViewRange)
            {
                // 在360度視野範圍內，只需要檢查遮擋
                if (!IsBlockedByObstacle(currentPos, targetPos))
                {
                    // 自動面向目標（如果啟用）
                    if (lookAtTarget && dirToTarget.magnitude > 0.1f)
                    {
                        LookAtTarget(dirToTarget);
                    }
                    return true;
                }
            }
        }

        // 如果不在360度視野內，檢查主要視野（有角度限制）
        float currentViewRange = GetViewRange();
        if (distanceToTarget > currentViewRange)
            return false;

        // 角度檢查：以 transform.rotation 為基準（使用從 Enemy 獲取的視野角度）
        float currentViewAngle = GetViewAngle();
        float angle = Vector2.Angle(transform.right, dirToTarget.normalized);
        if (angle > currentViewAngle * 0.5f)
            return false;

        // 障礙物檢查
        if (useRaycastDetection && IsBlockedByObstacle(currentPos, targetPos))
            return false;

        // 自動面向目標（只在非追擊狀態時，且沒有使用路徑規劃時）
        if (lookAtTarget && dirToTarget.magnitude > 0.1f && !IsInChaseState() && !IsUsingPathfinding())
        {
            LookAtTarget(dirToTarget);
        }

        return true;
    }

    // 當前應用的視野範圍和角度（由 Enemy.UpdateDangerLevelStats 設定）
    private float currentViewRange = 8f;
    private float currentViewAngle = 90f;

    /// <summary>
    /// 設定偵測參數（覆寫基類方法）
    /// 注意：此方法設置的是當前應用的視野範圍和角度（基礎值 × 倍數）
    /// </summary>
    public override void SetDetectionParameters(params object[] parameters)
    {
        if (parameters.Length >= 2)
        {
            currentViewRange = (float)parameters[0];
            currentViewAngle = (float)parameters[1];
        }
        if (parameters.Length >= 3)
        {
            chaseRange = (float)parameters[2]; // 更新追擊範圍
        }
    }

    /// <summary>
    /// 設定偵測參數（保留原有方法以維持向後兼容）
    /// 注意：此方法設置的是當前應用的視野範圍和角度（基礎值 × 倍數）
    /// </summary>
    public void SetDetectionParameters(float newViewRange, float newViewAngle, float newChaseRange)
    {
        currentViewRange = newViewRange;
        currentViewAngle = newViewAngle;
        chaseRange = newChaseRange; // 更新追擊範圍
    }

    /// <summary>
    /// 檢查目標是否超出追擊範圍（僅 Enemy 使用）
    /// </summary>
    public bool IsTargetOutOfChaseRange()
    {
        if (!HasValidTarget()) return true;
        return Vector2.Distance(transform.position, GetTarget().position) > chaseRange;
    }

    // IsBlockedByObstacle 已移至 BaseDetection
    // SetTarget, GetDirectionToTarget, GetDistanceToTarget, HasValidTarget, ClearTarget 已由基類 BaseDetection 統一提供

    public void SetRaycastDetection(bool enabled) => useRaycastDetection = enabled;

    /// <summary>
    /// 面向目標方向
    /// </summary>
    private void LookAtTarget(Vector2 directionToTarget)
    {
        if (directionToTarget.magnitude < 0.1f) return;

        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 設定是否自動面向目標
    /// </summary>
    public void SetLookAtTarget(bool enabled) => lookAtTarget = enabled;

    /// <summary>
    /// 設定視野方向（覆寫基類方法，用於巡邏時跟隨移動方向）
    /// </summary>
    public override void SetViewDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 獲取當前視野方向（覆寫基類方法）
    /// </summary>
    public override Vector2 GetViewDirection()
    {
        return transform.right;
    }

    /// <summary>
    /// 檢查是否在追擊狀態
    /// </summary>
    private bool IsInChaseState()
    {
        if (stateMachine == null) return false;
        return stateMachine.CurrentState == EnemyState.Chase;
    }

    /// <summary>
    /// 檢查是否正在使用路徑規劃
    /// </summary>
    private bool IsUsingPathfinding()
    {
        if (stateMachine == null) return false;
        // 在追擊或搜索狀態時使用路徑規劃
        return stateMachine.CurrentState == EnemyState.Chase || 
               stateMachine.CurrentState == EnemyState.Search;
    }

    /// <summary>
    /// 檢查是否應該使用360度視野（nearViewRange）
    /// 條件：危險等級最高（Critical）或處於 Chase/Search 狀態
    /// </summary>
    private bool ShouldUseNearViewRange()
    {
        // 檢查是否處於 Chase 或 Search 狀態
        if (stateMachine != null)
        {
            EnemyState currentState = stateMachine.CurrentState;
            if (currentState == EnemyState.Chase || currentState == EnemyState.Search)
            {
                return true;
            }
        }

        // 檢查危險等級是否為最高（Critical）
        if (dangerousManager != null)
        {
            return dangerousManager.CurrentDangerLevelType == DangerousManager.DangerLevel.Critical;
        }

        return false;
    }

    /// <summary>
    /// 獲取近距離360度視野範圍（從 Enemy 獲取）
    /// </summary>
    private float GetNearViewRange()
    {
        return enemy != null ? enemy.NearViewRange : 2f; // 默認值
    }

    /// <summary>
    /// 檢查敵人是否在攝影機視野外
    /// </summary>
    private bool IsOutsideCameraView()
    {
        if (!enableCameraCulling || mainCamera == null) return false;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        
        // 檢查是否在攝影機視野外（加上邊距）
        return screenPos.x < -cameraCullMargin || 
               screenPos.x > Screen.width + cameraCullMargin ||
               screenPos.y < -cameraCullMargin || 
               screenPos.y > Screen.height + cameraCullMargin ||
               screenPos.z < 0; // 在攝影機後方
    }

    /// <summary>
    /// 檢查敵人是否在攝影機視野內
    /// </summary>
    public bool IsInsideCameraView()
    {
        if (!enableCameraCulling || mainCamera == null) return true; // 如果未啟用剔除，視為在視野內

        return !IsOutsideCameraView();
    }

    /// <summary>
    /// 檢查是否應該進行偵測
    /// </summary>
    private bool ShouldPerformDetection()
    {
        // 如果啟用攝影機剔除且敵人在攝影機外
        if (enableCameraCulling && IsOutsideCameraView())
        {
            // 只有在追擊或搜索狀態時才繼續偵測
            if (stateMachine != null)
            {
                return stateMachine.CurrentState == EnemyState.Chase ||
                       stateMachine.CurrentState == EnemyState.Search;
            }
        }
        
        return true; // 其他情況都進行偵測
    }


}