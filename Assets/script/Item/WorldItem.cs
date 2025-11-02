using UnityEngine;

/// <summary>
/// WorldItem（場景中散落的物品）
/// 用於表示場景中可以撿取的物品
/// 儲存物品類型、位置、以及對應的 Item Prefab
/// </summary>
public class WorldItem : MonoBehaviour
{
    [Header("物品設定")]
    [SerializeField] private string itemType; // 物品類型名稱（對應 itemdata.txt 中的 ItemType）
    [SerializeField] private GameObject itemPrefab; // 對應的 Item Prefab（用於撿取後加入 ItemHolder）
    [SerializeField] private Sprite itemSprite; // 物品在場景中的顯示圖示
    
    [Header("視覺設定")]
    [SerializeField] private Vector3 itemScale = Vector3.one; // 物品大小（固定）
    [SerializeField] private float floatAmplitude = 0.2f; // 漂浮幅度
    [SerializeField] private float floatSpeed = 2f; // 漂浮速度
    [SerializeField] private bool enableFloating = true; // 是否啟用漂浮效果
    
    // 組件引用
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private float floatTimer;
    
    // 屬性
    public string ItemType => itemType;
    public GameObject ItemPrefab => itemPrefab;
    public Vector3 Position => transform.position;
    
    /// <summary>
    /// 設定物品類型（由 ItemManager 調用）
    /// </summary>
    public void SetItemType(string type)
    {
        itemType = type;
    }
    
    /// <summary>
    /// 設定物品 Prefab（由 ItemManager 調用）
    /// </summary>
    public void SetItemPrefab(GameObject prefab)
    {
        itemPrefab = prefab;
        
        // 嘗試從 prefab 獲取圖示
        if (prefab != null)
        {
            Item itemComponent = prefab.GetComponent<Item>();
            if (itemComponent != null && itemComponent.ItemIcon != null)
            {
                SetItemSprite(itemComponent.ItemIcon);
            }
        }
    }
    
    /// <summary>
    /// 設定物品顯示圖示
    /// </summary>
    public void SetItemSprite(Sprite sprite)
    {
        itemSprite = sprite;
        
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
    
    /// <summary>
    /// 設定物品大小（由 ItemManager 調用）
    /// </summary>
    public void SetItemScale(Vector3 scale)
    {
        itemScale = scale;
        transform.localScale = scale;
    }
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        startPosition = transform.position;
        floatTimer = Random.Range(0f, 2f * Mathf.PI); // 隨機起始相位
        
        // 應用固定的物品大小
        transform.localScale = itemScale;
    }
    
    private void Update()
    {
        if (enableFloating)
        {
            UpdateFloatingEffect();
        }
    }
    
    /// <summary>
    /// 初始化組件
    /// </summary>
    private void InitializeComponents()
    {
        // 獲取或添加 SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // 設定圖示
        if (itemSprite != null)
        {
            spriteRenderer.sprite = itemSprite;
        }
        
        // 獲取或添加 Collider（用於物理交互，可選）
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }
    }
    
    /// <summary>
    /// 更新漂浮效果
    /// </summary>
    private void UpdateFloatingEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;
        float offset = Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = startPosition + Vector3.up * offset;
    }
    
    /// <summary>
    /// 被撿取時調用（由 ItemManager 調用）
    /// </summary>
    public void OnPickedUp()
    {
        // 可以添加撿取特效、音效等
        Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        // 在編輯器中顯示物品範圍
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
    
    private void OnDrawGizmosSelected()
    {
        // 被選中時顯示更明顯的範圍
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 顯示物品類型
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, $"Type: {itemType}");
        #endif
    }
}

