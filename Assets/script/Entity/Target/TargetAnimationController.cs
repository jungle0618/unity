using UnityEngine;
using System;

public class TargetAnimationController : MonoBehaviour
{   
    [Header("Settings")]
    public float Hurt2AnimationThreshold = 1f;
    [Header("References")]
    [SerializeField] private Target target;
    [SerializeField] private Animator animator;
    private float speed = 0f;

    private Action StartMovingHandler;
    private Action StopMovingHandler;
    
    private Action<int, int> OnHealthChangedHandler;

    public void OnEnable()
    {
        animator = GetComponent<Animator>();

        StartMovingHandler = () => {animator.SetBool("isMoving", true);};
        StopMovingHandler = () => {animator.SetBool("isMoving", false);};

        OnHealthChangedHandler = (current, max) => {
            animator.SetTrigger((float)current/ (float)max < Hurt2AnimationThreshold ? "Hurt2" : "Hurt");
            VFXManager.Instance.PlayBloodSplatKnifeVFXHandler(target.transform);
        };


        if (target != null)
        {

            target.OnStartedMoving += StartMovingHandler;
            target.OnStoppedMoving += StopMovingHandler;

            target.OnHealthChanged += OnHealthChangedHandler;
            target.OnTargetDied += VFXManager.Instance.PlayDeathVFXHandler;
        }

        animator.SetBool("isMoving", false);
    }

    public void Start()
    {
    }

    public void OnDisable()
    {
        if (target != null)
        {
            target.OnStartedMoving -= StartMovingHandler;
            target.OnStoppedMoving -= StopMovingHandler;

            target.OnHealthChanged -= OnHealthChangedHandler;
            target.OnTargetDied -= VFXManager.Instance.PlayDeathVFXHandler;        
        }
    }
}
