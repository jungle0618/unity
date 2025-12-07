using UnityEngine;

/// <summary>
/// 敵人狀態，包含巡邏、警戒、追擊、搜索、返回、死亡
/// </summary>
public enum EnemyState
{
    Patrol,     // 巡邏
    Alert,      // 警戒
    Chase,      // 追擊
    Search,     // 搜索（到最後看到玩家的位置）
    Return,     // 返回
    Dead        // 死亡
}

/// <summary>
/// 敵人狀態機（繼承基礎狀態機）
/// </summary>
public class EnemyStateMachine : BaseStateMachine<EnemyState>
{
    private float alertTimer;
    private readonly float alertTime;

    public EnemyStateMachine(float alertTime)
    {
        this.alertTime = alertTime;
        CurrentState = EnemyState.Patrol; // 初始化狀態
    }

    /// <summary>
    /// 更新狀態（覆寫基類方法）
    /// </summary>
    public override void UpdateState(float deltaTime)
    {
        UpdateAlertTimer();
    }

    /// <summary>
    /// 改變狀態（覆寫基類方法，保留原有邏輯）
    /// </summary>
    public override void ChangeState(EnemyState newState)
    {
        //Debug.Log($"ChangeState: {CurrentState} -> {newState}");
        if (CurrentState.Equals(newState)) return;

        EnemyState oldState = CurrentState;
        OnExitState(CurrentState);

        CurrentState = newState;
        OnEnterState(newState);

        // 更新死亡狀態
        UpdateDeadStatus(newState);

        OnStateChanged?.Invoke(oldState, newState);
    }

    /// <summary>
    /// 更新警戒計時器
    /// </summary>
    public void UpdateAlertTimer()
    {
        if (CurrentState == EnemyState.Alert)
        {
            alertTimer -= Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// 檢查警戒時間是否結束
    /// </summary>
    public bool IsAlertTimeUp()
    {
        return alertTimer <= 0f;
    }

    protected override void OnEnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Alert:
                alertTimer = alertTime;
                break;
        }
    }

    protected override void OnExitState(EnemyState state)
    {
        // 可在狀態加入時，進行特殊管理
    }

    protected override void UpdateDeadStatus(EnemyState state)
    {
        IsDead = state == EnemyState.Dead;
    }
}