﻿using UnityEngine;

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
    
    [Header("Audio Settings")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;
    
    [Header("Graphics Settings")]
    [SerializeField] private bool fullscreen = true;
    [SerializeField] private int targetFrameRate = 60;
    
    [Header("Gameplay Settings")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private bool showMinimap = true;
    [SerializeField] private bool useGuardAreaSystem = true; // Enable guard/safe area system by default
    
    // PlayerPrefs keys
    private const string KEY_RUN_ENABLED = "Settings_RunEnabled";
    private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
    private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
    private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
    private const string KEY_FULLSCREEN = "Settings_Fullscreen";
    private const string KEY_TARGET_FPS = "Settings_TargetFPS";
    private const string KEY_DAMAGE_NUMBERS = "Settings_DamageNumbers";
    private const string KEY_MINIMAP = "Settings_Minimap";
    private const string KEY_GUARD_AREA_SYSTEM = "Settings_GuardAreaSystem";
    
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
    
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
            SaveSettings();
        }
    }
    
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
            SaveSettings();
        }
    }
    
    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
            SaveSettings();
        }
    }
    
    public bool Fullscreen
    {
        get => fullscreen;
        set
        {
            fullscreen = value;
            Screen.fullScreen = fullscreen;
            SaveSettings();
        }
    }
    
    public int TargetFrameRate
    {
        get => targetFrameRate;
        set
        {
            targetFrameRate = Mathf.Clamp(value, 30, 144);
            Application.targetFrameRate = targetFrameRate;
            SaveSettings();
        }
    }
    
    public bool ShowDamageNumbers
    {
        get => showDamageNumbers;
        set
        {
            showDamageNumbers = value;
            SaveSettings();
        }
    }
    
    public bool ShowMinimap
    {
        get => showMinimap;
        set
        {
            showMinimap = value;
            SaveSettings();
        }
    }
    
    public bool UseGuardAreaSystem
    {
        get => useGuardAreaSystem;
        set
        {
            useGuardAreaSystem = value;
            SaveSettings();
            Debug.Log($"[GameSettings] Guard Area System: {(useGuardAreaSystem ? "Enabled" : "Disabled")}");
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
        runEnabled = PlayerPrefs.GetInt(KEY_RUN_ENABLED, 0) == 1; // Default: false
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 0.8f);
        musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 0.8f);
        fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1; // Default: true
        targetFrameRate = PlayerPrefs.GetInt(KEY_TARGET_FPS, 60);
        showDamageNumbers = PlayerPrefs.GetInt(KEY_DAMAGE_NUMBERS, 1) == 1; // Default: true
        showMinimap = PlayerPrefs.GetInt(KEY_MINIMAP, 1) == 1; // Default: true
        useGuardAreaSystem = PlayerPrefs.GetInt(KEY_GUARD_AREA_SYSTEM, 1) == 1; // Default: true
        
        // Apply loaded settings
        ApplyAllSettings();
        
        Debug.Log($"[GameSettings] Loaded settings - Run: {runEnabled}, Master Vol: {masterVolume}, Fullscreen: {fullscreen}, Guard Area System: {useGuardAreaSystem}");
    }
    
    /// <summary>
    /// Save settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetInt(KEY_RUN_ENABLED, runEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, masterVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, musicVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KEY_TARGET_FPS, targetFrameRate);
        PlayerPrefs.SetInt(KEY_DAMAGE_NUMBERS, showDamageNumbers ? 1 : 0);
        PlayerPrefs.SetInt(KEY_MINIMAP, showMinimap ? 1 : 0);
        PlayerPrefs.SetInt(KEY_GUARD_AREA_SYSTEM, useGuardAreaSystem ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"[GameSettings] Saved settings");
    }
    
    /// <summary>
    /// Reset all settings to default values
    /// </summary>
    public void ResetToDefaults()
    {
        runEnabled = false;
        masterVolume = 0.8f;
        musicVolume = 0.7f;
        sfxVolume = 0.8f;
        fullscreen = true;
        targetFrameRate = 60;
        showDamageNumbers = true;
        showMinimap = true;
        useGuardAreaSystem = true; // Default: enabled
        
        ApplyAllSettings();
        SaveSettings();
        
        Debug.Log("[GameSettings] Settings reset to defaults");
    }
    
    /// <summary>
    /// Apply all settings immediately
    /// </summary>
    private void ApplyAllSettings()
    {
        ApplyAudioSettings();
        Screen.fullScreen = fullscreen;
        Application.targetFrameRate = targetFrameRate;
    }
    
    /// <summary>
    /// Apply audio settings to AudioListener
    /// </summary>
    private void ApplyAudioSettings()
    {
        AudioListener.volume = masterVolume;
        // Note: Individual music/sfx volume would need AudioMixer setup
        // For now, we just apply master volume
    }
}

