using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 地圖UI管理器
/// 負責管理遊戲地圖的顯示，包括玩家位置、地圖標記等
/// </summary>
public class MapUIManager : MonoBehaviour
{
    [Header("Map Container")]
    [SerializeField] private RectTransform mapContainer;        // 地圖容器
    [SerializeField] private RectTransform mapImageRect;        // 地圖圖片的 RectTransform
    
    [Header("Player Icon")]
    [SerializeField] private RectTransform playerIcon;          // 玩家圖標
    [SerializeField] private bool rotateWithPlayer = true;      // 玩家圖標是否跟隨玩家旋轉
    
    [Header("Map Settings")]
    [SerializeField] private float mapScale = 1f;               // 地圖縮放比例（世界座標到地圖座標）
    [SerializeField] private Vector2 worldOffset = Vector2.zero; // 世界座標偏移
    [SerializeField] private bool updateInRealtime = true;      // 是否即時更新
    
    [Header("Markers")]
    [SerializeField] private GameObject mapMarkerPrefab;        // 地圖標記預製體
    [SerializeField] private Transform markersContainer;        // 標記容器
    
    [Header("References")]
    [SerializeField] private bool autoFindPlayer = true;
    
    private Player player;
    private List<MapMarker> mapMarkers = new List<MapMarker>();
    private bool isVisible = false;
    
    // 逃亡點標記
    private MapMarker escapePointMarker;
    
    // 逃亡路徑線（地圖UI上）
    private GameObject escapePathLineContainer; // 容器存放所有路徑線段
    private List<GameObject> escapePathLineSegments = new List<GameObject>(); // 所有線段
    
    // 目標標記（Target markers）
    private Dictionary<Target, MapMarker> targetMarkers = new Dictionary<Target, MapMarker>();
    
    /// <summary>
    /// 初始化地圖UI
    /// </summary>
    public void Initialize()
    {
        // 尋找玩家
        if (autoFindPlayer)
        {
            player = FindFirstObjectByType<Player>();
        }
        
        if (player == null)
        {
            Debug.LogWarning("MapUIManager: 找不到 Player");
        }
        
        if (playerIcon == null)
        {
            Debug.LogWarning("MapUIManager: 玩家圖標未設定");
        }
        
        // 初始化地圖
        InitializeMap();
        
        Debug.Log("MapUIManager: 地圖UI已初始化");
    }
    
    private void Update()
    {
        // 只有在地圖可見且需要即時更新時才更新
        if (isVisible && updateInRealtime && player != null)
        {
            UpdatePlayerPosition();
        }
        
        // 更新目標標記位置 - 始終更新（不論地圖是否可見）
        UpdateTargetMarkers();
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
        
        // 當地圖顯示時，立即更新玩家位置
        if (visible && player != null)
        {
            UpdatePlayerPosition();
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
    
    #region 地圖內部邏輯
    
    /// <summary>
    /// 初始化地圖
    /// </summary>
    private void InitializeMap()
    {
        if (mapContainer == null)
        {
            Debug.LogWarning("MapUIManager: 地圖容器未設定");
            return;
        }
        
        // 清空現有標記
        ClearAllMarkers();
        
        // 這裡可以添加地圖初始化邏輯
        // 例如：載入地圖標記、設定地圖大小等
    }
    
    /// <summary>
    /// 更新玩家在地圖上的位置
    /// </summary>
    private void UpdatePlayerPosition()
    {
        if (playerIcon == null || player == null)
            return;
        
        // 將世界座標轉換為地圖座標
        Vector3 worldPos = player.transform.position;
        Vector2 mapPos = WorldToMapPosition(worldPos);
        
        // 更新玩家圖標位置
        playerIcon.anchoredPosition = mapPos;
        
        // 更新玩家圖標旋轉（如果需要）
        if (rotateWithPlayer)
        {
            // 注意：這裡假設地圖是俯視圖，使用Y軸旋轉
            float angle = -player.transform.eulerAngles.y;
            playerIcon.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    /// <summary>
    /// 將世界座標轉換為地圖座標
    /// </summary>
    private Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        // 優先使用 TilemapMapUIManager 的精確轉換方法
        TilemapMapUIManager tilemapMapUI = GetComponent<TilemapMapUIManager>();
        
        if (tilemapMapUI != null)
        {
            // 使用 TilemapMapUI 的公開轉換方法，保證完全對齊
            return tilemapMapUI.WorldToMapUIPosition(worldPosition);
        }
        
        // 降級方案：使用簡單的縮放和偏移
        Debug.LogWarning("[MapUI] 找不到 TilemapMapUIManager，使用簡單轉換");
        float mapX = (worldPosition.x + worldOffset.x) * mapScale;
        float mapY = (worldPosition.y + worldOffset.y) * mapScale;
        
        return new Vector2(mapX, mapY);
    }
    
    /// <summary>
    /// 將地圖座標轉換為世界座標
    /// </summary>
    private Vector3 MapToWorldPosition(Vector2 mapPosition)
    {
        float worldX = (mapPosition.x / mapScale) - worldOffset.x;
        float worldZ = (mapPosition.y / mapScale) - worldOffset.y;
        
        return new Vector3(worldX, 0, worldZ);
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
            Debug.LogWarning("MapUIManager: 地圖標記預製體未設定");
            return null;
        }
        
        Transform container = markersContainer != null ? markersContainer : mapContainer;
        if (container == null)
        {
            Debug.LogWarning("MapUIManager: 找不到標記容器");
            return null;
        }
        
        // 創建標記
        Debug.Log("[MapUIManager] 添加地圖標記: " + markerName + " at " + worldPosition);
        GameObject markerObj = Instantiate(mapMarkerPrefab, container);
        MapMarker marker = markerObj.GetComponent<MapMarker>();
        
        if (marker != null)
        {
            // 設定標記位置
            RectTransform markerRect = markerObj.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                Vector2 mapPos = WorldToMapPosition(worldPosition);
                markerRect.anchoredPosition = mapPos;
            }
            
            marker.SetWorldPosition(worldPosition);
            marker.SetMarkerName(markerName);
            mapMarkers.Add(marker);
        }
        else
        {
            Debug.LogError("MapUIManager: 標記預製體缺少 MapMarker 組件！");
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
    
    #region 地圖控制
    
    /// <summary>
    /// 設定地圖縮放
    /// </summary>
    public void SetMapScale(float scale)
    {
        mapScale = Mathf.Max(0.1f, scale);
        
        // 更新玩家位置
        if (isVisible && player != null)
        {
            UpdatePlayerPosition();
        }
    }
    
    /// <summary>
    /// 設定世界座標偏移
    /// </summary>
    public void SetWorldOffset(Vector2 offset)
    {
        worldOffset = offset;
        
        // 更新玩家位置
        if (isVisible && player != null)
        {
            UpdatePlayerPosition();
        }
    }
    
    #endregion
    
    #region 目標標記管理
    
    /// <summary>
    /// 添加目標標記到地圖
    /// </summary>
    public void AddTargetMarker(Target target)
    {
        if (target == null) return;
        
        // 如果已經有標記，不重複添加
        if (targetMarkers.ContainsKey(target)) return;
        
        // 創建標記
        Vector3 targetPos = target.transform.position;
        MapMarker marker = AddMarker(targetPos, "Target");
        
        if (marker != null)
        {
            // 設定為紅色
            marker.SetMarkerColor(new Color(1f, 0f, 0f, 1f)); // Red
            
            // 標記縮小以適應地圖
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                markerRect.localScale = Vector3.one * 0.4f; // 縮小到0.4倍
            }
            
            targetMarkers[target] = marker;
            Debug.Log($"[MapUI] 目標標記已添加: {target.name}");
        }
    }
    
    /// <summary>
    /// 移除目標標記
    /// </summary>
    public void RemoveTargetMarker(Target target)
    {
        if (target == null) return;
        
        if (targetMarkers.TryGetValue(target, out MapMarker marker))
        {
            RemoveMarker(marker);
            targetMarkers.Remove(target);
            Debug.Log($"[MapUI] 目標標記已移除: {target.name}");
        }
    }
    
    /// <summary>
    /// 更新所有目標標記位置
    /// </summary>
    private void UpdateTargetMarkers()
    {
        foreach (var kvp in targetMarkers)
        {
            Target target = kvp.Key;
            MapMarker marker = kvp.Value;
            
            if (target == null)
            {
                continue;
            }
            
            // 更新標記位置（即使死亡也保持顯示）
            Vector2 mapPos = WorldToMapPosition(target.transform.position);
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                markerRect.anchoredPosition = mapPos;
            }
            
            // 如果死亡，改變標記顏色為灰色
            if (target.IsDead)
            {
                marker.SetMarkerColor(new Color(0.5f, 0.5f, 0.5f, 1f)); // Gray
            }
        }
    }
    
    #endregion
    
    #region 逃亡點標記
    
    /// <summary>
    /// 顯示逃亡點標記（亮綠色）並繪製路徑線
    /// </summary>
    /// <param name="escapePointWorldPos">逃亡點世界座標</param>
    /// <param name="targetWorldPos">目標當前世界座標</param>
    /// <param name="pathNodes">A* 路徑節點列表（可選）</param>
    public void ShowEscapePoint(Vector3 escapePointWorldPos, Vector3 targetWorldPos, List<PathfindingNode> pathNodes = null)
    {
        // 如果已經有逃亡點標記，先移除
        if (escapePointMarker != null)
        {
            RemoveMarker(escapePointMarker);
            escapePointMarker = null;
        }
        
        // 清除舊的路徑線
        ClearEscapePathLine();
        
        // 創建新的逃亡點標記
        escapePointMarker = AddMarker(escapePointWorldPos, "Escape Point");
        
        if (escapePointMarker != null)
        {
            // 設定為亮綠色
            escapePointMarker.SetMarkerColor(new Color(0f, 1f, 0f, 1f)); // Bright Green
            
            // 標記縮小以適應地圖
            RectTransform markerRect = escapePointMarker.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                markerRect.localScale = Vector3.one * 0.4f; // 縮小到0.4倍
            }
            
            Debug.LogWarning($"[MapUI] ✓ 逃亡點已顯示在地圖上: {escapePointWorldPos}");
        }
        
        // 繪製路徑線（使用 A* 路徑或直線）
        if (pathNodes != null && pathNodes.Count > 0)
        {
            DrawEscapePathWithWaypoints(targetWorldPos, pathNodes);
        }
        else
        {
            // 沒有路徑，繪製直線
            DrawEscapePathDirectLine(targetWorldPos, escapePointWorldPos);
        }
    }
    
    /// <summary>
    /// 在地圖上繪製逃亡路徑線（使用 A* 路徑節點）
    /// </summary>
    private void DrawEscapePathWithWaypoints(Vector3 startWorldPos, List<PathfindingNode> pathNodes)
    {
        Transform container = markersContainer != null ? markersContainer : mapContainer;
        if (container == null) return;
        
        // 創建容器存放所有線段
        escapePathLineContainer = new GameObject("EscapePathLines");
        escapePathLineContainer.transform.SetParent(container, false);
        
        RectTransform containerRect = escapePathLineContainer.AddComponent<RectTransform>();
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = Vector2.zero;
        
        // 建立完整路徑點列表（起點 + 所有路徑節點）
        List<Vector2> pathMapPositions = new List<Vector2>();
        pathMapPositions.Add(WorldToMapPosition(startWorldPos)); // 起點：目標當前位置
        
        foreach (var node in pathNodes)
        {
            pathMapPositions.Add(WorldToMapPosition(node.worldPosition));
        }
        
        // 繪製每段線
        for (int i = 0; i < pathMapPositions.Count - 1; i++)
        {
            DrawLineSegment(pathMapPositions[i], pathMapPositions[i + 1], container);
        }
        
        Debug.LogWarning($"[MapUI] ✓ 逃亡路徑已繪製: {pathMapPositions.Count} 個點，{escapePathLineSegments.Count} 條線段");
    }
    
    /// <summary>
    /// 繪製單條線段
    /// </summary>
    private bool DrawLineSegment(Vector2 startMapPos, Vector2 endMapPos, Transform parent)
    {
        // 計算線的方向和長度
        Vector2 direction = endMapPos - startMapPos;
        float distance = direction.magnitude;
        
        if (distance < 0.1f)
        {
            Debug.Log($"[MapUI] 跳過過短線段: {startMapPos} → {endMapPos} (長度: {distance:F2})");
            return false; // 太短的線段不繪製
        }
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 創建線段 GameObject
        GameObject lineSegment = new GameObject($"PathSegment_{escapePathLineSegments.Count}");
        lineSegment.transform.SetParent(escapePathLineContainer.transform, false);
        
        // 添加 RectTransform
        RectTransform lineRect = lineSegment.AddComponent<RectTransform>();
        
        // 添加 Image 組件作為線條
        UnityEngine.UI.Image lineImage = lineSegment.AddComponent<UnityEngine.UI.Image>();
        lineImage.color = new Color(0f, 1f, 0f, 0.8f); // 亮綠色，稍微透明
        
        // 設置線條的位置、大小和旋轉
        lineRect.anchoredPosition = startMapPos;
        lineRect.sizeDelta = new Vector2(distance, 3f); // 寬度3像素
        lineRect.pivot = new Vector2(0, 0.5f); // 從左側中心開始
        lineRect.rotation = Quaternion.Euler(0, 0, angle);
        
        // 添加到列表以便後續清理
        escapePathLineSegments.Add(lineSegment);
        
        Debug.Log($"[MapUI] 線段已創建: {startMapPos:F1} → {endMapPos:F1}, 長度: {distance:F1}, 角度: {angle:F1}°");
        
        return true;
    }
    
    /// <summary>
    /// 在地圖上繪製直線逃亡路徑（無 A* 路徑時使用）
    /// </summary>
    private void DrawEscapePathDirectLine(Vector3 startWorldPos, Vector3 endWorldPos)
    {
        Transform container = markersContainer != null ? markersContainer : mapContainer;
        if (container == null) return;
        
        // 創建容器
        escapePathLineContainer = new GameObject("EscapePathLine_Direct");
        escapePathLineContainer.transform.SetParent(container, false);
        
        // 轉換為地圖座標
        Vector2 startMapPos = WorldToMapPosition(startWorldPos);
        Vector2 endMapPos = WorldToMapPosition(endWorldPos);
        
        // 繪製單條線
        DrawLineSegment(startMapPos, endMapPos, container);
        
        Debug.LogWarning($"[MapUI] ✓ 逃亡直線路徑已繪製: {startWorldPos} → {endWorldPos}");
    }
    
    /// <summary>
    /// 清除逃亡路徑線
    /// </summary>
    private void ClearEscapePathLine()
    {
        // 清除所有線段
        foreach (var segment in escapePathLineSegments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }
        escapePathLineSegments.Clear();
        
        // 清除容器
        if (escapePathLineContainer != null)
        {
            Destroy(escapePathLineContainer);
            escapePathLineContainer = null;
        }
    }
    
    /// <summary>
    /// 隱藏逃亡點標記
    /// </summary>
    public void HideEscapePoint()
    {
        if (escapePointMarker != null)
        {
            RemoveMarker(escapePointMarker);
            escapePointMarker = null;
            Debug.Log("[MapUI] 逃亡點已從地圖移除");
        }
        
        ClearEscapePathLine();
    }
    
    /// <summary>
    /// 檢查逃亡點是否正在顯示
    /// </summary>
    public bool IsEscapePointVisible()
    {
        return escapePointMarker != null;
    }
    
    #endregion
}

