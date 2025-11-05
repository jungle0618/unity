using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Tilemap 地圖UI管理器
/// 使用 Camera + RenderTexture 來顯示動態變化的 Tilemap
/// </summary>
public class TilemapMapUIManager : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Tilemap tilemap;                   // 要顯示的 Tilemap
    [SerializeField] private Grid grid;                         // Tilemap 的 Grid
    [SerializeField] private bool autoFindTilemap = true;       // 自動尋找 Tilemap
    
    [Header("Map Display")]
    [SerializeField] private RawImage mapDisplay;               // 顯示地圖的 RawImage
    [SerializeField] private RectTransform mapContainer;        // 地圖容器
    
    [Header("Map Camera")]
    [SerializeField] private Camera mapCamera;                  // 渲染地圖的相機
    [SerializeField] private bool autoCreateCamera = true;      // 自動創建相機
    [SerializeField] private int renderTextureSize = 512;       // RenderTexture 大小
    [SerializeField] private float cameraHeight = 10f;          // 相機高度
    [SerializeField] private LayerMask mapLayerMask = -1;       // 地圖要渲染的圖層（可多選）
    [SerializeField] private bool autoDetectTilemapLayer = true; // 自動偵測 Tilemap 的 Layer
    
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
    
    private Player player;
    private EntityManager entityManager;
    private bool isInitialized = false;
    private RenderTexture mapRenderTexture;
    private List<MapMarker> mapMarkers = new List<MapMarker>();
    private bool isVisible = false;
    private float lastUpdateTime = 0f;
    
    // Tilemap 邊界
    private Bounds tilemapBounds;
    private Vector2 mapSize;
    
    private void Awake()
    {
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
            if (tilemap != null)
            {
                grid = tilemap.GetComponentInParent<Grid>();
            }
        }
        
        if (tilemap == null)
        {
            Debug.LogError("TilemapMapUIManager: 找不到 Tilemap！");
            return;
        }
        
        // 獲取 Tilemap 邊界
        UpdateTilemapBounds();
        
        // 創建或設定相機
        SetupMapCamera();
        
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
        // 只有在地圖可見且需要即時更新時才更新
        if (isVisible && updateInRealtime && player != null)
        {
            // 使用更新間隔來降低性能消耗
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdatePlayerPosition();
                lastUpdateTime = Time.time;
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
        
        // 如果是自動創建的相機，銷毀它
        if (autoCreateCamera && mapCamera != null)
        {
            Destroy(mapCamera.gameObject);
        }
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
        
        // 啟用或禁用地圖相機
        if (mapCamera != null)
        {
            mapCamera.enabled = visible;
        }
        
        // 當地圖顯示時，立即更新
        if (visible && player != null)
        {
            UpdatePlayerPosition();
            lastUpdateTime = Time.time;
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
    /// 更新 Tilemap 邊界
    /// </summary>
    private void UpdateTilemapBounds()
    {
        if (tilemap == null) return;
        
        // 獲取 Tilemap 的實際使用邊界
        tilemap.CompressBounds();
        tilemapBounds = tilemap.localBounds;
        
        // Tilemap 是 2D 的，使用 X 和 Y
        mapSize = new Vector2(tilemapBounds.size.x, tilemapBounds.size.y);
        
        Debug.Log($"Tilemap 邊界更新: Center={tilemapBounds.center}, Size={tilemapBounds.size}");
    }
    
    /// <summary>
    /// 重新計算 Tilemap 邊界（當 Tilemap 改變時調用）
    /// </summary>
    public void RefreshTilemapBounds()
    {
        UpdateTilemapBounds();
        
        // 重新設定相機位置和大小
        if (mapCamera != null)
        {
            UpdateCameraPosition();
        }
    }
    
    #endregion
    
    #region 相機設定
    
    /// <summary>
    /// 設定地圖相機
    /// </summary>
    private void SetupMapCamera()
    {
        if (mapCamera == null && autoCreateCamera)
        {
            // 自動創建相機
            GameObject cameraObj = new GameObject("MapCamera");
            mapCamera = cameraObj.AddComponent<Camera>();
            
            // 設定相機屬性
            mapCamera.orthographic = true;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = Color.black;
            mapCamera.enabled = false; // 預設禁用
            
            Debug.Log("已自動創建地圖相機");
        }
        
        if (mapCamera != null)
        {
            // 自動偵測或使用手動設定的 LayerMask
            if (tilemap != null && autoDetectTilemapLayer)
            {
                // 使用 Tilemap 的 Layer
                int tilemapLayer = tilemap.gameObject.layer;
                mapCamera.cullingMask = 1 << tilemapLayer;
                Debug.Log($"地圖相機自動設定為 Tilemap 的 Layer: {LayerMask.LayerToName(tilemapLayer)} (Layer {tilemapLayer})");
            }
            else if (mapLayerMask.value != 0 && mapLayerMask.value != -1)
            {
                // 使用手動設定的 LayerMask（如果有明確設定）
                mapCamera.cullingMask = mapLayerMask;
                Debug.Log($"地圖相機使用手動設定的 LayerMask: {mapLayerMask.value}");
            }
            else if (tilemap != null)
            {
                // 如果沒有手動設定，退回自動偵測
                int tilemapLayer = tilemap.gameObject.layer;
                mapCamera.cullingMask = 1 << tilemapLayer;
                Debug.LogWarning($"未設定 LayerMask，自動使用 Tilemap Layer: {LayerMask.LayerToName(tilemapLayer)} (Layer {tilemapLayer})");
            }
            
            UpdateCameraPosition();
        }
    }
    
    /// <summary>
    /// 更新相機位置和大小
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (mapCamera == null || tilemap == null) return;
        
        // 設定相機位置（正面看向 Tilemap - 2D 遊戲）
        // 相機在 Z 軸負方向，看向 XY 平面
        Vector3 center = tilemapBounds.center;
        mapCamera.transform.position = new Vector3(center.x, center.y, -cameraHeight);
        mapCamera.transform.rotation = Quaternion.identity; // 直接面向前方（看向 Z+）
        
        // 設定正交大小（讓整個 Tilemap 都可見）
        float maxSize = Mathf.Max(tilemapBounds.size.x, tilemapBounds.size.y); // 使用 X 和 Y
        mapCamera.orthographicSize = maxSize / 2f;
        
        Debug.Log($"相機設定: 位置={mapCamera.transform.position}, 大小={mapCamera.orthographicSize}");
    }
    
    #endregion
    
    #region RenderTexture 設定
    
    /// <summary>
    /// 設定 RenderTexture
    /// </summary>
    private void SetupRenderTexture()
    {
        if (mapCamera == null || mapDisplay == null) return;
        
        // 創建 RenderTexture
        mapRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
        mapRenderTexture.Create();
        
        // 設定相機輸出
        mapCamera.targetTexture = mapRenderTexture;
        
        // 設定 RawImage 顯示
        mapDisplay.texture = mapRenderTexture;
        
        Debug.Log($"RenderTexture 已創建: {renderTextureSize}x{renderTextureSize}");
    }
    
    #endregion
    
    #region 玩家位置
    
    /// <summary>
    /// 更新玩家在地圖上的位置
    /// </summary>
    private void UpdatePlayerPosition()
    {
        if (playerIcon == null || player == null || mapDisplay == null)
            return;
        
        // 將世界座標轉換為地圖UI座標
        Vector3 worldPos = player.transform.position;
        Vector2 mapPos = WorldToMapUIPosition(worldPos);
        
        // 更新玩家圖標位置
        playerIcon.anchoredPosition = mapPos;
        
        // 更新玩家圖標旋轉（如果需要）
        if (rotateWithPlayer)
        {
            float angle = -player.transform.eulerAngles.y;
            playerIcon.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    /// <summary>
    /// 將世界座標轉換為地圖UI座標
    /// </summary>
    private Vector2 WorldToMapUIPosition(Vector3 worldPosition)
    {
        if (tilemap == null || mapDisplay == null) return Vector2.zero;
        
        // 計算相對於 Tilemap 中心的位置
        Vector3 relativePos = worldPosition - tilemapBounds.center;
        
        // Tilemap 是 2D 的，使用 X 和 Y 軸（不是 Z 軸）
        // 檢查是否有有效的尺寸，避免除以零
        float sizeX = tilemapBounds.size.x != 0 ? tilemapBounds.size.x : 1f;
        float sizeY = tilemapBounds.size.y != 0 ? tilemapBounds.size.y : 1f;
        
        // 轉換為 0-1 的比例
        float normalizedX = (relativePos.x / sizeX) + 0.5f;
        float normalizedY = (relativePos.y / sizeY) + 0.5f; // 使用 Y 軸，不是 Z 軸
        
        // 轉換為地圖UI座標
        Rect rect = mapDisplay.rectTransform.rect;
        float uiX = (normalizedX - 0.5f) * rect.width;
        float uiY = (normalizedY - 0.5f) * rect.height;
        
        return new Vector2(uiX, uiY);
    }
    
    /// <summary>
    /// 將地圖UI座標轉換為世界座標
    /// </summary>
    private Vector3 MapUIToWorldPosition(Vector2 mapUIPosition)
    {
        if (tilemap == null || mapDisplay == null) return Vector3.zero;
        
        // 轉換為 0-1 的比例
        Rect rect = mapDisplay.rectTransform.rect;
        float normalizedX = (mapUIPosition.x / rect.width) + 0.5f;
        float normalizedY = (mapUIPosition.y / rect.height) + 0.5f;
        
        // 轉換為世界座標（Tilemap 是 2D，使用 X 和 Y 軸）
        float worldX = tilemapBounds.center.x + (normalizedX - 0.5f) * tilemapBounds.size.x;
        float worldY = tilemapBounds.center.y + (normalizedY - 0.5f) * tilemapBounds.size.y;
        
        return new Vector3(worldX, worldY, 0); // Z 軸設為 0（2D 遊戲）
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
            grid = tilemap.GetComponentInParent<Grid>();
            RefreshTilemapBounds();
            
            // 如果啟用自動偵測，更新相機 LayerMask
            if (autoDetectTilemapLayer && mapCamera != null)
            {
                int tilemapLayer = tilemap.gameObject.layer;
                mapCamera.cullingMask = 1 << tilemapLayer;
                Debug.Log($"Tilemap 變更，相機 Layer 已更新為: {LayerMask.LayerToName(tilemapLayer)}");
            }
        }
    }
    
    /// <summary>
    /// 設定地圖相機的 LayerMask（手動模式）
    /// </summary>
    public void SetMapLayerMask(LayerMask layerMask)
    {
        mapLayerMask = layerMask;
        autoDetectTilemapLayer = false; // 停用自動偵測
        
        if (mapCamera != null)
        {
            mapCamera.cullingMask = mapLayerMask;
            Debug.Log($"地圖相機 LayerMask 已手動設定為: {mapLayerMask.value}");
        }
    }
    
    /// <summary>
    /// 設定地圖相機的單一 Layer
    /// </summary>
    public void SetMapLayer(int layer)
    {
        mapLayerMask = 1 << layer;
        autoDetectTilemapLayer = false; // 停用自動偵測
        
        if (mapCamera != null)
        {
            mapCamera.cullingMask = mapLayerMask;
            Debug.Log($"地圖相機 Layer 已手動設定為: {LayerMask.LayerToName(layer)} (Layer {layer})");
        }
    }
    
    /// <summary>
    /// 設定是否自動偵測 Tilemap Layer
    /// </summary>
    public void SetAutoDetectTilemapLayer(bool autoDetect)
    {
        autoDetectTilemapLayer = autoDetect;
        
        if (autoDetect && tilemap != null && mapCamera != null)
        {
            int tilemapLayer = tilemap.gameObject.layer;
            mapCamera.cullingMask = 1 << tilemapLayer;
            Debug.Log($"已啟用自動偵測，相機 Layer 設定為: {LayerMask.LayerToName(tilemapLayer)}");
        }
    }
    
    /// <summary>
    /// 獲取當前使用的 LayerMask
    /// </summary>
    public LayerMask GetCurrentLayerMask()
    {
        if (mapCamera != null)
        {
            return mapCamera.cullingMask;
        }
        return mapLayerMask;
    }
    
    #endregion
}

