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

    [Header("Alert Settings")]
    [Tooltip("開槍時會警報這個範圍內的所有敵人")]
    [SerializeField] private float alertRange = 15f;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash; // Optional: muzzle flash effect

    [Header("Movement Jitter (Player Only)")]
    [Tooltip("是否啟用移動時的射擊抖動")]
    [SerializeField] private bool enableMovementJitter = true;
    [Tooltip("最大抖動角度（度）")]
    [SerializeField] private float maxJitterAngle = 5f;
    [Tooltip("玩家移動速度閾值（超過此速度才有抖動）")]
    [SerializeField] private float jitterSpeedThreshold = 0.1f;

    // 公開屬性供 GunTrajectoryGuide 使用
    public bool EnableMovementJitter => enableMovementJitter;
    public float MaxJitterAngle => maxJitterAngle;
    public float JitterSpeedThreshold => jitterSpeedThreshold;

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        // 敵人的槍不檢查彈藥（無限彈藥）
        if (!IsEquippedByEnemy())
        {
            // 玩家需要檢查彈藥
            if (!HasAmmo)
            {
                Debug.Log("[Gun] Out of ammo!");
                return;
            }

            // 消耗彈藥（固定消耗1發）
            ConsumeAmmo();
        }

        // Get shooting direction from gun's rotation
        Vector2 shootDirection = transform.right; // Gun should be rotated to face target

        // Apply movement jitter if player is moving (only for players, not enemies)
        if (enableMovementJitter && !IsEquippedByEnemy() && attacker != null)
        {
            shootDirection = ApplyMovementJitter(shootDirection, attacker);
        }

        // Spawn bullet
        SpawnBullet(origin, shootDirection, attacker);

        // Play muzzle flash if available
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Alert nearby enemies when player shoots
        AlertNearbyEnemies(origin, attacker);

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

    /// <summary>
    /// 警報附近的敵人（開槍時呼叫）
    /// </summary>
    private void AlertNearbyEnemies(Vector2 shootPosition, GameObject attacker)
    {
        // 只有當攻擊者是玩家時才警報敵人
        if (attacker == null || !attacker.CompareTag("Player"))
        {
            return;
        }

        // 尋找 EntityManager 並呼叫警報方法
        EntityManager entityManager = GameObject.FindFirstObjectByType<EntityManager>();
        if (entityManager != null)
        {
            entityManager.AlertNearbyEnemies(shootPosition, alertRange);
            Debug.Log($"[Gun] Alerted enemies within {alertRange} units");
        }
        else
        {
            Debug.LogWarning("[Gun] EntityManager not found - cannot alert enemies");
        }
    }

    /// <summary>
    /// 覆寫減少耐久度方法
    /// 敵人的槍具有無限耐久度，玩家的槍正常消耗
    /// </summary>
    public override void ReduceDurability(int amount)
    {
        // 檢查武器是否由敵人裝備（通過檢查父物件的標籤或組件）
        if (IsEquippedByEnemy())
        {
            // 敵人的槍具有無限耐久度，不減少耐久度
            return;
        }

        // 玩家的槍正常減少耐久度
        base.ReduceDurability(amount);
    }

    /// <summary>
    /// 檢查武器是否由敵人裝備
    /// </summary>
    private bool IsEquippedByEnemy()
    {
        // 向上查找父物件，檢查是否有 Enemy 標籤或 Enemy 組件
        Transform current = transform.parent;
        while (current != null)
        {
            // 檢查標籤
            if (current.CompareTag("Enemy"))
            {
                return true;
            }

            if (current.CompareTag("Player"))
            {
                return false;
            }

            // 檢查是否有 Enemy 組件（使用反射避免直接引用 Enemy 類）
            if (current.GetComponent("Enemy") != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// 應用移動抖動到射擊方向
    /// 當玩家移動時，子彈會有輕微的隨機偏移
    /// </summary>
    private Vector2 ApplyMovementJitter(Vector2 originalDirection, GameObject attacker)
    {
        // 獲取玩家的移動速度
        Rigidbody2D rb = attacker.GetComponent<Rigidbody2D>();
        if (rb == null) return originalDirection;

        float currentSpeed = rb.linearVelocity.magnitude;

        // 如果速度低於閾值，不應用抖動
        if (currentSpeed < jitterSpeedThreshold)
        {
            return originalDirection;
        }

        // 計算抖動角度（直接使用最大抖動角度範圍）
        float jitterAngle = Random.Range(-maxJitterAngle, maxJitterAngle);

        // 將原始方向旋轉一個隨機角度
        float currentAngle = Mathf.Atan2(originalDirection.y, originalDirection.x) * Mathf.Rad2Deg;
        float newAngle = currentAngle + jitterAngle;
        float newAngleRad = newAngle * Mathf.Deg2Rad;

        Vector2 jitteredDirection = new Vector2(Mathf.Cos(newAngleRad), Mathf.Sin(newAngleRad));
        return jitteredDirection.normalized;
    }

    /// <summary>
    /// 獲取當前玩家的移動速度（供 GunTrajectoryGuide 使用）
    /// </summary>
    public float GetPlayerMovementSpeed()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                Rigidbody2D rb = current.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    return rb.linearVelocity.magnitude;
                }
            }
            current = current.parent;
        }
        return 0f;
    }

}

