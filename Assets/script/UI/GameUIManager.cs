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
    [SerializeField] private TilemapMapUIManager tilemapMapUIManager;  // 地圖UI
    [SerializeField] private WeaponSwitchUI weaponSwitchUI;          // 武器切換UI
    [SerializeField] private NonWeaponItemsUI nonWeaponItemsUI;      // 非武器物品UI
    
    [Header("Game Process UI Managers - 遊戲過程中的功能 UI")]
    [SerializeField] private PauseUIManager pauseUIManager;             // 暫停選單（屬於遊戲過程中的 UI）
    
    [Header("Notification & Dialogue UI Managers - 通知與對話 UI")]
    [SerializeField] private NotificationUIManager notificationUIManager; // 臨時通知UI
    [SerializeField] private DialogueUIManager dialogueUIManager;       // 對話UI
    
    [SerializeField] private GameOverUIManager gameOverUIManager;       // 結算頁面（遊戲結束時顯示）
    [SerializeField] private GameWinUIManager gameWinUIManager;         // 任務成功頁面（遊戲勝利時顯示）
    
    [Header("Optional UI Managers")]
    [SerializeField] private LoadingProgressUIManager loadingProgressUIManager;  // 載入進度（通常在 LoadingScene，可選）
    
    [Header("Initial Visibility Settings")]
    [SerializeField] private bool showHealthUI = true;
    [SerializeField] private bool showDangerUI = true;
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
        // 訂閱遊戲狀態變化事件（用於在遊戲結束時隱藏 UI）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        
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
    /// 處理遊戲狀態變化事件
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        // 當遊戲結束或勝利時，隱藏除了 Dialogue 以外的所有 UI
        if (newState == GameManager.GameState.GameOver || newState == GameManager.GameState.GameWin)
        {
            HideAllUIExceptDialogue();
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
        
        // 取消訂閱遊戲狀態變化事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
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
        
        // 初始化新的 UI
        if (weaponSwitchUI != null)
        {
            weaponSwitchUI.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: WeaponSwitchUI 未設定");
        }

        if (nonWeaponItemsUI != null)
        {
            nonWeaponItemsUI.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: NonWeaponItemsUI 未設定");
        }
        
        // 初始化通知與對話UI
        if (notificationUIManager != null)
        {
            notificationUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: NotificationUIManager 未設定");
        }
        
        if (dialogueUIManager != null)
        {
            dialogueUIManager.Initialize();
        }
        else
        {
            Debug.LogWarning("GameUIManager: DialogueUIManager 未設定");
        }
        
        // GameOver/Win UIManager
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
    /// 獲取地圖UI管理器
    /// </summary>
    public TilemapMapUIManager GetTilemapMapUIManager() => tilemapMapUIManager;
    
    /// <summary>
    /// 獲取暫停選單UI管理器
    /// </summary>
    public PauseUIManager GetPauseUIManager() => pauseUIManager;
    
    /// <summary>
    /// 獲取載入進度UI管理器（可選，通常在 LoadingScene）
    /// </summary>
    public LoadingProgressUIManager GetLoadingProgressUIManager() => loadingProgressUIManager;
    
    /// <summary>
    /// 獲取通知UI管理器
    /// </summary>
    public NotificationUIManager GetNotificationUIManager() => notificationUIManager;
    
    /// <summary>
    /// 獲取對話UI管理器
    /// </summary>
    public DialogueUIManager GetDialogueUIManager() => dialogueUIManager;
    
    /// <summary>
    /// 獲取結算頁面UI管理器
    /// </summary>
    public GameOverUIManager GetGameOverUIManager() => gameOverUIManager;
    
    /// <summary>
    /// 獲取任務成功頁面UI管理器
    /// </summary>
    public GameWinUIManager GetGameWinUIManager() => gameWinUIManager;
    
    #endregion
    
    #region Game End UI Control
    
    /// <summary>
    /// 隱藏所有遊戲進行中的 UI（用於遊戲結束時）
    /// </summary>
    public void HideAllGameplayUI()
    {
        if (healthUIManager != null)
            healthUIManager.gameObject.SetActive(false);
        
        if (dangerUIManager != null)
            dangerUIManager.gameObject.SetActive(false);
        
        if (tilemapMapUIManager != null)
            tilemapMapUIManager.gameObject.SetActive(false);
        
        if (weaponSwitchUI != null)
            weaponSwitchUI.gameObject.SetActive(false);
        
        if (nonWeaponItemsUI != null)
            nonWeaponItemsUI.gameObject.SetActive(false);

        if (gameOverUIManager != null)
            gameOverUIManager.gameObject.SetActive(false);
        
        if (gameWinUIManager != null)
            gameWinUIManager.gameObject.SetActive(false);
        
        Debug.Log("[GameUIManager] All gameplay UI hidden");
    }
    
    /// <summary>
    /// 隱藏除了 Dialogue 以外的所有 UI（用於遊戲結束時，在對話顯示前）
    /// </summary>
    public void HideAllUIExceptDialogue()
    {
        // 隱藏遊戲進行中的 UI
        if (healthUIManager != null)
            healthUIManager.gameObject.SetActive(false);
        
        if (dangerUIManager != null)
            dangerUIManager.gameObject.SetActive(false);
        
        if (tilemapMapUIManager != null)
            tilemapMapUIManager.gameObject.SetActive(false);
        
        if (weaponSwitchUI != null)
            weaponSwitchUI.gameObject.SetActive(false);
        
        if (nonWeaponItemsUI != null)
            nonWeaponItemsUI.gameObject.SetActive(false);
        
        // 隱藏暫停選單
        if (pauseUIManager != null)
            pauseUIManager.gameObject.SetActive(false);
        
        // 隱藏通知 UI
        if (notificationUIManager != null)
            notificationUIManager.gameObject.SetActive(false);
        
        // 隱藏 GameOver 和 GameWin UI（對話完成後會再顯示）
        if (gameOverUIManager != null)
            gameOverUIManager.gameObject.SetActive(false);
        
        if (gameWinUIManager != null)
            gameWinUIManager.gameObject.SetActive(false);
        
        // 注意：不隱藏 DialogueUIManager（保留，因為可能正在顯示對話）
        
        Debug.Log("[GameUIManager] All UI hidden except Dialogue");
    }
    
    /// <summary>
    /// 顯示 GameOver UI（用於對話完成後）
    /// </summary>
    public void ShowGameOverUI()
    {
        if (gameOverUIManager != null)
        {
            gameOverUIManager.gameObject.SetActive(true);
            gameOverUIManager.SetVisible(true);
        }
    }
    
    /// <summary>
    /// 顯示 GameWin UI（用於對話完成後）
    /// </summary>
    public void ShowGameWinUI()
    {
        if (gameWinUIManager != null)
        {
            gameWinUIManager.gameObject.SetActive(true);
            gameWinUIManager.SetVisible(true);
        }
    }
    
    /// <summary>
    /// 顯示所有遊戲進行中的 UI（用於恢復遊戲時）
    /// </summary>
    public void ShowAllGameplayUI()
    {
        if (healthUIManager != null)
            healthUIManager.gameObject.SetActive(showHealthUI);
        
        if (dangerUIManager != null)
            dangerUIManager.gameObject.SetActive(showDangerUI);
        
        if (tilemapMapUIManager != null)
            tilemapMapUIManager.gameObject.SetActive(showMapUI);
        
        if (weaponSwitchUI != null)
            weaponSwitchUI.gameObject.SetActive(true);
        
        if (nonWeaponItemsUI != null)
            nonWeaponItemsUI.gameObject.SetActive(true);
        
        if (gameOverUIManager != null)
            gameOverUIManager.gameObject.SetActive(true);
        
        if (gameWinUIManager != null)
            gameWinUIManager.gameObject.SetActive(true);
        Debug.Log("[GameUIManager] All gameplay UI shown");
    }
    
    #endregion
}
