using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 玩家血量UI顯示器
/// 顯示玩家的血量條和相關信息
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image healthBackgroundImage;
    
    
    [Header("UI Settings")]
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool showHealthText = true;
    [SerializeField] private bool animateColorChange = true;
    [SerializeField] private float colorChangeSpeed = 2f;
    
    [Header("Color Settings")]
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float mediumHealthThreshold = 0.6f;
    
    [Header("Target Player")]
    [SerializeField] private Player player;
    
    private Color targetColor;
    private Color currentColor;
    
    private void Start()
    {
        // 如果已經通過 SetPlayer() 設定了 Player，直接初始化
        if (player != null)
        {
            InitializePlayer();
        }
        // 否則，等待 HealthUIManager 通過 SetPlayer() 設定
        // 這確保了正確的執行順序（EntityManager 先初始化 Player）
        else
        {
            // 備用方案：嘗試查找 Player（如果 HealthUIManager 沒有使用）
            player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                InitializePlayer();
            }
            else
            {
                Debug.LogWarning("PlayerHealthUI: 找不到Player，等待 SetPlayer() 被調用...");
            }
        }
    }
    
    /// <summary>
    /// 初始化玩家相關的事件訂閱和顯示
    /// </summary>
    private void InitializePlayer()
    {
        if (player == null) return;
        
        // 取消舊的訂閱（如果有）
        player.OnHealthChanged -= OnHealthChanged;
        player.OnPlayerDied -= OnPlayerDied;
        
        // 訂閱血量變化事件
        player.OnHealthChanged += OnHealthChanged;
        player.OnPlayerDied += OnPlayerDied;
        
        // 初始化顯示
        UpdateHealthDisplay();
    }
    
    private void Update()
    {
        // 顏色動畫
        if (animateColorChange && healthFillImage != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorChangeSpeed);
            healthFillImage.color = currentColor;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (player != null)
        {
            player.OnHealthChanged -= OnHealthChanged;
            player.OnPlayerDied -= OnPlayerDied;
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
        
        // 更新滑桿
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
        }
        
        // 更新文字
        if (healthText != null && showHealthText)
        {
            if (showPercentage)
            {
                healthText.text = $"{player.CurrentHealth}/{player.MaxHealth} ({healthPercentage:P0})";
            }
            else
            {
                healthText.text = $"{player.CurrentHealth}/{player.MaxHealth}";
            }
        }
        
        // 更新顏色
        if (healthFillImage != null)
        {
            targetColor = GetHealthColor(healthPercentage);
            if (!animateColorChange)
            {
                healthFillImage.color = targetColor;
                currentColor = targetColor;
            }
        }
    }
    
    /// <summary>
    /// 根據血量百分比獲取顏色
    /// </summary>
    private Color GetHealthColor(float healthPercentage)
    {
        if (healthPercentage <= lowHealthThreshold)
        {
            return lowHealthColor;
        }
        else if (healthPercentage <= mediumHealthThreshold)
        {
            return mediumHealthColor;
        }
        else
        {
            return highHealthColor;
        }
    }
    
    /// <summary>
    /// 設定是否顯示百分比
    /// </summary>
    public void SetShowPercentage(bool show)
    {
        showPercentage = show;
        UpdateHealthDisplay();
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
    /// 設定顏色動畫速度
    /// </summary>
    public void SetColorChangeSpeed(float speed)
    {
        colorChangeSpeed = Mathf.Max(0.1f, speed);
    }
    
    /// <summary>
    /// 設定目標玩家
    /// </summary>
    public void SetPlayer(Player targetPlayer)
    {
        // 取消訂閱舊的玩家
        if (player != null)
        {
            player.OnHealthChanged -= OnHealthChanged;
            player.OnPlayerDied -= OnPlayerDied;
        }
        
        player = targetPlayer;
        
        // 訂閱新的玩家並初始化
        if (player != null)
        {
            InitializePlayer();
        }
    }
}
