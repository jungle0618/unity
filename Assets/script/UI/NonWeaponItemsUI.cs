using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 非武器物品顯示UI - 顯示所有非武器物品（如鑰匙）
/// 所有物品都會顯示在螢幕上
/// </summary>
public class NonWeaponItemsUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject itemIconPrefab;  // 物品圖示預製體
    [SerializeField] private Transform itemsContainer;   // 物品容器
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("Layout Settings")]
    [SerializeField] private float itemSpacing = 10f;    // 物品間距
    [SerializeField] private Vector2 itemSize = new Vector2(50, 50); // 物品圖示大小
    
    private ItemHolder itemHolder;
    private List<NonWeaponItemIconUI> itemIcons = new List<NonWeaponItemIconUI>();
    
    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize()
    {
        //Debug.Log("[NonWeaponItemsUI] Initialize called");
        
        // 檢查必要組件
        if (itemIconPrefab == null)
        {
            Debug.LogError("[NonWeaponItemsUI] itemIconPrefab is not assigned! Please assign it in the Inspector.");
        }
        
        if (itemsContainer == null)
        {
            Debug.LogError("[NonWeaponItemsUI] itemsContainer is not assigned! Please assign it in the Inspector.");
        }
        
        // 尋找玩家的 ItemHolder
        if (autoFindPlayer)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                itemHolder = player.GetComponent<ItemHolder>();
                //Debug.Log($"[NonWeaponItemsUI] Found player ItemHolder: {itemHolder != null}");
            }
            else
            {
                Debug.LogWarning("[NonWeaponItemsUI] Could not find Player in scene!");
            }
        }
        
        // 訂閱事件
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged += OnItemChanged;
            //Debug.Log("[NonWeaponItemsUI] Subscribed to ItemHolder events");
        }
        else
        {
            Debug.LogWarning("[NonWeaponItemsUI] ItemHolder is null, cannot subscribe to events");
        }
        
        // 如果容器有 HorizontalLayoutGroup，套用間距
        var hlg = itemsContainer ? itemsContainer.GetComponent<HorizontalLayoutGroup>() : null;
        if (hlg != null)
        {
            hlg.spacing = itemSpacing;
        }
        
        // 刷新顯示
        RefreshItemsDisplay();
    }
    
    private void OnDestroy()
    {
        // 取消訂閱
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged -= OnItemChanged;
        }
    }
    
    /// <summary>
    /// 設定 ItemHolder
    /// </summary>
    public void SetItemHolder(ItemHolder holder)
    {
        // 取消舊的訂閱
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged -= OnItemChanged;
        }
        
        itemHolder = holder;
        
        // 訂閱新的事件
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged += OnItemChanged;
            RefreshItemsDisplay();
        }
    }
    
    /// <summary>
    /// 刷新物品顯示
    /// </summary>
    private void RefreshItemsDisplay()
    {
        if (itemHolder == null || itemsContainer == null)
        {
            ClearAllItems();
            return;
        }
        
        // 獲取所有非武器物品
        var allItems = itemHolder.GetAllItems();
        var nonWeaponItems = allItems.Where(item => !(item is Weapon)).ToList();
        
        //Debug.Log($"[NonWeaponItemsUI] RefreshItemsDisplay: {nonWeaponItems.Count} non-weapon items, {itemIcons.Count} icons");
        
        // 先移除多餘的圖示（從後往前）
        while (itemIcons.Count > nonWeaponItems.Count)
        {
            RemoveLastItemIcon();
        }
        
        // 再創建不足的圖示
        while (itemIcons.Count < nonWeaponItems.Count)
        {
            CreateItemIcon();
        }
        
        // 更新每個圖示（確保圖示仍然存在）
        for (int i = 0; i < nonWeaponItems.Count; i++)
        {
            if (i < itemIcons.Count && itemIcons[i] != null)
            {
                itemIcons[i].SetItem(nonWeaponItems[i]);
                itemIcons[i].SetVisible(true);
            }
        }
        
        // 強制重新建置版面，避免第一次不顯示
        var containerRT = itemsContainer as RectTransform;
        if (containerRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRT);
        }
    }
    
    /// <summary>
    /// 創建新的物品圖示
    /// </summary>
    private void CreateItemIcon()
    {
        if (itemIconPrefab == null || itemsContainer == null)
            return;
        
        GameObject iconObj = Instantiate(itemIconPrefab, itemsContainer);
        
        // 如果 prefab 上誤帶了 Canvas，會造成嵌套 Canvas 與佈局異常，提出警告
        var nestedCanvas = iconObj.GetComponentInChildren<Canvas>();
        if (nestedCanvas != null)
        {
            Debug.LogWarning("[NonWeaponItemsUI] Detected a Canvas inside ItemIconPrefab. Please REMOVE Canvas from the prefab. Only RectTransform + Image/Text are allowed.");
        }
        
        // 確保 RectTransform 尺寸與縮放正確
        var rt = iconObj.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = iconObj.AddComponent<RectTransform>();
        }
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = itemSize;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        
        // 透過 LayoutElement 指定偏好尺寸，方便 HorizontalLayoutGroup 正確排版
        var le = iconObj.GetComponent<LayoutElement>();
        if (le == null) le = iconObj.AddComponent<LayoutElement>();
        le.preferredWidth = itemSize.x;
        le.preferredHeight = itemSize.y;
        le.minWidth = itemSize.x;
        le.minHeight = itemSize.y;
        
        NonWeaponItemIconUI icon = iconObj.GetComponent<NonWeaponItemIconUI>();
        if (icon == null)
        {
            icon = iconObj.AddComponent<NonWeaponItemIconUI>();
        }
        
        // 將尺寸同步給圖示（若提供對應 API）
        icon.Initialize();
        icon.SetSize(itemSize);
        
        itemIcons.Add(icon);
    }
    
    /// <summary>
    /// 移除最後一個物品圖示
    /// </summary>
    private void RemoveLastItemIcon()
    {
        if (itemIcons.Count == 0) return;
        
        int lastIndex = itemIcons.Count - 1;
        NonWeaponItemIconUI lastIcon = itemIcons[lastIndex];
        
        // 先從列表中移除
        itemIcons.RemoveAt(lastIndex);
        
        // 再銷毀 GameObject（避免在銷毀後還訪問）
        if (lastIcon != null && lastIcon.gameObject != null)
        {
            Destroy(lastIcon.gameObject);
            //Debug.Log($"[NonWeaponItemsUI] Removed item icon at index {lastIndex}");
        }
    }
    
    /// <summary>
    /// 清空所有物品
    /// </summary>
    private void ClearAllItems()
    {
        foreach (var icon in itemIcons)
        {
            if (icon != null && icon.gameObject != null)
            {
                Destroy(icon.gameObject);
            }
        }
        
        itemIcons.Clear();
    }
    
    /// <summary>
    /// 處理物品變更事件
    /// </summary>
    private void OnItemChanged(Item item)
    {
        RefreshItemsDisplay();
    }
}

