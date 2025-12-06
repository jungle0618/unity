using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private Animator animator;
    private int currentWeapon = -1;
    private float speed = 0f;

    public void TriggerAttackAnimation3D()
    {   
        if (currentWeapon == -1) return;
        animator.SetTrigger("Shoot");
    }

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        
        animator.SetFloat("Speed", speed);

        animator.SetInteger("weaponState", 1);
    }
}
