using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 玩家偵測系統（繼承基礎偵測組件）
/// - 判斷哪些實體（Enemy 和 Target）在玩家視野中
/// - 直接更新實體的可見性狀態
/// - 不 deactivate 實體，只控制渲染組件
/// 
/// 【封裝說明】
/// 此類的偵測參數（如 viewRange, viewAngle）應通過 Player 類的公共方法進行修改，而不是直接訪問。
/// 注意：Player 的視野範圍通常由 Player 類統一管理。
/// </summary>
public class PlayerDetection : BaseDetection
{
    [Header("偵測參數")]
    // 視野參數現在從 Player（原 PlayerController）獲取
    // 圖層遮罩已移至 BaseDetection
    
    [Header("性能優化")]
    [SerializeField] private float updateInterval = 0.1f; // 更新間隔
    [SerializeField] private int entitiesPerFrameCheck = 5; // 每幀檢查的實體數量
    
    [Header("除錯")]
    [SerializeField] private Color visibleEntityColor = Color.green;
    [SerializeField] private Color hiddenEntityColor = Color.red;
    
    // 組件引用
    private Player player;
    private EntityManager entityManager;
    
    // 實體可見性管理（統一處理 Enemy 和 Target）
    private HashSet<IEntity> visibleEntities = new HashSet<IEntity>();
    private HashSet<IEntity> hiddenEntities = new HashSet<IEntity>();
    
    // 追蹤每個實體的前一個可見性狀態（用於只在狀態改變時觸發更新）
    private Dictionary<IEntity, bool> previousVisibilityStates = new Dictionary<IEntity, bool>();
    
    // 性能優化
    private float lastUpdateTime = 0f;
    private int currentCheckIndex = 0;
    private List<IEntity> allEntitiesList = new List<IEntity>();
    
    // 玩家方向
    private Vector2 playerDirection = Vector2.right;
    
    public float ViewRange => GetViewRange();
    public float ViewAngle => GetViewAngle();
    public int VisibleEntityCount => visibleEntities.Count;
    public int HiddenEntityCount => hiddenEntities.Count;

    protected override void Awake()
    {
        base.Awake(); // 調用基類 Awake
        player = GetComponent<Player>();
        entityManager = FindFirstObjectByType<EntityManager>();
    }
    
    private void Start()
    {
        if (entityManager == null)
        {
            Debug.LogError("[PlayerDetection] 找不到 EntityManager！");
            enabled = false;
            return;
        }
        
        // 初始化所有實體為不可見狀態（延遲執行，確保實體已生成）
        StartCoroutine(DelayedInitializeAllEntitiesAsHidden());
    }
    
    /// <summary>
    /// 延遲初始化所有實體為不可見狀態（確保實體已生成）
    /// </summary>
    private System.Collections.IEnumerator DelayedInitializeAllEntitiesAsHidden()
    {
        // 等待一幀，確保 EntityManager 已完成實體生成
        yield return null;
        yield return null; // 再等待一幀，確保所有實體都已初始化
        
        InitializeAllEntitiesAsHidden();
    }
    
    /// <summary>
    /// 初始化所有實體為不可見狀態
    /// </summary>
    private void InitializeAllEntitiesAsHidden()
    {
        if (entityManager == null) return;
        
        // 獲取所有實體並初始化為不可見
        var enemies = entityManager.GetAllActiveEnemies();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy is IEntity enemyEntity && enemyEntity.GetEntityType() != EntityManager.EntityType.Player)
            {
                // 初始化為不可見狀態
                previousVisibilityStates[enemyEntity] = false;
                hiddenEntities.Add(enemyEntity);
                visibleEntities.Remove(enemyEntity);
            }
        }
        
        var targets = entityManager.GetAllActiveTargets();
        foreach (var target in targets)
        {
            if (target != null && target is IEntity targetEntity && targetEntity.GetEntityType() != EntityManager.EntityType.Player)
            {
                // 初始化為不可見狀態
                previousVisibilityStates[targetEntity] = false;
                hiddenEntities.Add(targetEntity);
                visibleEntities.Remove(targetEntity);
            }
        }
    }
    
    /// <summary>
    /// 覆寫基類方法，根據玩家蹲下狀態決定遮罩
    /// 當玩家蹲下時：walls + objects 都會遮擋視線
    /// 當玩家站立時：只有 walls 會遮擋視線
    /// </summary>
    protected override LayerMask GetObstacleLayerMask()
    {
        if (player != null && player.IsSquatting)
        {
            // 玩家蹲下時，walls 和 objects 都會遮擋視線
            return wallsLayerMask | objectsLayerMask;
        }
        else
        {
            // 玩家站立時，只有 walls 會遮擋視線（可以透過 objects 看到）
            return wallsLayerMask;
        }
    }
    
    private void Update()
    {
        // 更新玩家方向
        if (player != null)
        {
            playerDirection = player.GetWeaponDirection();
        }
        
        // 定期更新實體可見性
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateEntityVisibility();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// 更新實體可見性（分批處理以提升性能，統一處理 Enemy 和 Target）
    /// </summary>
    private void UpdateEntityVisibility()
    {
        if (entityManager == null) return;
        
        // 獲取所有實體列表（Enemy + Target）
        allEntitiesList.Clear();
        
        // 添加所有 Enemy
        var enemies = entityManager.GetAllActiveEnemies();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy is IEntity enemyEntity)
            {
                allEntitiesList.Add(enemyEntity);
                
                // 確保所有實體都已經初始化為不可見（如果尚未記錄）
                if (!previousVisibilityStates.ContainsKey(enemyEntity) && enemyEntity.GetEntityType() != EntityManager.EntityType.Player)
                {
                    // 初始化為不可見狀態
                    previousVisibilityStates[enemyEntity] = false;
                    hiddenEntities.Add(enemyEntity);
                    visibleEntities.Remove(enemyEntity);
                }
            }
        }
        
        // 添加所有 Target
        var targets = entityManager.GetAllActiveTargets();
        foreach (var target in targets)
        {
            if (target != null && target is IEntity targetEntity)
            {
                allEntitiesList.Add(targetEntity);
                
                // 確保所有實體都已經初始化為不可見（如果尚未記錄）
                if (!previousVisibilityStates.ContainsKey(targetEntity) && targetEntity.GetEntityType() != EntityManager.EntityType.Player)
                {
                    // 初始化為不可見狀態
                    previousVisibilityStates[targetEntity] = false;
                    hiddenEntities.Add(targetEntity);
                    visibleEntities.Remove(targetEntity);
                }
            }
        }
        
        if (allEntitiesList.Count == 0) return;
        
        // 分批檢查實體可見性
        int entitiesToCheck = Mathf.Min(entitiesPerFrameCheck, allEntitiesList.Count);
        
        for (int i = 0; i < entitiesToCheck; i++)
        {
            if (currentCheckIndex >= allEntitiesList.Count)
                currentCheckIndex = 0;
            
            if (currentCheckIndex < allEntitiesList.Count)
            {
                IEntity entity = allEntitiesList[currentCheckIndex];
                if (entity != null && !entity.IsDead)
                {
                    // 排除 Player，只檢測 Enemy 和 Target
                    EntityManager.EntityType entityType = entity.GetEntityType();
                    if (entityType != EntityManager.EntityType.Player)
                    {
                        CheckEntityVisibility(entity);
                    }
                }
                currentCheckIndex++;
            }
        }
    }
    
    /// <summary>
    /// 檢查單個實體的可見性（統一處理 Enemy 和 Target，直接更新可見性狀態）
    /// </summary>
    private void CheckEntityVisibility(IEntity entity)
    {
        if (entity == null) return;
        
        bool isVisible = IsEntityInPlayerView(entity);
        
        // 檢查前一個狀態是否存在且是否改變
        bool stateChanged = false;
        
        if (previousVisibilityStates.ContainsKey(entity))
        {
            bool previousState = previousVisibilityStates[entity];
            if (previousState != isVisible)
            {
                stateChanged = true;
            }
        }
        else
        {
            // 首次檢查，視為狀態改變（需要初始化）
            stateChanged = true;
        }
        
        // 更新狀態記錄
        previousVisibilityStates[entity] = isVisible;
        
        if (isVisible)
        {
            // 實體可見
            if (hiddenEntities.Contains(entity))
            {
                hiddenEntities.Remove(entity);
            }
            if (!visibleEntities.Contains(entity))
            {
                visibleEntities.Add(entity);
            }
        }
        else
        {
            // 實體不可見
            if (visibleEntities.Contains(entity))
            {
                visibleEntities.Remove(entity);
            }
            if (!hiddenEntities.Contains(entity))
            {
                hiddenEntities.Add(entity);
            }
        }
        // 只有當狀態改變時，才通過 EntityManager 更新實體的可見性
        if (stateChanged)
        {
            UpdateEntityVisualization(entity, isVisible);
        }
    }
    
    /// <summary>
    /// 更新實體的可見性狀態（委託給 EntityManager 統一處理）
    /// </summary>
    private void UpdateEntityVisualization(IEntity entity, bool isVisible)
    {
        if (entity == null || entityManager == null) return;
        
        // 排除 Player
        if (entity.GetEntityType() == EntityManager.EntityType.Player)
        {
            return;
        }
        
        // 委託給 EntityManager 統一處理
        entityManager.SetEntityVisualization(entity, isVisible);
    }
    
    /// <summary>
    /// 檢查實體是否在玩家視野中（統一處理 Enemy 和 Target）
    /// </summary>
    private bool IsEntityInPlayerView(IEntity entity)
    {
        if (entity == null) return false;
        
        Vector2 playerPos = transform.position;
        Vector2 entityPos = entity.Position;
        Vector2 dirToEntity = entityPos - playerPos;
        float distanceToEntity = dirToEntity.magnitude;
        
        // 距離檢查
        if (distanceToEntity > GetViewRange())
            return false;
        
        // 角度檢查
        if (GetViewAngle() < 360f)
        {
            float angleToEntity = Vector2.Angle(playerDirection, dirToEntity);
            if (angleToEntity > GetViewAngle() * 0.5f)
                return false;
        }
        
        // 遮擋檢查 - 使用 BaseDetection 提供的方法
        if (IsBlockedByObstacle(playerPos, entityPos))
            return false;
        
        return true;
    }
    
    /// <summary>
    /// 獲取視野參數（從 Player 獲取）
    /// </summary>
    private float GetViewRange()
    {
        return player != null ? player.ViewRange : 8f;
    }
    
    private float GetViewAngle()
    {
        return player != null ? player.ViewAngle : 90f;
    }
    
    /// <summary>
    /// 檢查是否可以看到目標（覆寫基類抽象方法）
    /// </summary>
    public override bool CanSeeTarget(Vector2 targetPos)
    {
        Vector2 playerPos = transform.position;
        Vector2 dirToTarget = targetPos - playerPos;
        float distanceToTarget = dirToTarget.magnitude;
        
        // 距離檢查
        if (distanceToTarget > GetViewRange())
            return false;
        
        // 角度檢查（如果視野角度小於 360 度）
        if (GetViewAngle() < 360f)
        {
            float angleToTarget = Vector2.Angle(playerDirection, dirToTarget);
            if (angleToTarget > GetViewAngle() * 0.5f)
                return false;
        }
        
        // 遮擋檢查 - 使用 BaseDetection 提供的方法
        if (IsBlockedByObstacle(playerPos, targetPos))
            return false;
        
        return true;
    }
    
    /// <summary>
    /// 設定障礙物層遮罩（向後兼容，已移至 BaseDetection）
    /// </summary>
    public void SetObstacleLayerMask(LayerMask layerMask)
    {
        // 假設傳入的是 walls + objects 的組合遮罩
        // 需要分離成 walls 和 objects（此方法保留用於向後兼容）
        Debug.LogWarning("[PlayerDetection] SetObstacleLayerMask 已棄用，請使用 SetLayerMasks(walls, objects)");
    }
    
    /// <summary>
    /// 強制更新所有實體可見性（Enemy 和 Target，排除 Player）
    /// </summary>
    public void ForceUpdateAllEntities()
    {
        if (entityManager == null) return;
        
        // 更新所有 Enemy
        var allEnemies = entityManager.GetAllActiveEnemies();
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy is IEntity enemyEntity)
            {
                // 確保不是 Player
                if (enemyEntity.GetEntityType() != EntityManager.EntityType.Player)
                {
                    CheckEntityVisibility(enemyEntity);
                }
            }
        }
        
        // 更新所有 Target
        var allTargets = entityManager.GetAllActiveTargets();
        foreach (var target in allTargets)
        {
            if (target != null && target is IEntity targetEntity)
            {
                // 確保不是 Player
                if (targetEntity.GetEntityType() != EntityManager.EntityType.Player)
                {
                    CheckEntityVisibility(targetEntity);
                }
            }
        }
        
        Debug.Log($"[PlayerDetection] 強制更新完成 - 可見: {visibleEntities.Count}, 隱藏: {hiddenEntities.Count}");
    }
    
    /// <summary>
    /// 新增實體到檢測系統（初始化為不可見狀態）
    /// </summary>
    public void AddEntity(IEntity entity)
    {
        if (entity == null || entityManager == null) return;
        
        // 排除 Player
        if (entity.GetEntityType() == EntityManager.EntityType.Player)
        {
            return;
        }
        
        // 初始化為不可見狀態
        previousVisibilityStates[entity] = false;
        hiddenEntities.Add(entity);
        visibleEntities.Remove(entity);
        
        // 檢查當前可見性（可能在調用時已經可見）
        CheckEntityVisibility(entity);
    }
    
    /// <summary>
    /// 從檢測系統移除實體
    /// </summary>
    public void RemoveEntity(IEntity entity)
    {
        if (entity == null) return;
        
        visibleEntities.Remove(entity);
        hiddenEntities.Remove(entity);
        previousVisibilityStates.Remove(entity);
    }
    
    /// <summary>
    /// 獲取可見實體列表
    /// </summary>
    public List<IEntity> GetVisibleEntities()
    {
        return new List<IEntity>(visibleEntities);
    }
    
    /// <summary>
    /// 獲取隱藏實體列表
    /// </summary>
    public List<IEntity> GetHiddenEntities()
    {
        return new List<IEntity>(hiddenEntities);
    }
    
    /// <summary>
    /// 檢查特定實體是否可見
    /// </summary>
    public bool IsEntityVisible(IEntity entity)
    {
        return entity != null && visibleEntities.Contains(entity);
    }
    
    private void OnDestroy()
    {
        // 清理資源
        visibleEntities.Clear();
        hiddenEntities.Clear();
        previousVisibilityStates.Clear();
    }
}
