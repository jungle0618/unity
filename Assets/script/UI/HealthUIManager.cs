using UnityEngine;

/// <summary>
/// 血條UI管理器
/// 負責管理玩家血量顯示相關的UI
/// </summary>
public class HealthUIManager : MonoBehaviour
{
    [Header("Health UI Reference")]
    [SerializeField] private PlayerHealthUI playerHealthUI;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private bool useEntityManager = true; // 是否使用 EntityManager 獲取 Player
    
    private Player player;
    private EntityManager entityManager;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // 嘗試獲取 EntityManager 引用
        if (useEntityManager)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
        }
    }
    
    /// <summary>
    /// 初始化血條UI
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        // 優先從 EntityManager 獲取 Player（如果可用）
        if (useEntityManager && entityManager != null)
        {
            player = entityManager.Player;
            
            // 如果 Player 還沒準備好，訂閱事件
            if (player == null && entityManager != null)
            {
                entityManager.OnPlayerReady += HandlePlayerReady;
                return; // 等待事件觸發
            }
        }
        else if (autoFindPlayer)
        {
            // 備用方案：直接查找
            player = FindFirstObjectByType<Player>();
        }
        
        // 設定血條UI
        SetupHealthUI();
    }
    
    /// <summary>
    /// 處理 Player 準備就緒事件
    /// </summary>
    private void HandlePlayerReady()
    {
        if (isInitialized) return;
        
        if (entityManager != null)
        {
            player = entityManager.Player;
            SetupHealthUI();
            
            // 取消訂閱（只需要一次）
            entityManager.OnPlayerReady -= HandlePlayerReady;
        }
    }
    
    /// <summary>
    /// 設置血條UI
    /// </summary>
    private void SetupHealthUI()
    {
        if (playerHealthUI != null && player != null)
        {
            playerHealthUI.SetPlayer(player);
            isInitialized = true;
            Debug.Log("HealthUIManager: 血條UI已初始化");
        }
        else if (playerHealthUI == null)
        {
            Debug.LogWarning("HealthUIManager: PlayerHealthUI 未設定");
        }
        else if (player == null)
        {
            Debug.LogWarning("HealthUIManager: 找不到 Player");
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件訂閱
        if (entityManager != null)
        {
            entityManager.OnPlayerReady -= HandlePlayerReady;
        }
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        
        if (playerHealthUI != null)
        {
            playerHealthUI.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 設定玩家（如果需要動態設定）
    /// </summary>
    public void SetPlayer(Player newPlayer)
    {
        player = newPlayer;
        
        if (playerHealthUI != null && player != null)
        {
            playerHealthUI.SetPlayer(player);
        }
    }
}



