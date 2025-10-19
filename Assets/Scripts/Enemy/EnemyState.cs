using UnityEngine;

/// <summary>
/// �ĤH���A�T�|�M���A�������ƾڵ��c
/// </summary>
public enum EnemyState
{
    Patrol,     // ����
    Alert,      // ĵ��
    Chase,      // �l��
    Return,     // ��^
    Dead        // ���`
}

/// <summary>
/// �ĤH���A���޲z��
/// </summary>
public class EnemyStateMachine
{
    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
    public bool IsDead => CurrentState == EnemyState.Dead;

    // ���A�ܧ�ƥ�
    public System.Action<EnemyState, EnemyState> OnStateChanged;

    private float alertTimer;
    private readonly float alertTime;

    public EnemyStateMachine(float alertTime)
    {
        this.alertTime = alertTime;
    }

    /// <summary>
    /// ���ܪ��A
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
    /// ��sĵ�٭p�ɾ�
    /// </summary>
    public void UpdateAlertTimer()
    {
        if (CurrentState == EnemyState.Alert)
        {
            alertTimer -= Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// �ˬdĵ�ٮɶ��O�_����
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
        // �i�b���B�[�J���}���A�ɪ��M�z�޿�
    }
}
