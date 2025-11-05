using UnityEngine;

/// <summary>
/// Target狀態，包含停留、逃亡和死亡三種狀態
/// </summary>
public enum TargetState
{
    Stay,       // 停留（不移動不轉動）
    Escape,     // 逃亡（往逃亡點移動）
    Dead        // 死亡（停止所有移動和 AI 邏輯）
}

/// <summary>
/// Target狀態機（繼承基礎狀態機）
/// </summary>
public class TargetStateMachine : BaseStateMachine<TargetState>
{
    public TargetStateMachine()
    {
        CurrentState = TargetState.Stay; // 初始化狀態為停留
    }

    /// <summary>
    /// 更新狀態（覆寫基類方法）
    /// </summary>
    public override void UpdateState(float deltaTime)
    {
        // Target 狀態機不需要額外更新邏輯
    }

    /// <summary>
    /// 改變狀態（覆寫基類方法）
    /// </summary>
    public override void ChangeState(TargetState newState)
    {
        if (CurrentState.Equals(newState)) return;

        TargetState oldState = CurrentState;
        OnExitState(CurrentState);

        CurrentState = newState;
        OnEnterState(newState);

        // 更新死亡狀態（Target 沒有死亡狀態，但保留以兼容基類）
        UpdateDeadStatus(newState);

        OnStateChanged?.Invoke(oldState, newState);
    }

    protected override void OnEnterState(TargetState state)
    {
        // Target 狀態轉換邏輯
    }

    protected override void OnExitState(TargetState state)
    {
        // Target 狀態轉換邏輯
    }

    protected override void UpdateDeadStatus(TargetState state)
    {
        // 更新死亡狀態
        IsDead = (state == TargetState.Dead);
    }
}