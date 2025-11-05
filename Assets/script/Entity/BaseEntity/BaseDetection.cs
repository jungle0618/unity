using UnityEngine;

/// <summary>
/// 基礎偵測組件抽象類別
/// 提供所有實體共用的偵測功能接口
/// </summary>
public abstract class BaseDetection : MonoBehaviour
{
    [Header("圖層遮罩設定")]
    [SerializeField] protected LayerMask wallsLayerMask;
    [SerializeField] protected LayerMask objectsLayerMask;
    
    protected Transform target;

    protected virtual void Awake()
    {
        // 自動設定圖層遮罩（如果未設定）
        if (wallsLayerMask == 0)
        {
            int wallsLayer = LayerMask.NameToLayer("walls");
            if (wallsLayer != -1)
                wallsLayerMask = 1 << wallsLayer;
        }
        
        if (objectsLayerMask == 0)
        {
            int objectsLayer = LayerMask.NameToLayer("objects");
            if (objectsLayer != -1)
                objectsLayerMask = 1 << objectsLayer;
        }
    }

    /// <summary>
    /// 設定偵測目標
    /// </summary>
    public virtual void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 獲取當前目標
    /// </summary>
    public virtual Transform GetTarget()
    {
        return target;
    }

    /// <summary>
    /// 清除目標
    /// </summary>
    public virtual void ClearTarget()
    {
        target = null;
    }

    /// <summary>
    /// 檢查是否有有效的目標
    /// </summary>
    public virtual bool HasValidTarget()
    {
        return target != null;
    }

    /// <summary>
    /// 檢查是否可以看到目標（由子類別實現具體邏輯）
    /// </summary>
    public abstract bool CanSeeTarget(Vector2 targetPos);

    /// <summary>
    /// 檢查是否可以看到當前設定的目標
    /// </summary>
    public virtual bool CanSeeCurrentTarget()
    {
        if (target == null) return false;
        return CanSeeTarget(target.position);
    }

    /// <summary>
    /// 獲取到目標的距離
    /// </summary>
    public virtual float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        return Vector2.Distance(transform.position, target.position);
    }

    /// <summary>
    /// 獲取朝向目標的方向
    /// </summary>
    public virtual Vector2 GetDirectionToTarget()
    {
        if (target == null) return Vector2.zero;
        return (target.position - transform.position).normalized;
    }

    /// <summary>
    /// 設定視野方向（由子類別實現具體邏輯）
    /// </summary>
    public virtual void SetViewDirection(Vector2 direction)
    {
        // 子類別可以覆寫此方法
    }

    /// <summary>
    /// 獲取當前視野方向（由子類別實現具體邏輯）
    /// </summary>
    public virtual Vector2 GetViewDirection()
    {
        return transform.right; // 預設返回 transform.right
    }

    /// <summary>
    /// 設定偵測參數（由子類別實現具體邏輯）
    /// </summary>
    public virtual void SetDetectionParameters(params object[] parameters)
    {
        // 子類別可以覆寫此方法
    }

    /// <summary>
    /// 獲取障礙物圖層遮罩（預設只返回 walls layer）
    /// 子類別可以覆寫此方法來實現自訂邏輯
    /// </summary>
    protected virtual LayerMask GetObstacleLayerMask()
    {
        return wallsLayerMask;
    }

    /// <summary>
    /// 檢查是否被障礙物遮擋
    /// </summary>
    protected bool IsBlockedByObstacle(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        float distance = Vector2.Distance(from, to);
        LayerMask obstacleMask = GetObstacleLayerMask();
        
        if (obstacleMask == 0)
            return false;
            
        RaycastHit2D hit = Physics2D.Raycast(from, direction, distance, obstacleMask);
        return hit.collider != null;
    }

    /// <summary>
    /// 設定圖層遮罩
    /// </summary>
    public void SetLayerMasks(LayerMask walls, LayerMask objects)
    {
        wallsLayerMask = walls;
        objectsLayerMask = objects;
    }
}

