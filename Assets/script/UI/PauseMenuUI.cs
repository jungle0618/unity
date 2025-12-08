using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Generation Settings")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;

    [Header("Button Positions (RectTransforms)")]
    [SerializeField] private RectTransform resumeButtonPos;
    [SerializeField] private RectTransform restartButtonPos;
    [SerializeField] private RectTransform mainMenuButtonPos;


    [Header("Settings")]
    [SerializeField] private bool hideOnStart = true;

    private readonly System.Collections.Generic.List<GameObject> generatedButtons = new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        // 初始隱藏
        if (pauseMenuPanel != null && hideOnStart)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 訂閱遊戲狀態變化
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

    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        if (pauseMenuPanel == null) return;

        // Paused 顯示，其餘隱藏
        if (newState == GameManager.GameState.Paused)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void OnResumeClicked() => ResumeButton();
    private void OnRestartClicked() => RestartButton();
    private void OnMainMenuClicked() => MainMenuButton();

    /// <summary>
    /// 生成按鈕
    /// </summary>
    private void GenerateButtons()
    {
        if (buttonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("[PauseMenuUI] Button Prefab or Container is missing!");
            return;
        }

        DestroyButtons();

        generatedButtons.Add(CreateButton("Resume", resumeButtonPos, ResumeButton));
        generatedButtons.Add(CreateButton("Restart", restartButtonPos, RestartButton));
        generatedButtons.Add(CreateButton("Main Menu", mainMenuButtonPos, MainMenuButton));
    }

    /// <summary>
    /// 銷毀按鈕
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
    /// 建立單一按鈕
    /// </summary>
    private GameObject CreateButton(string text, RectTransform targetPos, UnityEngine.Events.UnityAction onClickAction)
    {
        if (targetPos == null) return null;

        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
        btnObj.name = $"{text} Button";

        // 位置尺寸複製
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

        // 設定文字
        Transform background = btnObj.transform.Find("Background");
        if (background != null)
        {
            Transform label = background.Find("Label");
            if (label != null)
            {
                Text uiText = label.GetComponent<Text>();
                if (uiText != null) uiText.text = text;

                TMPro.TextMeshProUGUI tmpText = label.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = text;
            }
        }

        // 點擊事件
        Button btnComp = btnObj.GetComponent<Button>();
        if (btnComp != null && onClickAction != null)
        {
            btnComp.onClick.RemoveAllListeners();
            btnComp.onClick.AddListener(onClickAction);
        }

        return btnObj;
    }

    #region 面板控制

    public void Show()
    {
        if (pauseMenuPanel == null) return;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        GenerateButtons();
        pauseMenuPanel.SetActive(true);
    }

    public void Hide()
    {
        if (pauseMenuPanel == null) return;

        DestroyButtons();
        pauseMenuPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (pauseMenuPanel == null) return;
        if (pauseMenuPanel.activeSelf) Hide();
        else Show();
    }

    /// <summary>
    /// 與舊版兼容的顯示/隱藏介面
    /// </summary>
    public void SetPauseMenuActive(bool active)
    {
        if (active) Show();
        else Hide();
    }

    #endregion

    #region 按鈕函數

    public void ResumeButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        Hide();
    }

    public void RestartButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    public void MainMenuButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    #endregion
}

