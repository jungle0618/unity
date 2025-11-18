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
    
    [Header("Map Zoom Settings")]
    [SerializeField] private KeyCode zoomKey = KeyCode.M; // 放大鍵
    [SerializeField] private float zoomScale = 7.5f; // 地圖放大倍率
    [SerializeField] private float playerIconZoomScale = 1.5f; // 玩家圖標放大倍率（相對於原始大小）
    [SerializeField] private float transitionDuration = 0.2f; // 過渡動畫時間
    [SerializeField] private bool useSmoothTransition = true; // 是否使用平滑過渡
    
    private Player player;
    private EntityManager entityManager;
    private bool isInitialized = false;
    private RenderTexture mapRenderTexture;
    private List<MapMarker> mapMarkers = new List<MapMarker>();
    private bool isVisible = false;
    private float lastUpdateTime = 0f;
    
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
        updateInterval = 0.1f;
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
        // 處理地圖縮放輸入
        HandleMapZoomInput();
        
        // 處理過渡動畫
        if (useSmoothTransition && transitionTimer > 0f)
        {
            UpdateMapTransition();
        }
        
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
    /// 將世界座標轉換為地圖UI座標（公開方法供其他組件使用）
    /// </summary>
    public Vector2 WorldToMapUIPosition(Vector3 worldPosition)
    {
        if (tilemap == null || mapDisplay == null || mapCamera == null) return Vector2.zero;
        
        // 使用相機的視角範圍來計算座標轉換
        // 相機的 orthographicSize 是垂直方向的一半大小
        float orthoSize = mapCamera.orthographicSize;
        
        // 計算相機視角的寬高比（RenderTexture 是正方形，所以寬高比為 1）
        float aspectRatio = 1f; // RenderTexture 是正方形
        
        // 相機視角的實際寬度和高度
        float cameraWidth = orthoSize * 2f * aspectRatio;
        float cameraHeight = orthoSize * 2f;
        
        // 計算相對於相機中心的位置（相機在 tilemapBounds.center 上方）
        Vector3 relativePos = worldPosition - tilemapBounds.center;
        
        // 轉換為相機視角空間的座標（-0.5 到 0.5）
        float normalizedX = relativePos.x / cameraWidth;
        float normalizedY = relativePos.y / cameraHeight;
        
        // 轉換為地圖UI座標（0 到 1，然後轉換為 UI 座標）
        Rect rect = mapDisplay.rectTransform.rect;
        float uiX = normalizedX * rect.width;
        float uiY = normalizedY * rect.height;
        
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

