using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ItemHolder（物品管理系統）
/// 支援多物品切換系統，可以管理武器、鑰匙等所有 Item 類型
/// </summary>
public class ItemHolder : MonoBehaviour
{
    [Header("Item Prefabs")]
    [Tooltip("物品 Prefab 數組（可選）。如果為空，物品將通過代碼動態裝備（例如由 EntityManager 裝備）")]
    [SerializeField] private GameObject[] itemPrefabs; // Array of item prefabs for switching

    [Header("Behavior")]
    [SerializeField] private bool equipOnStart = true; // 一開始是否自動裝備 prefab
    [Tooltip("如果物品通過代碼動態裝備（不設置 itemPrefabs），設為 true 可隱藏警告")]
    [SerializeField] private bool allowDynamicEquipping = true; // 允許動態裝備（不從 itemPrefabs 初始化）
    [Tooltip("是否在沒有物品時使用空手狀態（預設為 false")]
    [SerializeField] private bool useEmptyHands = false; // 是否使用空手狀態

    // Item management
    private List<Item> availableItems = new List<Item>(); // All instantiated items
    private int currentItemIndex = 0;
    private Item currentItem;
    
    // 空手狀態（當沒有物品時使用）
    private EmptyHands emptyHands;

    // 記錄是哪個 prefab 用來裝備（方便避免重複 Instantiate 同一 prefab）
    private GameObject equippedPrefab;
    
    // 追蹤每個物品對應的原始 Prefab（用於掉落時重新生成）
    private Dictionary<Item, GameObject> itemToPrefabMap = new Dictionary<Item, GameObject>();

    // 防止重入（同一時間多次呼叫 EquipFromPrefab）
    private bool isEquipping = false;

    // attack state (only for weapons)
    private bool isAttacking = false;
    private float attackEndTime = 0f;
    private float originalRotation = 0f;
    

    // 向後兼容的屬性
    public Weapon CurrentWeapon => currentItem as Weapon;
    public int CurrentWeaponIndex => currentItemIndex;
    public int WeaponCount => availableItems.Count;
    
    // 新的 Item 相關屬性
    public Item CurrentItem => currentItem;
    public int CurrentItemIndex => currentItemIndex;
    public int ItemCount => availableItems.Count;
    
    /// <summary>
    /// 檢測當前物品是否是武器
    /// </summary>
    public bool IsCurrentItemWeapon => currentItem is Weapon;
    
    public event Action<Vector2, float, GameObject> OnAttackPerformed;
    public event Action<int, int> OnWeaponDurabilityChanged; // 當前耐久度, 最大耐久度
    public event Action OnWeaponBroken; // 武器損壞事件
    public event Action<Weapon> OnWeaponChanged; // 武器切換事件（向後兼容）
    public event Action<Item> OnItemChanged; // 物品切換事件

    private void Start()
    {
        // 初始化空手狀態（但不裝備，只是創建實例）
        if (useEmptyHands)
        {
            InitializeEmptyHands();
        }
        
        // Initialize items from prefabs array
        if (itemPrefabs != null && itemPrefabs.Length > 0)
        {
            foreach (var prefab in itemPrefabs)
            {
                if (prefab != null)
                {
                    var item = InstantiateItem(prefab);
                    if (item != null)
                    {
                        availableItems.Add(item);
                        itemToPrefabMap[item] = prefab; // 記錄對應的 Prefab
                        item.gameObject.SetActive(false); // Hide initially
                    }
                }
            }
            
            // 如果有物品，裝備第一個物品（無論 useEmptyHands 是否為 true）
            if (availableItems.Count > 0 && equipOnStart)
            {
                SwitchToItem(0);
            }
            // 只有在沒有任何物品時才裝備空手
            else if (availableItems.Count == 0 && useEmptyHands && emptyHands != null)
            {
                EquipEmptyHands();
            }
        }
        else
        {
            // 沒有設定 itemPrefabs（動態裝備模式）
            if (!allowDynamicEquipping)
            {
                Debug.LogWarning($"ItemHolder on {gameObject.name}: No item prefabs assigned! If items are equipped dynamically via code, set 'Allow Dynamic Equipping' to true to suppress this warning.");
            }
            
            // 動態裝備模式下不自動裝備空手，等待物品被動態添加
            // 空手會在 ClearAllItems 或沒有物品時自動裝備
        }
    }

    // 新增 Update 方法來檢查攻擊動畫是否結束
    private void Update()
    {
        // 檢查攻擊動畫是否結束（僅適用於武器）
        if (isAttacking && Time.time >= attackEndTime)
        {
            ResetItemRotation();
        }
    }

    private void OnDisable()
    {
        // 可選：當 holder 被停用時不自動 destroy 物品，視遊戲需求決定
        // 如果你希望停用時把物品一起 disable，可以 uncomment：
        // if (currentItem != null) currentItem.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // 若你希望當 holder 被銷毀時也銷毀其物品（避免 orphan），可以這樣：
        if (currentItem != null)
        {
            // 如果是武器，取消訂閱耐久度事件
            if (currentItem is Weapon weapon)
            {
                weapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
                weapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            }
            
            // 如果不是空手，銷毀物品
            if (!(currentItem is EmptyHands))
            {
                Destroy(currentItem.gameObject);
            }
            
            currentItem = null;
            equippedPrefab = null;
        }
        
        // 銷毀空手物件
        if (emptyHands != null && emptyHands.gameObject != null)
        {
            Destroy(emptyHands.gameObject);
            emptyHands = null;
        }
    }

    /// <summary>
    /// 設定物品（傳入已存在的 Item 實例）
    /// 預設會銷毀舊的實例；若使用物件池請改成回收舊物件。
    /// </summary>
    public void SetItem(Item item)
    {
        if (item == null)
        {
            // 取消訂閱舊武器的耐久度事件（如果是武器）
            if (currentItem is Weapon weapon)
            {
                weapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
                weapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            }
            
            if (currentItem != null)
            {
                currentItem.OnUnequip();
            }
            
            currentItem = null;
            equippedPrefab = null;
            return;
        }

        // 如果傳入的 item 已經是本 holder 的 child，且就是 currentItem，就直接返回
        if (currentItem == item && item.transform.parent == transform)
        {
            return;
        }

        // 如果已有其他物品，移除或銷毀（根據需求）
        if (currentItem != null && currentItem != item)
        {
            // 取消訂閱舊武器的耐久度事件（如果是武器）
            if (currentItem is Weapon weapon)
            {
                weapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
                weapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            }
            
            currentItem.OnUnequip();
            
            // 預設銷毀舊實例；若你用 pooling，改為回收
            Destroy(currentItem.gameObject);
        }

        currentItem = item;

        // 如果是武器，訂閱耐久度事件
        if (currentItem is Weapon weaponToSubscribe)
        {
            weaponToSubscribe.OnDurabilityChanged += OnWeaponDurabilityChangedHandler;
            weaponToSubscribe.OnWeaponBroken += OnWeaponBrokenHandler;
        }

        // 把物品掛到本 holder 下（local transform reset）
        currentItem.transform.SetParent(this.transform, worldPositionStays: false);
        currentItem.transform.localPosition = Vector3.zero;
        currentItem.transform.localRotation = Quaternion.identity;
        currentItem.transform.localScale = Vector3.one;
        
        currentItem.OnEquip();
    }

    /// <summary>
    /// 從 prefab 裝備物品（安全且會避免重複複製）
    /// 若已裝備相同 prefab，會直接回傳現有物品。
    /// </summary>
    public Item EquipFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        // 已有正在進行的裝備流程 → 直接回傳現有物品（或 null）
        if (isEquipping)
        {
            return currentItem;
        }

        // 如果已經裝備且已知是由同一 prefab 生成，則不再 Instantiate
        if (currentItem != null && equippedPrefab == prefab)
        {
            return currentItem;
        }

        // 如果 currentItem 存在但 equippedPrefab 不同，表示要換物品：先清除舊的
        if (currentItem != null && equippedPrefab != prefab)
        {
            currentItem.OnUnequip();
            Destroy(currentItem.gameObject);
            currentItem = null;
            equippedPrefab = null;
        }

        isEquipping = true;
        try
        {
            // Instantiate 並把它直接放在本 holder 下
            GameObject itemGO = Instantiate(prefab, this.transform);
            itemGO.transform.localPosition = Vector3.zero;
            itemGO.transform.localRotation = Quaternion.identity;
            itemGO.transform.localScale = Vector3.one;

            var item = itemGO.GetComponent<Item>();
            if (item == null)
            {
                Debug.LogWarning($"EquipFromPrefab: prefab {prefab.name} does not contain an Item component.");
                Destroy(itemGO);
                return null;
            }

            // 記錄是哪個 prefab 生成的，避免重複生成
            equippedPrefab = prefab;
            SetItem(item);
            return currentItem;
        }
        finally
        {
            isEquipping = false;
        }
    }

    /// <summary>
    /// 更新物品朝向
    /// </summary>
    public void UpdateItemDirection(Vector2 direction)
    {
        if (currentItem == null || direction.sqrMagnitude < 0.01f) return;

        currentItem.UpdateDirection(direction);

        // 如果是武器，記錄原始旋轉角度（用於攻擊動畫）
        if (currentItem is Weapon && !isAttacking)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            originalRotation = angle;
        }
    }

    /// <summary>
    /// 更新武器朝向（向後兼容方法）
    /// </summary>
    public void UpdateWeaponDirection(Vector2 direction)
    {
        UpdateItemDirection(direction);
    }

    /// <summary>
    /// 嘗試攻擊（僅適用於武器）
    /// </summary>
    public bool TryAttack(GameObject attacker)
    {
        if (!IsCurrentItemWeapon || isAttacking) return false;

        Weapon weapon = currentItem as Weapon;
        if (weapon == null) return false;

        Vector2 origin = transform.position;

        bool success = weapon.TryPerformAttack(origin, attacker);
        Debug.Log("TryAttack: " + success);
        if (success)
        {
            TriggerAttackAnimation();
            
            // 只有近戰武器才觸发範圍檢測事件（遠程武器用子彈處理傷害）
            if (weapon is MeleeWeapon meleeWeapon)
            {
                OnAttackPerformed?.Invoke(origin, meleeWeapon.AttackRange, attacker);
            }
        }

        return success;
    }

    public bool CanAttack()
    {
        // 空手無法攻擊
        if (IsEmptyHands()) return false;
        
        if (!IsCurrentItemWeapon) return false;
        Weapon weapon = currentItem as Weapon;
        return weapon != null && !isAttacking && weapon.CanAttack();
    }

    private void TriggerAttackAnimation()
    {
        if (!IsCurrentItemWeapon || isAttacking) return;
        Weapon weapon = currentItem as Weapon;
        if (weapon == null) return;

        isAttacking = true;
        const float attackDuration = 0.15f; // 攻擊動畫持續時間
        const float attackAngle = 30f; // 攻擊揮動角度
        
        attackEndTime = Time.time + attackDuration;
        float swingAngle = originalRotation + attackAngle;
        weapon.transform.rotation = Quaternion.Euler(0f, 0f, swingAngle);
    }

    private void ResetItemRotation()
    {
        if (currentItem == null || !IsCurrentItemWeapon) return;
        Weapon weapon = currentItem as Weapon;
        if (weapon == null) return;

        isAttacking = false;
        weapon.transform.rotation = Quaternion.Euler(0f, 0f, originalRotation);
    }

    public void StopAttackAnimation()
    {
        if (isAttacking)
        {
            ResetItemRotation();
        }
    }

    /// <summary>
    /// 切換到下一個物品（循環切換）
    /// 如果使用空手模式，會在物品之間包含空手狀態
    /// </summary>
    /// <returns>切換是否成功</returns>
    public bool SwitchToNextItem()
    {
        // 如果沒有物品且不使用空手，無法切換
        if (availableItems.Count == 0 && !useEmptyHands) return false;
        
        // 如果只有空手或只有一個物品（不使用空手時），無法切換
        if (availableItems.Count <= 1 && !useEmptyHands) return false;
        
        // 如果使用空手模式
        if (useEmptyHands)
        {
            // 如果當前是空手，切換到第一個物品（如果有）
            if (IsEmptyHands())
            {
                if (availableItems.Count > 0)
                {
                    return SwitchToItem(0);
                }
                return false; // 沒有其他物品可切換
            }
            
            // 如果當前是最後一個物品，切換到空手
            if (currentItemIndex == availableItems.Count - 1)
            {
                EquipEmptyHands();
                return true;
            }
            
            // 否則切換到下一個物品
            int nextIndex = currentItemIndex + 1;
            return SwitchToItem(nextIndex);
        }
        else
        {
            // 不使用空手模式，正常循環切換
            int nextIndex = (currentItemIndex + 1) % availableItems.Count;
            return SwitchToItem(nextIndex);
        }
    }

    /// <summary>
    /// 切換到上一個物品（循環切換）
    /// 如果使用空手模式，會在物品之間包含空手狀態
    /// </summary>
    /// <returns>切換是否成功</returns>
    public bool SwitchToPreviousItem()
    {
        // 如果沒有物品且不使用空手，無法切換
        if (availableItems.Count == 0 && !useEmptyHands) return false;
        
        // 如果只有空手或只有一個物品（不使用空手時），無法切換
        if (availableItems.Count <= 1 && !useEmptyHands) return false;
        
        // 如果使用空手模式
        if (useEmptyHands)
        {
            // 如果當前是空手，切換到最後一個物品（如果有）
            if (IsEmptyHands())
            {
                if (availableItems.Count > 0)
                {
                    return SwitchToItem(availableItems.Count - 1);
                }
                return false; // 沒有其他物品可切換
            }
            
            // 如果當前是第一個物品，切換到空手
            if (currentItemIndex == 0)
            {
                EquipEmptyHands();
                return true;
            }
            
            // 否則切換到上一個物品
            int prevIndex = currentItemIndex - 1;
            return SwitchToItem(prevIndex);
        }
        else
        {
            // 不使用空手模式，正常循環切換
            int prevIndex = (currentItemIndex - 1 + availableItems.Count) % availableItems.Count;
            return SwitchToItem(prevIndex);
        }
    }

    /// <summary>
    /// 切換物品
    /// </summary>
    /// <param name="index">目標物品的索引</param>
    /// <returns>切換是否成功</returns>
    public bool SwitchToItem(int index)
    {
        if (index < 0 || index >= availableItems.Count) return false;

        // 先卸下當前物品（如果有的話）
        if (currentItem != null)
        {
            // 取消訂閱舊武器的事件（防止重複訂閱）
            if (currentItem is Weapon oldWeapon)
            {
                oldWeapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
                oldWeapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            }
            
            currentItem.OnUnequip();
            currentItem.gameObject.SetActive(false);
        }

        currentItemIndex = index;
        currentItem = availableItems[currentItemIndex];

        // 啟用並重置新物品
        currentItem.gameObject.SetActive(true);
        currentItem.transform.SetParent(this.transform, worldPositionStays: false);
        currentItem.transform.localPosition = Vector3.zero;
        currentItem.transform.localRotation = Quaternion.identity;
        currentItem.transform.localScale = Vector3.one;
        
        currentItem.OnEquip();

        // 如果是武器，訂閱事件
        if (currentItem is Weapon weapon)
        {
            weapon.OnDurabilityChanged += OnWeaponDurabilityChangedHandler;
            weapon.OnWeaponBroken += OnWeaponBrokenHandler;
            OnWeaponChanged?.Invoke(weapon); // 向後兼容
        }
        
        OnItemChanged?.Invoke(currentItem);
        return true;
    }

    /// <summary>
    /// 切換到下一個武器類型（只在武器類型之間循環：Knife → Gun → 空手）
    /// 每種類型使用最舊的武器（先獲得的優先）
    /// </summary>
    /// <returns>切換是否成功</returns>
    public bool SwitchToNextWeapon()
    {
        // 獲取所有武器並按類型分組
        var weapons = GetItemsOfType<Weapon>();
        var weaponsByType = GroupWeaponsByType(weapons);
        
        if (weaponsByType.Count == 0)
        {
            // 沒有武器，如果使用空手則切換到空手
            if (useEmptyHands)
            {
                EquipEmptyHands();
                return true;
            }
            return false;
        }
        
        // 確定當前武器類型
        string currentType = null;
        if (currentItem != null && currentItem is Weapon currentWeapon)
        {
            currentType = GetWeaponTypeName(currentWeapon);
        }
        
        // 定義類型切換順序：Knife → Gun → 空手 → Knife
        string[] typeOrder = { "Knife", "Gun" };
        int currentTypeIndex = -1; // -1 表示空手或未知類型
        
        if (!string.IsNullOrEmpty(currentType))
        {
            for (int i = 0; i < typeOrder.Length; i++)
            {
                if (typeOrder[i] == currentType)
                {
                    currentTypeIndex = i;
                    break;
                }
            }
        }
        
        // 嘗試切換到下一個類型（最多嘗試3次：下一個武器類型 → 再下一個 → 空手）
        int attempts = 0;
        while (attempts < 3)
        {
            currentTypeIndex++;
            
            // 超過最後一個類型，考慮空手
            if (currentTypeIndex >= typeOrder.Length)
            {
                if (useEmptyHands)
                {
                    EquipEmptyHands();
                    return true;
                }
                else
                {
                    // 不使用空手，循環回第一個類型
                    currentTypeIndex = 0;
                }
            }
            
            // 嘗試切換到該類型的武器
            if (currentTypeIndex < typeOrder.Length)
            {
                string targetType = typeOrder[currentTypeIndex];
                if (weaponsByType.ContainsKey(targetType) && weaponsByType[targetType].Count > 0)
                {
                    // 使用該類型最舊的武器（Queue 的第一個）
                    Weapon targetWeapon = weaponsByType[targetType].Peek();
                    return SwitchToItemInstance(targetWeapon);
                }
            }
            
            attempts++;
        }
        
        // 如果所有嘗試都失敗，返回 false
        return false;
    }

    /// <summary>
    /// 切換到上一個武器類型（只在武器類型之間循環：Gun ← Knife ← 空手）
    /// </summary>
    /// <returns>切換是否成功</returns>
    public bool SwitchToPreviousWeapon()
    {
        // 獲取所有武器並按類型分組
        var weapons = GetItemsOfType<Weapon>();
        var weaponsByType = GroupWeaponsByType(weapons);
        
        if (weaponsByType.Count == 0)
        {
            // 沒有武器，如果使用空手則切換到空手
            if (useEmptyHands)
            {
                EquipEmptyHands();
                return true;
            }
            return false;
        }
        
        // 確定當前武器類型
        string currentType = null;
        if (currentItem != null && currentItem is Weapon currentWeapon)
        {
            currentType = GetWeaponTypeName(currentWeapon);
        }
        
        // 定義類型切換順序
        string[] typeOrder = { "Knife", "Gun" };
        int currentTypeIndex = typeOrder.Length; // 默認為空手位置
        
        if (!string.IsNullOrEmpty(currentType))
        {
            for (int i = 0; i < typeOrder.Length; i++)
            {
                if (typeOrder[i] == currentType)
                {
                    currentTypeIndex = i;
                    break;
                }
            }
        }
        
        // 嘗試切換到上一個類型
        int attempts = 0;
        while (attempts < 3)
        {
            currentTypeIndex--;
            
            // 小於0，考慮空手
            if (currentTypeIndex < 0)
            {
                if (useEmptyHands)
                {
                    EquipEmptyHands();
                    return true;
                }
                else
                {
                    // 不使用空手，循環到最後一個類型
                    currentTypeIndex = typeOrder.Length - 1;
                }
            }
            
            // 嘗試切換到該類型的武器
            if (currentTypeIndex >= 0 && currentTypeIndex < typeOrder.Length)
            {
                string targetType = typeOrder[currentTypeIndex];
                if (weaponsByType.ContainsKey(targetType) && weaponsByType[targetType].Count > 0)
                {
                    Weapon targetWeapon = weaponsByType[targetType].Peek();
                    return SwitchToItemInstance(targetWeapon);
                }
            }
            
            attempts++;
        }
        
        return false;
    }
    
    /// <summary>
    /// 將武器按類型分組（返回每個類型的隊列，保持獲取順序）
    /// </summary>
    private Dictionary<string, Queue<Weapon>> GroupWeaponsByType(List<Weapon> weapons)
    {
        var grouped = new Dictionary<string, Queue<Weapon>>();
        
        foreach (var weapon in weapons)
        {
            string type = GetWeaponTypeName(weapon);
            if (string.IsNullOrEmpty(type)) continue;
            
            if (!grouped.ContainsKey(type))
            {
                grouped[type] = new Queue<Weapon>();
            }
            
            grouped[type].Enqueue(weapon);
        }
        
        return grouped;
    }
    
    /// <summary>
    /// 切換到指定的物品實例
    /// </summary>
    private bool SwitchToItemInstance(Item item)
    {
        if (item == null) return false;
        
        // 在 availableItems 中找到該物品的索引
        for (int i = 0; i < availableItems.Count; i++)
        {
            if (availableItems[i] == item)
            {
                return SwitchToItem(i);
            }
        }
        
        return false;
    }

    /// <summary>
    /// 切換武器（向後兼容方法）
    /// </summary>
    /// <param name="index">目標武器的索引</param>
    /// <returns>切換是否成功</returns>
    public bool SwitchToWeapon(int index)
    {
        return SwitchToItem(index);
    }

    /// <summary>
    /// 設定武器（向後兼容方法）
    /// </summary>
    /// <param name="weapon">武器實例</param>
    public void SetWeapon(Weapon weapon)
    {
        SetItem(weapon);
    }

    /// <summary>
    /// 從 prefab 裝備武器（向後兼容方法）
    /// </summary>
    /// <param name="prefab">武器 prefab</param>
    /// <returns>裝備的武器</returns>
    public Weapon EquipWeaponFromPrefab(GameObject prefab)
    {
        var item = EquipFromPrefab(prefab);
        return item as Weapon;
    }

    /// <summary>
    /// 處理武器耐久度變化事件
    /// </summary>
    private void OnWeaponDurabilityChangedHandler(int currentDurability, int maxDurability)
    {
        OnWeaponDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }

    /// <summary>
    /// 處理武器損壞事件
    /// </summary>
    private void OnWeaponBrokenHandler()
    {
        if (currentItem != null)
        {
            Debug.Log($"武器 {currentItem.name} 已損壞並將被銷毀！");
            
            // 從可用物品列表中移除
            Item brokenItem = currentItem;
            Weapon brokenWeapon = brokenItem as Weapon;
            availableItems.Remove(brokenItem);
            
            // 取消訂閱事件
            if (brokenWeapon != null)
            {
                brokenWeapon.OnDurabilityChanged -= OnWeaponDurabilityChangedHandler;
                brokenWeapon.OnWeaponBroken -= OnWeaponBrokenHandler;
            }
            
            // 智能切換邏輯：優先同類型武器 → 其他武器 → 空手（不要鑰匙）
            Weapon nextWeapon = FindNextWeaponAfterBroken(brokenWeapon);
            
            if (nextWeapon != null)
            {
                // 找到下一個武器，切換到它
                SwitchToItemInstance(nextWeapon);
                Debug.Log($"武器損壞後切換到: {nextWeapon.ItemName}");
            }
            else
            {
                // 沒有其他武器了，裝備空手（如果啟用）
                if (useEmptyHands && emptyHands != null)
                {
                    EquipEmptyHands();
                    Debug.Log("所有武器都已損壞！已裝備空手。");
                }
                else
                {
                    currentItem = null;
                    currentItemIndex = 0;
                    equippedPrefab = null;
                    Debug.Log("所有武器都已損壞！");
                    
                    // 觸發物品變更事件，讓 UI 知道沒有武器了
                    OnItemChanged?.Invoke(null);
                }
            }
            
            // 在處理完所有清理工作後才觸發事件
            OnWeaponBroken?.Invoke();
        }
    }
    
    /// <summary>
    /// 武器損壞後尋找下一個要裝備的武器
    /// 優先級：同類型武器 → 其他類型武器 → null
    /// </summary>
    private Weapon FindNextWeaponAfterBroken(Weapon brokenWeapon)
    {
        var allWeapons = GetItemsOfType<Weapon>();
        
        if (allWeapons.Count == 0)
        {
            return null; // 沒有武器了
        }
        
        // 如果損壞的武器有類型，優先尋找同類型
        if (brokenWeapon != null)
        {
            string brokenType = GetWeaponTypeName(brokenWeapon);
            
            // 優先：同類型武器
            foreach (var weapon in allWeapons)
            {
                if (GetWeaponTypeName(weapon) == brokenType)
                {
                    return weapon; // 找到同類型，返回第一個（最舊的）
                }
            }
        }
        
        // 次選：任何其他武器（第一個即可）
        return allWeapons.Count > 0 ? allWeapons[0] : null;
    }
    
    /// <summary>
    /// 獲取武器類型名稱（用於分類）
    /// </summary>
    private string GetWeaponTypeName(Weapon weapon)
    {
        if (weapon == null) return null;
        
        string name = weapon.ItemName?.ToLowerInvariant();
        if (name != null)
        {
            if (name.Contains("knife")) return "Knife";
            if (name.Contains("gun")) return "Gun";
        }
        
        // 後備：使用類型名稱
        string typeName = weapon.GetType().Name.ToLowerInvariant();
        if (typeName.Contains("knife")) return "Knife";
        if (typeName.Contains("gun")) return "Gun";
        
        return weapon.GetType().Name; // 返回類型名作為默認
    }

    /// <summary>
    /// 修復當前武器的耐久度
    /// </summary>
    /// <param name="amount">修復的數量</param>
    public void RepairCurrentWeapon(int amount)
    {
        if (IsCurrentItemWeapon && currentItem is Weapon weapon)
        {
            weapon.RepairDurability(amount);
        }
    }

    /// <summary>
    /// 完全修復當前武器
    /// </summary>
    public void FullRepairCurrentWeapon()
    {
        if (IsCurrentItemWeapon && currentItem is Weapon weapon)
        {
            weapon.FullRepair();
        }
    }

    /// <summary>
    /// 獲取當前武器的耐久度信息
    /// </summary>
    /// <returns>耐久度信息 (當前耐久度, 最大耐久度, 耐久度百分比)</returns>
    public (int current, int max, float percentage) GetWeaponDurabilityInfo()
    {
        if (!IsCurrentItemWeapon || !(currentItem is Weapon weapon))
        {
            return (0, 0, 0f);
        }
        
        return (weapon.CurrentDurability, weapon.MaxDurability, weapon.DurabilityPercentage);
    }

    /// <summary>
    /// 從 prefab 添加物品到列表（不裝備，用於撿取物品）
    /// 規則：
    /// 1. 如果是武器且當前沒有裝備武器，自動裝備該武器
    /// 2. 如果是武器且已有武器，不裝備（實體最多持有一個武器）
    /// 3. 如果是非武器物品（如鑰匙），添加到列表但不裝備
    /// 4. 如果當前是空手且添加了武器，自動裝備武器
    /// </summary>
    /// <param name="prefab">物品 prefab</param>
    /// <returns>添加的 Item 組件，失敗則返回 null</returns>
    public Item AddItemFromPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("AddItemFromPrefab: prefab is null");
            return null;
        }
        
        // 實例化物品
        Item item = InstantiateItem(prefab);
        if (item == null)
        {
            return null;
        }
        
        bool isWeapon = item is Weapon;
        bool currentIsWeapon = IsCurrentItemWeapon;
        bool currentIsEmptyHands = IsEmptyHands();
        bool hadNoItems = availableItems.Count == 0; // 添加前是否沒有物品
        
        Debug.Log($"[ItemHolder] AddItemFromPrefab called on {gameObject.name}: item={item.ItemName}, isWeapon={isWeapon}, currentItem={currentItem?.ItemName ?? "null"}, currentIsWeapon={currentIsWeapon}, currentIsEmptyHands={currentIsEmptyHands}, hadNoItems={hadNoItems}");
        
        // 加入到可用物品列表尾端
        availableItems.Add(item);
        itemToPrefabMap[item] = prefab; // 記錄對應的 Prefab
        
        // 決定是否裝備這個物品
        bool shouldEquip = false;
        
        // 規則：如果這是第一個物品（之前沒有任何物品），總是裝備它
        if (hadNoItems)
        {
            shouldEquip = true;
            Debug.Log($"[ItemHolder] ✅ 裝備第一個物品 {item.ItemName} 到 {gameObject.name} (這是第一個添加的物品)");
        }
        else if (isWeapon)
        {
            // 如果是武器，且當前沒有武器或只有空手，則裝備
            if (!currentIsWeapon || currentIsEmptyHands)
            {
                shouldEquip = true;
                Debug.Log($"[ItemHolder] ✅ 裝備武器 {item.ItemName} 到 {gameObject.name} (當前沒有武器或是空手)");
            }
            else
            {
                // 已經有武器了，不裝備新武器（但會加入列表）
                Debug.Log($"[ItemHolder] ❌ {gameObject.name} 已有武器 {(currentItem != null ? currentItem.ItemName : "Unknown")}，將 {item.ItemName} 加入列表但不裝備");
            }
        }
        else
        {
            // 非武器物品（如鑰匙）
            // 只在當前是空手時才裝備
            if (currentIsEmptyHands)
            {
                shouldEquip = true;
                Debug.Log($"[ItemHolder] ✅ 裝備非武器物品 {item.ItemName} 到 {gameObject.name} (當前是空手)");
            }
            else
            {
                Debug.Log($"[ItemHolder] ❌ 將非武器物品 {item.ItemName} 加入 {gameObject.name} 的列表 (當前已有 {currentItem?.ItemName})");
            }
        }
        
        if (shouldEquip)
        {
            item.gameObject.SetActive(false); // 先設為不啟用
            Debug.Log($"[ItemHolder] Calling SwitchToItem({availableItems.Count - 1}) for {item.ItemName} on {gameObject.name}");
            SwitchToItem(availableItems.Count - 1); // 切換到剛加入的物品
            Debug.Log($"[ItemHolder] After SwitchToItem: currentItem={currentItem?.ItemName ?? "null"}, active={currentItem?.gameObject.activeSelf}");
        }
        else
        {
            // 不裝備，只是加入列表
            item.gameObject.SetActive(false);
            
            // 觸發物品變更事件以更新 UI
            OnItemChanged?.Invoke(currentItem); // 保持當前物品不變，但通知 UI 更新
        }
        
        Debug.Log($"[ItemHolder] ✅ Finished AddItemFromPrefab: {item.ItemName} added to {gameObject.name}. Total items: {availableItems.Count}, Currently equipped: {(currentItem != null ? currentItem.ItemName : "None")}, Item active: {currentItem?.gameObject.activeSelf}");
        
        return item;
    }
    
    /// <summary>
    /// 從 prefab 實例化物品（內部使用）
    /// </summary>
    /// <param name="prefab">物品 prefab</param>
    /// <returns>實例化的 Item 組件</returns>
    private Item InstantiateItem(GameObject prefab)
    {
        GameObject itemGO = Instantiate(prefab, this.transform);
        itemGO.transform.localPosition = Vector3.zero;
        itemGO.transform.localRotation = Quaternion.identity;
        itemGO.transform.localScale = Vector3.one;

        var item = itemGO.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning($"InstantiateItem: prefab {prefab.name} does not contain an Item component.");
            Destroy(itemGO);
            return null;
        }

        return item;
    }

    /// <summary>
    /// 從 prefab 實例化武器（向後兼容方法，內部使用）
    /// </summary>
    /// <param name="prefab">武器 prefab</param>
    /// <returns>實例化的 Weapon 組件</returns>
    private Weapon InstantiateWeapon(GameObject prefab)
    {
        var item = InstantiateItem(prefab);
        return item as Weapon;
    }

    /// <summary>
    /// 獲取指定類型的物品
    /// </summary>
    /// <typeparam name="T">物品類型</typeparam>
    /// <returns>找到的物品，如果不存在則返回 null</returns>
    public T GetItemOfType<T>() where T : Item
    {
        return availableItems.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// 獲取所有指定類型的物品
    /// </summary>
    /// <typeparam name="T">物品類型</typeparam>
    /// <returns>找到的物品列表</returns>
    public List<T> GetItemsOfType<T>() where T : Item
    {
        return availableItems.OfType<T>().ToList();
    }

    /// <summary>
    /// 獲取指定索引的物品
    /// </summary>
    /// <param name="index">物品索引</param>
    /// <returns>物品，如果索引無效則返回 null</returns>
    public Item GetItemAtIndex(int index)
    {
        if (index < 0 || index >= availableItems.Count)
            return null;
        return availableItems[index];
    }

    /// <summary>
    /// 獲取所有物品（只讀列表）
    /// </summary>
    /// <returns>只讀的物品列表</returns>
    public IReadOnlyList<Item> GetAllItems()
    {
        return availableItems.AsReadOnly();
    }
    
    /// <summary>
    /// 獲取物品對應的原始 Prefab
    /// </summary>
    /// <param name="item">物品實例</param>
    /// <returns>對應的 Prefab，如果不存在則返回 null</returns>
    public GameObject GetItemPrefab(Item item)
    {
        if (item != null && itemToPrefabMap.ContainsKey(item))
        {
            return itemToPrefabMap[item];
        }
        return null;
    }
    
    /// <summary>
    /// 獲取所有物品及其對應的 Prefab（用於掉落物品）
    /// </summary>
    /// <returns>物品和 Prefab 的鍵值對列表</returns>
    public List<KeyValuePair<Item, GameObject>> GetAllItemsWithPrefabs()
    {
        List<KeyValuePair<Item, GameObject>> result = new List<KeyValuePair<Item, GameObject>>();
        
        foreach (var item in availableItems)
        {
            if (item != null && itemToPrefabMap.ContainsKey(item))
            {
                result.Add(new KeyValuePair<Item, GameObject>(item, itemToPrefabMap[item]));
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 檢查是否擁有指定類型的鑰匙
    /// </summary>
    /// <param name="keyType">鑰匙類型</param>
    /// <returns>是否擁有該鑰匙</returns>
    public bool HasKey(KeyType keyType)
    {
        if (keyType == KeyType.None)
            return true; // 不需要鑰匙的門總是可以開啟
        
        foreach (var item in availableItems)
        {
            if (item is Key key)
            {
                if (key.CanUnlock(keyType))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 獲取可以開啟指定門的鑰匙
    /// </summary>
    /// <param name="keyType">所需的鑰匙類型</param>
    /// <returns>找到的鑰匙，如果沒有則返回 null</returns>
    public Key GetKeyForDoor(KeyType keyType)
    {
        if (keyType == KeyType.None)
            return null; // 不需要鑰匙
        
        foreach (var item in availableItems)
        {
            if (item is Key key)
            {
                if (key.CanUnlock(keyType))
                {
                    return key;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 檢查當前裝備的 item 是否為鑰匙
    /// </summary>
    /// <returns>是否為鑰匙</returns>
    public bool IsCurrentItemKey()
    {
        return currentItem != null && currentItem is Key;
    }
    
    /// <summary>
    /// 獲取當前裝備的鑰匙（如果有）
    /// </summary>
    /// <returns>鑰匙，如果當前 item 不是鑰匙則返回 null</returns>
    public Key GetCurrentKey()
    {
        return currentItem as Key;
    }
    
    /// <summary>
    /// 移除指定物品
    /// </summary>
    /// <param name="item">要移除的物品</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveItem(Item item)
    {
        if (item == null || !availableItems.Contains(item))
        {
            return false;
        }
        
        // 如果是當前物品，需要切換到其他物品
        bool wasCurrentItem = (item == currentItem);
        
        // 從列表中移除
        availableItems.Remove(item);
        itemToPrefabMap.Remove(item);
        
        // 如果是當前物品，切換到下一個可用物品
        if (wasCurrentItem)
        {
            if (availableItems.Count > 0)
            {
                // 調整索引
                if (currentItemIndex >= availableItems.Count)
                {
                    currentItemIndex = 0;
                }
                SwitchToItem(currentItemIndex);
            }
            else
            {
                // 沒有其他物品了，裝備空手（如果啟用）
                if (useEmptyHands && emptyHands != null)
                {
                    EquipEmptyHands();
                }
                else
                {
                    currentItem = null;
                    currentItemIndex = 0;
                    OnItemChanged?.Invoke(null);
                }
            }
        }
        else
        {
            // 調整當前索引（如果需要）
            int removedIndex = availableItems.IndexOf(item);
            if (removedIndex != -1 && removedIndex < currentItemIndex)
            {
                currentItemIndex--;
            }
            // 非當前裝備物品被移除時也要通知 UI 重新整理（例如鑰匙使用後消失）
            OnItemChanged?.Invoke(currentItem);
        }
        
        // 銷毀物品
        if (item != null && item.gameObject != null)
        {
            Destroy(item.gameObject);
        }
        
        Debug.Log($"[ItemHolder] 移除物品: {item.ItemName}，剩餘物品數: {availableItems.Count}");
        return true;
    }
    
    /// <summary>
    /// 獲取所有鑰匙
    /// </summary>
    /// <returns>鑰匙列表</returns>
    public List<Key> GetAllKeys()
    {
        return GetItemsOfType<Key>();
    }
    
    /// <summary>
    /// 獲取指定類型的鑰匙
    /// </summary>
    /// <param name="keyType">鑰匙類型</param>
    /// <returns>找到的鑰匙，如果沒有則返回 null</returns>
    public Key GetKeyByType(KeyType keyType)
    {
        foreach (var item in availableItems)
        {
            if (item is Key key && key.KeyType == keyType)
            {
                return key;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 清空所有物品（用於死亡時掉落）
    /// </summary>
    public void ClearAllItems()
    {
        Debug.Log($"[ItemHolder] ClearAllItems called on {gameObject.name}. Current items: {availableItems.Count}, currentItem: {currentItem?.ItemName ?? "null"}, useEmptyHands: {useEmptyHands}");
        
        // 銷毀所有物品實例（不包括空手）
        foreach (var item in availableItems)
        {
            if (item != null && item.gameObject != null && !(item is EmptyHands))
            {
                Destroy(item.gameObject);
            }
        }
        
        // 清空列表
        availableItems.Clear();
        itemToPrefabMap.Clear();
        currentItem = null;
        currentItemIndex = 0;
        
        // 只有在沒有任何物品時才裝備空手
        // 注意：此時 availableItems 已經清空，所以會裝備空手
        if (useEmptyHands)
        {
            // 確保空手已初始化（如果 ClearAllItems 在 Start 之前被調用）
            if (emptyHands == null)
            {
                Debug.Log($"[ItemHolder] ClearAllItems: emptyHands is null, initializing now on {gameObject.name}");
                InitializeEmptyHands();
            }
            
            // 由於列表已清空，裝備空手
            if (emptyHands != null)
            {
                Debug.Log($"[ItemHolder] ClearAllItems: No items remaining, equipping empty hands on {gameObject.name}");
                EquipEmptyHands();
            }
            else
            {
                Debug.LogError($"[ItemHolder] ClearAllItems: Failed to initialize empty hands on {gameObject.name}!");
            }
        }
        else
        {
            Debug.Log($"[ItemHolder] ClearAllItems: useEmptyHands is false, not equipping empty hands");
        }
        
        Debug.Log($"[ItemHolder] ClearAllItems complete on {gameObject.name}. currentItem: {currentItem?.ItemName ?? "null"}");
    }
    
    /// <summary>
    /// 初始化空手狀態
    /// </summary>
    private void InitializeEmptyHands()
    {
        if (emptyHands != null) return; // 已經初始化過了
        
        // 創建一個空的 GameObject 來掛載 EmptyHands 組件
        GameObject emptyHandsGO = new GameObject("EmptyHands");
        emptyHandsGO.transform.SetParent(this.transform, false);
        emptyHandsGO.transform.localPosition = Vector3.zero;
        emptyHandsGO.transform.localRotation = Quaternion.identity;
        emptyHandsGO.transform.localScale = Vector3.one;
        
        // 添加 EmptyHands 組件
        emptyHands = emptyHandsGO.AddComponent<EmptyHands>();
        emptyHands.gameObject.SetActive(false); // 初始時隱藏
        
        Debug.Log($"[ItemHolder] Initialized EmptyHands for {gameObject.name}");
    }
    
    /// <summary>
    /// 裝備空手狀態
    /// </summary>
    private void EquipEmptyHands()
    {
        if (!useEmptyHands || emptyHands == null) return;
        
        // 卸下當前物品（如果有）
        if (currentItem != null && !(currentItem is EmptyHands))
        {
            currentItem.OnUnequip();
            currentItem.gameObject.SetActive(false);
        }
        
        // 裝備空手
        currentItem = emptyHands;
        currentItemIndex = -1; // 空手沒有索引
        emptyHands.gameObject.SetActive(true);
        emptyHands.OnEquip();
        
        // 觸發物品變更事件
        OnItemChanged?.Invoke(emptyHands);
        
        Debug.Log($"[ItemHolder] Equipped EmptyHands for {gameObject.name}");
    }
    
    /// <summary>
    /// 公開：嘗試裝備空手（若系統啟用空手且已初始化）。
    /// </summary>
    public bool TryEquipEmptyHands()
    {
        if (!useEmptyHands)
        {
            Debug.Log("[ItemHolder] TryEquipEmptyHands ignored - useEmptyHands=false");
            return false;
        }
        if (emptyHands == null)
        {
            InitializeEmptyHands();
            if (emptyHands == null) return false;
        }
        EquipEmptyHands();
        return true;
    }

    /// <summary>
    /// 檢查當前是否為空手狀態
    /// </summary>
    public bool IsEmptyHands()
    {
        return currentItem is EmptyHands;
    }
}

