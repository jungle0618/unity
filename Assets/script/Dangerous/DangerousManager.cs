using System;
using UnityEngine;

/// <summary>
/// 危險指數管理器
/// 負責管理遊戲中的危險指數，提供查詢、增加、減少等功能
/// </summary>
public class DangerousManager : MonoBehaviour
{
    [Header("危險指數設定")]
    [SerializeField] private int maxDangerLevel = 100;
    [SerializeField] private int currentDangerLevel = 0;
    [SerializeField] private int minDangerLevel = 0;
    
    [Header("危險等級設定")]
    [SerializeField] private int lowDangerThreshold = 30;
    [SerializeField] private int mediumDangerThreshold = 60;
    [SerializeField] private int highDangerThreshold = 80;
    
    [Header("自動減少設定")]
    [SerializeField] private bool enableAutoDecrease = true;
    [SerializeField] private float autoDecreaseInterval = 2f;
    [SerializeField] private int autoDecreaseAmount = 1;
    
    // 事件
    public event Action<int, int> OnDangerLevelChanged; // 當前危險指數, 最大危險指數
    public event Action<DangerLevel> OnDangerLevelTypeChanged; // 危險等級變化
    public event Action OnDangerLevelReachedMax; // 危險指數達到最大值
    public event Action OnDangerLevelReachedMin; // 危險指數達到最小值
    
    // 危險等級枚舉
    public enum DangerLevel
    {
        Safe,       // 安全 (0-30)
        Low,        // 低危險 (31-60)
        Medium,     // 中等危險 (61-80)
        High,       // 高危險 (81-100)
        Critical    // 極度危險 (100)
    }
    
    // 單例模式
    public static DangerousManager Instance { get; private set; }
    
    // 屬性
    public int CurrentDangerLevel => currentDangerLevel;
    public int MaxDangerLevel => maxDangerLevel;
    public int MinDangerLevel => minDangerLevel;
    public float DangerPercentage => maxDangerLevel > 0 ? (float)currentDangerLevel / maxDangerLevel : 0f;
    public DangerLevel CurrentDangerLevelType => GetDangerLevelType(currentDangerLevel);
    
    private float lastAutoDecreaseTime;
    
    private void Awake()
    {
        // 單例模式設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化危險指數
        currentDangerLevel = Mathf.Clamp(currentDangerLevel, minDangerLevel, maxDangerLevel);
    }
    
    private void Start()
    {
        // 觸發初始危險等級事件
        OnDangerLevelTypeChanged?.Invoke(CurrentDangerLevelType);
    }
    
    private void Update()
    {
        // 自動減少危險指數
        if (enableAutoDecrease && Time.time >= lastAutoDecreaseTime + autoDecreaseInterval)
        {
            DecreaseDangerLevel(autoDecreaseAmount);
            lastAutoDecreaseTime = Time.time;
        }
    }
    
    /// <summary>
    /// 增加危險指數
    /// </summary>
    /// <param name="amount">增加的數量</param>
    /// <param name="source">危險來源（可選，用於調試）</param>
    public void IncreaseDangerLevel(int amount, string source = "")
    {
        if (amount <= 0) return;
        
        DangerLevel oldLevel = CurrentDangerLevelType;
        int oldDangerLevel = currentDangerLevel;
        
        currentDangerLevel = Mathf.Min(maxDangerLevel, currentDangerLevel + amount);
        
        // 觸發事件
        OnDangerLevelChanged?.Invoke(currentDangerLevel, maxDangerLevel);
        
        // 檢查危險等級是否變化
        DangerLevel newLevel = CurrentDangerLevelType;
        if (oldLevel != newLevel)
        {
            OnDangerLevelTypeChanged?.Invoke(newLevel);
        }
        
        // 檢查是否達到最大值
        if (currentDangerLevel >= maxDangerLevel && oldDangerLevel < maxDangerLevel)
        {
            OnDangerLevelReachedMax?.Invoke();
        }
        
        // 調試信息
        if (!string.IsNullOrEmpty(source))
        {
            Debug.Log($"危險指數增加 {amount} 點 (來源: {source})，當前: {currentDangerLevel}/{maxDangerLevel}");
        }
    }
    
    /// <summary>
    /// 減少危險指數
    /// </summary>
    /// <param name="amount">減少的數量</param>
    /// <param name="source">減少來源（可選，用於調試）</param>
    public void DecreaseDangerLevel(int amount, string source = "")
    {
        if (amount <= 0) return;
        
        DangerLevel oldLevel = CurrentDangerLevelType;
        int oldDangerLevel = currentDangerLevel;
        
        currentDangerLevel = Mathf.Max(minDangerLevel, currentDangerLevel - amount);
        
        // 觸發事件
        OnDangerLevelChanged?.Invoke(currentDangerLevel, maxDangerLevel);
        
        // 檢查危險等級是否變化
        DangerLevel newLevel = CurrentDangerLevelType;
        if (oldLevel != newLevel)
        {
            OnDangerLevelTypeChanged?.Invoke(newLevel);
        }
        
        // 檢查是否達到最小值
        if (currentDangerLevel <= minDangerLevel && oldDangerLevel > minDangerLevel)
        {
            OnDangerLevelReachedMin?.Invoke();
        }
        
        // 調試信息
        if (!string.IsNullOrEmpty(source))
        {
            Debug.Log($"危險指數減少 {amount} 點 (來源: {source})，當前: {currentDangerLevel}/{maxDangerLevel}");
        }
    }
    
    /// <summary>
    /// 設定危險指數
    /// </summary>
    /// <param name="level">新的危險指數</param>
    /// <param name="source">設定來源（可選，用於調試）</param>
    public void SetDangerLevel(int level, string source = "")
    {
        DangerLevel oldLevel = CurrentDangerLevelType;
        int oldDangerLevel = currentDangerLevel;
        
        currentDangerLevel = Mathf.Clamp(level, minDangerLevel, maxDangerLevel);
        
        // 觸發事件
        OnDangerLevelChanged?.Invoke(currentDangerLevel, maxDangerLevel);
        
        // 檢查危險等級是否變化
        DangerLevel newLevel = CurrentDangerLevelType;
        if (oldLevel != newLevel)
        {
            OnDangerLevelTypeChanged?.Invoke(newLevel);
        }
        
        // 檢查極值
        if (currentDangerLevel >= maxDangerLevel && oldDangerLevel < maxDangerLevel)
        {
            OnDangerLevelReachedMax?.Invoke();
        }
        else if (currentDangerLevel <= minDangerLevel && oldDangerLevel > minDangerLevel)
        {
            OnDangerLevelReachedMin?.Invoke();
        }
        
        // 調試信息
        if (!string.IsNullOrEmpty(source))
        {
            Debug.Log($"危險指數設定為 {currentDangerLevel} (來源: {source})");
        }
    }
    
    /// <summary>
    /// 重置危險指數到最小值
    /// </summary>
    public void ResetDangerLevel()
    {
        SetDangerLevel(minDangerLevel, "Reset");
    }
    
    /// <summary>
    /// 根據危險指數獲取危險等級
    /// </summary>
    /// <param name="dangerLevel">危險指數</param>
    /// <returns>危險等級</returns>
    public DangerLevel GetDangerLevelType(int dangerLevel)
    {
        if (dangerLevel >= maxDangerLevel)
            return DangerLevel.Critical;
        else if (dangerLevel >= highDangerThreshold)
            return DangerLevel.High;
        else if (dangerLevel >= mediumDangerThreshold)
            return DangerLevel.Medium;
        else if (dangerLevel >= lowDangerThreshold)
            return DangerLevel.Low;
        else
            return DangerLevel.Safe;
    }
    
    /// <summary>
    /// 獲取危險等級的顏色
    /// </summary>
    /// <param name="level">危險等級</param>
    /// <returns>對應的顏色</returns>
    public Color GetDangerLevelColor(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Safe:
                return Color.green;
            case DangerLevel.Low:
                return Color.yellow;
            case DangerLevel.Medium:
                return new Color(1f, 0.5f, 0f); // 橙色
            case DangerLevel.High:
                return Color.red;
            case DangerLevel.Critical:
                return new Color(0.5f, 0f, 0.5f); // 紫色
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// 獲取危險等級的描述文字
    /// </summary>
    /// <param name="level">危險等級</param>
    /// <returns>描述文字</returns>
    public string GetDangerLevelDescription(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Safe:
                return "安全";
            case DangerLevel.Low:
                return "低危險";
            case DangerLevel.Medium:
                return "中等危險";
            case DangerLevel.High:
                return "高危險";
            case DangerLevel.Critical:
                return "極度危險";
            default:
                return "未知";
        }
    }
    
    /// <summary>
    /// 啟用或停用自動減少功能
    /// </summary>
    /// <param name="enable">是否啟用</param>
    public void SetAutoDecreaseEnabled(bool enable)
    {
        enableAutoDecrease = enable;
        if (enable)
        {
            lastAutoDecreaseTime = Time.time;
        }
    }
    
    /// <summary>
    /// 設定自動減少參數
    /// </summary>
    /// <param name="interval">減少間隔時間</param>
    /// <param name="amount">每次減少的數量</param>
    public void SetAutoDecreaseSettings(float interval, int amount)
    {
        autoDecreaseInterval = Mathf.Max(0.1f, interval);
        autoDecreaseAmount = Mathf.Max(1, amount);
    }
}
