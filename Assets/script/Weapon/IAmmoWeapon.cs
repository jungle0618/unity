using System;

/// <summary>
/// 彈藥武器介面
/// 用於所有需要彈藥的遠程武器
/// </summary>
public interface IAmmoWeapon
{
    int CurrentAmmo { get; }
    int MaxAmmo { get; } // 固定為 int.MaxValue（無限大）
    int AmmoPerShot { get; } // 固定為 1
    bool HasAmmo { get; }
    
    void Reload(int amount);
    void FullReload();
    
    event Action<int, int> OnAmmoChanged; // current, max
}

