using UnityEngine;

/// <summary>
/// 勝利條件管理器
/// 勝利條件：目標死亡且玩家到達出口點
/// 失敗條件：玩家死亡或目標到達出口點
/// 
/// 注意：出口點位置從 patroldata.json 中的 Exit 實體讀取，由 EntityManager 初始化時傳遞
/// </summary>
public class WinConditionManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float exitReachDistance = 3f; // 到達出口的距離閾值
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Visual")]
    [SerializeField] private bool showExitGizmo = true;
    [SerializeField] private Color exitGizmoColor = Color.cyan;
    
    // 出口點位置（從 patroldata.json 讀取，由 EntityManager 通過 Initialize 方法設置）
    private Vector3 exitPoint = Vector3.zero;
    
    // 狀態追蹤
    private bool targetKilled = false;
    private bool playerReachedExit = false;
    private bool playerDied = false;
    private bool targetReachedExit = false;
    private bool winConditionChecked = false;
    private bool exitNotificationSent = false; // 是否已發送過出口點通知
    
    // 系統引用
    private GameManager gameManager;
    private Player player;
    
    // 目標引用（只有一個 target）
    private Target target;
    
    /// <summary>
    /// 初始化（出口點位置從 patroldata.json 讀取，由 EntityManager 調用）
    /// </summary>
    /// <param name="exitPosition">出口點位置（從 patroldata.json 中的 Exit 實體讀取）</param>
    public void Initialize(Vector3 exitPosition)
    {
        exitPoint = exitPosition;
        
        // 獲取系統引用
        GetPlayer();
        
        // 訂閱事件
        TrySubscribeToTarget();
        TrySubscribeToPlayerEvents();
        
        if (showDebugLogs)
        {
            Debug.Log($"[WinCondition] 初始化完成，出口點: {exitPoint}");
        }
    }
    
    /// <summary>
    /// 獲取 GameManager
    /// </summary>
    private GameManager GetGameManager()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        }
        return gameManager;
    }
    
    /// <summary>
    /// 獲取 Player
    /// </summary>
    private Player GetPlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }
        return player;
    }
    
    /// <summary>
    /// 嘗試訂閱玩家事件
    /// </summary>
    private void TrySubscribeToPlayerEvents()
    {
        Player currentPlayer = GetPlayer();
        if (currentPlayer == null) return;
        
        // 先取消訂閱（避免重複訂閱）
        currentPlayer.OnPlayerDied -= OnPlayerDied;
        
        // 再訂閱
        currentPlayer.OnPlayerDied += OnPlayerDied;
        
        if (showDebugLogs)
        {
            Debug.Log("[WinCondition] 已訂閱玩家事件");
        }
    }
    
    private void Start()
    {
        GetPlayer();
        
        if (target == null)
        {
            TrySubscribeToTarget();
        }
        
        TrySubscribeToPlayerEvents();
    }
    
    private void OnDestroy()
    {
        // 取消訂閱 Target 事件
        if (target != null)
        {
            target.OnTargetDied -= OnTargetKilled;
            target.OnTargetReachedEscapePoint -= OnTargetReachedExit;
        }
        
        // 取消訂閱 Player 事件
        if (player != null)
        {
            player.OnPlayerDied -= OnPlayerDied;
        }
    }
    
    private void Update()
    {
        if (winConditionChecked) return;
        
        // 確保有 GameManager 引用
        if (gameManager == null)
        {
            GetGameManager();
        }
        
        // 持續嘗試訂閱目標和玩家事件
        if (target == null)
        {
            TrySubscribeToTarget();
        }
        
        if (player == null)
        {
            TrySubscribeToPlayerEvents();
        }
        
        // 檢查玩家是否到達出口
        CheckPlayerReachedExit();
        
        // 檢查目標是否到達出口
        CheckTargetReachedExit();
        
        // 檢查勝利條件
        CheckWinCondition();
    }
    
    /// <summary>
    /// 嘗試訂閱目標事件（只有一個 target）
    /// </summary>
    private void TrySubscribeToTarget()
    {
        if (target != null) return;
        
        Target foundTarget = FindFirstObjectByType<Target>();
        if (foundTarget != null)
        {
            target = foundTarget;
            target.OnTargetDied += OnTargetKilled;
            target.OnTargetReachedEscapePoint += OnTargetReachedExit;
            
            if (showDebugLogs)
            {
                Debug.Log($"[WinCondition] 已訂閱目標事件: {target.name}");
            }
        }
    }
    
    /// <summary>
    /// 檢查玩家是否到達出口
    /// </summary>
    private void CheckPlayerReachedExit()
    {
        if (!targetKilled || playerReachedExit || exitPoint == Vector3.zero) return;
        
        Player currentPlayer = GetPlayer();
        if (currentPlayer == null) return;
        
        float distance = Vector3.Distance(currentPlayer.transform.position, exitPoint);
        
        if (distance <= exitReachDistance)
        {
            playerReachedExit = true;
            
            if (showDebugLogs)
            {
                Debug.LogWarning($"[WinCondition] ✓ 玩家已到達出口！距離: {distance:F2}");
            }
            
            CheckWinCondition();
        }
    }
    
    /// <summary>
    /// 檢查目標是否到達出口（檢查目標的 escapePoint，不是玩家的 exitPoint）
    /// </summary>
    private void CheckTargetReachedExit()
    {
        if (targetReachedExit || target == null || target.IsDead) return;
        
        // 獲取目標的 escapePoint（不是玩家的 exitPoint）
        TargetAIHandler aiHandler = target.GetComponent<TargetAIHandler>();
        if (aiHandler == null) return;
        
        Vector3 targetEscapePoint = aiHandler.GetEscapePoint();
        if (targetEscapePoint == Vector3.zero) return;
        
        float distance = Vector3.Distance(target.transform.position, targetEscapePoint);
        
        if (showDebugLogs)
        {
            Debug.Log($"[WinCondition] CheckTargetReachedExit: {target.name}");
            Debug.Log($"[WinCondition] Target Escape Point: {targetEscapePoint}");
            Debug.Log($"[WinCondition] Target Position: {target.transform.position}");
            Debug.Log($"[WinCondition] Distance to escape point: {distance}");
        }
        
        if (distance <= exitReachDistance)
        {
            targetReachedExit = true;
            
            if (showDebugLogs)
            {
                Debug.LogWarning($"[WinCondition] ✗ 目標已到達逃亡點: {target.name}");
            }
            
            // 觸發失敗
            TriggerFailure("Target reached escape point");
        }
    }
    
    /// <summary>
    /// 玩家死亡事件處理
    /// </summary>
    private void OnPlayerDied()
    {
        if (playerDied) return;
        
        playerDied = true;
        
        if (showDebugLogs)
        {
            Debug.LogWarning("[WinCondition] ✗ 玩家已死亡！任務失敗！");
        }
        
        TriggerFailure("Player died");
    }
    
    /// <summary>
    /// 目標到達逃亡點事件處理（當目標到達其 escapePoint 時觸發）
    /// </summary>
    private void OnTargetReachedExit(Target target)
    {
        if (targetReachedExit) return;
        
        // 目標已經到達其 escapePoint（不是玩家的 exitPoint），直接觸發失敗
        targetReachedExit = true;
        
        if (showDebugLogs)
        {
            TargetAIHandler aiHandler = target.GetComponent<TargetAIHandler>();
            Vector3 escapePoint = aiHandler != null ? aiHandler.GetEscapePoint() : Vector3.zero;
            Debug.LogWarning($"[WinCondition] ✗ 目標已到達逃亡點: {target.name} (escapePoint: {escapePoint})");
        }
        
        TriggerFailure("Target reached escape point");
    }
    
    /// <summary>
    /// 目標被殺死事件處理
    /// </summary>
    private void OnTargetKilled(Target target)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"[WinCondition] ✓ 目標已被殺死: {target.name}");
        }
        
        // 目標已死亡
        targetKilled = true;
        
        // 當目標死亡且還沒發送過通知時，顯示通知
        if (!exitNotificationSent)
        {
            ShowExitPointNotification();
            exitNotificationSent = true;
        }
        
        // 檢查勝利條件
        CheckWinCondition();
    }
    
    /// <summary>
    /// 顯示出口點通知
    /// </summary>
    private void ShowExitPointNotification()
    {
        NotificationUIManager notificationUI = FindFirstObjectByType<NotificationUIManager>();
        if (notificationUI != null)
        {
            notificationUI.ShowNotification("Target eliminated! Head to the exit point to complete the mission!", 5f);
            
            if (showDebugLogs)
            {
                Debug.LogWarning("[WinCondition] 已發送出口點通知");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[WinCondition] 無法找到 NotificationUIManager，無法顯示通知");
            }
        }
    }
    
    /// <summary>
    /// 檢查勝利條件
    /// </summary>
    private void CheckWinCondition()
    {
        if (winConditionChecked) return;
        
        // 檢查失敗條件（優先級最高）
        if (playerDied)
        {
            // 玩家死亡已在 OnPlayerDied 中處理，這裡不需要再次觸發
            return;
        }
        
        if (targetReachedExit)
        {
            // 目標到達逃亡點，確保觸發失敗（如果之前沒有成功觸發）
            TriggerFailure("Target reached escape point");
            return;
        }
        
        Debug.Log("[WinCondition] Target killed: " + targetKilled);
        Debug.Log("[WinCondition] Player reached exit: " + playerReachedExit);
        Debug.Log("[WinCondition] Target reached exit: " + targetReachedExit);
        
        // 勝利條件：目標死亡且玩家到達出口
        if (targetKilled && playerReachedExit)
        {
            TriggerWin();
        }
    }
    
    /// <summary>
    /// 觸發勝利
    /// </summary>
    private void TriggerWin()
    {
        if (winConditionChecked) return;
        
        winConditionChecked = true;
        
        if (showDebugLogs)
        {
            Debug.LogWarning("[WinCondition] ========================================");
            Debug.LogWarning("[WinCondition] 🎉 任務成功！玩家獲勝！");
            Debug.LogWarning("[WinCondition] ========================================");
        }
        
        GameManager gm = GetGameManager();
        if (gm != null)
        {
            gm.TriggerGameWin();
        }
        else
        {
            Debug.LogError("[WinCondition] 無法觸發勝利：GameManager 未找到！");
        }
    }
    
    /// <summary>
    /// 觸發失敗
    /// </summary>
    private void TriggerFailure(string reason)
    {
        Debug.LogError("[WinCondition] TriggerFailure: " + reason);
        if (winConditionChecked) return;
        
        winConditionChecked = true;
        
        if (showDebugLogs)
        {
            Debug.LogWarning($"[WinCondition] ✗ 任務失敗：{reason}");
        }
        
        GameManager gm = GetGameManager();
        if (gm != null)
        {
            gm.GameOver(reason);
        }
        else
        {
            Debug.LogError("[WinCondition] 無法觸發失敗：GameManager 未找到！");
        }
    }
    
    /// <summary>
    /// 獲取當前狀態（用於 UI 顯示）
    /// </summary>
    public string GetStatusText()
    {
        if (playerDied)
        {
            return "任務失敗：玩家已死亡";
        }
        
        if (targetReachedExit)
        {
            return "任務失敗：目標已到達逃亡點";
        }
        
        string status = "勝利條件：\n";
        status += targetKilled ? "✓ 殺死目標\n" : "○ 殺死目標\n";
        status += playerReachedExit ? "✓ 到達出口\n" : "○ 到達出口\n";
        
        return status;
    }
    
    /// <summary>
    /// 繪製出口點 Gizmo
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showExitGizmo || exitPoint == Vector3.zero) return;
        
        Gizmos.color = exitGizmoColor;
        
        // 繪製圓圈表示出口區域
        Gizmos.DrawWireSphere(exitPoint, exitReachDistance);
        
        // 繪製標記
        Gizmos.DrawLine(exitPoint + Vector3.up * 2f, exitPoint + Vector3.up * 3f);
        
        // 繪製方向指示
        Vector3 left = exitPoint + Vector3.left * 0.5f + Vector3.up * 2.5f;
        Vector3 right = exitPoint + Vector3.right * 0.5f + Vector3.up * 2.5f;
        Vector3 top = exitPoint + Vector3.up * 3f;
        
        Gizmos.DrawLine(left, top);
        Gizmos.DrawLine(right, top);
        
#if UNITY_EDITOR
        // 顯示文字標籤
        UnityEditor.Handles.color = exitGizmoColor;
        UnityEditor.Handles.Label(exitPoint + Vector3.up * 3.5f, "出口 (Exit)");
#endif
    }

    /// <summary>
    /// 取得出口點位置（供其他系統使用）
    /// </summary>
    public Vector3 GetExitPoint()
    {
        return exitPoint;
    }
}
