using UnityEngine;

/// <summary>
/// PauseUI 設定輔助工具
/// 用於驗證和自動設定 PauseUI 的整合
/// </summary>
public class PauseUISetupHelper : MonoBehaviour
{
    [ContextMenu("驗證 PauseUI 設定")]
    public void VerifyPauseUISetup()
    {
        Debug.Log("=== PauseUI 設定驗證 ===");
        
        // 1. 檢查 GameUIManager
        GameUIManager gameUIManager = FindFirstObjectByType<GameUIManager>();
        if (gameUIManager == null)
        {
            Debug.LogError("❌ GameUIManager 不存在！請確認 Canvas 上有 GameUIManager 組件。");
            return;
        }
        Debug.Log("✅ GameUIManager 已找到");
        
        // 2. 檢查 PauseUIManager 是否存在
        PauseUIManager pauseUIManager = FindFirstObjectByType<PauseUIManager>();
        if (pauseUIManager == null)
        {
            Debug.LogError("❌ PauseUIManager 不存在！請確認 PausePanel 上有 PauseUIManager 組件。");
            return;
        }
        Debug.Log($"✅ PauseUIManager 已找到: {pauseUIManager.gameObject.name}");
        
        // 3. 檢查是否已設定到 GameUIManager
        PauseUIManager setManager = gameUIManager.GetPauseUIManager();
        if (setManager == null)
        {
            Debug.LogWarning("⚠️ PauseUIManager 未設定到 GameUIManager！");
            Debug.LogWarning("   正在嘗試自動設定...");
            AutoSetupPauseUI(gameUIManager, pauseUIManager);
        }
        else
        {
            Debug.Log("✅ PauseUIManager 已設定到 GameUIManager");
            if (setManager == pauseUIManager)
            {
                Debug.Log("✅ 設定正確！");
            }
            else
            {
                Debug.LogWarning("⚠️ 設定的 PauseUIManager 與找到的不同");
            }
        }
        
        // 4. 檢查 PauseMenuUI
        PauseMenuUI pauseMenu = pauseUIManager.GetPauseMenuUI();
        if (pauseMenu == null)
        {
            Debug.LogWarning("⚠️ PauseMenuUI 未設定到 PauseUIManager");
        }
        else
        {
            Debug.Log($"✅ PauseMenuUI 已設定: {pauseMenu.gameObject.name}");
        }
        
        Debug.Log("=== 驗證完成 ===");
    }
    
    [ContextMenu("自動設定 PauseUI")]
    public void AutoSetupPauseUI()
    {
        GameUIManager gameUIManager = FindFirstObjectByType<GameUIManager>();
        PauseUIManager pauseUIManager = FindFirstObjectByType<PauseUIManager>();
        
        if (gameUIManager == null || pauseUIManager == null)
        {
            Debug.LogError("無法自動設定：找不到 GameUIManager 或 PauseUIManager");
            return;
        }
        
        AutoSetupPauseUI(gameUIManager, pauseUIManager);
    }
    
    private void AutoSetupPauseUI(GameUIManager gameUIManager, PauseUIManager pauseUIManager)
    {
        // 使用反射設定私有欄位
        var field = typeof(GameUIManager).GetField("pauseUIManager", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(gameUIManager, pauseUIManager);
            Debug.Log("✅ 已自動設定 PauseUIManager 到 GameUIManager！");
            
            // 重新初始化
            if (pauseUIManager != null)
            {
                pauseUIManager.Initialize();
            }
        }
        else
        {
            Debug.LogError("❌ 無法設定 PauseUIManager（反射失敗）");
        }
    }
    
    [ContextMenu("顯示設定狀態")]
    public void ShowSetupStatus()
    {
        Debug.Log("=== PauseUI 設定狀態 ===");
        
        GameUIManager gameUIManager = FindFirstObjectByType<GameUIManager>();
        if (gameUIManager != null)
        {
            PauseUIManager pauseUIManager = gameUIManager.GetPauseUIManager();
            if (pauseUIManager != null)
            {
                Debug.Log($"✅ PauseUIManager: {pauseUIManager.gameObject.name}");
                Debug.Log($"   位置: {pauseUIManager.transform.GetHierarchyPath()}");
            }
            else
            {
                Debug.LogWarning("⚠️ PauseUIManager: 未設定");
            }
        }
        else
        {
            Debug.LogError("❌ GameUIManager: 不存在");
        }
        
        // 檢查所有 PauseUIManager
        PauseUIManager[] allPauseManagers = FindObjectsByType<PauseUIManager>(FindObjectsSortMode.None);
        Debug.Log($"找到 {allPauseManagers.Length} 個 PauseUIManager");
        foreach (var manager in allPauseManagers)
        {
            Debug.Log($"  - {manager.gameObject.name} ({manager.transform.GetHierarchyPath()})");
        }
    }
}

/// <summary>
/// Transform 擴展方法 - 獲取完整路徑
/// </summary>
public static class TransformExtensions
{
    public static string GetHierarchyPath(this Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}



