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
    
    /// <summary>
    /// 初始化危險等級UI
    /// </summary>
    public void Initialize()
    {
        // 獲取 DangerousManager
        dangerousManager = DangerousManager.Instance;
        
        // 訂閱事件
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelChanged;
        }
        
        // 設定危險等級UI
        if (dangerousUI != null)
        {
            // DangerousUI會自動獲取DangerousManager.Instance，不需要額外設定
            Debug.Log("DangerUIManager: 危險等級UI已初始化");
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




