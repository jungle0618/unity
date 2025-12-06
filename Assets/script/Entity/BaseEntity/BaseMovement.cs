using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 基礎移動組件抽象類別
/// 提供所有實體共用的移動功能接口
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class BaseMovement : MonoBehaviour
{
    protected Rigidbody2D rb;

    // 智能移動模式追蹤
    private enum MoveMode { Direct, Pathfinding }
    private MoveMode currentMoveMode = MoveMode.Direct;
    private Vector2 currentMoveTarget;
    private Vector2 lastMoveCheckPosition; // 上次檢查移動模式時的位置
    private bool hasHitWall = false; // 是否撞到牆

    // 路徑規劃相關
    protected List<PathfindingNode> currentPath = new List<PathfindingNode>();
    protected int currentPathIndex = 0;
    protected Vector3 lastPathUpdatePosition;
    protected Vector3 lastTargetPosition;

    // 巡邏相關（子類可以通過 SerializeField 覆蓋）
    [SerializeField] protected Transform[] patrolPoints;
    protected int patrolIndex = 0;
    protected Vector2 spawnPoint;

    // 卡住檢測相關
    protected Vector2 lastPosition;
    protected float lastPositionUpdateTime;
    protected float lastStuckTime;

    // 路徑規劃參數（子類可以通過 SerializeField 覆蓋）
    protected virtual bool UsePathfinding() => true;
    protected virtual float GetPathUpdateDistance() => 2f;
    protected virtual float GetPathReachThreshold() => 0.5f;
    protected virtual float GetTargetPositionChangeThreshold() => 0.5f;
    protected virtual LayerMask GetObstaclesLayerMask() => 0;
    protected virtual float GetDirectChaseCheckRadius() => 0.3f;
    protected virtual float GetArriveThreshold() => 0.2f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}: Missing Rigidbody2D component!");
        }

        // 初始化移動模式追蹤
        lastMoveCheckPosition = transform.position;
        currentMoveMode = MoveMode.Direct;
        hasHitWall = false;
        lastPathUpdatePosition = transform.position;
        
        // 初始化巡邏和卡住檢測
        spawnPoint = transform.position;
        lastStuckTime = Time.time;
        lastPosition = transform.position;
        lastPositionUpdateTime = Time.time;
    }

    /// <summary>
    /// 向目標位置移動（智能移動：自動判斷直線或路徑規劃，自動獲取速度倍數）
    /// </summary>
    /// <param name="target">目標位置</param>
    public virtual void MoveTowards(Vector2 target)
    {
        if (rb == null) return;

        // 如果目標改變，立即判斷要直線移動還是路徑規劃
        if (Vector2.Distance(target, currentMoveTarget) > 1f)
        {
            currentMoveTarget = target;
            lastMoveCheckPosition = transform.position;
            ClearPath();
            
            // 立即判斷是否可以直線到達
            bool canReachDirectly = CanReachDirectly(target);
            if (canReachDirectly)
            {
                // 可以直線到達，使用直線移動
                currentMoveMode = MoveMode.Direct;
                hasHitWall = false;
            }
            else
            {
                // 不能直線到達，使用路徑規劃
                if (UsePathfinding() && HasPathfinding())
                {
                    currentMoveMode = MoveMode.Pathfinding;
                    hasHitWall = true;
                }
                else
                {
                    // 沒有路徑規劃，強制使用直線移動
                    currentMoveMode = MoveMode.Direct;
                    hasHitWall = false;
                }
            }
        }

        // 檢查是否需要重新評估移動模式（移動了 pathUpdateDistance 後）
        float movedDistance = Vector2.Distance(transform.position, lastMoveCheckPosition);
        if (movedDistance >= GetPathUpdateDistance())
        {
            // 如果當前使用直線移動，檢查是否撞牆
            if (currentMoveMode == MoveMode.Direct)
            {
                bool canReachDirectly = CanReachDirectly(target);
                if (!canReachDirectly && UsePathfinding() && HasPathfinding())
                {
                    // 撞到牆，切換到路徑規劃，並標記已撞牆
                    currentMoveMode = MoveMode.Pathfinding;
                    hasHitWall = true;
                    ClearPath();
                }
            }
            // 一旦撞牆，就一直使用 pathfinding 直到抵達目標，不再切換回直線
            
            lastMoveCheckPosition = transform.position;
        }

        // 根據當前模式執行移動
        if (currentMoveMode == MoveMode.Direct)
        {
            // 直線移動
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            float speed = CalculateSpeed();
            rb.linearVelocity = direction * speed;
        }
        else
        {
            // 路徑規劃移動
            MoveTowardsWithPathfindingInternal(target);
        }
    }
    
    /// <summary>
    /// 向目標位置移動（需要指定速度倍數）
    /// </summary>
    /// <param name="target">目標位置</param>
    /// <param name="speedMultiplier">速度倍數</param>
    public abstract void MoveTowards(Vector2 target, float speedMultiplier);
    
    /// <summary>
    /// 獲取速度倍數（由子類實現，用於自動獲取速度倍數）
    /// </summary>
    protected virtual float GetSpeedMultiplier()
    {
        // 默認返回 1.0，子類可以覆寫此方法來提供自動獲取速度倍數的邏輯
        return 1.0f;
    }
    
    /// <summary>
    /// 獲取基礎速度（由子類實現）
    /// </summary>
    protected abstract float GetBaseSpeed();
    
    /// <summary>
    /// 計算速度（基礎速度 * 狀態速度倍數 * 受傷倍數）
    /// </summary>
    protected float CalculateSpeed()
    {
        return GetBaseSpeed() * GetSpeedMultiplier() * GetInjurySpeedMultiplier();
    }
    
    /// <summary>
    /// 獲取受傷速度倍數（如果受傷則返回 0.7，否則返回 1.0）
    /// </summary>
    protected float GetInjurySpeedMultiplier()
    {
        // 從 EntityHealth 組件檢查是否受傷
        EntityHealth health = GetComponent<EntityHealth>();
        if (health != null && !health.IsDead)
        {
            // 如果當前血量小於最大血量，表示受傷
            if (health.CurrentHealth < health.MaxHealth)
            {
                return 0.7f; // 受傷時速度乘以 0.7
            }
        }
        return 1.0f; // 未受傷時正常速度
    }

    /// <summary>
    /// 檢查是否有可用的路徑規劃組件（由子類實現）
    /// </summary>
    protected abstract bool HasPathfinding();

    /// <summary>
    /// 獲取路徑規劃組件並計算路徑（由子類實現）
    /// </summary>
    protected abstract List<PathfindingNode> FindPath(Vector2 start, Vector2 target);

    /// <summary>
    /// 檢查是否能直線到達目標（公開方法，供外部調用）
    /// </summary>
    public bool CanReachDirectly(Vector2 target)
    {
        LayerMask obstaclesLayerMask = GetObstaclesLayerMask();
        if (obstaclesLayerMask == 0) return true; // 如果沒有設置障礙物層，假設可以直線到達

        Vector2 direction = (target - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, target);
        
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, GetDirectChaseCheckRadius(), direction, distance, obstaclesLayerMask);
        return hit.collider == null;
    }

    /// <summary>
    /// 內部路徑規劃移動方法（不檢查移動模式，自動獲取速度倍數）
    /// </summary>
    private void MoveTowardsWithPathfindingInternal(Vector2 target)
    {
        if (!UsePathfinding() || !HasPathfinding())
        {
            // 如果沒有路徑規劃，使用直接移動
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            if (rb != null)
            {
                float speed = CalculateSpeed();
                rb.linearVelocity = direction * speed;
            }
            return;
        }

        // 檢查是否需要更新路徑
        bool shouldUpdatePath = ShouldUpdatePath(target);
        
        if (shouldUpdatePath)
        {
            UpdatePathToTargetSafe(target);
        }

        // 沿著路徑移動
        FollowPath();
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
        if (Vector2.Distance(target, lastTargetPosition) > GetTargetPositionChangeThreshold())
        {
            return true;
        }

        // 如果實體移動了足夠的距離，需要更新路徑
        float movedDistance = Vector2.Distance(transform.position, lastPathUpdatePosition);
        if (movedDistance > GetPathUpdateDistance())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 更新路徑（自動處理不可行走的起點和終點）
    /// </summary>
    protected void UpdatePathToTargetSafe(Vector2 target)
    {
        if (!HasPathfinding()) return;

        PathfindingGrid grid = FindFirstObjectByType<PathfindingGrid>();
        if (grid == null) return;

        // 調整起點和終點到可行走位置
        Vector2 start = AdjustToWalkable(grid, transform.position, 2f);
        Vector2 adjustedTarget = AdjustToWalkable(grid, target, 3f);

        currentPath = FindPath(start, adjustedTarget);
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
    /// 沿著路徑移動（自動獲取速度倍數）
    /// </summary>
    private void FollowPath()
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
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        
        if (rb != null)
        {
            float speed = CalculateSpeed();
            rb.linearVelocity = direction * speed;
        }

        // 檢查是否到達當前路徑點
        if (Vector2.Distance(transform.position, targetPosition) < GetPathReachThreshold())
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
        return transform.position;
    }

    /// <summary>
    /// 獲取到下一個路徑點的方向
    /// </summary>
    public Vector2 GetDirectionToNextPathPoint()
    {
        if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            Vector2 nextPoint = currentPath[currentPathIndex].worldPosition;
            return (nextPoint - (Vector2)transform.position).normalized;
        }
        return Vector2.zero;
    }

    /// <summary>
    /// 獲取當前是否使用直線移動（基於當前移動模式）
    /// </summary>
    public bool IsUsingDirectMovement()
    {
        return currentMoveMode == MoveMode.Direct;
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
    /// 獲取當前巡邏點
    /// </summary>
    public Transform[] GetPatrolPoints()
    {
        return patrolPoints;
    }

    /// <summary>
    /// 檢查是否到達指定的location（使用自訂閾值）
    /// </summary>
    public bool HasArrivedAtLocation(Vector3 location)
    {
        return Vector2.Distance(transform.position, location) < GetArriveThreshold();
    }

    /// <summary>
    /// 檢查是否到達目標位置（使用自訂閾值）
    /// </summary>
    public bool HasArrivedAtWithCustomThreshold(Vector2 target)
    {
        return HasArrivedAt(target, GetArriveThreshold());
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
    /// 檢查實體是否卡住
    /// </summary>
    public bool IsStuckOrHittingWall()
    {
        if (rb == null) return false;

        float stuckThreshold = IsUsingDirectMovement() ? 1.0f : 0.5f;
        
        if (rb.linearVelocity.magnitude < 0.1f)
        {
            if (Time.time - lastStuckTime > stuckThreshold) return true;
        }
        else
        {
            lastStuckTime = Time.time;
        }

        if (Vector2.Distance(transform.position, lastPosition) < 0.05f && Time.time - lastPositionUpdateTime > 0.5f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 前進到下一個巡邏點（超過最後一個則回到第一個）
    /// </summary>
    protected void AdvancePatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    /// <summary>
    /// 更新位置追蹤（用於卡住檢測）
    /// </summary>
    protected virtual void Update()
    {
        // 更新位置追蹤
        if (Time.time - lastPositionUpdateTime > 0.1f)
        {
            lastPosition = transform.position;
            lastPositionUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// 在 LateUpdate 中處理朝向移動方向，避免與物理更新衝突
    /// </summary>
    protected virtual void LateUpdate()
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
    /// 停止移動
    /// </summary>
    public virtual void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// 獲取當前移動方向
    /// </summary>
    public virtual Vector2 GetMovementDirection()
    {
        if (rb == null) return Vector2.right;
        return rb.linearVelocity.normalized;
    }

    /// <summary>
    /// 獲取朝向目標的方向
    /// </summary>
    public virtual Vector2 GetDirectionToTarget(Vector2 target)
    {
        return (target - (Vector2)transform.position).normalized;
    }

    /// <summary>
    /// 檢查是否到達目標位置
    /// </summary>
    /// <param name="target">目標位置</param>
    /// <param name="threshold">到達閾值</param>
    public virtual bool HasArrivedAt(Vector2 target, float threshold = 0.2f)
    {
        return Vector2.Distance(transform.position, target) < threshold;
    }

    /// <summary>
    /// 設定移動速度（由子類別實現具體邏輯）
    /// </summary>
    public virtual void SetSpeed(float speed)
    {
        // 子類別可以覆寫此方法
    }

    /// <summary>
    /// 獲取移動速度（由子類別實現具體邏輯）
    /// </summary>
    public virtual float GetSpeed()
    {
        return 0f; // 子類別需要實現
    }
}

