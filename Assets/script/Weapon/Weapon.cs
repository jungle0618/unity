using System;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float attackCooldown = 0.5f;
    [SerializeField] protected int maxDurability = 100;
    [SerializeField] protected int durabilityLossPerAttack = 1;
    
    protected float lastAttackTime = -999f;
    protected int currentDurability;
    
    public float AttackRange => attackRange;
    public int MaxDurability => maxDurability;
    public int CurrentDurability => currentDurability;
    public float DurabilityPercentage => maxDurability > 0 ? (float)currentDurability / maxDurability : 1f;
    public bool IsBroken => currentDurability <= 0;

    public event Action<Vector2, float, GameObject> OnAttackPerformed; // 加上 attacker
    public event Action<int, int> OnDurabilityChanged; // 當前耐久度, 最大耐久度
    public event Action OnWeaponBroken; // 武器損壞事件

    protected virtual void Awake()
    {
        currentDurability = maxDurability;
    }

    public bool CanAttack() => Time.time >= lastAttackTime + attackCooldown && !IsBroken;

    public bool TryPerformAttack(Vector2 origin, GameObject attacker)
    {
        
        Debug.Log("TryPerformAttack");
        Debug.Log("CanAttack: " + CanAttack());
        Debug.Log("lastAttackTime: " + lastAttackTime);
        Debug.Log("attackCooldown: " + attackCooldown);
        Debug.Log("IsBroken: " + IsBroken);
        Debug.Log("Time.time: " + Time.time);
        Debug.Log("Time.time - lastAttackTime: " + (Time.time - lastAttackTime));
        Debug.Log("Time.time - lastAttackTime < attackCooldown: " + (Time.time - lastAttackTime < attackCooldown));
        if (!CanAttack()) return false;
        lastAttackTime = Time.time;
        PerformAttack(origin, attacker);
        
        // 減少耐久度
        ReduceDurability(durabilityLossPerAttack);
        
        OnAttackPerformed?.Invoke(origin, attackRange, attacker);
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
        
        // 如果武器損壞，觸發損壞事件
        if (oldDurability > 0 && currentDurability <= 0)
        {
            OnWeaponBroken?.Invoke();
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