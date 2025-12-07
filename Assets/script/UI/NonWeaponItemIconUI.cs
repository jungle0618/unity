using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 非武器物品圖示UI - 單個非武器物品的顯示
/// 顯示物品圖示、名稱（可選）、數量（可選）
/// </summary>
public class NonWeaponItemIconUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform borderRect;     // 邊框矩形
    [SerializeField] private RectTransform backgroundRect; // 背景矩形
    [SerializeField] private Image itemIcon;              // 物品圖示
    [SerializeField] private TextMeshProUGUI itemNameText; // 物品名稱（可選）
    [SerializeField] private TextMeshProUGUI countText;   // 數量文字（可選）
    [SerializeField] private GameObject tooltipPanel;     // 提示面板（可選）
    
    [Header("Visual Settings")]
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private bool showItemName = false;   // 是否顯示物品名稱
    [SerializeField] private bool showCount = false;      // 是否顯示數量
    
    [Header("Border Settings")]
    [SerializeField] private float borderWidth = 2f; // 邊框寬度
    [SerializeField] private Color borderColor = Color.black; // 邊框顏色（黑色）
    
    private Item currentItem;
    private Vector2 targetSize = new Vector2(50, 50);

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize()
    {
        if (itemIcon == null)
        {
            // 優先尋找名為 Icon 的 Image
            var iconTF = transform.Find("Icon");
            if (iconTF != null) itemIcon = iconTF.GetComponent<Image>();
            if (itemIcon == null)
            {
                // 退而求其次：找第一個子孫 Image
                var images = GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    itemIcon = images[images.Length - 1]; // 最後一個通常是子 Image
                }
            }
        }

        if (itemNameText != null)
        {
            itemNameText.gameObject.SetActive(showItemName);
        }

        if (countText != null)
        {
            countText.gameObject.SetActive(showCount);
        }

        // 強制調整尺寸
        ApplySize(targetSize);
        
        // 設置背景
        SetupBackground();
        
        // 設置邊框
        SetupBorder();
        
        // 設置圖標層級（確保在背景前面）
        SetupIconLayer();
        
        SetEmpty();
    }
    
    /// <summary>
    /// 設置圖標層級（確保在背景前面）
    /// </summary>
    private void SetupIconLayer()
    {
        if (itemIcon == null) return;
        
        RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
        if (iconRect == null) return;
        
        // 確保圖標在背景之上
        if (backgroundRect != null)
        {
            int backgroundIndex = backgroundRect.GetSiblingIndex();
            iconRect.SetSiblingIndex(backgroundIndex + 1);
        }
        else
        {
            // 如果沒有背景，確保圖標在邊框之上
            if (borderRect != null)
            {
                int borderIndex = borderRect.GetSiblingIndex();
                iconRect.SetSiblingIndex(borderIndex + 1);
            }
        }
    }
    
    /// <summary>
    /// 設置背景矩形
    /// </summary>
    private void SetupBackground()
    {
        if (backgroundRect == null) return;
        
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null) return;
        
        // 設置錨點：完全填充父物件
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 設置偏移為 0，讓背景完全填充父物件
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        
        // 確保背景在邊框之上，但在其他元素之下
        if (borderRect != null)
        {
            int borderIndex = borderRect.GetSiblingIndex();
            backgroundRect.SetSiblingIndex(borderIndex + 1);
        }
        else
        {
            backgroundRect.SetAsFirstSibling();
        }
        
        // 自動獲取或添加 Image 組件
        Image backgroundImage = backgroundRect.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = backgroundRect.gameObject.AddComponent<Image>();
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }
    }
    
    /// <summary>
    /// 設置邊框
    /// </summary>
    private void SetupBorder()
    {
        if (borderRect == null) return;
        
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null) return;
        
        float width = parentRect.rect.width;
        float height = parentRect.rect.height;
        
        if (width <= 0 || height <= 0) return;
        
        // 設置錨點：完全填充父物件
        borderRect.anchorMin = new Vector2(0f, 0f);
        borderRect.anchorMax = new Vector2(1f, 1f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 設置偏移，讓邊框比背景大 borderWidth
        borderRect.offsetMin = new Vector2(-borderWidth, -borderWidth);
        borderRect.offsetMax = new Vector2(borderWidth, borderWidth);
        
        // 確保邊框在最底層
        borderRect.SetAsFirstSibling();
        
        // 自動獲取或添加 Image 組件
        Image borderImage = borderRect.GetComponent<Image>();
        if (borderImage == null)
        {
            borderImage = borderRect.gameObject.AddComponent<Image>();
        }
        
        if (borderImage != null)
        {
            borderImage.color = borderColor;
        }
    }

    /// <summary>
    /// 設定 prefab 實例希望顯示的大小（由父管理器呼叫）
    /// </summary>
    public void SetSize(Vector2 size)
    {
        targetSize = size;
        ApplySize(targetSize);
    }

    private void ApplySize(Vector2 size)
    {
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }
        if (itemIcon != null)
        {
            var iconRT = itemIcon.GetComponent<RectTransform>();
            if (iconRT != null)
            {
                // 讓圖示稍小於背景
                var inner = size * 0.85f;
                iconRT.sizeDelta = inner;
                iconRT.localScale = Vector3.one;
                iconRT.anchoredPosition = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// 設定物品
    /// </summary>
    public void SetItem(Item item)
    {
        if (item == null)
        {
            SetEmpty();
            return;
        }
        
        currentItem = item;
        
        // 顯示物品圖示
        if (itemIcon != null)
        {
            if (item.ItemIcon != null)
            {
                itemIcon.sprite = item.ItemIcon;
                itemIcon.color = iconColor;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.color = Color.gray;
                itemIcon.enabled = false;
            }
        }
        
        // 顯示物品名稱
        if (itemNameText != null && showItemName)
        {
            itemNameText.text = item.ItemName;
            itemNameText.gameObject.SetActive(true);
        }
        
        // 顯示數量（暫時設為1）
        if (countText != null && showCount)
        {
            countText.text = "1";
            countText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 設定為空
    /// </summary>
    public void SetEmpty()
    {
        currentItem = null;
        
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        
        if (itemNameText != null)
        {
            itemNameText.text = "";
            itemNameText.gameObject.SetActive(false);
        }
        
        if (countText != null)
        {
            countText.text = "";
            countText.gameObject.SetActive(false);
        }
        
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (this == null) return;
        var go = gameObject;
        if (go != null)
        {
            go.SetActive(visible);
        }
    }

    /// <summary>
    /// 獲取當前物品
    /// </summary>
    public Item GetItem()
    {
        return currentItem;
    }

    /// <summary>
    /// 顯示提示訊息（滑鼠懸停時）
    /// </summary>
    public void ShowTooltip(bool show)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(show && currentItem != null);
        }
    }
}

