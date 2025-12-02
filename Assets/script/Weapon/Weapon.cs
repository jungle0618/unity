using System;
using UnityEngine;

/// <summary>
/// 武器類型
/// </summary>
public enum WeaponType
{
    Melee,  // 近戰
    Ranged  // 遠程
}

/// <summary>
/// Weapon（武器類別）
/// 繼承自 Item，是所有武器的基類
/// </summary>
public abstract class Weapon : Item
{
    [Header("Attack Settings")]
    [SerializeField] protected float attackCooldown = 0.5f;
    [SerializeField] protected int attackDamage = 1;
    
    [Header("Equip Settings")]
    [Tooltip("掏出武器後需要等待多久才能攻擊")]
    [SerializeField] protected float equipDelay = 0.3f; // 掏槍延遲
    
    [Header("Durability Settings")]
    [SerializeField] protected int maxDurability = 100;
    
    protected float lastAttackTime = -999f;
    protected float equipTime = -999f; // 記錄裝備時間
    protected int currentDurability;
    
    public abstract WeaponType Type { get; }
    public int MaxDurability => maxDurability;
    public int CurrentDurability => currentDurability;
    public float DurabilityPercentage => maxDurability > 0 ? (float)currentDurability / maxDurability : 1f;
    public bool IsBroken => currentDurability <= 0;
    public int AttackDamage => attackDamage;
    
    /// <summary>
    /// 武器是否已準備好（裝備延遲已過）
    /// </summary>
    public bool IsReady => Time.time >= equipTime + equipDelay;
    
    /// <summary>
    /// 裝備後剩餘的等待時間
    /// </summary>
    public float RemainingEquipTime => Mathf.Max(0, equipTime + equipDelay - Time.time);
    
    /// <summary>
    /// 攻擊冷卻剩餘時間
    /// </summary>
    public float RemainingAttackCooldown => Mathf.Max(0, lastAttackTime + attackCooldown - Time.time);
    
    /// <summary>
    /// 攻擊冷卻總時間
    /// </summary>
    public float AttackCooldownDuration => attackCooldown;
    
    /// <summary>
    /// 裝備延遲總時間
    /// </summary>
    public float EquipDelayDuration => equipDelay;

    public event Action<GameObject> OnAttackPerformed; // 攻擊事件
    public event Action<int, int> OnDurabilityChanged; // 當前耐久度, 最大耐久度
    public event Action OnWeaponBroken; // 武器損壞事件
    
    // New equipment events
    public event Action OnEquipped; // 武器裝備時
    public event Action OnUnequipped; // 武器卸下時
    public event Action OnBecameReady; // 裝備延遲結束，武器就緒

    private bool hasFireReadyEvent = false;

    protected virtual void Awake()
    {
        currentDurability = maxDurability;
    }

    protected virtual void Update()
    {
        // Check if weapon just became ready
        if (!hasFireReadyEvent && IsReady)
        {
            OnBecameReady?.Invoke();
            hasFireReadyEvent = true;
        }
    }
    
    /// <summary>
    /// 裝備武器時調用
    /// </summary>
    public override void OnEquip()
    {
        base.OnEquip();
        // 記錄裝備時間，用於計算掏槍延遲
        equipTime = Time.time;
        hasFireReadyEvent = false; // Reset ready event flag
        OnEquipped?.Invoke();
    }
    
    /// <summary>
    /// 卸下武器時調用
    /// </summary>
    public override void OnUnequip()
    {
        base.OnUnequip();
        OnUnequipped?.Invoke();
    }
    
    /// <summary>
    /// 更新武器方向
    /// </summary>
    /// <param name="direction">方向向量</param>
    public override void UpdateDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 檢查是否可以攻擊
    /// 條件：1) 攻擊冷卻完成 2) 裝備延遲完成 3) 武器未損壞
    /// </summary>
    public virtual bool CanAttack() 
    {
        return Time.time >= lastAttackTime + attackCooldown 
            && Time.time >= equipTime + equipDelay  // 新增：檢查裝備延遲
            && !IsBroken;
    }

    public bool TryPerformAttack(Vector2 origin, GameObject attacker)
    {
        if (!CanAttack()) return false;
        lastAttackTime = Time.time;
        PerformAttack(origin, attacker);
        
        // 減少耐久度（統一每次消耗1）
        ReduceDurability(1);
        
        OnAttackPerformed?.Invoke(attacker);
        return true;
    }

    protected abstract void PerformAttack(Vector2 origin, GameObject attacker);

    /// <summary>
    /// 減少武器耐久度
    /// </summary>
    /// <param name="amount">減少的數量</param>
    public virtual void ReduceDurability(int amount)
    {

        if (amount <= 0) return;
        
        int oldDurability = currentDurability;
        currentDurability = Mathf.Max(0, currentDurability - amount);
        // 觸發耐久度變化事件
        OnDurabilityChanged?.Invoke(currentDurability, maxDurability);
        
        // 如果武器損壞，觸發損壞事件並銷毀武器
        if (oldDurability > 0 && currentDurability <= 0)
        {
            OnWeaponBroken?.Invoke();
            
            // 延遲銷毀，讓事件處理完成
            Destroy(gameObject, 0.1f);
        }
    }

    /// <summary>
    /// 修復武器耐久度
    /// </summary>
    /// <param name="amount">修復的數量</param>
    public virtual void RepairDurability(int amount)
    {
        if (amount <= 0) return;
        
        int oldDurability = currentDurability;
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
        
        // 觸發耐久度變化事件
        OnDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }

    /// <summary>
    /// 完全修復武器
    /// </summary>
    public virtual void FullRepair()
    {
        currentDurability = maxDurability;
        OnDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }

    /// <summary>
    /// 設定武器耐久度
    /// </summary>
    /// <param name="durability">新的耐久度值</param>
    public virtual void SetDurability(int durability)
    {
        currentDurability = Mathf.Clamp(durability, 0, maxDurability);
        OnDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }
}