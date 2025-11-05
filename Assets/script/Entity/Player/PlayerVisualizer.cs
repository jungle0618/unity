using UnityEngine;

/// <summary>
/// 玩家視覺化組件（繼承基礎視覺化組件）
/// 職責：處理玩家視野運行時顯示、狀態顏色顯示
/// 
/// 重寫版本特點：
/// - 射線檢測只會在碰到 Objects 或 Walls 圖層時停止
/// - 當玩家蹲下時，objects layer 也會遮擋視線
/// - 使用圖層快取提高性能
/// - 正確的 Physics2D.Raycast 參數順序
/// - 支援大小寫不敏感的圖層名稱
/// - 血量顏色處理由基類 BaseVisualizer 提供
/// </summary>
public class PlayerVisualizer : BaseVisualizer
{
    [Header("物體圖層設定")]
    [SerializeField] protected LayerMask objectsLayer = -1; // 物體圖層遮罩（可在 Inspector 中設定）
    
    private Player player;
    private Vector2 lastWeaponDirection = Vector2.right; // 用於視野範圍計算

    protected override void OnInitialize()
    {
        base.OnInitialize();
        player = GetComponent<Player>();
        
        // 訂閱玩家血量變化事件
        if (player != null)
        {
            player.OnHealthChanged += HandleHealthChanged;
        }
        
        // 自動設定 objectsLayer（如果未在 Inspector 中設定）
        if (objectsLayer == -1)
        {
            int objectsLayerIndex = LayerMask.NameToLayer("Objects");
            if (objectsLayerIndex == -1)
            {
                objectsLayerIndex = LayerMask.NameToLayer("objects");
            }
            
            if (objectsLayerIndex != -1)
            {
                objectsLayer = 1 << objectsLayerIndex;
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
        LayerMask baseMask = base.GetObstacleLayerMask(); // 獲取 walls layer
        
        // 如果玩家正在蹲下，添加 objects layer
        if (player != null && player.IsSquatting)
        {
            return baseMask | objectsLayer;
        }
        
        // 玩家站立時，只使用 walls layer
        return baseMask;
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (player != null)
        {
            player.OnHealthChanged -= HandleHealthChanged;
        }
    }
    
    /// <summary>
    /// 處理玩家血量變化事件
    /// </summary>
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // 調用基類的血量變化處理方法
        OnHealthChanged(currentHealth, maxHealth);
    }

    private void Update()
    {
        // 更新武器方向（用於視野範圍計算）
        if (player != null)
        {
            lastWeaponDirection = player.GetWeaponDirection();
        }
        
        // 更新運行時視野顯示
        UpdateRuntimeVisionDisplay();
    }
    
    /// <summary>
    /// 更新運行時視野顯示
    /// </summary>
    private void UpdateRuntimeVisionDisplay()
    {
        if (!showRuntimeVision || player == null) return;
        
        Vector3 pos = transform.position;
        float currentViewRange = GetViewRange();
        float currentViewAngle = GetViewAngle();
        
        // 獲取玩家朝向（武器方向）
        float currentDirection = Mathf.Atan2(lastWeaponDirection.y, lastWeaponDirection.x) * Mathf.Rad2Deg;
        
        // 計算扇形的起始角度（以當前方向為中心）
        float halfAngle = currentViewAngle * 0.5f;
        float startAngle = currentDirection - halfAngle;
        
        // 更新運行時視野範圍
        UpdateRuntimeVision(pos, startAngle, currentViewAngle, currentViewRange, runtimeVisionColor);
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
}
