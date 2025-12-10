using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 玩家血量UI顯示器
/// 顯示玩家的血量條和相關信息
/// 使用兩個矩形（背景和前景）來實現血量條，統一邏輯
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform healthBarBorderRect;     // 血量條邊框矩形
    [SerializeField] private RectTransform healthBarBackgroundRect;  // 血量條背景矩形
    [SerializeField] private RectTransform healthBarForegroundRect;  // 血量條前景矩形
    [SerializeField] private RectTransform healthBarIconRect;        // 血量條圖標矩形
    [SerializeField] private TextMeshProUGUI healthText;            // 血量文字（可選）
    
    [Header("UI Settings")]
    [SerializeField] private bool showHealthText = true;
    
    [Header("Bar Settings")]
    [SerializeField] private float borderWidth = 2f; // 邊框寬度
    [SerializeField] private Color borderColor = Color.black; // 邊框顏色（黑色）
    [SerializeField] private Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 背景顏色（灰色）
    [SerializeField] private Color foregroundColor = Color.green; // 前景顏色（固定，不會變化）
    
    [Header("Icon Settings")]
    [SerializeField] private Sprite healthIcon; // 血量圖標
    [SerializeField] private Vector2 iconSize = new Vector2(40f, 40f); // 圖標尺寸
    [SerializeField] private float iconOverlap = 10f; // 圖標覆蓋血條的距離（從左邊）
    
    [Header("Target Player")]
    [SerializeField] private Player player;
    
    [Header("Auto Find Settings")]
    [SerializeField] private bool autoFindPlayer = true; // 是否自動查找 Player
    [SerializeField] private bool useEntityManager = true; // 是否使用 EntityManager 獲取 Player
    
    private float barWidth; // 血條寬度（用於計算前景條寬度）
    private float barHeight; // 血條高度
    private bool isInitialized = false;
    private EntityManager entityManager;
    
    private void Awake()
    {
        // 嘗試獲取 EntityManager 引用
        if (useEntityManager)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
        }
    }
    
    private void Start()
    {
        // 如果已經通過 SetPlayer() 設定了 Player，直接初始化
        if (player != null)
        {
            InitializePlayer();
        }
        // 否則，嘗試獲取 Player
        else if (autoFindPlayer)
        {
            TryFindPlayer();
        }
    }
    
    private void Update()
    {
        // 如果還沒初始化，持續嘗試獲取 Player
        if (!isInitialized && autoFindPlayer && player == null)
        {
            TryFindPlayer();
        }
    }
    
    /// <summary>
    /// 嘗試查找 Player
    /// </summary>
    private void TryFindPlayer()
    {
        if (isInitialized || player != null) return;
        
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
        
        // 如果 EntityManager 不可用或 Player 為 null，直接查找
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }
        
        // 如果找到 Player，初始化
        if (player != null)
        {
            InitializePlayer();
        }
    }
    
    /// <summary>
    /// 處理 Player 準備就緒事件
    /// </summary>
    private void HandlePlayerReady()
    {
        if (isInitialized || player != null) return;
        
        if (entityManager != null)
        {
            player = entityManager.Player;
            if (player != null)
            {
                InitializePlayer();
                
                // 取消訂閱（只需要一次）
                entityManager.OnPlayerReady -= HandlePlayerReady;
            }
        }
    }
    
    /// <summary>
    /// 初始化玩家相關的事件訂閱和顯示
    /// </summary>
    private void InitializePlayer()
    {
        if (player == null) return;
        
        // 初始化血量條矩形
        InitializeHealthBar();
        
        // 取消舊的訂閱（如果有）
        player.OnHealthChanged -= OnHealthChanged;
        player.OnPlayerDied -= OnPlayerDied;
        
        // 訂閱血量變化事件
        player.OnHealthChanged += OnHealthChanged;
        player.OnPlayerDied += OnPlayerDied;
        
        // 初始化顯示
        UpdateHealthDisplay();
        
        isInitialized = true;
        Debug.Log($"PlayerHealthUI: 已成功找到並初始化 Player: {player.name}");
    }
    
    /// <summary>
    /// 初始化血量條矩形
    /// </summary>
    private void InitializeHealthBar()
    {
        // 如果沒有手動設定，嘗試自動創建或查找
        if (healthBarBackgroundRect == null || healthBarForegroundRect == null)
        {
            Debug.LogWarning("PlayerHealthUI: 血量條矩形未設定，請在 Inspector 中設定 healthBarBackgroundRect 和 healthBarForegroundRect");
            return;
        }
        
        // 獲取父物件的 RectTransform（用於對齊）
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null)
        {
            Debug.LogWarning("PlayerHealthUI: 父物件沒有 RectTransform 組件");
            return;
        }
        
        // 獲取父物件的尺寸（預設使用 parent object 的長寬）
        barWidth = parentRect.rect.width;
        barHeight = parentRect.rect.height;
        
        if (barWidth <= 0 || barHeight <= 0)
        {
            Debug.LogWarning("PlayerHealthUI: 父物件尺寸無效，請確保父物件有正確的尺寸");
            return;
        }
        
        // 設置邊框矩形 - 最外層，比背景稍大
        if (healthBarBorderRect != null)
        {
            SetupBorderRect(healthBarBorderRect);
        }
        
        // 設置背景矩形 - 左右對齊到父物件
        if (healthBarBackgroundRect != null)
        {
            SetupBackgroundRect(healthBarBackgroundRect);
        }
        
        // 設置前景矩形 - 左邊對齊到父物件，寬度根據血量調整
        if (healthBarForegroundRect != null)
        {
            SetupForegroundRect(healthBarForegroundRect);
        }
        
        // 設置圖標矩形 - 在左邊，覆蓋部分血條
        if (healthBarIconRect != null)
        {
            SetupIconRect(healthBarIconRect);
        }
    }
    
    /// <summary>
    /// 設置邊框矩形
    /// </summary>
    private void SetupBorderRect(RectTransform rect)
    {
        // 設置錨點：左邊和右邊都對齊到父物件
        rect.anchorMin = new Vector2(0f, 0.5f); // 左邊，垂直居中
        rect.anchorMax = new Vector2(1f, 0.5f); // 右邊，垂直居中
        rect.pivot = new Vector2(0.5f, 0.5f);   // 中心點
        
        // 設置偏移，讓邊框比背景大 borderWidth
        rect.offsetMin = new Vector2(-borderWidth, -(barHeight * 0.5f + borderWidth)); // 左邊和底部
        rect.offsetMax = new Vector2(borderWidth, barHeight * 0.5f + borderWidth);  // 右邊和頂部
        
        // 確保邊框在最底層（作為最外層顯示）
        rect.SetAsFirstSibling();
        
        // 自動獲取或添加 Image 組件
        Image image = GetOrAddImage(rect);
        if (image != null)
        {
            image.color = borderColor;
        }
    }
    
    /// <summary>
    /// 設置背景矩形
    /// </summary>
    private void SetupBackgroundRect(RectTransform rect)
    {
        // 設置錨點：左邊和右邊都對齊到父物件
        rect.anchorMin = new Vector2(0f, 0.5f); // 左邊，垂直居中
        rect.anchorMax = new Vector2(1f, 0.5f); // 右邊，垂直居中
        rect.pivot = new Vector2(0.5f, 0.5f);   // 中心點
        
        // 設置偏移為 0，讓矩形完全填充父物件的寬度（在邊框內部）
        rect.offsetMin = new Vector2(0f, -barHeight * 0.5f); // 左邊和底部
        rect.offsetMax = new Vector2(0f, barHeight * 0.5f);  // 右邊和頂部
        
        // 自動獲取或添加 Image 組件
        Image image = GetOrAddImage(rect);
        if (image != null)
        {
            image.color = backgroundColor;
        }
    }
    
    /// <summary>
    /// 設置前景矩形
    /// </summary>
    private void SetupForegroundRect(RectTransform rect)
    {
        // 設置錨點：左邊對齊到父物件
        rect.anchorMin = new Vector2(0f, 0.5f); // 左邊，垂直居中
        rect.anchorMax = new Vector2(0f, 0.5f); // 左邊，垂直居中
        rect.pivot = new Vector2(0f, 0.5f);     // 左邊中心點
        
        // 初始設置為完整寬度（會在 UpdateHealthDisplay 中根據血量調整）
        rect.offsetMin = new Vector2(0f, -barHeight * 0.5f); // 左邊和底部
        rect.offsetMax = new Vector2(barWidth, barHeight * 0.5f); // 右邊和頂部
        
        // 自動獲取或添加 Image 組件
        Image image = GetOrAddImage(rect);
        if (image != null)
        {
            image.color = foregroundColor;
        }
    }
    
    /// <summary>
    /// 設置圖標矩形
    /// </summary>
    private void SetupIconRect(RectTransform rect)
    {
        // 設置錨點：左邊對齊到父物件
        rect.anchorMin = new Vector2(0f, 0.5f); // 左邊，垂直居中
        rect.anchorMax = new Vector2(0f, 0.5f); // 左邊，垂直居中
        rect.pivot = new Vector2(0.5f, 0.5f);   // 中心點
        
        // 設置位置：在左邊，部分覆蓋血條
        float iconX = -iconSize.x * 0.5f + iconOverlap; // 圖標中心點位置（負值表示在左邊，正值表示覆蓋）
        rect.offsetMin = new Vector2(iconX - iconSize.x * 0.5f, -iconSize.y * 0.5f); // 左邊和底部
        rect.offsetMax = new Vector2(iconX + iconSize.x * 0.5f, iconSize.y * 0.5f); // 右邊和頂部
        
        // 確保圖標在最上層
        rect.SetAsLastSibling();
        
        // 自動獲取或添加 Image 組件
        Image image = GetOrAddImage(rect);
        if (image != null)
        {
            image.sprite = healthIcon;
            image.preserveAspect = true; // 保持圖標比例
        }
    }
    
    /// <summary>
    /// 自動獲取或添加 Image 組件（如果 SerializeField 中有 rect 就不需要 image 的選項）
    /// </summary>
    private Image GetOrAddImage(RectTransform rect)
    {
        if (rect == null) return null;
        
        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }
        return image;
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (player != null)
        {
            player.OnHealthChanged -= OnHealthChanged;
            player.OnPlayerDied -= OnPlayerDied;
        }
        
        // 取消訂閱 EntityManager 事件
        if (entityManager != null)
        {
            entityManager.OnPlayerReady -= HandlePlayerReady;
        }
    }
    
    /// <summary>
    /// 處理血量變化事件
    /// </summary>
    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// 處理玩家死亡事件
    /// </summary>
    private void OnPlayerDied()
    {
        UpdateHealthDisplay();
        Debug.Log("玩家死亡，血條顯示為空");
    }
    
    /// <summary>
    /// 更新血量顯示
    /// </summary>
    private void UpdateHealthDisplay()
    {
        if (player == null) return;
        
        float healthPercentage = player.HealthPercentage;
        
        // 更新前景矩形寬度
        if (healthBarForegroundRect != null && barWidth > 0)
        {
            // 計算前景條的寬度（基於血量百分比）
            float foregroundWidth = barWidth * healthPercentage;
            
            // 確保寬度不小於一個最小值
            if (foregroundWidth < 0.01f)
            {
                foregroundWidth = 0.01f;
            }
            
            // 更新前景矩形的寬度（使用 offsetMax 來調整右邊位置，保持左邊對齊）
            healthBarForegroundRect.offsetMax = new Vector2(foregroundWidth, barHeight * 0.5f);
        }
        
        // 更新文字
        if (healthText != null && showHealthText)
        {
            healthText.text = $"{player.CurrentHealth}/{player.MaxHealth}";
        }
        
        // 更新前景顏色（固定顏色，不會變化）
        Image foregroundImage = GetOrAddImage(healthBarForegroundRect);
        if (foregroundImage != null)
        {
            foregroundImage.color = foregroundColor;
        }
    }
    
    /// <summary>
    /// 設定是否顯示血量文字
    /// </summary>
    public void SetShowHealthText(bool show)
    {
        showHealthText = show;
        if (healthText != null)
        {
            healthText.gameObject.SetActive(show);
        }
    }
    
    /// <summary>
    /// 設定目標玩家
    /// </summary>
    public void SetPlayer(Player targetPlayer)
    {
        Debug.Log("PlayerHealthUI: 設定目標玩家: " + (targetPlayer != null ? targetPlayer.name : "null"));
        
        // 取消訂閱舊的玩家
        if (player != null)
        {
            player.OnHealthChanged -= OnHealthChanged;
            player.OnPlayerDied -= OnPlayerDied;
        }
        
        player = targetPlayer;
        isInitialized = false; // 重置初始化狀態
        
        // 訂閱新的玩家並初始化
        if (player != null)
        {
            InitializePlayer();
        }
    }
    
    /// <summary>
    /// 檢查是否已初始化
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized && player != null;
    }
}
