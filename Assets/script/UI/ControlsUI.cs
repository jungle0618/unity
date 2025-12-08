using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 按鍵說明UI
/// 顯示遊戲中所有正式版本可使用的按鍵說明
/// </summary>
public class ControlsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject controlsPanel;
    
    [Header("Generation Settings")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer; // 按鈕生成的父物件
    
    [Header("Button Positions (RectTransforms)")]
    [SerializeField] private RectTransform closeButtonPos;
    
    [Header("Settings")]
    [SerializeField] private bool hideOnStart = true;
    
    // 內部引用 (用於追蹤生成的按鈕)
    private List<GameObject> generatedButtons = new List<GameObject>();
    
    private void Awake()
    {
        // 初始隱藏控制說明面板
        if (controlsPanel != null && hideOnStart)
        {
            controlsPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 生成所有按鈕
    /// </summary>
    private void GenerateButtons()
    {
        if (buttonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("[ControlsUI] Button Prefab or Container is missing!");
            return;
        }
        
        // 清除可能殘留的按鈕（安全起見）
        DestroyButtons();
        
        // 生成關閉按鈕
        generatedButtons.Add(CreateButton("Close", closeButtonPos, CloseButton));
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

    /// <summary>
    /// 顯示按鍵說明面板
    /// </summary>
    public void Show()
    {
        if (controlsPanel == null)
        {
            Debug.LogWarning("[ControlsUI] controlsPanel is not assigned.");
            return;
        }

        // 確保自身被啟用
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // 生成按鈕（確保只在顯示時建立）
        GenerateButtons();

        controlsPanel.SetActive(true);
    }
    
    /// <summary>
    /// 隱藏按鍵說明面板
    /// </summary>
    public void Hide()
    {
        if (controlsPanel == null)
        {
            Debug.LogWarning("[ControlsUI] controlsPanel is not assigned.");
            return;
        }

        // 關閉時清理按鈕
        DestroyButtons();

        controlsPanel.SetActive(false);
    }
    
    /// <summary>
    /// 切換顯示/隱藏
    /// </summary>
    public void Toggle()
    {
        if (controlsPanel != null)
        {
            if (controlsPanel.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }
    
    /// <summary>
    /// 關閉按鈕點擊事件
    /// </summary>
    public void CloseButton()
    {
        Hide();
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    
    /// <summary>
    /// 檢查是否可見
    /// </summary>
    public bool IsVisible()
    {
        return controlsPanel != null && controlsPanel.activeSelf;
    }
}

