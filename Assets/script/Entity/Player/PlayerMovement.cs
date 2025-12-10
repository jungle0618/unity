using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 玩家移動組件（繼承基礎移動組件）
/// 職責：處理玩家移動邏輯，保留按鍵輸入控制
/// 
/// 【封裝說明】
/// 此類的屬性（如 moveSpeed, runSpeed）應通過 Player 類的公共方法進行修改，而不是直接訪問。
/// 注意：Player 的移動主要由玩家輸入控制，通常不需要外部修改移動速度。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : BaseMovement
{
    [Header("移動參數")]
    [Tooltip("基礎移動速度倍數（相對於 Player.BaseSpeed）")]
    [SerializeField] private float normalSpeedMultiplier = 1.0f;
    [Tooltip("跑步速度倍數（相對於 Player.BaseSpeed）")]
    [SerializeField] private float runSpeedMultiplier = 1.6f;
    
    // 注意：moveSpeed 和 runSpeed 不再在這裡定義，應從 Player 的 BaseSpeed 獲取
    // 實際移動速度 = Player.BaseSpeed * normalSpeedMultiplier
    // 實際跑步速度 = Player.BaseSpeed * runSpeedMultiplier
    // 蹲下速度倍數應從 Player.SquatSpeedMultiplier 獲取
    
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
            // 取消所有事件訂閱
            inputActions.Player1.Move.performed -= OnMovePerformed;
            inputActions.Player1.Move.canceled -= OnMoveCanceled;
            inputActions.Player1.Run.performed -= OnRunPerformed;
            inputActions.Player1.Run.canceled -= OnRunCanceled;
            inputActions.Player1.Squat.performed -= OnSquatPerformed;
            inputActions.Player1.MoveCamera.performed -= OnMoveCameraPerformed;
            inputActions.Player1.MoveCamera.canceled -= OnMoveCameraCanceled;
            
            // 禁用 Player1 action map（必須在禁用整個輸入系統之前調用）
            inputActions.Player1.Disable();
            
            // 禁用整個輸入系統
            inputActions.Disable();
        }
    }
    
    private void OnDestroy()
    {
        // 清理輸入系統資源
        if (inputActions != null)
        {
            // 取消所有事件訂閱
            inputActions.Player1.Move.performed -= OnMovePerformed;
            inputActions.Player1.Move.canceled -= OnMoveCanceled;
            inputActions.Player1.Run.performed -= OnRunPerformed;
            inputActions.Player1.Run.canceled -= OnRunCanceled;
            inputActions.Player1.Squat.performed -= OnSquatPerformed;
            inputActions.Player1.MoveCamera.performed -= OnMoveCameraPerformed;
            inputActions.Player1.MoveCamera.canceled -= OnMoveCameraCanceled;
            
            // 禁用 Player1 action map
            inputActions.Player1.Disable();
            
            // 禁用整個輸入系統
            inputActions.Disable();
            
            // 釋放資源
            inputActions.Dispose();
            inputActions = null;
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
        
        // 從 Player 獲取基礎速度和乘數
        float baseSpeed = GetBaseSpeed();
        
        // 速度乘數：蹲下、跑步、正常移動（不會同時蹲下和跑步）
        float speedMultiplier;
        if (isSquatting)
        {
            speedMultiplier = GetSquatSpeedMultiplier();
        }
        else if (isRunning)
        {
            speedMultiplier = runSpeedMultiplier;
        }
        else
        {
            speedMultiplier = normalSpeedMultiplier;
        }
        
        float injuryMultiplier = GetInjurySpeedMultiplier(); // 受傷時速度乘以 0.7
        float currentSpeed = baseSpeed * speedMultiplier * injuryMultiplier;

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
            Vector2 currentPos = transform.position;
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
        Vector2 playerPos = transform.position;
        
        float leftEdge = cameraPos.x - halfWidth;
        float rightEdge = cameraPos.x + halfWidth;
        float bottomEdge = cameraPos.y - halfHeight;
        float topEdge = cameraPos.y + halfHeight;
        
        float playerDistanceFromLeft = playerPos.x - leftEdge;
        float playerDistanceFromRight = rightEdge - playerPos.x;
        float playerDistanceFromBottom = playerPos.y - bottomEdge;
        float playerDistanceFromTop = topEdge - playerPos.y;
        
        // 根據危險等級動態調整邊緣距離
        float currentEdgeDistance = GetEdgeDistanceByDangerLevel();
        Vector2 limitedInput = moveInput;
        
        // 水平限制：如果玩家距離邊緣小於等於限制，不允許繼續往外移動
        if (moveInput.x < 0 && playerDistanceFromLeft <= currentEdgeDistance)
        {
            // 玩家想往左移動，但已經太靠近左邊緣
            limitedInput.x = 0;
        }
        else if (moveInput.x > 0 && playerDistanceFromRight <= currentEdgeDistance)
        {
            // 玩家想往右移動，但已經太靠近右邊緣
            limitedInput.x = 0;
        }
        
        // 垂直限制：如果玩家距離邊緣小於等於限制，不允許繼續往外移動
        if (moveInput.y < 0 && playerDistanceFromBottom <= currentEdgeDistance)
        {
            // 玩家想往下移動，但已經太靠近下邊緣
            limitedInput.y = 0;
        }
        else if (moveInput.y > 0 && playerDistanceFromTop <= currentEdgeDistance)
        {
            // 玩家想往上移動，但已經太靠近上邊緣
            limitedInput.y = 0;
        }
        
        return limitedInput;
    }
    
    /// <summary>
    /// 根據危險等級獲取邊緣距離限制
    /// </summary>
    private float GetEdgeDistanceByDangerLevel()
    {
        if (DangerousManager.Instance == null)
        {
            return normalEdgeDistance;
        }
        
        var dangerLevel = DangerousManager.Instance.CurrentDangerLevelType;
        
        // Safe, Low, Medium：1 格
        // High, Critical：3 格
        switch (dangerLevel)
        {
            case DangerousManager.DangerLevel.Safe:
            case DangerousManager.DangerLevel.Low:
            case DangerousManager.DangerLevel.Medium:
                return 1f;
                
            case DangerousManager.DangerLevel.High:
            case DangerousManager.DangerLevel.Critical:
                return 3f;
                
            default:
                return normalEdgeDistance;
        }
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
        // Check if running is enabled in settings
        if (GameSettings.Instance != null && GameSettings.Instance.RunEnabled)
        {
            isRunning = true;
        }
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
    /// 獲取基礎速度（從 Player 組件，實現基類抽象方法）
    /// </summary>
    protected override float GetBaseSpeed()
    {
        Player player = GetComponent<Player>();
        if (player != null)
        {
            return player.BaseSpeed;
        }
        // 如果找不到 Player 組件，返回預設值（向後兼容）
        return 5f;
    }

    /// <summary>
    /// 檢查是否有可用的路徑規劃組件（實現基類抽象方法）
    /// 玩家不需要路徑規劃，始終返回 false
    /// </summary>
    protected override bool HasPathfinding()
    {
        return false; // 玩家不使用路徑規劃
    }

    /// <summary>
    /// 獲取路徑規劃組件並計算路徑（實現基類抽象方法）
    /// 玩家不需要路徑規劃，始終返回 null
    /// </summary>
    protected override List<PathfindingNode> FindPath(Vector2 start, Vector2 target)
    {
        return null; // 玩家不使用路徑規劃
    }
    
    /// <summary>
    /// 獲取蹲下速度倍數（從 Player 組件）
    /// </summary>
    private float GetSquatSpeedMultiplier()
    {
        Player player = GetComponent<Player>();
        if (player != null)
        {
            return player.SquatSpeedMultiplier;
        }
        // 如果找不到 Player 組件，返回預設值（向後兼容）
        return 0.5f;
    }

    /// <summary>
    /// 設定移動速度（覆寫基類方法）
    /// 注意：Player 的移動速度主要由 Player.BaseSpeed 和乘數控制
    /// 此方法保留以維持向後兼容，但實際速度應從 Player 獲取
    /// </summary>
    public override void SetSpeed(float newSpeed)
    {
        // 注意：此方法現在用於設置當前應用的速度（由外部系統調用）
        // 實際速度應從 Player.BaseSpeed * 乘數獲取
        // 此處保留以維持向後兼容
    }

    /// <summary>
    /// 獲取移動速度（覆寫基類方法）
    /// 返回當前應用的速度（從 Player 獲取基礎速度並應用乘數）
    /// </summary>
    public override float GetSpeed()
    {
        float baseSpeed = GetBaseSpeed();
        
        // 速度乘數：蹲下、跑步、正常移動（不會同時蹲下和跑步）
        float speedMultiplier;
        if (isSquatting)
        {
            speedMultiplier = GetSquatSpeedMultiplier();
        }
        else if (isRunning)
        {
            speedMultiplier = runSpeedMultiplier;
        }
        else
        {
            speedMultiplier = normalSpeedMultiplier;
        }
        
        float injuryMultiplier = GetInjurySpeedMultiplier(); // 受傷時速度乘以 0.7
        return baseSpeed * speedMultiplier * injuryMultiplier;
    }
}

