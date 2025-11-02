using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// 玩家主控制器（繼承 BaseEntity）
/// 職責：處理輸入、血量管理、武器控制、遊戲邏輯
/// 移動邏輯已移至 PlayerMovement 組件
/// </summary>
[RequireComponent(typeof(PlayerMovement), typeof(PlayerDetection), typeof(PlayerVisualizer))]
public class Player : BaseEntity<PlayerState>
{
    // 組件引用（使用屬性來訪問具體類型，避免序列化衝突）
    private PlayerMovement playerMovement => movement as PlayerMovement;
    private PlayerDetection playerDetection => detection as PlayerDetection;
    private PlayerVisualizer playerVisualizer => visualizer as PlayerVisualizer;
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
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invulnerabilityTime = 1f;
    private float lastDamageTime = -999f;
    
    [Header("血量顏色設定")]
    [SerializeField] private bool useHealthColor = true;
    [SerializeField] private Color healthyColor = Color.white;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private float criticalHealthThreshold = 0.3f; // 30%以下為危險血量
    [SerializeField] private float damagedHealthThreshold = 0.6f; // 60%以下為受傷血量
    
    [Header("遊戲結束條件設定")]
    [SerializeField] private Vector3 playerSpawnPoint = new Vector3(-52.31531f, -1.914664f, 0f);
    [SerializeField] private float spawnPointTolerance = 2f; // 玩家出生點容差範圍
    [Tooltip("遊戲目標物件，如果設定了此物件，當目標死亡且玩家回到出生點時遊戲勝利")]
    [SerializeField] private GameObject targetObject; // 改為序列化欄位，可在 Inspector 中指定
    
    [Header("玩家視野設定")]
    [SerializeField] private float viewRange = 8f; // 玩家視野範圍（固定值）
    [SerializeField] private float viewAngle = 90f; // 玩家視野角度（固定值）
    
    [Header("物品撿取設定")]
    [SerializeField] private float pickupRange = 2f; // 物品撿取範圍
    
    // 快取 Target Enemy 組件引用
    private Enemy targetEnemy;
    
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

    // 血量相關屬性和事件
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    // IsDead 已由基類 BaseEntity 提供（基於 stateMachine.IsDead）
    public bool IsInvulnerable => Time.time < lastDamageTime + invulnerabilityTime;
    
    // 蹲下相關屬性（從 PlayerMovement 獲取）
    public bool IsSquatting => playerMovement != null && playerMovement.IsSquatting;
    
    // 視野相關屬性
    public float ViewRange => viewRange;
    public float ViewAngle => viewAngle;

    public event System.Action<int, int> OnHealthChanged; // 當前血量, 最大血量
    public event System.Action OnPlayerDied; // 玩家死亡事件
    public event System.Action OnPlayerWon; // 玩家勝利事件

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
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 獲取攝影機
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();
            
        // 獲取攝影機控制器
        cameraController = FindFirstObjectByType<CameraController2D>();

        // 初始化輸入系統
        inputActions = new InputSystem_Actions();

        // 初始化血量
        currentHealth = maxHealth;
        
        // 初始化顏色
        UpdateSpriteColor();
        
        // 快取 Target 物件引用
        CacheTargetReference();
        
        // 獲取 ItemManager（用於撿取物品）
        itemManager = FindFirstObjectByType<ItemManager>();
        if (itemManager == null)
        {
            Debug.LogWarning("[Player] ItemManager not found in scene. Item pickup will not work.");
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
            inputActions.Player1.Attack.performed -= OnAttackPerformed;
            inputActions.Player1.Point.performed -= OnPointPerformed;
            inputActions.Player1.Click.performed -= OnClickPerformed;
            inputActions.Player1.Action.performed -= OnActionPerformed;
            inputActions.Player1.SwitchWeapon.performed -= OnSwitchWeaponPerformed;

            inputActions.Disable();
        }
    }

    protected override void Update()
    {
        base.Update(); // 調用基類 Update
        
        // 更新武器方向（限制更新頻率以提升性能）
        if (Time.time - weaponUpdateTime >= WEAPON_UPDATE_INTERVAL)
        {
            UpdateWeaponDirection();
            weaponUpdateTime = Time.time;
        }
        
        // 檢查遊戲結束條件（限制檢查頻率以提升性能）
        if (Time.time - gameEndCheckTime >= GAME_END_CHECK_INTERVAL)
        {
            CheckGameEndConditions();
            gameEndCheckTime = Time.time;
        }
    }
    
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 調用基類 FixedUpdate
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
        ItemHolder.TryAttack(gameObject);
    }

    private void OnActionPerformed(InputAction.CallbackContext ctx)
    {
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
        // 獲取玩家前方的位置
        Vector3 openPosition = transform.position + (Vector3)lastValidAimDirection * 1.5f;
        Debug.Log("OnActionPerformed: " + openPosition);
        
        // 呼叫DoorController來開啟門
        if (DoorController.Instance != null)
        {
            bool success = DoorController.Instance.RemoveDoorAtWorldPosition(openPosition);
            if (success)
            {
                Debug.Log($"[Player] 成功開啟/刪除門在位置: {openPosition}");
            }
            else
            {
                Debug.Log($"[Player] 在位置 {openPosition} 沒有找到門");
            }
        }
        else
        {
            Debug.LogWarning("[Player] DoorController 實例不存在");
        }
    }

    private void OnSwitchWeaponPerformed(InputAction.CallbackContext ctx)
    {
        // 切換物品邏輯
        if (ItemHolder != null)
        {
            ItemHolder.SwitchToNextItem();
        }
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
        
        // 更新顏色
        UpdateSpriteColor();

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
        
        // 更新顏色
        UpdateSpriteColor();

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
        
        // 更新顏色
        UpdateSpriteColor();
        
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
        
        // 更新顏色
        UpdateSpriteColor();

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
    /// 玩家死亡（覆寫基類方法）
    /// </summary>
    public override void Die()
    {
        if (IsDead) return;

        // 更新狀態機
        playerStateMachine?.ChangeState(PlayerState.Dead);
        
        // 停止移動
        if (playerMovement != null)
        {
            playerMovement.ClearMoveTarget();
        }

        Debug.Log("玩家死亡！");
        
        // 先觸發事件，讓 GameManager 處理
        OnPlayerDied?.Invoke();
        
        // 延遲禁用玩家物件，確保事件處理完成
        StartCoroutine(DisablePlayerAfterDeath());
        
        // 調用基類的死亡處理
        base.Die();
    }
    
    /// <summary>
    /// 死亡處理（覆寫基類方法）
    /// </summary>
    protected override void OnDeath()
    {
        // Player 特定的死亡邏輯已在 Die() 中處理
        // 基類的 Die() 已經處理了停止移動，這裡可以添加其他清理邏輯
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
    /// 復活玩家
    /// </summary>
    public void Resurrect()
    {
        currentHealth = maxHealth;
        lastDamageTime = -999f; // 重置無敵時間
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // 更新顏色
        UpdateSpriteColor();
        
        // 更新狀態機
        playerStateMachine?.ChangeState(PlayerState.Idle);
        
        gameObject.SetActive(true);
        Debug.Log("玩家復活！");
    }
    
    /// <summary>
    /// 更新sprite renderer的顏色
    /// </summary>
    private void UpdateSpriteColor()
    {
        if (spriteRenderer == null || !useHealthColor) return;
        
        Color targetColor;
        float healthPercentage = HealthPercentage;
        
        if (healthPercentage <= criticalHealthThreshold)
        {
            targetColor = criticalColor;
        }
        else if (healthPercentage <= damagedHealthThreshold)
        {
            targetColor = damagedColor;
        }
        else
        {
            targetColor = healthyColor;
        }
        
        spriteRenderer.color = targetColor;
    }
    
    /// <summary>
    /// 設定血量顏色
    /// </summary>
    public void SetHealthColors(Color healthy, Color damaged, Color critical)
    {
        healthyColor = healthy;
        damagedColor = damaged;
        criticalColor = critical;
        UpdateSpriteColor();
    }
    
    /// <summary>
    /// 設定是否使用血量顏色
    /// </summary>
    public void SetUseHealthColor(bool useHealthColor)
    {
        this.useHealthColor = useHealthColor;
        UpdateSpriteColor();
    }

    #endregion

    #region 遊戲結束條件

    /// <summary>
    /// 檢查遊戲結束條件
    /// </summary>
    private void CheckGameEndConditions()
    {
        // 如果玩家已死亡，不檢查勝利條件
        if (IsDead) return;
        
        // 如果沒有設定 Target，不檢查勝利條件
        if (targetObject == null) return;
        
        // 檢查 Target 是否死亡且玩家在出生點
        if (IsTargetDead() && IsPlayerAtSpawnPoint())
        {
            Debug.Log("[Player] Game end condition met: Target is dead and player is at spawn point");
            Win();
        }
    }
    
    /// <summary>
    /// 快取 Target 物件引用
    /// </summary>
    private void CacheTargetReference()
    {
        // 如果沒有在 Inspector 中指定，嘗試自動查找名為 "Target" 的物件
        if (targetObject == null)
        {
            targetObject = GameObject.Find("Target");
        }
        
        if (targetObject != null)
        {
            targetEnemy = targetObject.GetComponent<Enemy>();
            Debug.Log("[Player] Target object cached successfully");
        }
        else
        {
            // 降低警告級別，因為不是所有場景都需要 Target
            Debug.Log("[Player] Target object not found in scene. Win condition disabled.");
        }
    }
    
    /// <summary>
    /// 檢查 Target 是否死亡
    /// </summary>
    private bool IsTargetDead()
    {
        // 如果快取的引用無效，嘗試重新快取
        if (targetObject == null)
        {
            CacheTargetReference();
        }
        
        if (targetObject == null)
        {
            return false;
        }
        
        // 檢查 Target 是否有 Enemy 腳本（Target 直接使用 Enemy 組件，無需獨立實作）
        if (targetEnemy != null)
        {
            return targetEnemy.IsDead;
        }
        
        // 如果沒有 Enemy 腳本，檢查物件是否被禁用（死亡狀態）
        return !targetObject.activeInHierarchy;
    }
    
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
    /// 玩家勝利
    /// </summary>
    private void Win()
    {
        // 檢查是否已經處理過勝利（避免重複觸發）
        if (gameObject.activeSelf == false) return;

        Debug.Log("玩家勝利！");
        
        // 先觸發事件，讓 GameManager 處理
        OnPlayerWon?.Invoke();
        
        // 延遲禁用玩家物件，確保事件處理完成
        StartCoroutine(DisablePlayerAfterWin());
    }
    
    /// <summary>
    /// 延遲禁用玩家物件
    /// </summary>
    private System.Collections.IEnumerator DisablePlayerAfterWin()
    {
        // 等待一幀，確保事件處理完成
        yield return null;
        
        // 可以在這裡添加勝利邏輯
        // 例如：播放勝利動畫、停止移動、禁用輸入等
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 測試玩家勝利條件（僅用於測試）
    /// </summary>
    [ContextMenu("Test Player Win Condition")]
    public void TestPlayerWinCondition()
    {
        Debug.Log("[Player] Testing player win condition...");
        Debug.Log($"[Player] Target is dead: {IsTargetDead()}");
        Debug.Log($"[Player] Player at spawn point: {IsPlayerAtSpawnPoint()}");
        
        if (IsTargetDead() && IsPlayerAtSpawnPoint())
        {
            Debug.Log("[Player] Player win condition is met!");
            Win();
        }
        else
        {
            Debug.Log("[Player] Player win condition is not met yet.");
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

