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