using UnityEngine;

/// <summary>
/// 傷害彈出管理器
/// 管理所有傷害彈出文字的生成和顯示
/// 使用螢幕空間 Canvas 確保適當大小顯示
/// </summary>
public class DamagePopupManager : MonoBehaviour
{
    [Header("Prefab 設定")]
    [SerializeField] private GameObject damagePopupPrefab;
    
    [Header("Canvas 設定")]
    [SerializeField] private Canvas screenCanvas; // 螢幕空間 Canvas
    [SerializeField] private Camera mainCamera; // 主攝影機
    [SerializeField] private bool autoFindCanvas = true;
    
    [Header("偏移設定")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.5f, 0); // 世界座標偏移
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 20f); // 螢幕座標偏移（像素）
    
    private static DamagePopupManager instance;
    public static DamagePopupManager Instance => instance;
    
    private void Awake()
    {
        // 單例模式
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("[DamagePopupManager] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // 自動尋找 Canvas
        if (autoFindCanvas && screenCanvas == null)
        {
            FindOrCreateCanvas();
        }
        
        // 驗證設定
        if (damagePopupPrefab == null)
        {
            Debug.LogError("[DamagePopupManager] Damage Popup Prefab is not assigned!");
        }
    }
    
    /// <summary>
    /// 尋找或創建螢幕空間 Canvas
    /// </summary>
    private void FindOrCreateCanvas()
    {
        // 尋找主攝影機
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[DamagePopupManager] Main Camera not found!");
                return;
            }
        }
        
        // 嘗試尋找已存在的 DamagePopupCanvas
        GameObject canvasObj = GameObject.Find("DamagePopupCanvas");
        
        if (canvasObj != null)
        {
            screenCanvas = canvasObj.GetComponent<Canvas>();
        }
        
        // 如果找不到，創建一個新的
        if (screenCanvas == null)
        {
            canvasObj = new GameObject("DamagePopupCanvas");
            screenCanvas = canvasObj.AddComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            screenCanvas.worldCamera = mainCamera;
            screenCanvas.planeDistance = 10f; // 在攝影機前方10單位
            screenCanvas.sortingOrder = 100; // 確保在其他 UI 上方
            
            // 添加 CanvasScaler
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // 添加 GraphicRaycaster
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            Debug.Log("[DamagePopupManager] Created DamagePopupCanvas in Screen Space - Camera mode.");
        }
        else
        {
            // 確保已存在的 Canvas 設定正確
            if (screenCanvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                screenCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                screenCanvas.worldCamera = mainCamera;
                Debug.Log("[DamagePopupManager] Updated existing canvas to Screen Space - Camera mode.");
            }
        }
    }
    
    /// <summary>
    /// 顯示傷害彈出文字
    /// </summary>
    /// <param name="damage">傷害數值</param>
    /// <param name="worldPosition">世界座標位置</param>
    public void ShowDamagePopup(int damage, Vector3 worldPosition)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("[DamagePopupManager] Cannot show damage popup - prefab not assigned!");
            return;
        }
        
        if (screenCanvas == null)
        {
            Debug.LogWarning("[DamagePopupManager] Cannot show damage popup - canvas not found!");
            return;
        }
        
        if (mainCamera == null)
        {
            Debug.LogWarning("[DamagePopupManager] Cannot show damage popup - camera not found!");
            return;
        }
        
        // 將世界座標轉換為螢幕座標
        Vector3 worldPos = worldPosition + worldOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        // 添加螢幕偏移
        screenPos.x += screenOffset.x;
        screenPos.y += screenOffset.y;
        
        // 生成彈出文字
        GameObject popupObj = Instantiate(damagePopupPrefab, screenCanvas.transform);
        RectTransform popupRect = popupObj.GetComponent<RectTransform>();
        
        if (popupRect != null)
        {
            popupRect.position = screenPos;
        }
        
        // 初始化
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Initialize(damage, screenPos, false);
        }
        else
        {
            Debug.LogError("[DamagePopupManager] DamagePopup component not found on prefab!");
            Destroy(popupObj);
        }
    }
    
    /// <summary>
    /// 顯示治療彈出文字
    /// </summary>
    /// <param name="healAmount">治療數值</param>
    /// <param name="worldPosition">世界座標位置</param>
    public void ShowHealPopup(int healAmount, Vector3 worldPosition)
    {
        if (damagePopupPrefab == null || screenCanvas == null || mainCamera == null) return;
        
        // 將世界座標轉換為螢幕座標
        Vector3 worldPos = worldPosition + worldOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        // 添加螢幕偏移
        screenPos.x += screenOffset.x;
        screenPos.y += screenOffset.y;
        
        // 生成彈出文字
        GameObject popupObj = Instantiate(damagePopupPrefab, screenCanvas.transform);
        RectTransform popupRect = popupObj.GetComponent<RectTransform>();
        
        if (popupRect != null)
        {
            popupRect.position = screenPos;
        }
        
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Initialize(healAmount, screenPos, true);
        }
        else
        {
            Destroy(popupObj);
        }
    }
    
    /// <summary>
    /// 設定傷害彈出 Prefab（動態設定）
    /// </summary>
    public void SetDamagePopupPrefab(GameObject prefab)
    {
        damagePopupPrefab = prefab;
    }
    
    /// <summary>
    /// 設定螢幕空間 Canvas
    /// </summary>
    public void SetScreenCanvas(Canvas canvas)
    {
        screenCanvas = canvas;
    }
    
    /// <summary>
    /// 設定主攝影機
    /// </summary>
    public void SetMainCamera(Camera cam)
    {
        mainCamera = cam;
    }
}
