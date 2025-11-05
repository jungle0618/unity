using UnityEngine;

/// <summary>
/// 載入進度UI管理器
/// 負責管理載入場景中的進度顯示UI
/// 注意：此 UI 通常只在 LoadingScene 中使用，不在遊戲場景中
/// </summary>
public class LoadingProgressUIManager : MonoBehaviour
{
    [Header("Loading UI Reference")]
    [SerializeField] private LoadingProgressUI loadingProgressUI;
    [SerializeField] private bool autoFindLoadingUI = true;
    
    [Header("Settings")]
    [SerializeField] private bool initializeOnStart = true;
    
    private bool isVisible = true; // 載入UI通常預設顯示
    
    /// <summary>
    /// 初始化載入進度UI
    /// </summary>
    public void Initialize()
    {
        // 尋找 LoadingProgressUI
        if (autoFindLoadingUI && loadingProgressUI == null)
        {
            loadingProgressUI = FindFirstObjectByType<LoadingProgressUI>();
        }
        
        if (loadingProgressUI == null)
        {
            Debug.LogWarning("LoadingProgressUIManager: LoadingProgressUI 未設定或找不到");
            Debug.LogWarning("注意：此 UI 通常只在 LoadingScene 中使用");
            return;
        }
        
        // 初始顯示狀態
        SetVisible(isVisible);
        
        Debug.Log("LoadingProgressUIManager: 載入進度UI已初始化");
    }
    
    private void Start()
    {
        if (initializeOnStart)
        {
            Initialize();
        }
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
        
        if (loadingProgressUI != null)
        {
            loadingProgressUI.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 更新載入進度
    /// </summary>
    public void UpdateProgress(float progress)
    {
        if (loadingProgressUI != null)
        {
            loadingProgressUI.UpdateProgress(progress);
        }
    }
    
    /// <summary>
    /// 設定 LoadingProgressUI（如果需要動態設定）
    /// </summary>
    public void SetLoadingProgressUI(LoadingProgressUI loadingUI)
    {
        loadingProgressUI = loadingUI;
    }
    
    /// <summary>
    /// 獲取 LoadingProgressUI 引用
    /// </summary>
    public LoadingProgressUI GetLoadingProgressUI() => loadingProgressUI;
    
    /// <summary>
    /// 獲取當前進度（如果有）
    /// </summary>
    public float GetCurrentProgress()
    {
        // LoadingProgressUI 沒有公開的進度獲取方法
        // 如果需要，可以通過反射或修改 LoadingProgressUI 來實現
        return 0f;
    }
}

