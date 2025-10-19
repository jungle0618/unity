using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneLoaderCallback - Handles the actual scene loading with progress tracking
/// Place this on a GameObject in the LoadingScene
/// </summary>
public class SceneLoaderCallback : MonoBehaviour
{
    private bool _isFirstUpdate = true;

    private void Start()
    {
        Debug.Log("[SceneLoaderCallback] _isFirstUpdate set to true in Start");
        _isFirstUpdate = true;
    }

    private void Update()
    {
        Debug.Log("[SceneLoaderCallback] Update called, isFirstUpdate: " + _isFirstUpdate);
        // Backup method in case Start doesn't work
        if (!_isFirstUpdate) return;
        _isFirstUpdate = false;
        Debug.Log("[SceneLoaderCallback] Update called - Starting as backup");
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        Debug.Log("[SceneLoaderCallback] LoadSceneAsync coroutine started");
        
        // Small delay to ensure LoadingProgressUI is initialized
        yield return null;
        
        string targetSceneName = SceneLoader.GetTargetSceneName();
        Debug.Log($"[SceneLoaderCallback] Starting async load of scene: {targetSceneName}");

        // Validate scene name
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SceneLoaderCallback] Target scene name is null or empty!");
            yield break;
        }

        // Start loading the scene asynchronously
        AsyncOperation asyncLoad = null;
        
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            Debug.Log($"[SceneLoaderCallback] AsyncOperation created for: {targetSceneName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SceneLoaderCallback] Exception loading scene: {e.Message}");
            yield break;
        }

        if (asyncLoad == null)
        {
            Debug.LogError($"[SceneLoaderCallback] Failed to load scene: {targetSceneName}. Make sure it's added to Build Settings!");
            yield break;
        }

        // Don't activate the scene immediately
        asyncLoad.allowSceneActivation = false;
        
        Debug.Log("[SceneLoaderCallback] Scene loading started, updating progress...");

        // Update progress while loading
        int frameCount = 0;
        while (!asyncLoad.isDone)
        {
            frameCount++;
            
            // AsyncOperation.progress goes from 0 to 0.9 during loading
            var progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (frameCount % 30 == 0) // Log every 30 frames to avoid spam
            {
                Debug.Log($"[SceneLoaderCallback] Progress: {progress * 100:F1}% (raw: {asyncLoad.progress}, frame: {frameCount})");
            }
            
            // Notify the loading progress UI
            if (LoadingProgressUI.Instance != null)
            {
                LoadingProgressUI.Instance.UpdateProgress(progress);
            }
            else if (frameCount == 1)
            {
                Debug.LogWarning("[SceneLoaderCallback] LoadingProgressUI.Instance is null!");
            }

            // When loading is almost done (0.9), wait a bit then activate
            if (asyncLoad.progress >= 0.9f)
            {
                Debug.Log("[SceneLoaderCallback] Loading reached 90%, finalizing...");
                
                // Optional: wait a moment to show 100% completion
                yield return new WaitForSeconds(0.5f);

                // Set progress to 100%
                if (LoadingProgressUI.Instance != null)
                {
                    LoadingProgressUI.Instance.UpdateProgress(1f);
                }
                
                // Small delay before scene activation
                yield return new WaitForSeconds(0.3f);
                Debug.Log("[SceneLoaderCallback] Activating scene now!");
                
                // Allow scene activation
                // --- OPTIONAL: Wait for key press ---
                // This is great for letting the player read a tip or
                // just be ready before starting the level.
                // if (Input.anyKeyDown)
                // {
                //     operation.allowSceneActivation = true;
                // }
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
        
        Debug.Log("[SceneLoaderCallback] Scene loading complete!");
    }
}
