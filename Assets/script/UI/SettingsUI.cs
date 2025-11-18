using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Settings UI - User interface for game settings
/// Can be used in main menu or pause menu
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;
    
    [Header("Player Settings")]
    [SerializeField] private Toggle runToggle;
    
    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    
    [Header("Graphics Settings")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown fpsDropdown;
    
    [Header("Gameplay Settings")]
    [SerializeField] private Toggle damageNumbersToggle;
    [SerializeField] private Toggle minimapToggle;
    
    [Header("Buttons")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;
    
    private void Start()
    {
        // Setup button listeners
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
        
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClicked);
        
        // Setup control listeners
        SetupControlListeners();
        
        // Initialize UI with current settings
        LoadCurrentSettings();
        
        // Hide panel by default
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
    
    private void SetupControlListeners()
    {
        // Player settings
        if (runToggle != null)
            runToggle.onValueChanged.AddListener(OnRunToggleChanged);
        
        // Audio settings
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        
        // Graphics settings
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        
        if (fpsDropdown != null)
        {
            fpsDropdown.ClearOptions();
            fpsDropdown.AddOptions(new System.Collections.Generic.List<string> { "30 FPS", "60 FPS", "120 FPS", "Unlimited" });
            fpsDropdown.onValueChanged.AddListener(OnFPSDropdownChanged);
        }
        
        // Gameplay settings
        if (damageNumbersToggle != null)
            damageNumbersToggle.onValueChanged.AddListener(OnDamageNumbersToggleChanged);
        
        if (minimapToggle != null)
            minimapToggle.onValueChanged.AddListener(OnMinimapToggleChanged);
    }
    
    /// <summary>
    /// Load current settings from GameSettings and update UI
    /// </summary>
    private void LoadCurrentSettings()
    {
        if (GameSettings.Instance == null) return;
        
        // Player settings
        if (runToggle != null)
            runToggle.isOn = GameSettings.Instance.RunEnabled;
        
        // Audio settings
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = GameSettings.Instance.MasterVolume;
            UpdateVolumeText(masterVolumeText, GameSettings.Instance.MasterVolume);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = GameSettings.Instance.MusicVolume;
            UpdateVolumeText(musicVolumeText, GameSettings.Instance.MusicVolume);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = GameSettings.Instance.SFXVolume;
            UpdateVolumeText(sfxVolumeText, GameSettings.Instance.SFXVolume);
        }
        
        // Graphics settings
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = GameSettings.Instance.Fullscreen;
        
        if (fpsDropdown != null)
        {
            int fps = GameSettings.Instance.TargetFrameRate;
            int index = fps switch
            {
                30 => 0,
                60 => 1,
                120 => 2,
                _ => 3
            };
            fpsDropdown.value = index;
        }
        
        // Gameplay settings
        if (damageNumbersToggle != null)
            damageNumbersToggle.isOn = GameSettings.Instance.ShowDamageNumbers;
        
        if (minimapToggle != null)
            minimapToggle.isOn = GameSettings.Instance.ShowMinimap;
    }
    
    private void UpdateVolumeText(TextMeshProUGUI text, float volume)
    {
        if (text != null)
            text.text = $"{Mathf.RoundToInt(volume * 100)}%";
    }
    
    // Event handlers
    private void OnRunToggleChanged(bool value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.RunEnabled = value;
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.MasterVolume = value;
        UpdateVolumeText(masterVolumeText, value);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.MusicVolume = value;
        UpdateVolumeText(musicVolumeText, value);
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.SFXVolume = value;
        UpdateVolumeText(sfxVolumeText, value);
    }
    
    private void OnFullscreenToggleChanged(bool value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.Fullscreen = value;
    }
    
    private void OnFPSDropdownChanged(int index)
    {
        if (GameSettings.Instance == null) return;
        
        int fps = index switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            _ => -1 // Unlimited
        };
        
        GameSettings.Instance.TargetFrameRate = fps;
    }
    
    private void OnDamageNumbersToggleChanged(bool value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.ShowDamageNumbers = value;
    }
    
    private void OnMinimapToggleChanged(bool value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.ShowMinimap = value;
    }
    
    private void OnResetClicked()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ResetToDefaults();
            LoadCurrentSettings(); // Refresh UI
            Debug.Log("[SettingsUI] Settings reset to defaults");
        }
    }
    
    private void OnApplyClicked()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SaveSettings();
            Debug.Log("[SettingsUI] Settings applied and saved");
        }
        Hide();
    }
    
    /// <summary>
    /// Show settings panel
    /// </summary>
    public void Show()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            LoadCurrentSettings(); // Refresh settings when showing
        }
    }
    
    /// <summary>
    /// Hide settings panel
    /// </summary>
    public void Hide()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
    
    /// <summary>
    /// Toggle settings panel visibility
    /// </summary>
    public void Toggle()
    {
        if (settingsPanel != null)
        {
            if (settingsPanel.activeSelf)
                Hide();
            else
                Show();
        }
    }
}

