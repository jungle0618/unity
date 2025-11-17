using UnityEngine;

/// <summary>
/// 對話系統範例腳本
/// 展示如何使用 NotificationUIManager 和 DialogueUIManager
/// 
/// 使用方法：
/// 1. 將此腳本附加到任何 GameObject
/// 2. 在遊戲中按下相應按鍵測試功能
/// </summary>
public class DialogueSystemExample : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private bool enableTestKeys = true;
    
    [Header("快捷鍵說明")]
    [Tooltip("按 N 鍵測試通知")]
    [SerializeField] private KeyCode notificationTestKey = KeyCode.N;
    
    [Tooltip("按 D 鍵測試簡單對話")]
    [SerializeField] private KeyCode simpleDialogueTestKey = KeyCode.D;
    
    [Tooltip("按 M 鍵測試多條對話")]
    [SerializeField] private KeyCode multiDialogueTestKey = KeyCode.M;
    
    [Tooltip("按 C 鍵測試對話回調")]
    [SerializeField] private KeyCode callbackDialogueTestKey = KeyCode.C;
    
    private GameUIManager gameUIManager;
    private NotificationUIManager notificationManager;
    private DialogueUIManager dialogueManager;
    
    private void Start()
    {
        // 獲取 UI 管理器
        gameUIManager = FindFirstObjectByType<GameUIManager>();
        
        if (gameUIManager != null)
        {
            notificationManager = gameUIManager.GetNotificationUIManager();
            dialogueManager = gameUIManager.GetDialogueUIManager();
        }
        else
        {
            Debug.LogError("DialogueSystemExample: 找不到 GameUIManager！");
        }
        
        if (enableTestKeys)
        {
            Debug.Log("=== 對話系統測試按鍵 ===");
            Debug.Log($"按 {notificationTestKey} 測試通知");
            Debug.Log($"按 {simpleDialogueTestKey} 測試簡單對話");
            Debug.Log($"按 {multiDialogueTestKey} 測試多條對話");
            Debug.Log($"按 {callbackDialogueTestKey} 測試對話回調");
            Debug.Log("========================");
        }
    }
    
    private void Update()
    {
        if (!enableTestKeys) return;
        
        // 測試通知
        if (Input.GetKeyDown(notificationTestKey))
        {
            TestNotification();
        }
        
        // 測試簡單對話
        if (Input.GetKeyDown(simpleDialogueTestKey))
        {
            TestSimpleDialogue();
        }
        
        // 測試多條對話
        if (Input.GetKeyDown(multiDialogueTestKey))
        {
            TestMultiDialogue();
        }
        
        // 測試對話回調
        if (Input.GetKeyDown(callbackDialogueTestKey))
        {
            TestDialogueWithCallback();
        }
    }
    
    #region 測試方法
    
    /// <summary>
    /// 測試通知系統
    /// </summary>
    private void TestNotification()
    {
        if (notificationManager == null)
        {
            Debug.LogWarning("NotificationUIManager 未初始化！");
            return;
        }
        
        // 顯示隨機通知
        string[] notifications = new string[]
        {
            "這是一條測試通知！",
            "門被鎖住了！",
            "你被敵人發現了！",
            "物品已拾取！",
            "任務已完成！"
        };
        
        string randomNotification = notifications[Random.Range(0, notifications.Length)];
        notificationManager.ShowNotification(randomNotification);
        
        Debug.Log($"顯示通知: {randomNotification}");
    }
    
    /// <summary>
    /// 測試簡單對話
    /// </summary>
    private void TestSimpleDialogue()
    {
        if (dialogueManager == null)
        {
            Debug.LogWarning("DialogueUIManager 未初始化！");
            return;
        }
        
        dialogueManager.ShowDialogue(
            "你好，歡迎來到這個世界！這是一條簡單的測試對話。",
            "測試NPC"
        );
        
        Debug.Log("顯示簡單對話");
    }
    
    /// <summary>
    /// 測試多條對話
    /// </summary>
    private void TestMultiDialogue()
    {
        if (dialogueManager == null)
        {
            Debug.LogWarning("DialogueUIManager 未初始化！");
            return;
        }
        
        string[] messages = new string[]
        {
            "歡迎來到我們的村莊。",
            "這裡最近出現了一些怪物。",
            "它們從森林深處來，非常危險。",
            "你能幫我們調查一下嗎？",
            "作為回報，我會給你一些獎勵。"
        };
        
        dialogueManager.ShowDialogues(messages, "村長");
        
        Debug.Log("顯示多條對話");
    }
    
    /// <summary>
    /// 測試帶回調的對話
    /// </summary>
    private void TestDialogueWithCallback()
    {
        if (dialogueManager == null)
        {
            Debug.LogWarning("DialogueUIManager 未初始化！");
            return;
        }
        
        string[] messages = new string[]
        {
            "這是第一條訊息。",
            "這是第二條訊息。",
            "當你按下最後一個繼續按鈕後...",
            "會觸發一個回調函數！"
        };
        
        dialogueManager.ShowDialogues(messages, "系統", OnDialogueComplete);
        
        Debug.Log("顯示帶回調的對話");
    }
    
    /// <summary>
    /// 對話完成回調
    /// </summary>
    private void OnDialogueComplete()
    {
        Debug.Log("=== 對話已完成！ ===");
        
        // 顯示完成通知
        if (notificationManager != null)
        {
            notificationManager.ShowNotification("對話已完成！", 2.0f);
        }
    }
    
    #endregion
    
    #region 公開API範例
    
    /// <summary>
    /// 顯示自訂通知（供外部呼叫）
    /// </summary>
    public void ShowCustomNotification(string message, float duration = 2.0f)
    {
        if (notificationManager != null)
        {
            notificationManager.ShowNotification(message, duration);
        }
    }
    
    /// <summary>
    /// 顯示NPC對話（供外部呼叫）
    /// </summary>
    public void ShowNPCDialogue(string[] messages, string npcName, System.Action onComplete = null)
    {
        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogues(messages, npcName, onComplete);
        }
    }
    
    /// <summary>
    /// 顯示不同說話者的對話（供外部呼叫）
    /// </summary>
    public void ShowConversation(DialogueUIManager.DialogueEntry[] dialogues, System.Action onComplete = null)
    {
        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogues(dialogues, onComplete);
        }
    }
    
    #endregion
}

