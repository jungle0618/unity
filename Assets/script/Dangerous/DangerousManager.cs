using System;
using UnityEngine;

/// <summary>
/// 危險指數管理器
/// 負責管理遊戲中的危險指數，提供查詢、增加、減少等功能
/// </summary>
[DefaultExecutionOrder(200)] // 在 GameManager (150) 之後執行
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
    
    [Header("距離分段規則 (每秒變化量)")]
    [SerializeField] private float nearDistanceThreshold = 5f; // 5 格以內
    [SerializeField] private float midDistanceThreshold = 10f; // 5~10 格
    [SerializeField] private int nearIncreasePerSecond = 20;   // +20/秒
    [SerializeField] private int farDecreasePerSecond = 20;    // -20/秒 (10 up)
    
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
    
    // 來自敵人和target的知覺聚合（本幀）
    private bool anyEnemySeesPlayer = false; // 任何敵人看到玩家
    private bool anyTargetSeesPlayer = false; // 任何target看到玩家
    private float bestEnemyViewRangeMinusDistance = 0f; // 看到玩家的敵人中最優的 (視野半徑 - 距離) 值
    private float dangerFloatAccumulator = 0f; // 用於累積小數部分避免抖動
    
    // 追蹤連續看不到玩家的時間
    private float lastSeenTime = 0f; // 最後一次看到玩家的時間
    private const float NO_VISION_DECREASE_DELAY = 5f; // 連續5秒看不到玩家後開始減少
    private const int NO_VISION_DECREASE_RATE = 20; // 每秒減少20
    
    // 玩家引用（用於檢查武器和區域）
    private Player player;
    private ItemHolder playerItemHolder;
    
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
        
        // 初始化最後看到時間
        lastSeenTime = Time.time;
    }
    
    private void Start()
    {
        // 觸發初始危險等級事件
        OnDangerLevelTypeChanged?.Invoke(CurrentDangerLevelType);
        
        // 查找玩家引用
        FindPlayer();
    }
    
    /// <summary>
    /// 查找玩家引用
    /// </summary>
    private void FindPlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                playerItemHolder = player.GetComponent<ItemHolder>();
            }
        }
    }
    
    private void Update()
    {
        // 根據本幀敵人聚合輸入來更新危險係數
        ApplyAggregatedPerception(Time.deltaTime);
        
        // 重置聚合狀態，供下一幀使用
        anyEnemySeesPlayer = false;
        anyTargetSeesPlayer = false;
        bestEnemyViewRangeMinusDistance = 0f;
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
            //Debug.Log($"危險指數增加 {amount} 點 (來源: {source})，當前: {currentDangerLevel}/{maxDangerLevel}");
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
            //Debug.Log($"危險指數減少 {amount} 點 (來源: {source})，當前: {currentDangerLevel}/{maxDangerLevel}");
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
            //Debug.Log($"危險指數設定為 {currentDangerLevel} (來源: {source})");
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

    /// <summary>
    /// 由敵人偵測回報玩家距離、視野半徑與是否被看見（每幀可呼叫多次，系統會聚合）
    /// </summary>
    /// <param name="distanceToPlayer">敵人到玩家的距離</param>
    /// <param name="viewRange">敵人當前的視野半徑（最終值，不是base）</param>
    /// <param name="canSeePlayer">是否看到玩家</param>
    public void ReportEnemyPerception(float distanceToPlayer, float viewRange, bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            anyEnemySeesPlayer = true;
            // 計算視野半徑 - 距離，記錄最優的值（最大的）
            float viewRangeMinusDistance = viewRange - distanceToPlayer;
            if (viewRangeMinusDistance > bestEnemyViewRangeMinusDistance)
            {
                bestEnemyViewRangeMinusDistance = viewRangeMinusDistance;
            }
        }
    }
    
    /// <summary>
    /// 由target偵測回報是否看到玩家
    /// </summary>
    /// <param name="canSeePlayer">是否看到玩家</param>
    public void ReportTargetPerception(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            anyTargetSeesPlayer = true;
        }
    }
    
    /// <summary>
    /// 回報敵人是否實際看到玩家（不論是否應該增加危險值）
    /// 用於防止危險值在被追擊時下降（保留以維持向後兼容）
    /// </summary>
    [System.Obsolete("此方法已棄用，請使用 ReportEnemyPerception")]
    public void ReportEnemyActuallySeesPlayer(bool actuallySeesPlayer)
    {
        // 保留空實現以維持向後兼容
    }

    /// <summary>
    /// 根據聚合的敵人資訊，依規則更新危險係數。
    /// 新規則：
    /// 1. 當任何敵人跟target都沒看到player時，永遠不會增加
    /// 2. 當其中一個敵人看到player時，會根據enemy當前的視野半徑減掉player離enemy的距離來決定dangerous的增加速度(增加速度是每秒增加5*那個值)
    /// 3. target看到player時直接變成100
    /// 4. 如果連續5秒以上任何敵人跟target都沒看到player時，危險程度每秒-20
    /// </summary>
    private void ApplyAggregatedPerception(float deltaTime)
    {
        // 規則3：target看到player時直接變成100
        if (anyTargetSeesPlayer)
        {
            SetDangerLevel(maxDangerLevel, "Target sees player");
            lastSeenTime = Time.time; // 更新最後看到時間
            return;
        }
        
        // 規則2：當其中一個敵人看到player時，根據視野半徑和距離計算增加速度
        if (anyEnemySeesPlayer)
        {
            // 【新增】檢查玩家是否沒拿武器且不在警戒區
            // 如果滿足條件，永遠不會增加危險值
            if (ShouldPreventDangerIncrease())
            {
                // 玩家沒拿武器且不在警戒區，不增加危險值
                return;
            }
            
            lastSeenTime = Time.time; // 更新最後看到時間
            
            // 使用最優的 (視野半徑 - 距離) 值
            // 增加速度 = 每秒增加 5 * (視野半徑 - 距離)
            if (bestEnemyViewRangeMinusDistance > 0f)
            {
                float increaseRatePerSecond = 5f * bestEnemyViewRangeMinusDistance;
                
                dangerFloatAccumulator += increaseRatePerSecond * deltaTime;
                int deltaInt = 0;
                if (dangerFloatAccumulator >= 1f)
                {
                    deltaInt = Mathf.FloorToInt(dangerFloatAccumulator);
                    dangerFloatAccumulator -= deltaInt;
                }
                
                if (deltaInt > 0)
                {
                    IncreaseDangerLevel(deltaInt, "Enemy sees player (view range based)");
                }
            }
            // 如果視野半徑 - 距離 <= 0，不增加危險值（規則1）
            return;
        }
        
        // 規則1：當任何敵人跟target都沒看到player時，永遠不會增加
        // 規則4：如果連續5秒以上任何敵人跟target都沒看到player時，危險程度每秒-20
        float timeSinceLastSeen = Time.time - lastSeenTime;
        if (timeSinceLastSeen >= NO_VISION_DECREASE_DELAY)
        {
            // 連續5秒以上看不到玩家，每秒減少20
            dangerFloatAccumulator -= NO_VISION_DECREASE_RATE * deltaTime;
            int deltaInt = 0;
            if (dangerFloatAccumulator <= -1f)
            {
                deltaInt = Mathf.CeilToInt(dangerFloatAccumulator);
                dangerFloatAccumulator -= deltaInt;
            }
            
            if (deltaInt < 0)
            {
                DecreaseDangerLevel(-deltaInt, "No vision for 5+ seconds");
            }
        }
    }
    
    /// <summary>
    /// 檢查是否應該阻止危險值增加（玩家沒拿武器且不在警戒區）
    /// </summary>
    /// <returns>如果應該阻止增加，返回 true</returns>
    private bool ShouldPreventDangerIncrease()
    {
        // 如果找不到玩家，不阻止（向後兼容）
        if (player == null)
        {
            FindPlayer();
            if (player == null) return false;
        }
        
        // 檢查是否啟用 Guard Area System
        // 如果停用，不阻止（向後兼容）
        if (GameSettings.Instance != null && !GameSettings.Instance.UseGuardAreaSystem)
        {
            return false;
        }
        
        // 檢查玩家是否在警戒區
        Vector3 playerPosition = player.transform.position;
        bool isInGuardArea = false;
        if (AreaManager.Instance != null)
        {
            isInGuardArea = AreaManager.Instance.IsInGuardArea(playerPosition);
        }
        else
        {
            // 如果 AreaManager 不存在，默認為不在警戒區（向後兼容）
            isInGuardArea = false;
        }
        
        // 如果在警戒區，不阻止
        if (isInGuardArea)
        {
            return false;
        }
        
        // 在 Safe Area 中，檢查玩家是否持有武器
        if (playerItemHolder == null)
        {
            playerItemHolder = player.GetComponent<ItemHolder>();
        }
        
        if (playerItemHolder == null)
        {
            // 找不到 ItemHolder，不阻止（向後兼容）
            return false;
        }
        
        // 檢查玩家是否持有武器
        bool playerHasWeapon = playerItemHolder.IsCurrentItemWeapon;
        
        // 如果玩家沒拿武器且不在警戒區，阻止增加
        return !playerHasWeapon;
    }
}
