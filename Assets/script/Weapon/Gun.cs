using System;
using UnityEngine;

/// <summary>
/// Gun weapon that shoots bullets at enemies within range
/// Implements IAmmoWeapon for ammunition system
/// </summary>
public class Gun : Weapon, IAmmoWeapon
{
    [Header("Gun Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private Transform firePoint; // Optional: spawn point for bullets
    
    [Header("Ammunition")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int ammoPerShot = 1;
    private int _currentAmmo;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash; // Optional: muzzle flash effect

    // IAmmoWeapon implementation
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => maxAmmo;
    public int AmmoPerShot => ammoPerShot;
    public bool HasAmmo => _currentAmmo >= ammoPerShot;

    // Events for ammo changes
    public event Action<int, int> OnAmmoChanged; // current, max

    protected override void Awake()
    {
        base.Awake();
        
        // Initialize gun stats
        attackRange = 0f; // Guns should not have melee range, bullets will handle range
        attackCooldown = 0.5f; // Fire rate
        maxDurability = 100;
        durabilityLossPerAttack = 1;
        
        // Initialize ammo
        _currentAmmo = maxAmmo;
        Debug.Log("[Gun] Initialized with ammo: " + _currentAmmo + "/" + maxAmmo);
    }

    public override bool CanAttack()
    {
        return base.CanAttack() && HasAmmo;
    }

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        // Check if we have ammo
        if (!HasAmmo)
        {
            Debug.Log("[Gun] Out of ammo!");
            return;
        }

        // Consume ammo
        _currentAmmo -= ammoPerShot;
        OnAmmoChanged?.Invoke(_currentAmmo, maxAmmo);

        // Get shooting direction from gun's rotation
        Vector2 shootDirection = transform.right; // Gun should be rotated to face target

        // Spawn bullet
        SpawnBullet(origin, shootDirection, attacker);

        // Play muzzle flash if available
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        Debug.Log($"[Gun] Fired! Ammo: {_currentAmmo}/{maxAmmo}");
    }

    private void SpawnBullet(Vector2 origin, Vector2 direction, GameObject owner)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("[Gun] Bullet prefab not assigned!");
            return;
        }

        // Determine spawn position (use firePoint if available, otherwise use gun position)
        var spawnPosition = firePoint != null ? firePoint.position : (Vector3)origin;

        // Instantiate bullet
        var bulletObj = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        
        // Initialize bullet
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(direction, owner);
        }
        else
        {
            Debug.LogError("[Gun] Bullet prefab doesn't have Bullet component!");
            Destroy(bulletObj);
        }
    }

    /// <summary>
    /// Reload ammunition
    /// </summary>
    public void Reload(int amount)
    {
        if (amount <= 0) return;

        int oldAmmo = _currentAmmo;
        _currentAmmo = Mathf.Min(maxAmmo, _currentAmmo + amount);
        
        OnAmmoChanged?.Invoke(_currentAmmo, maxAmmo);
        Debug.Log($"[Gun] Reloaded {_currentAmmo - oldAmmo} bullets. Total: {_currentAmmo}/{maxAmmo}");
    }

    /// <summary>
    /// Fully reload ammunition
    /// </summary>
    public void FullReload()
    {
        _currentAmmo = maxAmmo;
        OnAmmoChanged?.Invoke(_currentAmmo, maxAmmo);
        Debug.Log($"[Gun] Fully reloaded! Ammo: {_currentAmmo}/{maxAmmo}");
    }

    /// <summary>
    /// Get ammo information
    /// </summary>
    public (int current, int max, float percentage) GetAmmoInfo()
    {
        float percentage = maxAmmo > 0 ? (float)_currentAmmo / maxAmmo : 0f;
        return (_currentAmmo, maxAmmo, percentage);
    }

    // Override to add ammo check warning
    public new bool TryPerformAttack(Vector2 origin, GameObject attacker)
    {
        if (!HasAmmo)
        {
            Debug.LogWarning("[Gun] Cannot attack - out of ammo!");
            return false;
        }

        return base.TryPerformAttack(origin, attacker);
    }
}

