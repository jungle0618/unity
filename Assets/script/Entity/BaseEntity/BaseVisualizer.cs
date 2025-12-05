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
    
    [Header("血量條設定")]
    [SerializeField] protected float healthBarHeight = 0.2f; // 血量條高度（相對於 entity 大小）
    [SerializeField] protected float healthBarOffset = 0.5f; // 血量條距離 entity 下方的偏移量（相對於 entity 大小）
    [SerializeField] protected Color healthBarBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 灰色背景（確保 alpha = 1）
    [SerializeField] protected Color healthBarForegroundColor = new Color(0f, 1f, 0f, 1f); // 綠色前景（確保 alpha = 1）
    
    // SpriteRenderer 引用
    protected SpriteRenderer spriteRenderer;
    
    // ItemHolder 引用（用於控制物品的渲染）
    protected ItemHolder itemHolder;
    
    // 血量條組件
    protected GameObject healthBarBackgroundObj; // 灰色背景長方形
    protected GameObject healthBarForegroundObj; // 綠色前景長方形
    protected SpriteRenderer healthBarBackgroundRenderer;
    protected SpriteRenderer healthBarForegroundRenderer;
    protected float healthBarWidth; // 血量條寬度（基於 entity 大小）
    protected float healthBarHeightValue; // 血量條高度值（基於 entity 大小）
    protected float healthBarOffsetY; // 血量條在 Y 軸的偏移量（基於 entity 大小，在世界空間中）
    
    // 運行時視野渲染組件
    protected LineRenderer visionLineRenderer;
    protected GameObject visionLineObj; // 存儲 LineRenderer 的 GameObject 引用，用於清理
    protected MeshFilter visionMeshFilter;
    protected MeshRenderer visionMeshRenderer;
    protected GameObject visionMeshObj; // 存儲 MeshRenderer 的 GameObject 引用，用於清理
    protected Mesh visionMesh;
    
    // 快取圖層索引以提高性能
    private int objectsLayerIndex = -1;
    private int wallsLayerIndex = -1;
    private bool layerCacheInitialized = false;
    
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
    /// 設置所有渲染組件的可見性（由子類別調用）
    /// </summary>
    public virtual void SetRendererVisibility(bool visible)
    {
        // 禁用/啟用實體自身的 SpriteRenderer
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
        
        // 禁用/啟用視野相關的渲染組件
        if (visionLineRenderer != null)
        {
            visionLineRenderer.enabled = visible && showRuntimeVision;
        }
        if (visionMeshRenderer != null)
        {
            visionMeshRenderer.enabled = visible && showRuntimeVision && useRuntimeMesh;
        }
        
        // 禁用/啟用血量條渲染組件
        if (healthBarBackgroundRenderer != null)
        {
            healthBarBackgroundRenderer.enabled = visible;
        }
        if (healthBarForegroundRenderer != null)
        {
            healthBarForegroundRenderer.enabled = visible;
        }
        
        // 禁用/啟用所有物品（武器和鑰匙）的 renderer，防止看的到物品看不到人
        // 物品的 SpriteRenderer 可能在子物件上
        if (itemHolder != null)
        {
            // 獲取所有物品（包括武器和鑰匙）
            var allItems = itemHolder.GetAllItems();
            foreach (var item in allItems)
            {
                if (item != null && item.gameObject != null)
                {
                    // 使用 GetComponentsInChildren 來查找所有子物件中的 SpriteRenderer（包括自己）
                    SpriteRenderer[] itemRenderers = item.gameObject.GetComponentsInChildren<SpriteRenderer>();
                    foreach (SpriteRenderer itemRenderer in itemRenderers)
                    {
                        if (itemRenderer != null)
                        {
                            itemRenderer.enabled = visible;
                        }
                    }
                }
            }
        }
    }

    private void Awake()
    {
        // 初始化圖層快取
        InitializeLayerCache();
        // 獲取 SpriteRenderer 組件
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 獲取 ItemHolder 組件
        itemHolder = GetComponent<ItemHolder>();
        // 初始化運行時視野組件
        InitializeRuntimeVision();
        OnInitialize();
    }
    
    private void Start()
    {
        // 在 Start 中初始化血量條，確保 SpriteRenderer 已經完全初始化
        InitializeHealthBar();
    }
    
    private void LateUpdate()
    {
        // 每幀更新血量條位置，使其跟隨 entity 位置但不跟隨旋轉
        UpdateHealthBarPosition();
    }
    
    /// <summary>
    /// 清理所有創建的對象（由 BaseEntity 在死亡時調用）
    /// </summary>
    public void CleanupCreatedObjects()
    {
        // 清理運行時視野相關對象
        if (visionLineObj != null)
        {
            Destroy(visionLineObj);
            visionLineObj = null;
            visionLineRenderer = null;
        }
        
        if (visionMeshObj != null)
        {
            Destroy(visionMeshObj);
            visionMeshObj = null;
            visionMeshFilter = null;
            visionMeshRenderer = null;
        }
        
        // 清理運行時創建的 Mesh
        if (visionMesh != null)
        {
            Destroy(visionMesh);
            visionMesh = null;
        }
        
        // 清理血量條 GameObject
        if (healthBarBackgroundObj != null)
        {
            Destroy(healthBarBackgroundObj);
            healthBarBackgroundObj = null;
            healthBarBackgroundRenderer = null;
        }
        if (healthBarForegroundObj != null)
        {
            Destroy(healthBarForegroundObj);
            healthBarForegroundObj = null;
            healthBarForegroundRenderer = null;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱 EntityHealth 事件
        EntityHealth entityHealth = GetComponent<EntityHealth>();
        if (entityHealth != null)
        {
            entityHealth.OnHealthChanged -= OnHealthChanged;
        }
        
        // 清理所有創建的對象
        CleanupCreatedObjects();
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
        visionLineObj = new GameObject("VisionLine");
        visionLineObj.transform.SetParent(transform);
        visionLineObj.transform.localPosition = Vector3.zero;
        visionLineObj.transform.localRotation = Quaternion.identity;
        
        visionLineRenderer = visionLineObj.AddComponent<LineRenderer>();
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
            visionMeshObj = new GameObject("VisionMesh");
            visionMeshObj.transform.SetParent(transform);
            visionMeshObj.transform.localPosition = Vector3.zero; // 使用相同的位置
            visionMeshObj.transform.localRotation = Quaternion.identity;
            visionMeshObj.transform.localScale = Vector3.one; // 確保縮放為 1
            
            visionMeshFilter = visionMeshObj.AddComponent<MeshFilter>();
            visionMeshRenderer = visionMeshObj.AddComponent<MeshRenderer>();
            
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
    
    #region 血量條處理
    
    /// <summary>
    /// 更新血量條位置（使其跟隨 entity 位置但不跟隨旋轉）
    /// </summary>
    private void UpdateHealthBarPosition()
    {
        if (healthBarBackgroundObj == null || healthBarForegroundObj == null) return;
        
        // 計算血量條在世界空間中的位置（entity 位置 + 世界空間向下偏移）
        // 使用世界空間的向下方向，不跟隨 entity 旋轉
        Vector3 worldPosition = transform.position + new Vector3(0, healthBarOffsetY, 0);
        
        // 更新位置，但保持旋轉為 identity（不旋轉）
        healthBarBackgroundObj.transform.position = worldPosition;
        healthBarBackgroundObj.transform.rotation = Quaternion.identity;
        
        // 計算前景條的位置（基於當前血量百分比）
        float healthPercentage = 1.0f;
        EntityHealth entityHealth = GetComponent<EntityHealth>();
        if (entityHealth != null && entityHealth.MaxHealth > 0)
        {
            healthPercentage = (float)entityHealth.CurrentHealth / entityHealth.MaxHealth;
        }
        float foregroundWidth = healthBarWidth * healthPercentage;
        if (foregroundWidth < 0.01f) foregroundWidth = 0.01f;
        
        // 計算前景條的 X 偏移以保持左端對齊
        float offsetX = (foregroundWidth - healthBarWidth) * 0.5f;
        healthBarForegroundObj.transform.position = worldPosition + new Vector3(offsetX, 0, 0);
        healthBarForegroundObj.transform.rotation = Quaternion.identity;
    }
    
    /// <summary>
    /// 初始化血量條
    /// </summary>
    protected virtual void InitializeHealthBar()
    {
        if (spriteRenderer == null) return;
        if (spriteRenderer.sprite == null) return; // 確保 sprite 已載入
        
        // 獲取 entity 的邊界大小
        Bounds spriteBounds = spriteRenderer.bounds;
        // 如果 bounds 為空或無效，使用 sprite 的本地邊界
        if (spriteBounds.size.x <= 0 || spriteBounds.size.y <= 0)
        {
            Bounds spriteLocalBounds = spriteRenderer.sprite.bounds;
            // 將 sprite 的本地邊界轉換為世界空間（考慮 transform 的 scale）
            Vector3 scale = transform.lossyScale;
            spriteBounds = new Bounds(
                spriteLocalBounds.center,
                new Vector3(
                    spriteLocalBounds.size.x * scale.x,
                    spriteLocalBounds.size.y * scale.y,
                    spriteLocalBounds.size.z * scale.z
                )
            );
        }
        float entityWidth = spriteBounds.size.x;
        float entityHeight = spriteBounds.size.y;
        
        // 計算血量條的尺寸（基於 entity 大小）
        healthBarWidth = entityWidth;
        healthBarHeightValue = entityHeight * healthBarHeight;
        // 計算血量條在世界空間中的 Y 軸偏移量（向下偏移，不跟隨 entity 旋轉）
        healthBarOffsetY = -(entityHeight * 0.5f + entityHeight * healthBarOffset);
        
        // 確保尺寸不為 0
        if (healthBarWidth <= 0) healthBarWidth = 1f;
        if (healthBarHeightValue <= 0) healthBarHeightValue = 0.1f;
        
        // 創建白色 Sprite（用於長方形）
        Sprite whiteSprite = CreateWhiteSprite();
        
        // 創建灰色背景長方形（不設置為子物件，保持在世界空間）
        healthBarBackgroundObj = new GameObject("HealthBarBackground");
        healthBarBackgroundObj.transform.SetParent(null); // 不設置父物件，保持在世界空間
        healthBarBackgroundObj.transform.position = transform.position + new Vector3(0, healthBarOffsetY, 0);
        healthBarBackgroundObj.transform.rotation = Quaternion.identity; // 保持不旋轉
        healthBarBackgroundObj.transform.localScale = Vector3.one; // 使用單位 scale，用 sprite 大小控制尺寸
        
        healthBarBackgroundRenderer = healthBarBackgroundObj.AddComponent<SpriteRenderer>();
        healthBarBackgroundRenderer.sprite = whiteSprite;
        healthBarBackgroundRenderer.color = healthBarBackgroundColor;
        healthBarBackgroundRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        healthBarBackgroundRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        healthBarBackgroundRenderer.enabled = true; // 確保 renderer 啟用
        
        // 設置初始大小（使用 transform scale）
        healthBarBackgroundObj.transform.localScale = new Vector3(healthBarWidth, healthBarHeightValue, 1f);
        
        // 創建綠色前景長方形（不設置為子物件，保持在世界空間）
        healthBarForegroundObj = new GameObject("HealthBarForeground");
        healthBarForegroundObj.transform.SetParent(null); // 不設置父物件，保持在世界空間
        healthBarForegroundObj.transform.position = transform.position + new Vector3(0, healthBarOffsetY, 0);
        healthBarForegroundObj.transform.rotation = Quaternion.identity; // 保持不旋轉
        healthBarForegroundObj.transform.localScale = Vector3.one; // 使用單位 scale
        
        healthBarForegroundRenderer = healthBarForegroundObj.AddComponent<SpriteRenderer>();
        healthBarForegroundRenderer.sprite = whiteSprite;
        healthBarForegroundRenderer.color = healthBarForegroundColor;
        healthBarForegroundRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        healthBarForegroundRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        healthBarForegroundRenderer.enabled = true; // 確保 renderer 啟用
        
        // 設置初始大小
        healthBarForegroundObj.transform.localScale = new Vector3(healthBarWidth, healthBarHeightValue, 1f);
        
        // 初始狀態隱藏血量條（血量為100%時不顯示）
        healthBarBackgroundObj.SetActive(false);
        healthBarForegroundObj.SetActive(false);
        
        // 初始化後，嘗試獲取當前血量並更新顯示
        // 通過 EntityHealth 組件獲取血量
        EntityHealth entityHealth = GetComponent<EntityHealth>();
        if (entityHealth != null)
        {
            // 直接訂閱 EntityHealth 的事件（不通過 Player/Enemy/Target 的委託）
            // 這樣可以確保即使實體類別的事件訂閱失敗，也能收到血量變化事件
            entityHealth.OnHealthChanged += OnHealthChanged;
            
            // 立即更新一次顯示
            UpdateHealthBar(entityHealth.CurrentHealth, entityHealth.MaxHealth);
        }
    }
    
    /// <summary>
    /// 創建白色 Sprite（用於血量條）
    /// </summary>
    private Sprite CreateWhiteSprite()
    {
        // 創建一個 1x1 的白色紋理
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        // 創建 Sprite
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }
    
    /// <summary>
    /// 更新血量條顯示
    /// </summary>
    /// <param name="currentHealth">當前血量</param>
    /// <param name="maxHealth">最大血量</param>
    protected virtual void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarBackgroundObj == null || healthBarForegroundObj == null) return;
        if (maxHealth <= 0) return;
        
        float healthPercentage = (float)currentHealth / maxHealth;

        // 當血量為100%時隱藏血量條
        if (healthPercentage >= 1.0f)
        {
            healthBarBackgroundObj.SetActive(false);
            healthBarForegroundObj.SetActive(false);
            return;
        }
        
        // 顯示血量條
        healthBarBackgroundObj.SetActive(true);
        healthBarForegroundObj.SetActive(true);
        
        // 確保 renderer 啟用
        if (healthBarBackgroundRenderer != null)
        {
            healthBarBackgroundRenderer.enabled = true;
        }
        if (healthBarForegroundRenderer != null)
        {
            healthBarForegroundRenderer.enabled = true;
        }
        
        // 計算綠色長方形的寬度（基於血量百分比）
        float foregroundWidth = healthBarWidth * healthPercentage;
        
        // 確保寬度不小於一個最小值
        if (foregroundWidth < 0.01f)
        {
            foregroundWidth = 0.01f;
        }
        
        // 更新綠色長方形的寬度
        // 由於使用 Sprite 和 localScale，我們需要調整 scale.x
        healthBarForegroundObj.transform.localScale = new Vector3(foregroundWidth, healthBarHeightValue, 1f);
        
        // 位置更新會在 LateUpdate 中統一處理，這裡只更新 scale
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
            UpdateHealthBar(currentHealth, maxHealth);
        }
    }
    
    #endregion
}

