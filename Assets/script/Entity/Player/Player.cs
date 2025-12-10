using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerDetection))]
[RequireComponent(typeof(PlayerVisualizer))]
[RequireComponent(typeof(EntityHealth))]
[DefaultExecutionOrder(100)] // 在 EntityManager (50) 之後，GameManager (150) 之前執行
public class Player : BaseEntity<PlayerState>, IEntity
{
    // 組件引用（使用屬性來訪問具體類型，避免序列化衝突）
    private PlayerMovement playerMovement => movement as PlayerMovement;
    private PlayerDetection playerDetection => detection as PlayerDetection;
    private PlayerVisualizer playerVisualizer => visualizer as PlayerVisualizer;
    // 注意：entityHealth 已移至基類 BaseEntity
    // ItemHolder 直接從基類訪問（BaseEntity 提供 ItemHolder 屬性）
    // 狀態機需要單獨存儲，因為它是在運行時創建的，不是組件
    [System.NonSerialized] private PlayerStateMachine playerStateMachineInstance;
    private PlayerStateMachine playerStateMachine => playerStateMachineInstance ?? (playerStateMachineInstance = stateMachine as PlayerStateMachine);
    private SpriteRenderer spriteRenderer;
    
    [Header("武器設定")]
    [SerializeField] private bool useMouseAiming = true;
    [Tooltip("如果沒有滑鼠瞄準輸入，武器是否跟隨移動方向")]
    [SerializeField] private bool weaponFollowMovement = true;

    [Header("血量設定")]
    [Tooltip("無敵時間（秒）")]
    [SerializeField] private float invulnerabilityTime = 1f;
    
    // 注意：血量相關邏輯已移至 EntityHealth 組件
    
    // 注意：血量顏色設定已移至 PlayerVisualizer 組件
    
    [Header("遊戲結束條件設定")]
    [SerializeField] private Vector3 playerSpawnPoint = new Vector3(-52.31531f, -1.914664f, 0f);
    [SerializeField] private float spawnPointTolerance = 2f; // 玩家出生點容差範圍
    
    // 公共屬性：供外部訪問出生點位置
    public Vector3 SpawnPoint => playerSpawnPoint;
    public float SpawnPointTolerance => spawnPointTolerance;
    
    [Header("玩家移動速度乘數")]
    [Tooltip("追擊速度倍數（相對於基礎速度）")]
    [SerializeField] private float chaseSpeedMultiplier = 1.0f;
    [Tooltip("蹲下速度倍數（相對於基礎速度）")]
    [SerializeField] private float squatSpeedMultiplier = 0.5f;
    
    [Header("物品撿取設定")]
    [SerializeField] private float pickupRange = 2f; // 物品撿取範圍
    [Tooltip("是否啟用自動撿起物品（無需按 E 鍵）")]
    [SerializeField] private bool enableAutoPickup = true; // 是否啟用自動撿起
    [Tooltip("自動撿起檢查間隔（秒），避免每幀都檢查")]
    [SerializeField] private float autoPickupCheckInterval = 0.1f; // 自動撿起檢查間隔
    
    [Header("視野設定")]
    [Tooltip("近距離360度視野範圍（全方向，半徑1-2）")]
    [SerializeField] private float nearViewRange = 1.5f; // 360度視野範圍
    
    // 注意：viewRange 和 viewAngle 現在從基類 BaseEntity 獲取（baseViewRange, baseViewAngle）
    
    // ItemManager 引用（用於撿取物品）
    private ItemManager itemManager;
    
    // 遊戲結束條件檢查頻率控制
    private float gameEndCheckTime = 0f;
    private const float GAME_END_CHECK_INTERVAL = 0.5f; // 每 0.5 秒檢查一次
    
    // 自動撿起檢查頻率控制
    private float autoPickupCheckTime = 0f;

    // 滑鼠/指標相關
    private Vector2 currentPointerScreenPos;
    private Vector2 lastValidAimDirection = Vector2.right; // 預設向右
    private Camera playerCamera;
    private CameraController2D cameraController;

    // 武器更新頻率控制
    private float weaponUpdateTime = 0f;
    private const float WEAPON_UPDATE_INTERVAL = 0.05f; // 20 FPS 更新武器方向
    
    // 輸入系統
    private InputSystem_Actions inputActions;

    // 血量相關屬性已由基類 BaseEntity 統一提供（MaxHealth, CurrentHealth, HealthPercentage, IsInvulnerable）
    // 視野相關屬性已由基類 BaseEntity 統一提供（ViewRange, ViewAngle）
    // IsDead 已由基類 BaseEntity 提供（基於 entityHealth.IsDead，即生命值 <= 0）
    
    // 蹲下相關屬性（從 PlayerMovement 獲取）
    public bool IsSquatting => playerMovement != null && playerMovement.IsSquatting;
    
    // 速度乘數屬性
    public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
    public float SquatSpeedMultiplier => squatSpeedMultiplier;
    
    // 視野屬性
    public float NearViewRange => nearViewRange; // 360度視野範圍

    // 血量變化事件已由基類 BaseEntity 統一提供
    
    public event System.Action OnPlayerDied; // 玩家死亡事件
    public event System.Action OnPlayerReachedSpawnPoint; // 玩家回到出生點事件

    #region Animation & Sound Events

    // Movement events
    public event System.Action OnStartedMoving;
    public event System.Action OnStoppedMoving;
    public event System.Action OnStartedRunning;
    public event System.Action OnStoppedRunning;
    public event System.Action<Vector2> OnMovementDirectionChanged;
    public event System.Action<float> OnSpeedChanged;

    // Hand/Weapon state events
    public event System.Action<Item> OnEquipChanged;
    public event System.Action<Weapon> OnWeaponAttack;
    public event System.Action<bool> OnItemUse;

    // Internal tracking
    private bool wasMoving = false;
    private bool wasRunning = false;
    private Vector2 lastDirection = Vector2.zero;
    private float lastSpeed = 0f;
    private Item lastEquippedItem = null;
    private bool wasAttacking = false;
    private bool wasUsingItem = false;
    private bool doorSuccessed = false;

    #endregion

    #region Unity 生命週期

    protected override void Awake()
    {
        // 重要：必須在 base.Awake() 之前初始化狀態機
        // 因為 base.Awake() 會調用 ValidateComponents() 來檢查狀態機是否存在
        playerStateMachineInstance = new PlayerStateMachine();
        base.stateMachine = playerStateMachineInstance; // 賦值給基類引用
        
        base.Awake(); // 調用基類 Awake，初始化基類的組件引用
        
        // 獲取具體類型的組件引用
        movement = GetComponent<PlayerMovement>();
        detection = GetComponent<PlayerDetection>();
        visualizer = GetComponent<PlayerVisualizer>();
        entityHealth = GetComponent<EntityHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 獲取攝影機
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();
            
        // 獲取攝影機控制器
        cameraController = FindFirstObjectByType<CameraController2D>();

        // 初始化輸入系統
        inputActions = new InputSystem_Actions();

        // 初始化 EntityHealth
        if (entityHealth != null)
        {
            entityHealth.SetInvulnerabilityTime(invulnerabilityTime);
            entityHealth.InitializeHealth();
            // 基類已訂閱死亡事件，無需重複訂閱
        }
        
        // 獲取 ItemManager（用於撿取物品）
        itemManager = FindFirstObjectByType<ItemManager>();
        if (itemManager == null)
        {
            Debug.LogWarning("[Player] ItemManager not found in scene. Item pickup will not work.");
        }
    }

    /// <summary>
    /// 初始化基礎數值（覆寫基類方法）
    /// 
    /// 【重要】建議在 Inspector 中直接設置 baseSpeed = 5.0，而不是依賴此方法
    /// 此方法僅作為後備方案，如果 Inspector 中未設置才會使用默認值
    /// </summary>
    protected override void InitializeBaseValues()
    {
        base.InitializeBaseValues(); // 調用基類方法

        // 如果基礎速度未在 Inspector 中設定（≤0），使用 Player 的默認值
        // 注意：建議在 Inspector 中直接設置 baseSpeed = 5.0
        if (baseSpeed <= 0f)
        {
            baseSpeed = 5f; // Player 的預設基礎速度（僅作為後備）
            Debug.LogWarning($"[Player] baseSpeed 未在 Inspector 中設置，使用默認值 5.0。建議在 Inspector 中直接設置。");
        }
        
        // 視野範圍和角度從 PlayerDetection 讀取（如果尚未設定）
        if (playerDetection != null)
        {
            if (baseViewRange <= 0f) baseViewRange = playerDetection.ViewRange;
            if (baseViewAngle <= 0f) baseViewAngle = playerDetection.ViewAngle;
        }
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();

            inputActions.Player1.Attack.performed += OnAttackPerformed;
            inputActions.Player1.Point.performed += OnPointPerformed;
            inputActions.Player1.Click.performed += OnClickPerformed;
            inputActions.Player1.Action.performed += OnActionPerformed;
            inputActions.Player1.SwitchWeapon.performed += OnSwitchWeaponPerformed;
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            // 取消所有事件訂閱
            inputActions.Player1.Attack.performed -= OnAttackPerformed;
            inputActions.Player1.Point.performed -= OnPointPerformed;
            inputActions.Player1.Click.performed -= OnClickPerformed;
            inputActions.Player1.Action.performed -= OnActionPerformed;
            inputActions.Player1.SwitchWeapon.performed -= OnSwitchWeaponPerformed;

            // 禁用 Player1 action map（必須在禁用整個輸入系統之前調用）
            inputActions.Player1.Disable();
            
            // 禁用整個輸入系統
            inputActions.Disable();
        }
    }
    
    protected override void OnDestroy()
    {
        // 調用基類的 OnDestroy 進行基礎清理
        base.OnDestroy();
        
        // 清理輸入系統資源
        if (inputActions != null)
        {
            // 取消所有事件訂閱
            inputActions.Player1.Attack.performed -= OnAttackPerformed;
            inputActions.Player1.Point.performed -= OnPointPerformed;
            inputActions.Player1.Click.performed -= OnClickPerformed;
            inputActions.Player1.Action.performed -= OnActionPerformed;
            inputActions.Player1.SwitchWeapon.performed -= OnSwitchWeaponPerformed;
            
            // 禁用 Player1 action map
            inputActions.Player1.Disable();
            
            // 禁用整個輸入系統
            inputActions.Disable();
            
            // 釋放資源
            inputActions.Dispose();
            inputActions = null;
        }
    }

    protected override void Update()
    {
        base.Update(); // 調用基類 Update
        
        // Fire animation events
        CheckAndFireAnimationEvents();
        
        // 數字鍵快速切換武器：1=Knife, 2=Gun, 3=Empty Hands
        HandleNumberKeyWeaponSwitch();
        
        // 更新武器方向（限制更新頻率以提升性能）
        if (Time.time - weaponUpdateTime >= WEAPON_UPDATE_INTERVAL)
        {
            UpdateWeaponDirection();
            weaponUpdateTime = Time.time;
        }
        
        // 檢查玩家是否回到出生點（限制檢查頻率以提升性能）
        if (Time.time - gameEndCheckTime >= GAME_END_CHECK_INTERVAL)
        {
            CheckPlayerReachedSpawnPoint(); // 檢查玩家是否回到出生點
            gameEndCheckTime = Time.time;
        }
        
        // 自動撿起物品（如果啟用）
        if (enableAutoPickup && Time.time - autoPickupCheckTime >= autoPickupCheckInterval)
        {
            TryAutoPickupItem();
            autoPickupCheckTime = Time.time;
        }
    }
    
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 調用基類 FixedUpdate
    }
    
    private void LateUpdate()
    {
        // 在 LateUpdate 中處理視覺相關的旋轉，避免與物理更新衝突
        // 只有在遊戲進行中時才旋轉玩家
        if (GameManager.Instance == null || !GameManager.Instance.IsPaused)
        {
            HandleRotation();
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化實體（實現基類抽象方法）
    /// </summary>
    protected override void InitializeEntity()
    {
        // 基礎初始化已完成（在 Awake 中）
        // 可以在這裡添加額外的初始化邏輯
        if (playerStateMachine != null)
        {
            playerStateMachine.ChangeState(PlayerState.Idle);
        }
    }
    
    /// <summary>
    /// 驗證必要組件（覆寫基類方法）
    /// </summary>
    protected override void ValidateComponents()
    {
        if (playerMovement == null)
            Debug.LogError($"{gameObject.name}: Missing PlayerMovement component!");

        if (playerDetection == null)
            Debug.LogError($"{gameObject.name}: Missing PlayerDetection component!");

        if (playerVisualizer == null)
            Debug.LogWarning($"{gameObject.name}: Missing PlayerVisualizer component!");

        if (playerStateMachine == null)
            Debug.LogError($"{gameObject.name}: Failed to initialize PlayerStateMachine!");
    }

    #endregion

    #region Animation Event Firing Logic

    /// <summary>
    /// Check and fire animation events based on player state changes
    /// </summary>
    private void CheckAndFireAnimationEvents()
    {
        if (playerMovement == null) return;

        // Check movement state - player is moving if has input or move target
        bool isCurrentlyMoving = playerMovement.MoveInput.sqrMagnitude > 0.01f || playerMovement.HasMoveTarget;
        if (isCurrentlyMoving != wasMoving)
        {
            if (isCurrentlyMoving)
                OnStartedMoving?.Invoke();
            else
                OnStoppedMoving?.Invoke();

            wasMoving = isCurrentlyMoving;
        }

        // Check running state (sprint)
        bool isCurrentlyRunning = playerMovement.IsRunning;
        if (isCurrentlyRunning != wasRunning)
        {
            if (isCurrentlyRunning)
                OnStartedRunning?.Invoke();
            else
                OnStoppedRunning?.Invoke();

            wasRunning = isCurrentlyRunning;
        }

        // Check movement direction changes
        Vector2 currentDirection = playerMovement.GetMovementDirection();
        if (Vector2.Distance(currentDirection, lastDirection) > 0.1f && currentDirection.sqrMagnitude > 0.01f)
        {
            OnMovementDirectionChanged?.Invoke(currentDirection);
            lastDirection = currentDirection;
        }

        // Check speed changes
        float currentSpeed = playerMovement.GetSpeed();
        if (Mathf.Abs(currentSpeed - lastSpeed) > 0.01f)
        {
            OnSpeedChanged?.Invoke(currentSpeed);
            lastSpeed = currentSpeed;
        }

        // Check weapon/item state changes
        if (itemHolder != null)
        {
            Item currentItem = itemHolder.CurrentItem;
            
            // Check if switched item
            if (currentItem != lastEquippedItem)
            {
                OnEquipChanged?.Invoke(currentItem);
            }

            lastEquippedItem = currentItem;
        }

        // Check attack/use actions
        if (wasAttacking)
        {
            OnWeaponAttack?.Invoke(itemHolder?.CurrentItem as Weapon);
            wasAttacking = false;
        }
        if (wasUsingItem)
        {
            OnItemUse?.Invoke(doorSuccessed);
            wasUsingItem = false;
        }
    }

    #endregion

    #region 輸入處理

    private void HandleRotation()
    {
        var direction = GetMouseWorldDirection();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnPointPerformed(InputAction.CallbackContext ctx)
    {
        currentPointerScreenPos = ctx.ReadValue<Vector2>();
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        if (playerCamera == null || playerMovement == null) return;

        float zDist = Mathf.Abs(playerCamera.transform.position.z - transform.position.z);
        Vector3 world = playerCamera.ScreenToWorldPoint(new Vector3(currentPointerScreenPos.x, currentPointerScreenPos.y, zDist));

        // 將點擊移動目標傳遞給 PlayerMovement
        playerMovement.SetMoveTarget(new Vector2(world.x, world.y));
        
        // 如果武器跟隨移動且沒有使用滑鼠瞄準，更新武器方向
        if (weaponFollowMovement && !useMouseAiming && playerMovement.HasMoveTarget)
        {
            Vector2 direction = (world - transform.position).normalized;
            if (direction.sqrMagnitude > 0.1f)
            {
                lastValidAimDirection = direction;
            }
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (ItemHolder == null) return;

        UpdateWeaponDirection();
        Debug.Log("[Player] OnAttackPerformed");
        if(ItemHolder.TryAttack(gameObject))
            wasAttacking = true;
    }

    private void OnActionPerformed(InputAction.CallbackContext ctx)
    {
        // 只處理開門（撿取物品已改為自動）
        wasUsingItem = true;
        doorSuccessed = TryOpenDoor();
    }
    
    /// <summary>
    /// 自動撿起物品（在 Update 中定期調用）
    /// </summary>
    private void TryAutoPickupItem()
    {
        // 如果玩家死亡，不自動撿起
        if (IsDead || itemManager == null || ItemHolder == null)
        {
            return;
        }
        
        // 嘗試撿取最近的物品（靜默模式，不輸出日誌）
        itemManager.TryPickupItem(transform.position, ItemHolder, pickupRange);
    }
    
    /// <summary>
    /// 嘗試開門
    /// </summary>
    private bool TryOpenDoor()
    {
        // 呼叫 DoorController 來開啟範圍內最近的門
        if (DoorController.Instance != null)
        {
            bool success = DoorController.Instance.TryOpenDoorWithKey(transform.position, ItemHolder);
            if (success)
            {
                Debug.Log($"[Player] 成功開啟門");
            }
            else
            {
                Debug.Log($"[Player] 無法開啟門（可能沒有鑰匙、沒有裝備鑰匙或範圍內沒有門）");
            }
            return success;
        }
        else
        {
            Debug.LogWarning("[Player] DoorController 實例不存在");
            return false;
        }
    }

    private void OnSwitchWeaponPerformed(InputAction.CallbackContext ctx)
    {
        if (ItemHolder == null) return;

        // 讀取滾輪值（Vector2）
        Vector2 scrollValue = ctx.ReadValue<Vector2>();
        
        // 滾輪向上（y > 0）：切換到下一個物品
        if (scrollValue.y > 0.1f)
        {
            ItemHolder.SwitchToNextWeapon();
        }
        // 滾輪向下（y < 0）：切換到上一個物品
        else if (scrollValue.y < -0.1f)
        {
            ItemHolder.SwitchToPreviousWeapon();
        }
    }

    private void HandleNumberKeyWeaponSwitch()
    {
        if (ItemHolder == null) return;
        
        // 1 = Knife
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            TrySwitchToWeaponType("Knife");
            return;
        }
        // 2 = Gun
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            TrySwitchToWeaponType("Gun");
            return;
        }
        // 3 = Empty Hands
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            ItemHolder.TryEquipEmptyHands();
            return;
        }
    }


    private void TrySwitchToWeaponType(string typeKey)
    {
        // 取得所有武器，依獲取順序（GetAllItems 的順序與加入一致）
        var allWeapons = ItemHolder.GetItemsOfType<Weapon>();
        Weapon target = null;
        foreach (var w in allWeapons)
        {
            if (GetWeaponTypeKey(w) == typeKey)
            {
                target = w; break;
            }
        }
        if (target == null) return;
        
        // 切換到該武器實例
        var allItems = ItemHolder.GetAllItems();
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] == target)
            {
                ItemHolder.SwitchToItem(i);
                return;
            }
        }
    }

    private string GetWeaponTypeKey(Weapon w)
    {
        if (w == null) return null;
        string n = w.ItemName != null ? w.ItemName.ToLowerInvariant() : null;
        if (!string.IsNullOrEmpty(n))
        {
            if (n.Contains("knife")) return "Knife";
            if (n.Contains("gun")) return "Gun";
        }
        string t = w.GetType().Name.ToLowerInvariant();
        if (t.Contains("knife")) return "Knife";
        if (t.Contains("gun")) return "Gun";
        return null;
    }

    #endregion

    #region 武器方向

    private void UpdateWeaponDirection()
    {
        if (ItemHolder == null) return;

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
        else if (weaponFollowMovement && playerMovement != null)
        {
            // 如果武器跟隨移動，從 PlayerMovement 獲取移動方向
            Vector2 moveInput = playerMovement.MoveInput;
            if (moveInput.sqrMagnitude > 0.1f)
            {
                lastValidAimDirection = moveInput.normalized;
                aimDirection = lastValidAimDirection;
            }
        }

        // 更新物品方向（向後兼容 UpdateWeaponDirection 方法）
        ItemHolder.UpdateWeaponDirection(aimDirection);
    }

    private Vector2 GetMouseWorldDirection()
    {
        if (playerCamera == null) return lastValidAimDirection;

        // 將滑鼠螢幕座標轉換為世界座標
        float zDist = Mathf.Abs(playerCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(currentPointerScreenPos.x, currentPointerScreenPos.y, zDist));

        // 計算從角色位置到滑鼠位置的方向
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        return direction;
    }

    /// <summary>
    /// 設定滑鼠瞄準模式
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
    /// 手動設定武器方向
    /// </summary>
    public void SetWeaponDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.1f)
        {
            lastValidAimDirection = direction.normalized;
            ItemHolder?.UpdateWeaponDirection(lastValidAimDirection);
        }
    }

    /// <summary>
    /// 取得當前武器方向
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
        return ItemHolder?.CanAttack() ?? false;
    }

    #endregion

    #region 血量管理

    // TakeDamage() 已由基類 BaseEntity 統一實現
    // 基類會自動處理傷害和死亡流程
    
    /// <summary>
    /// 獲取實體顯示名稱（覆寫以使用"玩家"作為顯示名稱）
    /// </summary>
    protected override string GetEntityDisplayName()
    {
        return "玩家";
    }
    
    /// <summary>
    /// 獲取實體類型（實現 IEntity 接口）
    /// </summary>
    public EntityManager.EntityType GetEntityType()
    {
        return EntityManager.EntityType.Player;
    }

    // 血量管理方法（Heal, FullHeal, SetHealth, IncreaseMaxHealth）已由基類 BaseEntity 統一提供

    /// <summary>
    /// 玩家死亡處理（覆寫基類方法）
    /// </summary>
    protected override void OnDeath()
    {
        playerStateMachine?.ChangeState(PlayerState.Dead);
        playerMovement?.ClearMoveTarget();
        OnPlayerDied?.Invoke();
        StartCoroutine(DisablePlayerAfterDeath());
    }
    
    /// <summary>
    /// 延遲禁用玩家物件
    /// </summary>
    private System.Collections.IEnumerator DisablePlayerAfterDeath()
    {
        yield return null;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 玩家死亡時不掉落物品（覆寫基類方法）
    /// </summary>
    protected override void DropAllItems()
    {
        // Player 死亡時不掉落物品，保留物品以便復活後使用
        // 如果需要掉落物品，可以移除此覆寫
    }

    /// <summary>
    /// 復活玩家
    /// </summary>
    public void Resurrect()
    {
        if (entityHealth == null) return;
        
        entityHealth.FullHeal();
        entityHealth.ResetInvulnerabilityTime(); // 重置無敵時間
        
        // 更新狀態機
        playerStateMachine?.ChangeState(PlayerState.Idle);
        
        gameObject.SetActive(true);
        Debug.Log("玩家復活！");
    }
    
    // 注意：視覺化相關方法已移至 PlayerVisualizer 組件

    #endregion

    #region 遊戲結束條件

    // 注意：遊戲結束條件檢查已移至 GameManager
    // Player 只負責觸發事件（OnPlayerReachedSpawnPoint, OnPlayerDied）
    // Target 管理已移至 GameManager，不再由 Player 處理
    
    /// <summary>
    /// 檢查玩家是否在出生點
    /// </summary>
    private bool IsPlayerAtSpawnPoint()
    {
        Vector3 playerPosition = transform.position;
        float distance = Vector3.Distance(playerPosition, playerSpawnPoint);
        
        return distance <= spawnPointTolerance;
    }
    
    /// <summary>
    /// 檢查玩家是否回到出生點（用於觸發事件）
    /// </summary>
    private bool hasReachedSpawnPoint = false;
    private void CheckPlayerReachedSpawnPoint()
    {
        if (!hasReachedSpawnPoint && IsPlayerAtSpawnPoint())
        {
            hasReachedSpawnPoint = true;
            OnPlayerReachedSpawnPoint?.Invoke();
            Debug.Log("[Player] 玩家回到出生點！");
        }
        else if (hasReachedSpawnPoint && !IsPlayerAtSpawnPoint())
        {
            // 如果離開出生點，重置標記
            hasReachedSpawnPoint = false;
        }
    }

    #endregion

    #region 除錯輔助

    private void OnDrawGizmosSelected()
    {
        // 顯示移動目標（如果有）
        if (playerMovement != null && playerMovement.HasMoveTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerMovement.MoveTarget, 0.2f);
            Gizmos.DrawLine(transform.position, playerMovement.MoveTarget);
        }

        // 顯示武器方向
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)lastValidAimDirection * 2f);
    }

    #endregion
}

