using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 勝利條件管理器
/// 負責追蹤並檢查遊戲勝利條件：
/// 1. 在目標逃脫前殺死目標
/// 2. 到達出口點且玩家存活
/// </summary>
public class WinConditionManager : MonoBehaviour
{
    [Header("Win Conditions")]
    [SerializeField] private bool requireTargetKilled = true; // 需要殺死目標
    [SerializeField] private bool requireReachExit = true;   // 需要到達出口
    
    [Header("Exit Point")]
    [SerializeField] private Vector3 exitPoint = Vector3.zero;
    [SerializeField] private float exitReachDistance = 1.5f; // 到達出口的距離閾值
    
    [Header("Visual")]
    [SerializeField] private bool showExitGizmo = true;
    [SerializeField] private Color exitGizmoColor = Color.cyan;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Win Condition Options")]
    [Tooltip("勝利條件：玩家到達出口點（true）或出生點（false）")]
    [SerializeField] private bool useExitPoint = true; // true = 出口點，false = 出生點
    
    // 狀態追蹤
    private bool targetKilled = false;
    private bool allTargetsKilled = false;
    private bool targetEscaped = false;
    private bool playerReachedExit = false;
    private bool playerReachedSpawnPoint = false;
    private bool playerDied = false;
    private bool winConditionChecked = false;
    
    // 系統引用
    private GameManager gameManager;
    private Player player;
    private EntityManager entityManager;
    
    // 目標引用
    private List<Target> targets = new List<Target>();
    
    // 出口點視覺標記
    private GameObject exitPointMarker;
    
    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize(Vector3 exitPosition)
    {
        exitPoint = exitPosition;
        
        // 獲取系統引用 - GameManager 和 Player 可能還沒初始化，延後獲取
        GetPlayer();
        GetEntityManager();
        
        // 訂閱事件
        TrySubscribeToTargets();
        TrySubscribeToPlayerEvents();
        
        if (showDebugLogs)
        {
            string winLocation = useExitPoint ? "出口點" : "出生點";
            Debug.Log($"[WinCondition] 初始化完成，{winLocation}: {exitPoint}（將在目標被殺死後顯示）");
        }
    }
    
    /// <summary>
    /// 在遊戲世界中創建出口點視覺標記
    /// </summary>
    private void CreateExitPointMarker()
    {
        if (exitPoint == Vector3.zero) return;
        
        // 創建標記 GameObject
        exitPointMarker = new GameObject("ExitPointMarker");
        exitPointMarker.transform.position = exitPoint;
        
        // 添加 SpriteRenderer 顯示圓形標記
        SpriteRenderer spriteRenderer = exitPointMarker.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite(64);
        spriteRenderer.color = new Color(0f, 0.7f, 1f, 0.6f); // 半透明藍色
        spriteRenderer.sortingOrder = 100; // 確保在其他物件上方
        
        // 設置大小 - 保持原始大小，視覺效果很好
        exitPointMarker.transform.localScale = Vector3.one * 2f;
        
        // 添加脈衝動畫
        ExitPointPulse pulse = exitPointMarker.AddComponent<ExitPointPulse>();
        
        if (showDebugLogs)
        {
            Debug.Log($"[WinCondition] 遊戲世界出口標記已創建於: {exitPoint}");
        }
    }
    
    /// <summary>
    /// 創建圓形精靈
    /// </summary>
    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];
        
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                // 創建漸變圓形
                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 在地圖UI上顯示出口點標記（藍色）
    /// </summary>
    private void ShowExitPointOnMap()
    {
        MapUIManager mapUI = FindFirstObjectByType<MapUIManager>();
        if (mapUI != null && exitPoint != Vector3.zero)
        {
            MapMarker exitMarker = mapUI.AddMarker(exitPoint, "Exit");
            if (exitMarker != null)
            {
                // 設定為藍色
                exitMarker.SetMarkerColor(new Color(0f, 0.5f, 1f, 1f)); // Cyan/Blue
                
                // 標記縮小以適應地圖
                RectTransform markerRect = exitMarker.GetComponent<RectTransform>();
                if (markerRect != null)
                {
                    markerRect.localScale = Vector3.one * 0.4f; // 與其他標記大小一致
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"[WinCondition] 出口點已顯示在地圖上: {exitPoint}");
                }
            }
        }
    }
    
    /// <summary>
    /// 獲取 GameManager（延遲獲取，避免初始化順序問題）
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
    /// 獲取 Player（延遲獲取，避免初始化順序問題）
    /// </summary>
    private Player GetPlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null && showDebugLogs)
            {
                Debug.LogWarning("[WinCondition] Player 尚未創建，將在後續獲取");
            }
        }
        return player;
    }
    
    /// <summary>
    /// 獲取 EntityManager（延遲獲取，避免初始化順序問題）
    /// </summary>
    private EntityManager GetEntityManager()
    {
        if (entityManager == null)
        {
            entityManager = FindFirstObjectByType<EntityManager>();
        }
        return entityManager;
    }
    
    /// <summary>
    /// 嘗試訂閱玩家事件（持續嘗試直到找到玩家）
    /// </summary>
    private void TrySubscribeToPlayerEvents()
    {
        Player currentPlayer = GetPlayer();
        if (currentPlayer == null) return;
        
        // 先取消訂閱（避免重複訂閱）
        currentPlayer.OnPlayerDied -= OnPlayerDied;
        currentPlayer.OnPlayerReachedSpawnPoint -= OnPlayerReachedSpawnPoint;
        
        // 再訂閱
        currentPlayer.OnPlayerDied += OnPlayerDied;
        currentPlayer.OnPlayerReachedSpawnPoint += OnPlayerReachedSpawnPoint;
        
        if (showDebugLogs)
        {
            Debug.Log("[WinCondition] 已訂閱玩家事件");
        }
    }
    
    private void Start()
    {
        // 嘗試獲取 Player 和 EntityManager
        GetPlayer();
        GetEntityManager();
        
        // 如果還沒有訂閱目標事件，再次嘗試
        if (targets.Count == 0)
        {
            TrySubscribeToTargets();
        }
        
        // 如果還沒有訂閱玩家事件，再次嘗試
        TrySubscribeToPlayerEvents();
    }
    
    private void OnDestroy()
    {
        // 取消訂閱 Target 事件
        foreach (var target in targets)
        {
            if (target != null)
            {
                target.OnTargetDied -= OnTargetKilled;
                target.OnTargetReachedEscapePoint -= OnTargetEscaped;
            }
        }
        
        // 取消訂閱 Player 事件
        if (player != null)
        {
            player.OnPlayerDied -= OnPlayerDied;
            player.OnPlayerReachedSpawnPoint -= OnPlayerReachedSpawnPoint;
        }
        
        // 清理出口點標記
        if (exitPointMarker != null)
        {
            Destroy(exitPointMarker);
        }
    }
    
    private void Update()
    {
        if (winConditionChecked) return;
        
        // Make sure we have GameManager reference
        if (gameManager == null)
        {
            GetGameManager();
        }
        
        // 如果還沒訂閱到任何目標，持續嘗試查找並訂閱
        if (targets.Count == 0)
        {
            TrySubscribeToTargets();
        }
        
        // 如果還沒訂閱玩家事件，持續嘗試
        if (player == null)
        {
            TrySubscribeToPlayerEvents();
        }
        
        // 獲取 Player 引用
        Player currentPlayer = GetPlayer();
        if (currentPlayer == null) return;
        
        // 注意：玩家死亡和到達出生點的檢查已通過事件處理（OnPlayerDied, OnPlayerReachedSpawnPoint）
        // 這裡只需要檢查出口點（如果使用出口點）
        if (useExitPoint)
        {
            CheckPlayerReachedExit();
        }
        
        // 檢查勝利條件
        CheckWinCondition();
    }
    
    /// <summary>
    /// 嘗試訂閱目標事件（持續嘗試直到找到目標）
    /// </summary>
    private void TrySubscribeToTargets()
    {
        Target[] allTargets = FindObjectsByType<Target>(FindObjectsSortMode.None);
        
        if (allTargets.Length == 0) return;
        
        int subscribedCount = 0;
        foreach (var target in allTargets)
        {
            if (target != null && !targets.Contains(target))
            {
                targets.Add(target);
                target.OnTargetDied += OnTargetKilled;
                target.OnTargetReachedEscapePoint += OnTargetEscaped;
                subscribedCount++;
            }
        }
        
        if (showDebugLogs && subscribedCount > 0)
        {
            Debug.LogWarning($"[WinCondition] 已訂閱 {subscribedCount} 個目標事件（總共 {targets.Count} 個）");
        }
    }
    
    /// <summary>
    /// 檢查玩家是否到達出口
    /// </summary>
    private void CheckPlayerReachedExit()
    {
        if (playerReachedExit) return;
        
        Player currentPlayer = GetPlayer();
        if (currentPlayer == null) return;
        
        float distance = Vector3.Distance(currentPlayer.transform.position, exitPoint);
        
        if (distance <= exitReachDistance)
        {
            playerReachedExit = true;
            
            if (showDebugLogs)
            {
                Debug.LogWarning($"[WinCondition] ✓ 玩家已到達出口！");
            }
            
            // 立即檢查勝利條件
            CheckWinCondition();
        }
    }
    
    // 注意：玩家到達出生點的檢查已由 Player 的 OnPlayerReachedSpawnPoint 事件處理
    // 不需要在 Update 中重複檢查，直接使用事件回調 OnPlayerReachedSpawnPoint()
    
    /// <summary>
    /// 玩家死亡事件處理
    /// </summary>
    private void OnPlayerDied()
    {
        if (playerDied) return;
        
        playerDied = true;
        winConditionChecked = true;
        
        if (showDebugLogs)
        {
            Debug.LogWarning("[WinCondition] ✗ 玩家已死亡！任務失敗！");
        }
        
        // 玩家死亡 = 任務失敗 → 觸發 GameOver
        GameManager gm = GetGameManager();
        if (gm != null)
        {
            gm.GameOver("Player died");
        }
        else
        {
            Debug.LogError("[WinCondition] 無法觸發失敗：GameManager 未找到！");
        }
    }
    
    /// <summary>
    /// 玩家到達出生點事件處理（如果使用出生點作為勝利條件）
    /// </summary>
    private void OnPlayerReachedSpawnPoint()
    {
        if (!useExitPoint)
        {
            playerReachedSpawnPoint = true;
            
            if (showDebugLogs)
            {
                Debug.LogWarning("[WinCondition] ✓ 玩家已到達出生點！");
            }
            
            // 立即檢查勝利條件
            CheckWinCondition();
        }
    }
    
    /// <summary>
    /// 目標被殺死事件處理
    /// </summary>
    private void OnTargetKilled(Target target)
    {
        targetKilled = true;
        
        // 檢查是否所有 Target 都已死亡
        CheckAllTargetsKilled();
        
        if (showDebugLogs)
        {
            Debug.LogWarning($"[WinCondition] ✓ 目標已被殺死: {target.name}");
        }
        
        // 如果所有目標都已死亡，顯示出口點/出生點提示
        if (allTargetsKilled)
        {
            if (useExitPoint)
            {
                // 目標被殺死後，顯示出口點
                ShowExitPointOnMap();
                CreateExitPointMarker();
                
                // Show notification to guide player to exit
                NotificationUIManager notificationUI = FindFirstObjectByType<NotificationUIManager>();
                if (notificationUI != null)
                {
                    notificationUI.ShowNotification("Target eliminated! Head to the exit point to complete the mission!", 5f);
                }
                
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[WinCondition] 出口點現已顯示，請前往 {exitPoint} 完成任務！");
                }
            }
            else
            {
                // 如果使用出生點，顯示提示
                NotificationUIManager notificationUI = FindFirstObjectByType<NotificationUIManager>();
                if (notificationUI != null)
                {
                    notificationUI.ShowNotification("All targets eliminated! Return to spawn point to complete the mission!", 5f);
                }
                
                if (showDebugLogs)
                {
                    Debug.LogWarning("[WinCondition] 所有目標已消滅，請返回出生點完成任務！");
                }
            }
        }
        
        // 檢查勝利條件
        CheckWinCondition();
    }
    
    /// <summary>
    /// 檢查是否所有 Target 都已死亡（使用 EntityManager 的現有方法）
    /// </summary>
    private void CheckAllTargetsKilled()
    {
        if (allTargetsKilled) return;
        
        // 直接使用 EntityManager 的現有方法
        EntityManager em = GetEntityManager();
        if (em != null)
        {
            allTargetsKilled = em.AreAllTargetsDead();
        }
        else
        {
            // 如果 EntityManager 不可用，回退到本地檢查
            bool allDead = true;
            foreach (var target in targets)
            {
                if (target != null && !target.IsDead)
                {
                    allDead = false;
                    break;
                }
            }
            allTargetsKilled = allDead;
        }
    }
    
    /// <summary>
    /// 目標逃脫事件處理
    /// </summary>
    private void OnTargetEscaped(Target target)
    {
        targetEscaped = true;
        
        if (showDebugLogs)
        {
            Debug.LogWarning($"[WinCondition] ✗ 目標已逃脫: {target.name}");
            Debug.LogWarning("[WinCondition] 任務失敗！");
        }
        
        // 目標逃脫 = 任務失敗 → 觸發 GameOver
        winConditionChecked = true;
        
        GameManager gm = GetGameManager();
        if (gm != null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[WinCondition] 觸發遊戲失敗...");
            }
            gm.GameOver("Target escaped");
        }
        else
        {
            Debug.LogError("[WinCondition] 無法觸發失敗：GameManager 未找到！");
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
            // 玩家死亡 = 任務失敗（已在 OnPlayerDied 中處理）
            return;
        }
        
        if (targetEscaped)
        {
            // 目標逃脫 = 任務失敗（已在 OnTargetEscaped 中處理）
            return;
        }
        
        // 檢查是否所有 Target 都已死亡
        CheckAllTargetsKilled();
        
        // 檢查勝利條件
        bool targetConditionMet = !requireTargetKilled || allTargetsKilled;
        bool locationConditionMet;
        
        if (useExitPoint)
        {
            // 使用出口點作為勝利條件
            locationConditionMet = !requireReachExit || playerReachedExit;
        }
        else
        {
            // 使用出生點作為勝利條件
            locationConditionMet = !requireReachExit || playerReachedSpawnPoint;
        }
        
        if (targetConditionMet && locationConditionMet)
        {
            // 所有條件滿足 = 勝利！
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
            Debug.LogWarning($"[WinCondition] - 目標已殺死: {targetKilled}");
            Debug.LogWarning($"[WinCondition] - 到達出口: {playerReachedExit}");
            Debug.LogWarning("[WinCondition] ========================================");
        }
        
        // 通知 GameManager 觸發勝利（延遲獲取以避免初始化順序問題）
        GameManager gm = GetGameManager();
        if (gm != null)
        {
            gm.GameWin();
        }
        else
        {
            Debug.LogError("[WinCondition] 無法觸發勝利：GameManager 仍未找到！");
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
        
        if (targetEscaped)
        {
            return "任務失敗：目標已逃脫";
        }
        
        string status = "勝利條件：\n";
        
        if (requireTargetKilled)
        {
            status += allTargetsKilled ? "✓ 殺死所有目標\n" : "○ 殺死所有目標\n";
        }
        
        if (requireReachExit)
        {
            string locationName = useExitPoint ? "出口" : "出生點";
            bool locationReached = useExitPoint ? playerReachedExit : playerReachedSpawnPoint;
            status += locationReached ? $"✓ 到達{locationName}\n" : $"○ 到達{locationName}\n";
        }
        
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
}

