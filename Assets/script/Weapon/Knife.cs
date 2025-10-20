using UnityEngine;

public class Knife : Weapon
{
    protected override void Awake()
    {
        base.Awake(); // 調用父類的Awake方法來初始化耐久度
        attackRange = 1.2f;
        attackCooldown = 0.3f;
        maxDurability = 50; // 小刀耐久度較低
        durabilityLossPerAttack = 2; // 每次攻擊減少2點耐久度
    }

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.gameObject == attacker) continue; // 跳過攻擊者本人
            
            // log all hit objects
            Debug.Log($"Knife hit: {hit.gameObject.name}");

            // 檢查是否擊中敵人（當玩家攻擊時）
            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log($"Knife 擊中敵人: {enemy.gameObject.name}");
                enemy.Die(); // 敵人被玩家攻擊時死亡
                continue;
            }

            // 檢查是否擊中玩家（當敵人攻擊時）
            var player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log($"Knife 擊中玩家: {player.gameObject.name}");
                
                // 檢查攻擊者是否是敵人
                var enemyAttacker = attacker.GetComponent<Enemy>();
                if (enemyAttacker != null)
                {
                    // 敵人攻擊玩家，造成傷害
                    int damage = 20; // 小刀傷害
                    player.TakeDamage(damage, "Enemy Knife Attack");
                    Debug.Log($"敵人對玩家造成 {damage} 點傷害");
                }
                else
                {
                    // 玩家攻擊玩家（friendly fire 或其他情況）
                    Debug.Log("玩家攻擊玩家 - 可能是 friendly fire");
                }
            }
        }
    }
}