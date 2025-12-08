using UnityEngine;
using System;

public class PlayerSFXController : MonoBehaviour
{
    [Header("Settings")]
    public float footstepSpeed = 1f;
    public float footstepCrouchSpeed = 0.6f;

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip gunEquipClip;
    [SerializeField] private AudioClip knifeEquipClip;
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private AudioClip knifeSwingClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip bloodSplatClip;
    [SerializeField] private AudioClip interactClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip doorOpenFail;
    [SerializeField] private AudioClip doorOpenSuccess;
    
    private AudioSource audioSource = null;

    private Action StartMovingHandler;
    private Action StopMovingHandler;
    private Action<Item> OnEquipChangedHandler;
    
    private Action<int, int> OnHealthChangedHandler;
    
    private Action<Weapon> OnWeaponAttackHandler;
    private Action<bool> OnItemUseHandler;

    private Action OnPlayerDiedHandler;


    public void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();

        StartMovingHandler = () => {
            SFXManager.Instance.PlaySFXatSource(footstepClip, audioSource, footstepSpeed * (playerMovement.IsSquatting ? footstepCrouchSpeed : 1f), true);
        };
        StopMovingHandler = () => {
            SFXManager.Instance.StopSFXatSource(audioSource);
        };
        OnEquipChangedHandler = (item) => {
            if (item is RangedWeapon)
                SFXManager.Instance.PlaySFX(gunEquipClip, player.transform.position, 1f);
            else if (item is MeleeWeapon)
                SFXManager.Instance.PlaySFX(knifeEquipClip, player.transform.position, 1f);
        };
        OnHealthChangedHandler = (current, max) => {
            SFXManager.Instance.PlaySFX(hurtClip, player.transform.position, 1f);
            SFXManager.Instance.PlaySFX(bloodSplatClip, player.transform.position, 1f);
        };
        OnWeaponAttackHandler = (weapon) => {
            if (weapon is RangedWeapon)
                SFXManager.Instance.PlaySFX(gunshotClip, player.transform.position, 1f);
            else if (weapon is MeleeWeapon)
                SFXManager.Instance.PlaySFX(knifeSwingClip, player.transform.position, 1f);
        };
        OnItemUseHandler= (success) => {
            if (success)
                SFXManager.Instance.PlaySFX(doorOpenSuccess, player.transform.position, 1f);
            else
                SFXManager.Instance.PlaySFX(doorOpenFail, player.transform.position, 1f);
        };

        OnPlayerDiedHandler = () => {
            SFXManager.Instance.PlaySFX(deathClip, player.transform.position, 1f);
            SFXManager.Instance.PlaySFX(bloodSplatClip, player.transform.position, 1f);    
        };

        if (player != null)
        {
            player.OnStartedMoving += StartMovingHandler;
            player.OnStoppedMoving += StopMovingHandler;
            player.OnEquipChanged += OnEquipChangedHandler;
            player.OnHealthChanged += OnHealthChangedHandler;
            player.OnWeaponAttack += OnWeaponAttackHandler;
            player.OnItemUse += OnItemUseHandler;
        }
    }

    public void OnDisable()
    {
        if (player != null)
        {
            player.OnStartedMoving -= StartMovingHandler;
            player.OnStoppedMoving -= StopMovingHandler;
            player.OnEquipChanged -= OnEquipChangedHandler;
            player.OnHealthChanged -= OnHealthChangedHandler;
            player.OnWeaponAttack -= OnWeaponAttackHandler;
            player.OnItemUse -= OnItemUseHandler;
        }
    }
}