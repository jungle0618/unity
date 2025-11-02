using UnityEngine;

/// <summary>
/// 基礎視覺化組件抽象類別
/// 提供所有實體共用的視覺化功能接口
/// 包含運行時視野顯示、血量顏色處理等功能
/// </summary>
public abstract class BaseVisualizer : MonoBehaviour
{
    
    [Header("運行時視野顯示設定")]
    [SerializeField] protected bool showRuntimeVision = false; // 是否在遊戲運行時顯示視野範圍
    [SerializeField] protected bool useRuntimeMesh = true; // 是否使用 Mesh 繪製實心視野（否則只顯示輪廓線）
    [SerializeField] protected Material visionMaterial; // 視野範圍的材質（需要支援透明度）
    [SerializeField] protected Color runtimeVisionColor = new Color(0f, 1f, 1f, 0.3f); // 運行時視野顏色
    [SerializeField] protected float runtimeLineWidth = 0.05f; // 輪廓線寬度
    
    [Header("視野範圍設定")]
    [SerializeField] protected LayerMask wallsLayer = -1; // 牆壁圖層遮罩（可在 Inspector 中設定）
    
    [Header("血量顏色設定")]
    [SerializeField] protected bool useHealthColor = true; // 是否啟用血量顏色
    [SerializeField] protected Color healthyColor = Color.white;
    [SerializeField] protected Color damagedColor = Color.yellow;
    [SerializeField] protected Color criticalColor = Color.red;
    [SerializeField] protected float criticalHealthThreshold = 0.3f; // 30%以下為危險血量
    [SerializeField] protected float damagedHealthThreshold = 0.6f; // 60%以下為受傷血量
    
    // SpriteRenderer 引用
    protected SpriteRenderer spriteRenderer;
    
    // 運行時視野渲染組件
    protected LineRenderer visionLineRenderer;
    protected MeshFilter visionMeshFilter;
    protected MeshRenderer visionMeshRenderer;
    protected Mesh visionMesh;
    
    // 快取圖層索引以提高性能
    private int objectsLayerIndex = -1;
    private int wallsLayerIndex = -1;
    private bool layerCacheInitialized = false;
    
    // 視野更新快取
    protected Vector3[] lastVisionPoints;
    protected bool visionNeedsUpdate = true;
    
    /// <summary>
    /// 設置是否在運行時顯示視野範圍
    /// </summary>
    public virtual void SetShowRuntimeVision(bool show)
    {
        showRuntimeVision = show;
        UpdateRuntimeVisionVisibility();
    }
    
    /// <summary>
    /// 獲取是否在運行時顯示視野範圍
    /// </summary>
    public virtual bool GetShowRuntimeVision()
    {
        return showRuntimeVision;
    }
    
    /// <summary>
    /// 更新運行時視野的可見性
    /// </summary>
    protected void UpdateRuntimeVisionVisibility()
    {
        if (visionLineRenderer != null)
        {
            visionLineRenderer.enabled = showRuntimeVision;
        }
        if (visionMeshRenderer != null)
        {
            visionMeshRenderer.enabled = showRuntimeVision && useRuntimeMesh;
        }
    }

    /// <summary>
    /// 設定視覺化顏色（由子類別實現具體邏輯）
    /// </summary>
    public virtual void SetVisualizationColors(params Color[] colors)
    {
        // 子類別可以覆寫此方法
    }
    
    private void Awake()
    {
        // 初始化圖層快取
        InitializeLayerCache();
        // 獲取 SpriteRenderer 組件
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 初始化運行時視野組件
        InitializeRuntimeVision();
        OnInitialize();
    }
    
    private void OnDestroy()
    {
        // 清理運行時創建的 Mesh
        if (visionMesh != null)
        {
            Destroy(visionMesh);
        }
    }
    
    /// <summary>
    /// 子類別可以在這個方法中進行初始化（由 Awake 調用）
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 子類別可以覆寫此方法
    }
    
    /// <summary>
    /// 初始化運行時視野渲染組件
    /// </summary>
    protected virtual void InitializeRuntimeVision()
    {
        if (!showRuntimeVision) return;
        
        // 創建 LineRenderer（用於繪製視野輪廓）
        GameObject lineObj = new GameObject("VisionLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localRotation = Quaternion.identity;
        
        visionLineRenderer = lineObj.AddComponent<LineRenderer>();
        visionLineRenderer.startWidth = runtimeLineWidth;
        visionLineRenderer.endWidth = runtimeLineWidth;
        visionLineRenderer.useWorldSpace = true;
        visionLineRenderer.loop = true;
        visionLineRenderer.material = visionMaterial != null ? visionMaterial : new Material(Shader.Find("Sprites/Default"));
        visionLineRenderer.startColor = new Color(runtimeVisionColor.r, runtimeVisionColor.g, runtimeVisionColor.b, 0.8f);
        visionLineRenderer.endColor = new Color(runtimeVisionColor.r, runtimeVisionColor.g, runtimeVisionColor.b, 0.8f);
        visionLineRenderer.sortingLayerName = "Default";
        visionLineRenderer.sortingOrder = -1;
        
        // 如果需要使用 Mesh 繪製實心視野
        if (useRuntimeMesh)
        {
            GameObject meshObj = new GameObject("VisionMesh");
            meshObj.transform.SetParent(transform);
            meshObj.transform.localPosition = Vector3.zero; // 使用相同的位置
            meshObj.transform.localRotation = Quaternion.identity;
            meshObj.transform.localScale = Vector3.one; // 確保縮放為 1
            
            visionMeshFilter = meshObj.AddComponent<MeshFilter>();
            visionMeshRenderer = meshObj.AddComponent<MeshRenderer>();
            
            visionMesh = new Mesh();
            visionMesh.name = "VisionMesh";
            visionMeshFilter.mesh = visionMesh;
            
            // 設置材質
            if (visionMaterial != null)
            {
                visionMeshRenderer.material = visionMaterial;
            }
            else
            {
                // 創建默認透明材質
                Material defaultMat = new Material(Shader.Find("Sprites/Default"));
                defaultMat.color = runtimeVisionColor;
                visionMeshRenderer.material = defaultMat;
            }
            
            visionMeshRenderer.sortingLayerName = "Default";
            visionMeshRenderer.sortingOrder = -2;
        }
        
        UpdateRuntimeVisionVisibility();
    }
    
    /// <summary>
    /// 初始化圖層快取以提高性能
    /// </summary>
    private void InitializeLayerCache()
    {
        objectsLayerIndex = LayerMask.NameToLayer("Objects");
        wallsLayerIndex = LayerMask.NameToLayer("Walls");
        
        // 如果找不到大寫版本，嘗試小寫版本
        if (objectsLayerIndex == -1)
        {
            objectsLayerIndex = LayerMask.NameToLayer("objects");
        }
        if (wallsLayerIndex == -1)
        {
            wallsLayerIndex = LayerMask.NameToLayer("walls");
        }
        
        layerCacheInitialized = true;
    }
    
    /// <summary>
    /// 獲取需要檢測的圖層遮罩（只檢測 Walls 圖層，子類別可以覆寫以添加其他圖層）
    /// </summary>
    protected virtual LayerMask GetObstacleLayerMask()
    {
        // 如果已經在 Inspector 中設定了圖層遮罩，優先使用
        if (wallsLayer != -1)
        {
            return wallsLayer;
        }
        
        // 自動獲取 Walls 圖層
        int wallsLayerIndex = LayerMask.NameToLayer("Walls");
        if (wallsLayerIndex == -1)
        {
            wallsLayerIndex = LayerMask.NameToLayer("walls");
        }
        
        if (wallsLayerIndex != -1)
        {
            return 1 << wallsLayerIndex;
        }
        
        return 0;
    }
    
    /// <summary>
    /// 檢查圖層是否為障礙物（Objects 或 Walls）
    /// </summary>
    protected bool IsObstacleLayer(int layerIndex)
    {
        if (!layerCacheInitialized)
        {
            InitializeLayerCache();
        }
        
        // 使用快取的圖層索引進行快速比較
        if (objectsLayerIndex != -1 && layerIndex == objectsLayerIndex)
        {
            return true;
        }
        if (wallsLayerIndex != -1 && layerIndex == wallsLayerIndex)
        {
            return true;
        }
        
        // 如果快取失敗，使用圖層名稱檢查作為備用方案
        string layerName = LayerMask.LayerToName(layerIndex);
        return layerName == "Objects" || layerName == "Walls" || 
               layerName == "objects" || layerName == "walls";
    }
    
    #region 運行時視野繪製
    
    /// <summary>
    /// 計算並更新運行時視野範圍（帶射線檢測）
    /// 由子類別在 Update 或需要更新視野時調用
    /// </summary>
    protected void UpdateRuntimeVision(Vector3 center, float startAngle, float angle, float range, Color color)
    {
        if (!showRuntimeVision) return;
        if (visionLineRenderer == null && visionMeshRenderer == null) return;
        
        LayerMask obstacleMask = GetObstacleLayerMask();
        
        // 計算射線數量（每2度一個射線）
        int rayCount = Mathf.Max(1, Mathf.RoundToInt(angle / 2f));
        float angleStep = angle / rayCount;
        
        // 儲存每個射線的終點
        Vector3[] rayEndPoints = new Vector3[rayCount + 1];
        
        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector2 direction = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad), 
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            );
            
            // 進行射線檢測
            RaycastHit2D hit = Physics2D.Raycast(center, direction, range, obstacleMask);
            
            if (hit.collider != null && IsObstacleLayer(hit.collider.gameObject.layer))
            {
                rayEndPoints[i] = hit.point;
            }
            else
            {
                rayEndPoints[i] = center + (Vector3)(direction * range);
            }
        }
        
        // 更新 LineRenderer
        UpdateVisionLineRenderer(rayEndPoints);
        
        // 更新 Mesh（如果啟用）
        if (useRuntimeMesh && visionMeshFilter != null)
        {
            UpdateVisionMesh(center, rayEndPoints, color);
        }
        
        lastVisionPoints = rayEndPoints;
    }
    
    /// <summary>
    /// 更新視野範圍 LineRenderer
    /// </summary>
    private void UpdateVisionLineRenderer(Vector3[] points)
    {
        if (visionLineRenderer == null || points == null || points.Length < 2) return;
        
        visionLineRenderer.positionCount = points.Length;
        visionLineRenderer.SetPositions(points);
    }
    
    /// <summary>
    /// 更新視野範圍 Mesh
    /// </summary>
    private void UpdateVisionMesh(Vector3 center, Vector3[] points, Color color)
    {
        if (visionMesh == null || visionMeshFilter == null || points == null || points.Length < 3) return;
        
        visionMesh.Clear();
        
        // 創建頂點陣列（中心點 + 所有射線終點）
        Vector3[] vertices = new Vector3[points.Length + 1];
        
        // 將所有點轉換為局部座標（相對於 VisionMesh GameObject 自己的座標系）
        vertices[0] = visionMeshFilter.transform.InverseTransformPoint(center); // 中心點
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i + 1] = visionMeshFilter.transform.InverseTransformPoint(points[i]);
        }
        
        // 創建三角形索引
        int[] triangles = new int[(points.Length - 1) * 3];
        for (int i = 0; i < points.Length - 1; i++)
        {
            triangles[i * 3] = 0; // 中心點
            triangles[i * 3 + 1] = i + 1; // 當前點
            triangles[i * 3 + 2] = i + 2; // 下一個點
        }
        
        // 創建 UV 座標
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
        }
        
        // 創建顏色陣列
        Color[] colors = new Color[vertices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }
        
        // 應用到 Mesh
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.uv = uvs;
        visionMesh.colors = colors;
        visionMesh.RecalculateNormals();
        visionMesh.RecalculateBounds();
    }
    
    #endregion
    
    #region 血量顏色處理
    
    /// <summary>
    /// 設定血量顏色（可由外部調用）
    /// </summary>
    public void SetHealthColors(Color healthy, Color damaged, Color critical)
    {
        healthyColor = healthy;
        damagedColor = damaged;
        criticalColor = critical;
        // 立即更新顏色（如果有血量百分比資訊）
        // 子類別應在設定後調用 UpdateSpriteColorByHealth
    }
    
    /// <summary>
    /// 設定血量閾值
    /// </summary>
    public void SetHealthThresholds(float criticalThreshold, float damagedThreshold)
    {
        criticalHealthThreshold = criticalThreshold;
        damagedHealthThreshold = damagedThreshold;
    }
    
    /// <summary>
    /// 根據血量百分比更新 Sprite Renderer 的顏色
    /// </summary>
    /// <param name="healthPercentage">血量百分比 (0.0 - 1.0)</param>
    protected void UpdateSpriteColorByHealth(float healthPercentage)
    {
        if (!useHealthColor || spriteRenderer == null) return;
        
        Color targetColor;
        
        // 根據血量百分比決定顏色
        if (healthPercentage <= criticalHealthThreshold)
        {
            targetColor = criticalColor;
        }
        else if (healthPercentage <= damagedHealthThreshold)
        {
            targetColor = damagedColor;
        }
        else
        {
            targetColor = healthyColor;
        }
        
        spriteRenderer.color = targetColor;
    }
    
    /// <summary>
    /// 血量變化處理（由子類別在收到血量變化事件時調用）
    /// </summary>
    /// <param name="currentHealth">當前血量</param>
    /// <param name="maxHealth">最大血量</param>
    protected void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (maxHealth > 0)
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            UpdateSpriteColorByHealth(healthPercentage);
        }
    }
    
    #endregion
}

