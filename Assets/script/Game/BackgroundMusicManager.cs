using UnityEngine;

/// <summary>
/// Background Music Manager - Plays background music with volume control
/// Integrates with GameSettings for music volume
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] [Range(0f, 1f)] private float volumeMultiplier = 1f; // Additional volume control
    
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Configure audio source
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }
    
    private void Start()
    {
        // Apply volume settings
        UpdateVolume();
        
        // Start playing music
        if (musicClip != null)
        {
            audioSource.Play();
            Debug.Log($"[BackgroundMusicManager] Playing music: {musicClip.name}");
        }
        else
        {
            Debug.LogWarning("[BackgroundMusicManager] No music clip assigned!");
        }
    }
    
    private void Update()
    {
        // Continuously update volume to respond to settings changes
        UpdateVolume();
    }
    
    /// <summary>
    /// Update volume based on GameSettings
    /// </summary>
    private void UpdateVolume()
    {
        if (GameSettings.Instance != null)
        {
            // Combine master volume, music volume, and volume multiplier
            float finalVolume = GameSettings.Instance.MasterVolume * 
                               GameSettings.Instance.MusicVolume * 
                               volumeMultiplier;
            audioSource.volume = finalVolume;
        }
        else
        {
            // Fallback if GameSettings doesn't exist
            audioSource.volume = volumeMultiplier;
        }
    }
    
    /// <summary>
    /// Set the music clip and start playing
    /// </summary>
    public void SetMusic(AudioClip clip, float volume = 1f)
    {
        musicClip = clip;
        volumeMultiplier = volume;
        audioSource.clip = clip;
        UpdateVolume();
        audioSource.Play();
    }
    
    /// <summary>
    /// Stop the music
    /// </summary>
    public void StopMusic()
    {
        audioSource.Stop();
    }
    
    /// <summary>
    /// Pause the music
    /// </summary>
    public void PauseMusic()
    {
        audioSource.Pause();
    }
    
    /// <summary>
    /// Resume the music
    /// </summary>
    public void ResumeMusic()
    {
        audioSource.UnPause();
    }
}
