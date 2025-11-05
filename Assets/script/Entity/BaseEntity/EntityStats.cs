using UnityEngine;

/// <summary>
/// 通用屬性管理組件
/// 用於管理危險等級相關的屬性乘數（Enemy 和 Target 使用）
/// </summary>
public class EntityStats : MonoBehaviour
{
    // 當前危險等級乘數（由 EntityManager 設定）
    private float currentViewRangeMultiplier = 1f;
    private float currentViewAngleMultiplier = 1f;
    private float currentSpeedMultiplier = 1f;
    private float currentDamageReduction = 0f;
    
    // 屬性訪問器
    public float ViewRangeMultiplier => currentViewRangeMultiplier;
    public float ViewAngleMultiplier => currentViewAngleMultiplier;
    public float SpeedMultiplier => currentSpeedMultiplier;
    public float DamageReduction => currentDamageReduction;
    
    /// <summary>
    /// 更新危險等級相關屬性
    /// </summary>
    /// <param name="viewRangeMultiplier">視野範圍倍數</param>
    /// <param name="viewAngleMultiplier">視野角度倍數</param>
    /// <param name="speedMultiplier">速度倍數</param>
    /// <param name="damageReduction">傷害減少比例（0-1）</param>
    public void UpdateDangerLevelStats(float viewRangeMultiplier, float viewAngleMultiplier, 
                                        float speedMultiplier, float damageReduction)
    {
        currentViewRangeMultiplier = viewRangeMultiplier;
        currentViewAngleMultiplier = viewAngleMultiplier;
        currentSpeedMultiplier = speedMultiplier;
        currentDamageReduction = Mathf.Clamp01(damageReduction);
    }
    
    /// <summary>
    /// 設定移動速度倍數
    /// </summary>
    /// <param name="multiplier">速度倍數</param>
    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeedMultiplier = multiplier;
    }
    
    /// <summary>
    /// 設定視野範圍倍數
    /// </summary>
    /// <param name="multiplier">視野範圍倍數</param>
    public void SetViewRangeMultiplier(float multiplier)
    {
        currentViewRangeMultiplier = multiplier;
    }
    
    /// <summary>
    /// 設定視野角度倍數
    /// </summary>
    /// <param name="multiplier">視野角度倍數</param>
    public void SetViewAngleMultiplier(float multiplier)
    {
        currentViewAngleMultiplier = multiplier;
    }
    
    /// <summary>
    /// 設定傷害減少
    /// </summary>
    /// <param name="reduction">傷害減少比例（0-1）</param>
    public void SetDamageReduction(float reduction)
    {
        currentDamageReduction = Mathf.Clamp01(reduction);
    }
    
    /// <summary>
    /// 獲取傷害減少
    /// </summary>
    /// <returns>傷害減少比例</returns>
    public float GetDamageReduction()
    {
        return currentDamageReduction;
    }
}

