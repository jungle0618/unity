using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController2D : MonoBehaviour
{
    [Header("固定縮放 (相機大小固定)")]
    public float fixedOrthographicSize = 5f; // 相機固定的 orthographicSize（腳本不會改變）

    [Header("玩家引用")]
    public Transform player;                 // 指向 player 的 transform（用於邊緣限制檢查）

    [Header("WASD 攝影機移動")]
    public float cameraMoveSpeed = 5f;       // WASD 控制攝影機移動速度
    public bool enableCameraMovement = true; // 是否啟用攝影機移動

    [Header("邊緣限制設定")]
    public bool useEdgeLimits = true;        // 是否使用邊緣限制

    private Camera cam;
    private InputSystem_Actions inputActions;
    private bool isCameraMode = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("CameraController2D 必須掛在一個 Camera 上。");
            enabled = false;
            return;
        }

        if (cam.orthographic == false)
        {
            Debug.LogWarning("這個腳本是為正交相機（Orthographic）設計的，建議切換到 2D 模式。");
        }

        // 將 orthographicSize 固定
        cam.orthographicSize = fixedOrthographicSize;
        
        // 初始化輸入系統
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        
        // 綁定攝影機移動事件
        inputActions.Player1.MoveCamera.performed += OnMoveCameraPerformed;
        inputActions.Player1.MoveCamera.canceled += OnMoveCameraCanceled;
    }

    void Update()
    {
        // WASD 控制攝影機移動（按住 Space 鍵時）
        HandleCameraMovement();
    }

    void HandleCameraMovement()
    {
        if (!enableCameraMovement) return;
        
        // 只有按下 Space 鍵時才能移動攝影機
        if (!isCameraMode) return;

        // 使用 Input System 獲取 WASD 輸入
        Vector2 input = inputActions.Player1.Move.ReadValue<Vector2>();

        // 如果有輸入，移動攝影機
        if (input.sqrMagnitude > 0.0001f)
        {
            Vector3 movement = new Vector3(input.x, input.y, 0f) * cameraMoveSpeed * Time.deltaTime;
            
            // 應用邊緣限制（如果啟用）
            if (useEdgeLimits)
            {
                movement = ApplyCameraEdgeLimits(movement);
            }
            
            transform.position += movement;
        }
    }

    // 重置相機到初始位置和固定大小
    public void ResetCamera()
    {
        float z = transform.position.z;
        float x = (player != null) ? player.position.x : 0f;
        transform.position = new Vector3(x, 0f, z);
        cam.orthographicSize = fixedOrthographicSize;
    }
    
    /// <summary>
    /// 攝影機移動模式開始
    /// </summary>
    private void OnMoveCameraPerformed(InputAction.CallbackContext ctx)
    {
        isCameraMode = true;
    }
    
    /// <summary>
    /// 攝影機移動模式結束
    /// </summary>
    private void OnMoveCameraCanceled(InputAction.CallbackContext ctx)
    {
        isCameraMode = false;
    }
    
    /// <summary>
    /// 應用攝影機邊緣限制，防止攝影機移動導致玩家超出邊緣限制
    /// </summary>
    private Vector3 ApplyCameraEdgeLimits(Vector3 movement)
    {
        if (player == null) return movement;
        
        // 計算攝影機視口邊界
        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.aspect * halfHeight;
        
        Vector3 cameraPos = transform.position;
        Vector3 playerPos = player.position;
        
        // 計算攝影機邊界
        float leftEdge = cameraPos.x - halfWidth;
        float rightEdge = cameraPos.x + halfWidth;
        float bottomEdge = cameraPos.y - halfHeight;
        float topEdge = cameraPos.y + halfHeight;
        
        // 計算玩家相對於攝影機邊緣的位置
        float playerDistanceFromLeft = playerPos.x - leftEdge;
        float playerDistanceFromRight = rightEdge - playerPos.x;
        float playerDistanceFromBottom = playerPos.y - bottomEdge;
        float playerDistanceFromTop = topEdge - playerPos.y;
        
        // 根據危險等級獲取邊緣距離限制
        float currentEdgeDistance = GetEdgeDistanceByDangerLevel();
        
        Vector3 limitedMovement = movement;
        
        // 水平限制
        // 注意：攝影機向左移動(movement.x < 0)時，玩家在畫面上相對向右移動
        if (movement.x < 0 && playerDistanceFromRight <= currentEdgeDistance)
        {
            // 攝影機向左移動但玩家已經接近右邊緣（攝影機遠離玩家），不允許繼續往外移動
            limitedMovement.x = 0;
        }
        else if (movement.x > 0 && playerDistanceFromLeft <= currentEdgeDistance)
        {
            // 攝影機向右移動但玩家已經接近左邊緣（攝影機遠離玩家），不允許繼續往外移動
            limitedMovement.x = 0;
        }
        
        // 垂直限制
        if (movement.y < 0 && playerDistanceFromTop <= currentEdgeDistance)
        {
            // 攝影機向下移動但玩家已經接近上邊緣（攝影機遠離玩家），不允許繼續往外移動
            limitedMovement.y = 0;
        }
        else if (movement.y > 0 && playerDistanceFromBottom <= currentEdgeDistance)
        {
            // 攝影機向上移動但玩家已經接近下邊緣（攝影機遠離玩家），不允許繼續往外移動
            limitedMovement.y = 0;
        }
        
        return limitedMovement;
    }
    
    /// <summary>
    /// 根據危險等級獲取邊緣距離限制
    /// </summary>
    private float GetEdgeDistanceByDangerLevel()
    {
        if (DangerousManager.Instance == null)
        {
            return 1f; // 預設：1 格
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
                return 1f; // 預設：1 格
        }
    }
    
    private void OnDestroy()
    {
        // 清理輸入系統
        if (inputActions != null)
        {
            inputActions.Player1.MoveCamera.performed -= OnMoveCameraPerformed;
            inputActions.Player1.MoveCamera.canceled -= OnMoveCameraCanceled;
            inputActions.Disable();
            inputActions.Dispose();
        }
    }
}