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

    [Header("Bones")]
    [SerializeField] private Transform torso;
    [SerializeField] private Transform chest;
    [SerializeField, Range(0f, 1f)] private float upperBodyWeight = 0.5f;

    private Vector2 speed = Vector2.zero;
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
        direction = playerMovement.MoveInput;
        weaponDirection = player.GetWeaponDirection().normalized;

        speed.x = Vector2.Dot(direction, new Vector2(weaponDirection.y, -weaponDirection.x));
        speed.y = Vector2.Dot(direction, weaponDirection);

        if (playerMovement.IsCameraMode)
            speed = Vector2.zero;


        animator.SetFloat("moveX", speed.x);
        animator.SetFloat("moveY", speed.y);

        animator.SetBool("isRunning", playerMovement.IsRunning);
        animator.SetBool("isWalking", (speed.magnitude > 0f));
        animator.SetBool("isCrouching", playerMovement.IsSquatting); 


    }

    void LateUpdate()
    {
        // Vector3 aimDir = new Vector3(weaponDirection.x, weaponDirection.y, 0f);

        // if (aimDir.sqrMagnitude > 0.001f)
        // {
        //     Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
        //     chest.localRotation = Quaternion.Euler(0f, targetRot.eulerAngles.y - torso.rotation.eulerAngles.y, 0f);
        // }
    }

}
