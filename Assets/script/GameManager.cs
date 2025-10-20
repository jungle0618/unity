using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager - Central game management system
/// Responsibilities: 
/// - Game state management (menu, playing, paused, game over)
/// - Reference management for all major systems
/// - Game initialization and cleanup
/// - Score and statistics tracking
/// - Game pause/resume functionality
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    
    [Header("System References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private EnemyManager enemyManager;
    
    [Header("Game Statistics")]
    [SerializeField] private int enemiesKilled = 0;
    [SerializeField] private float gameTime = 0f;
    [SerializeField] private int currentWave = 0;
    
    [Header("Game Settings")]
    [SerializeField] private bool startPaused = false;
    [SerializeField] private float timescale = 1f;

    // Properties
    public bool IsPaused => Time.timeScale == 0f;

    // Events
    public delegate void GameStateChangeHandler(GameState oldState, GameState newState);
    public event GameStateChangeHandler OnGameStateChanged;

    public delegate void EnemyKilledHandler(int totalKilled);
    public event EnemyKilledHandler OnEnemyKilled;

    public delegate void WaveChangedHandler(int waveNumber);
    public event WaveChangedHandler OnWaveChanged;

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
        Debug.Log("[GameManager] Initializing...");
        
        // DON'T find references in Awake/Initialize - they don't exist yet in MainMenuScene
        // They will be found when the game scene loads via OnSceneLoaded
        
        // Load saved game settings
        LoadGameSettings();
    }

    /// <summary>
    /// Called when a scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}");
        
        // Set game state based on scene
        if (scene.name == "MainMenuScene")
        {
            ChangeGameState(GameState.MainMenu);
            // Clear references since we're in menu (no gameplay managers here)
            ClearGameplayReferences();
        }
        else if (scene.name == "GameScene" || scene.name == "SampleScene")
        {
            // Find references in the game scene
            RefreshSystemReferences();
            ChangeGameState(GameState.Playing);
            StartNewGame();
        }
    }

    /// <summary>
    /// Refresh references to systems in the current scene
    /// </summary>
    private void RefreshSystemReferences()
    {
        playerManager = FindFirstObjectByType<PlayerManager>();
        enemyManager = FindFirstObjectByType<EnemyManager>();
        
        // Log warnings if managers are not found
        if (playerManager == null)
            Debug.LogWarning("[GameManager] PlayerManager not found in scene!");
        else
            Debug.Log("[GameManager] PlayerManager found and registered");
            
        if (enemyManager == null)
            Debug.LogWarning("[GameManager] EnemyManager not found in scene!");
        else
            Debug.Log("[GameManager] EnemyManager found and registered");
    }
    
    /// <summary>
    /// Clear gameplay references when leaving game scene
    /// </summary>
    private void ClearGameplayReferences()
    {
        playerManager = null;
        enemyManager = null;
        // Don't clear spawnPointManager if it uses DontDestroyOnLoad
    }

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
    public void GameOver()
    {
        Debug.Log("[GameManager] Game Over!");
        ChangeGameState(GameState.GameOver);
    }

    /// <summary>
    /// Handle game over logic
    /// </summary>
    private void HandleGameOver()
    {
        // Save high scores or statistics here
        SaveGameStatistics();
    }

    /// <summary>
    /// Restart the current game
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[GameManager] Restarting game...");
        // Set time scale back to normal
        Time.timeScale = 1f;
        SceneLoader.Load(SceneLoader.Scene.SampleScene);  // Changed from GameScene to SampleScene
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
    /// Save game statistics
    /// </summary>
    private void SaveGameStatistics()
    {
        // Save high scores or statistics
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (enemiesKilled > highScore)
        {
            PlayerPrefs.SetInt("HighScore", enemiesKilled);
            Debug.Log($"[GameManager] New high score: {enemiesKilled}");
        }
        
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get the high score
    /// </summary>
    public int GetHighScore()
    {
        return PlayerPrefs.GetInt("HighScore", 0);
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveGameSettings();
        }
    }
}
