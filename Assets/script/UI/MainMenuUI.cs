using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton = null;
    [SerializeField] private Button quitButton = null;
    [SerializeField] private Button controlsButton = null;
    
    [Header("Controls UI")]
    [SerializeField] private ControlsUI controlsUI = null;
    [SerializeField] private bool autoFindControlsUI = true;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        
        // 設定控制說明按鈕
        if (controlsButton != null)
        {
            controlsButton.onClick.AddListener(OnControlsButtonClicked);
        }
        
        // 自動尋找 ControlsUI
        if (autoFindControlsUI && controlsUI == null)
        {
            controlsUI = FindFirstObjectByType<ControlsUI>();
        }
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("[MainMenuUI] Start Button Clicked, loading GameScene...");
        SceneLoader.Load(SceneLoader.Scene.GameScene);
    }

    private void OnQuitButtonClicked()
    {
        Debug.Log("[MainMenuUI] Quit Button Clicked, quitting game...");
        QuitGame();
    }
    
    private void OnControlsButtonClicked()
    {
        Debug.Log("[MainMenuUI] Controls Button Clicked, showing controls...");
        if (controlsUI != null)
        {
            controlsUI.Show();
        }
        else
        {
            Debug.LogWarning("[MainMenuUI] ControlsUI not found! Please assign it in the Inspector or ensure it exists in the scene.");
        }
    }

    private void QuitGame()
    {
        #if UNITY_EDITOR
        // If running in Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("[MainMenuUI] Exiting Play Mode (Editor)");
        #else
        // If running as a build
        Application.Quit();
        Debug.Log("[MainMenuUI] Quitting Application");
        #endif
    }
}