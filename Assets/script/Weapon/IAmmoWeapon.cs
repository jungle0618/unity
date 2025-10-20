using UnityEngine;

/// <summary>
/// Interface for weapons that use ammunition
/// </summary>
public interface IAmmoWeapon
{
    int CurrentAmmo { get; }
    int MaxAmmo { get; }
    int AmmoPerShot { get; }
    bool HasAmmo { get; }
    void Reload(int amount);
    void FullReload();
}

