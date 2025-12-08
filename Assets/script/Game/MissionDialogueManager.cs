using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 任務對話管理器
/// 負責在遊戲開始、勝利、失敗時顯示任務對話序列
/// </summary>
public class MissionDialogueManager : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private TextAsset dialogueDataFile; // missiondialogues.json 文件
    
    [Header("Dialogue References")]
    [SerializeField] private DialogueUIManager dialogueUIManager;
    
    [Header("Settings")]
    [SerializeField] private bool showMissionStartDialogue = true;
    [SerializeField] private bool showMissionWinDialogue = true;
    [SerializeField] private bool showMissionFailDialogue = true;
    [SerializeField] private float delayBeforeShowingUI = 0.5f; // 對話完成後顯示UI的延遲時間
    [SerializeField] private bool showDebugInfo = false;
    
    private MissionDialogueDataLoader dialogueDataLoader;
    private MissionDialogueDataLoader.MissionDialogueData dialogueData;
    private bool hasShownStartDialogue = false;
    private bool isDialogueActive = false;
    private GameUIManager gameUIManager; // GameUIManager 引用
    
    private void Awake()
    {
        // 初始化數據載入器
        dialogueDataLoader = new MissionDialogueDataLoader(showDebugInfo);
    }
    
    private void Start()
    {
        // 載入對話數據
        if (dialogueDataFile != null)
        {
            if (dialogueDataLoader.LoadDialogueData(dialogueDataFile))
            {
                dialogueData = dialogueDataLoader.DialogueData;
                if (showDebugInfo)
                {
                    Debug.Log("[MissionDialogueManager] 對話數據載入成功");
                }
            }
            else
            {
                Debug.LogWarning("[MissionDialogueManager] 對話數據載入失敗，使用默認數據");
                dialogueData = dialogueDataLoader.DialogueData;
            }
        }
        else
        {
            Debug.LogWarning("[MissionDialogueManager] 對話數據文件未設定！請在 Inspector 中設定 missiondialogues.json");
            dialogueData = dialogueDataLoader.DialogueData; // 使用默認數據
        }
        
        // 訂閱遊戲狀態變化事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        
        // 嘗試獲取 GameUIManager 引用
        gameUIManager = FindFirstObjectByType<GameUIManager>();
        
        // 如果找不到 DialogueUIManager，嘗試從 GameUIManager 獲取
        if (dialogueUIManager == null && gameUIManager != null)
        {
            dialogueUIManager = gameUIManager.GetDialogueUIManager();
        }
        
        // 如果還是找不到，嘗試直接查找
        if (dialogueUIManager == null)
        {
            dialogueUIManager = FindFirstObjectByType<DialogueUIManager>();
        }
        
        if (dialogueUIManager == null)
        {
            Debug.LogWarning("[MissionDialogueManager] DialogueUIManager 未找到！對話功能可能無法使用。");
        }
        
        // 檢查當前遊戲狀態（處理時序問題：如果狀態變化在 Start() 之前發生）
        if (GameManager.Instance != null)
        {
            // 如果遊戲已經是 Playing 狀態，且還沒顯示過開始對話，立即顯示
            // 這處理了 MissionDialogueManager 的 Start() 在 GameManager.OnSceneLoaded 之後執行的情況
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing && 
                !hasShownStartDialogue && 
                showMissionStartDialogue)
            {
                hasShownStartDialogue = true;
                StartCoroutine(ShowMissionStartDialogueDelayed(0.1f));
            }
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
    /// 處理遊戲狀態變化
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        // 遊戲開始時顯示任務開始對話
        if (newState == GameManager.GameState.Playing && 
            oldState != GameManager.GameState.Playing && 
            !hasShownStartDialogue && 
            showMissionStartDialogue)
        {
            hasShownStartDialogue = true;
            StartCoroutine(ShowMissionStartDialogueDelayed(0.1f)); // 稍微延遲，確保所有系統已初始化
        }
        // 遊戲勝利時顯示勝利對話
        else if (newState == GameManager.GameState.GameWin && showMissionWinDialogue)
        {
            // 注意：GameUIManager 會自動隱藏所有 UI（除了 Dialogue），對話完成後會再顯示 GameWin UI
            ShowMissionWinDialogue();
        }
        // 遊戲失敗時顯示失敗對話
        else if (newState == GameManager.GameState.GameOver && showMissionFailDialogue)
        {
            // 注意：GameUIManager 會自動隱藏所有 UI（除了 Dialogue），對話完成後會再顯示 GameOver UI
            ShowMissionFailDialogue();
        }
    }
    
    /// <summary>
    /// 延遲顯示任務開始對話
    /// </summary>
    private IEnumerator ShowMissionStartDialogueDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowMissionStartDialogue();
    }
    
    /// <summary>
    /// 顯示任務開始對話
    /// </summary>
    private void ShowMissionStartDialogue()
    {
        if (dialogueUIManager == null || dialogueData == null)
        {
            Debug.LogWarning("[MissionDialogueManager] 無法顯示任務開始對話：DialogueUIManager 或對話數據為空");
            return;
        }
        
        if (dialogueData.missionStartDialogues == null || dialogueData.missionStartDialogues.Length == 0)
        {
            Debug.LogWarning("[MissionDialogueManager] 任務開始對話為空");
            return;
        }
        
        isDialogueActive = true;
        // 使用 dialogueId "mission_start" 啟用跳過功能
        // 第一次：顯示所有對話
        // 之後：只顯示最後一條對話
        dialogueUIManager.ShowDialogues(
            dialogueData.missionStartDialogues, 
            "", 
            "mission_start", // 對話唯一 ID
            OnMissionStartDialogueComplete
        );
    }
    
    /// <summary>
    /// 任務開始對話完成回調
    /// </summary>
    private void OnMissionStartDialogueComplete()
    {
        isDialogueActive = false;
        Debug.Log("[MissionDialogueManager] 任務開始對話完成，遊戲開始！");
        
        // 對話完成後，確保遊戲恢復正常時間
        if (GameManager.Instance != null)
        {
            Time.timeScale = 1f;
        }
    }
    
    /// <summary>
    /// 顯示任務勝利對話
    /// </summary>
    private void ShowMissionWinDialogue()
    {
        if (dialogueUIManager == null || dialogueData == null)
        {
            Debug.LogWarning("[MissionDialogueManager] 無法顯示任務勝利對話：DialogueUIManager 或對話數據為空");
            // 如果對話無法顯示，直接顯示勝利UI
            ShowGameWinUI();
            return;
        }
        
        if (dialogueData.missionWinDialogues == null || dialogueData.missionWinDialogues.Length == 0)
        {
            Debug.LogWarning("[MissionDialogueManager] 任務勝利對話為空，直接顯示勝利UI");
            ShowGameWinUI();
            return;
        }
        
        isDialogueActive = true;
        dialogueUIManager.ShowDialogues(
            dialogueData.missionWinDialogues, 
            "", 
            OnMissionWinDialogueComplete
        );
    }
    
    /// <summary>
    /// 任務勝利對話完成回調
    /// </summary>
    private void OnMissionWinDialogueComplete()
    {
        isDialogueActive = false;
        Debug.Log("[MissionDialogueManager] 任務勝利對話完成，顯示勝利UI");
        
        // 對話完成後，顯示勝利UI
        StartCoroutine(ShowGameWinUIAfterDelay(delayBeforeShowingUI));
    }
    
    /// <summary>
    /// 延遲顯示勝利UI
    /// </summary>
    private IEnumerator ShowGameWinUIAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ShowGameWinUI();
    }
    
    /// <summary>
    /// 顯示勝利UI
    /// </summary>
    private void ShowGameWinUI()
    {
        // 使用 GameUIManager 的方法來顯示 GameWin UI
        if (gameUIManager != null)
        {
            gameUIManager.ShowGameWinUI();
        }
        else
        {
            // 回退方案：直接查找並顯示
            GameWinUI gameWinUI = FindFirstObjectByType<GameWinUI>();
            if (gameWinUI != null)
            {
                gameWinUI.SetGameWinPanelActive(true);
            }
        }
    }
    
    /// <summary>
    /// 顯示任務失敗對話
    /// </summary>
    private void ShowMissionFailDialogue()
    {
        if (dialogueUIManager == null || dialogueData == null)
        {
            Debug.LogWarning("[MissionDialogueManager] 無法顯示任務失敗對話：DialogueUIManager 或對話數據為空");
            // 如果對話無法顯示，直接顯示失敗UI
            ShowGameOverUI();
            return;
        }
        
        if (dialogueData.missionFailDialogues == null || dialogueData.missionFailDialogues.Length == 0)
        {
            Debug.LogWarning("[MissionDialogueManager] 任務失敗對話為空，直接顯示失敗UI");
            ShowGameOverUI();
            return;
        }
        
        isDialogueActive = true;
        dialogueUIManager.ShowDialogues(
            dialogueData.missionFailDialogues, 
            "", 
            OnMissionFailDialogueComplete
        );
    }
    
    /// <summary>
    /// 任務失敗對話完成回調
    /// </summary>
    private void OnMissionFailDialogueComplete()
    {
        isDialogueActive = false;
        Debug.Log("[MissionDialogueManager] 任務失敗對話完成，顯示重試選單");
        
        // 對話完成後，顯示失敗UI
        StartCoroutine(ShowGameOverUIAfterDelay(delayBeforeShowingUI));
    }
    
    /// <summary>
    /// 延遲顯示失敗UI
    /// </summary>
    private IEnumerator ShowGameOverUIAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ShowGameOverUI();
    }
    
    /// <summary>
    /// 顯示失敗UI
    /// </summary>
    private void ShowGameOverUI()
    {
        // 使用 GameUIManager 的方法來顯示 GameOver UI
        if (gameUIManager != null)
        {
            gameUIManager.ShowGameOverUI();
        }
        else
        {
            // 回退方案：直接查找並顯示
            GameOverUI gameOverUI = FindFirstObjectByType<GameOverUI>();
            if (gameOverUI != null)
            {
                gameOverUI.SetGameOverPanelActive(true);
            }
        }
    }
    
    /// <summary>
    /// 重置狀態（用於重新開始遊戲）
    /// </summary>
    public void Reset()
    {
        hasShownStartDialogue = false;
        isDialogueActive = false;
    }
    
    /// <summary>
    /// 檢查是否正在顯示對話
    /// </summary>
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}

