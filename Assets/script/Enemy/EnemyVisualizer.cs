using UnityEngine;

/// <summary>
/// 敵人視覺化組件
/// 職責：處理 Gizmo 繪製、狀態顏色顯示
/// </summary>
public class EnemyVisualizer : MonoBehaviour
{
    [Header("視覺化設定")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color normalColor = Color.yellow;
    [SerializeField] private Color alertColor = Color.orange;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color deadColor = Color.gray;

    private EnemyDetection detection;
    private EnemyMovement movement;
    private EnemyStateMachine stateMachine;

    private void Awake()
    {
        detection = GetComponent<EnemyDetection>();
        movement = GetComponent<EnemyMovement>();
    }

    /// <summary>
    /// 設定狀態機參考
    /// </summary>
    public void SetStateMachine(EnemyStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 開關調試 Gizmos
    /// </summary>
    public void SetShowDebugGizmos(bool show)
    {
        showDebugGizmos = show;
    }

    /// <summary>
    /// 設定狀態顏色
    /// </summary>
    public void SetStateColors(Color normal, Color alert, Color chase, Color dead)
    {
        normalColor = normal;
        alertColor = alert;
        chaseColor = chase;
        deadColor = dead;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector3 pos = transform.position;
        Color currentColor = GetCurrentStateColor();

        // 偵測範圍
        if (detection != null)
        {
            Gizmos.color = currentColor;
            //Gizmos.DrawWireSphere(pos, detection.ViewRange);

            // 視野角度
            DrawViewAngle(pos, currentColor);

            // 追擊範圍
            Gizmos.color = Color.magenta;
            //Gizmos.DrawWireSphere(pos, detection.ChaseRange);
        }

        // 巡邏點
        DrawPatrolPoints();
    }

    private void DrawViewAngle(Vector3 center, Color color)
    {
        if (detection == null) return;

        Gizmos.color = color;

        float halfAngle = detection.ViewAngle * 0.5f;
        Vector3 forward = transform.right;

        // 計算視野邊界
        Vector3 leftBoundary = Quaternion.Euler(0, 0, halfAngle) * forward * detection.ViewRange;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -halfAngle) * forward * detection.ViewRange;

        Gizmos.DrawLine(center, center + leftBoundary);
        Gizmos.DrawLine(center, center + rightBoundary);
    }

    private void DrawPatrolPoints()
    {
        if (movement == null) return;

        Transform[] patrolPoints = movement.GetPatrolPoints();
        if (patrolPoints == null) return;

        Gizmos.color = Color.blue;
        foreach (var point in patrolPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, 0.2f);
            }
        }

        // 繪製巡邏路徑連線
        if (patrolPoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }
        }
    }

    private Color GetCurrentStateColor()
    {
        if (stateMachine == null) return normalColor;

        return stateMachine.CurrentState switch
        {
            EnemyState.Alert => alertColor,
            EnemyState.Chase => chaseColor,
            EnemyState.Dead => deadColor,
            _ => normalColor
        };
    }

    /// <summary>
    /// 在場景視圖中顯示狀態文字
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || stateMachine == null) return;

        Vector3 labelPos = transform.position + Vector3.up * 1.5f;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, $"State: {stateMachine.CurrentState}");
#endif
    }
}