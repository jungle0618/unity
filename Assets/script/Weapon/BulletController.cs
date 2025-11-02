using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("子彈設定")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float lifetime = 3f; // 子彈存活時間
    [SerializeField] private float speed = 20f;
    
    private GameObject attacker;
    private float spawnTime;
    
    private void Start()
    {
        spawnTime = Time.time;
        
        // 設定子彈速度
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }
    }
    
    private void Update()
    {
        // 檢查子彈存活時間
        if (Time.time - spawnTime > lifetime)
        {
            DestroyBullet();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 跳過攻擊者本人
        if (other.gameObject == attacker) return;
        
        Debug.Log($"子彈擊中: {other.gameObject.name}");
        
        // 檢查是否擊中敵人
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            Debug.Log($"子彈擊中敵人: {enemy.gameObject.name}");
            enemy.Die();
            DestroyBullet();
            return;
        }
        
        // 檢查是否擊中玩家
        var player = other.GetComponent<Player>();
        if (player != null)
        {
            Debug.Log($"子彈擊中玩家: {player.gameObject.name}");
            
            // 檢查攻擊者是否是敵人
            var enemyAttacker = attacker?.GetComponent<Enemy>();
            if (enemyAttacker != null)
            {
                player.TakeDamage(damage, "Enemy Bullet Attack");
                Debug.Log($"敵人子彈對玩家造成 {damage} 點傷害");
            }
            else
            {
                // 玩家射擊玩家（friendly fire）
                Debug.Log("玩家子彈擊中玩家 - 可能是 friendly fire");
            }
            
            DestroyBullet();
            return;
        }
        
        // 檢查是否擊中牆壁或其他障礙物
        if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Debug.Log($"子彈擊中障礙物: {other.gameObject.name}");
            DestroyBullet();
            return;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 處理碰撞
        OnTriggerEnter2D(collision.collider);
    }
    
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }
    
    public void SetAttacker(GameObject newAttacker)
    {
        attacker = newAttacker;
    }
    
    private void DestroyBullet()
    {
        // 可以在這裡添加子彈爆炸效果
        Destroy(gameObject);
    }
}
