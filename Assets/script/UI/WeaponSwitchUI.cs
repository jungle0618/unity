using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 武器切換UI - 顯示兩個武器槽位和空手選項
/// 位於螢幕底部中間，顯示當前裝備的武器狀態
/// </summary>
public class WeaponSwitchUI : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private WeaponSlotUI weaponSlot1;  // 左：Knife 類型
    [SerializeField] private WeaponSlotUI weaponSlot2;  // 右：Gun 類型
    
    [Header("Settings")]
    [SerializeField] private bool autoFindPlayer = true;
    
    private ItemHolder itemHolder;
    
    // 類型鍵值（避免硬編碼字串分散）
    private const string KnifeTypeKey = "Knife";
    private const string GunTypeKey = "Gun";
    
    // 當前選中的類型：-1=空手, 0=Knife, 1=Gun
    private int currentTypeIndex = -1;
    private readonly string[] typeOrder = new[] { KnifeTypeKey, GunTypeKey }; // 可擴展
    
    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[WeaponSwitchUI] Initialize called");
        
        // 檢查槽位是否已設置
        if (weaponSlot1 == null)
        {
            Debug.LogError("[WeaponSwitchUI] weaponSlot1 is NOT assigned! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log("[WeaponSwitchUI] weaponSlot1 is assigned ✓");
        }
        
        if (weaponSlot2 == null)
        {
            Debug.LogError("[WeaponSwitchUI] weaponSlot2 is NOT assigned! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log("[WeaponSwitchUI] weaponSlot2 is assigned ✓");
        }
        
        // 尋找玩家的 ItemHolder
        if (autoFindPlayer)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                itemHolder = player.GetComponent<ItemHolder>();
                Debug.Log($"[WeaponSwitchUI] Found player ItemHolder: {itemHolder != null}");
            }
            else
            {
                Debug.LogWarning("[WeaponSwitchUI] Could not find Player in scene!");
            }
        }
        
        // 訂閱事件
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged += OnItemChanged;
            itemHolder.OnWeaponDurabilityChanged += OnWeaponDurabilityChanged;
            Debug.Log("[WeaponSwitchUI] Subscribed to ItemHolder events");
        }
        else
        {
            Debug.LogWarning("[WeaponSwitchUI] ItemHolder is null, cannot subscribe to events");
        }
        
        // 初始化槽位
        if (weaponSlot1 != null) weaponSlot1.Initialize(0);
        if (weaponSlot2 != null) weaponSlot2.Initialize(1);
        
        // 刷新顯示
        RefreshWeaponDisplay();
    }
    
    private void OnDestroy()
    {
        // 取消訂閱
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged -= OnItemChanged;
            itemHolder.OnWeaponDurabilityChanged -= OnWeaponDurabilityChanged;
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
            itemHolder.OnWeaponDurabilityChanged -= OnWeaponDurabilityChanged;
        }
        
        itemHolder = holder;
        
        // 訂閱新的事件
        if (itemHolder != null)
        {
            itemHolder.OnItemChanged += OnItemChanged;
            itemHolder.OnWeaponDurabilityChanged += OnWeaponDurabilityChanged;
            RefreshWeaponDisplay();
        }
    }
    
    /// <summary>
    /// 刷新武器顯示
    /// </summary>
    private void RefreshWeaponDisplay()
    {
        Debug.Log("[WeaponSwitchUI] RefreshWeaponDisplay called");
        
        if (itemHolder == null)
        {
            Debug.LogWarning("[WeaponSwitchUI] itemHolder is null, clearing all slots");
            ClearAllSlots();
            return;
        }
        
        // 獲取所有武器
        List<Weapon> weapons = itemHolder.GetItemsOfType<Weapon>();
        Debug.Log($"[WeaponSwitchUI] Found {weapons.Count} weapons in ItemHolder");
        
        // 更新武器引用
        Weapon knifeTop = null;
        Weapon gunTop = null;
        
        // 由 ItemHolder 收集武器清單，分類，並維持先入先用（最舊的優先）
        Dictionary<string, Queue<Weapon>> queues = BuildTypeQueues(out Dictionary<string, int> counts);
        
        // 取得各類型當前要顯示的武器（最舊一把）
        knifeTop = queues.ContainsKey(KnifeTypeKey) && queues[KnifeTypeKey].Count > 0 ? queues[KnifeTypeKey].Peek() : null;
        gunTop   = queues.ContainsKey(GunTypeKey)   && queues[GunTypeKey].Count   > 0 ? queues[GunTypeKey].Peek()   : null;
        
        // 檢查當前裝備的武器並同步 currentTypeIndex
        Item currentEquipped = itemHolder.CurrentItem;
        if (currentEquipped != null && currentEquipped is Weapon currentWeapon)
        {
            string currentType = GetWeaponTypeKey(currentWeapon);
            if (currentType == KnifeTypeKey)
            {
                currentTypeIndex = 0;
            }
            else if (currentType == GunTypeKey)
            {
                currentTypeIndex = 1;
            }
            else
            {
                currentTypeIndex = -1; // Unknown weapon type
            }
        }
        else if (itemHolder.IsEmptyHands())
        {
            currentTypeIndex = -1;
        }
        
        // 畫面顯示 & 數量徽章
        if (knifeTop != null)
        {
            weaponSlot1.SetWeapon(knifeTop);
            weaponSlot1.SetVisible(true);
            weaponSlot1.SetCount(counts[KnifeTypeKey]);
        }
        else
        {
            weaponSlot1.SetEmpty();
            weaponSlot1.SetVisible(false);
        }
        
        if (gunTop != null)
        {
            weaponSlot2.SetWeapon(gunTop);
            weaponSlot2.SetVisible(true);
            weaponSlot2.SetCount(counts[GunTypeKey]);
        }
        else
        {
            weaponSlot2.SetEmpty();
            weaponSlot2.SetVisible(false);
        }
        
        // 設定選中亮暗：由當前類型索引判斷
        weaponSlot1.SetSelected(currentTypeIndex == 0);
        weaponSlot2.SetSelected(currentTypeIndex == 1);
        
        Debug.Log($"[WeaponSwitchUI] currentTypeIndex={currentTypeIndex}, Knife selected={currentTypeIndex == 0}, Gun selected={currentTypeIndex == 1}");
    }
    
    // 判斷武器類型鍵（可擴展：未來可改為 ScriptableObject tag 或 enum）
    private string GetWeaponTypeKey(Weapon w)
    {
        if (w == null) return null;
        string n = w.ItemName?.ToLowerInvariant();
        if (n != null)
        {
            if (n.Contains("knife")) return KnifeTypeKey;
            if (n.Contains("gun")) return GunTypeKey;
        }
        // 後備：依類型名稱
        var t = w.GetType().Name.ToLowerInvariant();
        if (t.Contains("knife")) return KnifeTypeKey;
        if (t.Contains("gun")) return GunTypeKey;
        return null; // 非支援武器類型
    }
    
    // 由 ItemHolder 收集武器清單，分類，並維持先入先用（最舊的優先）
    private Dictionary<string, Queue<Weapon>> BuildTypeQueues(out Dictionary<string, int> counts)
    {
        counts = new Dictionary<string, int>();
        var qmap = new Dictionary<string, Queue<Weapon>>();
        var weapons = itemHolder != null ? itemHolder.GetItemsOfType<Weapon>() : new List<Weapon>();
        // 保持 ItemHolder 順序即為獲取時間順序（假設 AddItemFromPrefab 追加於尾端）
        foreach (var w in weapons)
        {
            var key = GetWeaponTypeKey(w);
            if (string.IsNullOrEmpty(key)) continue; // 跳過非 Knife/Gun
            if (!qmap.ContainsKey(key)) qmap[key] = new Queue<Weapon>();
            qmap[key].Enqueue(w);
        }
        // 統計數量
        foreach (var k in typeOrder)
        {
            counts[k] = qmap.ContainsKey(k) ? qmap[k].Count : 0;
        }
        return qmap;
    }
    
    private void ClearAllSlots()
    {
        if (weaponSlot1 != null)
        {
            weaponSlot1.SetEmpty();
            weaponSlot1.SetVisible(false);
        }
        
        if (weaponSlot2 != null)
        {
            weaponSlot2.SetEmpty();
            weaponSlot2.SetVisible(false);
        }
    }
    
    /// <summary>
    /// 處理物品變更事件
    /// </summary>
    private void OnItemChanged(Item item)
    {
        RefreshWeaponDisplay();
    }
    
    /// <summary>
    /// 處理武器耐久度變更
    /// </summary>
    private void OnWeaponDurabilityChanged(int current, int max)
    {
        // 由 RefreshWeaponDisplay 決定當前 slot 顯示、此處可做細化（暫以整體刷新處理）
        RefreshWeaponDisplay();
    }
    
    /// <summary>
    /// 切換到下一個武器（包含空手）
    /// 順序：武器1 → 武器2 → 空手 → 武器1
    /// </summary>
    public void SwitchToNextWeapon()
    {
        if (itemHolder == null)
        {
            currentTypeIndex = -1; // 空手
            RefreshWeaponDisplay();
            return;
        }
        
        var queues = BuildTypeQueues(out _);
        int safety = 0;
        do
        {
            currentTypeIndex++;
            if (currentTypeIndex > 1) currentTypeIndex = -1; // 回到空手
            
            if (currentTypeIndex == -1)
            {
                itemHolder.TryEquipEmptyHands();
                Debug.Log("[WeaponSwitchUI] Switched to Empty Hands");
                RefreshWeaponDisplay();
                return;
            }
            else if (currentTypeIndex == 0)
            {
                if (queues.ContainsKey(KnifeTypeKey) && queues[KnifeTypeKey].Count > 0)
                {
                    var w = queues[KnifeTypeKey].Peek();
                    SwitchToWeaponInstance(w);
                    RefreshWeaponDisplay();
                    return;
                }
            }
            else if (currentTypeIndex == 1)
            {
                if (queues.ContainsKey(GunTypeKey) && queues[GunTypeKey].Count > 0)
                {
                    var w = queues[GunTypeKey].Peek();
                    SwitchToWeaponInstance(w);
                    RefreshWeaponDisplay();
                    return;
                }
            }
            safety++;
        } while (safety < 4);
        
        currentTypeIndex = -1;
        itemHolder.TryEquipEmptyHands();
        RefreshWeaponDisplay();
    }

    /// <summary>
    /// 切換到上一個武器（包含空手）
    /// 順序：武器1 ← 武器2 ← 空手 ← 武器1
    /// </summary>
    public void SwitchToPreviousWeapon()
    {
        if (itemHolder == null)
        {
            currentTypeIndex = -1; RefreshWeaponDisplay(); return;
        }
        var queues = BuildTypeQueues(out _);
        int safety = 0;
        do
        {
            currentTypeIndex--;
            if (currentTypeIndex < -1) currentTypeIndex = 1;
            
            if (currentTypeIndex == -1)
            {
                itemHolder.TryEquipEmptyHands();
                RefreshWeaponDisplay();
                return;
            }
            else if (currentTypeIndex == 0)
            {
                if (queues.ContainsKey(KnifeTypeKey) && queues[KnifeTypeKey].Count > 0)
                {
                    var w = queues[KnifeTypeKey].Peek();
                    SwitchToWeaponInstance(w);
                    RefreshWeaponDisplay();
                    return;
                }
            }
            else if (currentTypeIndex == 1)
            {
                if (queues.ContainsKey(GunTypeKey) && queues[GunTypeKey].Count > 0)
                {
                    var w = queues[GunTypeKey].Peek();
                    SwitchToWeaponInstance(w);
                    RefreshWeaponDisplay();
                    return;
                }
            }
            safety++;
        } while (safety < 4);
        currentTypeIndex = -1;
        itemHolder.TryEquipEmptyHands();
        RefreshWeaponDisplay();
    }
    
    private void SwitchToWeaponInstance(Weapon weapon)
    {
        if (itemHolder == null || weapon == null) return;
        var all = itemHolder.GetAllItems();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == weapon)
            {
                itemHolder.SwitchToItem(i);
                return;
            }
        }
    }
}

