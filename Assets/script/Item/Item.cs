using UnityEngine;

/// <summary>
/// Item（物品基類）
/// 比 Weapon 更抽象的類別，作為所有可裝備物品的基類
/// </summary>
public abstract class Item : MonoBehaviour
{
    [SerializeField] protected string itemName = "物品";
    [SerializeField] protected Sprite itemIcon;
    
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    
    /// <summary>
    /// 裝備時調用
    /// </summary>
    public virtual void OnEquip()
    {
        // 子類別可以覆寫此方法
    }
    
    /// <summary>
    /// 卸下時調用
    /// </summary>
    public virtual void OnUnequip()
    {
        // 子類別可以覆寫此方法
    }
    
    /// <summary>
    /// 更新物品方向（用於武器等需要旋轉的物品）
    /// </summary>
    /// <param name="direction">方向向量</param>
    public virtual void UpdateDirection(Vector2 direction)
    {
        // 子類別可以覆寫此方法（例如武器需要旋轉）
    }
}
