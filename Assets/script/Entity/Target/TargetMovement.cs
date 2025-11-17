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
    // 注意：speed 和 chaseSpeedMultiplier 不再在這裡定義
    // speed 應從 Target 的 BaseSpeed 獲取
    // 實際速度 = Target.BaseSpeed * 當前速度倍數
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("巡邏路徑")]
    [SerializeField] private Transform[] patrolPoints;

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

    private Vector2 spawnPoint;
    private int patrolIndex = 0;
    private GreedyPathfinding greedyPathfinding;
    private AStarPathfinding aStarPathfinding;
    
    // 路徑規劃
    private List<PathfindingNode> currentPath = new List<PathfindingNode>();
    private int currentPathIndex = 0;
    private Vector3 lastPathUpdatePosition;
    private Vector3 lastTargetPosition;

    // 直線追擊
    private bool isUsingDirectChase = false;
    private float lastDirectChaseCheckTime = 0f;
    private const float DIRECT_CHASE_CHECK_INTERVAL = 0.1f;
    
    // 卡住檢測
    private Vector2 lastPosition;
    private float lastPositionUpdateTime;
    private float lastStuckTime;

    public Vector2 SpawnPoint => spawnPoint;

    protected override void Awake()
    {
        base.Awake();
        spawnPoint = transform.position;

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

        // 初始化
        lastStuckTime = Time.time;
        lastPosition = transform.position;
        lastPositionUpdateTime = Time.time;
        lastPathUpdatePosition = transform.position;
    }

    /// <summary>
    /// 設定巡邏點
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
        patrolIndex = 0;
    }

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
        MoveTowards(targetPos, 1f);

        if (Vector2.Distance(Position, targetPos) < arriveThreshold)
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
        MoveTowards(targetPos, 1f);
    }

    /// <summary>
    /// 檢查是否到達指定的location
    /// </summary>
    public bool HasArrivedAtLocation(Vector3 location)
    {
        return Vector2.Distance(Position, location) < arriveThreshold;
    }

    /// <summary>
    /// 向目標移動（覆寫基類方法）
    /// </summary>
    public override void MoveTowards(Vector2 target, float speedMultiplier)
    {
        if (rb == null) return;

        float baseSpeed = GetBaseSpeed();
        Vector2 direction = (target - Position).normalized;
        rb.linearVelocity = direction * baseSpeed * speedMultiplier;
    }
    
    /// <summary>
    /// 獲取基礎速度（從 Target 組件）
    /// </summary>
    private float GetBaseSpeed()
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
    /// 檢查是否有可用的路徑規劃組件
    /// </summary>
    private bool HasPathfinding()
    {
        return (useAStar && aStarPathfinding != null) || greedyPathfinding != null;
    }
    
    /// <summary>
    /// 逃亡移動（使用路徑規劃，最短路徑到目標）
    /// </summary>
    public void MoveTowardsEscape(Vector3 target, float escapeSpeed)
    {
        float baseSpeed = GetBaseSpeed();
        float speedMultiplier = escapeSpeed / baseSpeed;
        
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
    /// 檢查是否能直線到達目標
    /// </summary>
    public bool CanReachDirectly(Vector2 target)
    {
        Vector2 direction = (target - Position).normalized;
        float distance = Vector2.Distance(Position, target);
        
        RaycastHit2D hit = Physics2D.CircleCast(Position, directChaseCheckRadius, direction, distance, obstaclesLayerMask);
        return hit.collider == null;
    }

    /// <summary>
    /// 直線追擊目標（Target 不使用此方法，保留以維持兼容性）
    /// </summary>
    public void ChaseTargetDirect(Vector2 targetPos)
    {
        if (rb == null) return;
        float baseSpeed = GetBaseSpeed();
        // Target 不使用 chaseSpeedMultiplier，直接使用基礎速度
        rb.linearVelocity = (targetPos - Position).normalized * baseSpeed;
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
                Vector2 directionToTarget = (targetPos - Position).normalized;
                if (directionToTarget.magnitude > 0.1f && detection != null)
                {
                    detection.SetViewDirection(directionToTarget);
                }
                return;
            }
            lastDirectChaseCheckTime = Time.time;
        }

        // 使用路徑規劃（Target 不使用 chaseSpeedMultiplier，使用基礎速度）
        MoveTowardsWithChasePathfinding(targetPos, 1.0f);
        
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
            MoveTowards(target, speedMultiplier);
            return;
        }

        // 檢查是否需要更新路徑（使用追擊專用的更新間隔）
        bool shouldUpdatePath = ShouldUpdateChasePath(target);
        
        if (shouldUpdatePath)
        {
            UpdatePathToTargetSafe(target);
        }

        // 沿著路徑移動（使用追擊專用的到達閾值）
        FollowChasePath(speedMultiplier);
    }

    /// <summary>
    /// 停止移動（覆寫基類方法）
    /// </summary>
    public override void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// 獲取返回目標位置（返回第一個patrol point，即spawn point）
    /// </summary>
    public Vector2 GetReturnTarget()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            return patrolPoints[0].position; // 總是返回第一個patrol point
        }
        return spawnPoint;
    }

    /// <summary>
    /// 檢查是否到達目標位置（覆寫基類方法）
    /// </summary>
    public override bool HasArrivedAt(Vector2 target, float threshold = 0.2f)
    {
        return Vector2.Distance(Position, target) < threshold;
    }

    /// <summary>
    /// 檢查是否到達目標位置（使用自訂閾值）
    /// </summary>
    public bool HasArrivedAtWithCustomThreshold(Vector2 target)
    {
        return HasArrivedAt(target, arriveThreshold);
    }

    /// <summary>
    /// 前進到下一個巡邏點（超過最後一個則回到第一個）
    /// </summary>
    private void AdvancePatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    /// <summary>
    /// 獲取當前巡邏點
    /// </summary>
    public Transform[] GetPatrolPoints()
    {
        return patrolPoints;
    }

    /// <summary>
    /// 獲取當前移動方向（覆寫基類方法）
    /// </summary>
    public override Vector2 GetMovementDirection()
    {
        if (rb == null) return Vector2.right;
        return rb.linearVelocity.normalized;
    }

    /// <summary>
    /// 獲取朝向目標的方向（覆寫基類方法）
    /// </summary>
    public override Vector2 GetDirectionToTarget(Vector2 target)
    {
        return (target - Position).normalized;
    }

    /// <summary>
    /// 獲取到下一個路徑點的方向
    /// </summary>
    public Vector2 GetDirectionToNextPathPoint()
    {
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            Vector2 nextPoint = currentPath[currentPathIndex].worldPosition;
            return (nextPoint - Position).normalized;
        }
        return Vector2.zero;
    }

    /// <summary>
    /// 使用路徑規劃向目標移動
    /// </summary>
    public void MoveTowardsWithPathfinding(Vector2 target, float speedMultiplier)
    {
        if (!usePathfinding || !HasPathfinding())
        {
            // 如果沒有路徑規劃，使用直接移動
            MoveTowards(target, speedMultiplier);
            return;
        }

        // 檢查是否需要更新路徑
        bool shouldUpdatePath = ShouldUpdatePath(target);
        
        if (shouldUpdatePath)
        {
            UpdatePathToTargetSafe(target);
        }

        // 沿著路徑移動
        FollowPath(speedMultiplier);
    }

    /// <summary>
    /// 檢查是否需要更新路徑（基於移動距離）
    /// </summary>
    private bool ShouldUpdatePath(Vector2 target)
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

        // 如果目標位置改變太多，需要更新路徑
        if (Vector2.Distance(target, lastTargetPosition) > targetPositionChangeThreshold)
        {
            return true;
        }

        // 如果敵人移動了足夠的距離，需要更新路徑
        float movedDistance = Vector2.Distance(Position, lastPathUpdatePosition);
        if (movedDistance > pathUpdateDistance)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 檢查是否需要更新追擊路徑（基於移動距離）
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
        float movedDistance = Vector2.Distance(Position, lastPathUpdatePosition);
        if (movedDistance > chasePathUpdateDistance)
        {
            return true;
        }

        return false;
    }


    /// <summary>
    /// 更新路徑（自動處理不可行走的起點和終點）
    /// </summary>
    private void UpdatePathToTargetSafe(Vector2 target)
    {
        if (!HasPathfinding()) 
        {
            if (showPathfindingDebug)
            {
                Debug.LogWarning($"{gameObject.name}: 沒有可用的路徑規劃組件");
            }
            return;
        }

        PathfindingGrid grid = FindFirstObjectByType<PathfindingGrid>();
        if (grid == null)
        {
            if (showPathfindingDebug)
            {
                Debug.LogError($"{gameObject.name}: 找不到 PathfindingGrid");
            }
            return;
        }

        // 調整起點和終點到可行走位置
        Vector2 start = AdjustToWalkable(grid, transform.position, 2f);
        Vector2 adjustedTarget = AdjustToWalkable(grid, target, 3f);

        if (showPathfindingDebug)
        {
            Debug.Log($"{gameObject.name}: 更新路徑 {start} → {adjustedTarget}");
        }

        // 優先使用 A* 算法
        if (useAStar && aStarPathfinding != null)
        {
            currentPath = aStarPathfinding.FindPath(start, adjustedTarget);
        }
        else if (greedyPathfinding != null)
        {
            currentPath = greedyPathfinding.FindPath(start, adjustedTarget);
        }
        
        if (currentPath != null && showPathfindingDebug)
        {
            // 使用 LogWarning 讓路徑信息更顯眼，方便快速閱讀
            string pathDetails = "";
            for (int i = 0; i < Mathf.Min(currentPath.Count, 10); i++) // 只顯示前10個節點
            {
                pathDetails += $"\n  [{i}] ({currentPath[i].worldPosition.x:F1}, {currentPath[i].worldPosition.y:F1})";
            }
            if (currentPath.Count > 10)
            {
                pathDetails += $"\n  ... 還有 {currentPath.Count - 10} 個節點";
            }
            
            string algorithm = (useAStar && aStarPathfinding != null) ? "A*" : "Greedy";
            Debug.Log($"[{algorithm}路徑] {gameObject.name}: ✓ 路徑已更新！\n" +
                           $"起點: ({start.x:F1}, {start.y:F1})\n" +
                           $"終點: ({adjustedTarget.x:F1}, {adjustedTarget.y:F1})\n" +
                           $"節點數: {currentPath.Count}{pathDetails}");
        }
        else if (currentPath == null && showPathfindingDebug)
        {
            Debug.LogWarning($"{gameObject.name}: ✗ 無法找到路徑！起點: ({start.x:F1}, {start.y:F1}) → 終點: ({adjustedTarget.x:F1}, {adjustedTarget.y:F1})");
        }
        
        currentPathIndex = 0;
        lastPathUpdatePosition = transform.position;
        lastTargetPosition = target;
    }

    /// <summary>
    /// 調整位置到最近的可行走點
    /// </summary>
    private Vector2 AdjustToWalkable(PathfindingGrid grid, Vector2 position, float searchRadius)
    {
        PathfindingNode node = grid.GetNode(position);
        if (node != null && node.isWalkable) return position;
        
        PathfindingNode nearest = grid.GetNearestWalkableNode(position, searchRadius);
        return nearest != null && nearest.isWalkable ? nearest.worldPosition : position;
    }

    /// <summary>
    /// 沿著路徑移動
    /// </summary>
    private void FollowPath(float speedMultiplier)
    {
        if (currentPath == null)
        {
            StopMovement();
            return;
        }

        // 如果路徑為空，表示已在目標位置附近，停止移動
        if (currentPath.Count == 0)
        {
            StopMovement();
            return;
        }

        if (currentPathIndex >= currentPath.Count)
        {
            StopMovement();
            return;
        }

        Vector2 targetPosition = currentPath[currentPathIndex].worldPosition;
        Vector2 direction = (targetPosition - Position).normalized;
        
        if (rb != null)
        {
            float baseSpeed = GetBaseSpeed();
            rb.linearVelocity = direction * baseSpeed * speedMultiplier;
        }

        // 檢查是否到達當前路徑點
        if (Vector2.Distance(Position, targetPosition) < pathReachThreshold)
        {
            currentPathIndex++;
        }
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
        Vector2 direction = (targetPosition - Position).normalized;
        
        if (rb != null)
        {
            float baseSpeed = GetBaseSpeed();
            Vector2 targetVelocity = direction * baseSpeed * speedMultiplier;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 8f);
        }

        if (Vector2.Distance(Position, targetPosition) < chasePathReachThreshold)
        {
            currentPathIndex++;
        }
    }

    /// <summary>
    /// 清除當前路徑
    /// </summary>
    public void ClearPath()
    {
        if (currentPath != null)
        {
            currentPath.Clear();
        }
        currentPathIndex = 0;
    }

    /// <summary>
    /// 檢查是否有有效路徑
    /// </summary>
    public bool HasValidPath()
    {
        return currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count;
    }

    /// <summary>
    /// 獲取當前路徑
    /// </summary>
    public List<PathfindingNode> GetCurrentPath()
    {
        return currentPath;
    }

    /// <summary>
    /// 獲取下一個路徑點
    /// </summary>
    public Vector2 GetNextPathPoint()
    {
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            return currentPath[currentPathIndex].worldPosition;
        }
        return Position;
    }

    /// <summary>
    /// 檢查敵人是否卡住
    /// </summary>
    public bool IsStuckOrHittingWall()
    {
        if (rb == null) return false;

        float stuckThreshold = isUsingDirectChase ? 1.0f : 0.5f;
        
        if (rb.linearVelocity.magnitude < 0.1f)
        {
            if (Time.time - lastStuckTime > stuckThreshold) return true;
        }
        else
        {
            lastStuckTime = Time.time;
        }

        if (Vector2.Distance(Position, lastPosition) < 0.05f && Time.time - lastPositionUpdateTime > 0.5f)
        {
            return true;
        }

        return false;
    }

    private void Update()
    {
        // 更新位置追蹤
        if (Time.time - lastPositionUpdateTime > 0.1f)
        {
            lastPosition = Position;
            lastPositionUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// 在 LateUpdate 中處理朝向移動方向，避免與物理更新衝突
    /// </summary>
    private void LateUpdate()
    {
        // 朝向移動方向
        Vector2 movementDirection = GetMovementDirection();
        if (movementDirection.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
