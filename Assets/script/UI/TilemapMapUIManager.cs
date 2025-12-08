using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// Tilemap 地圖UI管理器
/// 使用手動繪製方式生成地圖圖片並顯示在 UI 上
/// </summary>
public class TilemapMapUIManager : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Tilemap tilemap;                   // 要顯示的 Tilemap（主地圖）
    [SerializeField] private bool autoFindTilemap = true;       // 自動尋找 Tilemap
    [SerializeField] private MapExplorationManager explorationManager; // 地圖探索管理器
    
    [Header("Additional Tilemaps for Map Display")]
    [SerializeField] private Tilemap wallsTilemap;               // 牆壁 Tilemap
    [SerializeField] private Tilemap objectsTilemap;             // 物件 Tilemap
    [SerializeField] private Tilemap doorTilemap;                // 門 Tilemap
    [SerializeField] private Tilemap fogTilemap;                  // 霧層 Tilemap
    
    [Header("Map Tile Colors")]
    [SerializeField] private Color wallsColor = Color.gray;        // 牆壁顏色
    [SerializeField] private Color objectsColor = Color.yellow;   // 物件顏色
    [SerializeField] private Color doorColor = Color.blue;        // 門顏色
    [SerializeField] private Color fogColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // 霧層顏色（灰色）
    [SerializeField] private Color backgroundColor = Color.black; // 背景顏色
    
    [Header("Map Display")]
    [SerializeField] private RawImage mapDisplay;               // 顯示地圖的 RawImage
    [SerializeField] private RectTransform mapContainer;        // 地圖容器
    [SerializeField] private RectTransform mapBorderRect;       // 地圖邊框矩形
    
    [Header("Map Render Settings")]
    [SerializeField] private int renderTextureSize = 512;       // RenderTexture 大小
    
    [Header("Player Icon")]
    [SerializeField] private RectTransform playerIcon;          // 玩家圖標
    [SerializeField] private bool rotateWithPlayer = true;      // 玩家圖標是否跟隨旋轉
    
    [Header("Update Settings")]
    [SerializeField] private bool updateInRealtime = true;      // 是否即時更新
    [SerializeField] private float updateInterval = 0.1f;       // 更新間隔（秒）
    
    [Header("Markers")]
    [SerializeField] private GameObject mapMarkerPrefab;        // 地圖標記預製體
    [SerializeField] private Transform markersContainer;        // 標記容器
    
    [Header("References")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private bool useEntityManager = true; // 是否使用 EntityManager 獲取 Player
    
    [Header("Map Zoom Settings")]
    [SerializeField] private KeyCode zoomKey = KeyCode.V; // 放大鍵
    [SerializeField] private float zoomScale = 7.5f; // 地圖放大倍率
    [SerializeField] private float playerIconZoomScale = 1.5f; // 玩家圖標放大倍率（相對於原始大小）
    [SerializeField] private float transitionDuration = 0.2f; // 過渡動畫時間
    [SerializeField] private bool useSmoothTransition = true; // 是否使用平滑過渡
    
    [Header("Border Settings")]
    [SerializeField] private float borderWidth = 2f; // 邊框寬度
    [SerializeField] private Color borderColor = Color.black; // 邊框顏色（黑色）
    
    private Player player;
    private EntityManager entityManager;
    private bool isInitialized = false;
    private RenderTexture mapRenderTexture;
    private List<MapMarker> mapMarkers = new List<MapMarker>();
    private bool isVisible = false;
    private float lastUpdateTime = 0f;
    private float lastPlayerUpdateTime = 0f; // 玩家位置更新的獨立計時器
    
    // Tilemap 邊界
    private Bounds tilemapBounds;
    
    // 地圖縮放狀態
    private bool isZoomed = false;
    private Vector2 originalAnchoredPosition; // 原始位置
    private Vector2 originalSizeDelta; // 原始大小
    private Vector3 originalLocalScale; // 原始縮放
    private Vector2 zoomedAnchoredPosition; // 放大後位置（畫面中間）
    private Vector2 zoomedSizeDelta; // 放大後大小
    private Vector3 zoomedLocalScale; // 放大後縮放
    private bool hasStoredOriginalValues = false; // 是否已儲存原始值
    private float transitionTimer = 0f; // 過渡計時器
    
    // 玩家圖標縮放狀態
    private Vector3 originalPlayerIconScale; // 玩家圖標原始縮放
    private Vector3 zoomedPlayerIconScale; // 玩家圖標放大後的縮放
    
    private void Awake()
    {
        // 強制使用程式碼預設值（覆蓋場景中保存的值）
        zoomScale = 4.5f;
        playerIconZoomScale = 1.5f;
        
        // 嘗試獲取 EntityManager 引用
        if (useEntityManager)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
        }
    }
    
    /// <summary>
    /// 初始化地圖UI
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 優先從 EntityManager 獲取 Player（如果可用）
        if (useEntityManager && entityManager != null)
        {
            player = entityManager.Player;
            
            // 如果 Player 還沒準備好，訂閱事件
            if (player == null && entityManager != null)
            {
                entityManager.OnPlayerReady += HandlePlayerReady;
                // 繼續初始化其他部分（不依賴 Player 的部分）
            }
        }
        else if (autoFindPlayer)
        {
            // 備用方案：直接查找
            player = FindFirstObjectByType<Player>();
        }
        
        // 尋找 Tilemap
        if (autoFindTilemap && tilemap == null)
        {
            tilemap = FindFirstObjectByType<Tilemap>();
        }
        
        if (tilemap == null)
        {
            Debug.LogError("TilemapMapUIManager: 找不到 Tilemap！");
            return;
        }
        
        // 獲取 Tilemap 邊界
        UpdateTilemapBounds();
        
        // 設置探索管理器
        SetupExplorationManager();
        
        // 自動尋找未指定的 Tilemap
        AutoFindTilemaps();
        
        // 創建 RenderTexture
        SetupRenderTexture();
        
        // 如果 Player 已準備好，設置地圖UI
        if (player != null)
        {
            SetupMapUI();
        }
        else
        {
            Debug.LogWarning("TilemapMapUIManager: 找不到 Player，等待 Player 準備就緒...");
        }
        
        if (playerIcon == null)
        {
            Debug.LogWarning("TilemapMapUIManager: 玩家圖標未設定");
        }
        
        // 設置邊框
        SetupMapBorder();
    }
    
    /// <summary>
    /// 設置地圖邊框
    /// </summary>
    private void SetupMapBorder()
    {
        if (mapBorderRect == null || mapContainer == null) return;
        
        RectTransform containerRect = mapContainer;
        if (containerRect == null) return;
        
        // 設置錨點：完全填充地圖容器
        mapBorderRect.anchorMin = new Vector2(0f, 0f);
        mapBorderRect.anchorMax = new Vector2(1f, 1f);
        mapBorderRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 設置偏移，讓邊框比容器大 borderWidth
        mapBorderRect.offsetMin = new Vector2(-borderWidth, -borderWidth);
        mapBorderRect.offsetMax = new Vector2(borderWidth, borderWidth);
        
        // 確保邊框在最底層
        mapBorderRect.SetAsFirstSibling();
        
        // 自動獲取或添加 Image 組件
        Image borderImage = mapBorderRect.GetComponent<Image>();
        if (borderImage == null)
        {
            borderImage = mapBorderRect.gameObject.AddComponent<Image>();
        }
        
        if (borderImage != null)
        {
            borderImage.color = borderColor;
        }
    }
    
    /// <summary>
    /// 處理 Player 準備就緒事件
    /// </summary>
    private void HandlePlayerReady()
    {
        if (isInitialized) return;
        
        if (entityManager != null)
        {
            player = entityManager.Player;
            SetupMapUI();
            
            // 取消訂閱（只需要一次）
            entityManager.OnPlayerReady -= HandlePlayerReady;
        }
    }
    
    /// <summary>
    /// 設置地圖UI
    /// </summary>
    private void SetupMapUI()
    {
        if (player == null) return;
        
        isInitialized = true;
        
        // 更新玩家位置
        UpdatePlayerPosition();
        
        Debug.Log($"TilemapMapUIManager: 地圖UI已初始化 (Tilemap範圍: {tilemapBounds})");
    }
    
    private void Update()
    {
        // 確保所有 Tilemap 的渲染器設置正確（每幀檢查，確保 fog 和 mainTilemap 沒有渲染器）
        EnsureTilemapRenderers();
        
        // 定期重新生成地圖圖片（如果地圖可見且需要即時更新）
        if (isVisible && updateInRealtime)
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                GenerateMapImage();
                lastUpdateTime = Time.time;
            }
        }
        
        // 處理地圖縮放輸入
        HandleMapZoomInput();
        
        // 處理過渡動畫
        if (useSmoothTransition && transitionTimer > 0f)
        {
            UpdateMapTransition();
        }
        
        // 只有在地圖可見且需要即時更新時才更新玩家位置
        if (isVisible && updateInRealtime && player != null)
        {
            // 使用獨立的更新間隔來降低性能消耗
            if (Time.time - lastPlayerUpdateTime >= updateInterval)
            {
                UpdatePlayerPosition();
                lastPlayerUpdateTime = Time.time;
            }
        }
    }
    
    private void OnDestroy()
    {
        // 釋放 RenderTexture
        if (mapRenderTexture != null)
        {
            mapRenderTexture.Release();
            Destroy(mapRenderTexture);
        }
        
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
        
        // 當地圖顯示時，立即更新
        if (visible && player != null)
        {
            UpdatePlayerPosition();
            lastUpdateTime = Time.time;
            lastPlayerUpdateTime = Time.time;
            
            // 當地圖顯示時，儲存原始值（如果還沒儲存）
            if (!hasStoredOriginalValues && mapContainer != null)
            {
                StoreOriginalValues();
            }
        }
        
        // 如果地圖被隱藏，重置縮放狀態
        if (!visible && isZoomed)
        {
            SetMapZoomed(false, false);
        }
    }
    
    /// <summary>
    /// 設定玩家（如果需要動態設定）
    /// </summary>
    public void SetPlayer(Player newPlayer)
    {
        player = newPlayer;
        
        if (isVisible && player != null)
        {
            UpdatePlayerPosition();
        }
    }
    
    #region Tilemap 相關
    
    /// <summary>
    /// 自動尋找未指定的 Tilemap
    /// </summary>
    private void AutoFindTilemaps()
    {
        // 如果沒有指定 fogTilemap，嘗試從探索管理器獲取
        if (fogTilemap == null && explorationManager != null)
        {
            fogTilemap = explorationManager.GetComponentInChildren<Tilemap>();
            if (fogTilemap != null && fogTilemap.gameObject.name != "FogTilemap")
            {
                fogTilemap = null; // 如果不是 FogTilemap，清除引用
            }
        }
        
        // 如果仍然沒有找到，嘗試通過名稱查找
        if (fogTilemap == null)
        {
            GameObject fogObj = GameObject.Find("FogTilemap");
            if (fogObj != null)
            {
                fogTilemap = fogObj.GetComponent<Tilemap>();
            }
        }
        
        // 嘗試通過名稱查找其他 Tilemap（如果未指定）
        if (wallsTilemap == null)
        {
            GameObject wallsObj = GameObject.Find("walls");
            if (wallsObj == null) wallsObj = GameObject.Find("Walls");
            if (wallsObj != null)
            {
                wallsTilemap = wallsObj.GetComponent<Tilemap>();
            }
        }
        
        if (objectsTilemap == null)
        {
            GameObject objectsObj = GameObject.Find("objects");
            if (objectsObj == null) objectsObj = GameObject.Find("Objects");
            if (objectsObj != null)
            {
                objectsTilemap = objectsObj.GetComponent<Tilemap>();
            }
        }
        
        if (doorTilemap == null)
        {
            GameObject doorObj = GameObject.Find("door");
            if (doorObj == null) doorObj = GameObject.Find("Door");
            if (doorObj != null)
            {
                doorTilemap = doorObj.GetComponent<Tilemap>();
            }
        }
        
        Debug.Log($"TilemapMapUIManager: 已自動尋找 Tilemap - Walls: {wallsTilemap != null}, Objects: {objectsTilemap != null}, Door: {doorTilemap != null}, Fog: {fogTilemap != null}");
    }
    
    /// <summary>
    /// 設置探索管理器
    /// </summary>
    private void SetupExplorationManager()
    {
        // 如果沒有指定探索管理器，嘗試自動尋找
        if (explorationManager == null)
        {
            explorationManager = FindFirstObjectByType<MapExplorationManager>();
        }
        
        if (explorationManager != null)
        {
            Debug.Log("TilemapMapUIManager: 已找到探索管理器");
        }
    }
    
    /// <summary>
    /// 確保所有 Tilemap 的渲染器設置正確（fogTilemap 和 mainTilemap 不添加永久渲染器）
    /// </summary>
    private void EnsureTilemapRenderers()
    {
        // 確保其他 Tilemap 的渲染器啟用（walls, objects, door）
        // 注意：fogTilemap 和 mainTilemap (tilemap) 不添加永久渲染器
        // 它們只在 GenerateMapImage() 時臨時添加渲染器
        EnsureTilemapRenderer(wallsTilemap, 0);
        EnsureTilemapRenderer(objectsTilemap, 1);
        EnsureTilemapRenderer(doorTilemap, 2);
        
        // 確保 fogTilemap 和 mainTilemap 沒有渲染器（如果有的話，移除它）
        if (fogTilemap != null)
        {
            TilemapRenderer fogRenderer = fogTilemap.GetComponent<TilemapRenderer>();
            if (fogRenderer != null)
            {
                DestroyImmediate(fogRenderer);
            }
        }
        
        if (tilemap != null)
        {
            TilemapRenderer mainRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (mainRenderer != null)
            {
                DestroyImmediate(mainRenderer);
            }
        }
    }
    
    /// <summary>
    /// 確保指定 Tilemap 的渲染器設置正確
    /// </summary>
    private void EnsureTilemapRenderer(Tilemap tilemap, int sortingOrder)
    {
        if (tilemap == null) return;
        
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            // 如果沒有渲染器，添加一個
            renderer = tilemap.gameObject.AddComponent<TilemapRenderer>();
            Debug.Log($"TilemapMapUIManager: 已為 {tilemap.gameObject.name} 添加 TilemapRenderer");
        }
        
        if (renderer != null)
        {
            if (sortingOrder >= 0)
            {
                renderer.sortingOrder = sortingOrder;
            }
            renderer.enabled = true;
        }
    }
    
    /// <summary>
    /// 生成地圖圖片（完全手動繪製，使用指定的顏色，不讀取 tilemap 的圖片）
    /// </summary>
    private void GenerateMapImage()
    {
        if (mapRenderTexture == null || tilemap == null) return;
        
        // 獲取 Tilemap 的邊界和單元格大小
        BoundsInt bounds = tilemap.cellBounds;
        Vector3 cellSize = tilemap.cellSize;
        Vector3 cellGap = tilemap.cellGap;
        
        // 計算實際的單元格大小（包括間隙）
        float cellWidth = cellSize.x + cellGap.x;
        float cellHeight = cellSize.y + cellGap.y;
        
        // 計算地圖在世界空間中的範圍
        Vector3 mapMin = tilemap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));
        Vector3 mapMax = tilemap.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMax, 0));
        float mapWidth = mapMax.x - mapMin.x;
        float mapHeight = mapMax.y - mapMin.y;
        
        // 創建臨時 Texture2D 用於繪製
        Texture2D tempTexture = new Texture2D(renderTextureSize, renderTextureSize, TextureFormat.RGBA32, false);
        
        // 填充背景顏色
        Color[] pixels = new Color[renderTextureSize * renderTextureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        tempTexture.SetPixels(pixels);
        
        // 繪製各個 Tilemap 層（從底層到頂層）
        // 1. 主地圖（背景層，實際上不需要繪製，因為已經填充了背景色）
        
        // 2. 牆壁層
        DrawTilemapLayer(tempTexture, wallsTilemap, wallsColor, mapMin, mapWidth, mapHeight, bounds);
        
        // 3. 物件層
        DrawTilemapLayer(tempTexture, objectsTilemap, objectsColor, mapMin, mapWidth, mapHeight, bounds);
        
        // 4. 門層
        DrawTilemapLayer(tempTexture, doorTilemap, doorColor, mapMin, mapWidth, mapHeight, bounds);
        
        // 5. 霧層（最上層）
        DrawTilemapLayer(tempTexture, fogTilemap, fogColor, mapMin, mapWidth, mapHeight, bounds);
        
        // 應用所有像素更改
        tempTexture.Apply();
        
        // 將 Texture2D 複製到 RenderTexture
        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = mapRenderTexture;
        Graphics.Blit(tempTexture, mapRenderTexture);
        RenderTexture.active = previousRT;
        
        // 清理臨時 Texture2D
        DestroyImmediate(tempTexture);
    }
    
    /// <summary>
    /// 在 Texture2D 上繪製 Tilemap 層（只繪製有 tile 的位置，使用指定顏色）
    /// </summary>
    private void DrawTilemapLayer(Texture2D texture, Tilemap tilemap, Color color, Vector3 mapMin, float mapWidth, float mapHeight, BoundsInt bounds)
    {
        if (tilemap == null) return;
        
        // 獲取該 Tilemap 的邊界
        BoundsInt tilemapBounds = tilemap.cellBounds;
        Vector3 cellSize = tilemap.cellSize;
        Vector3 cellGap = tilemap.cellGap;
        
        // 計算實際的單元格大小（包括間隙）
        float actualCellWidth = cellSize.x + cellGap.x;
        float actualCellHeight = cellSize.y + cellGap.y;
        
        // 遍歷所有有 tile 的位置
        foreach (Vector3Int pos in tilemapBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(pos) != null)
            {
                // 將單元格位置轉換為世界座標（左下角）
                Vector3 cellWorldMin = tilemap.CellToWorld(pos);
                
                // 計算單元格的右上角世界座標
                Vector3 cellWorldMax = cellWorldMin + new Vector3(actualCellWidth, actualCellHeight, 0);
                
                // 計算在 Texture2D 中的像素位置（相對於地圖範圍）
                float normalizedXMin = (cellWorldMin.x - mapMin.x) / mapWidth;
                float normalizedYMin = (cellWorldMin.y - mapMin.y) / mapHeight;
                float normalizedXMax = (cellWorldMax.x - mapMin.x) / mapWidth;
                float normalizedYMax = (cellWorldMax.y - mapMin.y) / mapHeight;
                
                // 轉換為像素座標（使用 Ceil 和 Floor 確保覆蓋完整範圍）
                int pixelXMin = Mathf.FloorToInt(normalizedXMin * renderTextureSize);
                int pixelYMin = Mathf.FloorToInt(normalizedYMin * renderTextureSize);
                int pixelXMax = Mathf.CeilToInt(normalizedXMax * renderTextureSize);
                int pixelYMax = Mathf.CeilToInt(normalizedYMax * renderTextureSize);
                
                // 計算單元格在像素中的大小（確保至少為 1）
                int cellPixelWidth = Mathf.Max(1, pixelXMax - pixelXMin);
                int cellPixelHeight = Mathf.Max(1, pixelYMax - pixelYMin);
                
                // 繪製單元格（填充矩形區域，確保覆蓋邊界）
                for (int y = 0; y < cellPixelHeight; y++)
                {
                    for (int x = 0; x < cellPixelWidth; x++)
                    {
                        int px = pixelXMin + x;
                        int py = pixelYMin + y;
                        
                        // 確保在範圍內
                        if (px >= 0 && px < renderTextureSize && py >= 0 && py < renderTextureSize)
                        {
                            // 對於霧層，直接覆蓋（非透明），確保蓋過其他層
                            if (tilemap == fogTilemap)
                            {
                                // 確保 fog 顏色是非透明的（alpha = 1）
                                Color opaqueFogColor = new Color(color.r, color.g, color.b, 1f);
                                texture.SetPixel(px, py, opaqueFogColor);
                            }
                            else
                            {
                                texture.SetPixel(px, py, color);
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 更新 Tilemap 邊界
    /// </summary>
    private void UpdateTilemapBounds()
    {
        if (tilemap == null) return;
        
        // 獲取 Tilemap 的實際使用邊界
        tilemap.CompressBounds();
        tilemapBounds = tilemap.localBounds;
        
        Debug.Log($"Tilemap 邊界更新: Center={tilemapBounds.center}, Size={tilemapBounds.size}");
    }
    
    /// <summary>
    /// 重新計算 Tilemap 邊界（當 Tilemap 改變時調用）
    /// </summary>
    public void RefreshTilemapBounds()
    {
        UpdateTilemapBounds();
    }
    
    #endregion
    
    
    #region RenderTexture 設定
    
    /// <summary>
    /// 設定 RenderTexture
    /// </summary>
    private void SetupRenderTexture()
    {
        if (mapDisplay == null) return;
        
        // 創建 RenderTexture（不再需要相機）
        mapRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
        mapRenderTexture.Create();
        
        // 確保所有 Tilemap 的渲染器設置正確（walls, objects, door 需要渲染器用於實際遊戲顯示）
        EnsureTilemapRenderers();
        
        // 手動生成地圖圖片（使用指定的顏色，不讀取 tilemap 的圖片）
        GenerateMapImage();
        
        // 設定 RawImage 顯示
        mapDisplay.texture = mapRenderTexture;
        
        // 確保 RawImage 啟用
        if (mapDisplay != null)
        {
            mapDisplay.enabled = true;
        }
        
        Debug.Log($"RenderTexture 已創建: {renderTextureSize}x{renderTextureSize}, 使用手動繪製方式生成地圖");
    }
    
    #endregion
    
    #region 玩家位置
    
    /// <summary>
    /// 更新玩家在地圖上的位置
    /// </summary>
    private void UpdatePlayerPosition()
    {
        if (playerIcon == null || player == null || mapDisplay == null)
        {
            if (playerIcon == null) Debug.LogWarning("TilemapMapUIManager: playerIcon 為 null");
            if (player == null) Debug.LogWarning("TilemapMapUIManager: player 為 null");
            if (mapDisplay == null) Debug.LogWarning("TilemapMapUIManager: mapDisplay 為 null");
            return;
        }
        
        // 將世界座標轉換為地圖UI座標
        Vector3 worldPos = player.transform.position;
        Vector2 mapPos = WorldToMapUIPosition(worldPos);
        
        // 更新玩家圖標位置
        playerIcon.anchoredPosition = mapPos;
        
        // 調試信息（只在必要時啟用）
        // Debug.Log($"UpdatePlayerPosition: worldPos={worldPos}, mapPos={mapPos}, playerIcon.anchoredPosition={playerIcon.anchoredPosition}");
        
        // 更新玩家圖標旋轉（如果需要）
        if (rotateWithPlayer)
        {
            float angle = -player.transform.eulerAngles.y;
            playerIcon.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    /// <summary>
    /// 將世界座標轉換為地圖UI座標（公開方法供其他組件使用）
    /// </summary>
    public Vector2 WorldToMapUIPosition(Vector3 worldPosition)
    {
        if (tilemap == null || mapDisplay == null) return Vector2.zero;
        
        // 使用與 GenerateMapImage 相同的計算方式
        BoundsInt bounds = tilemap.cellBounds;
        
        // 計算地圖在世界空間中的範圍（與 GenerateMapImage 保持一致）
        Vector3 mapMin = tilemap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));
        Vector3 mapMax = tilemap.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMax, 0));
        float mapWidth = mapMax.x - mapMin.x;
        float mapHeight = mapMax.y - mapMin.y;
        
        // 如果地圖寬高為 0，返回零向量
        if (mapWidth <= 0 || mapHeight <= 0) return Vector2.zero;
        
        // 計算相對於地圖最小點的位置
        Vector3 relativePos = worldPosition - mapMin;
        
        // 轉換為標準化座標（0 到 1）
        float normalizedX = relativePos.x / mapWidth;
        float normalizedY = relativePos.y / mapHeight;
        
        // 轉換為地圖UI座標（相對於 mapDisplay 的 rect）
        Rect rect = mapDisplay.rectTransform.rect;
        
        // 將標準化座標（0-1）轉換為 UI 座標（從 rect 的左下角開始）
        float uiX = normalizedX * rect.width - rect.width * 0.5f;
        float uiY = normalizedY * rect.height - rect.height * 0.5f;
        
        return new Vector2(uiX, uiY);
    }
    
    #endregion
    
    #region 地圖標記管理
    
    /// <summary>
    /// 添加地圖標記
    /// </summary>
    public MapMarker AddMarker(Vector3 worldPosition, string markerName = "Marker")
    {
        if (mapMarkerPrefab == null)
        {
            Debug.LogWarning("TilemapMapUIManager: 地圖標記預製體未設定");
            return null;
        }
        
        Transform container = markersContainer != null ? markersContainer : mapContainer;
        if (container == null)
        {
            Debug.LogWarning("TilemapMapUIManager: 找不到標記容器");
            return null;
        }
        
        // 創建標記
        GameObject markerObj = Instantiate(mapMarkerPrefab, container);
        MapMarker marker = markerObj.GetComponent<MapMarker>();
        
        if (marker != null)
        {
            // 設定標記位置
            RectTransform markerRect = markerObj.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                Vector2 mapPos = WorldToMapUIPosition(worldPosition);
                markerRect.anchoredPosition = mapPos;
            }
            
            marker.SetWorldPosition(worldPosition);
            marker.SetMarkerName(markerName);
            mapMarkers.Add(marker);
        }
        else
        {
            Debug.LogError("TilemapMapUIManager: 標記預製體缺少 MapMarker 組件！");
            Destroy(markerObj);
        }
        
        return marker;
    }
    
    /// <summary>
    /// 移除地圖標記
    /// </summary>
    public void RemoveMarker(MapMarker marker)
    {
        if (marker != null && mapMarkers.Contains(marker))
        {
            mapMarkers.Remove(marker);
            Destroy(marker.gameObject);
        }
    }
    
    /// <summary>
    /// 清空所有標記
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (var marker in mapMarkers)
        {
            if (marker != null)
                Destroy(marker.gameObject);
        }
        mapMarkers.Clear();
    }
    
    /// <summary>
    /// 獲取所有標記
    /// </summary>
    public IReadOnlyList<MapMarker> GetAllMarkers()
    {
        return mapMarkers.AsReadOnly();
    }
    
    #endregion
    
    #region 地圖縮放功能
    
    /// <summary>
    /// 處理地圖縮放輸入
    /// </summary>
    private void HandleMapZoomInput()
    {
        if (mapContainer == null || !isVisible) return;
        
        // 儲存原始值（只在第一次需要時）
        if (!hasStoredOriginalValues)
        {
            StoreOriginalValues();
        }
        
        // 檢測按鍵狀態
        bool keyPressed = Input.GetKey(zoomKey);
        
        // 如果狀態改變，切換縮放
        if (keyPressed != isZoomed)
        {
            SetMapZoomed(keyPressed, useSmoothTransition);
        }
    }
    
    /// <summary>
    /// 儲存原始位置和大小
    /// </summary>
    private void StoreOriginalValues()
    {
        if (mapContainer == null) return;
        
        originalAnchoredPosition = mapContainer.anchoredPosition;
        originalSizeDelta = mapContainer.sizeDelta;
        originalLocalScale = mapContainer.localScale;
        
        // 計算放大後的位置（畫面中間）
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                // 使用 RectTransformUtility 來計算位置
                // 獲取 Canvas 中心在螢幕座標中的位置
                Vector2 canvasCenterScreen = RectTransformUtility.WorldToScreenPoint(
                    canvas.worldCamera ?? Camera.main,
                    canvasRect.position
                );
                
                // 將螢幕座標轉換為相對於 mapContainer 的 anchoredPosition
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    mapContainer,
                    canvasCenterScreen,
                    canvas.worldCamera ?? Camera.main,
                    out localPoint))
                {
                    // 計算從當前 anchor 位置到 Canvas 中心的偏移
                    // 需要考慮 anchor 的位置
                    Vector2 anchorPivot = (mapContainer.anchorMin + mapContainer.anchorMax) * 0.5f;
                    Rect canvasRectBounds = canvasRect.rect;
                    Vector2 anchorWorldPos = new Vector2(
                        (anchorPivot.x - 0.5f) * canvasRectBounds.width,
                        (anchorPivot.y - 0.5f) * canvasRectBounds.height
                    );
                    
                    // 計算偏移
                    zoomedAnchoredPosition = originalAnchoredPosition + (localPoint - anchorWorldPos);
                }
                else
                {
                    // 如果轉換失敗，使用簡化方法
                    // 計算從原始位置到中心的偏移
                    Rect canvasRectBounds = canvasRect.rect;
                    Vector2 anchorPivot = (mapContainer.anchorMin + mapContainer.anchorMax) * 0.5f;
                    Vector2 centerOffset = new Vector2(
                        (0.5f - anchorPivot.x) * canvasRectBounds.width,
                        (0.5f - anchorPivot.y) * canvasRectBounds.height
                    );
                    zoomedAnchoredPosition = originalAnchoredPosition + centerOffset;
                }
            }
            else
            {
                // 如果無法獲取 Canvas RectTransform，使用簡化方法
                zoomedAnchoredPosition = Vector2.zero;
            }
        }
        else
        {
            // 如果找不到 Canvas，使用簡化方法
            zoomedAnchoredPosition = Vector2.zero;
        }
        
        // 放大後的大小和縮放
        zoomedSizeDelta = originalSizeDelta * zoomScale;
        zoomedLocalScale = originalLocalScale * zoomScale;
        
        // 儲存玩家圖標的原始縮放，並計算放大後的縮放
        // 注意：playerIcon 是 mapContainer 的子物件，需要除以地圖縮放倍率來抵消父物件的縮放
        if (playerIcon != null)
        {
            originalPlayerIconScale = playerIcon.localScale;
            zoomedPlayerIconScale = originalPlayerIconScale * playerIconZoomScale / zoomScale;
        }
        else
        {
            originalPlayerIconScale = Vector3.one;
            zoomedPlayerIconScale = Vector3.one * playerIconZoomScale / zoomScale;
        }
        
        hasStoredOriginalValues = true;
    }
    
    /// <summary>
    /// 設定地圖縮放狀態
    /// </summary>
    private void SetMapZoomed(bool zoomed, bool smooth)
    {
        if (mapContainer == null || !hasStoredOriginalValues) return;
        
        isZoomed = zoomed;
        
        if (smooth && transitionDuration > 0f)
        {
            // 使用平滑過渡
            transitionTimer = transitionDuration;
        }
        else
        {
            // 立即切換
            ApplyMapZoomState(zoomed);
        }
    }
    
    /// <summary>
    /// 更新地圖過渡動畫
    /// </summary>
    private void UpdateMapTransition()
    {
        if (mapContainer == null || !hasStoredOriginalValues) return;
        
        transitionTimer -= Time.deltaTime;
        float t = 1f - (transitionTimer / transitionDuration);
        t = Mathf.Clamp01(t);
        
        // 使用平滑插值
        float smoothT = SmoothStep(t);
        
        // 插值位置、大小和縮放
        Vector2 currentPos = Vector2.Lerp(
            isZoomed ? originalAnchoredPosition : zoomedAnchoredPosition,
            isZoomed ? zoomedAnchoredPosition : originalAnchoredPosition,
            smoothT
        );
        
        Vector2 currentSize = Vector2.Lerp(
            isZoomed ? originalSizeDelta : zoomedSizeDelta,
            isZoomed ? zoomedSizeDelta : originalSizeDelta,
            smoothT
        );
        
        Vector3 currentScale = Vector3.Lerp(
            isZoomed ? originalLocalScale : zoomedLocalScale,
            isZoomed ? zoomedLocalScale : originalLocalScale,
            smoothT
        );
        
        mapContainer.anchoredPosition = currentPos;
        mapContainer.sizeDelta = currentSize;
        mapContainer.localScale = currentScale;
        
        // 同時更新玩家圖標的縮放
        if (playerIcon != null)
        {
            Vector3 currentPlayerIconScale = Vector3.Lerp(
                isZoomed ? originalPlayerIconScale : zoomedPlayerIconScale,
                isZoomed ? zoomedPlayerIconScale : originalPlayerIconScale,
                smoothT
            );
            playerIcon.localScale = currentPlayerIconScale;
        }
        
        // 如果過渡完成，確保最終值正確
        if (transitionTimer <= 0f)
        {
            ApplyMapZoomState(isZoomed);
            transitionTimer = 0f;
        }
    }
    
    /// <summary>
    /// 應用地圖縮放狀態（立即）
    /// </summary>
    private void ApplyMapZoomState(bool zoomed)
    {
        if (mapContainer == null || !hasStoredOriginalValues) return;
        
        if (zoomed)
        {
            mapContainer.anchoredPosition = zoomedAnchoredPosition;
            mapContainer.sizeDelta = zoomedSizeDelta;
            mapContainer.localScale = zoomedLocalScale;
            
            // 應用玩家圖標的縮放倍率
            if (playerIcon != null)
            {
                playerIcon.localScale = zoomedPlayerIconScale;
            }
        }
        else
        {
            mapContainer.anchoredPosition = originalAnchoredPosition;
            mapContainer.sizeDelta = originalSizeDelta;
            mapContainer.localScale = originalLocalScale;
            
            // 恢復玩家圖標原始大小
            if (playerIcon != null)
            {
                playerIcon.localScale = originalPlayerIconScale;
            }
        }
    }
    
    /// <summary>
    /// 平滑階梯函數（用於更自然的動畫）
    /// </summary>
    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
    
    /// <summary>
    /// 設定地圖縮放倍率
    /// </summary>
    public void SetZoomScale(float scale)
    {
        zoomScale = Mathf.Max(1f, scale);
        
        // 重新計算放大後的大小和縮放
        if (hasStoredOriginalValues)
        {
            zoomedSizeDelta = originalSizeDelta * zoomScale;
            zoomedLocalScale = originalLocalScale * zoomScale;
            
            // 重新計算玩家圖標的縮放（需要除以地圖縮放倍率以抵消父物件的縮放）
            if (playerIcon != null)
            {
                zoomedPlayerIconScale = originalPlayerIconScale * playerIconZoomScale / zoomScale;
            }
            else
            {
                zoomedPlayerIconScale = Vector3.one * playerIconZoomScale / zoomScale;
            }
            
            // 如果當前是放大狀態，立即應用新的大小和縮放
            if (isZoomed)
            {
                mapContainer.sizeDelta = zoomedSizeDelta;
                mapContainer.localScale = zoomedLocalScale;
                if (playerIcon != null)
                {
                    playerIcon.localScale = zoomedPlayerIconScale;
                }
            }
        }
    }
    
    /// <summary>
    /// 設定玩家圖標縮放倍率
    /// </summary>
    public void SetPlayerIconZoomScale(float scale)
    {
        playerIconZoomScale = Mathf.Max(0.1f, scale);
        
        // 重新計算玩家圖標的縮放（需要除以地圖縮放倍率以抵消父物件的縮放）
        if (hasStoredOriginalValues)
        {
            if (playerIcon != null)
            {
                zoomedPlayerIconScale = originalPlayerIconScale * playerIconZoomScale / zoomScale;
            }
            else
            {
                zoomedPlayerIconScale = Vector3.one * playerIconZoomScale / zoomScale;
            }
            
            // 如果當前是放大狀態，立即應用新的縮放
            if (isZoomed && playerIcon != null)
            {
                playerIcon.localScale = zoomedPlayerIconScale;
            }
        }
    }
    
    /// <summary>
    /// 設定過渡時間
    /// </summary>
    public void SetTransitionDuration(float duration)
    {
        transitionDuration = Mathf.Max(0f, duration);
    }
    
    /// <summary>
    /// 獲取當前是否處於放大狀態
    /// </summary>
    public bool IsZoomed() => isZoomed;
    
    #endregion
    
    #region 公開方法
    
    /// <summary>
    /// 設定更新間隔
    /// </summary>
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Max(0.01f, interval);
    }
    
    /// <summary>
    /// 設定 RenderTexture 大小
    /// </summary>
    public void SetRenderTextureSize(int size)
    {
        renderTextureSize = Mathf.Clamp(size, 256, 2048);
        
        // 重新創建 RenderTexture
        if (mapRenderTexture != null)
        {
            mapRenderTexture.Release();
            Destroy(mapRenderTexture);
        }
        
        SetupRenderTexture();
    }
    
    /// <summary>
    /// 獲取 Tilemap 引用
    /// </summary>
    public Tilemap GetTilemap() => tilemap;
    
    /// <summary>
    /// 設定 Tilemap
    /// </summary>
    public void SetTilemap(Tilemap newTilemap)
    {
        tilemap = newTilemap;
        if (tilemap != null)
        {
            RefreshTilemapBounds();
        }
    }
    
    #endregion
}

