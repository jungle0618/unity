using UnityEngine;

/// <summary>
/// 出口點脈衝動畫
/// 讓出口標記產生呼吸效果，更容易被注意到
/// </summary>
public class ExitPointPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float minAlpha = 0.4f;
    [SerializeField] private float maxAlpha = 0.8f;
    
    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private Color baseColor;
    private float time = 0f;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseScale = transform.localScale;
            baseColor = spriteRenderer.color;
        }
    }
    
    private void Update()
    {
        if (spriteRenderer == null) return;
        
        time += Time.deltaTime * pulseSpeed;
        
        // 使用 sin 波形產生平滑的脈衝效果
        float pulse = (Mathf.Sin(time) + 1f) / 2f; // 0 到 1
        
        // 更新縮放
        float scale = Mathf.Lerp(minScale, maxScale, pulse);
        transform.localScale = baseScale * scale;
        
        // 更新透明度
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
        Color newColor = baseColor;
        newColor.a = alpha;
        spriteRenderer.color = newColor;
    }
}

