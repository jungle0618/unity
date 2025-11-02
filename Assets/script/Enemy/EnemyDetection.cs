using UnityEngine;

/// <summary>
/// 敵人偵測系統（繼承基礎偵測組件）
/// - 視野偵測、距離判斷
/// - 可選：自動面向目標
/// - 與 DangerousManager 串接危險係數更新
/// </summary>
public class EnemyDetection : BaseDetection
{
    [Header("偵測參數")]
    [SerializeField] private EnemyStateMachine stateMachine;
    [SerializeField] private float viewRange = 8f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float chaseRange = 15f;

    [Header("障礙物偵測")]
    // 圖層遮罩已移至 BaseDetection
    [SerializeField] private bool useRaycastDetection = false;

    [Header("旋轉設定")]
    [SerializeField] private bool lookAtTarget = false; // 是否自動面向玩家

    [Header("性能優化")]
    [SerializeField] private bool enableCameraCulling = true; // 啟用攝影機剔除
    [SerializeField] private float cameraCullMargin = 2f; // 攝影機剔除邊距

    public float ViewRange => viewRange;
    public float ViewAngle => viewAngle;
    public float ChaseRange => chaseRange;

    private DangerousManager dangerousManager;
    private Camera mainCamera;
    private Player player; // 快取 Player 引用以檢查蹲下狀態

    protected override void Awake()
    {
        base.Awake(); // 調用基類 Awake
        dangerousManager = FindFirstObjectByType<DangerousManager>();
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // 快取 Player 引用
        player = FindFirstObjectByType<Player>();
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
        // 每幀彙報距離與可視狀態給 DangerousManager
        if (dangerousManager == null) return;

        // 檢查是否應該進行偵測
        if (!ShouldPerformDetection())
        {
            // 如果不需要偵測，報告最大距離和不可見狀態
            dangerousManager.ReportEnemyPerception(float.MaxValue, false);
            return;
        }

        bool canSee = CanSeeCurrentTarget();
        float distance = GetDistanceToTarget();
        dangerousManager.ReportEnemyPerception(distance, canSee);
    }

    /// <summary>
    /// 設定偵測目標（覆寫基類方法）
    /// </summary>
    public override void SetTarget(Transform playerTarget)
    {
        base.SetTarget(playerTarget);
    }

    /// <summary>
    /// 檢查是否可以看到玩家（保留向後兼容的別名）
    /// </summary>
    public bool CanSeePlayer()
    {
        return CanSeeCurrentTarget();
    }

    /// <summary>
    /// 檢查是否可以看到指定目標（覆寫基類抽象方法）
    /// </summary>
    public override bool CanSeeTarget(Vector2 targetPos)
    {
        // 檢查是否應該進行偵測
        if (!ShouldPerformDetection()) return false;
        
        Vector2 currentPos = transform.position;
        Vector2 dirToTarget = targetPos - currentPos;

        // 距離檢查
        if (dirToTarget.magnitude > viewRange)
            return false;

        // 角度檢查：以 transform.rotation 為基準
        float angle = Vector2.Angle(transform.right, dirToTarget.normalized);
        if (angle > viewAngle * 0.5f)
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

    /// <summary>
    /// 設定偵測參數（覆寫基類方法）
    /// </summary>
    public override void SetDetectionParameters(params object[] parameters)
    {
        if (parameters.Length >= 3)
        {
            viewRange = (float)parameters[0];
            viewAngle = (float)parameters[1];
            chaseRange = (float)parameters[2];
        }
    }

    /// <summary>
    /// 設定偵測參數（保留原有方法以維持向後兼容）
    /// </summary>
    public void SetDetectionParameters(float newViewRange, float newViewAngle, float newChaseRange)
    {
        viewRange = newViewRange;
        viewAngle = newViewAngle;
        chaseRange = newChaseRange;
    }

    /// <summary>
    /// 檢查目標是否超出追擊範圍
    /// </summary>
    public bool IsTargetOutOfChaseRange()
    {
        if (!HasValidTarget()) return true;
        return Vector2.Distance(transform.position, GetTarget().position) > chaseRange;
    }

    /// <summary>
    /// 獲取朝向目標的方向（覆寫基類方法）
    /// </summary>
    public override Vector2 GetDirectionToTarget()
    {
        return base.GetDirectionToTarget();
    }

    /// <summary>
    /// 獲取到目標的距離（覆寫基類方法）
    /// </summary>
    public override float GetDistanceToTarget()
    {
        return base.GetDistanceToTarget();
    }

    // IsBlockedByObstacle 已移至 BaseDetection

    public void SetRaycastDetection(bool enabled) => useRaycastDetection = enabled;

    /// <summary>
    /// 檢查是否有有效的目標（覆寫基類方法）
    /// </summary>
    public override bool HasValidTarget()
    {
        return base.HasValidTarget();
    }

    /// <summary>
    /// 清除目標（覆寫基類方法）
    /// </summary>
    public override void ClearTarget()
    {
        base.ClearTarget();
    }

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