using UnityEngine;

/// <summary>
/// 通用血量管理组件
/// 可以被 Player, Enemy, Target 等所有 Entity 使用
/// </summary>
public class EntityHealth : MonoBehaviour
{
    [Header("血量設定")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    
    [Header("傷害設定")]
    [Tooltip("傷害減少比例（0-1，僅 Enemy 和 Target 使用）")]
    [SerializeField] private float damageReduction = 0f;
    
    [Header("無敵時間設定（僅 Player 使用）")]
    [SerializeField] private float invulnerabilityTime = 0f;
    private float lastDamageTime = -999f;
    
    // 事件
    public event System.Action<int, int> OnHealthChanged; // 當前血量, 最大血量
    public event System.Action OnEntityDied; // 實體死亡事件
    
    // 屬性
    public int MaxHealth 
    { 
        get => maxHealth; 
        set => maxHealth = value; 
    }
    
    public int CurrentHealth 
    { 
        get => currentHealth; 
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth); 
    }
    
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvulnerable => invulnerabilityTime > 0f && Time.time < lastDamageTime + invulnerabilityTime;
    public float DamageReduction => damageReduction;
    
    private void Awake()
    {
        // 初始化血量
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// 受到傷害
    /// </summary>
    /// <param name="damage">傷害值</param>
    /// <param name="source">傷害來源</param>
    /// <param name="entityName">實體名稱（用於日誌）</param>
    /// <returns>是否造成傷害（如果無敵則返回 false）</returns>
    public bool TakeDamage(int damage, string source = "", string entityName = "")
    {
        if (IsDead || IsInvulnerable) return false;
        
        // 應用傷害減少
        float actualDamage = damage * (1f - damageReduction);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(actualDamage)); // 至少造成1點傷害
        
        // 扣除生命值
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // 記錄傷害時間（用於無敵時間）
        lastDamageTime = Time.time;
        
        // 調試信息
        if (!string.IsNullOrEmpty(source))
        {
            string name = string.IsNullOrEmpty(entityName) ? gameObject.name : entityName;
            if (damageReduction > 0f)
            {
                Debug.Log($"{name} 受到 {damage} 點傷害 (減少 {damageReduction:P0}，實際 {finalDamage} 點) (來源: {source})，剩餘生命值: {currentHealth}/{maxHealth}");
            }
            else
            {
                Debug.Log($"{name} 受到 {damage} 點傷害 (來源: {source})，剩餘生命值: {currentHealth}/{maxHealth}");
            }
        }
        
        // 觸發血量變化事件
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // 生命值歸零時觸發死亡事件
        if (currentHealth <= 0)
        {
            OnEntityDied?.Invoke();
        }
        
        return true;
    }
    
    /// <summary>
    /// 治療
    /// </summary>
    /// <param name="healAmount">治療量</param>
    /// <returns>實際治療量</returns>
    public int Heal(int healAmount)
    {
        if (IsDead || healAmount <= 0) return 0;
        
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        int actualHeal = currentHealth - oldHealth;
        
        // 觸發血量變化事件
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"{gameObject.name} 治療 {actualHeal} 點血量，當前血量: {currentHealth}/{maxHealth}");
        
        return actualHeal;
    }
    
    /// <summary>
    /// 完全治療
    /// </summary>
    public void FullHeal()
    {
        if (IsDead) return;
        
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"{gameObject.name} 完全治療，當前血量: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 設定血量
    /// </summary>
    /// <param name="health">新的血量值</param>
    public void SetHealth(int health)
    {
        if (IsDead) return;
        
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // 檢查是否死亡
        if (currentHealth <= 0)
        {
            OnEntityDied?.Invoke();
        }
    }
    
    /// <summary>
    /// 增加最大血量
    /// </summary>
    /// <param name="amount">增加的量</param>
    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0) return;
        
        maxHealth += amount;
        currentHealth += amount; // 同時增加當前血量
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"{gameObject.name} 最大血量增加 {amount}，當前血量: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 設定傷害減少
    /// </summary>
    /// <param name="reduction">傷害減少比例（0-1）</param>
    public void SetDamageReduction(float reduction)
    {
        damageReduction = Mathf.Clamp01(reduction);
    }
    
    /// <summary>
    /// 設定無敵時間
    /// </summary>
    /// <param name="time">無敵時間（秒）</param>
    public void SetInvulnerabilityTime(float time)
    {
        invulnerabilityTime = Mathf.Max(0f, time);
    }
    
    /// <summary>
    /// 重置無敵時間（用於復活等情況）
    /// </summary>
    public void ResetInvulnerabilityTime()
    {
        lastDamageTime = -999f;
    }
    
    /// <summary>
    /// 初始化血量（通常在 Awake 或初始化時調用）
    /// </summary>
    public void InitializeHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}

