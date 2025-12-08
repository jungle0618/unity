using UnityEngine;

/// <summary>
/// 3D門的視覺效果控制器
/// 連接到 DoorController 的事件，當門被開啟時播放 3D 動畫
/// 門一旦開啟就保持開啟狀態，與 2D Tilemap 門邏輯一致
/// </summary>
public class Door3DVisual : MonoBehaviour
{
    [Header("門的位置設定")]
    [Tooltip("這個 3D 門對應的 Tilemap 世界位置")]
    [SerializeField] private Vector3 tilemapWorldPosition;
    
    [Header("動畫設定（選擇其中一種）")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private SlidingDoor3DAnimation slidingDoorAnimation;
    
    // Animator 參數名稱
    private const string ANIM_OPEN = "Open";
    
    private bool hasOpened = false;
    
    private void Start()
    {
        // 自動查找動畫組件
        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInChildren<Animator>();
        }
        
        if (slidingDoorAnimation == null)
        {
            slidingDoorAnimation = GetComponentInChildren<SlidingDoor3DAnimation>();
        }
        
        if (doorAnimator == null && slidingDoorAnimation == null)
        {
            Debug.LogWarning($"[Door3DVisual] {gameObject.name} 沒有找到任何動畫組件！");
        }
    }
    
    private void OnEnable()
    {
        // 註冊到 DoorController 的開門檢查
        // 每次玩家嘗試開門時，我們檢查是否是這個門
        if (DoorController.Instance != null)
        {
            // 注意：我們需要修改 DoorController 來支援事件
            // 目前先使用輪詢方式檢查
        }
    }
    
    /// <summary>
    /// 播放開門動畫（只播放一次）
    /// </summary>
    public void PlayOpenAnimation()
    {
        if (hasOpened) return;
        
        hasOpened = true;
        
        // 優先使用 Animator
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(ANIM_OPEN);
            Debug.Log($"[Door3DVisual] 門 {gameObject.name} 開啟（使用 Animator）");
            return;
        }
        
        // 使用 SlidingDoor3DAnimation
        if (slidingDoorAnimation != null)
        {
            slidingDoorAnimation.Open();
            Debug.Log($"[Door3DVisual] 門 {gameObject.name} 開啟（使用 SlidingDoor3DAnimation）");
            return;
        }
        
        Debug.LogWarning($"[Door3DVisual] {gameObject.name} 沒有可用的動畫組件");
    }
    
    /// <summary>
    /// 設定門對應的 Tilemap 位置
    /// </summary>
    public void SetTilemapPosition(Vector3 position)
    {
        tilemapWorldPosition = position;
    }
    
    /// <summary>
    /// 在編輯器中顯示門的對應位置
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(tilemapWorldPosition, 0.5f);
        Gizmos.DrawLine(transform.position, tilemapWorldPosition);
        
        #if UNITY_EDITOR
        // 顯示文字標籤（僅在編輯器中）
        UnityEditor.Handles.Label(tilemapWorldPosition, "Tilemap Door Position");
        #endif
    }
    
    // 公開屬性
    public Vector3 TilemapWorldPosition => tilemapWorldPosition;
    public bool HasOpened => hasOpened;
}
