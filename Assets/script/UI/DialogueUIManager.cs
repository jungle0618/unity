using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 對話UI管理器
/// 負責顯示需要玩家互動的對話框，例如：NPC對話、重要劇情訊息等
/// </summary>
public class DialogueUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;          // 對話面板
    [SerializeField] private TextMeshProUGUI dialogueText;      // 對話文字
    [SerializeField] private TextMeshProUGUI speakerNameText;   // 說話者名稱（可選）
    [SerializeField] private Button continueButton;             // 繼續按鈕
    [SerializeField] private Button skipButton;                 // 跳過按鈕（可選）
    [SerializeField] private GameObject buttonContainer;        // 按鈕容器（可選）
    
    private Button dialoguePanelButton;                         // 對話面板點擊區域（自動生成或手動指定）
    
    [Header("Display Settings")]
    [SerializeField] private bool enableTypewriterEffect = true; // 是否啟用打字機效果
    [SerializeField] private float typewriterSpeed = 0.05f;      // 打字機速度（每個字元間隔）
    [SerializeField] private bool pauseGameDuringDialogue = true; // 對話時是否暫停遊戲
    
    [Header("Button Texts")]
    [SerializeField] private string continueButtonText = "Continue...";
    [SerializeField] private string finishButtonText = "Close.";
    
    [Header("Input Settings")]
    [SerializeField] private bool enableSpaceKeyToContinue = true; // 是否啟用空格鍵繼續
    [SerializeField] private KeyCode continueKey = KeyCode.Space;   // 繼續按鍵
    
    [Header("UI Visibility Settings")]
    [SerializeField] private bool hideOtherUIDuringDialogue = true; // 對話時是否隱藏其他 UI
    
    private Queue<DialogueEntry> dialogueQueue = new Queue<DialogueEntry>();
    private bool isShowingDialogue = false;
    private bool isTyping = false;
    private Coroutine typewriterCoroutine;
    private Action onDialogueComplete;
    private bool isInitialized = false;
    private string currentMessage = ""; // 保存當前正在顯示的完整訊息
    private GameUIManager gameUIManager; // GameUIManager 引用
    
    // Skip functionality
    private HashSet<string> shownDialogues = new HashSet<string>(); // 已顯示過的對話
    private const string SHOWN_DIALOGUES_KEY = "ShownDialogues"; // PlayerPrefs 鍵
    
    /// <summary>
    /// 對話條目結構
    /// </summary>
    [System.Serializable]
    public class DialogueEntry
    {
        public string speakerName;  // 說話者名稱（可為空）
        public string message;      // 對話內容
        
        public DialogueEntry(string message, string speakerName = "")
        {
            this.message = message;
            this.speakerName = speakerName;
        }
    }
    
    /// <summary>
    /// 初始化對話UI
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 驗證組件
        if (dialoguePanel == null)
        {
            Debug.LogError("DialogueUIManager: 對話面板未設定！");
            return;
        }
        
        if (dialogueText == null)
        {
            Debug.LogError("DialogueUIManager: 對話文字未設定！");
            return;
        }
        
        // 設定按鈕
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            UpdateContinueButtonText(continueButtonText);
            // 隱藏繼續按鈕（改用點擊面板繼續）
            continueButton.gameObject.SetActive(false);
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
        
        // 設定對話面板點擊區域
        SetupDialoguePanelClickArea();
        
        // 初始隱藏
        dialoguePanel.SetActive(false);
        
        // 嘗試獲取 GameUIManager 引用（用於隱藏/顯示其他 UI）
        if (hideOtherUIDuringDialogue)
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>();
            if (gameUIManager == null)
            {
                Debug.LogWarning("DialogueUIManager: 未找到 GameUIManager，無法隱藏其他 UI");
            }
        }
        
        // 載入已顯示過的對話記錄
        LoadShownDialogues();
        
        isInitialized = true;
        Debug.Log("DialogueUIManager: 對話UI已初始化");
    }
    
    /// <summary>
    /// 顯示單條對話
    /// </summary>
    /// <param name="message">對話內容</param>
    /// <param name="speakerName">說話者名稱（可選）</param>
    /// <param name="onComplete">對話完成回調（可選）</param>
    public void ShowDialogue(string message, string speakerName = "", Action onComplete = null)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("DialogueUIManager: 尚未初始化！");
            return;
        }
        
        // 清空現有對話隊列
        ClearDialogueQueue();
        
        // 添加新對話
        dialogueQueue.Enqueue(new DialogueEntry(message, speakerName));
        onDialogueComplete = onComplete;
        
        // 開始顯示
        StartShowingDialogue();
    }
    
    /// <summary>
    /// 顯示多條對話
    /// </summary>
    /// <param name="messages">對話內容陣列</param>
    /// <param name="speakerName">說話者名稱（可選，所有對話使用同一名稱）</param>
    /// <param name="onComplete">所有對話完成回調（可選）</param>
    public void ShowDialogues(string[] messages, string speakerName = "", Action onComplete = null)
    {
        // 調用帶 dialogueId 的版本，但不傳遞 ID（不啟用跳過功能）
        ShowDialogues(messages, speakerName, "", onComplete);
    }
    
    /// <summary>
    /// 顯示多條對話（支援跳過功能）
    /// </summary>
    /// <param name="messages">對話內容陣列</param>
    /// <param name="speakerName">說話者名稱（可選，所有對話使用同一名稱）</param>
    /// <param name="dialogueId">對話唯一ID（用於記錄是否已顯示，可選）</param>
    /// <param name="onComplete">所有對話完成回調（可選）</param>
    public void ShowDialogues(string[] messages, string speakerName, string dialogueId, Action onComplete)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("DialogueUIManager: 尚未初始化！");
            return;
        }
        
        if (messages == null || messages.Length == 0)
        {
            Debug.LogWarning("DialogueUIManager: 對話內容為空！");
            return;
        }
        
        // 檢查是否應該跳過（只顯示最後一條）
        bool skipToLast = !string.IsNullOrEmpty(dialogueId) && HasShownDialogue(dialogueId);
        
        // 清空現有對話隊列
        ClearDialogueQueue();
        
        // 根據跳過狀態添加對話
        if (skipToLast)
        {
            // 只添加最後一條對話
            Debug.Log($"DialogueUIManager: 對話 '{dialogueId}' 已顯示過，只顯示最後一條");
            dialogueQueue.Enqueue(new DialogueEntry(messages[messages.Length - 1], speakerName));
        }
        else
        {
            // 添加所有對話
            foreach (string message in messages)
            {
                dialogueQueue.Enqueue(new DialogueEntry(message, speakerName));
            }
            
            // 標記為已顯示（如果有 ID）
            if (!string.IsNullOrEmpty(dialogueId))
            {
                MarkDialogueAsShown(dialogueId);
            }
        }
        
        onDialogueComplete = onComplete;
        
        // 開始顯示
        StartShowingDialogue();
    }
    
    /// <summary>
    /// 顯示多條對話（使用 DialogueEntry）
    /// </summary>
    /// <param name="dialogues">對話條目陣列</param>
    /// <param name="onComplete">所有對話完成回調（可選）</param>
    public void ShowDialogues(DialogueEntry[] dialogues, Action onComplete = null)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("DialogueUIManager: 尚未初始化！");
            return;
        }
        
        if (dialogues == null || dialogues.Length == 0)
        {
            Debug.LogWarning("DialogueUIManager: 對話內容為空！");
            return;
        }
        
        // 清空現有對話隊列
        ClearDialogueQueue();
        
        // 添加所有對話
        foreach (DialogueEntry dialogue in dialogues)
        {
            dialogueQueue.Enqueue(dialogue);
        }
        
        onDialogueComplete = onComplete;
        
        // 開始顯示
        StartShowingDialogue();
    }
    
    /// <summary>
    /// 開始顯示對話
    /// </summary>
    private void StartShowingDialogue()
    {
        if (isShowingDialogue)
        {
            Debug.LogWarning("DialogueUIManager: 對話已在顯示中！");
            return;
        }
        
        isShowingDialogue = true;
        
        // 暫停遊戲（如果需要）
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 0f;
        }
        
        // 隱藏其他 UI（如果啟用）
        if (hideOtherUIDuringDialogue && gameUIManager != null)
        {
            gameUIManager.HideAllGameplayUI();
        }
        
        // 確保對話面板顯示在最前面（移動到 Hierarchy 最後面）
        if (dialoguePanel != null)
        {
            dialoguePanel.transform.SetAsLastSibling();
        }
        
        // 顯示面板
        dialoguePanel.SetActive(true);
        
        // 顯示第一條對話
        ShowNextDialogue();
    }
    
    /// <summary>
    /// 顯示下一條對話
    /// </summary>
    private void ShowNextDialogue()
    {
        if (dialogueQueue.Count == 0)
        {
            // 沒有更多對話，關閉對話框
            CloseDialogue();
            return;
        }
        
        // 取得下一條對話
        DialogueEntry entry = dialogueQueue.Dequeue();
        
        // 更新說話者名稱
        if (speakerNameText != null)
        {
            if (!string.IsNullOrEmpty(entry.speakerName))
            {
                speakerNameText.text = entry.speakerName;
                speakerNameText.gameObject.SetActive(true);
            }
            else
            {
                speakerNameText.gameObject.SetActive(false);
            }
        }
        
        // 更新繼續按鈕文字
        if (dialogueQueue.Count == 0)
        {
            // 這是最後一條對話
            UpdateContinueButtonText(finishButtonText);
        }
        else
        {
            UpdateContinueButtonText(continueButtonText);
        }
        
        // 顯示對話內容
        if (enableTypewriterEffect)
        {
            StartTypewriter(entry.message);
        }
        else
        {
            dialogueText.text = entry.message;
            isTyping = false;
        }
    }
    
    /// <summary>
    /// 開始打字機效果
    /// </summary>
    private void StartTypewriter(string message)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        
        currentMessage = message; // 保存完整訊息
        typewriterCoroutine = StartCoroutine(TypewriterEffect(message));
    }
    
    /// <summary>
    /// 打字機效果協程
    /// </summary>
    private System.Collections.IEnumerator TypewriterEffect(string message)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in message)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typewriterSpeed); // 使用 Realtime 以支援暫停時的顯示
        }
        
        isTyping = false;
        typewriterCoroutine = null;
    }
    
    /// <summary>
    /// 繼續按鈕點擊事件
    /// </summary>
    private void OnContinueButtonClicked()
    {
        if (isTyping)
        {
            // 如果正在打字，完成打字效果
            CompleteTypewriter();
        }
        else
        {
            // 顯示下一條對話
            ShowNextDialogue();
        }
    }
    
    /// <summary>
    /// 跳過按鈕點擊事件
    /// </summary>
    private void OnSkipButtonClicked()
    {
        // 清空對話隊列並關閉
        ClearDialogueQueue();
        CloseDialogue();
    }
    
    /// <summary>
    /// 完成打字機效果
    /// </summary>
    private void CompleteTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        // 立即顯示完整文字
        if (!string.IsNullOrEmpty(currentMessage) && dialogueText != null)
        {
            dialogueText.text = currentMessage;
        }
        
        isTyping = false;
    }
    
    /// <summary>
    /// 關閉對話
    /// </summary>
    private void CloseDialogue()
    {
        // 停止打字機效果
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        // 隱藏面板
        dialoguePanel.SetActive(false);
        
        // 恢復其他 UI（如果之前隱藏了）
        // 注意：只有在遊戲還在進行中時才恢復 UI，遊戲結束時不恢復（由 MissionDialogueManager 控制）
        if (hideOtherUIDuringDialogue && gameUIManager != null)
        {
            // 檢查遊戲狀態，只有在遊戲進行中時才恢復 UI
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                gameUIManager.ShowAllGameplayUI();
            }
            // 如果遊戲已結束（GameOver 或 GameWin），不恢復 UI
            // MissionDialogueManager 會在對話完成後顯示對應的結算 UI
        }
        
        // 恢復遊戲（如果之前暫停了）
        // 注意：遊戲結束時不恢復時間（保持暫停狀態）
        if (pauseGameDuringDialogue)
        {
            // 只有在遊戲還在進行中時才恢復時間
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                Time.timeScale = 1f;
            }
        }
        
        isShowingDialogue = false;
        isTyping = false;
        
        // 執行完成回調
        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
    }
    
    /// <summary>
    /// 清空對話隊列
    /// </summary>
    private void ClearDialogueQueue()
    {
        dialogueQueue.Clear();
    }
    
    /// <summary>
    /// 更新繼續按鈕文字
    /// </summary>
    private void UpdateContinueButtonText(string text)
    {
        if (continueButton != null)
        {
            TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }
    }
    
    /// <summary>
    /// 檢查是否正在顯示對話
    /// </summary>
    public bool IsShowingDialogue()
    {
        return isShowingDialogue;
    }
    
    /// <summary>
    /// 強制關閉對話（用於場景切換等情況）
    /// </summary>
    public void ForceClose()
    {
        ClearDialogueQueue();
        CloseDialogue();
        // CloseDialogue() 已經會恢復其他 UI，所以這裡不需要額外處理
    }
    
    /// <summary>
    /// 檢測輸入（空格鍵繼續）
    /// </summary>
    private void Update()
    {
        // 如果啟用了空格鍵繼續，且正在顯示對話
        if (enableSpaceKeyToContinue && isShowingDialogue)
        {
            // 檢測空格鍵按下（使用 GetKeyDown 避免重複觸發）
            if (Input.GetKeyDown(continueKey))
            {
                OnContinueButtonClicked();
            }
        }
    }
    
    /// <summary>
    /// 自動設定對話面板點擊區域
    /// </summary>
    private void SetupDialoguePanelClickArea()
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("DialogueUIManager: 對話面板為空，無法設定點擊區域");
            return;
        }
        
        // 檢查是否已有 Button 組件
        dialoguePanelButton = dialoguePanel.GetComponent<Button>();
        
        if (dialoguePanelButton == null)
        {
            // 添加 Button 組件
            dialoguePanelButton = dialoguePanel.AddComponent<Button>();
            Debug.Log("DialogueUIManager: 已添加對話面板點擊區域 Button 組件");
        }
        
        // 設定為透明（不影響視覺）
        dialoguePanelButton.transition = UnityEngine.UI.Selectable.Transition.None;
        
        // 添加點擊監聽
        dialoguePanelButton.onClick.AddListener(OnContinueButtonClicked);
        
        // 確保對話面板有 Image 組件（Button 需要 Graphic 組件才能接收點擊）
        UnityEngine.UI.Image panelImage = dialoguePanel.GetComponent<UnityEngine.UI.Image>();
        if (panelImage == null)
        {
            panelImage = dialoguePanel.AddComponent<UnityEngine.UI.Image>();
            // 設定為幾乎透明但仍可接收點擊
            Color transparent = Color.white;
            transparent.a = 0.01f;
            panelImage.color = transparent;
            Debug.Log("DialogueUIManager: 已添加 Image 組件用於點擊檢測");
        }
        
        // 確保 Raycast Target 啟用
        panelImage.raycastTarget = true;
        
        Debug.Log("DialogueUIManager: 對話面板點擊區域已設定完成");
    }
    
    // ========== Skip Functionality Helper Methods ==========
    
    /// <summary>
    /// 載入已顯示過的對話記錄
    /// </summary>
    private void LoadShownDialogues()
    {
        if (!PlayerPrefs.HasKey(SHOWN_DIALOGUES_KEY))
        {
            shownDialogues = new HashSet<string>();
            return;
        }
        
        string savedData = PlayerPrefs.GetString(SHOWN_DIALOGUES_KEY);
        if (string.IsNullOrEmpty(savedData))
        {
            shownDialogues = new HashSet<string>();
            return;
        }
        
        // 分割儲存的對話 ID（使用逗號分隔）
        string[] dialogueIds = savedData.Split(',');
        shownDialogues = new HashSet<string>(dialogueIds);
        
        Debug.Log($"DialogueUIManager: 載入了 {shownDialogues.Count} 個已顯示的對話記錄");
    }
    
    /// <summary>
    /// 儲存已顯示過的對話記錄
    /// </summary>
    private void SaveShownDialogues()
    {
        if (shownDialogues.Count == 0)
        {
            PlayerPrefs.SetString(SHOWN_DIALOGUES_KEY, "");
        }
        else
        {
            // 將 HashSet 轉換為逗號分隔的字串
            string savedData = string.Join(",", shownDialogues);
            PlayerPrefs.SetString(SHOWN_DIALOGUES_KEY, savedData);
        }
        
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 檢查對話是否已顯示過
    /// </summary>
    /// <param name="dialogueId">對話唯一ID</param>
    /// <returns>是否已顯示過</returns>
    private bool HasShownDialogue(string dialogueId)
    {
        return !string.IsNullOrEmpty(dialogueId) && shownDialogues.Contains(dialogueId);
    }
    
    /// <summary>
    /// 標記對話為已顯示
    /// </summary>
    /// <param name="dialogueId">對話唯一ID</param>
    private void MarkDialogueAsShown(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;
        
        if (!shownDialogues.Contains(dialogueId))
        {
            shownDialogues.Add(dialogueId);
            SaveShownDialogues();
            Debug.Log($"DialogueUIManager: 對話 '{dialogueId}' 已標記為已顯示");
        }
    }
    
    /// <summary>
    /// 重置所有已顯示的對話記錄（用於新遊戲或重置進度）
    /// </summary>
    public void ResetShownDialogues()
    {
        shownDialogues.Clear();
        PlayerPrefs.DeleteKey(SHOWN_DIALOGUES_KEY);
        PlayerPrefs.Save();
        Debug.Log("DialogueUIManager: 已重置所有對話記錄");
    }
    
    private void OnDestroy()
    {
        // 清理按鈕監聽器
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
        
        if (dialoguePanelButton != null)
        {
            dialoguePanelButton.onClick.RemoveListener(OnContinueButtonClicked);
        }
        
        // 恢復時間縮放
        if (isShowingDialogue && pauseGameDuringDialogue)
        {
            Time.timeScale = 1f;
        }
    }
}


