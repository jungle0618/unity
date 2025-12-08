using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Death VFX Settings")]
    public float fadeDelay = 0.2f;
    public float fadeDuration = 0.6f;

    [Header("Muzzle Flash VFX Settings")]
    public GameObject muzzleFlashVFXPrefab;
    [SerializeField] private float offsetFromGun = 0.5f;

    [Header("Blood Splat VFX Settings")]
    [SerializeField] private ParticleSystem bloodSplatKnifeVFXPrefab;
    
    [Header("Bullet Impact VFX Settings")]
    public float zOffset = -2f;
    [SerializeField] private ParticleSystem bloodSplatGunVFXPrefab;
    [SerializeField] private ParticleSystem objectHitVFXPrefab;

    [Header("Screen Shake VFX Settings")]
    [SerializeField] private ScreenShakeVFX screenShakeVFX;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // optional if you want it persistent
    }


    private void MakeMaterialTransparent(Material mat)
    {
        if (!mat.HasProperty("_Color"))
            return;

        // Only switch once
        mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent (URP/Standard Shader)
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    public void PlayDeathVFXHandler<TState>(BaseEntity<TState> entity) where TState : System.Enum
    {
        StartCoroutine(PlayDeathVFX(entity));
    }

    public void PlayerPlayDeathVFXHandler()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null)
            return;

        StartCoroutine(PlayDeathVFX(player));
    }

    private IEnumerator PlayDeathVFX<TState>(BaseEntity<TState> entity) where TState : System.Enum
    {
        
        GameObject clone = Instantiate(
            entity.gameObject,
            entity.transform.position,
            entity.transform.rotation
        );


        Animator animator = clone.GetComponentInChildren<Animator>();
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        animator.SetTrigger("Die");

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found for Death VFX.");
        }
        if (animator == null)
        {
            Debug.LogWarning("No animator found for Death VFX.");
        }

        yield return new WaitForSeconds(fadeDelay);

        float t = 0;

        SkinnedMeshRenderer[] skinnedRenderers = clone.GetComponentsInChildren<SkinnedMeshRenderer>();
        MeshRenderer[] meshRenderers = clone.GetComponentsInChildren<MeshRenderer>();

        List<Material> materials = new List<Material>();

        foreach (var r in skinnedRenderers)
            materials.AddRange(r.materials);
        foreach (var r in meshRenderers)
            materials.AddRange(r.materials);

       foreach (var mat in materials)
        {
            MakeMaterialTransparent(mat);
        } 

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);

            foreach (var mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }

            yield return null;
        }

        Destroy(clone);
    }

    public void PlayMuzzleFlashVFXHandler(GameObject attacker)
    {
        Transform gun = FindDeepChild(attacker.transform, "Gun");

        if (gun == null)
        {
            Debug.LogWarning("Gun transform not found");
            return;
        }

        GameObject muzzleFlash = Instantiate(muzzleFlashVFXPrefab, gun.position + gun.right * offsetFromGun, gun.rotation);
        muzzleFlash.GetComponent<MuzzleFlashVFX>().StartCoroutine("PlayMuzzleFlashVFX");
    }

    public void PlayerPlayMuzzleFlashVFXHandler(Weapon weapon)
    {
        Player player = FindObjectOfType<Player>();
        if (player == null)
            return;
        Transform gun = FindDeepChild(player.transform, "Gun");

        if (gun == null)
        {
            Debug.LogWarning("Gun transform not found");
            return;
        }

        GameObject muzzleFlash = Instantiate(muzzleFlashVFXPrefab, gun.position + gun.right * offsetFromGun, gun.rotation);
        muzzleFlash.GetComponent<MuzzleFlashVFX>().StartCoroutine("PlayMuzzleFlashVFX");
    }

    public void PlayBloodSplatKnifeVFXHandler(Transform position)
    {
        PlayBloodSplatVFX(position.position, Vector3.zero, false);
    }

    private void PlayBloodSplatVFX(Vector3 position, Vector3 rotation, bool isGunshot)
    {
        ParticleSystem bloodSplatPrefab = isGunshot ? bloodSplatGunVFXPrefab : bloodSplatKnifeVFXPrefab;
        ParticleSystem bloodSplatInstance = Instantiate(bloodSplatPrefab, position, Quaternion.Euler(rotation));
        Debug.Log("Playing Blood Splat VFX at " + position);
        bloodSplatInstance.Play();
        Destroy(bloodSplatInstance.gameObject, bloodSplatInstance.main.duration + bloodSplatInstance.main.startLifetime.constantMax);
    }
    public void PlayBulletImpactVFXHandler(GameObject hitObject, Bullet bullet)
    {
        Vector3 position = new Vector3(bullet.transform.position.x, bullet.transform.position.y, bullet.transform.position.z + zOffset);
        Vector3 direction = new Vector3(-bullet.Direction.x, -bullet.Direction.y, 0f);
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.forward);
        if (hitObject is IEntity)
            PlayBloodSplatVFX(position, rotation.eulerAngles, true);
        else
            PlayBulletObjectHitVFX(position, rotation.eulerAngles);
    }

    private void PlayBulletObjectHitVFX(Vector3 position, Vector3 rotation)
    {
        ParticleSystem objectHitVFX = Instantiate(objectHitVFXPrefab, position, Quaternion.Euler(rotation));
        objectHitVFX.Play();
        Destroy(objectHitVFX.gameObject, objectHitVFX.main.duration + objectHitVFX.main.startLifetime.constantMax);
    }

    public void PlayScreenShakeVFXHandler()
    {
        screenShakeVFX.Shake();
    }
}