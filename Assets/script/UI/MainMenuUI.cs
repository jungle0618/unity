using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton = null;
    [SerializeField] private Button quitButton = null;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
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