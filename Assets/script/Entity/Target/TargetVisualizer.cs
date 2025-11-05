using UnityEngine;

/// <summary>
/// Target視覺化組件（繼承基礎視覺化組件）
/// 職責：處理運行時視野顯示、血量顏色顯示
/// </summary>
public class TargetVisualizer : BaseVisualizer
{
    private Target target;
    private TargetMovement movement;
    private TargetDetection detection;
    private TargetStateMachine stateMachine;
    private bool canVisualize = true; // 是否可以執行視覺化（由 Target 控制，影響 Gizmos 繪製等）

    protected override void OnInitialize()
    {
        base.OnInitialize();
        target = GetComponent<Target>();
        movement = GetComponent<TargetMovement>();
        detection = GetComponent<TargetDetection>();
        
        // 訂閱Target血量變化事件
        if (target != null)
        {
            target.OnHealthChanged += HandleHealthChanged;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (target != null)
        {
            target.OnHealthChanged -= HandleHealthChanged;
        }
    }
    
    /// <summary>
    /// 處理Target血量變化事件
    /// </summary>
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // 調用基類的血量變化處理方法
        OnHealthChanged(currentHealth, maxHealth);
    }

    /// <summary>
    /// 設定狀態機參考
    /// </summary>
    public void SetStateMachine(TargetStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 設定是否可以執行視覺化（由 Target 調用）
    /// 控制視覺化邏輯的執行（如 Gizmos）和所有渲染組件
    /// </summary>
    public void SetCanVisualize(bool canVisualize)
    {
        this.canVisualize = canVisualize;
        
        // 設置所有渲染組件的可見性
        SetRendererVisibility(canVisualize);
    }

    /// <summary>
    /// 設定是否在玩家視野內（用於 PlayerDetection 系統，現已由 EnemyManager 處理）
    /// </summary>
    /// <param name="inView">是否在玩家視野內</param>
    public void SetInPlayerView(bool inView)
    {
        // 可以在這裡實現視覺化邏輯，例如改變透明度或顏色
        // 目前為空實現，保留接口供未來擴展
    }
    
    private void Update()
    {
        // 更新運行時視野顯示
        UpdateRuntimeVisionDisplay();
    }
    
    /// <summary>
    /// 更新運行時視野顯示
    /// </summary>
    private void UpdateRuntimeVisionDisplay()
    {
        // 如果不可以視覺化或未啟用運行時視野，不執行更新
        if (!canVisualize || !showRuntimeVision || detection == null) return;
        
        Vector3 pos = transform.position;
        float viewRange = detection.ViewRange;
        float viewAngle = detection.ViewAngle;
        
        // 獲取敵人朝向（transform.right 是敵人的前方方向）
        Vector2 viewDirection = transform.right;
        float currentDirection = Mathf.Atan2(viewDirection.y, viewDirection.x) * Mathf.Rad2Deg;
        
        // 計算扇形的起始角度（以當前方向為中心）
        float halfAngle = viewAngle * 0.5f;
        float startAngle = currentDirection - halfAngle;
        
        // 更新運行時視野範圍（使用黃色）
        UpdateRuntimeVision(pos, startAngle, viewAngle, viewRange, runtimeVisionColor);
    }
}