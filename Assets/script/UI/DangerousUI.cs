using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 危險指數UI顯示器
/// 顯示當前危險指數和危險等級
/// 使用兩個矩形（背景和前景）來實現危險指數條，統一邏輯
/// </summary>
public class DangerousUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform dangerBarBorderRect;     // 危險指數條邊框矩形
    [SerializeField] private RectTransform dangerBarBackgroundRect;    // 危險指數條背景矩形
    [SerializeField] private RectTransform dangerBarForegroundRect;  // 危險指數條前景矩形
    [SerializeField] private RectTransform dangerBarIconRect;        // 危險指數條圖標矩形
    [SerializeField] private TextMeshProUGUI dangerText;
    [SerializeField] private TextMeshProUGUI dangerLevelText;
    [SerializeField] private Image dangerLevelIcon;
    
    [Header("UI Settings")]
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool showDangerLevel = true;
    
    [Header("Bar Settings")]
    [SerializeField] private float borderWidth = 2f; // 邊框寬度
    [SerializeField] private Color borderColor = Color.black; // 邊框顏色（黑色）
    [SerializeField] private Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 背景顏色（灰色）
    [SerializeField] private Color foregroundColor = Color.red; // 前景顏色（固定，不會變化）
    
    [Header("Bar Icon Settings")]
    [SerializeField] private Sprite dangerBarIcon; // 危險指數條圖標
    [SerializeField] private Vector2 iconSize = new Vector2(40f, 40f); // 圖標尺寸
    [SerializeField] private float iconOverlap = 10f; // 圖標覆蓋血條的距離（從左邊）
    
    [Header("Danger Level Icon Settings")]
    [SerializeField] private Sprite safeIcon;
    [SerializeField] private Sprite lowDangerIcon;
    [SerializeField] private Sprite mediumDangerIcon;
    [SerializeField] private Sprite highDangerIcon;
    [SerializeField] private Sprite criticalIcon;
    
    private DangerousManager dangerousManager;
    private float barWidth; // 危險指數條寬度（用於計算前景條寬度）
    private float barHeight; // 危險指數條高度
    
    private void Start()
    {
        // 獲取DangerousManager實例
        dangerousManager = DangerousManager.Instance;
        
        if (dangerousManager == null)
        {
            Debug.LogError("DangerousUI: 找不到DangerousManager實例！");
            return;
        }
        
        // 初始化危險指數條矩形
        InitializeDangerBar();
        
        // 訂閱危險指數事件
        dangerousManager.OnDangerLevelChanged += OnDangerLevelChanged;
        dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelTypeChanged;
        
        // 初始化顯示
        UpdateDangerDisplay();
    }
    
    /// <summary>
    /// 初始化危險指數條矩形
    /// </summary>
    private void InitializeDangerBar()
    {
        // 如果沒有手動設定，嘗試自動創建或查找
        if (dangerBarBackgroundRect == null || dangerBarForegroundRect == null)
        {
            Debug.LogWarning("DangerousUI: 危險指數條矩形未設定，請在 Inspector 中設定 dangerBarBackgroundRect 和 dangerBarForegroundRect");
            return;
        }
        
        // 獲取父物件的 RectTransform（用於對齊）
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null)
        {
            Debug.LogWarning("DangerousUI: 父物件沒有 RectTransform 組件");
            return;
        }
        
        // 獲取父物件的尺寸（預設使用 parent object 的長寬）
        barWidth = parentRect.rect.width;
        barHeight = parentRect.rect.height;
        
        if (barWidth <= 0 || barHeight <= 0)
        {
            Debug.LogWarning("DangerousUI: 父物件尺寸無效，請確保父物件有正確的尺寸");
            return;
        }
        
        // 設置邊框矩形 - 最外層，比背景稍大
        if (dangerBarBorderRect != null)
        {
            SetupBorderRect(dangerBarBorderRect);
        }
        
        // 設置背景矩形 - 左右對齊到父物件
        if (dangerBarBackgroundRect != null)
        {
            SetupBackgroundRect(dangerBarBackgroundRect);
        }
        
        // 設置前景矩形 - 左邊對齊到父物件，寬度根據危險指數調整
        if (dangerBarForegroundRect != null)
        {
            SetupForegroundRect(dangerBarForegroundRect);
        }
        
        // 設置圖標矩形 - 在左邊，覆蓋部分血條
        if (dangerBarIconRect != null)
        {
            SetupIconRect(dangerBarIconRect);
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
        
        // 初始設置為完整寬度（會在 UpdateDangerDisplay 中根據危險指數調整）
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
            image.sprite = dangerBarIcon;
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
        
        // 更新前景矩形寬度
        if (dangerBarForegroundRect != null && barWidth > 0)
        {
            // 計算前景條的寬度（基於危險指數百分比）
            float foregroundWidth = barWidth * dangerPercentage;
            
            // 確保寬度不小於一個最小值
            if (foregroundWidth < 0.01f)
            {
                foregroundWidth = 0.01f;
            }
            
            // 更新前景矩形的寬度（使用 offsetMax 來調整右邊位置，保持左邊對齊）
            dangerBarForegroundRect.offsetMax = new Vector2(foregroundWidth, barHeight * 0.5f);
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
        
        // 更新前景顏色（固定顏色，不會變化）
        Image foregroundImage = GetOrAddImage(dangerBarForegroundRect);
        if (foregroundImage != null)
        {
            foregroundImage.color = foregroundColor;
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
}
