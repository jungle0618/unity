using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// 玩家主控制器（繼承 BaseEntity）
/// 職責：處理輸入、血量管理、武器控制、遊戲邏輯
/// 移動邏輯已移至 PlayerMovement 組件
/// 
/// 【封裝原則】
/// 所有 Player 狀態的修改都必須通過 Player 類的公共方法進行，禁止直接訪問內部組件來修改狀態。
/// 
/// ❌ 錯誤範例（禁止）：
///   - player.Movement.SetSpeed(2);  // 直接訪問 PlayerMovement
///   - player.Detection.SetDetectionParameters(...);  // 直接訪問 PlayerDetection
///   - player.gameObject.GetComponent<PlayerMovement>().moveSpeed = 2;  // 直接修改屬性
/// 
/// ✅ 正確範例（推薦）：
///   - player.TakeDamage(10, "Enemy");
///   - player.Heal(20);
///   - player.SetHealth(100);
///   - player.SetMouseAiming(true);
///   - player.SetWeaponDirection(direction);
/// 
/// 如需修改 Player 的狀態，請使用以下公共方法：
///   - TakeDamage() - 造成傷害
///   - Heal() - 治療
///   - SetHealth() - 設定血量
///   - FullHeal() - 完全治療
///   - IncreaseMaxHealth() - 增加最大血量
///   - SetMouseAiming() - 設定滑鼠瞄準模式
///   - SetWeaponFollowMovement() - 設定武器是否跟隨移動
///   - SetWeaponDirection() - 手動設定武器方向
///   - Resurrect() - 復活玩家
/// </summary>
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

    [Header("動畫控制器")]
    [SerializeField] private PlayerAnimationController animationController;

    
    // 注意：viewRange 和 viewAngle 現在從基類 BaseEntity 獲取（baseViewRange, baseViewAngle）
    
    // ItemManager 引用（用於撿取物品）
    private ItemManager itemManager;
    
    // 遊戲結束條件檢查頻率控制
    private float gameEndCheckTime = 0f;
    private const float GAME_END_CHECK_INTERVAL = 0.5f; // 每 0.5 秒檢查一次

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

    // 血量相關屬性（委託給 EntityHealth）
    public int MaxHealth => entityHealth != null ? entityHealth.MaxHealth : 0;
    public int CurrentHealth => entityHealth != null ? entityHealth.CurrentHealth : 0;
    public float HealthPercentage => entityHealth != null ? entityHealth.HealthPercentage : 0f;
    // IsDead 已由基類 BaseEntity 提供（基於 stateMachine.IsDead）
    public bool IsInvulnerable => entityHealth != null ? entityHealth.IsInvulnerable : false;
    
    // 蹲下相關屬性（從 PlayerMovement 獲取）
    public bool IsSquatting => playerMovement != null && playerMovement.IsSquatting;
    
    // 視野相關屬性（從基類獲取）
    public float ViewRange => BaseViewRange;
    public float ViewAngle => BaseViewAngle;
    
    // 速度乘數屬性
    public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
    public float SquatSpeedMultiplier => squatSpeedMultiplier;

    // 血量變化事件（委託給 EntityHealth）
    public event System.Action<int, int> OnHealthChanged
    {
        add { if (entityHealth != null) entityHealth.OnHealthChanged += value; }
        remove { if (entityHealth != null) entityHealth.OnHealthChanged -= value; }
    }
    
    public event System.Action OnPlayerDied; // 玩家死亡事件
    public event System.Action OnPlayerReachedSpawnPoint; // 玩家回到出生點事件

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
    /// </summary>
    protected override void InitializeBaseValues()
    {
        base.InitializeBaseValues(); // 調用基類方法

        // 從組件讀取基礎值（如果基類尚未設定）
        // 注意：Player 的基礎速度應該在 Inspector 中設定
        // 如果未設定，使用預設值（Player 的預設基礎速度為 5）
        if (playerMovement != null && baseSpeed <= 0f)
        {
            baseSpeed = 5f; // Player 的預設基礎速度
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
    }
    
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 調用基類 FixedUpdate
    }
    
    private void LateUpdate()
    {
        // 在 LateUpdate 中處理視覺相關的旋轉，避免與物理更新衝突
        HandleRotation();
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

        if (ItemHolder.TryAttack(gameObject))
            animationController.TriggerAttackAnimation3D();
    }

    private void OnActionPerformed(InputAction.CallbackContext ctx)
    {
        animationController.TriggerInteractAnimation();

        // 優先處理撿取物品
        bool itemPickedUp = TryPickupItem();

        // 如果沒有撿到物品，則嘗試開門
        if (!itemPickedUp)
        {
            TryOpenDoor();
        }
    }
    
    /// <summary>
    /// 嘗試撿取物品
    /// </summary>
    /// <returns>是否成功撿取物品</returns>
    private bool TryPickupItem()
    {
        if (itemManager == null || ItemHolder == null)
        {
            return false;
        }
        
        // 嘗試撿取最近的物品
        bool success = itemManager.TryPickupItem(transform.position, ItemHolder, pickupRange);
        
        if (success)
        {
            Debug.Log($"[Player] 成功撿取物品！當前物品數量: {ItemHolder.ItemCount}");
        }
        
        return success;
    }
    
    /// <summary>
    /// 嘗試開門
    /// </summary>
    private void TryOpenDoor()
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
        }
        else
        {
            Debug.LogWarning("[Player] DoorController 實例不存在");
        }
    }

    private void OnSwitchWeaponPerformed(InputAction.CallbackContext ctx)
    {
        // 切換武器邏輯（只在武器之間循環，不包括鑰匙等）
        if (ItemHolder != null)
        {
            ItemHolder.SwitchToNextWeapon(); // 只切換武器 + 空手
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

    public void TakeDamage(int damage, string source = "")
    {
        if (entityHealth == null) return;
        
        // 使用 EntityHealth 處理傷害（死亡事件會自動觸發 Die()）
        entityHealth.TakeDamage(damage, source, "玩家");
    }
    
    /// <summary>
    /// 獲取實體類型（實現 IEntity 接口）
    /// </summary>
    public EntityManager.EntityType GetEntityType()
    {
        return EntityManager.EntityType.Player;
    }

    /// <summary>
    /// 治療
    /// </summary>
    /// <param name="healAmount">治療量</param>
    public void Heal(int healAmount)
    {
        if (entityHealth == null) return;
        entityHealth.Heal(healAmount);
    }

    /// <summary>
    /// 完全治療
    /// </summary>
    public void FullHeal()
    {
        if (entityHealth == null) return;
        entityHealth.FullHeal();
    }

    /// <summary>
    /// 設定血量
    /// </summary>
    /// <param name="health">新的血量值</param>
    public void SetHealth(int health)
    {
        if (entityHealth == null) return;
        entityHealth.SetHealth(health);
        if (entityHealth.IsDead)
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
        if (entityHealth == null) return;
        entityHealth.IncreaseMaxHealth(amount);
    }

    /// <summary>
    /// 玩家死亡處理（覆寫基類方法，處理 Player 特定邏輯）
    /// </summary>
    protected override void OnDeath()
    {
        // 更新狀態機
        playerStateMachine?.ChangeState(PlayerState.Dead);
        
        // 清除移動目標（Player 特定）
        if (playerMovement != null)
        {
            playerMovement.ClearMoveTarget();
        }

        Debug.Log("玩家死亡！");
        
        // 觸發 Player 特定事件，讓 GameManager 處理
        OnPlayerDied?.Invoke();
        
        // 延遲禁用玩家物件，確保事件處理完成
        StartCoroutine(DisablePlayerAfterDeath());
    }
    
    /// <summary>
    /// 延遲禁用玩家物件
    /// </summary>
    private System.Collections.IEnumerator DisablePlayerAfterDeath()
    {
        // 等待一幀，確保事件處理完成
        yield return null;
        
        // 可以在這裡添加死亡邏輯
        // 例如：播放死亡動畫、停止移動、禁用輸入等
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

