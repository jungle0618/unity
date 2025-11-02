using System;
using UnityEngine;

/// <summary>
/// 遠程武器基類
/// 所有遠程武器（如槍、弓、弩等）都應繼承此類
/// 遠程武器的 attackRange 代表射程/子彈飛行距離
/// </summary>
public abstract class RangedWeapon : Weapon, IAmmoWeapon
{
    [Header("Ranged Settings")]
    [SerializeField] protected float attackRange = 20f; // 射程
    
    [Header("Ammunition")]
    [SerializeField] protected int startingAmmo = 30; // 開局子彈數
    protected int _currentAmmo;

    public float AttackRange => attackRange;
    public override WeaponType Type => WeaponType.Ranged;

    // IAmmoWeapon implementation
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => int.MaxValue; // 無限大
    public int AmmoPerShot => 1; // 固定為1
    public bool HasAmmo => _currentAmmo > 0;

    // Events for ammo changes
    public event Action<int, int> OnAmmoChanged; // current, max

    protected override void Awake()
    {
        base.Awake();
        
        // Initialize ammo
        _currentAmmo = startingAmmo;
        Debug.Log($"[{GetType().Name}] Initialized with ammo: {_currentAmmo}");
    }

    public override bool CanAttack()
    {
        return base.CanAttack() && HasAmmo;
    }

    /// <summary>
    /// 消耗彈藥（每次固定消耗1發）
    /// </summary>
    protected void ConsumeAmmo()
    {
        if (_currentAmmo > 0)
        {
            _currentAmmo--;
            OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
        }
    }

    /// <summary>
    /// 補充彈藥
    /// </summary>
    public void Reload(int amount)
    {
        if (amount <= 0) return;

        int oldAmmo = _currentAmmo;
        _currentAmmo += amount;
        
        OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
        Debug.Log($"[{GetType().Name}] Reloaded {amount} bullets. Total: {_currentAmmo}");
    }

    /// <summary>
    /// 完全補滿彈藥（回到開局數量）
    /// </summary>
    public void FullReload()
    {
        _currentAmmo = startingAmmo;
        OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
        Debug.Log($"[{GetType().Name}] Fully reloaded! Ammo: {_currentAmmo}");
    }

    /// <summary>
    /// 取得彈藥資訊
    /// </summary>
    public (int current, int max, float percentage) GetAmmoInfo()
    {
        // 因為沒有 maxAmmo，用 startingAmmo 作為參考
        float percentage = startingAmmo > 0 ? Mathf.Min(1f, (float)_currentAmmo / startingAmmo) : 1f;
        return (_currentAmmo, MaxAmmo, percentage);
    }
}

