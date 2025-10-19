using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 遊戲UI管理器
/// 管理遊戲中的所有UI元素，包括血條和危險等級顯示
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private PlayerHealthUI playerHealthUI;
    [SerializeField] private DangerousUI dangerousUI;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject healthPanel;
    [SerializeField] private GameObject dangerPanel;
    [SerializeField] private GameObject gameOverPanel;
    
    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    
    [Header("Settings")]
    [SerializeField] private bool showHealthUI = true;
    [SerializeField] private bool showDangerUI = true;
    
    private PlayerController playerController;
    private DangerousManager dangerousManager;
    
    private void Start()
    {
        // 獲取必要的組件
        playerController = FindFirstObjectByType<PlayerController>();
        dangerousManager = DangerousManager.Instance;
        
        // 初始化UI
        InitializeUI();
        
        // 訂閱事件
        if (playerController != null)
        {
            playerController.OnPlayerDied += OnPlayerDied;
        }
        
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelChanged;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (playerController != null)
        {
            playerController.OnPlayerDied -= OnPlayerDied;
        }
        
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged -= OnDangerLevelChanged;
        }
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 設定血條UI
        if (playerHealthUI != null)
        {
            playerHealthUI.SetPlayerController(playerController);
        }
        
        // 設定危險等級UI
        if (dangerousUI != null)
        {
            // DangerousUI會自動獲取DangerousManager.Instance，不需要額外設定
            Debug.Log("危險等級UI已初始化");
        }
        
        // 設定按鈕事件
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        
        // 初始顯示狀態
        SetHealthUIVisible(showHealthUI);
        SetDangerUIVisible(showDangerUI);
        SetGameOverUIVisible(false);
    }
    
    /// <summary>
    /// 處理玩家死亡事件
    /// </summary>
    private void OnPlayerDied()
    {
        SetGameOverUIVisible(true);
        if (gameOverText != null)
        {
            gameOverText.text = "遊戲結束\n玩家死亡";
        }
    }
    
    /// <summary>
    /// 處理危險等級變化事件
    /// </summary>
    private void OnDangerLevelChanged(DangerousManager.DangerLevel level)
    {
        // 可以在這裡添加危險等級變化時的UI效果
        Debug.Log($"危險等級變化: {dangerousManager.GetDangerLevelDescription(level)}");
    }
    
    /// <summary>
    /// 設定血條UI可見性
    /// </summary>
    public void SetHealthUIVisible(bool visible)
    {
        if (healthPanel != null)
        {
            healthPanel.SetActive(visible);
        }
        
        if (playerHealthUI != null)
        {
            playerHealthUI.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 設定危險等級UI可見性
    /// </summary>
    public void SetDangerUIVisible(bool visible)
    {
        if (dangerPanel != null)
        {
            dangerPanel.SetActive(visible);
        }
        
        if (dangerousUI != null)
        {
            dangerousUI.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 設定遊戲結束UI可見性
    /// </summary>
    public void SetGameOverUIVisible(bool visible)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 重新開始遊戲
    /// </summary>
    public void RestartGame()
    {
        // 復活玩家
        if (playerController != null)
        {
            playerController.Resurrect();
        }
        
        // 重置危險等級
        if (dangerousManager != null)
        {
            dangerousManager.ResetDangerLevel();
        }
        
        // 隱藏遊戲結束UI
        SetGameOverUIVisible(false);
        
        Debug.Log("遊戲重新開始");
    }
    
    /// <summary>
    /// 退出遊戲
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 切換血條UI顯示
    /// </summary>
    public void ToggleHealthUI()
    {
        showHealthUI = !showHealthUI;
        SetHealthUIVisible(showHealthUI);
    }
    
    /// <summary>
    /// 切換危險等級UI顯示
    /// </summary>
    public void ToggleDangerUI()
    {
        showDangerUI = !showDangerUI;
        SetDangerUIVisible(showDangerUI);
    }
}
