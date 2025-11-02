using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家移動組件（繼承基礎移動組件）
/// 職責：處理玩家移動邏輯，保留按鍵輸入控制
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : BaseMovement
{
    [Header("移動參數")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float squatSpeedMultiplier = 0.5f; // 蹲下時移動速度減半
    
    [Header("點擊移動")]
    [SerializeField] private float arrivalThreshold = 0.1f;
    
    [Header("邊緣限制設定")]
    [SerializeField] private bool useEdgeLimits = true;
    [SerializeField] private float normalEdgeDistance = 1f;
    
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool isRunning = false;
    private bool isSquatting = false;
    private bool isCameraMode = false;
    
    // 點擊移動
    private bool hasMoveTarget = false;
    private Vector2 moveTarget;
    
    // 攝影機和邊緣限制
    private Camera playerCamera;
    private CameraController2D cameraController;

    protected override void Awake()
    {
        base.Awake(); // 調用基類 Awake，初始化 rb
        
        inputActions = new InputSystem_Actions();
        
        // 獲取攝影機
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();
            
        // 獲取攝影機控制器
        cameraController = FindFirstObjectByType<CameraController2D>();
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
            inputActions.Player1.Move.performed += OnMovePerformed;
            inputActions.Player1.Move.canceled += OnMoveCanceled;
            inputActions.Player1.Run.performed += OnRunPerformed;
            inputActions.Player1.Run.canceled += OnRunCanceled;
            inputActions.Player1.Squat.performed += OnSquatPerformed;
            inputActions.Player1.MoveCamera.performed += OnMoveCameraPerformed;
            inputActions.Player1.MoveCamera.canceled += OnMoveCameraCanceled;
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player1.Move.performed -= OnMovePerformed;
            inputActions.Player1.Move.canceled -= OnMoveCanceled;
            inputActions.Player1.Run.performed -= OnRunPerformed;
            inputActions.Player1.Run.canceled -= OnRunCanceled;
            inputActions.Player1.Squat.performed -= OnSquatPerformed;
            inputActions.Player1.MoveCamera.performed -= OnMoveCameraPerformed;
            inputActions.Player1.MoveCamera.canceled -= OnMoveCameraCanceled;
            inputActions.Disable();
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    /// <summary>
    /// 向目標移動（覆寫基類方法，用於 AI 控制的移動）
    /// </summary>
    public override void MoveTowards(Vector2 target, float speedMultiplier)
    {
        // Player 不使用 AI 移動，但保留接口以維持兼容性
        // 實際移動由 HandleMovement 處理
    }

    /// <summary>
    /// 處理移動（保留按鍵輸入控制）
    /// </summary>
    private void HandleMovement()
    {
        // 如果按下 Space 鍵（攝影機移動模式），玩家不移動
        if (isCameraMode)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }
        
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        
        // 蹲下時移動速度減半
        if (isSquatting)
        {
            currentSpeed *= squatSpeedMultiplier;
        }

        // 使用 WASD 鍵盤輸入進行移動
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            // 檢查邊緣限制
            Vector2 limitedMoveInput = ApplyEdgeLimits(moveInput);
            if (rb != null)
            {
                rb.linearVelocity = limitedMoveInput * currentSpeed;
            }
            hasMoveTarget = false; // 鍵盤輸入優先
            return;
        }

        // 點擊移動
        if (hasMoveTarget)
        {
            Vector2 currentPos = Position;
            Vector2 toTarget = moveTarget - currentPos;

            if (toTarget.sqrMagnitude <= arrivalThreshold * arrivalThreshold)
            {
                hasMoveTarget = false;
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                Vector2 moveDirection = toTarget.normalized;
                if (rb != null)
                {
                    rb.linearVelocity = moveDirection * currentSpeed;
                }
            }
            return;
        }

        // 停止移動
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// 應用邊緣限制，防止玩家移動到攝影機邊緣外
    /// </summary>
    private Vector2 ApplyEdgeLimits(Vector2 moveInput)
    {
        if (!useEdgeLimits || playerCamera == null) return moveInput;
        
        float halfHeight = playerCamera.orthographicSize;
        float halfWidth = playerCamera.aspect * halfHeight;
        
        Vector3 cameraPos = playerCamera.transform.position;
        Vector2 playerPos = Position;
        
        float leftEdge = cameraPos.x - halfWidth;
        float rightEdge = cameraPos.x + halfWidth;
        float bottomEdge = cameraPos.y - halfHeight;
        float topEdge = cameraPos.y + halfHeight;
        
        float playerDistanceFromLeft = playerPos.x - leftEdge;
        float playerDistanceFromRight = rightEdge - playerPos.x;
        float playerDistanceFromBottom = playerPos.y - bottomEdge;
        float playerDistanceFromTop = topEdge - playerPos.y;
        
        float currentEdgeDistance = normalEdgeDistance;
        Vector2 limitedInput = moveInput;
        
        // 水平限制
        if (moveInput.x < 0 && playerDistanceFromLeft <= currentEdgeDistance)
        {
            limitedInput.x = 0;
        }
        else if (moveInput.x > 0 && playerDistanceFromRight <= currentEdgeDistance)
        {
            limitedInput.x = 0;
        }
        
        // 垂直限制
        if (moveInput.y < 0 && playerDistanceFromBottom <= currentEdgeDistance)
        {
            limitedInput.y = 0;
        }
        else if (moveInput.y > 0 && playerDistanceFromTop <= currentEdgeDistance)
        {
            limitedInput.y = 0;
        }
        
        return limitedInput;
    }

    // 輸入事件處理
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 0.0001f)
            hasMoveTarget = false;
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
    }

    private void OnRunPerformed(InputAction.CallbackContext ctx)
    {
        isRunning = true;
    }

    private void OnRunCanceled(InputAction.CallbackContext ctx)
    {
        isRunning = false;
    }

    private void OnSquatPerformed(InputAction.CallbackContext ctx)
    {
        ToggleSquat();
    }

    private void OnMoveCameraPerformed(InputAction.CallbackContext ctx)
    {
        isCameraMode = true;
    }

    private void OnMoveCameraCanceled(InputAction.CallbackContext ctx)
    {
        isCameraMode = false;
    }

    // 公共方法
    public void SetMoveTarget(Vector2 target)
    {
        moveTarget = target;
        hasMoveTarget = true;
    }

    public void ClearMoveTarget()
    {
        hasMoveTarget = false;
    }

    public bool HasMoveTarget => hasMoveTarget;
    public Vector2 MoveTarget => moveTarget;

    public void ToggleSquat()
    {
        isSquatting = !isSquatting;
    }

    public void SetSquatting(bool squatting)
    {
        isSquatting = squatting;
    }

    public bool IsSquatting => isSquatting;
    public bool IsRunning => isRunning;
    public bool IsCameraMode => isCameraMode;

    public Vector2 MoveInput => moveInput;

    /// <summary>
    /// 設定移動速度（覆寫基類方法）
    /// </summary>
    public override void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    /// <summary>
    /// 獲取移動速度（覆寫基類方法）
    /// </summary>
    public override float GetSpeed()
    {
        return isRunning ? runSpeed : moveSpeed;
    }
}

