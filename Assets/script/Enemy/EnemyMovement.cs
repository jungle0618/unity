using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敵人移動控制器
/// 職責：處理移動邏輯、巡邏行為、路徑規劃
/// </summary>
public class EnemyMovement : MonoBehaviour
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

    private Rigidbody2D rb;
    private Vector2 spawnPoint;
    private int patrolIndex = 0;
    
    // 路徑規劃相關
    private List<PathfindingNode> currentPath = new List<PathfindingNode>();
    private int currentPathIndex = 0;
    private float lastPathUpdateTime = 0f;
    private Vector3 lastTargetPosition;

    public Vector2 Position => transform.position;
    public Vector2 SpawnPoint => spawnPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnPoint = transform.position;

        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}: Missing Rigidbody2D component!");
        }

        // 初始化路徑規劃
        if (pathfinding == null)
        {
            pathfinding = FindObjectOfType<GreedyPathfinding>();
        }
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
    /// 向目標移動
    /// </summary>
    public void MoveTowards(Vector2 target, float speedMultiplier)
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
        MoveTowardsSmart(targetPos, chaseSpeedMultiplier);
    }

    /// <summary>
    /// 追擊移動（帶朝向控制）
    /// </summary>
    public void ChaseTargetWithRotation(Vector2 targetPos, EnemyDetection detection)
    {
        MoveTowardsSmart(targetPos, chaseSpeedMultiplier);
        
        // 在追擊時，讓敵人朝向跟隨移動方向而不是直接朝向玩家
        Vector2 movementDirection = GetMovementDirection();
        if (movementDirection.magnitude > 0.1f && detection != null)
        {
            detection.SetViewDirection(movementDirection);
        }
    }

    /// <summary>
    /// 停止移動
    /// </summary>
    public void StopMovement()
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
    /// 檢查是否到達目標位置
    /// </summary>
    public bool HasArrivedAt(Vector2 target)
    {
        return Vector2.Distance(Position, target) < arriveThreshold;
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
    /// 獲取當前移動方向
    /// </summary>
    public Vector2 GetMovementDirection()
    {
        if (rb == null) return Vector2.right;
        return rb.linearVelocity.normalized;
    }

    /// <summary>
    /// 獲取朝向目標的方向
    /// </summary>
    public Vector2 GetDirectionToTarget(Vector2 target)
    {
        return (target - Position).normalized;
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
    /// 更新到目標的路徑
    /// </summary>
    private void UpdatePathToTarget(Vector2 target)
    {
        if (pathfinding == null) return;

        currentPath = pathfinding.FindPath(transform.position, target);
        currentPathIndex = 0;
        lastPathUpdateTime = Time.time;
        lastTargetPosition = target;

        if (currentPath != null && currentPath.Count > 0)
        {
            Debug.Log($"找到路徑，包含 {currentPath.Count} 個節點");
        }
        else
        {
            Debug.LogWarning("無法找到路徑到目標！");
        }
    }

    /// <summary>
    /// 沿著路徑移動
    /// </summary>
    private void FollowPath(float speedMultiplier)
    {
        if (currentPath == null || currentPath.Count == 0)
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
    /// 清除當前路徑
    /// </summary>
    public void ClearPath()
    {
        currentPath.Clear();
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
}