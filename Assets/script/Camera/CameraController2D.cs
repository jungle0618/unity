using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController2D : MonoBehaviour
{
    [Header("固定縮放 (相機大小固定)")]
    public float fixedOrthographicSize = 5f; // 相機固定的 orthographicSize（腳本不會改變）

    [Header("玩家引用")]
    public Transform player;                 // 指向 player 的 transform（用於跟隨玩家）

    [Header("跟隨玩家設定")]
    public bool followPlayer = true;         // 是否跟隨玩家

    [Header("WASD 攝影機移動")]
    public float cameraMoveSpeed = 5f;       // WASD 控制攝影機移動速度
    public bool enableCameraMovement = true; // 是否啟用攝影機移動

    [Header("邊緣限制設定")]
    public bool useEdgeLimits = true;        // 是否使用邊緣限制
    public float edgeDistance = 1.3f;          // 玩家距離邊緣的距離（單位：Unity單位），當玩家接近邊緣時相機不能往該方向移動

    private Camera cam;
    private InputSystem_Actions inputActions;
    private bool isCameraMode = false;       // 按下空白鍵時為 true，相機不跟隨玩家
    private EntityManager entityManager;     // EntityManager 引用，用於訂閱玩家生成事件
    private Vector3 lastPlayerPosition;      // 上一幀玩家位置，用於計算移動量
    private bool hasLastPlayerPosition = false; // 是否已記錄上一幀玩家位置
    private bool hasInitializedCameraPosition = false; // 是否已初始化相機位置（移動到玩家中心）

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
        
        // 嘗試立即查找玩家（如果 Inspector 中沒有設定）
        TryFindPlayer();
        
        // 如果還沒找到玩家，訂閱 EntityManager 的 OnPlayerReady 事件
        if (player == null)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
            if (entityManager != null)
            {
                entityManager.OnPlayerReady += OnPlayerReady;
            }
        }
        
        // 初始化輸入系統
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        
        // 綁定攝影機移動事件
        inputActions.Player1.MoveCamera.performed += OnMoveCameraPerformed;
        inputActions.Player1.MoveCamera.canceled += OnMoveCameraCanceled;
    }

    void Update()
    {
        // 如果還沒有找到玩家，持續嘗試查找（備用方案）
        if (player == null)
        {
            TryFindPlayer();
        }
        
        // 檢查是否按下 Y 鍵，將相機拉回以玩家為中心
        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
        {
            CenterCameraOnPlayer();
        }
        
        // 跟隨玩家（未按下空白鍵時）
        if (!isCameraMode && followPlayer && player != null)
        {
            FollowPlayer();
        }
        
        // WASD 控制攝影機移動（按住 Space 鍵時）
        HandleCameraMovement();
    }
    
    /// <summary>
    /// 嘗試查找玩家
    /// </summary>
    private void TryFindPlayer()
    {
        if (player != null) return;
        
        // 優先從 EntityManager 獲取
        if (entityManager == null)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
        }
        
        if (entityManager != null && entityManager.Player != null)
        {
            player = entityManager.Player.transform;
            // 初始化玩家位置記錄和相機位置
            if (player != null)
            {
                InitializeCameraToPlayer();
            }
            return;
        }
        
        // 備用方案：直接查找
        Player playerComponent = FindFirstObjectByType<Player>();
        if (playerComponent != null)
        {
            player = playerComponent.transform;
            // 初始化玩家位置記錄和相機位置
            if (player != null)
            {
                InitializeCameraToPlayer();
            }
        }
    }
    
    /// <summary>
    /// 當玩家準備就緒時調用（EntityManager.OnPlayerReady 事件處理）
    /// </summary>
    private void OnPlayerReady()
    {
        if (player == null && entityManager != null && entityManager.Player != null)
        {
            player = entityManager.Player.transform;
            // 初始化玩家位置記錄和相機位置
            if (player != null)
            {
                InitializeCameraToPlayer();
            }
        }
    }
    
    /// <summary>
    /// 初始化相機位置到玩家中心（只在第一次找到玩家時執行）
    /// </summary>
    private void InitializeCameraToPlayer()
    {
        if (player == null) return;
        
        // 初始化玩家位置記錄
        if (!hasLastPlayerPosition)
        {
            lastPlayerPosition = player.position;
            hasLastPlayerPosition = true;
        }
        
        // 將相機移動到玩家位置（保持 z 軸）
        if (!hasInitializedCameraPosition)
        {
            transform.position = new Vector3(player.position.x, player.position.y, transform.position.z);
            hasInitializedCameraPosition = true;
        }
    }
    
    /// <summary>
    /// 將相機拉回以玩家為中心（按下 Y 鍵時調用）
    /// </summary>
    private void CenterCameraOnPlayer()
    {
        if (player == null) return;
        
        // 將相機移動到玩家位置（保持 z 軸）
        transform.position = new Vector3(player.position.x, player.position.y, transform.position.z);
        
        // 更新玩家位置記錄，讓相機從新位置開始跟隨
        lastPlayerPosition = player.position;
        hasLastPlayerPosition = true;
    }

    /// <summary>
    /// 跟隨玩家移動（跟隨玩家的移動量，而不是移動到玩家位置）
    /// </summary>
    void FollowPlayer()
    {
        if (player == null) return;
        
        // 如果是第一次跟隨，記錄當前玩家位置，不移動相機
        if (!hasLastPlayerPosition)
        {
            lastPlayerPosition = player.position;
            hasLastPlayerPosition = true;
            return;
        }
        
        // 計算玩家本幀的移動量
        Vector3 playerMovement = player.position - lastPlayerPosition;
        
        // 將相機移動相同的量（跟隨玩家移動，而不是移動到玩家位置）
        transform.position += playerMovement;
        
        // 更新記錄的玩家位置
        lastPlayerPosition = player.position;
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
            
            // 應用邊緣限制（防止玩家被拉到相機外面）
            if (useEdgeLimits)
            {
                movement = ApplyEdgeLimits(movement);
            }
            
            transform.position += movement;
        }
    }
    
    /// <summary>
    /// 應用邊緣限制，防止玩家被拉到相機視口外面
    /// </summary>
    private Vector3 ApplyEdgeLimits(Vector3 movement)
    {
        if (player == null || cam == null) return movement;
        
        // 計算相機視口邊界
        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.aspect * halfHeight;
        
        Vector3 cameraPos = transform.position;
        Vector3 playerPos = player.position;
        
        // 計算相機邊界
        float leftEdge = cameraPos.x - halfWidth;
        float rightEdge = cameraPos.x + halfWidth;
        float bottomEdge = cameraPos.y - halfHeight;
        float topEdge = cameraPos.y + halfHeight;
        
        // 計算玩家相對於相機邊緣的距離
        float playerDistanceFromLeft = playerPos.x - leftEdge;
        float playerDistanceFromRight = rightEdge - playerPos.x;
        float playerDistanceFromBottom = playerPos.y - bottomEdge;
        float playerDistanceFromTop = topEdge - playerPos.y;
        
        Vector3 limitedMovement = movement;
        
        // 水平限制
        // 如果玩家接近左邊緣，且相機要向右移動（會讓玩家更接近左邊緣），則限制移動
        if (playerDistanceFromLeft <= edgeDistance && movement.x > 0)
        {
            limitedMovement.x = 0;
        }
        // 如果玩家接近右邊緣，且相機要向左移動（會讓玩家更接近右邊緣），則限制移動
        else if (playerDistanceFromRight <= edgeDistance && movement.x < 0)
        {
            limitedMovement.x = 0;
        }
        
        // 垂直限制
        // 如果玩家接近下邊緣，且相機要向上移動（會讓玩家更接近下邊緣），則限制移動
        if (playerDistanceFromBottom <= edgeDistance && movement.y > 0)
        {
            limitedMovement.y = 0;
        }
        // 如果玩家接近上邊緣，且相機要向下移動（會讓玩家更接近上邊緣），則限制移動
        else if (playerDistanceFromTop <= edgeDistance && movement.y < 0)
        {
            limitedMovement.y = 0;
        }
        
        return limitedMovement;
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
    /// 攝影機移動模式開始（按下空白鍵時，停止跟隨玩家）
    /// </summary>
    private void OnMoveCameraPerformed(InputAction.CallbackContext ctx)
    {
        isCameraMode = true;
        
        // 更新玩家位置記錄，確保記錄是最新的
        if (player != null)
        {
            lastPlayerPosition = player.position;
            hasLastPlayerPosition = true;
        }
    }
    
    /// <summary>
    /// 攝影機移動模式結束（放開空白鍵時，繼續跟隨玩家，不重新置中）
    /// </summary>
    private void OnMoveCameraCanceled(InputAction.CallbackContext ctx)
    {
        isCameraMode = false;
        
        // 重置玩家位置記錄，讓相機從當前位置開始跟隨
        if (player != null)
        {
            lastPlayerPosition = player.position;
            hasLastPlayerPosition = true;
        }
    }
    
    private void OnDestroy()
    {
        // 取消 EntityManager 事件訂閱
        if (entityManager != null)
        {
            entityManager.OnPlayerReady -= OnPlayerReady;
        }
        
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