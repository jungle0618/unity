using UnityEngine;

/// <summary>
/// Game Settings - Persistent game settings that can be configured in menus
/// Settings are saved to PlayerPrefs and loaded at game start
/// </summary>
public class GameSettings : MonoBehaviour
{
    // Singleton instance
    public static GameSettings Instance { get; private set; }
    
    [Header("Player Settings")]
    [SerializeField] private bool runEnabled = false; // Running disabled by default
    
    // PlayerPrefs keys
    private const string KEY_RUN_ENABLED = "Settings_RunEnabled";
    
    // Properties
    public bool RunEnabled 
    { 
        get => runEnabled;
        set
        {
            runEnabled = value;
            SaveSettings();
        }
    }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
        
        LoadSettings();
    }
    
    /// <summary>
    /// Load settings from PlayerPrefs
    /// </summary>
    public void LoadSettings()
    {
        runEnabled = PlayerPrefs.GetInt(KEY_RUN_ENABLED, 0) == 1; // Default: false (0)
        Debug.Log($"[GameSettings] Loaded settings - Run Enabled: {runEnabled}");
    }
    
    /// <summary>
    /// Save settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetInt(KEY_RUN_ENABLED, runEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[GameSettings] Saved settings - Run Enabled: {runEnabled}");
    }
    
    /// <summary>
    /// Reset all settings to default values
    /// </summary>
    public void ResetToDefaults()
    {
        runEnabled = false; // Default: disabled
        SaveSettings();
        Debug.Log("[GameSettings] Settings reset to defaults");
    }
}

