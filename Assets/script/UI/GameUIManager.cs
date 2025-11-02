using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 遊戲UI管理器
/// 管理遊戲中的所有UI元素，包括血條、危險等級和物品欄顯示
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private PlayerHealthUI playerHealthUI;
    [SerializeField] private DangerousUI dangerousUI;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject healthPanel;
    [SerializeField] private GameObject dangerPanel;
    [SerializeField] private GameObject hotbarPanel;
    
    [Header("Item Hotbar Settings")]
    [SerializeField] private GameObject itemSlotPrefab;      // 物品格子預製體
    [SerializeField] private Transform slotsContainer;       // 格子容器
    [SerializeField] private bool autoFindPlayer = true;     // 自動尋找玩家的 ItemHolder
    
    [Header("Settings")]
    [SerializeField] private bool showHealthUI = true;
    [SerializeField] private bool showDangerUI = true;
    [SerializeField] private bool showHotbarUI = true;
    
    private Player player;
    private DangerousManager dangerousManager;
    
    // 物品欄相關
    private ItemHolder itemHolder;
    private List<ItemSlotUI> itemSlots = new List<ItemSlotUI>();
    private int currentSelectedIndex = -1;
    
    private void Start()
    {
        // 獲取必要的組件
        player = FindFirstObjectByType<Player>();
        dangerousManager = DangerousManager.Instance;
        
        // 獲取 ItemHolder（如果需要）
        if (player != null && autoFindPlayer)
        {
            itemHolder = player.GetComponent<ItemHolder>();
        }
        
        // 初始化UI
        InitializeUI();
        
        // 訂閱事件
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelChanged;
        }
        
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged += OnItemChanged;
            itemHolder.OnWeaponDurabilityChanged += OnWeaponDurabilityChanged;
            itemHolder.OnWeaponBroken += OnWeaponBroken;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelTypeChanged -= OnDangerLevelChanged;
        }
        
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged -= OnItemChanged;
            itemHolder.OnWeaponDurabilityChanged -= OnWeaponDurabilityChanged;
            itemHolder.OnWeaponBroken -= OnWeaponBroken;
        }
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 設定血條UI
        if (playerHealthUI != null)
        {
            playerHealthUI.SetPlayer(player);
        }
        
        // 設定危險等級UI
        if (dangerousUI != null)
        {
            // DangerousUI會自動獲取DangerousManager.Instance，不需要額外設定
            Debug.Log("危險等級UI已初始化");
        }
        
        // 初始化物品欄UI
        InitializeItemHotbar();
        
        // 初始顯示狀態
        SetHealthUIVisible(showHealthUI);
        SetDangerUIVisible(showDangerUI);
        SetHotbarUIVisible(showHotbarUI);
    }
    
    /// <summary>
    /// 處理危險等級變化事件
    /// </summary>
    private void OnDangerLevelChanged(DangerousManager.DangerLevel level)
    {
        // 可以在這裡添加危險等級變化時的UI效果
        Debug.Log($"危險等級變化: {dangerousManager.GetDangerLevelDescription(level)}");
    }
    
    /// <summary>
    /// 設定血條UI可見性
    /// </summary>
    public void SetHealthUIVisible(bool visible)
    {
        if (healthPanel != null)
        {
            healthPanel.SetActive(visible);
        }
        
        if (playerHealthUI != null)
        {
            playerHealthUI.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 設定危險等級UI可見性
    /// </summary>
    public void SetDangerUIVisible(bool visible)
    {
        if (dangerPanel != null)
        {
            dangerPanel.SetActive(visible);
        }
        
        if (dangerousUI != null)
        {
            dangerousUI.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 切換血條UI顯示
    /// </summary>
    public void ToggleHealthUI()
    {
        showHealthUI = !showHealthUI;
        SetHealthUIVisible(showHealthUI);
    }
    
    /// <summary>
    /// 切換危險等級UI顯示
    /// </summary>
    public void ToggleDangerUI()
    {
        showDangerUI = !showDangerUI;
        SetDangerUIVisible(showDangerUI);
    }
    
    #region 物品欄UI管理
    
    /// <summary>
    /// 初始化物品快捷欄
    /// </summary>
    private void InitializeItemHotbar()
    {
        if (itemSlotPrefab == null || slotsContainer == null)
        {
            Debug.LogWarning("GameUIManager: 物品欄設定不完整，跳過初始化");
            return;
        }
        
        // 清空現有格子
        ClearAllItemSlots();
        
        // 動態創建格子（根據 ItemHolder 的物品數量）
        RefreshAllItemSlots();
    }
    
    /// <summary>
    /// 清空所有物品格子
    /// </summary>
    private void ClearAllItemSlots()
    {
        foreach (var slot in itemSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        itemSlots.Clear();
        currentSelectedIndex = -1;
    }
    
    /// <summary>
    /// 創建新的物品格子
    /// </summary>
    private ItemSlotUI CreateItemSlot(int index)
    {
        if (itemSlotPrefab == null || slotsContainer == null)
            return null;
            
        GameObject slotObj = Instantiate(itemSlotPrefab, slotsContainer);
        ItemSlotUI slot = slotObj.GetComponent<ItemSlotUI>();
        
        if (slot != null)
        {
            slot.Initialize(index);
        }
        else
        {
            Debug.LogError("GameUIManager: 格子預製體缺少 ItemSlotUI 組件！");
            Destroy(slotObj);
        }
        
        return slot;
    }
    
    /// <summary>
    /// 刷新所有物品格子的顯示（動態調整數量）
    /// </summary>
    private void RefreshAllItemSlots()
    {
        if (itemHolder == null) return;
        
        IReadOnlyList<Item> allItems = itemHolder.GetAllItems();
        int itemCount = allItems.Count;
        
        // 調整槽位數量以匹配物品數量
        while (itemSlots.Count < itemCount)
        {
            // 需要更多槽位，創建新的
            ItemSlotUI newSlot = CreateItemSlot(itemSlots.Count);
            if (newSlot != null)
            {
                itemSlots.Add(newSlot);
            }
        }
        
        while (itemSlots.Count > itemCount)
        {
            // 槽位太多，移除最後一個
            int lastIndex = itemSlots.Count - 1;
            if (itemSlots[lastIndex] != null)
            {
                Destroy(itemSlots[lastIndex].gameObject);
            }
            itemSlots.RemoveAt(lastIndex);
        }
        
        // 如果沒有物品，重置選中索引
        if (itemCount == 0)
        {
            currentSelectedIndex = -1;
            return;
        }
        
        // 更新每個格子的顯示
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i < itemCount)
            {
                itemSlots[i].SetItem(allItems[i]);
                
                // 設定選中狀態
                bool isSelected = (i == itemHolder.CurrentItemIndex);
                itemSlots[i].SetSelected(isSelected);
                
                if (isSelected)
                {
                    currentSelectedIndex = i;
                }
            }
        }
    }
    
    /// <summary>
    /// 處理物品切換事件
    /// </summary>
    private void OnItemChanged(Item item)
    {
        if (itemHolder == null) return;
        
        // 檢查槽位數量是否與物品數量一致
        IReadOnlyList<Item> allItems = itemHolder.GetAllItems();
        if (itemSlots.Count != allItems.Count)
        {
            // 數量不一致，需要完整刷新
            RefreshAllItemSlots();
            return;
        }
        
        if (itemSlots.Count == 0) return;
        
        int newIndex = itemHolder.CurrentItemIndex;
        
        // 取消舊的選中狀態
        if (currentSelectedIndex >= 0 && currentSelectedIndex < itemSlots.Count)
        {
            itemSlots[currentSelectedIndex].SetSelected(false);
        }
        
        // 設定新的選中狀態
        if (newIndex >= 0 && newIndex < itemSlots.Count)
        {
            itemSlots[newIndex].SetSelected(true);
            currentSelectedIndex = newIndex;
            
            // 如果是武器，更新耐久度顯示
            if (item is Weapon weapon)
            {
                itemSlots[newIndex].UpdateWeaponDurability(weapon.CurrentDurability, weapon.MaxDurability);
            }
        }
    }
    
    /// <summary>
    /// 處理武器耐久度變化事件
    /// </summary>
    private void OnWeaponDurabilityChanged(int current, int max)
    {
        // 更新所有格子中相同武器的耐久度顯示
        if (itemHolder == null || itemSlots.Count == 0) return;
        
        IReadOnlyList<Item> allItems = itemHolder.GetAllItems();
        
        // 找出當前武器並更新對應的槽位
        for (int i = 0; i < allItems.Count && i < itemSlots.Count; i++)
        {
            if (allItems[i] is Weapon weapon)
            {
                itemSlots[i].UpdateWeaponDurability(weapon.CurrentDurability, weapon.MaxDurability);
            }
        }
    }
    
    /// <summary>
    /// 處理武器損壞事件
    /// </summary>
    private void OnWeaponBroken()
    {
        Debug.Log("GameUIManager: 武器已損壞");
        
        // 注意：不需要手動刷新 UI
        // 因為 ItemHolder 在移除武器後會觸發 OnItemChanged
        // OnItemChanged 會檢測到槽位數量不一致並自動刷新
    }
    
    /// <summary>
    /// 設定物品欄UI可見性
    /// </summary>
    public void SetHotbarUIVisible(bool visible)
    {
        if (hotbarPanel != null)
        {
            hotbarPanel.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 切換物品欄UI顯示
    /// </summary>
    public void ToggleHotbarUI()
    {
        showHotbarUI = !showHotbarUI;
        SetHotbarUIVisible(showHotbarUI);
    }
    
    /// <summary>
    /// 設定 ItemHolder（如果需要動態設定）
    /// </summary>
    public void SetItemHolder(ItemHolder holder)
    {
        // 取消舊的訂閱
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged -= OnItemChanged;
            itemHolder.OnWeaponDurabilityChanged -= OnWeaponDurabilityChanged;
            itemHolder.OnWeaponBroken -= OnWeaponBroken;
        }
        
        itemHolder = holder;
        
        // 訂閱新的事件
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged += OnItemChanged;
            itemHolder.OnWeaponDurabilityChanged += OnWeaponDurabilityChanged;
            itemHolder.OnWeaponBroken += OnWeaponBroken;
            RefreshAllItemSlots();
        }
    }
    
    #endregion
}
