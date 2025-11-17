using UnityEngine;

/// <summary>
/// 遊戲結束結算頁面UI管理器
/// 負責管理結算頁面的顯示和隱藏，與 GameManager 整合
/// </summary>
public class GameOverUIManager : MonoBehaviour
{
    [Header("Game Over UI Reference")]
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private bool autoFindGameOverUI = true;
    
    [Header("Settings")]
    [SerializeField] private bool autoSubscribeToGameManager = true; // 自動訂閱 GameManager 事件
    
    private bool isVisible = false;
    
    /// <summary>
    /// 初始化結算頁面UI
    /// </summary>
    public void Initialize()
    {
        // 尋找 GameOverUI
        if (autoFindGameOverUI && gameOverUI == null)
        {
            gameOverUI = FindFirstObjectByType<GameOverUI>();
        }
        
        if (gameOverUI == null)
        {
            Debug.LogWarning("GameOverUIManager: GameOverUI 未設定或找不到");
            return;
        }
        
        // 訂閱 GameManager 事件（如果需要）
        if (autoSubscribeToGameManager && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        
        // 初始隱藏
        SetVisible(false);
        
        Debug.Log("GameOverUIManager: 結算頁面UI已初始化");
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
    
    private void OnApplicationQuit()
    {
        // 確保在應用程式退出時取消訂閱
        OnDestroy();
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        // 檢查物件是否已被銷毀（Unity 的 == 重載會處理已銷毀的物件）
        if (this == null || gameObject == null)
        {
            return;
        }
        
        isVisible = visible;
        gameObject.SetActive(visible);
        
        if (gameOverUI != null)
        {
            gameOverUI.SetGameOverPanelActive(visible);
        }
    }
    
    /// <summary>
    /// 處理遊戲狀態變化事件
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        // 檢查物件是否已被銷毀（Unity 的 == 重載會處理已銷毀的物件）
        if (this == null || gameObject == null)
        {
            // 如果物件已被銷毀，取消訂閱事件
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
            return;
        }
        
        // 當遊戲結束時顯示，其他狀態隱藏
        if (newState == GameManager.GameState.GameOver)
        {
            SetVisible(true);
            
            // Hide all gameplay UI
            GameUIManager gameUIManager = FindFirstObjectByType<GameUIManager>();
            if (gameUIManager != null)
            {
                gameUIManager.HideAllGameplayUI();
            }
        }
        else
        {
            SetVisible(false);
        }
    }
    
    /// <summary>
    /// 設定 GameOverUI（如果需要動態設定）
    /// </summary>
    public void SetGameOverUI(GameOverUI gameOver)
    {
        gameOverUI = gameOver;
    }
    
    /// <summary>
    /// 獲取 GameOverUI 引用
    /// </summary>
    public GameOverUI GetGameOverUI() => gameOverUI;
}

