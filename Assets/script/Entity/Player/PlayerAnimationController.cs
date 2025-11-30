using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private ItemHolder itemHolder;
    [SerializeField] private GameObject gun;
    [SerializeField] private GameObject knife;
    [SerializeField] private ParticleSystem muzzleFlash;

    private int currentWeapon = -1;
    private float speed = 0f;
    private Vector2 direction = Vector2.zero;
    private Vector2 relativeDirection = Vector2.zero;
    private Vector2 weaponDirection = Vector2.zero;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void TriggerAttackAnimation3D()
    {   
        if (currentWeapon == -1) return;
        if(currentWeapon == 0)
        {
            animator.SetTrigger("Slash");
        }
        else
        {
            animator.SetTrigger("Shoot");
        }
    }

    public void TriggerInteractAnimation()
    {
        animator.SetTrigger("Interact");
    }

    // Update is called once per frame
    void Update()
    {  
        speed = playerMovement.GetSpeed();
        direction = playerMovement.MoveInput;
        if (itemHolder.CurrentWeapon != null)
        {
            currentWeapon = itemHolder.CurrentWeapon is MeleeWeapon ? 0 : 1;
        }
        else
        {
            currentWeapon = -1;
        }
        weaponDirection = player.GetWeaponDirection();

        relativeDirection = weaponDirection.normalized - direction.normalized;

        animator.SetInteger("weaponState", currentWeapon);

        gun.SetActive(currentWeapon == 1);
        knife.SetActive(currentWeapon == 0);

        relativeDirection.x =  Vector2.Dot(direction, new Vector2(weaponDirection.y, -weaponDirection.x));
        relativeDirection.y =  Vector2.Dot(direction, weaponDirection);

        if (speed > 0f) {
            animator.SetFloat("moveX", relativeDirection.x);
            animator.SetFloat("moveY", relativeDirection.y);
        }

        animator.SetBool("isRunning", playerMovement.IsRunning);
        animator.SetBool("isWalking", (speed > 0f));
        animator.SetBool("isCrouching", playerMovement.IsSquatting);   
    }
}
