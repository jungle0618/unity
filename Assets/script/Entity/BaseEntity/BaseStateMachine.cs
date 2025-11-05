using UnityEngine;

/// <summary>
/// 基礎狀態機抽象類別（泛型）
/// 支援任意枚舉類型的狀態
/// </summary>
public abstract class BaseStateMachine<TState> where TState : System.Enum
{
    public TState CurrentState { get; protected set; }
    public bool IsDead { get; protected set; }

    // 狀態變更事件
    public System.Action<TState, TState> OnStateChanged;

    /// <summary>
    /// 改變狀態
    /// </summary>
    public virtual void ChangeState(TState newState)
    {
        if (CurrentState.Equals(newState)) return;

        TState oldState = CurrentState;
        OnExitState(CurrentState);

        CurrentState = newState;
        OnEnterState(newState);

        // 更新死亡狀態
        UpdateDeadStatus(newState);

        OnStateChanged?.Invoke(oldState, newState);
    }

    /// <summary>
    /// 更新狀態（每幀調用）
    /// </summary>
    public virtual void UpdateState(float deltaTime)
    {
        // 子類別可以覆寫此方法來實現狀態特定的更新邏輯
    }

    /// <summary>
    /// 進入狀態時的處理
    /// </summary>
    protected virtual void OnEnterState(TState state)
    {
        // 子類別可以覆寫此方法來實現狀態進入邏輯
    }

    /// <summary>
    /// 離開狀態時的處理
    /// </summary>
    protected virtual void OnExitState(TState state)
    {
        // 子類別可以覆寫此方法來實現狀態離開邏輯
    }

    /// <summary>
    /// 更新死亡狀態（由子類別實現具體邏輯）
    /// </summary>
    protected abstract void UpdateDeadStatus(TState state);

    /// <summary>
    /// 檢查是否可以轉換到指定狀態
    /// </summary>
    public virtual bool CanTransitionTo(TState newState)
    {
        return true; // 預設允許所有轉換，子類別可以覆寫以添加限制
    }
}


