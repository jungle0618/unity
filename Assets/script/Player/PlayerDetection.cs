using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家偵測系統（繼承基礎偵測組件）
/// - 判斷哪些敵人在玩家視野中
/// - 與 EnemyManager 整合，控制敵人渲染
/// - 不 deactivate 敵人，只控制渲染組件
/// </summary>
public class PlayerDetection : BaseDetection
{
    [Header("偵測參數")]
    // 視野參數現在從 Player（原 PlayerController）獲取
    // 圖層遮罩已移至 BaseDetection
    
    [Header("性能優化")]
    [SerializeField] private float updateInterval = 0.1f; // 更新間隔
    [SerializeField] private int enemiesPerFrameCheck = 5; // 每幀檢查的敵人數量
    
    [Header("除錯")]
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private Color visibleEnemyColor = Color.green;
    [SerializeField] private Color hiddenEnemyColor = Color.red;
    
    // 組件引用
    private Player player;
    private EnemyManager enemyManager;
    
    // 敵人可見性管理（只記錄可見性狀態，不處理渲染）
    private HashSet<Enemy> visibleEnemies = new HashSet<Enemy>();
    private HashSet<Enemy> hiddenEnemies = new HashSet<Enemy>();
    
    // 追蹤每個敵人的前一個可見性狀態（用於只在狀態改變時觸發更新）
    private Dictionary<Enemy, bool> previousVisibilityStates = new Dictionary<Enemy, bool>();
    
    // 性能優化
    private float lastUpdateTime = 0f;
    private int currentCheckIndex = 0;
    private List<Enemy> allEnemiesList = new List<Enemy>();
    
    // 玩家方向
    private Vector2 playerDirection = Vector2.right;
    
    public float ViewRange => GetViewRange();
    public float ViewAngle => GetViewAngle();
    public int VisibleEnemyCount => visibleEnemies.Count;
    public int HiddenEnemyCount => hiddenEnemies.Count;

    protected override void Awake()
    {
        base.Awake(); // 調用基類 Awake
        player = GetComponent<Player>();
        enemyManager = FindFirstObjectByType<EnemyManager>();
    }
    
    private void Start()
    {
        if (enemyManager == null)
        {
            Debug.LogError("[PlayerDetection] 找不到 EnemyManager！");
            enabled = false;
            return;
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
        
        // 定期更新敵人可見性
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateEnemyVisibility();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// 更新敵人可見性（分批處理以提升性能）
    /// </summary>
    private void UpdateEnemyVisibility()
    {
        if (enemyManager == null) return;
        
        // 獲取所有敵人列表
        allEnemiesList.Clear();
        allEnemiesList.AddRange(enemyManager.GetAllActiveEnemies());
        
        if (allEnemiesList.Count == 0) return;
        
        // 分批檢查敵人可見性
        int enemiesToCheck = Mathf.Min(enemiesPerFrameCheck, allEnemiesList.Count);
        
        for (int i = 0; i < enemiesToCheck; i++)
        {
            if (currentCheckIndex >= allEnemiesList.Count)
                currentCheckIndex = 0;
            
            if (currentCheckIndex < allEnemiesList.Count)
            {
                Enemy enemy = allEnemiesList[currentCheckIndex];
                if (enemy != null && !enemy.IsDead)
                {
                    CheckEnemyVisibility(enemy);
                }
                currentCheckIndex++;
            }
        }
    }
    
    /// <summary>
    /// 檢查單個敵人的可見性（只更新可見性狀態，只在狀態改變時通知 EnemyManager）
    /// </summary>
    private void CheckEnemyVisibility(Enemy enemy)
    {
        if (enemy == null) return;
        
        bool isVisible = IsEnemyInPlayerView(enemy);
        
        // 檢查前一個狀態是否存在且是否改變
        bool stateChanged = false;
        if (previousVisibilityStates.ContainsKey(enemy))
        {
            bool previousState = previousVisibilityStates[enemy];
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
        previousVisibilityStates[enemy] = isVisible;
        
        if (isVisible)
        {
            // 敵人可見
            if (hiddenEnemies.Contains(enemy))
            {
                hiddenEnemies.Remove(enemy);
            }
            if (!visibleEnemies.Contains(enemy))
            {
                visibleEnemies.Add(enemy);
            }
        }
        else
        {
            // 敵人不可見
            if (visibleEnemies.Contains(enemy))
            {
                visibleEnemies.Remove(enemy);
            }
            if (!hiddenEnemies.Contains(enemy))
            {
                hiddenEnemies.Add(enemy);
            }
        }
        
        // 只有當狀態改變時，才通知 EnemyManager 更新視覺化和渲染狀態
        if (stateChanged && enemyManager != null)
        {
            enemyManager.OnEnemyVisibilityChanged(enemy, isVisible);
        }
    }
    
    /// <summary>
    /// 檢查敵人是否在玩家視野中
    /// </summary>
    private bool IsEnemyInPlayerView(Enemy enemy)
    {
        if (enemy == null) return false;
        
        Vector2 playerPos = transform.position;
        Vector2 enemyPos = enemy.Position;
        Vector2 dirToEnemy = enemyPos - playerPos;
        float distanceToEnemy = dirToEnemy.magnitude;
        
        // 距離檢查
        if (distanceToEnemy > GetViewRange())
            return false;
        
        // 角度檢查
        if (GetViewAngle() < 360f)
        {
            float angleToEnemy = Vector2.Angle(playerDirection, dirToEnemy);
            if (angleToEnemy > GetViewAngle() * 0.5f)
                return false;
        }
        
        // 遮擋檢查 - 使用 BaseDetection 提供的方法
        if (IsBlockedByObstacle(playerPos, enemyPos))
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
    /// 強制更新所有敵人可見性
    /// </summary>
    public void ForceUpdateAllEnemies()
    {
        if (enemyManager == null) return;
        
        var allEnemies = enemyManager.GetAllActiveEnemies();
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                CheckEnemyVisibility(enemy);
            }
        }
        
        Debug.Log($"[PlayerDetection] 強制更新完成 - 可見: {visibleEnemies.Count}, 隱藏: {hiddenEnemies.Count}");
    }
    
    /// <summary>
    /// 新增敵人到檢測系統
    /// </summary>
    public void AddEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        
        CheckEnemyVisibility(enemy);
    }
    
    /// <summary>
    /// 從檢測系統移除敵人
    /// </summary>
    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        
        visibleEnemies.Remove(enemy);
        hiddenEnemies.Remove(enemy);
        previousVisibilityStates.Remove(enemy);
    }
    
    /// <summary>
    /// 獲取可見敵人列表
    /// </summary>
    public List<Enemy> GetVisibleEnemies()
    {
        return new List<Enemy>(visibleEnemies);
    }
    
    /// <summary>
    /// 獲取隱藏敵人列表
    /// </summary>
    public List<Enemy> GetHiddenEnemies()
    {
        return new List<Enemy>(hiddenEnemies);
    }
    
    /// <summary>
    /// 檢查特定敵人是否可見
    /// </summary>
    public bool IsEnemyVisible(Enemy enemy)
    {
        return enemy != null && visibleEnemies.Contains(enemy);
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Vector3 playerPos = transform.position;
        
        // 繪製視野範圍
        Gizmos.color = new Color(0, 1, 1, 0.3f); // 青色半透明
        DrawViewCone(playerPos, playerDirection, GetViewRange(), GetViewAngle());
        
        // 繪製可見敵人
        Gizmos.color = visibleEnemyColor;
        foreach (var enemy in visibleEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.Position, 0.5f);
                Gizmos.DrawLine(playerPos, enemy.Position);
            }
        }
        
        // 繪製隱藏敵人
        Gizmos.color = hiddenEnemyColor;
        foreach (var enemy in hiddenEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.Position, 0.3f);
            }
        }
    }
    
    /// <summary>
    /// 繪製視野錐形
    /// </summary>
    private void DrawViewCone(Vector3 center, Vector2 direction, float range, float angle)
    {
        if (angle >= 360f)
        {
            // 全方向視野
            Gizmos.DrawWireSphere(center, range);
            return;
        }
        
        float halfAngle = angle * 0.5f;
        float directionAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 計算邊界方向
        Vector3 leftBoundary = Quaternion.Euler(0, 0, halfAngle) * direction * range;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -halfAngle) * direction * range;
        
        // 繪製邊界線
        Gizmos.DrawLine(center, center + leftBoundary);
        Gizmos.DrawLine(center, center + rightBoundary);
        
        // 繪製弧線
        int segments = Mathf.Max(8, Mathf.RoundToInt(angle / 5f));
        Vector3[] arcPoints = new Vector3[segments + 1];
        
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = directionAngle - halfAngle + t * angle;
            Vector3 direction3D = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0);
            arcPoints[i] = center + direction3D * range;
        }
        
        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(arcPoints[i], arcPoints[i + 1]);
        }
    }
    
    private void OnDestroy()
    {
        // 清理資源
        visibleEnemies.Clear();
        hiddenEnemies.Clear();
        previousVisibilityStates.Clear();
    }
}
