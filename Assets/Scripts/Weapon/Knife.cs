using UnityEngine;

public class Knife : Weapon
{
    private void Awake()
    {
        attackRange = 1.2f;
        attackCooldown = 0.3f;
    }

    protected override void PerformAttack(Vector2 origin, GameObject attacker)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.gameObject == attacker) continue; // 跳過攻擊者本人

            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die(); // 或 enemy.TakeDamage(...)
                continue;
            }

            var player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                // 若 attacker 是玩家，可能是玩家互打或 friendly fire
                // 處理玩家受擊邏輯
            }
        }
    }
}
