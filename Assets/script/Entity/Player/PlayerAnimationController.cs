using UnityEngine;
using System;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Settings")]
    public float Hurt2AnimationThreshold = 1f;
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject gun;
    [SerializeField] private GameObject knife;

    [Header("Bones")]
    [SerializeField] private Transform chest;

    [Header("Crouch Visual")]
    [SerializeField] private Transform hips;
    [SerializeField] private Transform torso;

    [SerializeField] private float crouchDrop = 0.5f;
    [SerializeField] private float crouchTorsoLean = 6f;
    [SerializeField] private float crouchSpeed = 10f;

    private Vector3 hipsLocalStartPos;
    private Quaternion torsoLocalStartRot;

    private Vector2 speed = Vector2.zero;
    private Vector2 direction = Vector2.zero;
    private Vector2 weaponDirection = Vector2.zero;
    private Action<Item> OnEquipChangedHandler;
    
    private Action<int, int> OnHealthChangedHandler;
    
    private Action<Weapon> OnWeaponAttackHandler;
    private Action<bool> OnItemUseHandler;

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

        OnHealthChangedHandler = (current, max) => {
            animator.SetTrigger((float)current/ (float)max < Hurt2AnimationThreshold ? "Hurt2" : "Hurt");
            VFXManager.Instance.PlayBloodSplatKnifeVFXHandler(player.transform);
        };
        
        OnWeaponAttackHandler = (weapon) => {
            if (weapon is RangedWeapon) VFXManager.Instance.PlayerPlayMuzzleFlashVFXHandler(weapon); 
            animator.SetTrigger(weapon is RangedWeapon ? "Shoot" : "Slash");
        };
        OnItemUseHandler = (success) => {animator.SetTrigger("Interact");};

        if (player != null)
        {
            player.OnEquipChanged += OnEquipChangedHandler;

            player.OnHealthChanged += OnHealthChangedHandler;
            player.OnPlayerDied += VFXManager.Instance.PlayerPlayDeathVFXHandler;
            player.OnWeaponAttack += OnWeaponAttackHandler;
            player.OnItemUse += OnItemUseHandler;
        }

        knife.SetActive(false);
        gun.SetActive(false);

        hipsLocalStartPos = hips.localPosition;
        torsoLocalStartRot = torso.localRotation;
    }

    public void OnDisable()
    {
        if (player != null)
        {
            player.OnEquipChanged -= OnEquipChangedHandler;

            player.OnHealthChanged -= OnHealthChangedHandler;
            player.OnWeaponAttack -= OnWeaponAttackHandler;
            player.OnItemUse -= OnItemUseHandler;

            if (VFXManager.Instance != null)
            {
                player.OnPlayerDied -= VFXManager.Instance.PlayerPlayDeathVFXHandler;
            }     
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
    //     if (playerMovement.IsCameraMode)
    //         return;
        
    //     float x = Vector2.Dot(weaponDirection, new Vector2(direction.y, -direction.x));
    //     float y = Vector2.Dot(weaponDirection, direction);

    //     Debug.Log($"Weapon Dir: {weaponDirection}, Move Dir: {direction}, x: {x}, y: {y}");

    //     Vector3 flatDir = new Vector3(x, 0f, y);

    //     if (flatDir.sqrMagnitude > 0.001f)
    //     {
    //         float targetYaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;

    //         chest.rotation = Quaternion.Euler(0f, 0f, targetYaw);
    //     }
        ApplyFakeCrouch(playerMovement.IsSquatting);
    }

    void ApplyFakeCrouch(bool crouching)
    {
        // TARGET HIP HEIGHT (WORLD-DOWN)
        Vector3 targetHips =
            crouching
            ? hipsLocalStartPos + Vector3.down * crouchDrop
            : hipsLocalStartPos;

        hips.localPosition = Vector3.Lerp(
            hips.localPosition,
            targetHips,
            Time.deltaTime * crouchSpeed
        );

        // TARGET TORSO LEAN (SLIGHT FORWARD)
        Quaternion targetTorso =
            crouching
            ? torsoLocalStartRot * Quaternion.Euler(crouchTorsoLean, 0f, 0f)
            : torsoLocalStartRot;

        torso.localRotation = Quaternion.Slerp(
            torso.localRotation,
            targetTorso,
            Time.deltaTime * crouchSpeed
        );
    }
}
