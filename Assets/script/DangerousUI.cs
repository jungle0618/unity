using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 危險指數UI顯示器
/// 顯示當前危險指數和危險等級
/// </summary>
public class DangerousUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider dangerSlider;
    [SerializeField] private TextMeshProUGUI dangerText;
    [SerializeField] private TextMeshProUGUI dangerLevelText;
    [SerializeField] private Image dangerFillImage;
    [SerializeField] private Image dangerLevelIcon;
    
    [Header("UI Settings")]
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool showDangerLevel = true;
    [SerializeField] private bool animateColorChange = true;
    [SerializeField] private float colorChangeSpeed = 2f;
    
    [Header("Icon Settings")]
    [SerializeField] private Sprite safeIcon;
    [SerializeField] private Sprite lowDangerIcon;
    [SerializeField] private Sprite mediumDangerIcon;
    [SerializeField] private Sprite highDangerIcon;
    [SerializeField] private Sprite criticalIcon;
    
    private DangerousManager dangerousManager;
    private Color targetColor;
    private Color currentColor;
    
    private void Start()
    {
        // 獲取DangerousManager實例
        dangerousManager = DangerousManager.Instance;
        
        if (dangerousManager == null)
        {
            Debug.LogError("DangerousUI: 找不到DangerousManager實例！");
            return;
        }
        
        // 訂閱危險指數事件
        dangerousManager.OnDangerLevelChanged += OnDangerLevelChanged;
        dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelTypeChanged;
        
        // 初始化顯示
        UpdateDangerDisplay();
    }
    
    private void Update()
    {
        // 顏色動畫
        if (animateColorChange && dangerFillImage != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorChangeSpeed);
            dangerFillImage.color = currentColor;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelChanged -= OnDangerLevelChanged;
            dangerousManager.OnDangerLevelTypeChanged -= OnDangerLevelTypeChanged;
        }
    }
    
    /// <summary>
    /// 處理危險指數變化事件
    /// </summary>
    private void OnDangerLevelChanged(int currentDanger, int maxDanger)
    {
        UpdateDangerDisplay();
    }
    
    /// <summary>
    /// 處理危險等級變化事件
    /// </summary>
    private void OnDangerLevelTypeChanged(DangerousManager.DangerLevel level)
    {
        UpdateDangerLevelDisplay(level);
    }
    
    /// <summary>
    /// 更新危險指數顯示
    /// </summary>
    private void UpdateDangerDisplay()
    {
        if (dangerousManager == null) return;
        
        float dangerPercentage = dangerousManager.DangerPercentage;
        
        // 更新滑桿
        if (dangerSlider != null)
        {
            dangerSlider.value = dangerPercentage;
        }
        
        // 更新文字
        if (dangerText != null)
        {
            if (showPercentage)
            {
                dangerText.text = $"{dangerousManager.CurrentDangerLevel}/{dangerousManager.MaxDangerLevel} ({dangerPercentage:P0})";
            }
            else
            {
                dangerText.text = $"{dangerousManager.CurrentDangerLevel}/{dangerousManager.MaxDangerLevel}";
            }
        }
        
        // 更新顏色
        if (dangerFillImage != null)
        {
            targetColor = dangerousManager.GetDangerLevelColor(dangerousManager.CurrentDangerLevelType);
            if (!animateColorChange)
            {
                dangerFillImage.color = targetColor;
                currentColor = targetColor;
            }
        }
    }
    
    /// <summary>
    /// 更新危險等級顯示
    /// </summary>
    private void UpdateDangerLevelDisplay(DangerousManager.DangerLevel level)
    {
        if (!showDangerLevel) return;
        
        // 更新危險等級文字
        if (dangerLevelText != null)
        {
            dangerLevelText.text = dangerousManager.GetDangerLevelDescription(level);
            dangerLevelText.color = dangerousManager.GetDangerLevelColor(level);
        }
        
        // 更新危險等級圖標
        if (dangerLevelIcon != null)
        {
            Sprite iconSprite = GetDangerLevelIcon(level);
            if (iconSprite != null)
            {
                dangerLevelIcon.sprite = iconSprite;
            }
        }
    }
    
    /// <summary>
    /// 根據危險等級獲取對應的圖標
    /// </summary>
    private Sprite GetDangerLevelIcon(DangerousManager.DangerLevel level)
    {
        switch (level)
        {
            case DangerousManager.DangerLevel.Safe:
                return safeIcon;
            case DangerousManager.DangerLevel.Low:
                return lowDangerIcon;
            case DangerousManager.DangerLevel.Medium:
                return mediumDangerIcon;
            case DangerousManager.DangerLevel.High:
                return highDangerIcon;
            case DangerousManager.DangerLevel.Critical:
                return criticalIcon;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 設定是否顯示百分比
    /// </summary>
    public void SetShowPercentage(bool show)
    {
        showPercentage = show;
        UpdateDangerDisplay();
    }
    
    /// <summary>
    /// 設定是否顯示危險等級
    /// </summary>
    public void SetShowDangerLevel(bool show)
    {
        showDangerLevel = show;
        if (dangerLevelText != null)
        {
            dangerLevelText.gameObject.SetActive(show);
        }
        if (dangerLevelIcon != null)
        {
            dangerLevelIcon.gameObject.SetActive(show);
        }
    }
    
    /// <summary>
    /// 設定顏色動畫速度
    /// </summary>
    public void SetColorChangeSpeed(float speed)
    {
        colorChangeSpeed = Mathf.Max(0.1f, speed);
    }
}
