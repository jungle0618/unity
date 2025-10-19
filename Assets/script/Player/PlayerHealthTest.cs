using UnityEngine;

/// <summary>
/// 玩家血量測試腳本
/// 用於測試血量系統的功能
/// </summary>
public class PlayerHealthTest : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private KeyCode damageKey = KeyCode.D;
    [SerializeField] private KeyCode healKey = KeyCode.H;
    [SerializeField] private KeyCode fullHealKey = KeyCode.F;
    [SerializeField] private KeyCode killKey = KeyCode.K;
    [SerializeField] private KeyCode resurrectKey = KeyCode.R;
    
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private int healAmount = 20;
    
    private PlayerController playerController;
    
    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        
        if (playerController == null)
        {
            Debug.LogError("PlayerHealthTest: 找不到PlayerController！");
        }
    }
    
    private void Update()
    {
        if (playerController == null) return;
        
        // 測試按鍵
        if (Input.GetKeyDown(damageKey))
        {
            DamagePlayer();
        }
        
        if (Input.GetKeyDown(healKey))
        {
            HealPlayer();
        }
        
        if (Input.GetKeyDown(fullHealKey))
        {
            FullHealPlayer();
        }
        
        if (Input.GetKeyDown(killKey))
        {
            KillPlayer();
        }
        
        if (Input.GetKeyDown(resurrectKey))
        {
            ResurrectPlayer();
        }
    }
    
    /// <summary>
    /// 對玩家造成傷害
    /// </summary>
    public void DamagePlayer()
    {
        if (playerController != null)
        {
            playerController.TakeDamage(damageAmount, "Test Damage");
        }
    }
    
    /// <summary>
    /// 治療玩家
    /// </summary>
    public void HealPlayer()
    {
        if (playerController != null)
        {
            playerController.Heal(healAmount);
        }
    }
    
    /// <summary>
    /// 完全治療玩家
    /// </summary>
    public void FullHealPlayer()
    {
        if (playerController != null)
        {
            playerController.FullHeal();
        }
    }
    
    /// <summary>
    /// 殺死玩家
    /// </summary>
    public void KillPlayer()
    {
        if (playerController != null)
        {
            playerController.SetHealth(0);
        }
    }
    
    /// <summary>
    /// 復活玩家
    /// </summary>
    public void ResurrectPlayer()
    {
        if (playerController != null)
        {
            playerController.Resurrect();
        }
    }
    
    /// <summary>
    /// 增加最大血量
    /// </summary>
    public void IncreaseMaxHealth(int amount)
    {
        if (playerController != null)
        {
            playerController.IncreaseMaxHealth(amount);
        }
    }
    
    private void OnGUI()
    {
        if (playerController == null) return;
        
        // 顯示控制說明
        GUI.Box(new Rect(10, 10, 300, 150), "血量測試控制");
        GUI.Label(new Rect(20, 35, 280, 20), $"D - 造成 {damageAmount} 點傷害");
        GUI.Label(new Rect(20, 55, 280, 20), $"H - 治療 {healAmount} 點血量");
        GUI.Label(new Rect(20, 75, 280, 20), "F - 完全治療");
        GUI.Label(new Rect(20, 95, 280, 20), "K - 殺死玩家");
        GUI.Label(new Rect(20, 115, 280, 20), "R - 復活玩家");
        
        // 顯示當前血量
        GUI.Box(new Rect(10, 170, 300, 80), "當前狀態");
        GUI.Label(new Rect(20, 195, 280, 20), $"血量: {playerController.CurrentHealth}/{playerController.MaxHealth}");
        GUI.Label(new Rect(20, 215, 280, 20), $"血量百分比: {playerController.HealthPercentage:P0}");
        GUI.Label(new Rect(20, 235, 280, 20), $"是否死亡: {playerController.IsDead}");
    }
}
