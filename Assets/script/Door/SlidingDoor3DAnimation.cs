using UnityEngine;

/// <summary>
/// 滑動門動畫控制器（側向滑動）
/// 使用位置移動來實現門的滑動效果
/// </summary>
public class SlidingDoor3DAnimation : MonoBehaviour
{
    [Header("門的設定")]
    [SerializeField] private Transform doorTransform; // 門的 Transform（會移動的部分）
    
    [Header("滑動設定")]
    [SerializeField] private float slideDistance = 2f;     // 滑動距離
    [SerializeField] private Vector3 slideDirection = Vector3.right; // 滑動方向（預設向右）
    [SerializeField] private float animationDuration = 1f; // 動畫持續時間（秒）
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 動畫曲線
    
    private bool isAnimating = false;
    private bool isOpen = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private float animationProgress = 0f;
    
    private void Start()
    {
        // 如果沒有指定門的 Transform，使用自身
        if (doorTransform == null)
        {
            doorTransform = transform;
        }
        
        // 記錄關閉狀態的位置
        closedPosition = doorTransform.localPosition;
        
        // 計算開啟狀態的位置（沿滑動方向移動）
        openPosition = closedPosition + slideDirection.normalized * slideDistance;
    }
    
    private void Update()
    {
        if (!isAnimating) return;
        
        // 更新動畫進度
        animationProgress += Time.deltaTime / animationDuration;
        animationProgress = Mathf.Clamp01(animationProgress);
        
        // 應用動畫曲線
        float curveValue = slideCurve.Evaluate(animationProgress);
        
        // 插值位置
        doorTransform.localPosition = Vector3.Lerp(closedPosition, openPosition, curveValue);
        
        // 動畫完成
        if (animationProgress >= 1f)
        {
            isAnimating = false;
            animationProgress = 0f;
            isOpen = true;
        }
    }
    
    /// <summary>
    /// 開啟門（滑動動畫）
    /// </summary>
    public void Open()
    {
        if (isOpen || isAnimating) return;
        
        isAnimating = true;
        animationProgress = 0f;
    }
    
    /// <summary>
    /// 立即設定門為開啟狀態（不播放動畫）
    /// </summary>
    public void SetOpenImmediate()
    {
        isOpen = true;
        isAnimating = false;
        animationProgress = 0f;
        
        if (doorTransform != null)
        {
            doorTransform.localPosition = openPosition;
        }
    }
    
    // 公開屬性
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
}
