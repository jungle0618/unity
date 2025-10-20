using UnityEngine;

public class Gun : Weapon
{
    [Header("槍械設定")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    
    protected override void Awake()
    {
        base.Awake(); // 調用父類的Awake方法來初始化耐久度
        attackRange = 10f; // 槍的射程較遠
        attackCooldown = 0.5f; // 射擊間隔
        maxDurability = 100; // 槍的耐久度
        durabilityLossPerAttack = 1; // 每次射擊減少1點耐久度
        
        // 如果沒有設定發射點，使用武器本身的位置
        if (firePoint == null)
            firePoint = transform;
    }

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        // 計算射擊方向
        Vector2 shootDirection = GetShootDirection(origin, attacker);
        
        // 創建子彈
        CreateBullet(origin, shootDirection, attacker);
        
        Debug.Log($"槍械射擊: 方向 {shootDirection}, 攻擊者 {attacker.name}");
    }
    
    private Vector2 GetShootDirection(Vector2 origin, GameObject attacker)
    {
        // 如果是玩家射擊，使用滑鼠方向
        var playerController = attacker.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // 獲取滑鼠世界位置
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0f;
                return ((Vector2)mouseWorldPos - origin).normalized;
            }
        }
        
        // 如果是敵人射擊，朝向玩家
        var enemy = attacker.GetComponent<Enemy>();
        if (enemy != null)
        {
            // 尋找最近的玩家
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                return ((Vector2)player.transform.position - origin).normalized;
            }
        }
        
        // 預設方向（向右）
        return Vector2.right;
    }
    
    private void CreateBullet(Vector2 origin, Vector2 direction, GameObject attacker)
    {
        // 如果沒有子彈預製體，使用射線檢測
        if (bulletPrefab == null)
        {
            PerformRaycastAttack(origin, direction, attacker);
            return;
        }
        
        // 創建子彈物件
        GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
        
        // 設定子彈方向
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        
        // 設定子彈速度
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * bulletSpeed;
        }
        
        // 設定子彈傷害和攻擊者
        BulletController bulletController = bullet.GetComponent<BulletController>();
        if (bulletController != null)
        {
            bulletController.SetDamage(damage);
            bulletController.SetAttacker(attacker);
        }
    }
    
    private void PerformRaycastAttack(Vector2 origin, Vector2 direction, GameObject attacker)
    {
        // 使用射線檢測進行攻擊
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, attackRange);
        
        if (hit.collider != null)
        {
            Debug.Log($"槍械射線擊中: {hit.collider.gameObject.name}");
            
            // 檢查是否擊中敵人
            var enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log($"槍械擊中敵人: {enemy.gameObject.name}");
                enemy.Die();
                return;
            }
            
            // 檢查是否擊中玩家
            var player = hit.collider.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log($"槍械擊中玩家: {player.gameObject.name}");
                
                // 檢查攻擊者是否是敵人
                var enemyAttacker = attacker.GetComponent<Enemy>();
                if (enemyAttacker != null)
                {
                    player.TakeDamage(damage, "Enemy Gun Attack");
                    Debug.Log($"敵人對玩家造成 {damage} 點傷害");
                }
            }
        }
        
        // 繪製射線（除錯用）
        Debug.DrawRay(origin, direction * attackRange, Color.red, 0.1f);
    }
}
