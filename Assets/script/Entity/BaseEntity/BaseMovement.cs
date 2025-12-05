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

