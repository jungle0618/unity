using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 按鍵說明UI
/// 顯示遊戲中所有正式版本可使用的按鍵說明
/// </summary>
public class ControlsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private Button closeButton;
    
    [Header("Content References")]
    [SerializeField] private TextMeshProUGUI controlsText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRectTransform;
    [Tooltip("Content 的最小高度（避免內容太少時顯示異常）")]
    [SerializeField] private float minContentHeight = 100f;
    [Tooltip("Content 底部的額外空間（讓文字下方留白）")]
    [SerializeField] private float bottomPadding = 20f;
    
    [Header("Settings")]
    [SerializeField] private bool hideOnStart = true;
    
    private void Awake()
    {
        // 初始隱藏控制說明面板
        if (controlsPanel != null && hideOnStart)
        {
            controlsPanel.SetActive(false);
        }
    }
    
    private void Start()
    {
        // 設定關閉按鈕
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
        
        // 更新按鍵說明文字
        if (controlsText != null)
        {
            UpdateControlsText();
            // 自動調整 Content 高度
            UpdateContentHeight();
        }
        
        // 如果沒有手動指定 contentRectTransform，嘗試從 scrollRect 獲取
        if (contentRectTransform == null && scrollRect != null)
        {
            contentRectTransform = scrollRect.content;
        }
    }
    
    /// <summary>
    /// 顯示按鍵說明面板
    /// </summary>
    public void Show()
    {
        if (controlsPanel != null)
        {
            // 先激活面板
            controlsPanel.SetActive(true);
            
            // 確保 GameObject 本身也是激活的（協程需要在激活的 GameObject 上運行）
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            
            // 使用協程延遲更新，確保 UI 系統完成布局計算
            // 注意：必須在 GameObject 激活後才能啟動協程
            StartCoroutine(UpdateUIAfterFrame());
        }
    }
    
    /// <summary>
    /// 在下一幀更新 UI（確保 TextMeshPro 完成網格更新）
    /// </summary>
    private System.Collections.IEnumerator UpdateUIAfterFrame()
    {
        // 等待一幀，讓 UI 系統完成布局計算
        yield return null;
        
        // 再次確保面板是激活的
        if (controlsPanel != null && !controlsPanel.activeSelf)
        {
            controlsPanel.SetActive(true);
            yield return null; // 再等待一幀
        }
        
        // 強制 Canvas 更新
        Canvas.ForceUpdateCanvases();
        
        // 更新 Content 高度（確保文字內容變化時高度正確）
        UpdateContentHeight();
        
        // 重置滾動位置到頂部
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
    
    /// <summary>
    /// 隱藏按鍵說明面板
    /// </summary>
    public void Hide()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
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
    private void OnCloseButtonClicked()
    {
        Hide();
    }
    
    /// <summary>
    /// 更新按鍵說明文字
    /// </summary>
    private void UpdateControlsText()
    {
        if (controlsText == null) return;
        
        string text = GenerateControlsText();
        controlsText.text = text;
    }
    
    /// <summary>
    /// 根據文字內容自動調整 Content 高度
    /// </summary>
    private void UpdateContentHeight()
    {
        if (controlsText == null || contentRectTransform == null) return;
        
        // 強制 TextMeshPro 計算文字所需的實際高度
        controlsText.ForceMeshUpdate();
        
        // 獲取文字的首選高度（包含所有內容）
        float preferredHeight = controlsText.preferredHeight;
        
        // 添加底部留白
        float heightWithPadding = preferredHeight + bottomPadding;
        
        // 確保高度不小於最小值
        float newHeight = Mathf.Max(heightWithPadding, minContentHeight);
        
        // 更新 Content 的高度
        Vector2 sizeDelta = contentRectTransform.sizeDelta;
        sizeDelta.y = newHeight;
        contentRectTransform.sizeDelta = sizeDelta;
    }
    
    /// <summary>
    /// 生成按鍵說明文字
    /// </summary>
    private string GenerateControlsText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // English version
        sb.AppendLine("<size=40><color=#000000><b>🎮 Game Controls</b></color></size>");
        sb.AppendLine();
        
        // Basic Movement Controls
        sb.AppendLine("<size=32><color=#000000><b>Basic Movement</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>W / A / S / D</b> - Move character (Up / Left / Down / Right)");
        sb.AppendLine("<b>Shift</b> - Sprint (Hold)");
        sb.AppendLine("<b>Z</b> - Crouch (Toggle)");
        sb.AppendLine();
        
        // Combat Controls
        sb.AppendLine("<size=32><color=#000000><b>Combat</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>Q</b> - Attack (Use currently equipped weapon)");
        sb.AppendLine();
        
        // Interaction Controls
        sb.AppendLine("<size=32><color=#000000><b>Interaction</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>E</b> - Interact (Pick up items, open doors, etc.)");
        sb.AppendLine("<b>R</b> - Switch items (Cycle through weapons)");
        sb.AppendLine();
        
        // Quick Weapon Switch
        sb.AppendLine("<size=32><color=#000000><b>Quick Weapon Switch</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>1</b> or <b>Numpad 1</b> - Switch to Knife");
        sb.AppendLine("<b>2</b> or <b>Numpad 2</b> - Switch to Gun");
        sb.AppendLine("<b>3</b> or <b>Numpad 3</b> - Switch to Empty Hands");
        sb.AppendLine();
        
        // Camera Controls
        sb.AppendLine("<size=32><color=#000000><b>Camera Controls</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>Space</b> (Hold) - Move camera (Player cannot move while holding, use WASD to move camera)");
        sb.AppendLine("<b>Y</b> - Center camera on player");
        sb.AppendLine();
        
        // Game Controls
        sb.AppendLine("<size=32><color=#000000><b>Game Controls</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>ESC</b> - Pause/Resume game");
        sb.AppendLine();
        
        // UI Functions
        sb.AppendLine("<size=32><color=#000000><b>UI Functions</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>M</b> - Map zoom (Hold to zoom in, release to zoom out)");
        sb.AppendLine();
        
        // Notes
        sb.AppendLine("<size=28><color=#000000><b>📝 Notes</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("• While holding <b>Space</b>, the character cannot move, only camera can be controlled");
        sb.AppendLine("• Crouching reduces movement speed but makes you less detectable by enemies");
        
        /* Chinese version (commented out)
        sb.AppendLine("<size=40><color=#000000><b>🎮 遊戲操作說明</b></color></size>");
        sb.AppendLine();
        
        // 基本移動控制
        sb.AppendLine("<size=32><color=#000000><b>基本移動控制</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>W / A / S / D</b> - 移動角色（上下左右）");
        sb.AppendLine("<b>Shift</b> - 快速奔跑（按住）");
        sb.AppendLine("<b>Z</b> - 蹲下（切換）");
        sb.AppendLine();
        
        // 戰鬥操作
        sb.AppendLine("<size=32><color=#000000><b>戰鬥操作</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>Q</b> - 攻擊（使用當前裝備的武器）");
        sb.AppendLine();
        
        // 互動操作
        sb.AppendLine("<size=32><color=#000000><b>互動操作</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>E</b> - 互動（撿取物品、開門等）");
        sb.AppendLine("<b>R</b> - 切換物品（循環切換武器）");
        sb.AppendLine();
        
        // 武器快速切換
        sb.AppendLine("<size=32><color=#000000><b>武器快速切換</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>1</b> 或 <b>小鍵盤1</b> - 切換到小刀（Knife）");
        sb.AppendLine("<b>2</b> 或 <b>小鍵盤2</b> - 切換到槍（Gun）");
        sb.AppendLine("<b>3</b> 或 <b>小鍵盤3</b> - 切換到空手（Empty Hands）");
        sb.AppendLine();
        
        // 鏡頭控制
        sb.AppendLine("<size=32><color=#000000><b>鏡頭控制</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>Space</b>（長按） - 移動鏡頭（按住時玩家無法移動，可用 WASD 移動鏡頭）");
        sb.AppendLine("<b>Y</b> - 將相機拉回以玩家為中心");
        sb.AppendLine();
        
        // 遊戲控制
        sb.AppendLine("<size=32><color=#000000><b>遊戲控制</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>ESC</b> - 暫停/恢復遊戲");
        sb.AppendLine();
        
        // UI 功能
        sb.AppendLine("<size=32><color=#000000><b>UI 功能</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("<b>M</b> - 地圖縮放（按住放大，放開恢復）");
        sb.AppendLine();
        
        // 注意事項
        sb.AppendLine("<size=28><color=#000000><b>📝 注意事項</b></color></size>");
        sb.AppendLine();
        sb.AppendLine("• 長按 <b>Space</b> 時，角色將無法移動，只能控制鏡頭");
        sb.AppendLine("• 蹲下時移動速度會降低，但更不容易被敵人發現");
        */
        
        return sb.ToString();
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

