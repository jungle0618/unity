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
    [SerializeField] private PlayerController playerController;
    
    private Color targetColor;
    private Color currentColor;
    
    private void Start()
    {
        // 獲取PlayerController
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        
        if (playerController == null)
        {
            Debug.LogError("PlayerHealthUI: 找不到PlayerController！");
            return;
        }
        
        // 訂閱血量變化事件
        playerController.OnHealthChanged += OnHealthChanged;
        playerController.OnPlayerDied += OnPlayerDied;
        
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
        if (playerController != null)
        {
            playerController.OnHealthChanged -= OnHealthChanged;
            playerController.OnPlayerDied -= OnPlayerDied;
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
        if (playerController == null) return;
        
        float healthPercentage = playerController.HealthPercentage;
        
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
                healthText.text = $"{playerController.CurrentHealth}/{playerController.MaxHealth} ({healthPercentage:P0})";
            }
            else
            {
                healthText.text = $"{playerController.CurrentHealth}/{playerController.MaxHealth}";
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
    public void SetPlayerController(PlayerController player)
    {
        // 取消訂閱舊的玩家
        if (playerController != null)
        {
            playerController.OnHealthChanged -= OnHealthChanged;
            playerController.OnPlayerDied -= OnPlayerDied;
        }
        
        playerController = player;
        
        // 訂閱新的玩家
        if (playerController != null)
        {
            playerController.OnHealthChanged += OnHealthChanged;
            playerController.OnPlayerDied += OnPlayerDied;
            UpdateHealthDisplay();
        }
    }
}
