using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Settings UI - 被動式 UI 面板控制器
/// 自動生成按鈕並提供功能
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Generation Settings")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer; // 按鈕生成的父物件
    
    [Header("Button Positions (RectTransforms)")]
    [SerializeField] private RectTransform closeButtonPos;
    [SerializeField] private RectTransform easyButtonPos;
    [SerializeField] private RectTransform mediumButtonPos;
    [SerializeField] private RectTransform hardButtonPos;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI difficultyText; // 顯示當前難度的文字
    
    // 內部引用 (用於追蹤生成的按鈕)
    private List<GameObject> generatedButtons = new List<GameObject>();
    
    /// <summary>
    /// 生成所有按鈕
    /// </summary>
    private void GenerateButtons()
    {
        if (buttonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("[SettingsUI] Button Prefab or Container is missing!");
            return;
        }
        
        // 清除可能殘留的按鈕（安全起見）
        DestroyButtons();
        
        // 生成各個按鈕並加入列表 (文字改為英文)
        generatedButtons.Add(CreateButton("Close", closeButtonPos, CloseButton));
        generatedButtons.Add(CreateButton("Easy", easyButtonPos, EasyButton));
        generatedButtons.Add(CreateButton("Medium", mediumButtonPos, MediumButton));
        generatedButtons.Add(CreateButton("Hard", hardButtonPos, HardButton));
        
        // 更新難度文字
        UpdateDifficultyText();
    }
    
    /// <summary>
    /// 更新難度文字說明
    /// </summary>
    private void UpdateDifficultyText()
    {
        if (difficultyText == null || GameSettings.Instance == null) return;
        
        string difficultyName = GameSettings.Instance.GetDifficultyName();
        difficultyText.text = $"Current Difficulty: {difficultyName}";
    }
    
    /// <summary>
    /// 銷毀所有生成的按鈕
    /// </summary>
    private void DestroyButtons()
    {
        foreach (var btn in generatedButtons)
        {
            if (btn != null)
            {
                Destroy(btn);
            }
        }
        generatedButtons.Clear();
    }
    
    /// <summary>
    /// 創建單個按鈕
    /// </summary>
    private GameObject CreateButton(string text, RectTransform targetPos, UnityEngine.Events.UnityAction onClickAction)
    {
        if (targetPos == null) return null;
        
        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
        btnObj.name = $"{text} Button";
        
        // 設定位置和大小
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.anchorMin = targetPos.anchorMin;
            btnRect.anchorMax = targetPos.anchorMax;
            btnRect.pivot = targetPos.pivot;
            btnRect.anchoredPosition = targetPos.anchoredPosition;
            btnRect.sizeDelta = targetPos.sizeDelta;
            btnRect.rotation = targetPos.rotation;
            btnRect.localScale = targetPos.localScale;
        }
        
        // 設定文字 (SF Button -> Background -> Label -> Text)
        Transform background = btnObj.transform.Find("Background");
        if (background != null)
        {
            Transform label = background.Find("Label");
            if (label != null)
            {
                Text textComp = label.GetComponent<Text>();
                if (textComp != null) textComp.text = text;
                
                TextMeshProUGUI tmpComp = label.GetComponent<TextMeshProUGUI>();
                if (tmpComp != null) tmpComp.text = text;
            }
        }
        
        // 設定點擊事件 (SF Button 上的 Button 組件)
        Button btnComp = btnObj.GetComponent<Button>();
        if (btnComp != null && onClickAction != null)
        {
            btnComp.onClick.RemoveAllListeners();
            btnComp.onClick.AddListener(onClickAction);
        }
        
        return btnObj;
    }
    
    #region 面板控制
    
    public void Show()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            GenerateButtons(); // 打開時生成按鈕
        }
    }
    
    public void Hide()
    {
        if (settingsPanel != null)
        {
            DestroyButtons(); // 關閉時銷毀按鈕
            settingsPanel.SetActive(false);
        }
    }
    
    public void Toggle()
    {
        if (settingsPanel != null)
        {
            if (settingsPanel.activeSelf) Hide();
            else Show();
        }
    }
    
    #endregion
    
    #region 按鈕函數
    
    public void CloseButton()
    {
        Hide();
    }
    
    public void EasyButton()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetDifficulty(0); // Easy
            UpdateDifficultyText(); // 更新難度文字
        }
    }
    
    public void MediumButton()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetDifficulty(1); // Medium
            UpdateDifficultyText(); // 更新難度文字
        }
    }
    
    public void HardButton()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetDifficulty(2); // Hard
            UpdateDifficultyText(); // 更新難度文字
        }
    }
    
    #endregion
}
