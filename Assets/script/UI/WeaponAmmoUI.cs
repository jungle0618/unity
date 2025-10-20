using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Weapon Ammo UI - Displays current weapon and ammunition
/// Shows bullet count for guns and weapon name for all weapons
/// </summary>
public class WeaponAmmoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image weaponIcon;
    
    [Header("Settings")]
    [SerializeField] private bool showWeaponName = true;
    [SerializeField] private bool showAmmoForGunOnly = true;
    
    private WeaponHolder weaponHolder;
    private Gun currentGun;
    
    private void Start()
    {
        // Find the player's weapon holder
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            weaponHolder = player.GetComponent<WeaponHolder>();
            if (weaponHolder != null)
            {
                // Subscribe to weapon changes
                weaponHolder.OnWeaponChanged += OnWeaponChanged;
                
                // Initialize with current weapon
                OnWeaponChanged(weaponHolder.CurrentWeapon);
            }
        }
        
        // Initialize UI
        UpdateUI();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponChanged -= OnWeaponChanged;
        }
        
        if (currentGun != null)
        {
            currentGun.OnAmmoChanged -= OnAmmoChanged;
        }
    }
    
    /// <summary>
    /// Handle weapon change
    /// </summary>
    private void OnWeaponChanged(Weapon newWeapon)
    {
        // Unsubscribe from old gun
        if (currentGun != null)
        {
            currentGun.OnAmmoChanged -= OnAmmoChanged;
            currentGun = null;
        }
        
        // Check if new weapon is a gun
        if (newWeapon != null)
        {
            currentGun = newWeapon as Gun;
            if (currentGun != null)
            {
                // Subscribe to ammo changes
                currentGun.OnAmmoChanged += OnAmmoChanged;
            }
        }
        
        // Update UI
        UpdateUI();
    }
    
    /// <summary>
    /// Handle ammo change
    /// </summary>
    private void OnAmmoChanged(int currentAmmo, int maxAmmo)
    {
        UpdateUI();
    }
    
    /// <summary>
    /// Update the UI display
    /// </summary>
    private void UpdateUI()
    {
        if (weaponHolder == null || weaponHolder.CurrentWeapon == null)
        {
            // No weapon equipped
            if (weaponNameText != null) weaponNameText.text = "No Weapon";
            if (ammoText != null) ammoText.text = "";
            if (weaponIcon != null) weaponIcon.enabled = false;
            return;
        }
        
        Weapon weapon = weaponHolder.CurrentWeapon;
        
        // Update weapon name
        if (showWeaponName && weaponNameText != null)
        {
            weaponNameText.text = "Current Weapon: ";
            weaponNameText.text += weapon.gameObject.name.Replace("(Clone)", "").Trim();
        }
        
        // Update ammo display
        if (ammoText != null)
        {
            if (currentGun != null)
            {
                // Show ammo for gun
                ammoText.text = $"Ammo Left: {currentGun.CurrentAmmo} / {currentGun.MaxAmmo}";
                ammoText.gameObject.SetActive(true);
            }
            else if (!showAmmoForGunOnly)
            {
                // Show "Melee" or durability for non-gun weapons
                ammoText.text = "Melee";
                ammoText.gameObject.SetActive(true);
            }
            else
            {
                // Hide ammo text for non-gun weapons
                ammoText.gameObject.SetActive(false);
            }
        }
        
        // Update weapon icon (if you have weapon sprites)
        if (weaponIcon != null)
        {
            weaponIcon.enabled = true;
            // You can set the sprite here if you have weapon icons
        }
    }
}

