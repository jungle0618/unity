using UnityEngine;
using System;

public class EnemyAnimationController : MonoBehaviour
{   
    [Header("Settings")]
    public float Hurt2AnimationThreshold = 1f;
    [Header("References")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject gun;
    private Weapon weapon = null;
    private float speed = 0f;

    private Action StartMovingHandler;
    private Action StopMovingHandler;

    private Action EquipItemHandler;
    private Action UnequipItemHandler;
    
    private Action<int, int> OnHealthChangedHandler;

    private Action<GameObject> OnAttackPerformedHandler;

    private void toggleItemVisibility(GameObject item, bool visible)
    {
        foreach (var r in item.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
    }

    public void OnEnable()
    {
        animator = GetComponent<Animator>();
        if (gun == null)
            gun = GetComponentInChildren<RangedWeapon>(true).gameObject;

        StartMovingHandler = () => {animator.SetBool("isMoving", true);};
        StopMovingHandler = () => {animator.SetBool("isMoving", false);};

        EquipItemHandler = () => {toggleItemVisibility(gun, true); animator.SetInteger("weaponState", 1);};
        UnequipItemHandler = () => {toggleItemVisibility(gun, false); animator.SetInteger("weaponState", -1);};

        OnHealthChangedHandler = (current, max) => {
            animator.SetTrigger((float)current/ (float)max < Hurt2AnimationThreshold ? "Hurt2" : "Hurt");
            VFXManager.Instance.PlayBloodSplatKnifeVFXHandler(enemy.transform);
        };
        OnAttackPerformedHandler = (attacker) => {
            animator.SetTrigger("Shoot");
            VFXManager.Instance.PlayMuzzleFlashVFXHandler(weapon.gameObject);
            VFXManager.Instance.PlayScreenShakeVFXHandler();
        };

        if (enemy != null)
        {
            enemy.OnStartedChasing += EquipItemHandler;
            // enemy.OnStoppedChasing += UnequipItemHandler;
            enemy.OnEnteredPatrol += UnequipItemHandler;
            enemy.OnEnteredSearch += EquipItemHandler;
            enemy.OnEnteredReturn += UnequipItemHandler;

            enemy.OnStartedMoving += StartMovingHandler;
            enemy.OnStoppedMoving += StopMovingHandler;

            enemy.OnHealthChanged += OnHealthChangedHandler;
            enemy.OnEnemyDied += VFXManager.Instance.PlayDeathVFXHandler;
        }

        toggleItemVisibility(gun, false);
    }

    public void Start()
    {
        if (enemy.ItemHolder != null && enemy.ItemHolder.CurrentWeapon != null)
        {
            weapon = enemy.ItemHolder.CurrentWeapon;
            weapon.OnAttackPerformed += OnAttackPerformedHandler;
            weapon.OnAttackPerformed += VFXManager.Instance.PlayMuzzleFlashVFXHandler;
        }
    }

    public void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnStartedChasing -= EquipItemHandler;
            // enemy.OnStoppedChasing -= UnequipItemHandler;
            enemy.OnEnteredPatrol -= UnequipItemHandler;
            enemy.OnEnteredAlert -= EquipItemHandler;
            enemy.OnEnteredSearch -= EquipItemHandler;
            enemy.OnEnteredReturn -= UnequipItemHandler;

            enemy.OnStartedMoving -= StartMovingHandler;
            enemy.OnStoppedMoving -= StopMovingHandler;

            enemy.OnHealthChanged -= OnHealthChangedHandler;
            if (VFXManager.Instance != null)
            {
                enemy.OnEnemyDied -= VFXManager.Instance.PlayDeathVFXHandler;
            }      
        }
        if (weapon != null)
        {
            weapon.OnAttackPerformed -= OnAttackPerformedHandler;
            weapon.OnAttackPerformed -= VFXManager.Instance.PlayMuzzleFlashVFXHandler;
        }
    }
}
