using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 通知UI管理器
/// 負責顯示臨時通知訊息，例如：門被鎖住、沒有武器、被敵人發現等
/// </summary>
public class NotificationUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;      // 通知面板
    [SerializeField] private TextMeshProUGUI notificationText;  // 通知文字
    [SerializeField] private CanvasGroup canvasGroup;           // 用於淡入淡出效果
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;       // 淡入時間
    [SerializeField] private float displayDuration = 2.0f;      // 顯示時間
    [SerializeField] private float fadeOutDuration = 0.5f;      // 淡出時間
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Movement Settings")]
    [SerializeField] private bool enableMovement = true;         // 是否啟用移動效果
    [SerializeField] private float moveDistance = 50f;           // 移動距離（像素）
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Coroutine currentNotificationCoroutine;
    private bool isInitialized = false;
    
    /// <summary>
    /// 初始化通知UI
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 驗證組件
        if (notificationPanel == null)
        {
            Debug.LogError("NotificationUIManager: Notification panel is not assigned!");
            return;
        }
        
        if (notificationText == null)
        {
            Debug.LogError("NotificationUIManager: Notification text is not assigned!");
            return;
        }
        
        // 獲取 CanvasGroup（如果未設定，自動添加）
        if (canvasGroup == null)
        {
            canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // 獲取 RectTransform
        rectTransform = notificationPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
        
        // 初始隱藏
        notificationPanel.SetActive(false);
        canvasGroup.alpha = 0f;
        
        isInitialized = true;
        Debug.Log("NotificationUIManager: Initialized.");
    }
    
    /// <summary>
    /// 顯示臨時通知
    /// </summary>
    /// <param name="message">通知訊息</param>
    /// <param name="duration">顯示持續時間（-1 使用預設值）</param>
    public void ShowNotification(string message, float duration = -1f)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("NotificationUIManager: Not initialized yet!");
            return;
        }
        
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("NotificationUIManager: Message is empty!");
            return;
        }
        
        // 如果有正在顯示的通知，先停止
        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
        }
        
        // 使用預設時間或自訂時間
        float actualDuration = duration > 0 ? duration : displayDuration;
        
        // 開始新的通知協程
        currentNotificationCoroutine = StartCoroutine(ShowNotificationCoroutine(message, actualDuration));
    }
    
    /// <summary>
    /// 顯示通知的協程
    /// </summary>
    private IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        // 設定文字
        notificationText.text = message;
        
        // 重置位置
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
        
        // 顯示面板
        notificationPanel.SetActive(true);
        
        // === 淡入階段 ===
        float elapsed = 0f;
        Vector2 startPos = originalPosition;
        Vector2 targetPos = originalPosition + (enableMovement ? new Vector2(0, moveDistance) : Vector2.zero);
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            
            // 淡入
            canvasGroup.alpha = fadeInCurve.Evaluate(t);
            
            // 移動
            if (enableMovement && rectTransform != null)
            {
                float moveT = moveCurve.Evaluate(t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, moveT);
            }
            
            yield return null;
        }
        
        // 確保完全顯示
        canvasGroup.alpha = 1f;
        if (enableMovement && rectTransform != null)
        {
            rectTransform.anchoredPosition = targetPos;
        }
        
        // === 顯示階段 ===
        yield return new WaitForSeconds(duration);
        
        // === 淡出階段 ===
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            
            // 淡出
            canvasGroup.alpha = fadeOutCurve.Evaluate(t);
            
            yield return null;
        }
        
        // 確保完全隱藏
        canvasGroup.alpha = 0f;
        notificationPanel.SetActive(false);
        
        // 重置位置
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
        
        currentNotificationCoroutine = null;
    }
    
    /// <summary>
    /// 立即隱藏通知
    /// </summary>
    public void HideNotification()
    {
        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
            currentNotificationCoroutine = null;
        }
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
    
    /// <summary>
    /// 檢查是否正在顯示通知
    /// </summary>
    public bool IsShowingNotification()
    {
        return currentNotificationCoroutine != null;
    }
    
    private void OnDestroy()
    {
        // 清理協程
        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
        }
    }
}
