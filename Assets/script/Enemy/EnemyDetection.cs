using UnityEngine;

/// <summary>
/// 敵人偵測系統
/// - 視野偵測、距離判斷
/// - 可選：自動面向目標
/// - 與 DangerousManager 串接危險係數更新
/// </summary>
public class EnemyDetection : MonoBehaviour
{
    [Header("偵測參數")]
    [SerializeField] private EnemyStateMachine stateMachine;
    [SerializeField] private float viewRange = 8f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float chaseRange = 15f;

    [Header("障礙物偵測")]
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private bool useRaycastDetection = false;

    [Header("旋轉設定")]
    [SerializeField] private bool lookAtTarget = false; // 是否自動面向玩家
    [SerializeField] private bool lookAtTargetInChase = false; // 追擊時是否面向玩家

    public float ViewRange => viewRange;
    public float ViewAngle => viewAngle;
    public float ChaseRange => chaseRange;

    private Transform target;
    private DangerousManager dangerousManager;

    private void Awake()
    {
        dangerousManager = FindObjectOfType<DangerousManager>();
    }

    private void Update()
    {
        // 每幀彙報距離與可視狀態給 DangerousManager
        if (dangerousManager == null) return;

        bool canSee = CanSeePlayer();
        float distance = GetDistanceToTarget();
        dangerousManager.ReportEnemyPerception(distance, canSee);
    }

    /// <summary>
    /// 設定偵測目標
    /// </summary>
    public void SetTarget(Transform playerTarget)
    {
        target = playerTarget;
    }

    public Transform GetTarget() => target;

    /// <summary>
    /// 檢查是否可以看到玩家
    /// </summary>
    public bool CanSeePlayer()
    {
        if (target == null) return false;
        return CanSeeTarget(target.position);
    }

    /// <summary>
    /// 檢查是否可以看到指定目標
    /// </summary>
    public bool CanSeeTarget(Vector2 targetPos)
    {
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

        // 自動面向目標（只在非追擊狀態時）
        if (lookAtTarget && dirToTarget.magnitude > 0.1f && !IsInChaseState())
        {
            LookAtTarget(dirToTarget);
        }

        return true;
    }

    /// <summary>
    /// 檢查目標是否超出追擊範圍
    /// </summary>
    public bool IsTargetOutOfChaseRange()
    {
        if (target == null) return true;
        return Vector2.Distance(transform.position, target.position) > chaseRange;
    }

    public Vector2 GetDirectionToTarget()
    {
        if (target == null) return Vector2.zero;
        return (target.position - transform.position).normalized;
    }

    public float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        return Vector2.Distance(transform.position, target.position);
    }

    private bool IsBlockedByObstacle(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        float distance = Vector2.Distance(from, to);
        RaycastHit2D hit = Physics2D.Raycast(from, direction, distance, obstacleLayerMask);
        return hit.collider != null;
    }

    public void SetDetectionParameters(float newViewRange, float newViewAngle, float newChaseRange)
    {
        viewRange = newViewRange;
        viewAngle = newViewAngle;
        chaseRange = newChaseRange;
    }

    public void SetRaycastDetection(bool enabled) => useRaycastDetection = enabled;

    public bool HasValidTarget() => target != null;

    public void ClearTarget() => target = null;

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
    /// 設定視野方向（用於巡邏時跟隨移動方向）
    /// </summary>
    public void SetViewDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 獲取當前視野方向
    /// </summary>
    public Vector2 GetViewDirection()
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
    /// 設定追擊時是否面向目標
    /// </summary>
    public void SetLookAtTargetInChase(bool enabled) => lookAtTargetInChase = enabled;
}