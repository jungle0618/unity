using UnityEngine;

/// <summary>
/// 武器耐久度系統使用示例
/// 展示如何使用耐久度相關的功能
/// </summary>
public class WeaponDurabilityExample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponHolder weaponHolder;
    
    [Header("Debug Controls")]
    [SerializeField] private KeyCode repairKey = KeyCode.R;
    [SerializeField] private KeyCode damageKey = KeyCode.T;
    [SerializeField] private int repairAmount = 10;
    [SerializeField] private int damageAmount = 5;
    
    private void Start()
    {
        // 訂閱武器耐久度事件
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponDurabilityChanged += OnWeaponDurabilityChanged;
            weaponHolder.OnWeaponBroken += OnWeaponBroken;
        }
    }
    
    private void Update()
    {
        // 按R鍵修復武器
        if (Input.GetKeyDown(repairKey))
        {
            RepairWeapon();
        }
        
        // 按T鍵手動損壞武器（用於測試）
        if (Input.GetKeyDown(damageKey))
        {
            DamageWeapon();
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponDurabilityChanged -= OnWeaponDurabilityChanged;
            weaponHolder.OnWeaponBroken -= OnWeaponBroken;
        }
    }
    
    /// <summary>
    /// 修復武器
    /// </summary>
    public void RepairWeapon()
    {
        if (weaponHolder != null)
        {
            weaponHolder.RepairCurrentWeapon(repairAmount);
            Debug.Log($"修復武器 {repairAmount} 點耐久度");
        }
    }
    
    /// <summary>
    /// 手動損壞武器（用於測試）
    /// </summary>
    public void DamageWeapon()
    {
        if (weaponHolder != null && weaponHolder.CurrentWeapon != null)
        {
            weaponHolder.CurrentWeapon.ReduceDurability(damageAmount);
            Debug.Log($"武器耐久度減少 {damageAmount} 點");
        }
    }
    
    /// <summary>
    /// 完全修復武器
    /// </summary>
    public void FullRepairWeapon()
    {
        if (weaponHolder != null)
        {
            weaponHolder.FullRepairCurrentWeapon();
            Debug.Log("武器已完全修復");
        }
    }
    
    /// <summary>
    /// 處理耐久度變化事件
    /// </summary>
    private void OnWeaponDurabilityChanged(int currentDurability, int maxDurability)
    {
        float percentage = maxDurability > 0 ? (float)currentDurability / maxDurability : 0f;
        Debug.Log($"武器耐久度: {currentDurability}/{maxDurability} ({percentage:P0})");
        
        // 根據耐久度百分比給出警告
        if (percentage <= 0.2f && percentage > 0f)
        {
            Debug.LogWarning("武器耐久度極低！");
        }
        else if (percentage <= 0.5f && percentage > 0.2f)
        {
            Debug.LogWarning("武器耐久度較低，建議修復");
        }
    }
    
    /// <summary>
    /// 處理武器損壞事件
    /// </summary>
    private void OnWeaponBroken()
    {
        Debug.LogError("武器已損壞！無法攻擊！");
        
        // 可以在這裡添加其他邏輯，比如：
        // - 播放損壞音效
        // - 顯示損壞UI
        // - 自動切換到其他武器
        // - 觸發遊戲事件等
    }
    
    /// <summary>
    /// 獲取武器耐久度信息
    /// </summary>
    public void LogWeaponDurabilityInfo()
    {
        if (weaponHolder != null)
        {
            var info = weaponHolder.GetWeaponDurabilityInfo();
            Debug.Log($"武器耐久度信息: 當前={info.current}, 最大={info.max}, 百分比={info.percentage:P0}");
        }
    }
}
