using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 任務成功頁面UI
/// 顯示遊戲統計數據（擊殺數、遊戲時間、最快速通關時間等）和操作按鈕
/// </summary>
public class GameWinUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameWinPanel;
    
    [Header("Statistics Display")]
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;
    
    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Settings")]
    [SerializeField] private string enemiesKilledFormat = "Enemies Killed: {0}";
    [SerializeField] private string gameTimeFormat = "Game Time: {0:F1}s";
    [SerializeField] private string bestTimeFormat = "Best Time: {0:F1}s";

    private void Awake()
    {
        // 初始隱藏任務成功頁面
        if (gameWinPanel != null)
            gameWinPanel.SetActive(false);
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
        if (gameWinPanel == null) return;

        // 當遊戲勝利時顯示任務成功頁面
        if (newState == GameManager.GameState.GameWin)
        {
            UpdateStatistics();
            gameWinPanel.SetActive(true);
        }
        else
        {
            gameWinPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 更新統計數據顯示
    /// </summary>
    private void UpdateStatistics()
    {
        if (GameManager.Instance == null) return;

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

        // 更新最快速通關時間
        if (bestTimeText != null)
        {
            float bestTime = GameManager.Instance.GetBestTime();
            bestTimeText.text = string.Format(bestTimeFormat, bestTime);
        }
    }

    /// <summary>
    /// 重新開始遊戲按鈕點擊事件
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("[GameWinUI] Restart button clicked");
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
        Debug.Log("[GameWinUI] Main Menu button clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    /// <summary>
    /// 公開方法：設定任務成功頁面顯示/隱藏（可從其他腳本調用）
    /// </summary>
    public void SetGameWinPanelActive(bool active)
    {
        if (gameWinPanel != null)
        {
            gameWinPanel.SetActive(active);
            
            // 如果顯示，更新統計數據
            if (active)
            {
                UpdateStatistics();
            }
        }
    }
}

