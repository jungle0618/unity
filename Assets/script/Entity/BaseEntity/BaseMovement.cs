using UnityEngine;

/// <summary>
/// 基礎移動組件抽象類別
/// 提供所有實體共用的移動功能接口
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class BaseMovement : MonoBehaviour
{
    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}: Missing Rigidbody2D component!");
        }
    }

    /// <summary>
    /// 向目標位置移動
    /// </summary>
    /// <param name="target">目標位置</param>
    /// <param name="speedMultiplier">速度倍數</param>
    public abstract void MoveTowards(Vector2 target, float speedMultiplier);
    
    /// <summary>
    /// 獲取受傷速度倍數（如果受傷則返回 0.7，否則返回 1.0）
    /// </summary>
    protected float GetInjurySpeedMultiplier()
    {
        // 從 EntityHealth 組件檢查是否受傷
        EntityHealth health = GetComponent<EntityHealth>();
        if (health != null && !health.IsDead)
        {
            // 如果當前血量小於最大血量，表示受傷
            if (health.CurrentHealth < health.MaxHealth)
            {
                return 0.7f; // 受傷時速度乘以 0.7
            }
        }
        return 1.0f; // 未受傷時正常速度
    }

    /// <summary>
    /// 停止移動
    /// </summary>
    public virtual void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// 獲取當前移動方向
    /// </summary>
    public virtual Vector2 GetMovementDirection()
    {
        if (rb == null) return Vector2.right;
        return rb.linearVelocity.normalized;
    }

    /// <summary>
    /// 獲取朝向目標的方向
    /// </summary>
    public virtual Vector2 GetDirectionToTarget(Vector2 target)
    {
        return (target - (Vector2)transform.position).normalized;
    }

    /// <summary>
    /// 檢查是否到達目標位置
    /// </summary>
    /// <param name="target">目標位置</param>
    /// <param name="threshold">到達閾值</param>
    public virtual bool HasArrivedAt(Vector2 target, float threshold = 0.2f)
    {
        return Vector2.Distance(transform.position, target) < threshold;
    }

    /// <summary>
    /// 設定移動速度（由子類別實現具體邏輯）
    /// </summary>
    public virtual void SetSpeed(float speed)
    {
        // 子類別可以覆寫此方法
    }

    /// <summary>
    /// 獲取移動速度（由子類別實現具體邏輯）
    /// </summary>
    public virtual float GetSpeed()
    {
        return 0f; // 子類別需要實現
    }
}

