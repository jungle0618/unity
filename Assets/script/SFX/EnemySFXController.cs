using UnityEngine;
using System;

public class EnemySFXController : MonoBehaviour
{
    [Header("Settings")]
    public float footstepSpeed = 1f;

    [Header("References")]
    [SerializeField] private Enemy enemy;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip gunEquipClip;
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip bloodSplatClip;
    [SerializeField] private AudioClip deathClip;
    
    private Weapon weapon = null;
    private AudioSource audioSource = null;

    private Action StartMovingHandler;
    private Action StopMovingHandler;

    private Action EquipItemHandler;
    private Action UnequipItemHandler;
    
    private Action<int, int> OnHealthChangedHandler;
    private Action<Enemy> OnEnemyDiedHandler;

    private Action<GameObject> OnAttackPerformedHandler;

    public void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();

        StartMovingHandler = () => {
            SFXManager.Instance.PlaySFXatSource(footstepClip, audioSource, footstepSpeed, true);
        };
        StopMovingHandler = () => {
            SFXManager.Instance.StopSFXatSource(audioSource);
        };

        EquipItemHandler = () => {SFXManager.Instance.PlaySFX(gunEquipClip, enemy.transform.position, 1f);};
        UnequipItemHandler = () =>{SFXManager.Instance.PlaySFX(gunEquipClip, enemy.transform.position, 1f);};

        OnHealthChangedHandler = (current, max) => {
            SFXManager.Instance.PlaySFX(hurtClip, enemy.transform.position, 1f);
            SFXManager.Instance.PlaySFX(bloodSplatClip, enemy.transform.position, 1f);
        };
        OnAttackPerformedHandler = (attacker) => {SFXManager.Instance.PlaySFX(gunshotClip, enemy.transform.position, 1f);};
        OnEnemyDiedHandler = (enemy) => {
            SFXManager.Instance.PlaySFX(deathClip, enemy.transform.position, 1f);
            SFXManager.Instance.PlaySFX(bloodSplatClip, enemy.transform.position, 1f);
        };

        if (enemy != null)
        {
            enemy.OnStartedChasing += EquipItemHandler;
            enemy.OnStoppedChasing += UnequipItemHandler;
            enemy.OnEnteredPatrol += UnequipItemHandler;
            enemy.OnEnteredSearch += EquipItemHandler;
            enemy.OnEnteredReturn += UnequipItemHandler;

            enemy.OnStartedMoving += StartMovingHandler;
            enemy.OnStoppedMoving += StopMovingHandler;
            enemy.OnHealthChanged += OnHealthChangedHandler;
            enemy.OnEnemyDied += OnEnemyDiedHandler;
        }
    }

    public void Start()
    {
        if (enemy.ItemHolder != null && enemy.ItemHolder.CurrentWeapon != null)
        {
            weapon = enemy.ItemHolder.CurrentWeapon;
            weapon.OnAttackPerformed += OnAttackPerformedHandler;
        }
    }

    public void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnStartedChasing -= EquipItemHandler;
            enemy.OnStoppedChasing -= UnequipItemHandler;
            enemy.OnEnteredPatrol -= UnequipItemHandler;
            enemy.OnEnteredSearch -= EquipItemHandler;
            enemy.OnEnteredReturn -= UnequipItemHandler;
    
            enemy.OnStartedMoving -= StartMovingHandler;
            enemy.OnStoppedMoving -= StopMovingHandler;
            enemy.OnHealthChanged -= OnHealthChangedHandler;
            enemy.OnEnemyDied -= OnEnemyDiedHandler;
        }

        if (weapon != null)
        {
            weapon.OnAttackPerformed -= OnAttackPerformedHandler;
        }
    }
}