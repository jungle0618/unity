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
        // 基本的座標轉換：世界XZ座標 -> 地圖XY座標
        float mapX = (worldPosition.x + worldOffset.x) * mapScale;
        float mapY = (worldPosition.z + worldOffset.y) * mapScale;
        
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
}

/// <summary>
/// 地圖標記類別
/// 代表地圖上的一個標記點
/// </summary>
public class MapMarker : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image markerImage;
    [SerializeField] private TMPro.TextMeshProUGUI markerNameText;
    
    private Vector3 worldPosition;
    private string markerName;
    
    public Vector3 WorldPosition => worldPosition;
    public string MarkerName => markerName;
    
    public void SetWorldPosition(Vector3 position)
    {
        worldPosition = position;
    }
    
    public void SetMarkerName(string name)
    {
        markerName = name;
        if (markerNameText != null)
        {
            markerNameText.text = name;
        }
    }
    
    public void SetMarkerColor(Color color)
    {
        if (markerImage != null)
        {
            markerImage.color = color;
        }
    }
}

