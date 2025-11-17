using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Target AI 邏輯處理組件
/// 負責處理所有 AI 狀態邏輯和移動決策
/// </summary>
[RequireComponent(typeof(Target))]
public class TargetAIHandler : MonoBehaviour
{
    private Target target;
    private TargetStateMachine targetStateMachine;
    private TargetMovement targetMovement;
    private TargetDetection targetDetection;
    
    // 逃亡相關變數
    private Vector3 currentEscapeTarget;
    private bool hasReachedGuard = false; // 是否已到達守衛位置（僅在 safe 等級時使用）
    private bool hasReachedEscapePoint = false; // 是否已到達逃亡點
    private bool hasEverEnteredEscapeMode = false; // 是否曾經進入過逃亡模式（一旦進入就不會回到 Stay）
    
    // 系統引用
    private DangerousManager dangerousManager;
    private EntityManager entityManager;
    
    // 逃亡設定
    private Vector3 escapePoint = Vector3.zero;
    private float escapeSpeed = 3f;
    
    [Header("逃亡行為設定")]
    [Tooltip("是否無條件直接前往逃亡點（不經過守衛）。如果為 true，則忽略危險等級，直接前往逃亡點")]
    [SerializeField] private bool alwaysGoDirectlyToEscapePoint = true;
    
    [Header("調試設定")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 事件
    public System.Action<Target> OnTargetReachedEscapePoint;
    
    // 快取數據（從 Target 獲取）
    private Vector2 cachedPosition;
    private Vector2 cachedDirectionToPlayer;
    private bool cachedCanSeePlayer;
    
    // 地圖可視化更新計時器
    private float lastMapVisualizationUpdate = 0f;
    private const float MAP_VISUALIZATION_UPDATE_INTERVAL = 1.0f; // 每秒更新一次
    
    private void Awake()
    {
        target = GetComponent<Target>();
        if (target == null)
        {
            Debug.LogError($"{gameObject.name}: TargetAIHandler requires Target component!");
            enabled = false;
            return;
        }
    }
    
    private void Start()
    {
        // 獲取組件引用
        targetStateMachine = target.StateMachine as TargetStateMachine;
        targetMovement = target.Movement as TargetMovement;
        targetDetection = target.Detection as TargetDetection;
        
        // 獲取系統引用
        dangerousManager = DangerousManager.Instance;
        entityManager = FindFirstObjectByType<EntityManager>();
    }
    
    /// <summary>
    /// 初始化 AI Handler（由 Target 調用）
    /// </summary>
    public void Initialize(Vector3 escapePointPosition, float escapeSpeed)
    {
        this.escapePoint = escapePointPosition;
        this.escapeSpeed = escapeSpeed;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: TargetAIHandler 初始化 - 逃亡點: {escapePoint}, 速度: {escapeSpeed}");
        }
    }
    
    /// <summary>
    /// 設定逃亡點
    /// </summary>
    public void SetEscapePoint(Vector3 escapePointPosition)
    {
        this.escapePoint = escapePointPosition;
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: 逃亡點設定為 {escapePointPosition}");
        }
    }
    
    /// <summary>
    /// 設定逃亡速度
    /// </summary>
    public void SetEscapeSpeed(float speed)
    {
        this.escapeSpeed = speed;
    }
    
    /// <summary>
    /// 獲取逃亡點位置（用於調試和外部訪問）
    /// </summary>
    public Vector3 GetEscapePoint()
    {
        return escapePoint;
    }
    
    /// <summary>
    /// 獲取當前逃亡目標位置（用於可視化）
    /// </summary>
    public Vector3 GetCurrentEscapeTarget()
    {
        return currentEscapeTarget;
    }
    
    /// <summary>
    /// 檢查是否曾經進入逃亡模式
    /// </summary>
    public bool HasEverEnteredEscapeMode()
    {
        return hasEverEnteredEscapeMode;
    }
    
    /// <summary>
    /// 更新快取數據（由 Target 調用）
    /// </summary>
    public void UpdateCachedData(Vector2 position, Vector2 directionToPlayer, bool canSeePlayer)
    {
        cachedPosition = position;
        cachedDirectionToPlayer = directionToPlayer;
        cachedCanSeePlayer = canSeePlayer;
    }
    
    /// <summary>
    /// AI 決策更新（使用間隔，減少 CPU 負載）
    /// 根據 target_ai.md：當不在 Escape 狀態且不在攝影機範圍內時，不執行 AI 更新
    /// </summary>
    public void UpdateAIDecision()
    {
        if (targetStateMachine == null || targetDetection == null || targetMovement == null) return;

        // 檢查是否應該更新 AI（考慮攝影機剔除）
        // 根據 target_ai.md：當不在 Escape 狀態且不在攝影機範圍內時，不執行 AI 更新
        if (!targetDetection.ShouldUpdateAI())
        {
            // 視野外且不在 Escape 狀態，跳過 AI 更新
            // 移動邏輯仍會在 ExecuteMovement() 中執行
            return;
        }

        // 根據當前狀態執行對應邏輯
        switch (targetStateMachine.CurrentState)
        {
            case TargetState.Stay:
                HandleStayState();
                break;

            case TargetState.Escape:
                HandleEscapeState();
                break;
        }
    }

    /// <summary>
    /// 執行移動（每幀更新，確保移動流暢）
    /// </summary>
    public void ExecuteMovement()
    {
        if (targetStateMachine == null || targetStateMachine.CurrentState != TargetState.Escape) return;
        if (targetMovement == null) return;

        // 逃亡狀態：向當前逃亡目標移動
        targetMovement.MoveTowardsEscape(currentEscapeTarget, escapeSpeed);
    }
    
    /// <summary>
    /// 開始逃亡
    /// </summary>
    public void StartEscape()
    {
        if (targetStateMachine == null) return;
        
        targetStateMachine.ChangeState(TargetState.Escape);
        hasReachedGuard = false;
        hasReachedEscapePoint = false;
        hasEverEnteredEscapeMode = true; // 標記已經進入逃亡模式
        
        // 根據設定決定逃亡路線
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: 開始逃亡 - 當前危險等級: {dangerousManager?.CurrentDangerLevelType}, 逃亡點: {escapePoint}, 無條件直接前往: {alwaysGoDirectlyToEscapePoint}");
        }
        
        // 檢查是否無條件直接前往逃亡點
        if (alwaysGoDirectlyToEscapePoint)
        {
            // 無條件直接前往逃亡點（不經過守衛）
            currentEscapeTarget = escapePoint;
            hasReachedGuard = true; // 標記為已到達守衛（跳過守衛階段）
            
            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name}: 開始逃亡（無條件模式）- 直接前往逃亡點 {escapePoint}");
            }
        }
        else
        {
            // 根據危險等級決定逃亡路線
            bool shouldGoViaGuard = dangerousManager != null && 
                                   dangerousManager.CurrentDangerLevelType == DangerousManager.DangerLevel.Safe;
            
            if (shouldGoViaGuard)
            {
                // Safe 等級：先到最近的守衛，再到逃亡點
                Vector3 nearestGuard = FindNearestGuard();
                if (nearestGuard != Vector3.zero)
                {
                    currentEscapeTarget = nearestGuard;
                    hasReachedGuard = false;
                    if (showDebugInfo)
                    {
                        Debug.Log($"{gameObject.name}: 開始逃亡（Safe等級）- 先前往守衛位置 {nearestGuard}，然後前往逃亡點 {escapePoint}");
                    }
                }
                else
                {
                    // 找不到守衛，直接去逃亡點
                    currentEscapeTarget = escapePoint;
                    hasReachedGuard = true;
                    if (showDebugInfo)
                    {
                        Debug.Log($"{gameObject.name}: 找不到守衛，直接前往逃亡點 {escapePoint}");
                    }
                }
            }
            else
            {
                // 非 Safe 等級：直接到逃亡點
                currentEscapeTarget = escapePoint;
                hasReachedGuard = true;
                if (showDebugInfo)
                {
                    Debug.Log($"{gameObject.name}: 開始逃亡（非Safe等級）- 直接前往逃亡點 {escapePoint}");
                }
            }
        }
        
        // 延遲顯示地圖標記，讓路徑先計算
        // 使用協程在下一幀更新地圖可視化
        StartCoroutine(UpdateMapVisualizationNextFrame());
    }
    
    /// <summary>
    /// 在下一幀更新地圖可視化（等待路徑計算完成）
    /// </summary>
    private System.Collections.IEnumerator UpdateMapVisualizationNextFrame()
    {
        // 等待一幀，讓 ExecuteMovement 有機會計算路徑
        yield return null;
        
        // 再等待幾幀確保路徑完全計算完成
        for (int i = 0; i < 3; i++)
        {
            yield return null;
        }
        
        // 現在更新地圖可視化
        UpdateMapVisualization();
    }
    
    /// <summary>
    /// 更新地圖可視化（顯示逃亡點和路徑）
    /// </summary>
    public void UpdateMapVisualization()
    {
        MapUIManager mapUI = FindFirstObjectByType<MapUIManager>();
        if (mapUI != null && escapePoint != Vector3.zero)
        {
            // 獲取當前的 A* 路徑
            List<PathfindingNode> currentPath = targetMovement?.GetCurrentPath();
            
            // 傳遞逃亡點、目標位置和路徑
            mapUI.ShowEscapePoint(escapePoint, transform.position, currentPath);
            
            if (currentPath != null && currentPath.Count > 0)
            {
                Debug.LogWarning($"[TargetAI] {gameObject.name} 地圖已更新：逃亡點標記 + A* 路徑（{currentPath.Count} 個節點）");
            }
            else
            {
                Debug.LogWarning($"[TargetAI] {gameObject.name} 地圖已更新：逃亡點標記（路徑尚未計算或使用直線）");
            }
        }
    }
    
    /// <summary>
    /// 處理停留狀態（不移動不轉動）
    /// </summary>
    private void HandleStayState()
    {
        // 如果曾經進入過逃亡模式，不應該回到 Stay 狀態，強制切換回 Escape
        if (hasEverEnteredEscapeMode)
        {
            targetStateMachine?.ChangeState(TargetState.Escape);
            return;
        }
        
        // 停止移動
        targetMovement?.StopMovement();
        
        // 檢查是否看到玩家
        if (cachedCanSeePlayer)
        {
            // 看到玩家，切換到逃亡狀態
            StartEscape();
        }
    }
    
    /// <summary>
    /// 處理逃亡狀態
    /// </summary>
    private void HandleEscapeState()
    {
        // 定期更新地圖可視化（每秒一次）
        if (Time.time - lastMapVisualizationUpdate > MAP_VISUALIZATION_UPDATE_INTERVAL)
        {
            UpdateMapVisualization();
            lastMapVisualizationUpdate = Time.time;
        }
        
        // 檢查是否到達逃亡點
        if (!hasReachedEscapePoint && Vector2.Distance(cachedPosition, escapePoint) < 1f)
        {
            // 到達逃亡點，停止移動
            hasReachedEscapePoint = true;
            targetMovement?.StopMovement();
            
            // 在地圖上隱藏逃亡點標記
            MapUIManager mapUI = FindFirstObjectByType<MapUIManager>();
            if (mapUI != null)
            {
                mapUI.HideEscapePoint();
            }
            
            // 顯示通知
            NotificationUIManager notification = FindFirstObjectByType<NotificationUIManager>();
            if (notification != null)
            {
                notification.ShowNotification("The target has escaped!", 10f);
            }
            
            Debug.LogWarning($"[目標逃脫] {gameObject.name} 已到達逃亡點 {escapePoint}！");
            
            // 觸發到達逃亡點事件
            if (target != null)
            {
                OnTargetReachedEscapePoint?.Invoke(target);
            }
            return;
        }
        
        // 檢查是否到達中間目標（守衛位置）
        if (!hasReachedGuard && dangerousManager != null && 
            dangerousManager.CurrentDangerLevelType == DangerousManager.DangerLevel.Safe)
        {
            // Safe 等級時，需要先到守衛位置
            if (Vector2.Distance(cachedPosition, currentEscapeTarget) < 1f)
            {
                hasReachedGuard = true;
                // 重新計算到逃亡點的路徑
                currentEscapeTarget = escapePoint;
                targetMovement?.ClearPath();
                Debug.Log($"{gameObject.name}: 已到達守衛位置，前往逃亡點");
            }
        }
    }
    
    /// <summary>
    /// 尋找最近的守衛位置
    /// </summary>
    private Vector3 FindNearestGuard()
    {
        if (entityManager == null) return Vector3.zero;
        
        // 獲取所有活躍的敵人（守衛）
        var allEnemies = entityManager.GetAllActiveEnemies();
        if (allEnemies == null || allEnemies.Count == 0) return Vector3.zero;
        
        Vector3 nearestGuard = Vector3.zero;
        float nearestDistance = float.MaxValue;
        
        foreach (var enemy in allEnemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0) continue;
            
            float distance = Vector2.Distance(cachedPosition, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestGuard = enemy.transform.position;
            }
        }
        
        return nearestGuard;
    }
}


