using UnityEngine;

/// <summary>
/// 子彈預製體腳本
/// 用於創建簡單的子彈物件
/// </summary>
public class BulletPrefab : MonoBehaviour
{
    [Header("子彈外觀")]
    [SerializeField] private Sprite bulletSprite;
    [SerializeField] private Color bulletColor = Color.yellow;
    [SerializeField] private float bulletSize = 0.1f;
    
    private void Start()
    {
        SetupBullet();
    }
    
    private void SetupBullet()
    {
        // 添加必要的組件
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // 子彈不受重力影響
        }
        
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = bulletSize;
        }
        
        if (GetComponent<BulletController>() == null)
        {
            gameObject.AddComponent<BulletController>();
        }
        
        // 設定子彈外觀
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (bulletSprite != null)
        {
            renderer.sprite = bulletSprite;
        }
        else
        {
            // 創建簡單的圓形子彈
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                Vector2 pos = new Vector2(i % 32, i / 32);
                float distance = Vector2.Distance(pos, new Vector2(16, 16));
                pixels[i] = distance < 16 ? bulletColor : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            renderer.sprite = sprite;
        }
        
        // 設定子彈大小
        transform.localScale = Vector3.one * bulletSize;
        
        // 設定圖層（避免與其他物件衝突）
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
}
