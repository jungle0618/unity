using UnityEngine;
using TMPro;

/// <summary>
/// 傷害彈出文字
/// 顯示受到的傷害數值，支援普通/暴擊樣式
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("UI 組件")]
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("動畫設定")]
    [SerializeField] private float lifetime = 1.5f; // 存活時間
    [SerializeField] private float floatSpeed = 1.5f; // 上浮速度
    [SerializeField] private float fadeStartTime = 0.5f; // 開始淡出的時間
    [SerializeField] private Vector2 randomOffset = new Vector2(0.5f, 0.3f); // 隨機偏移範圍
    
    [Header("顏色設定")]
    [SerializeField] private Color damageColor = Color.white;
    [SerializeField] private Color healColor = Color.green;
    
    [Header("字體大小")]
    [SerializeField] private float fontSize = 24f; // 螢幕空間適用大小
    
    private float spawnTime;
    private Vector2 velocity;
    private RectTransform rectTransform;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
    
    /// <summary>
    /// 初始化傷害彈出文字
    /// </summary>
    /// <param name="damage">傷害數值</param>
    /// <param name="screenPosition">螢幕座標位置（已由 Manager 設定）</param>
    /// <param name="isHeal">是否治療</param>
    public void Initialize(int damage, Vector3 screenPosition, bool isHeal = false)
    {
        spawnTime = Time.time;
        
        // 位置已由 Manager 通過 RectTransform.position 設定，這裡只添加隨機偏移
        if (rectTransform != null)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(-randomOffset.x * 10f, randomOffset.x * 10f), // 螢幕像素偏移
                Random.Range(0, randomOffset.y * 10f)
            );
            rectTransform.anchoredPosition += randomPos;
        }
        
        // 設定文字內容
        if (isHeal)
        {
            damageText.text = $"+{damage}";
            damageText.color = healColor;
        }
        else
        {
            damageText.text = damage.ToString();
            damageText.color = damageColor;
        }
        
        damageText.fontSize = fontSize;
        
        // 設定初始透明度
        canvasGroup.alpha = 1f;
        
        // 設定上浮速度（螢幕空間像素/秒）
        velocity = Vector2.up * (floatSpeed * 50f); // 轉換為像素速度
    }
    
    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        
        // 上浮（使用 RectTransform 在螢幕空間移動）
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += velocity * Time.deltaTime;
        }
        
        // 減速
        velocity *= 0.95f;
        
        // 淡出
        if (elapsed > fadeStartTime)
        {
            float fadeProgress = (elapsed - fadeStartTime) / (lifetime - fadeStartTime);
            canvasGroup.alpha = 1f - fadeProgress;
        }
        
        // 存活時間到期，銷毀
        if (elapsed > lifetime)
        {
            Destroy(gameObject);
        }
    }
}

