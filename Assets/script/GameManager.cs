using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GameManager - Central game management system
/// Responsibilities: 
/// - Game state management (menu, playing, paused, game over)
/// - Reference management for all major systems
/// - Game initialization and cleanup
/// - Score and statistics tracking
/// - Game pause/resume functionality
/// - Manager initialization coordination
/// </summary>
[DefaultExecutionOrder(150)] // 在 EntityManager (50) 和 Player (100) 之後執行
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        GameWin
    }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    
    [Header("System References")]
    [SerializeField] private EntityManager entityManager;
    
    [Header("Manager Initialization")]
    [Tooltip("自動協調管理器初始化順序")]
    [SerializeField] private bool autoInitializeManagers = true;
    [Tooltip("顯示初始化調試信息")]
    [SerializeField] private bool showInitializationDebug = false;
    
    // Target 記錄（由 GameManager 管理，不再由 Player 管理）
    private List<Target> activeTargets = new List<Target>();
    
    [Header("Game Statistics")]
    [SerializeField] private int enemiesKilled = 0;
    [SerializeField] private float gameTime = 0f;
    [SerializeField] private int currentWave = 0;
    private float bestTime = float.MaxValue; // 最快速通關時間（秒）
    
    [Header("Game Settings")]
    [SerializeField] private bool startPaused = false;
    [SerializeField] private float timescale = 1f;

    // Properties
    public bool IsPaused => Time.timeScale == 0f;
    public GameState CurrentState => currentState;

    // Events
    public delegate void GameStateChangeHandler(GameState oldState, GameState newState);
    public event GameStateChangeHandler OnGameStateChanged;

    public delegate void EnemyKilledHandler(int totalKilled);
    public event EnemyKilledHandler OnEnemyKilled;

    public delegate void WaveChangedHandler(int waveNumber);
    public event WaveChangedHandler OnWaveChanged;
    
    // 初始化階段事件
    public enum InitializationPhase
    {
        CoreSystems,      // EntityManager, Player
        GameSystems,      // DangerousManager, ItemManager
        UISystems         // 所有 UI Manager
    }
    
    public delegate void PhaseInitializedHandler(InitializationPhase phase);
    public event PhaseInitializedHandler OnPhaseInitialized;
    
    // Target 管理
    /// <summary>
    /// 註冊 Target（由 EntityManager 調用）
    /// </summary>
    public void RegisterTarget(Target target)
    {
        if (target != null && !activeTargets.Contains(target))
        {
            activeTargets.Add(target);
            if (showInitializationDebug)
                Debug.Log($"[GameManager] Target registered: {target.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 取消註冊 Target
    /// </summary>
    public void UnregisterTarget(Target target)
    {
        if (target != null && activeTargets.Remove(target))
        {
            if (showInitializationDebug)
                Debug.Log($"[GameManager] Target unregistered: {target.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 檢查是否所有 Target 都已死亡
    /// </summary>
    public bool AreAllTargetsDead()
    {
        if (activeTargets.Count == 0)
        {
            // 如果沒有註冊的 Target，嘗試從 EntityManager 獲取
            if (entityManager != null)
            {
                return entityManager.AreAllTargetsDead();
            }
            return false;
        }
        
        foreach (var target in activeTargets)
        {
            if (target != null && !target.IsDead)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 獲取活躍的 Target 數量
    /// </summary>
    public int ActiveTargetCount => activeTargets.Count;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (startPaused)
        {
            PauseGame();
        }
        
        // 在 GameScene 中自動初始化管理器
        if (currentState == GameState.Playing && autoInitializeManagers)
        {
            StartCoroutine(InitializeManagersSequentially());
        }
    }

    private void Update()
    {
        // Update game time only when playing
        if (currentState == GameState.Playing && !IsPaused)
        {
            gameTime += Time.deltaTime;
        }

        // Handle pause input (ESC key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[GameManager] Escape key pressed");
            if (currentState == GameState.Playing)
            {
                Debug.Log("[GameManager] Game is playing, pausing game");
                TogglePause();
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Initialize the GameManager
    /// </summary>
    private void Initialize()
    {
        //Debug.Log("[GameManager] Initializing...");
        
        // DON'T find references in Awake/Initialize - they don't exist yet in MainMenuScene
        // They will be found when the game scene loads via OnSceneLoaded
        
        // Load saved game settings
        LoadGameSettings();
        
        // Load best time from PlayerPrefs
        LoadBestTime();
    }

    /// <summary>
    /// Called when a scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log($"[GameManager] Scene loaded: {scene.name}");
        
        // Set game state based on scene
        if (scene.name == "MainMenuScene")
        {
            ChangeGameState(GameState.MainMenu);
            // Clear references since we're in menu (no gameplay managers here)
            ClearGameplayReferences();
        }
        else if (scene.name == "GameScene")
        {
            // Find references in the game scene
            RefreshSystemReferences();
            ChangeGameState(GameState.Playing);
            StartNewGame();
        }
    }

    /// <summary>
    /// 按順序初始化所有管理器（協調初始化流程）
    /// </summary>
    private IEnumerator InitializeManagersSequentially()
    {
        if (showInitializationDebug)
            Debug.Log("[GameManager] Starting manager initialization sequence...");
        
        // 階段 1: 核心系統（EntityManager 應該已經通過 DefaultExecutionOrder 初始化）
        yield return WaitForManager<EntityManager>();
        if (showInitializationDebug)
            Debug.Log("[GameManager] Phase 1: Core systems initialized");
        OnPhaseInitialized?.Invoke(InitializationPhase.CoreSystems);
        
        // 階段 2: 遊戲系統
        yield return WaitForManager<DangerousManager>();
        yield return WaitForManager<ItemManager>();
        if (showInitializationDebug)
            Debug.Log("[GameManager] Phase 2: Game systems initialized");
        OnPhaseInitialized?.Invoke(InitializationPhase.GameSystems);
        
        // 階段 3: UI 系統（通常已經通過事件系統初始化，但這裡確保順序）
        yield return WaitForManager<GameUIManager>();
        if (showInitializationDebug)
            Debug.Log("[GameManager] Phase 3: UI systems initialized");
        OnPhaseInitialized?.Invoke(InitializationPhase.UISystems);
        
        if (showInitializationDebug)
            Debug.Log("[GameManager] All managers initialized successfully");
    }
    
    /// <summary>
    /// 等待指定的 Manager 初始化完成
    /// </summary>
    private IEnumerator WaitForManager<T>() where T : MonoBehaviour
    {
        T manager = FindFirstObjectByType<T>();
        if (manager == null)
        {
            Debug.LogWarning($"[GameManager] {typeof(T).Name} not found in scene");
            yield break;
        }
        
        // 等待一幀，確保 Start() 已執行
        yield return null;
        
        if (showInitializationDebug)
            Debug.Log($"[GameManager] {typeof(T).Name} ready");
    }
    
    /// <summary>
    /// Refresh references to systems in the current scene
    /// </summary>
    private void RefreshSystemReferences()
    {
        entityManager = FindFirstObjectByType<EntityManager>();
        
        // Log warnings if manager is not found
        if (entityManager == null)
            Debug.LogWarning("[GameManager] EntityManager not found in scene!");
        else
        {
            Debug.Log("[GameManager] EntityManager found and registered");
            
            // 注意：玩家事件監聽已移至 WinConditionManager
            // GameManager 不再需要監聽玩家事件
        }
    }
    
    /// <summary>
    /// Clear gameplay references when leaving game scene
    /// </summary>
    private void ClearGameplayReferences()
    {
        // 清理 Target 列表
        activeTargets.Clear();
        
        entityManager = null;
        // Don't clear spawnPointManager if it uses DontDestroyOnLoad
    }
    
    // 勝利/失敗條件檢查已移至 WinConditionManager
    // GameManager 只負責狀態管理和事件通知

    /// <summary>
    /// Change the game state
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        if (currentState == newState)
            return;

        var oldState = currentState;
        currentState = newState;

        Debug.Log($"[GameManager] State changed: {oldState} -> {newState}");

        // Invoke event
        OnGameStateChanged?.Invoke(oldState, newState);

        // Handle state-specific logic
        HandleStateChange(oldState, newState);
    }

    /// <summary>
    /// Handle logic when state changes
    /// </summary>
    private void HandleStateChange(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;

            case GameState.Playing:
                Time.timeScale = timescale;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                HandleGameOver();
                break;

            case GameState.GameWin:
                Time.timeScale = 0f;
                HandleGameWin();
                break;
        }
    }

    /// <summary>
    /// Start a new game
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[GameManager] Starting new game...");
        
        // Reset statistics
        enemiesKilled = 0;
        gameTime = 0f;
        currentWave = 1;
        
        // Set time scale
        Time.timeScale = timescale;
        
        ChangeGameState(GameState.Playing);
        
        OnWaveChanged?.Invoke(currentWave);
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing)
            return;

        Debug.Log("[GameManager] Game paused");
        ChangeGameState(GameState.Paused);
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
            return;

        Debug.Log("[GameManager] Game resumed");
        ChangeGameState(GameState.Playing);
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Register an enemy kill
    /// </summary>
    public void RegisterEnemyKill()
    {
        enemiesKilled++;
        Debug.Log($"[GameManager] Enemy killed! Total: {enemiesKilled}");
        OnEnemyKilled?.Invoke(enemiesKilled);
    }


    /// <summary>
    /// Trigger game over
    /// </summary>
    public void GameOver(string reason = "Player died")
    {
        Debug.Log($"[GameManager] Game Over! Reason: {reason}");
        
        // Set the reason in GameOverUI
        GameOverUI gameOverUI = FindFirstObjectByType<GameOverUI>();
        if (gameOverUI != null)
        {
            gameOverUI.SetReason(reason);
        }
        
        ChangeGameState(GameState.GameOver);
    }
    
    /// <summary>
    /// Trigger game win (公開方法，供 WinConditionManager 調用)
    /// </summary>
    public void TriggerGameWin()
    {
        if (currentState == GameState.GameWin || currentState == GameState.GameOver)
        {
            return; // 已經結束，不重複觸發
        }
        
        Debug.LogWarning("[GameManager] 🎉 遊戲勝利！");
        GameWin();
    }
    
    /// <summary>
    /// Trigger game win (原方法，內部使用)
    /// </summary>
    public void GameWin()
    {
        Debug.Log("[GameManager] Game Win!");
        ChangeGameState(GameState.GameWin);
    }
    
    /// <summary>
    /// Handle game over logic
    /// </summary>
    private void HandleGameOver()
    {
        // 遊戲結束時的處理邏輯
        // 如果需要保存統計數據，可以在這裡添加
    }
    
    /// <summary>
    /// Handle game win logic
    /// </summary>
    private void HandleGameWin()
    {
        // Save best time
        SaveBestTime();
    }
    
    /// <summary>
    /// Save best completion time
    /// </summary>
    private void SaveBestTime()
    {
        // If current time is faster than best time, update it
        if (gameTime < bestTime)
        {
            bestTime = gameTime;
            PlayerPrefs.SetFloat("BestTime", bestTime);
            PlayerPrefs.Save();
            Debug.Log($"[GameManager] New record! Best time: {bestTime:F1} seconds");
        }
    }
    
    /// <summary>
    /// Load best time from PlayerPrefs
    /// </summary>
    private void LoadBestTime()
    {
        bestTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);
    }
    
    /// <summary>
    /// Get enemies killed count
    /// </summary>
    public int GetEnemiesKilled()
    {
        return enemiesKilled;
    }
    
    /// <summary>
    /// Get current game time
    /// </summary>
    public float GetGameTime()
    {
        return gameTime;
    }
    
    /// <summary>
    /// Get best completion time
    /// </summary>
    public float GetBestTime()
    {
        return bestTime;
    }

    /// <summary>
    /// Restart the current game
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[GameManager] Restarting game...");
        // Set time scale back to normal
        Time.timeScale = 1f;
        SceneLoader.Load(SceneLoader.Scene.GameScene);  // 使用正確的場景名稱
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[GameManager] Returning to main menu...");
        Time.timeScale = 1f;
        SceneLoader.Load(SceneLoader.Scene.MainMenuScene);
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game...");
        SaveGameSettings();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Load game settings from PlayerPrefs
    /// </summary>
    private void LoadGameSettings()
    {
        // Load any saved settings here
        // Example: timescale = PlayerPrefs.GetFloat("TimeScale", 1f);
    }

    /// <summary>
    /// Save game settings to PlayerPrefs
    /// </summary>
    private void SaveGameSettings()
    {
        // Save any settings here
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Save game statistics（已移除最高分保存功能）
    /// </summary>
    private void SaveGameStatistics()
    {
        // 如果需要保存其他統計數據，可以在這裡添加
        PlayerPrefs.Save();
    }
    

    /// <summary>
    /// 獲取當前波次
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWave;
    }

    /// <summary>
    /// Set time scale (for slow motion effects, etc.)
    /// </summary>
    public void SetTimeScale(float scale)
    {
        timescale = Mathf.Clamp(scale, 0.1f, 2f);
        if (currentState == GameState.Playing)
        {
            Time.timeScale = timescale;
        }
    }

    // 測試方法已移除，勝利/失敗條件檢查已移至 WinConditionManager

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveGameSettings();
        }
    }
}
