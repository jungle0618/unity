using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敵人移動控制器（繼承基礎移動組件）
/// 職責：處理移動邏輯、巡邏行為、路徑規劃
/// </summary>
public class EnemyMovement : BaseMovement
{
    [Header("移動參數")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseSpeedMultiplier = 1.5f;
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("巡邏路徑")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("路徑規劃")]
    [SerializeField] private GreedyPathfinding pathfinding;
    [SerializeField] private bool usePathfinding = true;
    [SerializeField] private float pathUpdateInterval = 0.5f;
    [SerializeField] private float pathReachThreshold = 0.5f;
    [SerializeField] private float chasePathUpdateInterval = 0.2f; // 追擊時更頻繁更新路徑
    [SerializeField] private float chasePathReachThreshold = 0.3f; // 追擊時更精確的到達閾值

    private Vector2 spawnPoint;
    private int patrolIndex = 0;
    
    // 路徑規劃相關
    private List<PathfindingNode> currentPath = new List<PathfindingNode>();
    private int currentPathIndex = 0;
    private float lastPathUpdateTime = 0f;
    private Vector3 lastTargetPosition;

    public Vector2 SpawnPoint => spawnPoint;

    protected override void Awake()
    {
        base.Awake(); // 調用基類 Awake，初始化 rb
        spawnPoint = transform.position;

        // 初始化路徑規劃
        if (pathfinding == null)
        {
            pathfinding = FindFirstObjectByType<GreedyPathfinding>();
        }
        
        // 檢查路徑規劃設定
        if (usePathfinding && pathfinding == null)
        {
            Debug.LogError($"{gameObject.name}: 啟用了路徑規劃但找不到 GreedyPathfinding 組件！");
        }
        else if (usePathfinding && pathfinding != null)
        {
            Debug.Log($"{gameObject.name}: 路徑規劃已啟用，使用 {pathfinding.name}");
        }

        // 初始化卡住檢測變數
        lastStuckTime = Time.time;
        lastPosition = transform.position;
        lastPositionUpdateTime = Time.time;
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

        Vector2 direction = (target - Position).normalized;
        rb.linearVelocity = direction * speed * speedMultiplier;
    }

    /// <summary>
    /// 向目標移動（智能選擇是否使用路徑規劃）
    /// </summary>
    public void MoveTowardsSmart(Vector2 target, float speedMultiplier)
    {
        if (usePathfinding && pathfinding != null)
        {
            MoveTowardsWithPathfinding(target, speedMultiplier);
        }
        else
        {
            MoveTowards(target, speedMultiplier);
        }
    }

    /// <summary>
    /// 追擊移動
    /// </summary>
    public void ChaseTarget(Vector2 targetPos)
    {
        MoveTowardsWithChasePathfinding(targetPos, chaseSpeedMultiplier);
    }

    /// <summary>
    /// 移動到目標位置（帶路徑朝向控制，主要用於搜索狀態）
    /// </summary>
    public void ChaseTargetWithRotation(Vector2 targetPos, EnemyDetection detection)
    {
        MoveTowardsWithChasePathfinding(targetPos, chaseSpeedMultiplier);
        
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
        if (!usePathfinding || pathfinding == null)
        {
            // 如果沒有路徑規劃，使用直接移動
            MoveTowards(target, speedMultiplier);
            return;
        }

        // 檢查是否需要更新路徑（使用追擊專用的更新間隔）
        bool shouldUpdatePath = ShouldUpdateChasePath(target);
        
        if (shouldUpdatePath)
        {
            UpdatePathToTarget(target);
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
        if (!usePathfinding || pathfinding == null)
        {
            // 如果沒有路徑規劃，使用直接移動
            MoveTowards(target, speedMultiplier);
            return;
        }

        // 檢查是否需要更新路徑
        bool shouldUpdatePath = ShouldUpdatePath(target);
        
        if (shouldUpdatePath)
        {
            UpdatePathToTarget(target);
        }

        // 沿著路徑移動
        FollowPath(speedMultiplier);
    }

    /// <summary>
    /// 檢查是否需要更新路徑
    /// </summary>
    private bool ShouldUpdatePath(Vector2 target)
    {
        // 如果目標位置改變太多，需要更新路徑
        if (Vector2.Distance(target, lastTargetPosition) > pathReachThreshold)
        {
            return true;
        }

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

        // 定期更新路徑
        if (Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 檢查是否需要更新追擊路徑
    /// </summary>
    private bool ShouldUpdateChasePath(Vector2 target)
    {
        // 如果目標位置改變太多，需要更新路徑
        if (Vector2.Distance(target, lastTargetPosition) > chasePathReachThreshold)
        {
            return true;
        }

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

        // 追擊時更頻繁更新路徑
        if (Time.time - lastPathUpdateTime > chasePathUpdateInterval)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 更新到目標的路徑
    /// </summary>
    private void UpdatePathToTarget(Vector2 target)
    {
        if (pathfinding == null) 
        {
            Debug.LogWarning($"{gameObject.name}: Pathfinding 組件為 null！");
            return;
        }

        Debug.Log($"{gameObject.name}: 開始計算路徑從 {transform.position} 到 {target}");
        
        currentPath = pathfinding.FindPath(transform.position, target);
        currentPathIndex = 0;
        lastPathUpdateTime = Time.time;
        lastTargetPosition = target;

        if (currentPath != null && currentPath.Count > 0)
        {
            Debug.Log($"{gameObject.name}: 找到路徑，包含 {currentPath.Count} 個節點");
        }
        else if (currentPath != null && currentPath.Count == 0)
        {
            Debug.Log($"{gameObject.name}: 已在目標位置附近，無需移動");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: 無法找到路徑到目標 {target}！");
        }
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
            rb.linearVelocity = direction * speed * speedMultiplier;
        }

        // 檢查是否到達當前路徑點
        if (Vector2.Distance(Position, targetPosition) < pathReachThreshold)
        {
            currentPathIndex++;
        }
    }

    /// <summary>
    /// 沿著路徑移動（追擊專用，更平滑）
    /// </summary>
    private void FollowChasePath(float speedMultiplier)
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
        
        // 除錯信息
        if (Time.frameCount % 60 == 0) // 每秒輸出一次
        {
            Debug.Log($"敵人路徑跟隨: 當前索引={currentPathIndex}/{currentPath.Count-1}, " +
                     $"目標位置={targetPosition}, 當前位置={Position}, 距離={Vector2.Distance(Position, targetPosition):F2}");
        }
        
        if (rb != null)
        {
            // 追擊時使用更平滑的速度變化
            Vector2 currentVelocity = rb.linearVelocity;
            Vector2 targetVelocity = direction * speed * speedMultiplier;
            
            // 平滑的速度過渡，避免急轉彎
            Vector2 smoothedVelocity = Vector2.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * 8f);
            rb.linearVelocity = smoothedVelocity;
        }

        // 檢查是否到達當前路徑點（使用更精確的閾值）
        if (Vector2.Distance(Position, targetPosition) < chasePathReachThreshold)
        {
            currentPathIndex++;
            Debug.Log($"到達路徑點 {currentPathIndex-1}，前進到下一個點 {currentPathIndex}");
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
    /// 檢查敵人是否撞牆或卡住
    /// </summary>
    public bool IsStuckOrHittingWall()
    {
        if (rb == null) return false;

        // 檢查速度是否接近零且持續一段時間（可能卡住）
        if (rb.linearVelocity.magnitude < 0.1f)
        {
            // 如果速度為零，檢查是否持續了一段時間
            if (Time.time - lastStuckTime > 0.3f)
            {
                return true;
            }
        }
        else
        {
            // 如果有速度，重置卡住時間
            lastStuckTime = Time.time;
        }

        // 檢查是否在短時間內移動距離很小（可能撞牆）
        Vector2 currentPos = Position;
        if (Vector2.Distance(currentPos, lastPosition) < 0.05f && Time.time - lastPositionUpdateTime > 0.5f)
        {
            return true;
        }

        return false;
    }

    // 用於檢測卡住的變數
    private Vector2 lastPosition;
    private float lastPositionUpdateTime;
    private float lastStuckTime;

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
    /// 設定移動速度（覆寫基類方法）
    /// </summary>
    public override void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    /// <summary>
    /// 獲取移動速度（覆寫基類方法）
    /// </summary>
    public override float GetSpeed()
    {
        return speed;
    }
}