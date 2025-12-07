﻿using UnityEngine;
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
    [SerializeField] private RectTransform borderRect;   // 邊框矩形
    [SerializeField] private RectTransform backgroundRect; // 背景矩形
    [SerializeField] private Image weaponIcon;          // 武器圖示
    [SerializeField] private Image durabilityBar;       // 耐久度條
    [SerializeField] private GameObject durabilityPanel; // 耐久度面板
    [SerializeField] private CanvasGroup canvasGroup;   // 用於控制亮暗
    [SerializeField] private Image cooldownOverlay;     // 冷卻遮罩（新增）
    
    [Header("Count/Badge")]
    [SerializeField] private TextMeshProUGUI countBadge; // 右下角堆疊數字（可選）
    
    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private float selectedAlpha = 1.0f;
    [SerializeField] private float unselectedAlpha = 0.5f;
    
    [Header("Border Settings")]
    [SerializeField] private float borderWidth = 2f; // 邊框寬度
    [SerializeField] private Color borderColor = Color.black; // 邊框顏色（黑色）
    
    [Header("Durability Colors")]
    [SerializeField] private Color durabilityHighColor = Color.green;
    [SerializeField] private Color durabilityMediumColor = Color.yellow;
    [SerializeField] private Color durabilityLowColor = Color.red;
    
    [Header("Cooldown Settings")]
    [SerializeField] private Color attackCooldownColor = new Color(0f, 0f, 0f, 0.7f); // Dark overlay for attack cooldown
    [SerializeField] private Color equipDelayColor = new Color(1f, 0.6f, 0f, 0.7f);   // Orange overlay for equip delay
    
    [Header("Animation")]
    [SerializeField] private float transitionSpeed = 10f; // 過渡動畫速度
    
    private int slotIndex;
    private bool isSelected = false;
    private bool isEmpty = true;
    private Weapon currentWeapon;
    private float targetAlpha;
    
    private void Start()
    {
        // 在 Start 中再次設置邊框，確保父物件尺寸已正確
        if (borderRect != null)
        {
            SetupBorder();
        }
    }
    
    private void Update()
    {
        // 平滑過渡 alpha 值
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * transitionSpeed);
        }
        
        // 更新冷卻遮罩
        UpdateCooldownOverlay();
    }
    
    /// <summary>
    /// 更新冷卻遮罩顯示
    /// </summary>
    private void UpdateCooldownOverlay()
    {
        if (cooldownOverlay == null || currentWeapon == null || isEmpty)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
                cooldownOverlay.enabled = false;
            }
            return;
        }
        
        // Check equip delay first (takes priority)
        float remainingEquipTime = currentWeapon.RemainingEquipTime;
        if (remainingEquipTime > 0)
        {
            // Show equip delay overlay (orange)
            cooldownOverlay.enabled = true;
            cooldownOverlay.color = equipDelayColor;
            
            // Calculate fill amount (1.0 = fully blocking, 0.0 = ready)
            float equipDelay = currentWeapon.EquipDelayDuration;
            float fillAmount = equipDelay > 0 ? remainingEquipTime / equipDelay : 0f;
            cooldownOverlay.fillAmount = fillAmount;
            return;
        }
        
        // Check attack cooldown
        float remainingCooldown = currentWeapon.RemainingAttackCooldown;
        if (remainingCooldown > 0)
        {
            // Show attack cooldown overlay (dark)
            cooldownOverlay.enabled = true;
            cooldownOverlay.color = attackCooldownColor;
            
            // Calculate fill amount
            float attackCooldown = currentWeapon.AttackCooldownDuration;
            float fillAmount = attackCooldown > 0 ? remainingCooldown / attackCooldown : 0f;
            cooldownOverlay.fillAmount = fillAmount;
        }
        else
        {
            // Weapon is ready - hide overlay
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.enabled = false;
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
        
        // 設置背景
        SetupBackground();
        
        // 設置邊框
        SetupBorder();
        
        // 設置圖標層級（確保在背景前面）
        SetupIconLayer();
        
        SetCount(0); // 預設隱藏
    }
    
    /// <summary>
    /// 設置圖標層級（確保在背景前面）
    /// </summary>
    private void SetupIconLayer()
    {
        if (weaponIcon == null) return;
        
        RectTransform iconRect = weaponIcon.GetComponent<RectTransform>();
        if (iconRect == null) return;
        
        // 確保圖標在背景之上
        if (backgroundRect != null)
        {
            int backgroundIndex = backgroundRect.GetSiblingIndex();
            iconRect.SetSiblingIndex(backgroundIndex + 1);
        }
        else
        {
            // 如果沒有背景，確保圖標在邊框之上
            if (borderRect != null)
            {
                int borderIndex = borderRect.GetSiblingIndex();
                iconRect.SetSiblingIndex(borderIndex + 1);
            }
        }
    }
    
    /// <summary>
    /// 設置背景矩形
    /// </summary>
    private void SetupBackground()
    {
        if (backgroundRect == null) return;
        
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null) return;
        
        // 設置錨點：完全填充父物件
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 1f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 設置偏移為 0，讓背景完全填充父物件
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        
        // 確保背景在邊框之上，但在其他元素之下
        if (borderRect != null)
        {
            int borderIndex = borderRect.GetSiblingIndex();
            backgroundRect.SetSiblingIndex(borderIndex + 1);
        }
        else
        {
            backgroundRect.SetAsFirstSibling();
        }
        
        // 自動獲取或添加 Image 組件
        Image backgroundImage = backgroundRect.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = backgroundRect.gameObject.AddComponent<Image>();
        }
        
        if (backgroundImage != null)
        {
            // 初始顏色會在 SetSelected 中設置
            backgroundImage.color = unselectedColor;
        }
    }
    
    /// <summary>
    /// 設置邊框
    /// </summary>
    private void SetupBorder()
    {
        if (borderRect == null) return;
        
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null) return;
        
        // 強制重建布局以確保尺寸正確
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        
        float width = parentRect.rect.width;
        float height = parentRect.rect.height;
        
        // 如果尺寸無效，使用 LayoutGroup 計算的尺寸
        if (width <= 0 || height <= 0)
        {
            // 嘗試從 LayoutElement 獲取
            var layoutElement = GetComponent<UnityEngine.UI.LayoutElement>();
            if (layoutElement != null)
            {
                if (width <= 0) width = layoutElement.preferredWidth > 0 ? layoutElement.preferredWidth : 100f;
                if (height <= 0) height = layoutElement.preferredHeight > 0 ? layoutElement.preferredHeight : 100f;
            }
            else
            {
                // 使用預設尺寸
                width = width <= 0 ? 100f : width;
                height = height <= 0 ? 100f : height;
            }
        }
        
        // 設置錨點：完全填充父物件
        borderRect.anchorMin = new Vector2(0f, 0f);
        borderRect.anchorMax = new Vector2(1f, 1f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 設置偏移，讓邊框比背景大 borderWidth
        borderRect.offsetMin = new Vector2(-borderWidth, -borderWidth);
        borderRect.offsetMax = new Vector2(borderWidth, borderWidth);
        
        // 確保邊框在最底層
        borderRect.SetAsFirstSibling();
        
        // 自動獲取或添加 Image 組件
        Image borderImage = borderRect.GetComponent<Image>();
        if (borderImage == null)
        {
            borderImage = borderRect.gameObject.AddComponent<Image>();
        }
        
        if (borderImage != null)
        {
            borderImage.color = borderColor;
        }
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
                //Debug.Log($"[WeaponSlotUI] ✓ Set weapon sprite for {weapon.ItemName}: {weaponSprite.name} → UI Image component");
                
                // 驗證設置成功
                if (weaponIcon.sprite == weaponSprite)
                {
                    //Debug.Log($"[WeaponSlotUI] ✓ Verified: UI Image.sprite is correctly set to {weaponSprite.name}");
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
        
        //Debug.Log($"[WeaponSlotUI] Getting sprite for weapon: {weapon.ItemName}");
        
        // 1. 嘗試從 SpriteRenderer 取得（武器 prefab 通常有 SpriteRenderer）
        var spriteRenderer = weapon.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            //Debug.Log($"[WeaponSlotUI] Found SpriteRenderer on {weapon.ItemName}");
            if (spriteRenderer.sprite != null)
            {
                //Debug.Log($"[WeaponSlotUI] ✓ Using sprite from SpriteRenderer: {spriteRenderer.sprite.name}");
                return spriteRenderer.sprite;
            }
            else
            {
                Debug.LogWarning($"[WeaponSlotUI] SpriteRenderer found but sprite is null on {weapon.ItemName}");
            }
        }
        else
        {
            //Debug.Log($"[WeaponSlotUI] No SpriteRenderer found on {weapon.ItemName} root");
        }
        
        // 2. 嘗試從子物件的 SpriteRenderer 取得
        spriteRenderer = weapon.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            //Debug.Log($"[WeaponSlotUI] Found SpriteRenderer in children of {weapon.ItemName}");
            if (spriteRenderer.sprite != null)
            {
                //Debug.Log($"[WeaponSlotUI] ✓ Using sprite from child SpriteRenderer: {spriteRenderer.sprite.name}");
                return spriteRenderer.sprite;
            }
            else
            {
                Debug.LogWarning($"[WeaponSlotUI] Child SpriteRenderer found but sprite is null on {weapon.ItemName}");
            }
        }
        else
        {
            //Debug.Log($"[WeaponSlotUI] No SpriteRenderer found in children of {weapon.ItemName}");
        }
        
        // 3. 退而求其次：使用 ItemIcon（如果有設定）
        if (weapon.ItemIcon != null)
        {
            //Debug.Log($"[WeaponSlotUI] ✓ Using ItemIcon as fallback: {weapon.ItemIcon.name}");
            return weapon.ItemIcon;
        }
        else
        {
            //Debug.Log($"[WeaponSlotUI] ItemIcon is also null on {weapon.ItemName}");
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
        if (backgroundRect != null)
        {
            Image backgroundImage = backgroundRect.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : unselectedColor;
            }
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

