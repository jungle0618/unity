using UnityEngine;

/// <summary>
/// EmptyHands（空手狀態）
/// 表示實體沒有裝備任何物品時的狀態
/// 這是一個特殊的 Item，不能攻擊也沒有功能，僅作為佔位符
/// </summary>
public class EmptyHands : Item
{
    private void Awake()
    {
        // 設定預設名稱（如果沒有在 Inspector 中設定）
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = "空手";
        }
    }
    
    /// <summary>
    /// 裝備空手時調用
    /// </summary>
    public override void OnEquip()
    {
        base.OnEquip();
        // 空手不需要特殊的裝備邏輯
    }
    
    /// <summary>
    /// 卸下空手時調用
    /// </summary>
    public override void OnUnequip()
    {
        base.OnUnequip();
        // 空手不需要特殊的卸下邏輯
    }
    
    /// <summary>
    /// 空手不需要更新方向
    /// </summary>
    public override void UpdateDirection(Vector2 direction)
    {
        // 空手不需要旋轉
    }
}

