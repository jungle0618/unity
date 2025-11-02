using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader {
    public enum Scene {
        MainMenuScene,
        GameScene,    // 遊戲場景名稱
        LoadingScene
    }

    private static Scene _targetScene = Scene.MainMenuScene;

    public static void Load(Scene targetScene) {
        _targetScene = targetScene;
        Debug.Log($"[SceneLoader] Loading scene: {targetScene} via LoadingScene");
        SceneManager.LoadScene(nameof(Scene.LoadingScene));
    }

    public static void LoaderCallback() {
        Debug.Log("[SceneLoader] LoaderCallback - Old method called");
        SceneManager.LoadScene(_targetScene.ToString());
        Debug.Log("Loaded scene: " + _targetScene.ToString());
    }
    
    /// <summary>
    /// Get the target scene name for async loading
    /// </summary>
    public static string GetTargetSceneName()
    {
        string sceneName = _targetScene.ToString();
        Debug.Log($"[SceneLoader] GetTargetSceneName returning: {sceneName}");
        return sceneName;
    }
    
    /// <summary>
    /// Get the current target scene enum
    /// </summary>
    public static Scene GetTargetScene()
    {
        return _targetScene;
    }
}