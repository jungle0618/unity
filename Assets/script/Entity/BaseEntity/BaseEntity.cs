using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    
    // 死亡處理標記
    private bool isHandleDie = false;

    // 組件屬性（供外部訪問）
    public BaseStateMachine<TState> StateMachine => stateMachine;
    public BaseMovement Movement => movement;
    public BaseDetection Detection => detection;
    public BaseVisualizer Visualizer => visualizer;
    public ItemHolder ItemHolder => itemHolder;

    // 實體屬性
    public Vector2 Position => transform.position;
    public bool IsDead => entityHealth?.IsDead ?? false;
    
    // 視野相關屬性（統一提供，避免子類重複定義）
    public float ViewRange => BaseViewRange;
    public float ViewAngle => BaseViewAngle;
    
    // 血量相關屬性（統一提供，避免子類重複定義）
    public int MaxHealth => entityHealth?.MaxHealth ?? 0;
    public int CurrentHealth => entityHealth?.CurrentHealth ?? 0;
    public float HealthPercentage => entityHealth?.HealthPercentage ?? 0f;
    public bool IsInvulnerable => entityHealth?.IsInvulnerable ?? false;
    
    // 血量變化事件（統一提供，避免子類重複定義）
    public event System.Action<int, int> OnHealthChanged
    {
        add { if (entityHealth != null) entityHealth.OnHealthChanged += value; }
        remove { if (entityHealth != null) entityHealth.OnHealthChanged -= value; }
    }

    [Header("基礎屬性（遊戲開始後不會改變）")]
    [Tooltip("基礎移動速度（遊戲開始後不會改變）\n" +
             "建議在 Inspector 中直接設置此值：\n" +
             "- Player: 5.0\n" +
             "- Enemy: 6.0\n" +
             "- Target: 2.0\n" +
             "如果未設置（≤0），子類會在初始化時使用默認值")]
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
        // 子類別可以覆寫此方法來實現具體更新邏輯
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
    /// 
    /// 【重要】建議在 Inspector 中直接設置 baseSpeed，而不是依賴此方法
    /// 子類別可以覆寫此方法來設置特定類型的默認值（僅作為後備方案）
    /// </summary>
    protected virtual void InitializeBaseValues()
    {
        // 如果基礎速度未在 Inspector 中設定（為 0 或負數），嘗試從組件讀取
        // 注意：這只是後備方案，建議在 Inspector 中直接設置
        if (baseSpeed <= 0f && movement != null)
        {
            baseSpeed = movement.GetSpeed();
        }

        // 基礎數值一旦設定就不應該再改變
        // 子類別可以覆寫此方法來設置特定類型的默認值（如 baseViewRange, baseViewAngle）
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
    /// 造成傷害（統一處理從受傷到死亡的完整流程）
    /// </summary>
    /// <param name="damage">傷害值</param>
    /// <param name="source">傷害來源</param>
    public virtual void TakeDamage(int damage, string source = "")
    {
        if (entityHealth == null) return;
        
        // 注意：傷害減少直接從 EntityHealth 獲取
        
        // 使用 EntityHealth 處理傷害
        // 如果生命值歸零，EntityHealth 會觸發 OnEntityDied 事件
        // BaseEntity 已訂閱此事件，會自動調用 HandleEntityDeath() -> Die()
        // 子類可以覆寫 GetEntityDisplayName() 來自定義實體名稱（用於日誌）
        entityHealth.TakeDamage(damage, source, GetEntityDisplayName());
    }
    
    /// <summary>
    /// 獲取實體顯示名稱（用於日誌等）
    /// 子類可以覆寫此方法來自定義名稱
    /// </summary>
    protected virtual string GetEntityDisplayName()
    {
        return gameObject.name;
    }

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
    /// 實體死亡（統一處理通用邏輯和死亡邏輯）
    /// </summary>
    public virtual void Die()
    {
        if (isHandleDie) return;
        isHandleDie = true;

        // 通用操作
        movement?.StopMovement();
        visualizer?.CleanupCreatedObjects();
        
        // 死亡處理（狀態改變、禁用 GameObject、觸發事件等）
        OnDeath();
        
        // 掉落物品
        DropAllItems();
    }

    /// <summary>
    /// 死亡處理（由子類實現：改變狀態、禁用 GameObject、觸發事件）
    /// </summary>
    protected virtual void OnDeath()
    {
        // 子類需要覆寫此方法來實現：
        // 1. 改變狀態為 Dead
        // 2. 禁用 GameObject（或延遲禁用）
        // 3. 觸發實體特定事件
    }
    
    /// <summary>
    /// 掉落所有物品（死亡時調用）
    /// </summary>
    protected virtual void DropAllItems()
    {
        if (itemHolder == null || itemHolder.ItemCount == 0) return;
        
        ItemManager itemManager = FindFirstObjectByType<ItemManager>();
        if (itemManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot drop items - ItemManager not found in scene!");
            return;
        }
        
        var itemsWithPrefabs = itemHolder.GetAllItemsWithPrefabs();
        if (itemsWithPrefabs.Count == 0) return;
        
        // 提取 Prefab 列表
        List<GameObject> prefabs = itemsWithPrefabs
            .Where(pair => pair.Value != null)
            .Select(pair => pair.Value)
            .ToList();
        
        if (prefabs.Count > 0)
        {
            itemManager.DropItemsAtPosition(prefabs, transform.position, 1.5f);
            itemHolder.ClearAllItems();
        }
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
    
    /// <summary>
    /// 治療（統一提供，避免子類重複定義）
    /// </summary>
    public virtual void Heal(int healAmount)
    {
        entityHealth?.Heal(healAmount);
    }
    
    /// <summary>
    /// 完全治療（統一提供，避免子類重複定義）
    /// </summary>
    public virtual void FullHeal()
    {
        entityHealth?.FullHeal();
    }
    
    /// <summary>
    /// 設定血量（統一提供，避免子類重複定義）
    /// </summary>
    public virtual void SetHealth(int health)
    {
        if (entityHealth == null) return;
        entityHealth.SetHealth(health);
        if (IsDead) Die();
    }
    
    /// <summary>
    /// 增加最大血量（統一提供，避免子類重複定義）
    /// </summary>
    public virtual void IncreaseMaxHealth(int amount)
    {
        entityHealth?.IncreaseMaxHealth(amount);
    }
    
    /// <summary>
    /// 獲取傷害減少值（統一提供，避免子類重複定義）
    /// </summary>
    public virtual float GetDamageReduction()
    {
        // 注意：傷害減少現在直接從 EntityHealth 獲取，不再從 EntityStats 獲取
        return entityHealth?.DamageReduction ?? 0f;
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

