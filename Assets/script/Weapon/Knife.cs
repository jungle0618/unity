using UnityEngine;

/// <summary>
/// 刀子 - 近戰武器
/// 特性：無限耐久度
/// </summary>
public class Knife : MeleeWeapon
{
    /// <summary>
    /// 覆寫減少耐久度方法以實現無限耐久度
    /// 刀子永不損壞
    /// </summary>
    public override void ReduceDurability(int amount)
    {
        // 刀子具有無限耐久度，不減少耐久度
    }

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        // 注意：傷害處理已移至 EntityManager.HandleAttack 統一處理
        // 此方法只負責視覺效果和碰撞檢測（如果需要）
        // 實際傷害由 ItemHolder.OnAttackPerformed 事件觸發 EntityManager.HandleAttack 處理
        
        // 可以在此處添加攻擊動畫、音效等視覺效果
        Debug.Log($"[Knife] PerformAttack at {origin} by {attacker.name}");
    }
}