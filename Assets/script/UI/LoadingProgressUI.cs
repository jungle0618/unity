using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LoadingProgressUI - Manages the loading progress slider and text
/// Place this on a GameObject in the LoadingScene
/// </summary>
public class LoadingProgressUI : MonoBehaviour
{
    public static LoadingProgressUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Settings")]
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private float smoothSpeed = 5f;

    private float _targetProgress;
    private float _currentProgress;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize slider
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }

        // Initialize text
        UpdateProgressText(0f);
    }

    private void Update()
    {
        // Smoothly interpolate progress
        if (_currentProgress < _targetProgress)
        {
            // Debug.Log(_currentProgress);
            _currentProgress = Mathf.MoveTowards(_currentProgress, _targetProgress, Time.deltaTime * smoothSpeed);
            
            // Update UI
            if (progressSlider)
            {
                progressSlider.value = _currentProgress;
            }

            UpdateProgressText(_currentProgress);
        }
    }

    /// <summary>
    /// Update the loading progress (0 to 1)
    /// </summary>
    public void UpdateProgress(float progress)
    {
        _targetProgress = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Update the progress text display
    /// </summary>
    private void UpdateProgressText(float progress)
    {
        if (progressText == null || !showPercentage) return;
        var percentage = Mathf.RoundToInt(progress * 100f);
        progressText.text = $"Loading... {percentage}%";
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

