using System;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float attackCooldown = 0.5f;
    protected float lastAttackTime = -999f;
    public float AttackRange => attackRange;

    public event Action<Vector2, float, GameObject> OnAttackPerformed; // ¥[¤W attacker

    public bool CanAttack() => Time.time >= lastAttackTime + attackCooldown;

    public bool TryPerformAttack(Vector2 origin, GameObject attacker)
    {
        if (!CanAttack()) return false;
        lastAttackTime = Time.time;
        PerformAttack(origin, attacker);
        OnAttackPerformed?.Invoke(origin, attackRange, attacker);
        return true;
    }

    protected abstract void PerformAttack(Vector2 origin, GameObject attacker);
}


