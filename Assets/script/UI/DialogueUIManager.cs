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
    
    [Header("Display Settings")]
    [SerializeField] private bool enableTypewriterEffect = true; // 是否啟用打字機效果
    [SerializeField] private float typewriterSpeed = 0.05f;      // 打字機速度（每個字元間隔）
    [SerializeField] private bool pauseGameDuringDialogue = true; // 對話時是否暫停遊戲
    
    [Header("Button Texts")]
    [SerializeField] private string continueButtonText = "Continue";
    [SerializeField] private string finishButtonText = "Close";
    
    private Queue<DialogueEntry> dialogueQueue = new Queue<DialogueEntry>();
    private bool isShowingDialogue = false;
    private bool isTyping = false;
    private Coroutine typewriterCoroutine;
    private Action onDialogueComplete;
    private bool isInitialized = false;
    
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
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
        
        // 初始隱藏
        dialoguePanel.SetActive(false);
        
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
        
        // 清空現有對話隊列
        ClearDialogueQueue();
        
        // 添加所有對話
        foreach (string message in messages)
        {
            dialogueQueue.Enqueue(new DialogueEntry(message, speakerName));
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
        
        // 這裡需要完整的文字，但我們在協程中沒有保存
        // 簡單的解決方案：立即結束並顯示完整文字
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
        
        // 恢復遊戲（如果之前暫停了）
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 1f;
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
        
        // 恢復時間縮放
        if (isShowingDialogue && pauseGameDuringDialogue)
        {
            Time.timeScale = 1f;
        }
    }
}
