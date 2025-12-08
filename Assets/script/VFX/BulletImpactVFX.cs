using UnityEngine;
using System;

public class BulletImpactVFX : MonoBehaviour
{
    private Bullet bullet;


    public void OnEnable()
    {
        bullet = GetComponentInParent<Bullet>();
        if (bullet != null)
        {
            bullet.OnBulletHit += VFXManager.Instance.PlayBulletImpactVFXHandler;
        }
    }
}
