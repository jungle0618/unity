using UnityEngine;
using System;

public class EnemyAnimationController : MonoBehaviour
{   
    [Header("Settings")]
    public float Hurt2AnimationThreshold = 0.5f;
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

    public void OnEnable()
    {
        animator = GetComponent<Animator>();
        if (gun == null)
            gun = GetComponentInChildren<RangedWeapon>(true).gameObject;

        StartMovingHandler = () => {Debug.Log($"{enemy.name}: Started Moving"); animator.SetBool("isMoving", true);};
        StopMovingHandler = () => {Debug.Log($"{enemy.name}: Stopped Moving"); animator.SetBool("isMoving", false);};

        EquipItemHandler = () => {gun.SetActive(true); animator.SetInteger("weaponState", 1);};
        UnequipItemHandler = () => {gun.SetActive(false); animator.SetInteger("weaponState", -1);};

        OnHealthChangedHandler = (current, max) => {animator.SetTrigger((float)current/ (float)max < Hurt2AnimationThreshold ? "Hurt2" : "Hurt");};
        OnAttackPerformedHandler = (attacker) => {animator.SetTrigger("Shoot");};

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
            enemy.OnEnemyDied += VFXManager.Instance.PlayDeathVFXHandler;
        }

        gun.SetActive(false);
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
            enemy.OnEnteredAlert -= EquipItemHandler;
            enemy.OnEnteredSearch -= EquipItemHandler;
            enemy.OnEnteredReturn -= UnequipItemHandler;

            enemy.OnStartedMoving -= StartMovingHandler;
            enemy.OnStoppedMoving -= StopMovingHandler;

            enemy.OnHealthChanged -= OnHealthChangedHandler;
            enemy.OnEnemyDied -= VFXManager.Instance.PlayDeathVFXHandler;        
        }
        if (weapon != null)
        {
            weapon.OnAttackPerformed -= OnAttackPerformedHandler;
        }
    }
}
