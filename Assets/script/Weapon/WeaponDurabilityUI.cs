using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 武器耐久度UI顯示器
/// 可以訂閱WeaponHolder的耐久度事件來顯示武器耐久度
/// </summary>
public class WeaponDurabilityUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider durabilitySlider;
    [SerializeField] private TextMeshProUGUI durabilityText;
    [SerializeField] private Image durabilityFillImage;
    
    [Header("Color Settings")]
    [SerializeField] private Color highDurabilityColor = Color.green;
    [SerializeField] private Color mediumDurabilityColor = Color.yellow;
    [SerializeField] private Color lowDurabilityColor = Color.red;
    [SerializeField] private float lowDurabilityThreshold = 0.3f;
    [SerializeField] private float mediumDurabilityThreshold = 0.6f;
    
    [Header("Target Weapon Holder")]
    [SerializeField] private WeaponHolder weaponHolder;
    
    private void Start()
    {
        // 如果沒有指定weaponHolder，嘗試從父物件或場景中找到
        if (weaponHolder == null)
        {
            weaponHolder = GetComponentInParent<WeaponHolder>();
            if (weaponHolder == null)
            {
                weaponHolder = FindFirstObjectByType<WeaponHolder>();
            }
        }
        
        // 訂閱耐久度事件
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponDurabilityChanged += OnDurabilityChanged;
            weaponHolder.OnWeaponBroken += OnWeaponBroken;
            
            // 初始化顯示
            var durabilityInfo = weaponHolder.GetWeaponDurabilityInfo();
            UpdateDurabilityDisplay(durabilityInfo.current, durabilityInfo.max);
        }
        else
        {
            Debug.LogWarning("WeaponDurabilityUI: 找不到WeaponHolder組件！");
        }
    }
    
    private void OnDestroy()
    {
        // 取消訂閱事件
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponDurabilityChanged -= OnDurabilityChanged;
            weaponHolder.OnWeaponBroken -= OnWeaponBroken;
        }
    }
    
    /// <summary>
    /// 處理耐久度變化事件
    /// </summary>
    private void OnDurabilityChanged(int currentDurability, int maxDurability)
    {
        UpdateDurabilityDisplay(currentDurability, maxDurability);
    }
    
    /// <summary>
    /// 處理武器損壞事件
    /// </summary>
    private void OnWeaponBroken()
    {
        UpdateDurabilityDisplay(0, 0);
        Debug.Log("武器已損壞！");
    }
    
    /// <summary>
    /// 更新耐久度顯示
    /// </summary>
    private void UpdateDurabilityDisplay(int currentDurability, int maxDurability)
    {
        if (maxDurability <= 0)
        {
            // 沒有武器或武器損壞
            if (durabilitySlider != null)
            {
                durabilitySlider.value = 0f;
                durabilitySlider.gameObject.SetActive(false);
            }
            
            if (durabilityText != null)
            {
                durabilityText.text = "無武器";
            }
            
            return;
        }
        
        float durabilityPercentage = (float)currentDurability / maxDurability;
        
        // 更新滑桿
        if (durabilitySlider != null)
        {
            durabilitySlider.value = durabilityPercentage;
            durabilitySlider.gameObject.SetActive(true);
        }
        
        // 更新文字
        if (durabilityText != null)
        {
            durabilityText.text = $"{currentDurability}/{maxDurability}";
        }
        
        // 更新顏色
        if (durabilityFillImage != null)
        {
            if (durabilityPercentage <= lowDurabilityThreshold)
            {
                durabilityFillImage.color = lowDurabilityColor;
            }
            else if (durabilityPercentage <= mediumDurabilityThreshold)
            {
                durabilityFillImage.color = mediumDurabilityColor;
            }
            else
            {
                durabilityFillImage.color = highDurabilityColor;
            }
        }
    }
    
    /// <summary>
    /// 設定目標WeaponHolder
    /// </summary>
    public void SetWeaponHolder(WeaponHolder holder)
    {
        // 取消訂閱舊的holder
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponDurabilityChanged -= OnDurabilityChanged;
            weaponHolder.OnWeaponBroken -= OnWeaponBroken;
        }
        
        weaponHolder = holder;
        
        // 訂閱新的holder
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponDurabilityChanged += OnDurabilityChanged;
            weaponHolder.OnWeaponBroken += OnWeaponBroken;
            
            // 更新顯示
            var durabilityInfo = weaponHolder.GetWeaponDurabilityInfo();
            UpdateDurabilityDisplay(durabilityInfo.current, durabilityInfo.max);
        }
    }
}
