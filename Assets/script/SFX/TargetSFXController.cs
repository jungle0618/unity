using UnityEngine;
using System;

public class TargetSFXController : MonoBehaviour
{
    [Header("Settings")]
    public float footstepSpeed = 1f;

    [Header("References")]
    [SerializeField] private Target target;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip bloodSplatClip;
    [SerializeField] private AudioClip deathClip;
    
    private Weapon weapon = null;
    private AudioSource audioSource = null;

    private Action StartMovingHandler;
    private Action StopMovingHandler;
    
    private Action<int, int> OnHealthChangedHandler;
    private Action<Target> OnTargetDiedHandler;

    public void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();

        StartMovingHandler = () => {
            SFXManager.Instance.PlaySFXatSource(footstepClip, audioSource, footstepSpeed, true);
        };
        StopMovingHandler = () => {
            SFXManager.Instance.StopSFXatSource(audioSource);
        };


        OnHealthChangedHandler = (current, max) => {
            SFXManager.Instance.PlaySFX(hurtClip, target.transform.position, 1f);
            SFXManager.Instance.PlaySFX(bloodSplatClip, target.transform.position, 1f);
        };
        OnTargetDiedHandler = (target) => {
            SFXManager.Instance.PlaySFX(deathClip, target.transform.position, 1f);
            SFXManager.Instance.PlaySFX(bloodSplatClip, target.transform.position, 1f);
        };

        if (target != null)
        {
            target.OnStartedMoving += StartMovingHandler;
            target.OnStoppedMoving += StopMovingHandler;
            target.OnHealthChanged += OnHealthChangedHandler;
            target.OnTargetDied += OnTargetDiedHandler;
        }
    }

    public void OnDisable()
    {
        if (target != null)
        {
            target.OnStartedMoving -= StartMovingHandler;
            target.OnStoppedMoving -= StopMovingHandler;
            target.OnHealthChanged -= OnHealthChangedHandler;
            target.OnTargetDied -= OnTargetDiedHandler;
        }
    }
}