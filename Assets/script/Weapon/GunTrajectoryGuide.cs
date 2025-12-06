using UnityEngine;

/// <summary>
/// 槍械彈道指引線
/// 當玩家持有槍械時，顯示一條虛線表示子彈飛行路徑
/// </summary>
[RequireComponent(typeof(Gun))]
public class GunTrajectoryGuide : MonoBehaviour
{
    [Header("Guide Line Settings")]
    [SerializeField] private Color lineColor = new Color(1f, 0f, 0f, 0.5f); // 紅色半透明
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float lineLength = 5f; // 指引線長度
    [SerializeField] private int segmentCount = 10; // 虛線段數
    [SerializeField] private float dashLength = 0.5f; // 每段虛線的長度
    [SerializeField] private float gapLength = 0.5f; // 虛線間隔
    
    [Header("Jitter Visualization")]
    [Tooltip("抖動區域顏色（更透明）")]
    [SerializeField] private Color jitterAreaColor = new Color(1f, 0.5f, 0f, 0.2f); // 橙色超透明
    [Tooltip("抖動區域邊界線數量（顯示上下邊界）")]
    [SerializeField] private int jitterBoundaryLines = 2; // 上下各一條
    
    [Header("Visibility Settings")]
    [SerializeField] private bool onlyShowForPlayer = true; // 只為玩家顯示
    [SerializeField] private int sortingOrder = 10; // 渲染順序
    
    private LineRenderer lineRenderer; // 中心線
    private LineRenderer[] jitterLineRenderers; // 抖動邊界線（上下）
    private Gun gun;
    private bool isEquippedByPlayer = false;
    private bool isInitialized = false;

    private void Awake()
    {
        gun = GetComponent<Gun>();
    }

    private void Start()
    {
        InitializeLineRenderer();
    }

    private void Update()
    {
        // 檢查是否由玩家裝備
        CheckIfEquippedByPlayer();
        
        // 更新指引線
        UpdateGuideLine();
    }

    /// <summary>
    /// 初始化 LineRenderer
    /// </summary>
    private void InitializeLineRenderer()
    {
        if (isInitialized) return;

        // 創建中心線
        GameObject lineObj = new GameObject("TrajectoryGuideLine_Center");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;

        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = false;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.enabled = false;

        // 創建抖動邊界線（上下各一條）
        jitterLineRenderers = new LineRenderer[jitterBoundaryLines];
        for (int i = 0; i < jitterBoundaryLines; i++)
        {
            GameObject jitterLineObj = new GameObject($"TrajectoryGuideLine_Jitter_{i}");
            jitterLineObj.transform.SetParent(transform);
            jitterLineObj.transform.localPosition = Vector3.zero;
            jitterLineObj.transform.localRotation = Quaternion.identity;

            jitterLineRenderers[i] = jitterLineObj.AddComponent<LineRenderer>();
            jitterLineRenderers[i].material = new Material(Shader.Find("Sprites/Default"));
            jitterLineRenderers[i].startWidth = lineWidth * 0.5f; // 更細
            jitterLineRenderers[i].endWidth = lineWidth * 0.5f;
            jitterLineRenderers[i].startColor = jitterAreaColor;
            jitterLineRenderers[i].endColor = jitterAreaColor;
            jitterLineRenderers[i].useWorldSpace = false;
            jitterLineRenderers[i].sortingOrder = sortingOrder - 1; // 在中心線後面
            jitterLineRenderers[i].enabled = false;
        }

        isInitialized = true;
    }

    /// <summary>
    /// 檢查武器是否由玩家裝備
    /// </summary>
    private void CheckIfEquippedByPlayer()
    {
        bool wasEquippedByPlayer = isEquippedByPlayer;
        isEquippedByPlayer = false;

        Transform current = transform.parent;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                isEquippedByPlayer = true;
                break;
            }

            if (current.CompareTag("Enemy"))
            {
                isEquippedByPlayer = false;
                break;
            }

            current = current.parent;
        }

        // 如果裝備狀態改變，更新顯示
        if (wasEquippedByPlayer != isEquippedByPlayer)
        {
            UpdateLineVisibility();
        }
    }

    /// <summary>
    /// 更新指引線可見性
    /// </summary>
    private void UpdateLineVisibility()
    {
        if (lineRenderer == null) return;

        // 只在玩家裝備時顯示（如果 onlyShowForPlayer 為 true）
        if (onlyShowForPlayer)
        {
            lineRenderer.enabled = isEquippedByPlayer;
        }
        else
        {
            lineRenderer.enabled = true;
        }
    }

    /// <summary>
    /// 更新指引線位置
    /// </summary>
    private void UpdateGuideLine()
    {
        if (lineRenderer == null || !lineRenderer.enabled) return;

        // 計算中心虛線的點
        Vector3[] centerPositions = CalculateDashedLinePositions();
        lineRenderer.positionCount = centerPositions.Length;
        lineRenderer.SetPositions(centerPositions);

        // 檢查是否需要顯示抖動區域
        if (gun != null && gun.EnableMovementJitter && jitterLineRenderers != null)
        {
            float playerSpeed = gun.GetPlayerMovementSpeed();
            
            // 如果玩家移動速度超過閾值，顯示抖動邊界
            if (playerSpeed >= gun.JitterSpeedThreshold)
            {
                UpdateJitterBoundaries(playerSpeed);
            }
            else
            {
                // 隱藏抖動邊界線
                foreach (var jitterLine in jitterLineRenderers)
                {
                    if (jitterLine != null)
                    {
                        jitterLine.enabled = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 更新抖動邊界線（顯示可能的射擊範圍）
    /// </summary>
    private void UpdateJitterBoundaries(float playerSpeed)
    {
        if (gun == null || jitterLineRenderers == null) return;

        float maxLength = gun != null ? Mathf.Min(gun.AttackRange, lineLength) : lineLength;
        
        // 使用最大抖動角度（不隨速度縮放）
        float jitterAngle = gun.MaxJitterAngle;

        // 上邊界線（+角度）
        if (jitterLineRenderers.Length > 0 && jitterLineRenderers[0] != null)
        {
            Vector3[] upperPositions = CalculateDashedLinePositionsWithAngle(jitterAngle);
            jitterLineRenderers[0].positionCount = upperPositions.Length;
            jitterLineRenderers[0].SetPositions(upperPositions);
            jitterLineRenderers[0].enabled = true;
        }

        // 下邊界線（-角度）
        if (jitterLineRenderers.Length > 1 && jitterLineRenderers[1] != null)
        {
            Vector3[] lowerPositions = CalculateDashedLinePositionsWithAngle(-jitterAngle);
            jitterLineRenderers[1].positionCount = lowerPositions.Length;
            jitterLineRenderers[1].SetPositions(lowerPositions);
            jitterLineRenderers[1].enabled = true;
        }
    }

    /// <summary>
    /// 計算虛線的位置點
    /// </summary>
    private Vector3[] CalculateDashedLinePositions()
    {
        return CalculateDashedLinePositionsWithAngle(0f);
    }

    /// <summary>
    /// 計算虛線的位置點（帶角度偏移）
    /// </summary>
    private Vector3[] CalculateDashedLinePositionsWithAngle(float angleDegrees)
    {
        // 使用槍的 attackRange 作為最大長度，但不超過 lineLength
        float maxLength = gun != null ? Mathf.Min(gun.AttackRange, lineLength) : lineLength;
        
        // 計算需要多少個虛線段
        float totalDashUnit = dashLength + gapLength;
        int dashCount = Mathf.CeilToInt(maxLength / totalDashUnit);
        
        // 每段虛線需要2個點（起點和終點）
        int totalPoints = dashCount * 2;
        Vector3[] positions = new Vector3[totalPoints];
        
        // 計算旋轉（用於抖動邊界）
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angleRad);
        float sinAngle = Mathf.Sin(angleRad);
        
        int index = 0;
        for (int i = 0; i < dashCount; i++)
        {
            float startDistance = i * totalDashUnit;
            float endDistance = Mathf.Min(startDistance + dashLength, maxLength);
            
            // 如果起點已經超過最大長度，停止
            if (startDistance >= maxLength) break;
            
            // 虛線段的起點和終點（應用角度旋轉）
            Vector3 startPos = new Vector3(
                startDistance * cosAngle,
                startDistance * sinAngle,
                0
            );
            Vector3 endPos = new Vector3(
                endDistance * cosAngle,
                endDistance * sinAngle,
                0
            );
            
            positions[index++] = startPos;
            positions[index++] = endPos;
            
            // 如果終點達到最大長度，停止
            if (endDistance >= maxLength) break;
        }
        
        // 調整數組大小以匹配實際點數
        if (index < totalPoints)
        {
            System.Array.Resize(ref positions, index);
        }
        
        return positions;
    }

    /// <summary>
    /// 設置指引線顏色
    /// </summary>
    public void SetLineColor(Color color)
    {
        lineColor = color;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    /// <summary>
    /// 設置指引線寬度
    /// </summary>
    public void SetLineWidth(float width)
    {
        lineWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }

    /// <summary>
    /// 手動啟用/禁用指引線
    /// </summary>
    public void SetGuideLineEnabled(bool enabled)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = enabled && (!onlyShowForPlayer || isEquippedByPlayer);
        }
    }

    private void OnDestroy()
    {
        if (lineRenderer != null && lineRenderer.gameObject != null)
        {
            Destroy(lineRenderer.gameObject);
        }

        if (jitterLineRenderers != null)
        {
            foreach (var jitterLine in jitterLineRenderers)
            {
                if (jitterLine != null && jitterLine.gameObject != null)
                {
                    Destroy(jitterLine.gameObject);
                }
            }
        }
    }
}

