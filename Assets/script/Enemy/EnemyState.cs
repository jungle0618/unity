using UnityEngine;

/// <summary>
/// 敵人狀態枚舉和狀態相關的數據結構
/// </summary>
public enum EnemyState
{
    Patrol,     // 巡邏
    Alert,      // 警戒
    Chase,      // 追擊
    Return,     // 返回
    Dead        // 死亡
}

/// <summary>
/// 敵人狀態機管理器
/// </summary>
public class EnemyStateMachine
{
    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
    public bool IsDead => CurrentState == EnemyState.Dead;

    // 狀態變更事件
    public System.Action<EnemyState, EnemyState> OnStateChanged;

    private float alertTimer;
    private readonly float alertTime;

    public EnemyStateMachine(float alertTime)
    {
        this.alertTime = alertTime;
    }

    /// <summary>
    /// 改變狀態
    /// </summary>
    public void ChangeState(EnemyState newState)
    {
        if (CurrentState == newState) return;

        EnemyState oldState = CurrentState;
        OnExitState(CurrentState);

        CurrentState = newState;
        OnEnterState(newState);

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

    private void OnEnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Alert:
                alertTimer = alertTime;
                break;
        }
    }

    private void OnExitState(EnemyState state)
    {
        // 可在此處加入離開狀態時的清理邏輯
    }
}
