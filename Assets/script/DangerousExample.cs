using UnityEngine;

/// <summary>
/// 危險指數系統使用示例
/// 展示如何使用DangerousManager的功能
/// </summary>
/// 
public class DangerousExample : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private KeyCode increaseKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode decreaseKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode resetKey = KeyCode.R;
    [SerializeField] private int testAmount = 10;
    
    [Header("自動測試")]
    [SerializeField] private bool enableAutoTest = false;
    [SerializeField] private float autoTestInterval = 3f;
    [SerializeField] private int autoTestAmount = 5;
    
    private DangerousManager dangerousManager;
    private EnemyManager enemyManager;
    private PlayerController playerController;
    private float lastAutoTestTime;
    
    // 危險等級調整參數
    [Header("危險等級調整參數")]
    [SerializeField] private float[] enemyFovMultipliers = { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f }; // 對應5個危險等級
    [SerializeField] private float[] enemySpeedMultipliers = { 1.0f, 1.1f, 1.3f, 1.6f, 2.0f }; // 對應5個危險等級
    [SerializeField] private float[] enemyDamageReduction = { 0f, 0.1f, 0.2f, 0.3f, 0.5f }; // 對應5個危險等級的傷害減少
    
    private void Start()
    {
        // 獲取DangerousManager實例
        dangerousManager = DangerousManager.Instance;
        
        if (dangerousManager == null)
        {
            Debug.LogError("DangerousExample: 找不到DangerousManager實例！");
            return;
        }
        
        // 獲取EnemyManager和PlayerController
        enemyManager = FindFirstObjectByType<EnemyManager>();
        playerController = FindFirstObjectByType<PlayerController>();
        
        if (enemyManager == null)
        {
            Debug.LogWarning("DangerousExample: 找不到EnemyManager！");
        }
        
        if (playerController == null)
        {
            Debug.LogWarning("DangerousExample: 找不到PlayerController！");
        }
        
        // 訂閱危險指數事件
        dangerousManager.OnDangerLevelChanged += OnDangerLevelChanged;
        dangerousManager.OnDangerLevelTypeChanged += OnDangerLevelTypeChanged;
        dangerousManager.OnDangerLevelReachedMax += OnDangerLevelReachedMax;
        dangerousManager.OnDangerLevelReachedMin += OnDangerLevelReachedMin;
        
        // 顯示初始狀態
        LogDangerStatus();
    }
    
    private void Update()
    {
        // 手動控制
        if (Input.GetKeyDown(increaseKey))
        {
            IncreaseDanger();
        }
        
        if (Input.GetKeyDown(decreaseKey))
        {
            DecreaseDanger();
        }
        
        if (Input.GetKeyDown(resetKey))
        {
            ResetDanger();
        }
        
        // 自動測試
        if (enableAutoTest && Time.time >= lastAutoTestTime + autoTestInterval)
        {
            if (Random.Range(0, 2) == 0)
            {
                IncreaseDanger(autoTestAmount);
            }
            else
            {
                DecreaseDanger(autoTestAmount);
            }
            lastAutoTestTime = Time.time;
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (dangerousManager != null)
        {
            dangerousManager.OnDangerLevelChanged -= OnDangerLevelChanged;
            dangerousManager.OnDangerLevelTypeChanged -= OnDangerLevelTypeChanged;
            dangerousManager.OnDangerLevelReachedMax -= OnDangerLevelReachedMax;
            dangerousManager.OnDangerLevelReachedMin -= OnDangerLevelReachedMin;
        }
    }
    
    /// <summary>
    /// 增加危險指數
    /// </summary>
    public void IncreaseDanger(int amount = -1)
    {
        if (amount == -1) amount = testAmount;
        dangerousManager.IncreaseDangerLevel(amount, "Manual Test");
    }
    
    /// <summary>
    /// 減少危險指數
    /// </summary>
    public void DecreaseDanger(int amount = -1)
    {
        if (amount == -1) amount = testAmount;
        dangerousManager.DecreaseDangerLevel(amount, "Manual Test");
    }
    
    /// <summary>
    /// 重置危險指數
    /// </summary>
    public void ResetDanger()
    {
        dangerousManager.ResetDangerLevel();
        Debug.Log("危險指數已重置");
    }
    
    /// <summary>
    /// 設定危險指數
    /// </summary>
    public void SetDangerLevel(int level)
    {
        dangerousManager.SetDangerLevel(level, "Manual Set");
    }
    
    /// <summary>
    /// 處理危險指數變化事件
    /// </summary>
    private void OnDangerLevelChanged(int currentDanger, int maxDanger)
    {
        Debug.Log($"危險指數變化: {currentDanger}/{maxDanger} ({dangerousManager.DangerPercentage:P0})");
    }
    
    /// <summary>
    /// 處理危險等級變化事件
    /// </summary>
    private void OnDangerLevelTypeChanged(DangerousManager.DangerLevel level)
    {
        Debug.Log($"危險等級變化: {dangerousManager.GetDangerLevelDescription(level)}");
        
        // 根據危險等級調整遊戲參數
        AdjustGameParameters(level);
        
        // 根據危險等級執行不同的邏輯
        switch (level)
        {
            case DangerousManager.DangerLevel.Safe:
                Debug.Log("環境安全，可以放鬆警惕");
                break;
            case DangerousManager.DangerLevel.Low:
                Debug.Log("開始出現危險信號，需要小心");
                break;
            case DangerousManager.DangerLevel.Medium:
                Debug.Log("危險程度中等，建議提高警覺");
                break;
            case DangerousManager.DangerLevel.High:
                Debug.Log("高危險環境！請立即採取防護措施");
                break;
            case DangerousManager.DangerLevel.Critical:
                Debug.Log("極度危險！請立即撤離或尋求幫助！");
                break;
        }
    }
    
    /// <summary>
    /// 處理危險指數達到最大值事件
    /// </summary>
    private void OnDangerLevelReachedMax()
    {
        Debug.LogError("危險指數達到最大值！環境極度危險！");
        // 可以在這裡觸發遊戲結束、警告音效等
    }
    
    /// <summary>
    /// 處理危險指數達到最小值事件
    /// </summary>
    private void OnDangerLevelReachedMin()
    {
        Debug.Log("危險指數達到最小值，環境安全");
        // 可以在這裡觸發安全音效、恢復效果等
    }
    
    /// <summary>
    /// 記錄當前危險狀態
    /// </summary>
    public void LogDangerStatus()
    {
        if (dangerousManager == null) return;
        
        Debug.Log($"=== 危險指數狀態 ===");
        Debug.Log($"當前危險指數: {dangerousManager.CurrentDangerLevel}/{dangerousManager.MaxDangerLevel}");
        Debug.Log($"危險百分比: {dangerousManager.DangerPercentage:P0}");
        Debug.Log($"危險等級: {dangerousManager.GetDangerLevelDescription(dangerousManager.CurrentDangerLevelType)}");
        Debug.Log($"危險等級顏色: {dangerousManager.GetDangerLevelColor(dangerousManager.CurrentDangerLevelType)}");
    }
    
    /// <summary>
    /// 模擬敵人攻擊增加危險指數
    /// </summary>
    public void SimulateEnemyAttack()
    {
        int dangerIncrease = Random.Range(5, 15);
        dangerousManager.IncreaseDangerLevel(dangerIncrease, "Enemy Attack");
    }
    
    /// <summary>
    /// 模擬安全措施減少危險指數
    /// </summary>
    public void SimulateSafetyMeasure()
    {
        int dangerDecrease = Random.Range(3, 8);
        dangerousManager.DecreaseDangerLevel(dangerDecrease, "Safety Measure");
    }
    
    /// <summary>
    /// 模擬環境事件
    /// </summary>
    public void SimulateEnvironmentalEvent()
    {
        int randomChange = Random.Range(-10, 20);
        if (randomChange > 0)
        {
            dangerousManager.IncreaseDangerLevel(randomChange, "Environmental Hazard");
        }
        else
        {
            dangerousManager.DecreaseDangerLevel(-randomChange, "Environmental Improvement");
        }
    }
    
    /// <summary>
    /// 根據危險等級調整遊戲參數
    /// </summary>
    private void AdjustGameParameters(DangerousManager.DangerLevel level)
    {
        int levelIndex = (int)level;
        
        // 調整敵人參數
        if (enemyManager != null)
        {
            AdjustEnemyParameters(levelIndex);
        }
        
        // 調整玩家參數
        if (playerController != null)
        {
            AdjustPlayerParameters(levelIndex);
        }
    }
    
    /// <summary>
    /// 調整敵人參數
    /// </summary>
    private void AdjustEnemyParameters(int levelIndex)
    {
        // 確保索引在範圍內
        levelIndex = Mathf.Clamp(levelIndex, 0, enemyFovMultipliers.Length - 1);
        
        float fovMultiplier = enemyFovMultipliers[levelIndex];
        float speedMultiplier = enemySpeedMultipliers[levelIndex];
        float damageReduction = enemyDamageReduction[levelIndex];
        
        Debug.Log($"調整敵人參數 - 等級: {levelIndex}, FOV倍數: {fovMultiplier}, 速度倍數: {speedMultiplier}, 傷害減少: {damageReduction:P0}");
        
        // 調用EnemyManager的方法來調整所有敵人的參數
        if (enemyManager != null)
        {
            enemyManager.SetAllEnemiesFovMultiplier(fovMultiplier);
            enemyManager.SetAllEnemiesSpeedMultiplier(speedMultiplier);
            enemyManager.SetAllEnemiesDamageReduction(damageReduction);
            
            Debug.Log($"已調整所有敵人的FOV倍數為: {fovMultiplier}");
            Debug.Log($"已調整所有敵人的速度倍數為: {speedMultiplier}");
            Debug.Log($"已設定敵人傷害減少為: {damageReduction:P0}");
        }
    }
    
    /// <summary>
    /// 調整玩家參數
    /// </summary>
    private void AdjustPlayerParameters(int levelIndex)
    {
        // 根據危險等級調整玩家相關參數
        // 例如：移動速度、視野範圍、武器傷害等
        
        Debug.Log($"調整玩家參數 - 危險等級: {levelIndex}");
        
        // 這裡可以根據需要調整玩家的各種參數
        // 例如：
        // - 在高危險等級時降低玩家移動速度
        // - 調整玩家的視野範圍
        // - 修改武器傷害等
        
        if (playerController != null)
        {
            // 假設PlayerController有相關的調整方法
            // playerController.SetMovementSpeedMultiplier(speedMultiplier);
            // playerController.SetFovMultiplier(fovMultiplier);
            
            Debug.Log("已調整玩家參數");
        }
    }
}
