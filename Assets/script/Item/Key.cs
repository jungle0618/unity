using UnityEngine;

/// <summary>
/// Key（鑰匙）類
/// 繼承自 Item，用於開啟需要特定鑰匙的門
/// </summary>
public class Key : Item
{
    [Header("鑰匙設定")]
    [SerializeField] private KeyType keyType = KeyType.Red;
    [Tooltip("鑰匙是否在使用後消失（單次使用）")]
    [SerializeField] private bool consumeOnUse = false;
    
    // 屬性
    public KeyType KeyType => keyType;
    public bool ConsumeOnUse => consumeOnUse;
    
    // 統計資訊
    private int useCount = 0;
    public int UseCount => useCount;
    
    /// <summary>
    /// 裝備時調用
    /// </summary>
    public override void OnEquip()
    {
        base.OnEquip();
        // 鑰匙裝備時顯示（像武器一樣）
        gameObject.SetActive(true);
        Debug.Log($"[Key] 裝備了 {keyType} 鑰匙");
    }
    
    /// <summary>
    /// 卸下時調用
    /// </summary>
    public override void OnUnequip()
    {
        base.OnUnequip();
        Debug.Log($"[Key] 卸下了 {keyType} 鑰匙");
    }
    
    /// <summary>
    /// 嘗試使用鑰匙開啟門（由鑰匙決定所有邏輯）
    /// </summary>
    /// <param name="requiredKeyType">門需要的鑰匙類型</param>
    /// <param name="shouldRemove">是否需要移除鑰匙（單次使用）</param>
    /// <returns>是否可以開啟門</returns>
    public bool TryUnlockDoor(KeyType requiredKeyType, out bool shouldRemove)
    {
        shouldRemove = false;
        
        // 不需要鑰匙的門不應該調用這個方法
        if (requiredKeyType == KeyType.None)
        {
            Debug.LogWarning($"[Key] 門不需要鑰匙，不應該調用 TryUnlockDoor");
            return false;
        }
        
        // 檢查鑰匙類型是否匹配
        bool canUnlock = false;
        
        // 萬能鑰匙可以開啟任何門
        if (keyType == KeyType.Master)
        {
            canUnlock = true;
        }
        // 鑰匙類型必須匹配
        else if (keyType == requiredKeyType)
        {
            canUnlock = true;
        }
        
        // 如果可以開啟
        if (canUnlock)
        {
            useCount++;
            shouldRemove = consumeOnUse; // 由鑰匙決定是否需要移除
            
            Debug.Log($"[Key] 使用 {keyType} 鑰匙開啟 {requiredKeyType} 門（第 {useCount} 次使用）{(shouldRemove ? " - 單次使用，將被移除" : " - 可重複使用")}");
            return true;
        }
        
        // 鑰匙類型不匹配
        Debug.LogWarning($"[Key] {keyType} 鑰匙無法開啟 {requiredKeyType} 門");
        return false;
    }
    
    /// <summary>
    /// 檢查是否可以開啟指定類型的門（不消耗使用次數，僅檢查）
    /// </summary>
    /// <param name="requiredKeyType">門需要的鑰匙類型</param>
    /// <returns>是否可以開啟</returns>
    public bool CanUnlock(KeyType requiredKeyType)
    {
        // 萬能鑰匙可以開啟任何門
        if (keyType == KeyType.Master)
            return true;
        
        // 不需要鑰匙的門任何鑰匙都無效
        if (requiredKeyType == KeyType.None)
            return false;
        
        // 鑰匙類型必須匹配
        return keyType == requiredKeyType;
    }
    
    /// <summary>
    /// 設定鑰匙類型（用於運行時更改）
    /// </summary>
    /// <param name="newKeyType">新的鑰匙類型</param>
    public void SetKeyType(KeyType newKeyType)
    {
        keyType = newKeyType;
        itemName = $"{keyType} 鑰匙";
        Debug.Log($"[Key] 鑰匙類型已設定為: {keyType}");
    }
    
    /// <summary>
    /// 重置使用次數
    /// </summary>
    public void ResetUseCount()
    {
        useCount = 0;
    }
    
    /// <summary>
    /// 獲取鑰匙描述
    /// </summary>
    /// <returns>鑰匙的描述文字</returns>
    public string GetDescription()
    {
        string description = $"{keyType} 鑰匙";
        
        if (keyType == KeyType.Master)
        {
            description += "（萬能鑰匙）";
        }
        
        if (consumeOnUse)
        {
            description += " - 單次使用";
        }
        else
        {
            description += " - 可重複使用（無限次）";
        }
        
        if (useCount > 0)
        {
            description += $"\n已使用 {useCount} 次";
        }
        
        return description;
    }
}

