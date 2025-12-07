using UnityEngine;

/// <summary>
/// 危險等級UI管理器
/// 負責管理危險等級顯示相關的UI
/// </summary>
public class DangerUIManager : MonoBehaviour
{
    [Header("Danger UI Reference")]
    [SerializeField] private DangerousUI dangerousUI;
    
    private DangerousManager dangerousManager;
    private bool isInitialized = false;
    
    /// <summary>
    /// 初始化危險等級UI
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 嘗試獲取 DangerousManager（可能還沒初始化，需要重試）
        TryInitialize();
    }
    
    private void Update()
    {
        // 如果還沒初始化，持續嘗試獲取 DangerousManager
        if (!isInitialized)
        {
            TryInitialize();
        }
    }
    
    /// <summary>
    /// 嘗試初始化（獲取 DangerousManager）
    /// </summary>
    private void TryInitialize()
    {
        if (isInitialized) return;
        
        // 獲取 DangerousManager
        dangerousManager = DangerousManager.Instance;
        
        if (dangerousManager == null)
        {
            // DangerousManager 可能還沒初始化，稍後再試
            return;
        }
        
        // 訂閱事件
        dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelChanged;
        
        // 設定危險等級UI
        if (dangerousUI != null)
        {
            // DangerousUI會自動獲取DangerousManager.Instance，不需要額外設定
            isInitialized = true;
            Debug.Log("DangerUIManager: 危險等級UI已初始化，已找到 DangerousManager");
        }
        else
        {
            Debug.LogWarning("DangerUIManager: DangerousUI 未設定");
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged -= OnDangerLevelChanged;
        }
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        
        if (dangerousUI != null)
        {
            dangerousUI.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 處理危險等級變化事件
    /// </summary>
    private void OnDangerLevelChanged(DangerousManager.DangerLevel level)
    {
        // 可以在這裡添加危險等級變化時的UI效果
        if (dangerousManager != null)
        {
            Debug.Log($"DangerUIManager: 危險等級變化 - {dangerousManager.GetDangerLevelDescription(level)}");
        }
    }
}




