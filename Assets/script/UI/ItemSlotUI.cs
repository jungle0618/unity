using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 物品格子UI（類似 Minecraft 的物品欄格子）
/// 簡化版：只顯示物品圖示、選中高亮、武器耐久度
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;           // 物品圖示
    [SerializeField] private Image background;         // 背景
    [SerializeField] private Image selectedBorder;     // 選中框
    [SerializeField] private Image durabilityBar;      // 耐久度條（填充型）
    [SerializeField] private GameObject durabilityPanel; // 耐久度面板
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color emptyIconColor = new Color(1, 1, 1, 0.2f);
    
    [Header("Durability Colors")]
    [SerializeField] private Color durabilityHighColor = Color.green;
    [SerializeField] private Color durabilityMediumColor = Color.yellow;
    [SerializeField] private Color durabilityLowColor = Color.red;
    
    private int slotIndex;
    private bool isSelected = false;
    private bool isEmpty = true;
    
    /// <summary>
    /// 初始化格子
    /// </summary>
    public void Initialize(int index)
    {
        slotIndex = index;
        SetEmpty();
        SetSelected(false);
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
        
        isEmpty = false;
        
        // 顯示物品圖示
        if (itemIcon != null)
        {
            if (item.ItemIcon != null)
            {
                itemIcon.sprite = item.ItemIcon;
                itemIcon.color = Color.white;
                itemIcon.enabled = true;
            }
            else
            {
                // 如果沒有圖示，顯示預設
                itemIcon.sprite = null;
                itemIcon.color = emptyIconColor;
                itemIcon.enabled = false;
            }
        }
        
        // 檢查是否是武器
        if (item is Weapon weapon)
        {
            UpdateWeaponDurability(weapon.CurrentDurability, weapon.MaxDurability);
        }
        else
        {
            // 非武器不顯示耐久度
            HideDurability();
        }
    }
    
    /// <summary>
    /// 更新武器耐久度顯示
    /// </summary>
    public void UpdateWeaponDurability(int current, int max)
    {
        if (durabilityPanel != null)
            durabilityPanel.SetActive(true);
            
        if (durabilityBar != null)
        {
            float percentage = max > 0 ? (float)current / max : 0f;
            durabilityBar.fillAmount = percentage;
            
            // 根據耐久度百分比改變顏色
            if (percentage > 0.5f)
                durabilityBar.color = durabilityHighColor;
            else if (percentage > 0.25f)
                durabilityBar.color = durabilityMediumColor;
            else
                durabilityBar.color = durabilityLowColor;
        }
    }
    
    /// <summary>
    /// 隱藏耐久度顯示
    /// </summary>
    public void HideDurability()
    {
        if (durabilityPanel != null)
            durabilityPanel.SetActive(false);
    }
    
    /// <summary>
    /// 設定為選中狀態
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 顯示/隱藏選中框
        if (selectedBorder != null)
        {
            selectedBorder.enabled = selected;
        }
        
        // 改變背景顏色（可選）
        if (background != null)
        {
            background.color = selected ? selectedColor : normalColor;
        }
    }
    
    /// <summary>
    /// 設定為空格子
    /// </summary>
    private void SetEmpty()
    {
        isEmpty = true;
        
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = emptyIconColor;
            itemIcon.enabled = false;
        }
        
        HideDurability();
    }
    
    public bool IsEmpty => isEmpty;
    public int SlotIndex => slotIndex;
    public bool IsSelected => isSelected;
}

