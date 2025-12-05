using UnityEngine;

/// <summary>
/// Target偵測系統（繼承基礎偵測組件）
/// - 視野偵測、距離判斷
/// - 可選：自動面向目標
/// - 與 DangerousManager 串接危險係數更新
/// 
/// 【封裝說明】
/// 此類的屬性（如 viewRange, viewAngle）應通過 Target 類的公共方法進行修改，而不是直接訪問。
/// 正確方式：使用 Target.UpdateDangerLevelStats() 來更新偵測參數。
/// </summary>
public class TargetDetection : BaseDetection
{
    [Header("偵測參數")]
    [SerializeField] private TargetStateMachine stateMachine;
    
    /// <summary>
    /// 設定狀態機引用（由 Target 調用）
    /// </summary>
    public void SetStateMachine(TargetStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }
    
    // 注意：viewRange 和 viewAngle 不再在這裡定義，應從 Target 的 BaseViewRange 和 BaseViewAngle 獲取
    // 實際視野範圍 = Target.BaseViewRange * 當前視野範圍倍數

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
    
    // 當前應用的視野範圍和角度（由 Target.UpdateDangerLevelStats 設定）
    private float currentViewRange = 8f;
    private float currentViewAngle = 90f;
    
    /// <summary>
    /// 獲取當前視野範圍（使用當前應用的視野範圍，由 Target.UpdateDangerLevelStats 設定）
    /// </summary>
    private float GetViewRange()
    {
        return currentViewRange;
    }
    
    /// <summary>
    /// 獲取當前視野角度（使用當前應用的視野角度，由 Target.UpdateDangerLevelStats 設定）
    /// </summary>
    private float GetViewAngle()
    {
        return currentViewAngle;
    }

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
        // Target 不需要向 DangerousManager 報告（只有 Enemy 需要）
        // 這裡可以保留空實現或移除
    }

    // CanSeePlayer() 已由基類 BaseDetection 統一提供

    /// <summary>
    /// 檢查是否可以看到指定目標（覆寫基類抽象方法）
    /// </summary>
    public override bool CanSeeTarget(Vector2 targetPos)
    {
        // 檢查是否應該進行偵測
        if (!ShouldPerformDetection()) return false;
        
        Vector2 currentPos = transform.position;
        Vector2 dirToTarget = targetPos - currentPos;

        // 距離檢查（使用當前應用的視野範圍）
        float currentViewRange = GetViewRange();
        if (dirToTarget.magnitude > currentViewRange)
            return false;

        // 角度檢查：以 transform.rotation 為基準（使用當前應用的視野角度）
        float currentViewAngle = GetViewAngle();
        float angle = Vector2.Angle(transform.right, dirToTarget.normalized);
        if (angle > currentViewAngle * 0.5f)
            return false;

        // 障礙物檢查
        if (useRaycastDetection && IsBlockedByObstacle(currentPos, targetPos))
            return false;

        // 自動面向目標（只在非逃亡狀態時，且沒有使用路徑規劃時）
        if (lookAtTarget && dirToTarget.magnitude > 0.1f && !IsInEscapeState() && !IsUsingPathfinding())
        {
            LookAtTarget(dirToTarget);
        }

        return true;
    }

    /// <summary>
    /// 設定偵測參數（覆寫基類方法）
    /// 注意：此方法設置的是當前應用的視野範圍和角度（基礎值 × 倍數）
    /// Target 不使用 chaseRange，所以只處理前兩個參數
    /// </summary>
    public override void SetDetectionParameters(params object[] parameters)
    {
        if (parameters.Length >= 2)
        {
            currentViewRange = (float)parameters[0];
            currentViewAngle = (float)parameters[1];
        }
    }

    /// <summary>
    /// 設定偵測參數（保留原有方法以維持向後兼容）
    /// 注意：此方法設置的是當前應用的視野範圍和角度（基礎值 × 倍數）
    /// Target 不使用 chaseRange，所以忽略第三個參數
    /// </summary>
    public void SetDetectionParameters(float newViewRange, float newViewAngle, float newChaseRange)
    {
        currentViewRange = newViewRange;
        currentViewAngle = newViewAngle;
        // Target 不使用 chaseRange，忽略第三個參數
    }

    // IsBlockedByObstacle 已移至 BaseDetection
    // SetTarget, GetDirectionToTarget, GetDistanceToTarget, HasValidTarget 已由基類 BaseDetection 統一提供

    public void SetRaycastDetection(bool enabled) => useRaycastDetection = enabled;

    /// <summary>
    /// 清除目標（覆寫基類方法）
    /// </summary>

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
    /// 檢查是否在逃亡狀態
    /// </summary>
    private bool IsInEscapeState()
    {
        if (stateMachine == null) return false;
        return stateMachine.CurrentState == TargetState.Escape;
    }

    /// <summary>
    /// 檢查是否正在使用路徑規劃
    /// </summary>
    private bool IsUsingPathfinding()
    {
        if (stateMachine == null) return false;
        // 在逃亡狀態時使用路徑規劃
        return stateMachine.CurrentState == TargetState.Escape;
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
        // 如果啟用攝影機剔除且目標在攝影機外
        if (enableCameraCulling && IsOutsideCameraView())
        {
            // 只有在逃亡狀態時才繼續偵測
            if (stateMachine != null)
            {
                return stateMachine.CurrentState == TargetState.Escape;
            }
        }
        
        return true; // 其他情況都進行偵測
    }

    /// <summary>
    /// 檢查是否應該更新 AI 邏輯（考慮攝影機剔除）
    /// 根據 target_ai.md：當不在 Escape 狀態且不在攝影機範圍內時，不更新 AI
    /// </summary>
    public override bool ShouldUpdateAI()
    {
        if (stateMachine == null) return false;
        
        TargetState currentState = stateMachine.CurrentState;
        
        // Escape 狀態始終更新（即使視野外）
        if (currentState == TargetState.Escape)
        {
            return true;
        }
        
        // 其他狀態需要檢查是否在攝影機視野內
        if (enableCameraCulling)
        {
            return !IsOutsideCameraView();
        }
        
        return true; // 如果未啟用攝影機剔除，始終更新
    }

}