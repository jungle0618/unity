using UnityEngine;

/// <summary>
/// Target視覺化組件（繼承基礎視覺化組件）
/// 職責：處理運行時視野顯示、血量顏色顯示
/// </summary>
public class TargetVisualizer : BaseVisualizer
{
    private Target target;
    private TargetMovement movement;
    private TargetDetection detection;
    private TargetStateMachine stateMachine;
    private TargetAIHandler aiHandler;
    private bool canVisualize = true; // 是否可以執行視覺化（由 Target 控制，影響 Gizmos 繪製等）
    
    [Header("逃亡路徑視覺化")]
    [SerializeField] private bool showEscapePath = true; // 是否顯示逃亡路徑
    [SerializeField] private Color escapePathColor = new Color(1f, 0f, 0f, 0.8f); // 逃亡路徑顏色（紅色）
    [SerializeField] private Color escapePointColor = new Color(1f, 0.5f, 0f, 1f); // 逃亡點顏色（橙色）
    [SerializeField] private float escapePathWidth = 0.1f; // 逃亡路徑線寬
    [SerializeField] private float escapePointRadius = 0.5f; // 逃亡點標記半徑
    
    // 逃亡路徑視覺化組件
    private LineRenderer escapePathLineRenderer;
    private GameObject escapePathLineObj;
    private GameObject escapePointMarker;
    private SpriteRenderer escapePointRenderer;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        target = GetComponent<Target>();
        movement = GetComponent<TargetMovement>();
        detection = GetComponent<TargetDetection>();
        
        // 訂閱Target血量變化事件
        if (target != null)
        {
            target.OnHealthChanged += HandleHealthChanged;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (target != null)
        {
            target.OnHealthChanged -= HandleHealthChanged;
        }
        
        // 清理逃亡路徑視覺化組件
        CleanupEscapePathVisualization();
    }
    
    /// <summary>
    /// 處理Target血量變化事件
    /// </summary>
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // 調用基類的血量變化處理方法
        OnHealthChanged(currentHealth, maxHealth);
    }

    /// <summary>
    /// 設定狀態機參考
    /// </summary>
    public void SetStateMachine(TargetStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 設定是否可以執行視覺化（由 Target 調用）
    /// 控制視覺化邏輯的執行（如 Gizmos）和所有渲染組件
    /// </summary>
    public void SetCanVisualize(bool canVisualize)
    {
        this.canVisualize = canVisualize;
        
        // 設置所有渲染組件的可見性
        SetRendererVisibility(canVisualize);
    }

    /// <summary>
    /// 設定是否在玩家視野內（用於 PlayerDetection 系統，現已由 EnemyManager 處理）
    /// </summary>
    /// <param name="inView">是否在玩家視野內</param>
    public void SetInPlayerView(bool inView)
    {
        // 可以在這裡實現視覺化邏輯，例如改變透明度或顏色
        // 目前為空實現，保留接口供未來擴展
    }
    
    private void Update()
    {
        // 更新運行時視野顯示
        UpdateRuntimeVisionDisplay();
        
        // 更新逃亡路徑顯示
        UpdateEscapePathDisplay();
    }
    
    /// <summary>
    /// 更新運行時視野顯示
    /// </summary>
    private void UpdateRuntimeVisionDisplay()
    {
        // 如果不可以視覺化或未啟用運行時視野，不執行更新
        if (!canVisualize || !showRuntimeVision || detection == null) return;
        
        Vector3 pos = transform.position;
        float viewRange = detection.ViewRange;
        float viewAngle = detection.ViewAngle;
        
        // 獲取敵人朝向（transform.right 是敵人的前方方向）
        Vector2 viewDirection = transform.right;
        float currentDirection = Mathf.Atan2(viewDirection.y, viewDirection.x) * Mathf.Rad2Deg;
        
        // 計算扇形的起始角度（以當前方向為中心）
        float halfAngle = viewAngle * 0.5f;
        float startAngle = currentDirection - halfAngle;
        
        // 更新運行時視野範圍（使用黃色）
        UpdateRuntimeVision(pos, startAngle, viewAngle, viewRange, runtimeVisionColor);
    }
    
    /// <summary>
    /// 初始化逃亡路徑視覺化組件
    /// </summary>
    private void InitializeEscapePathVisualization()
    {
        // 創建路徑 LineRenderer
        escapePathLineObj = new GameObject("EscapePathLine");
        escapePathLineObj.transform.SetParent(transform);
        escapePathLineObj.transform.localPosition = Vector3.zero;
        
        escapePathLineRenderer = escapePathLineObj.AddComponent<LineRenderer>();
        escapePathLineRenderer.startWidth = escapePathWidth;
        escapePathLineRenderer.endWidth = escapePathWidth;
        
        // 嘗試多種 Shader，確保至少有一個能工作
        Material lineMaterial = null;
        Shader[] shadersTry = new Shader[]
        {
            Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"),
            Shader.Find("Sprites/Default"),
            Shader.Find("Unlit/Color"),
            Shader.Find("UI/Default")
        };
        
        foreach (var shader in shadersTry)
        {
            if (shader != null)
            {
                lineMaterial = new Material(shader);
                break;
            }
        }
        
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        
        escapePathLineRenderer.material = lineMaterial;
        escapePathLineRenderer.startColor = escapePathColor;
        escapePathLineRenderer.endColor = escapePathColor;
        escapePathLineRenderer.sortingLayerName = "Default";
        escapePathLineRenderer.sortingOrder = 100; // 確保在其他物件上方
        escapePathLineRenderer.useWorldSpace = true; // 使用世界空間
        escapePathLineRenderer.enabled = false; // 初始隱藏
        
        Debug.Log($"[TargetVisualizer] 逃亡路徑 LineRenderer 已初始化，使用材質: {lineMaterial.shader.name}");
        
        // 創建逃亡點標記
        escapePointMarker = new GameObject("EscapePointMarker");
        escapePointMarker.transform.SetParent(transform);
        
        escapePointRenderer = escapePointMarker.AddComponent<SpriteRenderer>();
        escapePointRenderer.sprite = CreateCircleSprite(32); // 創建圓形精靈
        escapePointRenderer.color = escapePointColor;
        escapePointMarker.transform.localScale = Vector3.one * escapePointRadius;
        escapePointRenderer.sortingOrder = 99;
        escapePointRenderer.enabled = false; // 初始隱藏
    }
    
    /// <summary>
    /// 清理逃亡路徑視覺化組件
    /// </summary>
    private void CleanupEscapePathVisualization()
    {
        if (escapePathLineObj != null)
        {
            Destroy(escapePathLineObj);
            escapePathLineObj = null;
            escapePathLineRenderer = null;
        }
        
        if (escapePointMarker != null)
        {
            Destroy(escapePointMarker);
            escapePointMarker = null;
            escapePointRenderer = null;
        }
    }
    
    /// <summary>
    /// 更新逃亡路徑顯示
    /// </summary>
    private void UpdateEscapePathDisplay()
    {
        // Debug first frame
        if (Time.frameCount == 1)
        {
            Debug.LogWarning($"[TargetVisualizer] 初始狀態: canVisualize={canVisualize}, showEscapePath={showEscapePath}, aiHandler={(aiHandler != null ? "有" : "無")}, stateMachine={(stateMachine != null ? "有" : "無")}");
        }
        
        if (!canVisualize || !showEscapePath || aiHandler == null || stateMachine == null)
        {
            // 隱藏路徑和標記
            if (escapePathLineRenderer != null) escapePathLineRenderer.enabled = false;
            if (escapePointRenderer != null) escapePointRenderer.enabled = false;
            return;
        }
        
        // 只有在逃亡模式時才顯示路徑
        bool isInEscapeMode = aiHandler.HasEverEnteredEscapeMode();
        
        if (Time.frameCount % 60 == 0 && isInEscapeMode)
        {
            Debug.LogWarning($"[TargetVisualizer] 逃亡模式激活，escapePathLineRenderer={(escapePathLineRenderer != null ? "存在" : "不存在")}");
        }
        
        if (isInEscapeMode)
        {
            // 顯示逃亡點標記
            Vector3 escapePoint = aiHandler.GetEscapePoint();
            if (escapePointMarker != null && escapePoint != Vector3.zero)
            {
                escapePointMarker.transform.position = escapePoint;
                if (escapePointRenderer != null)
                {
                    escapePointRenderer.enabled = true;
                }
            }
            
            // 顯示路徑
            if (movement != null && escapePathLineRenderer != null)
            {
                var path = movement.GetCurrentPath();
                
                // 嘗試使用路徑規劃的路徑
                if (path != null && path.Count > 0)
                {
                    // 使用路徑規劃的路徑
                    Vector3[] pathPoints = new Vector3[path.Count + 1];
                    pathPoints[0] = transform.position; // 起點是當前位置
                    
                    for (int i = 0; i < path.Count; i++)
                    {
                        pathPoints[i + 1] = path[i].worldPosition;
                    }
                    
                    escapePathLineRenderer.positionCount = pathPoints.Length;
                    escapePathLineRenderer.SetPositions(pathPoints);
                    escapePathLineRenderer.enabled = true;
                    
                    // 調試信息（每60幀輸出一次）
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[遊戲視圖] {gameObject.name}: ✓ 繪製路徑規劃路徑，節點數: {path.Count}，LineRenderer.enabled={escapePathLineRenderer.enabled}");
                    }
                }
                else
                {
                    // 沒有路徑規劃，顯示直線到逃亡點
                    if (escapePoint != Vector3.zero)
                    {
                        Vector3[] directPath = new Vector3[2];
                        directPath[0] = transform.position;
                        directPath[1] = escapePoint;
                        
                        escapePathLineRenderer.positionCount = 2;
                        escapePathLineRenderer.SetPositions(directPath);
                        escapePathLineRenderer.enabled = true;
                        
                        // 調試信息（每60幀輸出一次）
                        if (Time.frameCount % 60 == 0)
                        {
                            Debug.LogWarning($"[遊戲視圖] {gameObject.name}: ✓ 繪製直線路徑到逃亡點 {escapePoint}，LineRenderer.enabled={escapePathLineRenderer.enabled}");
                        }
                    }
                    else
                    {
                        escapePathLineRenderer.enabled = false;
                        Debug.LogWarning($"{gameObject.name}: 逃亡點為零，無法繪製路徑");
                    }
                }
            }
        }
        else
        {
            // 不在逃亡模式，隱藏路徑和標記
            if (escapePathLineRenderer != null) escapePathLineRenderer.enabled = false;
            if (escapePointRenderer != null) escapePointRenderer.enabled = false;
        }
    }
    
    /// <summary>
    /// 創建圓形精靈（用於逃亡點標記）
    /// </summary>
    private Sprite CreateCircleSprite(int segments)
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        
        Vector2 center = new Vector2(32, 32);
        float radius = 30;
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    colors[y * 64 + x] = Color.white;
                }
                else
                {
                    colors[y * 64 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
}