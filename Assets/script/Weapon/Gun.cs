using UnityEngine;

/// <summary>
/// 槍械 - 遠程武器
/// 射程由 attackRange 決定（也用於計算子彈飛行時間）
/// </summary>
public class Gun : RangedWeapon
{
    [Header("Gun Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private Transform firePoint; // Optional: spawn point for bullets

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash; // Optional: muzzle flash effect

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        // Check if we have ammo
        if (!HasAmmo)
        {
            Debug.Log("[Gun] Out of ammo!");
            return;
        }

        // Consume ammo (固定消耗1發)
        ConsumeAmmo();

        // Get shooting direction from gun's rotation
        Vector2 shootDirection = transform.right; // Gun should be rotated to face target

        // Spawn bullet
        SpawnBullet(origin, shootDirection, attacker);

        // Play muzzle flash if available
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        Debug.Log($"[Gun] Fired! Ammo: {_currentAmmo}");
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
        
        // Initialize bullet (由 Gun 統一設定所有參數)
        // lifetime 由射程和速度計算：lifetime = attackRange / bulletSpeed
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            float bulletLifetime = attackRange / bulletSpeed;
            bullet.Initialize(direction, owner, attackDamage, bulletSpeed, bulletLifetime);
        }
        else
        {
            Debug.LogError("[Gun] Bullet prefab doesn't have Bullet component!");
            Destroy(bulletObj);
        }
    }

}

