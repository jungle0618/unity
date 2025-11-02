using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController2D : MonoBehaviour
{
    [Header("固定縮放 (相機大小固定)")]
    public float fixedOrthographicSize = 5f; // 相機固定的 orthographicSize（腳本不會改變）

    [Header("水平追蹤 (由 player 決定)")]
    public Transform player;                 // 指向 player 的 transform（在 Inspector 指定）
    [Tooltip("水平追蹤平滑速度，值越大追蹤越快（實際是在 Lerp 上乘 Time.deltaTime）。")]
    public float horizontalLerpSpeed = 10f;

    [Header("WASD 攝影機移動")]
    public float cameraMoveSpeed = 5f;       // WASD 控制攝影機移動速度
    public bool enableCameraMovement = true; // 是否啟用攝影機移動

    [Header("邊緣限制設定")]
    public bool useEdgeLimits = true;        // 是否使用邊緣限制
    public float normalEdgeDistance = 1f;    // 平常狀態：角色離攝影機邊緣至少要有幾格
    public float alertEdgeDistance = 3f;     // 警戒狀態：角色離攝影機邊緣至少要有幾格
    
    [Header("移動邊界限制（可選）")]
    public bool useBounds = false;           // 是否使用邊界限制
    public Vector2 minBounds = new Vector2(-10, -10);
    public Vector2 maxBounds = new Vector2(10, 10);

    private Camera cam;
    private InputSystem_Actions inputActions;
    private bool isCameraMode = false;
    private bool shouldFollowPlayer = false; // 是否應該跟隨玩家

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
        // 水平由 player 決定（平滑）
        FollowPlayerHorizontal();

        // WASD 控制攝影機移動
        HandleCameraMovement();
    }

    void FollowPlayerHorizontal()
    {
        if (player == null) return;

        // 如果處於攝影機移動模式或不應該跟隨玩家，不跟隨玩家
        if (isCameraMode || !shouldFollowPlayer) return;

        // 檢查是否需要邊緣限制
        if (useEdgeLimits)
        {
            ApplyEdgeLimits();
        }
        else
        {
            // 平滑跟隨玩家位置（X和Y軸）
            Vector3 targetPos = new Vector3(player.position.x, player.position.y, transform.position.z);
            float t = Mathf.Clamp01(horizontalLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
        }

        if (useBounds)
            ClampCameraToBounds();
    }
    
    void ApplyEdgeLimits()
    {
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
        
        // 計算目標攝影機位置（讓玩家保持在邊緣限制範圍內）
        Vector3 targetCameraPos = cameraPos;
        
        // 水平跟隨邏輯
        if (playerDistanceFromLeft < normalEdgeDistance)
        {
            // 玩家太靠近左邊緣，攝影機向左移動
            targetCameraPos.x = playerPos.x + halfWidth - normalEdgeDistance;
        }
        else if (playerDistanceFromRight < normalEdgeDistance)
        {
            // 玩家太靠近右邊緣，攝影機向右移動
            targetCameraPos.x = playerPos.x - halfWidth + normalEdgeDistance;
        }
        else
        {
            // 玩家在安全範圍內，攝影機跟隨玩家
            targetCameraPos.x = playerPos.x;
        }
        
        // 垂直跟隨邏輯
        if (playerDistanceFromBottom < normalEdgeDistance)
        {
            // 玩家太靠近下邊緣，攝影機向下移動
            targetCameraPos.y = playerPos.y + halfHeight - normalEdgeDistance;
        }
        else if (playerDistanceFromTop < normalEdgeDistance)
        {
            // 玩家太靠近上邊緣，攝影機向上移動
            targetCameraPos.y = playerPos.y - halfHeight + normalEdgeDistance;
        }
        else
        {
            // 玩家在安全範圍內，攝影機跟隨玩家
            targetCameraPos.y = playerPos.y;
        }
        
        // 平滑移動到目標位置
        float t = Mathf.Clamp01(horizontalLerpSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(cameraPos, targetCameraPos, t);
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
            
            // 應用邊緣限制
            Vector3 limitedMovement = ApplyCameraEdgeLimits(movement);
            transform.position += limitedMovement;

            if (useBounds)
                ClampCameraToBounds();
        }
    }

    // 依相機 viewport 大小與邊界做 clamp（避免相機移出邊界）
    void ClampCameraToBounds()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.aspect * halfHeight;

        float minX = minBounds.x + halfWidth;
        float maxX = maxBounds.x - halfWidth;
        float minY = minBounds.y + halfHeight;
        float maxY = maxBounds.y - halfHeight;

        Vector3 pos = transform.position;

        // 若邊界太小（比相機視口還小），則把相機固定在邊界中心
        if (minX > maxX)
            pos.x = (minBounds.x + maxBounds.x) * 0.5f;
        else
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY > maxY)
            pos.y = (minBounds.y + maxBounds.y) * 0.5f;
        else
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
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
        shouldFollowPlayer = false; // 釋放Space鍵後不再跟隨玩家
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
        
        // 獲取當前邊緣距離限制
        float currentEdgeDistance = normalEdgeDistance; // 可以根據遊戲狀態調整
        
        Vector3 limitedMovement = movement;
        
        // 水平限制
        if (movement.x < 0 && playerDistanceFromRight <= currentEdgeDistance)
        {
            // 攝影機向左移動但玩家已經接近右邊緣（攝影機遠離玩家）
            limitedMovement.x = 0;
        }
        else if (movement.x > 0 && playerDistanceFromLeft <= currentEdgeDistance)
        {
            // 攝影機向右移動但玩家已經接近左邊緣（攝影機遠離玩家）
            limitedMovement.x = 0;
        }
        
        // 垂直限制
        if (movement.y < 0 && playerDistanceFromTop <= currentEdgeDistance)
        {
            // 攝影機向下移動但玩家已經接近上邊緣（攝影機遠離玩家）
            limitedMovement.y = 0;
        }
        else if (movement.y > 0 && playerDistanceFromBottom <= currentEdgeDistance)
        {
            // 攝影機向上移動但玩家已經接近下邊緣（攝影機遠離玩家）
            limitedMovement.y = 0;
        }
        
        return limitedMovement;
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