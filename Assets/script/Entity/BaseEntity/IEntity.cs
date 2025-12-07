using UnityEngine;

/// <summary>
/// 實體通用接口
/// 用於統一訪問所有實體類型（Player, Enemy, Target 等）
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 實體位置
    /// </summary>
    Vector2 Position { get; }
    
    /// <summary>
    /// 是否已死亡
    /// </summary>
    bool IsDead { get; }
    
    /// <summary>
    /// 實體的 GameObject
    /// </summary>
    GameObject gameObject { get; }
    
    /// <summary>
    /// 造成傷害
    /// </summary>
    /// <param name="damage">傷害值</param>
    /// <param name="source">傷害來源</param>
    /// <param name="attackerPosition">攻擊者位置（用於視野檢測，可選）</param>
    void TakeDamage(int damage, string source = "", Vector2? attackerPosition = null);
    
    /// <summary>
    /// 獲取實體類型
    /// </summary>
    EntityManager.EntityType GetEntityType();
}

