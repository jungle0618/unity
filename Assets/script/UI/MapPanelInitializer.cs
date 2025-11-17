using UnityEngine;

/// <summary>
/// MapPanel 初始化器
/// 自動初始化 MapPanel 上的所有地圖管理器
/// 同時處理 TilemapMapUIManager 和 MapUIManager
/// </summary>
public class MapPanelInitializer : MonoBehaviour
{
    [Header("Auto Initialize")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool setVisibleOnStart = false; // 如果地圖預設顯示則設為 true
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private TilemapMapUIManager tilemapManager;
    private MapUIManager mapManager;
    
    private void Awake()
    {
        // 獲取兩個管理器組件
        tilemapManager = GetComponent<TilemapMapUIManager>();
        mapManager = GetComponent<MapUIManager>();
        
        if (tilemapManager == null && mapManager == null)
        {
            Debug.LogWarning($"[MapPanelInitializer] {gameObject.name} 沒有找到任何地圖管理器組件！");
        }
    }
    
    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeManagers();
            
            if (setVisibleOnStart)
            {
                ShowMap();
            }
        }
    }
    
    /// <summary>
    /// 初始化所有地圖管理器
    /// </summary>
    public void InitializeManagers()
    {
        if (tilemapManager != null)
        {
            tilemapManager.Initialize();
            if (showDebugLogs)
            {
                Debug.Log($"[MapPanelInitializer] TilemapMapUIManager 已初始化");
            }
        }
        
        if (mapManager != null)
        {
            mapManager.Initialize();
            if (showDebugLogs)
            {
                Debug.Log($"[MapPanelInitializer] MapUIManager 已初始化");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[MapPanelInitializer] MapPanel 初始化完成");
        }
    }
    
    /// <summary>
    /// 顯示地圖（調用兩個管理器的 SetVisible）
    /// </summary>
    public void ShowMap()
    {
        if (tilemapManager != null)
        {
            tilemapManager.SetVisible(true);
        }
        
        if (mapManager != null)
        {
            mapManager.SetVisible(true);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[MapPanelInitializer] 地圖已顯示");
        }
    }
    
    /// <summary>
    /// 隱藏地圖
    /// </summary>
    public void HideMap()
    {
        if (tilemapManager != null)
        {
            tilemapManager.SetVisible(false);
        }
        
        if (mapManager != null)
        {
            mapManager.SetVisible(false);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[MapPanelInitializer] 地圖已隱藏");
        }
    }
    
    /// <summary>
    /// 切換地圖顯示/隱藏
    /// </summary>
    public void ToggleMap()
    {
        // 使用 TilemapMapUIManager 的可見性作為參考
        // 因為它通常控制整個地圖面板
        if (tilemapManager != null && gameObject.activeSelf)
        {
            HideMap();
        }
        else
        {
            ShowMap();
        }
    }
}

