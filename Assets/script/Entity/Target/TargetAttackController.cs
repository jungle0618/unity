using UnityEngine;

/// <summary>
/// Target攻擊控制器（目標不需要攻擊功能，此類保留以兼容性）
/// 注意：目標沒有武裝，不會攻擊玩家
/// </summary>
public class TargetAttackController : MonoBehaviour
{
    /// <summary>
    /// 嘗試攻擊玩家（目標不攻擊，總是返回 false）
    /// </summary>
    public bool TryAttackPlayer(Transform playerTransform)
    {
        // 目標不攻擊
        return false;
    }

    /// <summary>
    /// 設定攻擊冷卻時間（目標不需要，保留以兼容性）
    /// </summary>
    public void SetAttackCooldown(float cooldownSeconds)
    {
        // 目標不需要攻擊功能
    }
}