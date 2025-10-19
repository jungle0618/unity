using UnityEngine;

/// <summary>
/// �ĤH���ʱ��
/// ¾�d�G�B�z�����޿�B���ަ欰
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("���ʰѼ�")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseSpeedMultiplier = 1.5f;
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("���޸��|")]
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
    /// �]�w�����I
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
        patrolIndex = 0;
    }

    /// <summary>
    /// ���樵�޲���
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
    /// �V�ؼв���
    /// </summary>
    public void MoveTowards(Vector2 target, float speedMultiplier)
    {
        if (rb == null) return;

        Vector2 direction = (target - Position).normalized;
        rb.linearVelocity = direction * speed * speedMultiplier;
    }

    /// <summary>
    /// �l������
    /// </summary>
    public void ChaseTarget(Vector2 targetPos)
    {
        MoveTowards(targetPos, chaseSpeedMultiplier);
    }

    /// <summary>
    /// �����
    /// </summary>
    public void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// �����^�ؼЦ�m
    /// </summary>
    public Vector2 GetReturnTarget()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            return patrolPoints[patrolIndex].position;
        }
        return spawnPoint;
    }

    /// <summary>
    /// �ˬd�O�_��F�ؼЦ�m
    /// </summary>
    public bool HasArrivedAt(Vector2 target)
    {
        return Vector2.Distance(Position, target) < arriveThreshold;
    }

    /// <summary>
    /// �����e�����I
    /// </summary>
    public Transform[] GetPatrolPoints()
    {
        return patrolPoints;
    }
}