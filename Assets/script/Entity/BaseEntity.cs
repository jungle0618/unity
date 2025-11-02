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

        // 驗證必要組件
        ValidateComponents();
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
    /// 實體死亡
    /// </summary>
    public virtual void Die()
    {
        if (IsDead) return;

        // 停止移動
        movement?.StopMovement();

        // 通知子類別處理死亡邏輯
        OnDeath();
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
        Debug.LogError($"{gameObject.name}: Dropping all items4");
        if (itemHolder == null || itemHolder.ItemCount == 0)
        {
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
        // 子類別可以覆寫此方法來清理資源
    }

    #endregion
}

