using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敵人移動控制器（繼承基礎移動組件）
/// 職責：處理移動邏輯、巡邏行為、路徑規劃
/// 
/// 【封裝說明】
/// 此類的屬性（如 speed）應通過 Enemy 類的公共方法進行修改，而不是直接訪問。
/// 正確方式：使用 Enemy.UpdateDangerLevelStats() 來更新速度。
/// </summary>
public class EnemyMovement : BaseMovement
{
    [Header("移動參數")]
    // 注意：speed 不再在這裡定義
    // speed 應從 Enemy 的 BaseSpeed 獲取
    // 速度倍數應從 Enemy.GetStateSpeedMultiplier() 獲取（根據當前狀態）
    // 實際速度 = Enemy.BaseSpeed * Enemy.GetStateSpeedMultiplier()
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("巡邏路徑")]
    [Tooltip("巡邏速度倍數（相對於基礎速度，建議 0.3-0.5）")]
    [SerializeField] private float patrolSpeedMultiplier = 0.35f;
    // 注意：patrolPoints 已移至基類 BaseMovement

    [Header("路徑規劃")]
    [SerializeField] private bool usePathfinding = true;
    [SerializeField] private float pathUpdateDistance = 2f;
    [SerializeField] private float pathReachThreshold = 0.5f;
    [SerializeField] private float targetPositionChangeThreshold = 0.5f;

    [Header("直線追擊檢測")]
    [SerializeField] private LayerMask obstaclesLayerMask;
    [SerializeField] private float directChaseCheckRadius = 0.3f;

    private GreedyPathfinding pathfinding;
    
    // 巡邏暫停
    private bool isPausedAtPatrolPoint = false;
    private float patrolPauseEndTime = 0f; // Changed from timer to end time

    public Vector2 SpawnPoint => spawnPoint;

    protected override void Awake()
    {
        base.Awake();

        // 自動查找路徑規劃組件
        pathfinding = FindFirstObjectByType<GreedyPathfinding>();
        if (usePathfinding && pathfinding == null)
        {
            Debug.LogError($"{gameObject.name}: 找不到 GreedyPathfinding 組件！");
        }

        // 自動配置 ObstaclesLayerMask
        if (obstaclesLayerMask == 0)
        {
            int wallsLayer = LayerMask.NameToLayer("Walls");
            int objectsLayer = LayerMask.NameToLayer("Objects");
            int obstaclesLayer = LayerMask.NameToLayer("Obstacles");
            
            if (wallsLayer != -1) obstaclesLayerMask |= (1 << wallsLayer);
            if (objectsLayer != -1) obstaclesLayerMask |= (1 << objectsLayer);
            if (obstaclesLayer != -1) obstaclesLayerMask |= (1 << obstaclesLayer);
        }
    }

    /// <summary>
    /// 覆寫基類方法以提供 Enemy 特定的 arriveThreshold
    /// </summary>
    protected override float GetArriveThreshold() => arriveThreshold;
    
    /// <summary>
    /// 檢查是否正在巡邏點暫停
    /// </summary>
    public bool IsPausedAtPatrolPoint()
    {
        return isPausedAtPatrolPoint;
    }
    

    /// <summary>
    /// 向目標移動（覆寫基類方法，內部使用，自動獲取速度倍數）
    /// </summary>
    public override void MoveTowards(Vector2 target, float speedMultiplier)
    {
        // 忽略傳入的 speedMultiplier，使用基類的智能移動邏輯（自動獲取速度倍數）
        base.MoveTowards(target);
    }
    
    /// <summary>
    /// 獲取基礎速度（從 Enemy 組件，實現基類抽象方法）
    /// </summary>
    protected override float GetBaseSpeed()
    {
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            return enemy.BaseSpeed;
        }
        // 如果找不到 Enemy 組件，返回預設值（向後兼容）
        return 2f;
    }
    
    /// <summary>
    /// 根據當前狀態獲取速度倍數（從 Enemy 組件）
    /// </summary>
    private float GetStateSpeedMultiplier()
    {
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            return enemy.GetStateSpeedMultiplier();
        }
        // 如果找不到 Enemy 組件，返回預設值（向後兼容）
        return 1.0f;
    }
    
    /// <summary>
    /// 獲取速度倍數（覆寫基類方法，自動從 Enemy 組件獲取）
    /// </summary>
    protected override float GetSpeedMultiplier()
    {
        return GetStateSpeedMultiplier();
    }

    /// <summary>
    /// 檢查是否有可用的路徑規劃組件（實現基類抽象方法）
    /// </summary>
    protected override bool HasPathfinding()
    {
        return usePathfinding && pathfinding != null;
    }

    /// <summary>
    /// 獲取路徑規劃組件並計算路徑（實現基類抽象方法）
    /// </summary>
    protected override List<PathfindingNode> FindPath(Vector2 start, Vector2 target)
    {
        if (pathfinding == null) return null;
        return pathfinding.FindPath(start, target);
    }

    /// <summary>
    /// 覆寫基類的虛擬方法以提供 Enemy 特定的參數
    /// </summary>
    protected override bool UsePathfinding() => usePathfinding;
    protected override float GetPathUpdateDistance() => pathUpdateDistance;
    protected override float GetPathReachThreshold() => pathReachThreshold;
    protected override float GetTargetPositionChangeThreshold() => targetPositionChangeThreshold;
    protected override LayerMask GetObstaclesLayerMask() => obstaclesLayerMask;
    protected override float GetDirectChaseCheckRadius() => directChaseCheckRadius;

    /// <summary>
    /// 獲取當前是否使用直線移動（基於當前移動模式，別名方法）
    /// </summary>
    public bool IsUsingDirectChase()
    {
        return IsUsingDirectMovement();
    }

    /// <summary>
    /// 設定移動速度（覆寫基類方法）
    /// 注意：實際速度應從 Enemy.BaseSpeed 獲取，此方法僅用於設置當前應用的速度倍數
    /// </summary>
    public override void SetSpeed(float newSpeed)
    {
        // 注意：此方法現在用於設置當前應用的速度（由 Enemy 調用）
        // 實際速度 = Enemy.BaseSpeed * 當前速度倍數
        // 此處保留以維持向後兼容，但實際速度應從 Enemy 獲取
    }

    /// <summary>
    /// 獲取移動速度（覆寫基類方法）
    /// 返回當前 rb.linearVelocity 的大小（實際移動速度）
    /// </summary>
    public override float GetSpeed()
    {
        if (rb == null) return 0f;
        return rb.linearVelocity.magnitude;
    }
}