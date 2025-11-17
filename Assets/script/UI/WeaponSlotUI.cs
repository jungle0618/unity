using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 武器槽位UI - 單個武器的顯示槽位
/// 顯示武器圖示、名稱、耐久度
/// 亮起 = 當前裝備，暗淡 = 未裝備
/// </summary>
public class WeaponSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image weaponIcon;          // 武器圖示
    [SerializeField] private TextMeshProUGUI weaponName; // 武器名稱
    [SerializeField] private Image background;          // 背景
    [SerializeField] private Image durabilityBar;       // 耐久度條
    [SerializeField] private GameObject durabilityPanel; // 耐久度面板
    [SerializeField] private CanvasGroup canvasGroup;   // 用於控制亮暗
    
    [Header("Count/Badge")]
    [SerializeField] private TextMeshProUGUI countBadge; // 右下角堆疊數字（可選）
    
    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private float selectedAlpha = 1.0f;
    [SerializeField] private float unselectedAlpha = 0.5f;
    
    [Header("Durability Colors")]
    [SerializeField] private Color durabilityHighColor = Color.green;
    [SerializeField] private Color durabilityMediumColor = Color.yellow;
    [SerializeField] private Color durabilityLowColor = Color.red;
    
    [Header("Animation")]
    [SerializeField] private float transitionSpeed = 10f; // 過渡動畫速度
    
    private int slotIndex;
    private bool isSelected = false;
    private bool isEmpty = true;
    private Weapon currentWeapon;
    private float targetAlpha;
    
    private void Update()
    {
        // 平滑過渡 alpha 值
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * transitionSpeed);
        }
    }
    
    /// <summary>
    /// 初始化槽位
    /// </summary>
    public void Initialize(int index)
    {
        slotIndex = index;
        SetEmpty();
        SetSelected(false);
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 若未綁定 countBadge，嘗試在子階層尋找名稱為 "Count" 的 TMP
        if (countBadge == null)
        {
            var tf = transform.Find("Count");
            if (tf != null) countBadge = tf.GetComponent<TextMeshProUGUI>();
        }
        
        SetCount(0); // 預設隱藏
    }
    
    /// <summary>
    /// 設定武器
    /// </summary>
    public void SetWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            SetEmpty();
            return;
        }
        
        currentWeapon = weapon;
        isEmpty = false;
        
        // 顯示武器圖示 - 從 SpriteRenderer 或 ItemIcon 取得
        if (weaponIcon != null)
        {
            Sprite weaponSprite = GetWeaponSprite(weapon);
            
            if (weaponSprite != null)
            {
                weaponIcon.sprite = weaponSprite;
                weaponIcon.enabled = true;
                Debug.Log($"[WeaponSlotUI] ✓ Set weapon sprite for {weapon.ItemName}: {weaponSprite.name} → UI Image component");
                
                // 驗證設置成功
                if (weaponIcon.sprite == weaponSprite)
                {
                    Debug.Log($"[WeaponSlotUI] ✓ Verified: UI Image.sprite is correctly set to {weaponSprite.name}");
                }
                else
                {
                    Debug.LogError($"[WeaponSlotUI] ❌ Failed to set sprite! UI Image.sprite is {weaponIcon.sprite?.name ?? "null"}");
                }
            }
            else
            {
                Debug.LogWarning($"[WeaponSlotUI] No sprite found for weapon {weapon.ItemName}");
                weaponIcon.sprite = null;
                weaponIcon.enabled = false;
            }
        }
        else
        {
            Debug.LogError("[WeaponSlotUI] weaponIcon (UI Image) is NOT assigned! Please assign it in the Inspector.");
        }
        
        // 顯示武器名稱
        if (weaponName != null)
        {
            weaponName.text = weapon.ItemName;
        }
        else
        {
            Debug.LogWarning("[WeaponSlotUI] weaponName (TextMeshPro) is not assigned");
        }
        
        // 更新耐久度
        UpdateDurability(weapon.CurrentDurability, weapon.MaxDurability);
    }
    
    /// <summary>
    /// 從武器獲取顯示用的 Sprite
    /// 優先從 SpriteRenderer 取得，其次從 ItemIcon
    /// </summary>
    private Sprite GetWeaponSprite(Weapon weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("[WeaponSlotUI] GetWeaponSprite called with null weapon");
            return null;
        }
        
        Debug.Log($"[WeaponSlotUI] Getting sprite for weapon: {weapon.ItemName}");
        
        // 1. 嘗試從 SpriteRenderer 取得（武器 prefab 通常有 SpriteRenderer）
        var spriteRenderer = weapon.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Debug.Log($"[WeaponSlotUI] Found SpriteRenderer on {weapon.ItemName}");
            if (spriteRenderer.sprite != null)
            {
                Debug.Log($"[WeaponSlotUI] ✓ Using sprite from SpriteRenderer: {spriteRenderer.sprite.name}");
                return spriteRenderer.sprite;
            }
            else
            {
                Debug.LogWarning($"[WeaponSlotUI] SpriteRenderer found but sprite is null on {weapon.ItemName}");
            }
        }
        else
        {
            Debug.Log($"[WeaponSlotUI] No SpriteRenderer found on {weapon.ItemName} root");
        }
        
        // 2. 嘗試從子物件的 SpriteRenderer 取得
        spriteRenderer = weapon.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Debug.Log($"[WeaponSlotUI] Found SpriteRenderer in children of {weapon.ItemName}");
            if (spriteRenderer.sprite != null)
            {
                Debug.Log($"[WeaponSlotUI] ✓ Using sprite from child SpriteRenderer: {spriteRenderer.sprite.name}");
                return spriteRenderer.sprite;
            }
            else
            {
                Debug.LogWarning($"[WeaponSlotUI] Child SpriteRenderer found but sprite is null on {weapon.ItemName}");
            }
        }
        else
        {
            Debug.Log($"[WeaponSlotUI] No SpriteRenderer found in children of {weapon.ItemName}");
        }
        
        // 3. 退而求其次：使用 ItemIcon（如果有設定）
        if (weapon.ItemIcon != null)
        {
            Debug.Log($"[WeaponSlotUI] ✓ Using ItemIcon as fallback: {weapon.ItemIcon.name}");
            return weapon.ItemIcon;
        }
        else
        {
            Debug.Log($"[WeaponSlotUI] ItemIcon is also null on {weapon.ItemName}");
        }
        
        Debug.LogError($"[WeaponSlotUI] ❌ No sprite found for {weapon.ItemName} - check weapon prefab has SpriteRenderer with sprite assigned!");
        return null;
    }
    
    /// <summary>
    /// 設定為空槽位
    /// </summary>
    public void SetEmpty()
    {
        isEmpty = true;
        currentWeapon = null;
        
        if (weaponIcon != null)
        {
            weaponIcon.sprite = null;
            weaponIcon.enabled = false;
        }
        
        if (weaponName != null)
        {
            weaponName.text = "---";
        }
        
        if (durabilityPanel != null)
        {
            durabilityPanel.SetActive(false);
        }
        
        SetCount(0);
    }
    
    /// <summary>
    /// 設定選中狀態（亮/暗）
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 設定目標透明度
        targetAlpha = selected ? selectedAlpha : unselectedAlpha;
        
        // 更新背景顏色
        if (background != null)
        {
            background.color = selected ? selectedColor : unselectedColor;
        }
        
        // 更新武器名稱顏色
        if (weaponName != null)
        {
            Color nameColor = weaponName.color;
            nameColor.a = selected ? 1.0f : 0.7f;
            weaponName.color = nameColor;
        }
    }
    
    /// <summary>
    /// 設定可見性
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// 更新耐久度顯示
    /// </summary>
    public void UpdateDurability(int current, int max)
    {
        if (durabilityPanel != null)
        {
            durabilityPanel.SetActive(true);
        }
        
        if (durabilityBar != null)
        {
            float percentage = max > 0 ? (float)current / max : 0f;
            durabilityBar.fillAmount = percentage;
            
            // 根據耐久度百分比改變顏色
            if (percentage > 0.5f)
                durabilityBar.color = durabilityHighColor;
            else if (percentage > 0.25f)
                durabilityBar.color = durabilityMediumColor;
            else
                durabilityBar.color = durabilityLowColor;
        }
    }
    
    // 新增：設定右下角數量徽章
    public void SetCount(int count)
    {
        if (countBadge == null) return;
        if (count <= 1)
        {
            countBadge.text = string.Empty;
            countBadge.gameObject.SetActive(false);
        }
        else
        {
            countBadge.text = count.ToString();
            countBadge.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 獲取當前武器
    /// </summary>
    public Weapon GetWeapon()
    {
        return currentWeapon;
    }
    
    /// <summary>
    /// 是否為空槽位
    /// </summary>
    public bool IsEmpty()
    {
        return isEmpty;
    }
    
    /// <summary>
    /// 是否被選中
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
}

