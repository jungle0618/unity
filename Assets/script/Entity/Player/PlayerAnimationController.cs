using UnityEngine;
using System;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Settings")]
    public float Hurt2AnimationThreshold = 0.5f;
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject gun;
    [SerializeField] private GameObject knife;

    private float speed = 0f;
    private Vector2 direction = Vector2.zero;
    private Vector2 weaponDirection = Vector2.zero;
    private Action<Item> OnEquipChangedHandler;
    
    private Action<int, int> OnHealthChangedHandler;
    
    private Action<Weapon> OnWeaponAttackHandler;
    private Action OnActionPerformedHandler;

    public void OnEnable()
    {
        animator = GetComponent<Animator>();
        if (gun == null)
            gun = GetComponentInChildren<RangedWeapon>(true).gameObject;
        if (knife == null)
            knife = GetComponentInChildren<MeleeWeapon>(true).gameObject;

        OnEquipChangedHandler = (item) => {
            int isMelee = item is MeleeWeapon ? 0 : 1;
            if (item is EmptyHands) isMelee = -1;
            knife.SetActive(isMelee == 0);
            gun.SetActive(isMelee == 1);
            animator.SetInteger("weaponState", isMelee);
        };

        OnHealthChangedHandler = (current, max) => {animator.SetTrigger((float)current/ (float)max < Hurt2AnimationThreshold ? "Hurt2" : "Hurt");};
        
        OnWeaponAttackHandler = (weapon) => {animator.SetTrigger(weapon is RangedWeapon ? "Shoot" : "Slash");};
        OnActionPerformedHandler = () => {animator.SetTrigger("Interact");};

        if (player != null)
        {
            player.OnEquipChanged += OnEquipChangedHandler;

            player.OnHealthChanged += OnHealthChangedHandler;
            player.OnPlayerDied += VFXManager.Instance.PlayerPlayDeathVFXHandler;
            player.OnWeaponAttack += OnWeaponAttackHandler;
            player.OnItemUse += OnActionPerformedHandler;
        }

        knife.SetActive(false);
        gun.SetActive(false);
    }

    public void OnDisable()
    {
        if (player != null)
        {
            player.OnEquipChanged -= OnEquipChangedHandler;

            player.OnHealthChanged -= OnHealthChangedHandler;
            player.OnPlayerDied -= VFXManager.Instance.PlayerPlayDeathVFXHandler;
            player.OnWeaponAttack -= OnWeaponAttackHandler;
            player.OnItemUse -= OnActionPerformedHandler;
        }
    }

    public void Update()
    {   
        speed = playerMovement.GetSpeed();
        direction = playerMovement.MoveInput;
        weaponDirection = player.GetWeaponDirection().normalized;

        animator.SetFloat("moveX", Vector2.Dot(direction, new Vector2(weaponDirection.y, -weaponDirection.x)));
        animator.SetFloat("moveY", Vector2.Dot(direction, weaponDirection));

        if (playerMovement.IsCameraMode) {
            animator.SetFloat("moveX", 0f);
            animator.SetFloat("moveY", 0f);
        }

        animator.SetBool("isRunning", playerMovement.IsRunning);
        animator.SetBool("isWalking", (speed > 0f));
        animator.SetBool("isCrouching", playerMovement.IsSquatting);   
    }
}
