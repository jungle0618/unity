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
            if (hit.gameObject == attacker) continue; // ���L�����̥��H

            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die(); // �� enemy.TakeDamage(...)
                continue;
            }

            var player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                // �Y attacker �O���a�A�i��O���a������ friendly fire
                // �B�z���a�����޿�
            }
        }
    }
}
