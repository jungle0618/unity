using UnityEngine;

/// <summary>
/// 敵人移動控制器
/// 職責：處理移動邏輯、巡邏行為
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("移動參數")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseSpeedMultiplier = 1.5f;
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("巡邏路徑")]
    [SerializeField] private Transform[] patrolPoints;

    private Rigidbody2D rb;
    private Vector2 spawnPoint;
    private int patrolIndex = 0;

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

        Vector2 targetPos = patrolPoints[patrolIndex].position;
        MoveTowards(targetPos, 1f);

        if (Vector2.Distance(Position, targetPos) < arriveThreshold)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
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
    /// 追擊移動
    /// </summary>
    public void ChaseTarget(Vector2 targetPos)
    {
        MoveTowards(targetPos, chaseSpeedMultiplier);
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
    /// 獲取當前巡邏點
    /// </summary>
    public Transform[] GetPatrolPoints()
    {
        return patrolPoints;
    }
}