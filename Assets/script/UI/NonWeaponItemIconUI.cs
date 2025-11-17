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
    [SerializeField] private Image itemIcon;              // 物品圖示
    [SerializeField] private Image background;            // 背景
    [SerializeField] private TextMeshProUGUI itemNameText; // 物品名稱（可選）
    [SerializeField] private TextMeshProUGUI countText;   // 數量文字（可選）
    [SerializeField] private GameObject tooltipPanel;     // 提示面板（可選）
    
    [Header("Visual Settings")]
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private bool showItemName = false;   // 是否顯示物品名稱
    [SerializeField] private bool showCount = false;      // 是否顯示數量
    
    private Item currentItem;
    private Vector2 targetSize = new Vector2(50, 50);

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize()
    {
        // 嘗試自動尋找子節點（容錯：Unity 可能因 UI 建立流程插入 Canvas）
        if (background == null)
        {
            background = GetComponentInChildren<Image>();
        }
        if (itemIcon == null)
        {
            // 優先尋找名為 Icon 的 Image
            var iconTF = transform.Find("Icon");
            if (iconTF != null) itemIcon = iconTF.GetComponent<Image>();
            if (itemIcon == null)
            {
                // 退而求其次：找第一個子孫 Image，但避免 background 本體
                var images = GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    itemIcon = images[images.Length - 1]; // 最後一個通常是子 Image
                }
            }
        }

        if (background != null)
        {
            background.color = backgroundColor;
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
        
        SetEmpty();
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

