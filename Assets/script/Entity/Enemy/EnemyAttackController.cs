using UnityEngine;

/// <summary>
/// 專責處理敵人攻擊邏輯的控制器。
/// 職責：攻擊冷卻、朝向更新、委派給 ItemHolder 進行實際攻擊。
/// </summary>
[RequireComponent(typeof(ItemHolder))]
public class EnemyAttackController : MonoBehaviour
{
    [Header("攻擊參數")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackDetectionRange = 3f;

    private float lastAttackTime = 0f;
    private ItemHolder itemHolder;

    private void Awake()
    {
        itemHolder = GetComponent<ItemHolder>();
    }

    /// <summary>
    /// 嘗試攻擊玩家
    /// </summary>
    public bool TryAttackPlayer(Transform playerTransform)
    {
        if (itemHolder == null || playerTransform == null) return false;
        
        // 確保當前物品是武器
        if (!itemHolder.IsCurrentItemWeapon) return false;

        // 冷卻檢查
        if (Time.time - lastAttackTime < attackCooldown) return false;

        // 距離檢查
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance > attackDetectionRange) return false;

        // 面向與方向更新
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        itemHolder.UpdateWeaponDirection(directionToPlayer);

        if (!itemHolder.CanAttack()) return false;

        bool attackSucceeded = itemHolder.TryAttack(gameObject);
        if (attackSucceeded)
        {
            lastAttackTime = Time.time;
        }
        return attackSucceeded;
    }

    /// <summary>
    /// 設定攻擊冷卻時間
    /// </summary>
    public void SetAttackCooldown(float cooldownSeconds)
    {
        attackCooldown = Mathf.Max(0f, cooldownSeconds);
    }
}