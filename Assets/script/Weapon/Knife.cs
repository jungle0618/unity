using UnityEngine;

/// <summary>
/// 刀子 - 近戰武器
/// </summary>
public class Knife : MeleeWeapon
{

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.gameObject == attacker) continue; // 跳過攻擊者本人
            
            // log all hit objects
            Debug.Log($"Knife hit: {hit.gameObject.name}");

            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log($"[Knife] Hit enemy: {enemy.gameObject.name} for {attackDamage} damage");
                enemy.TakeDamage(attackDamage, "Player Knife");
                continue;
            }

            var player = hit.GetComponent<Player>();
            if (player != null)
            {
                // 檢查攻擊者是否是敵人
                var enemyAttacker = attacker.GetComponent<Enemy>();
                if (enemyAttacker != null)
                {
                    Debug.Log($"[Knife] Enemy {enemyAttacker.gameObject.name} attacked player for {attackDamage} damage");
                    player.TakeDamage(attackDamage, "Enemy Knife");
                }
                continue;
            }
        }
    }
}