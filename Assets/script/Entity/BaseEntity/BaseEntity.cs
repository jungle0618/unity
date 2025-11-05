using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 基礎實體抽象類別
/// 整合所有核心組件：State, Movement, Detection, Visualizer, WeaponManager
/// 所有人物物件（Enemy, Player, Target）都應該繼承此類別
/// </summary>
public abstract class BaseEntity<TState> : MonoBehaviour where TState : System.Enum
{
    // 核心組件引用（運行時獲取，不需要序列化）
    [System.NonSerialized] protected BaseStateMachine<TState> stateMachine;
    [System.NonSerialized] protected BaseMovement movement;
    [System.NonSerialized] protected BaseDetection detection;
    [System.NonSerialized] protected BaseVisualizer visualizer;
    [System.NonSerialized] protected ItemHolder itemHolder; // 已實作好的組件
    [System.NonSerialized] protected EntityHealth entityHealth; // 血量管理組件

    // 組件屬性（供外部訪問）
    public BaseStateMachine<TState> StateMachine => stateMachine;
    public BaseMovement Movement => movement;
    public BaseDetection Detection => detection;
    public BaseVisualizer Visualizer => visualizer;
    public ItemHolder ItemHolder => itemHolder;
    
    // 向後兼容的屬性
    public ItemHolder WeaponHolder => itemHolder;

    // 實體屬性
    public Vector2 Position => transform.position;
    public bool IsDead => stateMachine?.IsDead ?? false;

    [Header("基礎屬性（遊戲開始後不會改變）")]
    [Tooltip("基礎移動速度（遊戲開始後不會改變）")]
    [SerializeField] protected float baseSpeed = 2f;
    [Tooltip("基礎視野範圍（遊戲開始後不會改變）")]
    [SerializeField] protected float baseViewRange = 8f;
    [Tooltip("基礎視野角度（遊戲開始後不會改變）")]
    [SerializeField] protected float baseViewAngle = 90f;

    // 基礎屬性訪問器（只讀）
    public float BaseSpeed => baseSpeed;
    public float BaseViewRange => baseViewRange;
    public float BaseViewAngle => baseViewAngle;

    #region Unity 生命週期

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void Start()
    {
        InitializeEntity();
    }

    protected virtual void Update()
    {
        if (IsDead) return;
        UpdateEntity();
    }

    protected virtual void FixedUpdate()
    {
        if (IsDead) return;
        FixedUpdateEntity();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化所有組件
    /// </summary>
    protected virtual void InitializeComponents()
    {
        // 獲取組件引用
        movement = GetComponent<BaseMovement>();
        detection = GetComponent<BaseDetection>();
        visualizer = GetComponent<BaseVisualizer>();
        itemHolder = GetComponent<ItemHolder>();
        entityHealth = GetComponent<EntityHealth>();

        // 訂閱 EntityHealth 的死亡事件（統一處理）
        if (entityHealth != null)
        {
            entityHealth.OnEntityDied += HandleEntityDeath;
        }

        // 初始化基礎數值（從組件讀取，如果尚未設定）
        InitializeBaseValues();

        // 驗證必要組件
        ValidateComponents();
    }
    
    /// <summary>
    /// 處理實體死亡（由 EntityHealth 調用，統一處理）
    /// </summary>
    protected virtual void HandleEntityDeath()
    {
        Die();
    }

    /// <summary>
    /// 初始化基礎數值（從組件讀取，如果尚未設定）
    /// 這些值在遊戲開始後不會改變
    /// </summary>
    protected virtual void InitializeBaseValues()
    {
        // 如果基礎數值未設定（為 0 或負數），從組件讀取
        if (baseSpeed <= 0f && movement != null)
        {
            baseSpeed = movement.GetSpeed();
        }

        if (baseViewRange <= 0f && detection != null)
        {
            // 嘗試從 Detection 組件獲取視野範圍
            // 注意：BaseDetection 可能沒有直接的方法獲取，子類別需要覆寫此方法
        }

        // 基礎數值一旦設定就不應該再改變
        // 子類別可以覆寫此方法來從特定組件讀取初始值
    }

    /// <summary>
    /// 驗證必要組件是否存在
    /// </summary>
    protected virtual void ValidateComponents()
    {
        if (movement == null)
            Debug.LogWarning($"{gameObject.name}: Missing BaseMovement component!");

        if (detection == null)
            Debug.LogWarning($"{gameObject.name}: Missing BaseDetection component!");

        if (visualizer == null)
            Debug.LogWarning($"{gameObject.name}: Missing BaseVisualizer component!");

        // ItemHolder 是可選的
        if (itemHolder == null)
            Debug.Log($"{gameObject.name}: No ItemHolder component (optional)");
    }

    /// <summary>
    /// 初始化實體（由子類別實現具體邏輯）
    /// </summary>
    protected abstract void InitializeEntity();

    #endregion

    #region 更新邏輯

    /// <summary>
    /// 更新實體（每幀調用）
    /// </summary>
    protected virtual void UpdateEntity()
    {
        // 子類別可以覆寫此方法來實現具體更新邏輯
    }

    /// <summary>
    /// 固定更新實體（固定時間步長調用）
    /// </summary>
    protected virtual void FixedUpdateEntity()
    {
        // 更新狀態機
        if (stateMachine != null)
        {
            stateMachine.UpdateState(Time.fixedDeltaTime);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 設定目標（委託給 Detection 組件）
    /// </summary>
    public virtual void SetTarget(Transform target)
    {
        detection?.SetTarget(target);
    }

    /// <summary>
    /// 獲取目標（委託給 Detection 組件）
    /// </summary>
    public virtual Transform GetTarget()
    {
        return detection?.GetTarget();
    }

    /// <summary>
    /// 實體死亡（統一處理通用邏輯）
    /// </summary>
    public virtual void Die()
    {
        // 檢查是否已經死亡（避免重複處理）
        if (IsDead) return;

        // 停止移動（通用操作）
        movement?.StopMovement();

        // 清理 BaseVisualizer 創建的對象
        if (visualizer != null)
        {
            visualizer.CleanupCreatedObjects();
        }

        // 通知子類別處理特定死亡邏輯（狀態改變、事件觸發等）
        OnDeath();
        
        // 掉落物品（通用操作，子類可以覆寫 DropAllItems 來控制是否掉落）
        DropAllItems();
    }

    /// <summary>
    /// 死亡處理（由子類別實現）
    /// </summary>
    protected virtual void OnDeath()
    {
        
    }
    
    /// <summary>
    /// 掉落所有物品（死亡時調用）
    /// </summary>
    protected virtual void DropAllItems()
    {
        // 檢查 itemHolder 是否存在（不是所有實體都有 ItemHolder 組件，這是正常的）
        if (itemHolder == null || itemHolder.ItemCount == 0)
        {
            // 如果 itemHolder 為 null，這是正常的（某些實體不需要物品系統）
            // 只有在 itemHolder 存在但沒有物品時才記錄調試信息
            if (itemHolder != null && itemHolder.ItemCount == 0)
            {
                // 有 itemHolder 但沒有物品，這是正常的，不需要記錄
                // Debug.Log($"{gameObject.name}: itemHolder 存在但沒有物品，跳過掉落");
            }
            // itemHolder 為 null 的情況不記錄，因為這是設計選擇
            return; // 沒有物品需要掉落
        }
        
        // 尋找場景中的 ItemManager
        ItemManager itemManager = FindFirstObjectByType<ItemManager>();
        if (itemManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot drop items - ItemManager not found in scene!");
            return;
        }
        
        // 獲取所有物品及其對應的 Prefab
        var itemsWithPrefabs = itemHolder.GetAllItemsWithPrefabs();
        
        if (itemsWithPrefabs.Count == 0)
        {
            return; // 沒有物品需要掉落
        }
        
        // 提取 Prefab 列表
        List<GameObject> prefabs = new List<GameObject>();
        foreach (var pair in itemsWithPrefabs)
        {
            if (pair.Value != null)
            {
                prefabs.Add(pair.Value);
            }
        }
        
        // 在死亡位置掉落物品（圓形散落）
        itemManager.DropItemsAtPosition(prefabs, transform.position, 1.5f);
        
        // 清空 ItemHolder 的所有物品
        itemHolder.ClearAllItems();
        
        Debug.Log($"{gameObject.name}: Dropped {prefabs.Count} items at death position");
    }

    /// <summary>
    /// 檢查是否可以看到目標
    /// </summary>
    public virtual bool CanSeeTarget(Vector2 targetPos)
    {
        return detection?.CanSeeTarget(targetPos) ?? false;
    }

    /// <summary>
    /// 獲取到目標的距離
    /// </summary>
    public virtual float GetDistanceToTarget()
    {
        return detection?.GetDistanceToTarget() ?? float.MaxValue;
    }

    #endregion

    #region 清理

    protected virtual void OnDestroy()
    {
        // 清理資源
        Cleanup();
    }

    /// <summary>
    /// 清理資源（由子類別實現）
    /// </summary>
    protected virtual void Cleanup()
    {
        // 取消訂閱 EntityHealth 事件
        if (entityHealth != null)
        {
            entityHealth.OnEntityDied -= HandleEntityDeath;
        }
    }

    #endregion
}

