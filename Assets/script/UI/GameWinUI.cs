using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 任務成功頁面UI
/// 顯示遊戲統計數據（擊殺數、遊戲時間、最快速通關時間等）和操作按鈕
/// </summary>
public class GameWinUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject gameWinPanel;

    [Header("Generation Settings")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;

    [Header("Button Positions (RectTransforms)")]
    [SerializeField] private RectTransform restartButtonPos;
    [SerializeField] private RectTransform mainMenuButtonPos;

    [Header("Statistics Display")]
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;

    [Header("Settings")]
    [SerializeField] private string enemiesKilledFormat = "Enemies Killed: {0}";
    [SerializeField] private string gameTimeFormat = "Game Time: {0:F1}s";
    [SerializeField] private string bestTimeFormat = "Best Time: {0:F1}s";

    // 內部引用 (用於追蹤生成的按鈕)
    private readonly List<GameObject> generatedButtons = new List<GameObject>();

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
    /// 生成所有按鈕
    /// </summary>
    private void GenerateButtons()
    {
        if (buttonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("[GameWinUI] Button Prefab or Container is missing!");
            return;
        }

        DestroyButtons();

        generatedButtons.Add(CreateButton("Restart", restartButtonPos, OnRestartClicked));
        generatedButtons.Add(CreateButton("Main Menu", mainMenuButtonPos, OnMainMenuClicked));
    }

    /// <summary>
    /// 銷毀所有生成的按鈕
    /// </summary>
    private void DestroyButtons()
    {
        foreach (var btn in generatedButtons)
        {
            if (btn != null)
            {
                Destroy(btn);
            }
        }
        generatedButtons.Clear();
    }

    /// <summary>
    /// 創建單個按鈕
    /// </summary>
    private GameObject CreateButton(string text, RectTransform targetPos, UnityEngine.Events.UnityAction onClickAction)
    {
        if (targetPos == null) return null;

        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
        btnObj.name = $"{text} Button";

        // 設定位置和大小
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.anchorMin = targetPos.anchorMin;
            btnRect.anchorMax = targetPos.anchorMax;
            btnRect.pivot = targetPos.pivot;
            btnRect.anchoredPosition = targetPos.anchoredPosition;
            btnRect.sizeDelta = targetPos.sizeDelta;
            btnRect.rotation = targetPos.rotation;
            btnRect.localScale = targetPos.localScale;
        }

        // 設定文字 (SF Button -> Background -> Label -> Text)
        Transform background = btnObj.transform.Find("Background");
        if (background != null)
        {
            Transform label = background.Find("Label");
            if (label != null)
            {
                // 確保文字在階層順序最前面
                label.SetAsLastSibling();

                Text textComp = label.GetComponent<Text>();
                if (textComp != null) textComp.text = text;

                TextMeshProUGUI tmpComp = label.GetComponent<TextMeshProUGUI>();
                if (tmpComp != null) tmpComp.text = text;
            }
        }

        // 設定點擊事件
        Button btnComp = btnObj.GetComponent<Button>();
        if (btnComp != null && onClickAction != null)
        {
            btnComp.onClick.RemoveAllListeners();
            btnComp.onClick.AddListener(onClickAction);
        }

        return btnObj;
    }

    /// <summary>
    /// 處理遊戲狀態變化事件
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        if (gameWinPanel == null) return;

        if (newState == GameManager.GameState.GameWin)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    /// <summary>
    /// 更新統計數據顯示
    /// </summary>
    private void UpdateStatistics()
    {
        if (GameManager.Instance == null) return;

        if (enemiesKilledText != null)
        {
            int enemiesKilled = GameManager.Instance.GetEnemiesKilled();
            enemiesKilledText.text = string.Format(enemiesKilledFormat, enemiesKilled);
        }

        if (gameTimeText != null)
        {
            float gameTime = GameManager.Instance.GetGameTime();
            gameTimeText.text = string.Format(gameTimeFormat, gameTime);
        }

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

    #region 面板控制

    public void Show()
    {
        if (gameWinPanel != null)
        {
            gameWinPanel.SetActive(true);
            GenerateButtons();
            UpdateStatistics();
        }
    }

    public void Hide()
    {
        if (gameWinPanel != null)
        {
            DestroyButtons();
            gameWinPanel.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (gameWinPanel != null)
        {
            if (gameWinPanel.activeSelf) Hide();
            else Show();
        }
    }

    #endregion

    /// <summary>
    /// 公開方法：設定任務成功頁面顯示/隱藏（可從其他腳本調用）
    /// </summary>
    public void SetGameWinPanelActive(bool active)
    {
        if (gameWinPanel != null)
        {
            if (active) Show();
            else Hide();
        }
    }
}

