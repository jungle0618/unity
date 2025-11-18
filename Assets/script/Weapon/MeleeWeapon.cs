using UnityEngine;

/// <summary>
/// 近戰武器基類
/// 所有近戰武器（如刀、劍、斧頭等）都應繼承此類
/// </summary>
public abstract class MeleeWeapon : Weapon
{
    [Header("Melee Settings")]
    [SerializeField] protected float attackRange = 2.5f;
    
    public float AttackRange => attackRange;
    public override WeaponType Type => WeaponType.Melee;
}

