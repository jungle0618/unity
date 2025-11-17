using UnityEngine;

/// <summary>
/// 遊戲UI總協調器
/// 負責管理 GameScene 中的所有 UI 系統，包括：
/// - 遊戲進行中的 UI（血條、危險等級、物品欄、地圖）
/// - 遊戲過程中的功能 UI（暫停選單）
/// 
/// 注意：此管理器專用於 GameScene，不適用於其他場景（如 MainMenuScene、LoadingScene）
/// </summary>
[DefaultExecutionOrder(300)] // 在所有遊戲系統初始化完成後執行
public class GameUIManager : MonoBehaviour
{
    [Header("Game UI Managers - 遊戲進行中的 UI")]
    [SerializeField] private HealthUIManager healthUIManager;          // 血條UI
    [SerializeField] private DangerUIManager dangerUIManager;          // 危險等級UI
    [SerializeField] private HotbarUIManager hotbarUIManager;           // 物品欄UI
    [SerializeField] private TilemapMapUIManager tilemapMapUIManager;  // 地圖UI
    
    [Header("Game Process UI Managers - 遊戲過程中的功能 UI")]
    [SerializeField] private PauseUIManager pauseUIManager;             // 暫停選單（屬於遊戲過程中的 UI）
    [SerializeField] private GameOverUIManager gameOverUIManager;       // 結算頁面（遊戲結束時顯示）
    [SerializeField] private GameWinUIManager gameWinUIManager;         // 任務成功頁面（遊戲勝利時顯示）
    
    [Header("Optional UI Managers")]
    [SerializeField] private LoadingProgressUIManager loadingProgressUIManager;  // 載入進度（通常在 LoadingScene，可選）
    
    [Header("Initial Visibility Settings")]
    [SerializeField] private bool showHealthUI = true;
    [SerializeField] private bool showDangerUI = true;
    [SerializeField] private bool showHotbarUI = true;
    [SerializeField] private bool showMapUI = false;
    // 注意：暫停選單由 GameManager 自動控制，不需要手動設定
    
    [Header("Player Initialization")]
    [SerializeField] private bool waitForPlayer = true; // 是否等待 Player 生成後再初始化
    [SerializeField] private bool useEntityManager = true; // 是否使用 EntityManager 獲取 Player
    
    private EntityManager entityManager;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // 嘗試獲取 EntityManager 引用
        if (useEntityManager)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
        }
    }
    
    private void Start()
    {
        // 如果需要等待 Player，訂閱 EntityManager 事件
        if (waitForPlayer && useEntityManager && entityManager != null)
        {
            // 如果 Player 已經存在，立即初始化
            if (entityManager.Player != null)
            {
                InitializeAllUI();
            }
            else
            {
                // 訂閱事件，等待 Player 準備就緒
                entityManager.OnPlayerReady += HandlePlayerReady;
                Debug.Log("GameUIManager: 等待 Player 生成後再初始化 UI...");
            }
        }
        else
        {
            // 不需要等待或找不到 EntityManager，直接初始化
            InitializeAllUI();
        }
    }
    
    /// <summary>
    /// 處理 Player 準備就緒事件
    /// </summary>
    private void HandlePlayerReady()
    {
        if (isInitialized) return;
        
        InitializeAllUI();
        
        // 取消訂閱（只需要一次）
        if (entityManager != null)
        {
            entityManager.OnPlayerReady -= HandlePlayerReady;
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件訂閱
        if (entityManager != null)
        {
            entityManager.OnPlayerReady -= HandlePlayerReady;
        }
    }
    
    /// <summary>
    /// 初始化所有UI子系統
    /// </summary>
    private void InitializeAllUI()
    {
        if (isInitialized)
        {
            Debug.LogWarning("GameUIManager: UI 已經初始化過，跳過重複初始化");
            return;
        }
        
        Debug.Log("GameUIManager: 開始初始化所有 UI...");
        
        // 初始化各個UI管理器
        if (healthUIManager != null)
        {
            healthUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: HealthUIManager 未設定");
        }
        
        if (dangerUIManager != null)
        {
            dangerUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: DangerUIManager 未設定");
        }
        
        if (hotbarUIManager != null)
        {
            hotbarUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: HotbarUIManager 未設定");
        }
        
        if (tilemapMapUIManager != null)
        {
            tilemapMapUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: TilemapMapUIManager 未設定");
        }
        
        if (pauseUIManager != null)
        {
            pauseUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: PauseUIManager 未設定");
        }
        
        if (gameOverUIManager != null)
        {
            gameOverUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: GameOverUIManager 未設定");
        }
        
        if (gameWinUIManager != null)
        {
            gameWinUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: GameWinUIManager 未設定");
        }
        
        // 可選的載入進度UI（通常在 LoadingScene，不在 GameScene）
        if (loadingProgressUIManager != null)
        {
            loadingProgressUIManager.Initialize();
        }
        // 注意：不設定不會顯示警告，因為這是可選的
        
        // 設定初始可見性
        SetHealthUIVisible(showHealthUI);
        SetDangerUIVisible(showDangerUI);
        SetHotbarUIVisible(showHotbarUI);
        SetMapUIVisible(showMapUI);
        // 暫停選單由 GameManager 自動控制，不需要手動設定
        
        isInitialized = true;
        Debug.Log("GameUIManager: 所有UI已初始化完成");
    }
    
    #region UI 可見性控制
    
    /// <summary>
    /// 設定血條UI可見性
    /// </summary>
    public void SetHealthUIVisible(bool visible)
    {
        showHealthUI = visible;
        if (healthUIManager != null)
        {
            healthUIManager.SetVisible(visible);
        }
    }
    
    /// <summary>
    /// 設定危險等級UI可見性
    /// </summary>
    public void SetDangerUIVisible(bool visible)
    {
        showDangerUI = visible;
        if (dangerUIManager != null)
        {
            dangerUIManager.SetVisible(visible);
        }
    }
    
    /// <summary>
    /// 設定物品欄UI可見性
    /// </summary>
    public void SetHotbarUIVisible(bool visible)
    {
        showHotbarUI = visible;
        if (hotbarUIManager != null)
        {
            hotbarUIManager.SetVisible(visible);
        }
    }
    
    /// <summary>
    /// 設定地圖UI可見性
    /// </summary>
    public void SetMapUIVisible(bool visible)
    {
        showMapUI = visible;
        if (tilemapMapUIManager != null)
        {
            tilemapMapUIManager.SetVisible(visible);
        }
    }
    
    /// <summary>
    /// 切換血條UI顯示
    /// </summary>
    public void ToggleHealthUI()
    {
        SetHealthUIVisible(!showHealthUI);
    }
    
    /// <summary>
    /// 切換危險等級UI顯示
    /// </summary>
    public void ToggleDangerUI()
    {
        SetDangerUIVisible(!showDangerUI);
    }
    
    /// <summary>
    /// 切換物品欄UI顯示
    /// </summary>
    public void ToggleHotbarUI()
    {
        SetHotbarUIVisible(!showHotbarUI);
    }
    
    /// <summary>
    /// 切換地圖UI顯示
    /// </summary>
    public void ToggleMapUI()
    {
        SetMapUIVisible(!showMapUI);
        }
    
    #endregion
    
    #region 動態設定（可選）
    
    /// <summary>
    /// 設定血條UI管理器
    /// </summary>
    public void SetHealthUIManager(HealthUIManager manager)
    {
        healthUIManager = manager;
        if (manager != null)
        {
            manager.Initialize();
        }
    }
    
    /// <summary>
    /// 設定危險等級UI管理器
    /// </summary>
    public void SetDangerUIManager(DangerUIManager manager)
            {
        dangerUIManager = manager;
        if (manager != null)
            {
            manager.Initialize();
        }
    }
    
    /// <summary>
    /// 設定物品欄UI管理器
    /// </summary>
    public void SetHotbarUIManager(HotbarUIManager manager)
        {
        hotbarUIManager = manager;
        if (manager != null)
            {
            manager.Initialize();
        }
    }
    
    /// <summary>
    /// 設定地圖UI管理器
    /// </summary>
    public void SetTilemapMapUIManager(TilemapMapUIManager manager)
    {
        tilemapMapUIManager = manager;
        if (manager != null)
        {
            manager.Initialize();
        }
    }
    
    /// <summary>
    /// 設定暫停選單UI管理器
    /// </summary>
    public void SetPauseUIManager(PauseUIManager manager)
    {
        pauseUIManager = manager;
        if (manager != null)
        {
            manager.Initialize();
        }
    }
    
    /// <summary>
    /// 設定結算頁面UI管理器
    /// </summary>
    public void SetGameOverUIManager(GameOverUIManager manager)
    {
        gameOverUIManager = manager;
        if (manager != null)
        {
            manager.Initialize();
        }
    }
    
    /// <summary>
    /// 設定任務成功頁面UI管理器
    /// </summary>
    public void SetGameWinUIManager(GameWinUIManager manager)
    {
        gameWinUIManager = manager;
        if (manager != null)
        {
            manager.Initialize();
        }
    }
    
    /// <summary>
    /// 設定載入進度UI管理器（可選，通常在 LoadingScene）
    /// </summary>
    public void SetLoadingProgressUIManager(LoadingProgressUIManager manager)
    {
        loadingProgressUIManager = manager;
        if (manager != null)
        {
            manager.Initialize();
        }
    }
    
    #endregion
    
    #region 訪問器（可選）
    
    /// <summary>
    /// 獲取血條UI管理器
    /// </summary>
    public HealthUIManager GetHealthUIManager() => healthUIManager;
    
    /// <summary>
    /// 獲取危險等級UI管理器
    /// </summary>
    public DangerUIManager GetDangerUIManager() => dangerUIManager;
    
    /// <summary>
    /// 獲取物品欄UI管理器
    /// </summary>
    public HotbarUIManager GetHotbarUIManager() => hotbarUIManager;
    
    /// <summary>
    /// 獲取地圖UI管理器
    /// </summary>
    public TilemapMapUIManager GetTilemapMapUIManager() => tilemapMapUIManager;
    
    /// <summary>
    /// 獲取暫停選單UI管理器
    /// </summary>
    public PauseUIManager GetPauseUIManager() => pauseUIManager;
    
    /// <summary>
    /// 獲取結算頁面UI管理器
    /// </summary>
    public GameOverUIManager GetGameOverUIManager() => gameOverUIManager;
    
    /// <summary>
    /// 獲取任務成功頁面UI管理器
    /// </summary>
    public GameWinUIManager GetGameWinUIManager() => gameWinUIManager;
    
    /// <summary>
    /// 獲取載入進度UI管理器（可選，通常在 LoadingScene）
    /// </summary>
    public LoadingProgressUIManager GetLoadingProgressUIManager() => loadingProgressUIManager;
    
    #endregion
}
