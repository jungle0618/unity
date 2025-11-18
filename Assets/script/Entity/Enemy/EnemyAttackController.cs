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

        // 【新增】檢查是否應該攻擊（基於區域和玩家狀態）
        if (!ShouldAttackPlayer(playerTransform))
        {
            return false;
        }

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
    /// 判斷是否應該攻擊玩家（基於區域類型和玩家狀態）
    /// Guard Area: 始終攻擊
    /// Safe Area: 只有當玩家持有武器或危險等級觸發時才攻擊
    /// </summary>
    private bool ShouldAttackPlayer(Transform playerTransform)
    {
        if (playerTransform == null) return false;
        
        // 【新增】檢查是否啟用 Guard Area System
        // 如果停用，使用原始行為（總是可以攻擊）
        if (GameSettings.Instance != null && !GameSettings.Instance.UseGuardAreaSystem)
        {
            return true; // 原始行為：總是可以攻擊
        }
        
        // 檢查玩家位置所在區域
        Vector3 playerPosition = playerTransform.position;
        
        // 如果 AreaManager 不存在，默認為 Guard Area 行為（向後兼容）
        if (AreaManager.Instance == null)
        {
            return true;
        }
        
        // 如果在 Guard Area，始終攻擊
        if (AreaManager.Instance.IsInGuardArea(playerPosition))
        {
            return true;
        }
        
        // 在 Safe Area 中，檢查玩家是否持有武器
        Player player = playerTransform.GetComponent<Player>();
        if (player == null) return true; // 找不到 Player 組件，默認攻擊
        
        ItemHolder playerItemHolder = player.GetComponent<ItemHolder>();
        if (playerItemHolder == null) return true; // 找不到 ItemHolder，默認攻擊
        
        // 檢查玩家是否持有武器
        bool playerHasWeapon = playerItemHolder.IsCurrentItemWeapon;
        
        // 檢查是否危險等級被觸發
        bool isDangerTriggered = false;
        DangerousManager dangerManager = DangerousManager.Instance;
        if (dangerManager != null)
        {
            // 危險等級 > Safe 時視為觸發
            isDangerTriggered = dangerManager.CurrentDangerLevelType != DangerousManager.DangerLevel.Safe;
        }
        
        // Safe Area 邏輯：玩家持有武器 OR 危險等級觸發 → 攻擊
        return playerHasWeapon || isDangerTriggered;
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