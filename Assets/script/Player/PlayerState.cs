using UnityEngine;

/// <summary>
/// 玩家狀態，包含空閒、移動、攻擊、死亡
/// </summary>
public enum PlayerState
{
    Idle,      // 空閒
    Moving,    // 移動中
    Attacking, // 攻擊中
    Dead       // 死亡
}

/// <summary>
/// 玩家狀態機（繼承基礎狀態機）
/// </summary>
public class PlayerStateMachine : BaseStateMachine<PlayerState>
{
    public PlayerStateMachine()
    {
        CurrentState = PlayerState.Idle; // 初始化狀態
    }

    /// <summary>
    /// 更新狀態（覆寫基類方法）
    /// </summary>
    public override void UpdateState(float deltaTime)
    {
        // Player 狀態機目前不需要每幀更新邏輯
        // 狀態轉換由外部邏輯觸發
    }

    /// <summary>
    /// 改變狀態（覆寫基類方法）
    /// </summary>
    public override void ChangeState(PlayerState newState)
    {
        if (CurrentState.Equals(newState)) return;

        PlayerState oldState = CurrentState;
        OnExitState(CurrentState);

        CurrentState = newState;
        OnEnterState(newState);

        // 更新死亡狀態
        UpdateDeadStatus(newState);

        OnStateChanged?.Invoke(oldState, newState);
    }

    protected override void OnEnterState(PlayerState state)
    {
        // 可以在此添加狀態進入邏輯
    }

    protected override void OnExitState(PlayerState state)
    {
        // 可以在此添加狀態離開邏輯
    }

    protected override void UpdateDeadStatus(PlayerState state)
    {
        IsDead = state == PlayerState.Dead;
    }
}

