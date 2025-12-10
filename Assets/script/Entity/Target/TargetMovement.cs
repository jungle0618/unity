using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Target移動控制器（繼承基礎移動組件）
/// 職責：處理移動邏輯、巡邏行為、路徑規劃
/// 
/// 【封裝說明】
/// 此類的屬性（如 speed）應通過 Target 類的公共方法進行修改，而不是直接訪問。
/// 正確方式：使用 Target.UpdateDangerLevelStats() 來更新速度。
/// </summary>
public class TargetMovement : BaseMovement
{
    [Header("移動參數")]
    // 注意：speed 不再在這裡定義
    // speed 應從 Target 的 BaseSpeed 獲取
    // 速度倍數應從 Target.GetStateSpeedMultiplier() 獲取（根據當前狀態）
    // 實際速度 = Target.BaseSpeed * Target.GetStateSpeedMultiplier()
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("巡邏路徑")]
    // 注意：patrolPoints 已移至基類 BaseMovement

    [Header("路徑規劃")]
    [SerializeField] private bool usePathfinding = true;
    [SerializeField] private bool useAStar = true; // 優先使用 A* 算法
    [SerializeField] private float pathUpdateDistance = 2f;
    [SerializeField] private float chasePathUpdateDistance = 1f;
    [SerializeField] private float pathReachThreshold = 0.5f;
    [SerializeField] private float chasePathReachThreshold = 0.3f;
    [SerializeField] private float targetPositionChangeThreshold = 0.5f;
    [SerializeField] private bool showPathfindingDebug = true; // 顯示路徑規劃調試信息

    [Header("直線追擊檢測")]
    [SerializeField] private LayerMask obstaclesLayerMask;
    [SerializeField] private float directChaseCheckRadius = 0.3f;

    private GreedyPathfinding greedyPathfinding;
    private AStarPathfinding aStarPathfinding;

    // 直線追擊
    private bool isUsingDirectChase = false;
    private float lastDirectChaseCheckTime = 0f;
    private const float DIRECT_CHASE_CHECK_INTERVAL = 0.1f;

    public Vector2 SpawnPoint => spawnPoint;

    protected override void Awake()
    {
        base.Awake();

        // 自動查找路徑規劃組件
        greedyPathfinding = FindFirstObjectByType<GreedyPathfinding>();
        aStarPathfinding = FindFirstObjectByType<AStarPathfinding>();
        if (usePathfinding && greedyPathfinding == null && aStarPathfinding == null)
        {
            Debug.LogError($"{gameObject.name}: 找不到 GreedyPathfinding 或 AStarPathfinding 組件！");
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
    /// 覆寫基類方法以提供 Target 特定的 arriveThreshold
    /// </summary>
    protected override float GetArriveThreshold() => arriveThreshold;

    /// <summary>
    /// 執行巡邏移動
    /// </summary>
    public void PerformPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            StopMovement();
            return;
        }

        // 確保索引在合法範圍內
        if (patrolIndex < 0 && patrolPoints.Length > 0)
        {
            patrolIndex = 0;
        }

        Vector2 targetPos = patrolPoints[patrolIndex].position;
        float speedMultiplier = GetStateSpeedMultiplier();
        MoveTowards(targetPos, speedMultiplier);

        if (Vector2.Distance(transform.position, targetPos) < arriveThreshold)
        {
            AdvancePatrolIndex();
        }
    }

    /// <summary>
    /// 沿著locations移動（用於Patrol和Alert狀態）
    /// </summary>
    public void MoveAlongLocations(Vector3[] locations, int currentIndex)
    {
        if (locations == null || locations.Length == 0)
        {
            StopMovement();
            return;
        }

        Vector2 targetPos = locations[currentIndex];
        float speedMultiplier = GetStateSpeedMultiplier();
        MoveTowards(targetPos, speedMultiplier);
    }


    /// <summary>
    /// 向目標移動（覆寫基類方法，使用智能移動邏輯）
    /// </summary>
    public override void MoveTowards(Vector2 target, float speedMultiplier)
    {
        // 使用基類的智能移動邏輯（自動獲取速度倍數）
        base.MoveTowards(target);
    }
    
    /// <summary>
    /// 獲取基礎速度（從 Target 組件，實現基類抽象方法）
    /// </summary>
    protected override float GetBaseSpeed()
    {
        Target target = GetComponent<Target>();
        if (target != null)
        {
            return target.BaseSpeed;
        }
        // 如果找不到 Target 組件，返回預設值（向後兼容）
        return 2f;
    }
    
    /// <summary>
    /// 根據當前狀態獲取速度倍數（從 Target 組件）
    /// </summary>
    private float GetStateSpeedMultiplier()
    {
        Target target = GetComponent<Target>();
        if (target != null)
        {
            return target.GetStateSpeedMultiplier();
        }
        // 如果找不到 Target 組件，返回預設值（向後兼容）
        return 0f;
    }

    /// <summary>
    /// 獲取速度倍數（覆寫基類方法，自動從 Target 組件獲取）
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
        return usePathfinding && HasPathfindingInternal();
    }

    /// <summary>
    /// 檢查是否有可用的路徑規劃組件（內部方法）
    /// </summary>
    private bool HasPathfindingInternal()
    {
        return (useAStar && aStarPathfinding != null) || greedyPathfinding != null;
    }

    /// <summary>
    /// 獲取路徑規劃組件並計算路徑（實現基類抽象方法）
    /// </summary>
    protected override List<PathfindingNode> FindPath(Vector2 start, Vector2 target)
    {
        // 優先使用 A* 算法
        if (useAStar && aStarPathfinding != null)
        {
            return aStarPathfinding.FindPath(start, target);
        }
        else if (greedyPathfinding != null)
        {
            return greedyPathfinding.FindPath(start, target);
        }
        return null;
    }

    /// <summary>
    /// 覆寫基類的虛擬方法以提供 Target 特定的參數
    /// </summary>
    protected override bool UsePathfinding() => usePathfinding;
    protected override float GetPathUpdateDistance() => pathUpdateDistance;
    protected override float GetPathReachThreshold() => pathReachThreshold;
    protected override float GetTargetPositionChangeThreshold() => targetPositionChangeThreshold;
    protected override LayerMask GetObstaclesLayerMask() => obstaclesLayerMask;
    protected override float GetDirectChaseCheckRadius() => directChaseCheckRadius;
    
    
    /// <summary>
    /// 逃亡移動（使用路徑規劃，最短路徑到目標）
    /// </summary>
    public void MoveTowardsEscape(Vector3 target)
    {
        float speedMultiplier = GetStateSpeedMultiplier();
        
        if (usePathfinding && greedyPathfinding != null)
        {
            // 使用路徑規劃移動到目標
            MoveTowardsWithPathfinding(target, speedMultiplier);
        }
        else
        {
            // 沒有路徑規劃，直接移動
            MoveTowards(target, speedMultiplier);
        }
    }

    /// <summary>
    /// 向目標移動（智能選擇是否使用路徑規劃）
    /// </summary>
    public void MoveTowardsSmart(Vector2 target, float speedMultiplier)
    {
        if (usePathfinding && HasPathfinding())
        {
            MoveTowardsWithPathfinding(target, speedMultiplier);
        }
        else
        {
            MoveTowards(target, speedMultiplier);
        }
    }

    /// <summary>
    /// 追擊移動（智能選擇直線追擊或路徑規劃）
    /// </summary>
    public void ChaseTarget(Vector2 targetPos)
    {
        // 定期檢查是否能直線到達目標
        if (Time.time - lastDirectChaseCheckTime >= DIRECT_CHASE_CHECK_INTERVAL)
        {
            isUsingDirectChase = CanReachDirectly(targetPos);
            lastDirectChaseCheckTime = Time.time;
        }

        // 根據檢查結果選擇追擊方式（Target 不使用 chaseSpeedMultiplier，使用基礎速度）
        if (isUsingDirectChase)
        {
            ChaseTargetDirect(targetPos);
        }
        else
        {
            MoveTowardsWithChasePathfinding(targetPos, 1.0f);
        }
    }


    /// <summary>
    /// 直線追擊目標（Target 不使用此方法，保留以維持兼容性）
    /// </summary>
    public void ChaseTargetDirect(Vector2 targetPos)
    {
        if (rb == null) return;
        float baseSpeed = GetBaseSpeed();
        float speedMultiplier = GetStateSpeedMultiplier();
        float injuryMultiplier = GetInjurySpeedMultiplier(); // 受傷時速度乘以 0.7
        rb.linearVelocity = (targetPos - (Vector2)transform.position).normalized * baseSpeed * speedMultiplier * injuryMultiplier;
    }

    /// <summary>
    /// 獲取當前是否使用直線追擊
    /// </summary>
    public bool IsUsingDirectChase()
    {
        return isUsingDirectChase;
    }

    /// <summary>
    /// 移動到目標位置（帶路徑朝向控制，主要用於搜索狀態）
    /// </summary>
    public void ChaseTargetWithRotation(Vector2 targetPos, TargetDetection detection)
    {
        // 先檢查是否能直線到達，如果可以就直接移動（搜索狀態也可以使用直線移動）
        if (Time.time - lastDirectChaseCheckTime >= DIRECT_CHASE_CHECK_INTERVAL)
        {
            bool canReachDirectly = CanReachDirectly(targetPos);
            if (canReachDirectly)
            {
                // 直接移動到目標
                ChaseTargetDirect(targetPos);
                
                // 朝向目標
                Vector2 directionToTarget = (targetPos - (Vector2)transform.position).normalized;
                if (directionToTarget.magnitude > 0.1f && detection != null)
                {
                    detection.SetViewDirection(directionToTarget);
                }
                return;
            }
            lastDirectChaseCheckTime = Time.time;
        }

        // 使用路徑規劃
        float speedMultiplier = GetStateSpeedMultiplier();
        MoveTowardsWithChasePathfinding(targetPos, speedMultiplier);
        
        // 讓敵人朝向路徑的下一個點
        Vector2 directionToNextPoint = GetDirectionToNextPathPoint();
        if (directionToNextPoint.magnitude > 0.1f && detection != null)
        {
            detection.SetViewDirection(directionToNextPoint);
        }
        else
        {
            // 如果沒有路徑點，使用移動方向作為備選
            Vector2 movementDirection = GetMovementDirection();
            if (movementDirection.magnitude > 0.1f && detection != null)
            {
                detection.SetViewDirection(movementDirection);
            }
        }
    }

    /// <summary>
    /// 使用追擊專用的路徑規劃移動
    /// </summary>
    public void MoveTowardsWithChasePathfinding(Vector2 target, float speedMultiplier)
    {
        if (!usePathfinding || !HasPathfinding())
        {
            // 如果沒有路徑規劃，使用直接移動
            base.MoveTowards(target);
            return;
        }

        // 檢查是否需要更新路徑（使用追擊專用的更新間隔）
        bool shouldUpdatePath = ShouldUpdateChasePath(target);
        
        if (shouldUpdatePath)
        {
            // 使用基類的方法更新路徑
            UpdatePathToTargetSafe(target);
        }

        // 沿著路徑移動（使用追擊專用的到達閾值）
        FollowChasePath(speedMultiplier);
    }


    /// <summary>
    /// 使用路徑規劃向目標移動
    /// </summary>
    public void MoveTowardsWithPathfinding(Vector2 target, float speedMultiplier)
    {
        // 使用基類的智能移動邏輯（自動獲取速度倍數）
        base.MoveTowards(target);
    }

    /// <summary>
    /// 檢查是否需要更新追擊路徑（基於移動距離，使用追擊專用的更新間隔）
    /// </summary>
    private bool ShouldUpdateChasePath(Vector2 target)
    {
        // 如果路徑為空，需要更新路徑
        if (currentPath == null || currentPath.Count == 0)
        {
            return true;
        }

        // 如果已經到達路徑終點，需要更新路徑
        if (currentPathIndex >= currentPath.Count)
        {
            return true;
        }

        // 如果目標位置改變太多，需要更新路徑（追擊時目標會移動）
        if (Vector2.Distance(target, lastTargetPosition) > targetPositionChangeThreshold)
        {
            return true;
        }

        // 如果敵人移動了足夠的距離，需要更新路徑（追擊時使用更小的距離閾值）
        float movedDistance = Vector2.Distance(transform.position, lastPathUpdatePosition);
        if (movedDistance > chasePathUpdateDistance)
        {
            return true;
        }

        return false;
    }



    /// <summary>
    /// 沿著路徑移動（追擊專用）
    /// </summary>
    private void FollowChasePath(float speedMultiplier)
    {
        if (currentPath == null || currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            StopMovement();
            return;
        }

        Vector2 targetPosition = currentPath[currentPathIndex].worldPosition;
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        
        if (rb != null)
        {
            float baseSpeed = GetBaseSpeed();
            float injuryMultiplier = GetInjurySpeedMultiplier(); // 受傷時速度乘以 0.7
            Vector2 targetVelocity = direction * baseSpeed * speedMultiplier * injuryMultiplier;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 8f);
        }

        if (Vector2.Distance(transform.position, targetPosition) < chasePathReachThreshold)
        {
            currentPathIndex++;
        }
    }


    /// <summary>
    /// 設定移動速度（覆寫基類方法）
    /// 注意：實際速度應從 Target.BaseSpeed 獲取，此方法僅用於設置當前應用的速度倍數
    /// </summary>
    public override void SetSpeed(float newSpeed)
    {
        // 注意：此方法現在用於設置當前應用的速度（由 Target 調用）
        // 實際速度 = Target.BaseSpeed * 當前速度倍數
        // 此處保留以維持向後兼容，但實際速度應從 Target 獲取
    }

    /// <summary>
    /// 獲取移動速度（覆寫基類方法）
    /// 返回當前應用的速度（從 Target 獲取基礎速度）
    /// </summary>
    public override float GetSpeed()
    {
        return GetBaseSpeed();
    }
}
