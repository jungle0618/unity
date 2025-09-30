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

    [Header("���ʳt��")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    private bool isRunning = false;

    private WeaponHolder weaponHolder;

    [Header("�ƹ��I������")]
    [SerializeField] private float arrivalThreshold = 0.1f;
    private bool hasMoveTarget = false;
    private Vector2 moveTarget;

    [Header("�Z������")]
    [SerializeField] private bool useMouseAiming = true;
    [Tooltip("��S���ƹ���J�ɡA�Z���O�_���H���ʤ�V")]
    [SerializeField] private bool weaponFollowMovement = true;

    // �ƹ�/���Ь���
    private Vector2 currentPointerScreenPos;
    private Vector2 lastValidAimDirection = Vector2.right; // �w�]�¥k
    private Camera playerCamera;

    // �ʯ��u�� - ��֨C�V�p��
    private float weaponUpdateTime = 0f;
    private const float WEAPON_UPDATE_INTERVAL = 0.05f; // 20 FPS ��s�Z���¦V

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new InputSystem_Actions();
        weaponHolder = GetComponent<WeaponHolder>();

        // �֨��۾��Ѧ�
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
        // ��s�Z���¦V�]���C�W�v�H�`�٩ʯ�^
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

        // ��L/�n���J
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            rb.linearVelocity = moveInput * currentSpeed;

            // �p�G�ҥΪZ�����H���ʥB�S���ϥηƹ��˷�
            if (weaponFollowMovement && !useMouseAiming)
            {
                lastValidAimDirection = moveInput.normalized;
            }
            return;
        }

        // �I������
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

                // �p�G�ҥΪZ�����H���ʥB�S���ϥηƹ��˷�
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

            // �p�G�ƹ���V���ġA��s�̫ᦳ�Ĥ�V
            if (aimDirection.sqrMagnitude > 0.1f)
            {
                lastValidAimDirection = aimDirection;
            }
        }

        // ��s�Z���¦V
        weaponHolder.UpdateWeaponDirection(aimDirection);
    }

    private Vector2 GetMouseWorldDirection()
    {
        if (playerCamera == null) return lastValidAimDirection;

        // �N�ù��y���ഫ���@�ɮy��
        float zDist = Mathf.Abs(playerCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(currentPointerScreenPos.x, currentPointerScreenPos.y, zDist));

        // �p��q���a��ƹ���m����V
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        return direction;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 0.0001f)
            hasMoveTarget = false; // ��L��J�u��
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

        // �T�O�����e�Z���¦V�O�̷s��
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

    #region ���@��k

    /// <summary>
    /// �����ƹ��˷ǼҦ�
    /// </summary>
    public void SetMouseAiming(bool enabled)
    {
        useMouseAiming = enabled;
    }

    /// <summary>
    /// �]�w�Z���O�_���H���ʤ�V
    /// </summary>
    public void SetWeaponFollowMovement(bool enabled)
    {
        weaponFollowMovement = enabled;
    }

    /// <summary>
    /// ��ʳ]�w�Z���¦V
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
    /// �����e�Z���¦V
    /// </summary>
    public Vector2 GetWeaponDirection()
    {
        return lastValidAimDirection;
    }

    /// <summary>
    /// �ˬd�O�_�i�H����
    /// </summary>
    public bool CanAttack()
    {
        return weaponHolder?.CanAttack() ?? false;
    }

    #endregion

    #region �������U

    private void OnDrawGizmosSelected()
    {
        // ��ܲ��ʥؼ�
        if (hasMoveTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(moveTarget, 0.2f);
            Gizmos.DrawLine(transform.position, moveTarget);
        }

        // ��ܪZ���¦V
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)lastValidAimDirection * 2f);
    }

    #endregion
}
