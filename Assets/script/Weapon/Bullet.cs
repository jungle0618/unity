using UnityEngine;

/// <summary>
/// Bullet projectile that travels in a direction and damages enemies
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    private float speed;
    private float lifetime;
    private int damage;
    
    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers = ~0; // All layers by default
    [SerializeField] private float raycastCheckDistance = 0.5f; // Distance to check ahead

    private Vector2 _direction;
    private GameObject _owner;
    private float _spawnTime;
    private bool _hasHitTarget = false;

    public Vector2 Direction => _direction;
    // Event for VFX on hit
    public event System.Action<GameObject, Bullet> OnBulletHit;

    public void Initialize(Vector2 shootDirection, GameObject shooter, int bulletDamage, float bulletSpeed, float bulletLifetime)
    {
        _direction = shootDirection.normalized;
        _owner = shooter;
        _spawnTime = Time.time;
        
        // 由 Gun 統一設定所有參數
        damage = bulletDamage;
        speed = bulletSpeed;
        lifetime = bulletLifetime;

        // Rotate bullet to face direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        
        //Debug.Log($"[Bullet] Initialized at {transform.position}, direction: {_direction}, speed: {speed}, damage: {damage}");
        
        // Check if bullet has visual component
        if (GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogWarning("[Bullet] No SpriteRenderer found! Bullet will be invisible. Add a SpriteRenderer component.");
        }
    }

    private void Update()
    {
        if (_hasHitTarget)
            return;

        // Store previous position for raycast
        Vector2 currentPosition = transform.position;
        
        // Calculate movement for this frame
        float moveDistance = speed * Time.deltaTime;
        Vector2 moveVector = _direction * moveDistance;
        
        // Update rotation to always face movement direction
        if (_direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        // Perform raycast to check for collisions before moving
        // Important: Start raycast slightly ahead to avoid hitting self
        RaycastHit2D hit = Physics2D.Raycast(
            currentPosition + _direction * 0.1f, // Start slightly ahead
            _direction, 
            moveDistance + raycastCheckDistance,
            collisionLayers
        );
        
        if (hit.collider != null)
        {
            // Don't hit the owner or self
            if (hit.collider.gameObject != _owner && hit.collider.gameObject != gameObject)
            {
                //Debug.Log($"[Bullet] Raycast detected collision with: {hit.collider.gameObject.name} at distance {hit.distance}");
                
                // IMPORTANT: Disable the bullet's collider immediately to prevent OnTriggerEnter2D from firing
                var bulletCollider = GetComponent<Collider2D>();
                if (bulletCollider != null)
                {
                    bulletCollider.enabled = false;
                }
                
                // Move to hit point
                transform.position = hit.point;
                
                // Handle the collision
                HandleCollision(hit.collider);
                return;
            }
        }
        
        // Move bullet if no collision detected
        transform.position = currentPosition + moveVector;

        // Destroy after lifetime
        if (Time.time >= _spawnTime + lifetime)
        {
            //Debug.Log("[Bullet] Destroyed after lifetime expired");
            Destroy(gameObject);
        }
    }

    private void HandleCollision(Collider2D collision)
    {
        if (_hasHitTarget)
            return;
            
        _hasHitTarget = true;
        
        //Debug.Log($"[Bullet] HandleCollision called for: {collision.gameObject.name}");

        // Check if hit an enemy
        var enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            // 檢查攻擊者類型
            var playerAttacker = _owner.GetComponent<Player>();
            var enemyAttacker = _owner.GetComponent<Enemy>();
            var targetAttacker = _owner.GetComponent<Target>();
            
            if (playerAttacker != null)
            {
                // Player 可以攻擊 Enemy
                //Debug.Log($"[Bullet] Player shot Enemy for {damage} damage");
                Vector2 attackerPos = _owner != null ? (Vector2)_owner.transform.position : transform.position;
                enemy.TakeDamage(damage, "Player Gun", attackerPos);
                OnBulletHit?.Invoke(enemy.gameObject, this);
            }
            else if (enemyAttacker != null)
            {
                // Enemy 不能攻擊 Enemy（同類型），只銷毀子彈
                //Debug.Log($"[Bullet] Enemy bullet hit Enemy - no damage (Enemy cannot attack Enemy)");
            }
            else if (targetAttacker != null)
            {
                // Target 不能攻擊 Enemy，只銷毀子彈
                //Debug.Log($"[Bullet] Target bullet hit Enemy - no damage (Target cannot attack Enemy)");
            }
            else
            {
                //Debug.Log($"[Bullet] Hit Enemy: {enemy.gameObject.name}");
            }
            Destroy(gameObject);
            return;
        }

        // Check if hit a player
        var player = collision.GetComponent<Player>();
        if (player != null)
        {
            // 檢查攻擊者類型
            var playerAttacker = _owner.GetComponent<Player>();
            var enemyAttacker = _owner.GetComponent<Enemy>();
            var targetAttacker = _owner.GetComponent<Target>();
            
            if (playerAttacker != null)
            {
                // Player 不能攻擊 Player（同類型），只銷毀子彈
                //Debug.Log($"[Bullet] Player bullet hit Player - no damage (Player cannot attack Player)");
            }
            else if (enemyAttacker != null)
            {
                // Enemy 可以攻擊 Player
                //Debug.Log($"[Bullet] Enemy {enemyAttacker.gameObject.name} shot player for {damage} damage");
                Vector2 attackerPos = _owner != null ? (Vector2)_owner.transform.position : transform.position;
                player.TakeDamage(damage, "Enemy Gun", attackerPos);
                OnBulletHit?.Invoke(player.gameObject, this); 
            }
            else if (targetAttacker != null)
            {
                // Target 可以攻擊 Player
                //Debug.Log($"[Bullet] Target {targetAttacker.gameObject.name} shot player for {damage} damage");
                Vector2 attackerPos = _owner != null ? (Vector2)_owner.transform.position : transform.position;
                player.TakeDamage(damage, "Target Gun", attackerPos);
                OnBulletHit?.Invoke(player.gameObject, this);
            }
            else
            {
                //Debug.Log($"[Bullet] Hit player: {player.gameObject.name}");
            }
            Destroy(gameObject);
            return;
        }

        // Check if hit a target
        var target = collision.GetComponent<Target>();
        if (target != null)
        {
            // 檢查攻擊者類型
            var playerAttacker = _owner.GetComponent<Player>();
            var enemyAttacker = _owner.GetComponent<Enemy>();
            var targetAttacker = _owner.GetComponent<Target>();
            
            if (playerAttacker != null)
            {
                // Player 可以攻擊 Target
                //Debug.Log($"[Bullet] Player shot Target for {damage} damage");
                Vector2 attackerPos = _owner != null ? (Vector2)_owner.transform.position : transform.position;
                target.TakeDamage(damage, "Player Gun", attackerPos);
                OnBulletHit?.Invoke(target.gameObject, this);
            }
            else if (enemyAttacker != null)
            {
                // Enemy 不能攻擊 Target，只銷毀子彈
                //Debug.Log($"[Bullet] Enemy bullet hit Target - no damage (Enemy cannot attack Target)");
            }
            else if (targetAttacker != null)
            {
                // Target 不能攻擊 Target（同類型），只銷毀子彈
                //Debug.Log($"[Bullet] Target bullet hit Target - no damage (Target cannot attack Target)");
            }
            else
            {
                //Debug.Log($"[Bullet] Hit Target: {target.gameObject.name}");
            }
            Destroy(gameObject);
            return;
        }

        // Check if hit a door
        var doorController = collision.GetComponent<DoorController>();
        if (doorController != null)
        {
            //Debug.Log($"[Bullet] Hit door: {collision.gameObject.name}");
            OnBulletHit?.Invoke(doorController.gameObject, this);
            Destroy(gameObject);
            return;
        }

        // Check for Tilemap
        var tilemap = collision.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (tilemap != null)
        {
            //Debug.Log($"[Bullet] Hit tilemap (wall/obstacle)");
            OnBulletHit?.Invoke(tilemap.gameObject, this);
            Destroy(gameObject);
            return;
        }

        // Check for TilemapCollider2D
        var tilemapCollider = collision.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            //Debug.Log($"[Bullet] Hit tilemap collider (wall/obstacle)");
            OnBulletHit?.Invoke(tilemapCollider.gameObject, this);
            Destroy(gameObject);
            return;
        }

        // Hit any other obstacle
        //Debug.Log($"[Bullet] Hit obstacle: {collision.gameObject.name}");
        Destroy(gameObject);
    }

    // Keep this as backup in case raycast misses something
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHitTarget)
            return;

        // Don't hit the owner
        if (collision.gameObject == _owner)
            return;

        //Debug.Log($"[Bullet] OnTriggerEnter2D detected: {collision.gameObject.name}");
        HandleCollision(collision);
    }

    private void OnDrawGizmos()
    {
        // Visualize raycast in editor
        if (Application.isPlaying && !_hasHitTarget && _direction != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Vector3 start = transform.position + (Vector3)_direction * 0.1f;
            Vector3 direction = (Vector3)_direction * (speed * Time.deltaTime + raycastCheckDistance);
            Gizmos.DrawRay(start, direction);
            
            // Draw bullet position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
