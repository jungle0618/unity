using UnityEngine;

/// <summary>
/// 專責處理敵人攻擊邏輯的控制器。
/// 職責：攻擊冷卻、朝向更新、委派給 ItemHolder 進行實際攻擊。
/// </summary>
[RequireComponent(typeof(ItemHolder))]
public class EnemyAttackController : MonoBehaviour
{
    [Header("攻擊參數")]
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private float attackDetectionRange = 3f;
    [Tooltip("是否使用武器的實際攻擊範圍（對持槍敵人啟用此選項）")]
    [SerializeField] private bool useWeaponAttackRange = true;

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

        // 距離檢查（使用有效攻擊範圍）
        float effectiveRange = GetEffectiveAttackRange();
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance > effectiveRange) return false;

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
    /// 獲取有效攻擊範圍（根據武器類型自動調整）
    /// </summary>
    public float GetEffectiveAttackRange()
    {
        // 如果啟用了使用武器攻擊範圍選項
        if (useWeaponAttackRange && itemHolder != null && itemHolder.CurrentWeapon != null)
        {
            // 檢查是否為遠程武器（槍械）
            if (itemHolder.CurrentWeapon is RangedWeapon rangedWeapon)
            {
                return rangedWeapon.AttackRange;
            }
            // 檢查是否為近戰武器（刀械）
            else if (itemHolder.CurrentWeapon is MeleeWeapon meleeWeapon)
            {
                return meleeWeapon.AttackRange;
            }
        }
        
        // 否則使用預設的攻擊偵測範圍
        return attackDetectionRange;
    }

    /// <summary>
    /// 設定攻擊冷卻時間
    /// </summary>
    public void SetAttackCooldown(float cooldownSeconds)
    {
        attackCooldown = Mathf.Max(0f, cooldownSeconds);
    }
}