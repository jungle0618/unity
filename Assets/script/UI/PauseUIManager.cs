using UnityEngine;

/// <summary>
/// 暫停選單UI管理器
/// 負責管理暫停選單的顯示和隱藏，與 GameManager 整合
/// </summary>
public class PauseUIManager : MonoBehaviour
{
    [Header("Pause UI Reference")]
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private bool autoFindPauseMenu = true;
    
    [Header("Settings")]
    [SerializeField] private bool autoSubscribeToGameManager = true; // 自動訂閱 GameManager 事件
    
    private bool isVisible = false;
    
    /// <summary>
    /// 初始化暫停選單UI
    /// </summary>
    public void Initialize()
    {
        // 尋找 PauseMenuUI
        if (autoFindPauseMenu && pauseMenuUI == null)
        {
            pauseMenuUI = FindFirstObjectByType<PauseMenuUI>();
        }
        
        if (pauseMenuUI == null)
        {
            Debug.LogWarning("PauseUIManager: PauseMenuUI 未設定或找不到");
            return;
        }
        
        // 訂閱 GameManager 事件（如果需要）
        if (autoSubscribeToGameManager && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        
        // 初始隱藏
        SetVisible(false);
        
        Debug.Log("PauseUIManager: 暫停選單UI已初始化");
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
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetPauseMenuActive(visible);
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
        
        // 當遊戲暫停時顯示，其他狀態隱藏
        if (newState == GameManager.GameState.Paused)
        {
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }
    
    /// <summary>
    /// 設定 PauseMenuUI（如果需要動態設定）
    /// </summary>
    public void SetPauseMenuUI(PauseMenuUI pauseMenu)
    {
        pauseMenuUI = pauseMenu;
    }
    
    /// <summary>
    /// 獲取 PauseMenuUI 引用
    /// </summary>
    public PauseMenuUI GetPauseMenuUI() => pauseMenuUI;
}

