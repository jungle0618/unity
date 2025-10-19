using UnityEngine;

/// <summary>
/// �ĤH�����t��
/// - ���������B�Z���P�_
/// - �i��G�۰ʭ��V�ؼ�
/// </summary>
public class EnemyDetection : MonoBehaviour
{
    [Header("�����Ѽ�")]
    [SerializeField] private EnemyStateMachine stateMachine;
    [SerializeField] private float viewRange = 8f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float chaseRange = 15f;

    [Header("��ê������")]
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private bool useRaycastDetection = false;

    [Header("����]�w")]
    [SerializeField] private bool lookAtTarget = false; // �O�_�۰ʭ��V���a

    public float ViewRange => viewRange;
    public float ViewAngle => viewAngle;
    public float ChaseRange => chaseRange;

    private Transform target;



    /// <summary>
    /// �]�w�����ؼ�
    /// </summary>
    public void SetTarget(Transform playerTarget)
    {
        target = playerTarget;
    }

    public Transform GetTarget() => target;

    /// <summary>
    /// �ˬd�O�_�i�H�ݨ쪱�a
    /// </summary>
    public bool CanSeePlayer()
    {
        if (target == null) return false;
        return CanSeeTarget(target.position);
    }

    /// <summary>
    /// �ˬd�O�_�i�H�ݨ���w�ؼ�
    /// </summary>
    public bool CanSeeTarget(Vector2 targetPos)
    {
        Vector2 currentPos = transform.position;
        Vector2 dirToTarget = targetPos - currentPos;

        // �Z���ˬd
        if (dirToTarget.magnitude > viewRange)
            return false;

        // �����ˬd�G�H transform.rotation �����
        float angle = Vector2.Angle(transform.right, dirToTarget.normalized);
        if (angle > viewAngle * 0.5f)
            return false;

        // ��ê���ˬd
        if (useRaycastDetection && IsBlockedByObstacle(currentPos, targetPos))
            return false;

        return true;
    }

    /// <summary>
    /// �ˬd�ؼЬO�_�W�X�l���d��
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
}
