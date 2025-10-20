using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private InputSystem_Actions inputActions;

    [Header("除錯移除錯動除錯設除錯定除錯")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    private bool isRunning = false;

    private WeaponHolder weaponHolder;

    [Header("除錯點除錯擊除錯移除錯動除錯設除錯定除錯")]
    [SerializeField] private float arrivalThreshold = 0.1f;
    private bool hasMoveTarget = false;
    private Vector2 moveTarget;

    [Header("除錯武除錯器除錯設除錯定除錯")]
    [SerializeField] private bool useMouseAiming = true;
    [Tooltip("除錯如除錯果除錯沒除錯有除錯滑除錯鼠除錯瞄除錯準除錯輸除錯入除錯，除錯武除錯器除錯是除錯否除錯跟除錯隨除錯移除錯動除錯方除錯向除錯")]
    [SerializeField] private bool weaponFollowMovement = true;

    [Header("血量設定")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invulnerabilityTime = 1f;
    private float lastDamageTime = -999f;

    // 除錯滑除錯鼠除錯/除錯指除錯標除錯相除錯關除錯
    private Vector2 currentPointerScreenPos;
    private Vector2 lastValidAimDirection = Vector2.right; // 除錯預除錯設除錯向除錯右除錯
    private Camera playerCamera;

    // 除錯武除錯器除錯更除錯新除錯 除錯-除錯 除錯限除錯制除錯更除錯新除錯頻除錯率除錯
    private float weaponUpdateTime = 0f;
    private const float WEAPON_UPDATE_INTERVAL = 0.05f; // 除錯2除錯0除錯 除錯F除錯P除錯S除錯 除錯更除錯新除錯武除錯器除錯方除錯向除錯

    // 血量相關屬性和事件
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvulnerable => Time.time < lastDamageTime + invulnerabilityTime;

    public event System.Action<int, int> OnHealthChanged; // 當前血量, 最大血量
    public event System.Action OnPlayerDied; // 玩家死亡事件

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new InputSystem_Actions();
        weaponHolder = GetComponent<WeaponHolder>();

        // 除錯獲除錯取除錯攝除錯影除錯機除錯參除錯考除錯
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();

        // 初始化血量
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player1.Move.performed += OnMovePerformed;
        inputActions.Player1.Move.canceled += OnMoveCanceled;

        inputActions.Player1.Attack.performed += OnAttackPerformed;

        inputActions.Player1.Point.performed += OnPointPerformed;
        inputActions.Player1.Click.performed += OnClickPerformed;
        inputActions.Player1.Open.performed += OnOpenPerformed;

        inputActions.Player1.Run.performed += OnRunPerformed;
        inputActions.Player1.Run.canceled += OnRunCanceled;
    }

    private void OnDisable()
    {
        inputActions.Player1.Move.performed -= OnMovePerformed;
        inputActions.Player1.Move.canceled -= OnMoveCanceled;

        inputActions.Player1.Attack.performed -= OnAttackPerformed;

        inputActions.Player1.Point.performed -= OnPointPerformed;
        inputActions.Player1.Click.performed -= OnClickPerformed;
        inputActions.Player1.Open.performed -= OnOpenPerformed;

        inputActions.Player1.Run.performed -= OnRunPerformed;
        inputActions.Player1.Run.canceled -= OnRunCanceled;

        inputActions.Disable();
    }

    private void Update()
    {
        // 除錯更除錯新除錯武除錯器除錯方除錯向除錯（除錯限除錯制除錯更除錯新除錯頻除錯率除錯以除錯提除錯升除錯效除錯能除錯）除錯
        if (Time.time - weaponUpdateTime >= WEAPON_UPDATE_INTERVAL)
        {
            UpdateWeaponDirection();
            weaponUpdateTime = Time.time;
        }
    }
    private void HandleRotation()
    {
        var direction = GetMouseWorldDirection();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

    }
    private void FixedUpdate()
    {
        HandleRotation();
        HandleMovement();
    }

    private void HandleMovement()
    {
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        // 除錯鍵除錯盤除錯/除錯搖除錯桿除錯輸除錯入除錯
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            rb.linearVelocity = moveInput * currentSpeed;

            // 除錯如除錯果除錯武除錯器除錯跟除錯隨除錯移除錯動除錯且除錯沒除錯有除錯使除錯用除錯滑除錯鼠除錯瞄除錯準除錯
            if (weaponFollowMovement && !useMouseAiming)
            {
                lastValidAimDirection = moveInput.normalized;
            }
            return;
        }

        // 除錯點除錯擊除錯移除錯動除錯
        if (hasMoveTarget)
        {
            Vector2 currentPos = rb.position;
            Vector2 toTarget = moveTarget - currentPos;

            if (toTarget.sqrMagnitude <= arrivalThreshold * arrivalThreshold)
            {
                hasMoveTarget = false;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                Vector2 moveDirection = toTarget.normalized;
                rb.linearVelocity = moveDirection * currentSpeed;

                // 除錯如除錯果除錯武除錯器除錯跟除錯隨除錯移除錯動除錯且除錯沒除錯有除錯使除錯用除錯滑除錯鼠除錯瞄除錯準除錯
                if (weaponFollowMovement && !useMouseAiming)
                {
                    lastValidAimDirection = moveDirection;
                }
            }
            return;
        }

        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateWeaponDirection()
    {
        if (weaponHolder == null) return;

        Vector2 aimDirection = lastValidAimDirection;

        if (useMouseAiming)
        {
            aimDirection = GetMouseWorldDirection();

            // 除錯如除錯果除錯滑除錯鼠除錯方除錯向除錯有除錯效除錯，除錯更除錯新除錯最除錯後除錯有除錯效除錯方除錯向除錯
            if (aimDirection.sqrMagnitude > 0.1f)
            {
                lastValidAimDirection = aimDirection;
            }
        }

        // 除錯更除錯新除錯武除錯器除錯方除錯向除錯
        weaponHolder.UpdateWeaponDirection(aimDirection);
    }

    private Vector2 GetMouseWorldDirection()
    {
        if (playerCamera == null) return lastValidAimDirection;

        // 除錯將除錯滑除錯鼠除錯螢除錯幕除錯座除錯標除錯轉除錯換除錯為除錯世除錯界除錯座除錯標除錯
        float zDist = Mathf.Abs(playerCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(currentPointerScreenPos.x, currentPointerScreenPos.y, zDist));

        // 除錯計除錯算除錯從除錯角除錯色除錯位除錯置除錯到除錯滑除錯鼠除錯位除錯置除錯的除錯方除錯向除錯
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        return direction;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 0.0001f)
            hasMoveTarget = false; // 除錯鍵除錯盤除錯輸除錯入除錯優除錯先除錯
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
    }

    private void OnPointPerformed(InputAction.CallbackContext ctx)
    {
        currentPointerScreenPos = ctx.ReadValue<Vector2>();
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        if (playerCamera == null) return;

        float zDist = Mathf.Abs(playerCamera.transform.position.z - transform.position.z);
        Vector3 world = playerCamera.ScreenToWorldPoint(new Vector3(currentPointerScreenPos.x, currentPointerScreenPos.y, zDist));

        moveTarget = new Vector2(world.x, world.y);
        hasMoveTarget = true;
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (weaponHolder == null) return;

        UpdateWeaponDirection();
        Debug.Log("[PlayerController] OnAttackPerformed");
        weaponHolder.TryAttack(gameObject);
    }

    private void OnRunPerformed(InputAction.CallbackContext ctx)
    {
        isRunning = true;
    }

    private void OnRunCanceled(InputAction.CallbackContext ctx)
    {
        isRunning = false;
    }

    private void OnOpenPerformed(InputAction.CallbackContext ctx)
    {
        // 獲取玩家前方的位置
        Vector3 openPosition = transform.position + (Vector3)lastValidAimDirection * 1.5f;
        Debug.Log("OnOpenPerformed: " + openPosition);
        // 呼叫DoorController來開啟門
        if (DoorController.Instance != null)
        {
            bool success = DoorController.Instance.RemoveDoorAtWorldPosition(openPosition);
            if (success)
            {
                Debug.Log($"成功開啟/刪除門在位置: {openPosition}");
            }
            else
            {
                Debug.Log($"在位置 {openPosition} 沒有找到門");
            }
        }
        else
        {
            Debug.LogWarning("DoorController 實例不存在");
        }
    }

    #region 公開方法

    /// <summary>
    /// 除錯設除錯定除錯滑除錯鼠除錯瞄除錯準除錯模除錯式除錯
    /// </summary>
    public void SetMouseAiming(bool enabled)
    {
        useMouseAiming = enabled;
    }

    /// <summary>
    /// 除錯設除錯定除錯武除錯器除錯是除錯否除錯跟除錯隨除錯移除錯動除錯方除錯向除錯
    /// </summary>
    public void SetWeaponFollowMovement(bool enabled)
    {
        weaponFollowMovement = enabled;
    }

    /// <summary>
    /// 除錯手除錯動除錯設除錯定除錯武除錯器除錯方除錯向除錯
    /// </summary>
    public void SetWeaponDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.1f)
        {
            lastValidAimDirection = direction.normalized;
            weaponHolder?.UpdateWeaponDirection(lastValidAimDirection);
        }
    }

    /// <summary>
    /// 除錯取除錯得除錯當除錯前除錯武除錯器除錯方除錯向除錯
    /// </summary>
    public Vector2 GetWeaponDirection()
    {
        return lastValidAimDirection;
    }

    /// <summary>
    /// 除錯檢除錯查除錯是除錯否除錯可除錯以除錯攻除錯擊除錯
    /// </summary>
    public bool CanAttack()
    {
        return weaponHolder?.CanAttack() ?? false;
    }

    #endregion

    #region 除錯輔助

    private void OnDrawGizmosSelected()
    {
        // 除錯顯除錯示除錯移除錯動除錯目除錯標除錯
        if (hasMoveTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(moveTarget, 0.2f);
            Gizmos.DrawLine(transform.position, moveTarget);
        }

        // 除錯顯除錯示除錯武除錯器除錯方除錯向除錯
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)lastValidAimDirection * 2f);
    }

    #endregion

    #region 血量管理

    /// <summary>
    /// 受到傷害
    /// </summary>
    /// <param name="damage">傷害值</param>
    /// <param name="source">傷害來源</param>
    public void TakeDamage(int damage, string source = "")
    {
        if (IsDead || IsInvulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        lastDamageTime = Time.time;

        // 觸發血量變化事件
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 調試信息
        if (!string.IsNullOrEmpty(source))
        {
            Debug.Log($"玩家受到 {damage} 點傷害 (來源: {source})，當前血量: {currentHealth}/{maxHealth}");
        }

        // 檢查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 治療
    /// </summary>
    /// <param name="healAmount">治療量</param>
    public void Heal(int healAmount)
    {
        if (IsDead || healAmount <= 0) return;

        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        // 觸發血量變化事件
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"玩家治療 {currentHealth - oldHealth} 點血量，當前血量: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// 完全治療
    /// </summary>
    public void FullHeal()
    {
        if (IsDead) return;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"玩家完全治療，當前血量: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// 設定血量
    /// </summary>
    /// <param name="health">新的血量值</param>
    public void SetHealth(int health)
    {
        if (IsDead) return;

        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 檢查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 增加最大血量
    /// </summary>
    /// <param name="amount">增加的量</param>
    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0) return;

        maxHealth += amount;
        currentHealth += amount; // 同時增加當前血量
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"最大血量增加 {amount}，當前血量: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// 玩家死亡
    /// </summary>
    private void Die()
    {
        if (IsDead) return;

        Debug.Log("玩家死亡！");
        OnPlayerDied?.Invoke();

        // 可以在這裡添加死亡邏輯
        // 例如：播放死亡動畫、停止移動、禁用輸入等
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 復活玩家
    /// </summary>
    public void Resurrect()
    {
        currentHealth = maxHealth;
        lastDamageTime = -999f; // 重置無敵時間
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        gameObject.SetActive(true);
        Debug.Log("玩家復活！");
    }

    #endregion
}