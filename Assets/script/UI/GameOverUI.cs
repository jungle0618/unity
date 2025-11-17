using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 遊戲結束結算頁面UI
/// 顯示遊戲統計數據（殺敵數、遊戲時間等）和操作按鈕
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    
    [Header("Statistics Display")]
    [SerializeField] private TextMeshProUGUI reasonText;  // Reason for game over
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI gameTimeText;
    
    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Settings")]
    [SerializeField] private string reasonFormat = "Reason: {0}";
    [SerializeField] private string enemiesKilledFormat = "Enemies Killed: {0}";
    [SerializeField] private string gameTimeFormat = "Time Survived: {0:F1}s";
    
    private string currentReason = "Unknown";

    private void Awake()
    {
        // 初始隱藏結算頁面
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        // 訂閱遊戲狀態變化事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        // 設定按鈕監聽器
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnDestroy()
    {
        // 取消訂閱事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    /// <summary>
    /// 處理遊戲狀態變化事件
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        if (gameOverPanel == null) return;

        // 當遊戲結束時顯示結算頁面
        if (newState == GameManager.GameState.GameOver)
        {
            UpdateStatistics();
            gameOverPanel.SetActive(true);
        }
        else
        {
            gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 更新統計數據顯示
    /// </summary>
    private void UpdateStatistics()
    {
        if (GameManager.Instance == null) return;

        // Update reason
        if (reasonText != null)
        {
            reasonText.text = string.Format(reasonFormat, currentReason);
        }

        // 更新擊殺數
        if (enemiesKilledText != null)
        {
            int enemiesKilled = GameManager.Instance.GetEnemiesKilled();
            enemiesKilledText.text = string.Format(enemiesKilledFormat, enemiesKilled);
        }

        // 更新遊戲時間
        if (gameTimeText != null)
        {
            float gameTime = GameManager.Instance.GetGameTime();
            gameTimeText.text = string.Format(gameTimeFormat, gameTime);
        }
    }

    /// <summary>
    /// 重新開始遊戲按鈕點擊事件
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("[GameOverUI] Restart button clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    /// <summary>
    /// 返回主選單按鈕點擊事件
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverUI] Main Menu button clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    /// <summary>
    /// 公開方法：設定結算頁面顯示/隱藏（可從其他腳本調用）
    /// </summary>
    public void SetGameOverPanelActive(bool active)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(active);
            
            // 如果顯示，更新統計數據
            if (active)
            {
                UpdateStatistics();
            }
        }
    }
    
    /// <summary>
    /// Set the reason for game over
    /// </summary>
    public void SetReason(string reason)
    {
        currentReason = reason;
        if (reasonText != null)
        {
            reasonText.text = string.Format(reasonFormat, reason);
        }
    }
}

