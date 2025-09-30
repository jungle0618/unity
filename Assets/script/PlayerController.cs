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

    [Header("移動速度")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    private bool isRunning = false;

    private WeaponHolder weaponHolder;

    [Header("滑鼠點擊移動")]
    [SerializeField] private float arrivalThreshold = 0.1f;
    private bool hasMoveTarget = false;
    private Vector2 moveTarget;

    [Header("武器控制")]
    [SerializeField] private bool useMouseAiming = true;
    [Tooltip("當沒有滑鼠輸入時，武器是否跟隨移動方向")]
    [SerializeField] private bool weaponFollowMovement = true;

    // 滑鼠/指標相關
    private Vector2 currentPointerScreenPos;
    private Vector2 lastValidAimDirection = Vector2.right; // 預設朝右
    private Camera playerCamera;

    // 性能優化 - 減少每幀計算
    private float weaponUpdateTime = 0f;
    private const float WEAPON_UPDATE_INTERVAL = 0.05f; // 20 FPS 更新武器朝向

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new InputSystem_Actions();
        weaponHolder = GetComponent<WeaponHolder>();

        // 快取相機參考
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player1.Move.performed += OnMovePerformed;
        inputActions.Player1.Move.canceled += OnMoveCanceled;

        inputActions.Player1.Attack.performed += OnAttackPerformed;

        inputActions.Player1.Point.performed += OnPointPerformed;
        inputActions.Player1.Click.performed += OnClickPerformed;

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

        inputActions.Player1.Run.performed -= OnRunPerformed;
        inputActions.Player1.Run.canceled -= OnRunCanceled;

        inputActions.Disable();
    }

    private void Update()
    {
        // 更新武器朝向（較低頻率以節省性能）
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

        // 鍵盤/搖桿輸入
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            rb.linearVelocity = moveInput * currentSpeed;

            // 如果啟用武器跟隨移動且沒有使用滑鼠瞄準
            if (weaponFollowMovement && !useMouseAiming)
            {
                lastValidAimDirection = moveInput.normalized;
            }
            return;
        }

        // 點擊移動
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

                // 如果啟用武器跟隨移動且沒有使用滑鼠瞄準
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

            // 如果滑鼠方向有效，更新最後有效方向
            if (aimDirection.sqrMagnitude > 0.1f)
            {
                lastValidAimDirection = aimDirection;
            }
        }

        // 更新武器朝向
        weaponHolder.UpdateWeaponDirection(aimDirection);
    }

    private Vector2 GetMouseWorldDirection()
    {
        if (playerCamera == null) return lastValidAimDirection;

        // 將螢幕座標轉換為世界座標
        float zDist = Mathf.Abs(playerCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(currentPointerScreenPos.x, currentPointerScreenPos.y, zDist));

        // 計算從玩家到滑鼠位置的方向
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        return direction;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 0.0001f)
            hasMoveTarget = false; // 鍵盤輸入優先
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

        // 確保攻擊前武器朝向是最新的
        UpdateWeaponDirection();
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

    #region 公共方法

    /// <summary>
    /// 切換滑鼠瞄準模式
    /// </summary>
    public void SetMouseAiming(bool enabled)
    {
        useMouseAiming = enabled;
    }

    /// <summary>
    /// 設定武器是否跟隨移動方向
    /// </summary>
    public void SetWeaponFollowMovement(bool enabled)
    {
        weaponFollowMovement = enabled;
    }

    /// <summary>
    /// 手動設定武器朝向
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
    /// 獲取當前武器朝向
    /// </summary>
    public Vector2 GetWeaponDirection()
    {
        return lastValidAimDirection;
    }

    /// <summary>
    /// 檢查是否可以攻擊
    /// </summary>
    public bool CanAttack()
    {
        return weaponHolder?.CanAttack() ?? false;
    }

    #endregion

    #region 除錯輔助

    private void OnDrawGizmosSelected()
    {
        // 顯示移動目標
        if (hasMoveTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(moveTarget, 0.2f);
            Gizmos.DrawLine(transform.position, moveTarget);
        }

        // 顯示武器朝向
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)lastValidAimDirection * 2f);
    }

    #endregion
}
